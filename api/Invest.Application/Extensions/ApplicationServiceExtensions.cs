using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Invest.Application.Commands.Auth;
using Invest.Application.Commands.Portfolio;
using Invest.Application.Commands.Profile;
using Invest.Application.Commands.User;
using Invest.Application.Handlers;
using Invest.Application.Validators;
using Invest.Domain.Services;

namespace Invest.Application.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationHandlers(this IServiceCollection services)
    {
        services.AddScoped<AuthHandler>();
        services.AddScoped<UserHandler>();
        services.AddScoped<AllocationService>();
        services.AddScoped<ProfileHandler>();
        services.AddScoped<ChatHandler>();
        services.AddScoped<SubStrategyHandler>();
        services.AddScoped<RankingHandler>();
        services.AddScoped<PortfolioHandler>();
        services.AddScoped<DeviationCalculator>();
        services.AddScoped<DashboardHandler>();
        services.AddScoped<AlertHandler>();
        services.AddScoped<B3ImportHandler>();

        services.AddScoped<IValidator<RegisterCommand>, RegisterValidator>();
        services.AddScoped<IValidator<LoginCommand>, LoginValidator>();
        services.AddScoped<IValidator<UpdateUserCommand>, UpdateUserValidator>();
        services.AddScoped<IValidator<CreateProfileCommand>, CreateProfileValidator>();
        services.AddScoped<IValidator<ImportPortfolioCommand>, ImportPortfolioValidator>();

        return services;
    }
}
