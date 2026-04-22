using FluentValidation;
using Invest.Application.Commands.Auth;
using Invest.Application.Common;
using Invest.Application.Responses;
using Invest.Domain.Entities;
using Invest.Domain.Enums;
using Invest.Domain.Interfaces;

namespace Invest.Application.Handlers;

public class AuthHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IValidator<RegisterCommand> _registerValidator;
    private readonly IValidator<LoginCommand> _loginValidator;

    public AuthHandler(
        IUserRepository userRepository,
        IJwtService jwtService,
        IPasswordHasher passwordHasher,
        IValidator<RegisterCommand> registerValidator,
        IValidator<LoginCommand> loginValidator)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterCommand command)
    {
        var validation = await _registerValidator.ValidateAsync(command);
        if (!validation.IsValid)
        {
            var error = validation.Errors.First();
            return Result<AuthResponse>.Failure("VALIDATION_ERROR", error.ErrorMessage, error.PropertyName);
        }

        if (await _userRepository.EmailExistsAsync(command.Email))
            return Result<AuthResponse>.Failure("EMAIL_ALREADY_EXISTS", "Email já está em uso.", "email");

        var passwordHash = _passwordHasher.Hash(command.Password);
        var user = User.Create(command.Nome, command.Email.ToLower(), passwordHash, command.Telefone);

        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(30);
        user.SetRefreshToken(refreshToken, refreshTokenExpiry);

        await _userRepository.AddAsync(user);

        var accessToken = _jwtService.GenerateAccessToken(user);
        var expiresAt = DateTime.UtcNow.Add(_jwtService.GetAccessTokenExpiration());

        return Result<AuthResponse>.Success(new AuthResponse(
            accessToken,
            refreshToken,
            expiresAt,
            MapToUserResponse(user)
        ));
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginCommand command)
    {
        var validation = await _loginValidator.ValidateAsync(command);
        if (!validation.IsValid)
        {
            var error = validation.Errors.First();
            return Result<AuthResponse>.Failure("VALIDATION_ERROR", error.ErrorMessage, error.PropertyName);
        }

        var user = await _userRepository.GetByEmailAsync(command.Email);
        if (user == null || !_passwordHasher.Verify(command.Password, user.PasswordHash))
            return Result<AuthResponse>.Failure("INVALID_CREDENTIALS", "Email ou senha incorretos.");

        if (user.Status == UserStatus.Inativo)
            return Result<AuthResponse>.Failure("ACCOUNT_INACTIVE", "Conta desativada.");

        var refreshToken = _jwtService.GenerateRefreshToken();
        user.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(30));
        await _userRepository.UpdateAsync(user);

        var accessToken = _jwtService.GenerateAccessToken(user);
        var expiresAt = DateTime.UtcNow.Add(_jwtService.GetAccessTokenExpiration());

        return Result<AuthResponse>.Success(new AuthResponse(
            accessToken,
            refreshToken,
            expiresAt,
            MapToUserResponse(user)
        ));
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenCommand command)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(command.RefreshToken);
        if (user == null || !user.HasValidRefreshToken(command.RefreshToken))
            return Result<AuthResponse>.Failure("INVALID_REFRESH_TOKEN", "Token de atualização inválido ou expirado.");

        var newRefreshToken = _jwtService.GenerateRefreshToken();
        user.SetRefreshToken(newRefreshToken, DateTime.UtcNow.AddDays(30));
        await _userRepository.UpdateAsync(user);

        var accessToken = _jwtService.GenerateAccessToken(user);
        var expiresAt = DateTime.UtcNow.Add(_jwtService.GetAccessTokenExpiration());

        return Result<AuthResponse>.Success(new AuthResponse(
            accessToken,
            newRefreshToken,
            expiresAt,
            MapToUserResponse(user)
        ));
    }

    public async Task<Result<MessageResponse>> ForgotPasswordAsync(ForgotPasswordCommand command)
    {
        // Fase 0: resposta sempre genérica para não revelar se o email existe
        // TODO Fase 5+: integrar com serviço de email
        await Task.CompletedTask;
        return Result<MessageResponse>.Success(
            new MessageResponse("Se o email estiver cadastrado, você receberá as instruções em breve."));
    }

    public async Task<Result<MessageResponse>> ResetPasswordAsync(ResetPasswordCommand command)
    {
        // TODO Fase 5+: implementar fluxo completo de reset com token por email
        await Task.CompletedTask;
        return Result<MessageResponse>.Failure("NOT_IMPLEMENTED", "Funcionalidade disponível em breve.");
    }

    public async Task<Result<MessageResponse>> LogoutAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return Result<MessageResponse>.Failure("USER_NOT_FOUND", "Usuário não encontrado.");

        user.RevokeRefreshToken();
        await _userRepository.UpdateAsync(user);

        return Result<MessageResponse>.Success(new MessageResponse("Logout realizado com sucesso."));
    }

    private static UserResponse MapToUserResponse(User user) =>
        new(user.Id, user.Nome, user.Email, user.Telefone,
            user.Status.ToString(), user.OnboardingStep.ToString(), user.CreatedAt);
}
