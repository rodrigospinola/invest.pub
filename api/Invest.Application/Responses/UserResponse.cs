namespace Invest.Application.Responses;

public record UserResponse(
    Guid Id,
    string Nome,
    string Email,
    string? Telefone,
    string Status,
    string OnboardingStep,
    DateTime CreatedAt
);
