using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Invest.Domain.Interfaces;
using Invest.Infrastructure.Data;
using Invest.Infrastructure.Repositories;
using Invest.Infrastructure.Services;

namespace Invest.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["DATABASE_URL"]
            ?? throw new InvalidOperationException("DATABASE_URL não configurada.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IUserSubStrategyRepository, UserSubStrategyRepository>();
        services.AddScoped<IUserAssetRepository, UserAssetRepository>();
        services.AddScoped<IBatchRankingRepository, BatchRankingRepository>();
        services.AddScoped<IPortfolioHistoryRepository, PortfolioHistoryRepository>();
        services.AddScoped<IAssetHistoryRepository, AssetHistoryRepository>();
        services.AddScoped<IBenchmarkRepository, BenchmarkRepository>();
        services.AddScoped<IAlertRepository, AlertRepository>();

        services.AddHttpClient<IVertexAiService, VertexAiService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
