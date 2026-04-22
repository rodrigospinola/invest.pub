using Invest.Application.Queries.Chat;
using Invest.Application.Common;
using Invest.Application.Responses;
using Invest.Domain.Constants;
using Invest.Domain.Enums;
using Invest.Domain.Interfaces;
using Invest.Domain.Services;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Invest.Application.Handlers;

public class ChatHandler
{
    private readonly IVertexAiService _vertexAiService;
    private readonly IUserProfileRepository _profileRepository;
    private readonly AllocationService _allocationService;

    public ChatHandler(
        IVertexAiService vertexAiService,
        IUserProfileRepository profileRepository,
        AllocationService allocationService)
    {
        _vertexAiService = vertexAiService;
        _profileRepository = profileRepository;
        _allocationService = allocationService;
    }

    public async Task<Result<ChatResponse>> SendMessageAsync(SendChatMessageQuery query)
    {
        var systemPrompt = GetSystemPrompt(query.Context);
        var tools = GetTools(query.Context);

        // Limita histórico às últimas 10 mensagens para reduzir tokens enviados ao modelo
        const int MaxHistory = 10;
        var history = query.History ?? new List<ChatMessage>();
        if (history.Count > MaxHistory)
            history = history.Skip(history.Count - MaxHistory).ToList();

        var messages = history
            .Select(m => new VertexAiMessage(m.Role, m.Content))
            .ToList();

        messages.Add(new VertexAiMessage("user", query.Message));

        var toolsCalled = new List<string>();
        List<AllocationPreviewItem>? allocationPreview = null;

        // Loop agentico: continua até o modelo dar resposta final (sem tool use)
        for (int i = 0; i < 5; i++)
        {
            var request = new VertexAiRequest(systemPrompt, messages, tools);
            var response = await _vertexAiService.SendMessageAsync(request);

            if (response.StopReason == "end_turn" || response.ToolUses == null || !response.ToolUses.Any())
            {
                var (cleanText, suggestedReplies) = ParseSuggestions(response.Text);

                var responseHistory = messages
                    .Where(m => m.Role != "tool" &&
                                !(m.Role == "assistant" && m.Content.TrimStart().StartsWith("[")))
                    .Select(m => new ChatMessageResponse(m.Role, m.Content))
                    .Append(new ChatMessageResponse("assistant", cleanText))
                    .ToList();

                return Result<ChatResponse>.Success(new ChatResponse(
                    cleanText,
                    responseHistory,
                    toolsCalled.Any() ? toolsCalled : null,
                    suggestedReplies,
                    allocationPreview
                ));
            }

            messages.Add(new VertexAiMessage("assistant", JsonSerializer.Serialize(response.ToolUses)));

            foreach (var toolUse in response.ToolUses)
            {
                toolsCalled.Add(toolUse.Name);
                var toolResult = await ExecuteToolAsync(toolUse, query.UserId);

                // Captura o preview da alocação para exibir no chat
                if (toolUse.Name == "get_allocation")
                    allocationPreview = ParseAllocationPreview(toolResult);

                messages.Add(new VertexAiMessage("tool",
                    JsonSerializer.Serialize(new { tool_use_id = toolUse.Id, name = toolUse.Name, content = toolResult })));
            }
        }

        return Result<ChatResponse>.Failure("CHAT_ERROR", "O agente não conseguiu completar a resposta.");
    }

    private async Task<string> ExecuteToolAsync(VertexAiToolUse toolUse, Guid userId)
    {
        var inputJson = JsonSerializer.Serialize(toolUse.Input);
        var input = JsonSerializer.Deserialize<JsonElement>(inputJson);

        return toolUse.Name switch
        {
            "get_allocation" => ExecuteGetAllocation(input),
            "compare_portfolios" => await ExecuteComparePortfolios(input, userId),
            "save_profile" => await ExecuteSaveProfile(input, userId),
            _ => $"Tool '{toolUse.Name}' não reconhecida."
        };
    }

    private string ExecuteGetAllocation(JsonElement input)
    {
        var perfilStr = input.GetProperty("perfil").GetString()?.ToLower() ?? "moderado";
        var valor = input.GetProperty("valor").GetDecimal();

        var perfil = perfilStr switch
        {
            "conservador" => PerfilRisco.Conservador,
            "arrojado" => PerfilRisco.Arrojado,
            _ => PerfilRisco.Moderado
        };

        var faixa = _allocationService.DeterminarFaixa(valor);
        var alocacao = AllocationTargets.Get(faixa, perfil);

        return JsonSerializer.Serialize(new
        {
            faixa = faixa.ToString(),
            perfil = perfilStr,
            classes = alocacao.Select(a => new { classe = a.classe, percentual = a.percentual })
        });
    }

