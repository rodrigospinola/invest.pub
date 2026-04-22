namespace Invest.Application.Responses;

public record AllocationResponse(
    string Faixa,
    string Perfil,
    List<AllocationClasseResponse> Classes
);
