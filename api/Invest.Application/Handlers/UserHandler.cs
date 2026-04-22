using FluentValidation;
using Invest.Application.Commands.User;
using Invest.Application.Common;
using Invest.Application.Queries.User;
using Invest.Application.Responses;
using Invest.Domain.Entities;
using Invest.Domain.Interfaces;

namespace Invest.Application.Handlers;

public class UserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IValidator<UpdateUserCommand> _updateValidator;

    public UserHandler(IUserRepository userRepository, IValidator<UpdateUserCommand> updateValidator)
    {
        _userRepository = userRepository;
        _updateValidator = updateValidator;
    }

    public async Task<Result<UserResponse>> GetUserAsync(GetUserQuery query)
    {
        var user = await _userRepository.GetByIdAsync(query.UserId);
        if (user == null)
            return Result<UserResponse>.Failure("USER_NOT_FOUND", "Usuário não encontrado.");

        return Result<UserResponse>.Success(MapToUserResponse(user));
    }

    public async Task<Result<UserResponse>> UpdateUserAsync(UpdateUserCommand command)
    {
        var validation = await _updateValidator.ValidateAsync(command);
        if (!validation.IsValid)
        {
            var error = validation.Errors.First();
            return Result<UserResponse>.Failure("VALIDATION_ERROR", error.ErrorMessage, error.PropertyName);
        }

        var user = await _userRepository.GetByIdAsync(command.UserId);
        if (user == null)
            return Result<UserResponse>.Failure("USER_NOT_FOUND", "Usuário não encontrado.");

        user.Update(command.Nome, command.Telefone);
        await _userRepository.UpdateAsync(user);

        return Result<UserResponse>.Success(MapToUserResponse(user));
    }

    public async Task<Result<MessageResponse>> DeactivateUserAsync(DeactivateUserCommand command)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId);
        if (user == null)
            return Result<MessageResponse>.Failure("USER_NOT_FOUND", "Usuário não encontrado.");

        user.Deactivate();
        await _userRepository.UpdateAsync(user);

        return Result<MessageResponse>.Success(new MessageResponse("Conta desativada com sucesso."));
    }

    private static UserResponse MapToUserResponse(User user) =>
        new(user.Id, user.Nome, user.Email, user.Telefone,
            user.Status.ToString(), user.OnboardingStep.ToString(), user.CreatedAt);
}
