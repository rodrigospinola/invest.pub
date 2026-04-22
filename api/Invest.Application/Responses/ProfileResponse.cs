namespace Invest.Application.Responses;

public record AllocationClasseResponse(string Classe, decimal Percentual);

public record ProfileResponse(
    Guid Id,
    Guid UserId,
    string Perfil,
    decimal ValorTotal,
    string Faixa,
    bool TemCarteiraExistente,
    List<AllocationClasseResponse> AlocacaoAlvo,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
