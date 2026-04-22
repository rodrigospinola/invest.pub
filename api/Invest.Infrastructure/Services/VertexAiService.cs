using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Invest.Domain.Interfaces;

namespace Invest.Infrastructure.Services;

public class VertexAiService : IVertexAiService
{
    private readonly HttpClient _httpClient;
    private readonly string _project;
    private readonly string _location;
    private readonly string _model;
    private readonly string? _apiKey;

    public VertexAiService(IConfiguration configuration, HttpClient httpClient)
    {
        _httpClient = httpClient;
        _project = configuration["VERTEX_AI_PROJECT"] ?? "";
        _location = configuration["VERTEX_AI_LOCATION"] ?? "us-central1";
        _model = configuration["VERTEX_AI_MODEL"] ?? "gemini-2.5-flash";
        _apiKey = configuration["GEMINI_API_KEY"];
    }

    public async Task<VertexAiResponse> SendMessageAsync(VertexAiRequest request)
    {
        string endpoint;
        string? token = null;

        if (!string.IsNullOrEmpty(_apiKey))
        {
            // Google AI Studio — sem necessidade de GCP/ADC
            endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
        }
        else
        {
            // Vertex AI — usa ADC
            token = await GetAccessTokenAsync();
            endpoint = $"https://{_location}-aiplatform.googleapis.com/v1/projects/{_project}" +
                       $"/locations/{_location}/publishers/google/models/{_model}:generateContent";
        }

        var body = BuildRequestBody(request);
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        if (token != null)
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var httpResponse = await _httpClient.SendAsync(httpRequest);
        var responseJson = await httpResponse.Content.ReadAsStringAsync();

        if (!httpResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"Gemini error {httpResponse.StatusCode}: {responseJson}");

        return ParseResponse(responseJson);
    }

    private static object BuildRequestBody(VertexAiRequest request)
    {
        var contents = new List<object>();

        foreach (var msg in request.Messages)
        {
            if (msg.Role == "tool")
            {
                // Tool result → functionResponse em mensagem user
                var toolData = JsonSerializer.Deserialize<JsonElement>(msg.Content);
                var toolUseId = toolData.GetProperty("tool_use_id").GetString() ?? "";
                var content = toolData.GetProperty("content").GetString() ?? "";

                // Id foi codificado como "toolname_guid" — recupera o nome
                var toolName = toolUseId.Contains('_')
                    ? toolUseId[..toolUseId.LastIndexOf('_')]
                    : toolUseId;

                contents.Add(new
                {
                    role = "user",
                    parts = new object[]
                    {
                        new
                        {
                            functionResponse = new
                            {
                                name = toolName,
                                response = new { result = content }
                            }
                        }
                    }
                });
            }
            else if (msg.Role == "assistant" && msg.Content.TrimStart().StartsWith("["))
            {
                // Tool use do assistente → functionCall parts
                var toolUses = JsonSerializer.Deserialize<List<VertexAiToolUse>>(msg.Content);
                contents.Add(new
                {
                    role = "model",
                    parts = toolUses!.Select(tu =>
                    {
                        var argsJson = JsonSerializer.Serialize(tu.Input);
                        var args = JsonSerializer.Deserialize<JsonElement>(argsJson);
                        return (object)new { functionCall = new { name = tu.Name, args } };
                    }).ToArray()
                });
            }
            else
            {
                var role = msg.Role == "assistant" ? "model" : "user";
                contents.Add(new
                {
                    role,
                    parts = new[] { new { text = msg.Content } }
                });
            }
        }

        var bodyDict = new Dictionary<string, object>
        {
            ["contents"] = contents,
            ["systemInstruction"] = new
            {
                parts = new[] { new { text = request.SystemPrompt } }
            },
            ["generationConfig"] = new
            {
                maxOutputTokens = request.MaxTokens,
                temperature = 0.7
            }
        };

        if (request.Tools != null && request.Tools.Count != 0)
        {
            bodyDict["tools"] = new[]
            {
                new
                {
                    functionDeclarations = request.Tools.Select(t => new
                    {
                        name = t.Name,
                        description = t.Description,
                        parameters = t.InputSchema
                    }).ToArray()
                }
            };
        }

        return bodyDict;
    }

    private static VertexAiResponse ParseResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var candidate = root.GetProperty("candidates")[0];
        var parts = candidate.GetProperty("content").GetProperty("parts");

        var textParts = new List<string>();
        var toolUses = new List<VertexAiToolUse>();

        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("text", out var textProp))
            {
                textParts.Add(textProp.GetString() ?? "");
            }
            else if (part.TryGetProperty("functionCall", out var fc))
            {
                var name = fc.GetProperty("name").GetString() ?? "";
                object inputObj = fc.TryGetProperty("args", out var argsEl)
                    ? (object)argsEl.Clone()
                    : new { };

                // Codifica nome no Id para recuperar depois no tool result
                var id = $"{name}_{Guid.NewGuid():N}";
                toolUses.Add(new VertexAiToolUse(id, name, inputObj));
            }
        }

        var stopReason = toolUses.Count != 0 ? "tool_use" : "end_turn";

        return new VertexAiResponse(
            string.Join("\n", textParts),
            toolUses.Count != 0 ? toolUses : null,
            stopReason
        );
    }

    private static async Task<string> GetAccessTokenAsync()
    {
        var credential = await GoogleCredential.GetApplicationDefaultAsync();
        var scoped = credential.CreateScoped("https://www.googleapis.com/auth/cloud-platform");
        return await scoped.UnderlyingCredential.GetAccessTokenForRequestAsync();
    }
}
