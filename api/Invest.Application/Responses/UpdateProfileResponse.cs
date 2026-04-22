namespace Invest.Application.Responses;

public record UpdateProfileResponse(
    Guid Id,
    string Perfil,
    decimal ValorTotal,
    string Faixa,
    List<AllocationClasseResponse> AlocacaoAlvo,
    bool MudouFaixa
);
