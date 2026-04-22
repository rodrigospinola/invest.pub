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

    public VertexAiService(IConfiguration configuration, HttpClient httpClient)
    {
        _httpClient = httpClient;
        _project = configuration["VERTEX_AI_PROJECT"]
            ?? throw new InvalidOperationException("VERTEX_AI_PROJECT não configurado.");
        _location = configuration["VERTEX_AI_LOCATION"] ?? "us-central1";
        _model = configuration["VERTEX_AI_MODEL"] ?? "claude-haiku-4-5@20251001";
    }

    public async Task<VertexAiResponse> SendMessageAsync(VertexAiRequest request)
    {
        var token = await GetAccessTokenAsync();

        var endpoint = $"https://{_location}-aiplatform.googleapis.com/v1/projects/{_project}" +
                       $"/locations/{_location}/publishers/anthropic/models/{_model}:rawPredict";

        var body = BuildRequestBody(request);
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var httpResponse = await _httpClient.SendAsync(httpRequest);
        var responseJson = await httpResponse.Content.ReadAsStringAsync();

        if (!httpResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"Vertex AI error {httpResponse.StatusCode}: {responseJson}");

        return ParseResponse(responseJson);
    }

    private object BuildRequestBody(VertexAiRequest request)
    {
        var messages = new List<object>();

        foreach (var msg in request.Messages)
        {
            if (msg.Role == "tool")
            {
                // Tool result — formato especial do Anthropic
                var toolData = JsonSerializer.Deserialize<JsonElement>(msg.Content);
                messages.Add(new
                {
                    role = "user",
                    content = new[]
                    {
                        new
                        {
                            type = "tool_result",
                            tool_use_id = toolData.GetProperty("tool_use_id").GetString(),
                            content = toolData.GetProperty("content").GetString()
                        }
                    }
                });
            }
            else if (msg.Role == "assistant" && msg.Content.TrimStart().StartsWith("["))
            {
                // Tool use do assistente
                var toolUses = JsonSerializer.Deserialize<List<VertexAiToolUse>>(msg.Content);
                messages.Add(new
                {
                    role = "assistant",
                    content = toolUses!.Select(tu => new
                    {
                        type = "tool_use",
                        id = tu.Id,
                        name = tu.Name,
                        input = tu.Input
                    }).ToArray()
                });
            }
            else
            {
                messages.Add(new { role = msg.Role, content = msg.Content });
            }
        }

        var body = new Dictionary<string, object>
        {
            ["anthropic_version"] = "vertex-2023-10-16",
            ["max_tokens"] = request.MaxTokens,
            ["system"] = request.SystemPrompt,
            ["messages"] = messages
        };

        if (request.Tools != null && request.Tools.Any())
        {
            body["tools"] = request.Tools.Select(t => new
            {
                name = t.Name,
                description = t.Description,
                input_schema = t.InputSchema
            }).ToList();
        }

        return body;
    }

    private static VertexAiResponse ParseResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var stopReason = root.TryGetProperty("stop_reason", out var sr) ? sr.GetString() ?? "end_turn" : "end_turn";
        var content = root.GetProperty("content");

        var textParts = new List<string>();
        var toolUses = new List<VertexAiToolUse>();

        foreach (var item in content.EnumerateArray())
        {
            var type = item.GetProperty("type").GetString();
            if (type == "text")
            {
                textParts.Add(item.GetProperty("text").GetString() ?? "");
            }
            else if (type == "tool_use")
            {
                toolUses.Add(new VertexAiToolUse(
                    item.GetProperty("id").GetString() ?? Guid.NewGuid().ToString(),
                    item.GetProperty("name").GetString() ?? "",
                    item.GetProperty("input")
                ));
            }
        }

        return new VertexAiResponse(
            string.Join("\n", textParts),
            toolUses.Any() ? toolUses : null,
            stopReason
        );
    }

    private static async Task<string> GetAccessTokenAsync()
    {
        var credential = await GoogleCredential
            .GetApplicationDefaultAsync();
        var scoped = credential.CreateScoped("https://www.googleapis.com/auth/cloud-platform");
        var token = await scoped.UnderlyingCredential.GetAccessTokenForRequestAsync();
        return token;
    }
}