    private async Task<string> ExecuteComparePortfolios(JsonElement input, Guid userId)
    {
        var profile = await _profileRepository.GetByUserIdAsync(userId);
        if (profile == null)
            return JsonSerializer.Serialize(new { erro = "Perfil não encontrado." });

        var carteiraAtualElement = input.GetProperty("carteira_atual");
        var carteiraAtual = JsonSerializer.Deserialize<Dictionary<string, decimal>>(carteiraAtualElement.GetRawText())
            ?? new Dictionary<string, decimal>();

        var recomendado = _allocationService.ObterAlocacao(profile.Perfil, profile.ValorTotal)
            .ToDictionary(a => a.classe, a => a.percentual);

        var comparacao = recomendado.Keys.Union(carteiraAtual.Keys).Distinct()
            .Select(classe => new
            {
                classe,
                atual = carteiraAtual.GetValueOrDefault(classe, 0m),
                recomendado = recomendado.GetValueOrDefault(classe, 0m),
                delta = recomendado.GetValueOrDefault(classe, 0m) - carteiraAtual.GetValueOrDefault(classe, 0m)
            }).ToList();

        return JsonSerializer.Serialize(new { comparacao });
    }

    private async Task<string> ExecuteSaveProfile(JsonElement input, Guid userId)
    {
        var existing = await _profileRepository.GetByUserIdAsync(userId);

        var perfilStr = input.GetProperty("perfil").GetString()?.ToLower() ?? "moderado";
        var valor = input.GetProperty("valor").GetDecimal();

        var perfil = perfilStr switch
        {
            "conservador" => PerfilRisco.Conservador,
            "arrojado" => PerfilRisco.Arrojado,
            _ => PerfilRisco.Moderado
        };

        var faixa = _allocationService.DeterminarFaixa(valor);

        if (existing == null)
        {
            var profile = Domain.Entities.UserProfile.Create(userId, perfil, valor, faixa, false);
            await _profileRepository.AddAsync(profile);
        }
        else
        {
            existing.Update(perfil, valor, faixa);
            await _profileRepository.UpdateAsync(existing);
        }

        return JsonSerializer.Serialize(new { sucesso = true, mensagem = "Perfil salvo com sucesso." });
    }

    private static List<AllocationPreviewItem>? ParseAllocationPreview(string toolResult)
    {
        try
        {
            var doc = JsonSerializer.Deserialize<JsonElement>(toolResult);
            if (!doc.TryGetProperty("classes", out var classes)) return null;

            return classes.EnumerateArray()
                .Select(c => new AllocationPreviewItem(
                    c.GetProperty("classe").GetString() ?? "",
                    c.GetProperty("percentual").GetDecimal()))
                .ToList();
        }
        catch (JsonException) { return null; }
    }

    private static (string text, List<string>? suggestions) ParseSuggestions(string text)
    {
        var match = Regex.Match(text, @"\nSUGESTÕES:\s*(.+)$", RegexOptions.Multiline);
        if (!match.Success)
            return (text.Trim(), null);

        var suggestions = match.Groups[1].Value
            .Split('|')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        var cleanText = text[..match.Index].Trim();
        return (cleanText, suggestions.Any() ? suggestions : null);
    }

    private static string GetSystemPrompt(string context) => context switch
    {
        "onboarding" => SystemPrompts.Onboarding,
        "comparison" => SystemPrompts.Comparison,
        _ => SystemPrompts.Onboarding
    };

    private static List<VertexAiToolDefinition> GetTools(string context)
    {
        var tools = new List<VertexAiToolDefinition>
        {
            new("get_allocation",
                "Retorna a alocação-alvo por classe de ativo dado o perfil de risco e valor total do investidor.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        perfil = new { type = "string", description = "Perfil de risco: conservador, moderado ou arrojado" },
                        valor = new { type = "number", description = "Valor total disponível para investir em reais" }
                    },
                    required = new[] { "perfil", "valor" }
                }),
            new("save_profile",
                "Salva o perfil do investidor no banco após confirmação do usuário.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        perfil = new { type = "string", description = "Perfil de risco classificado: conservador, moderado ou arrojado" },
                        valor = new { type = "number", description = "Valor total do investidor em reais" }
                    },
                    required = new[] { "perfil", "valor" }
                })
        };

        if (context == "comparison")
        {
            tools.Add(new("compare_portfolios",
                "Compara a carteira atual do investidor com a carteira recomendada.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        carteira_atual = new
                        {
                            type = "object",
                            description = "Carteira atual com percentuais por classe. Ex: {\"RF Pós\": 60, \"Ações\": 40}"
                        }
                    },
                    required = new[] { "carteira_atual" }
                }));
        }

        return tools;
    }
}
