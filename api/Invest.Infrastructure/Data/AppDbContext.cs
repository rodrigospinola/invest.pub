using Microsoft.EntityFrameworkCore;
using Invest.Domain.Entities;
using Invest.Infrastructure.Data.Configurations;

namespace Invest.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserSubStrategy> UserSubStrategies => Set<UserSubStrategy>();
    public DbSet<PortfolioDesign> PortfolioDesigns => Set<PortfolioDesign>();
    public DbSet<UserAsset> UserAssets => Set<UserAsset>();
    public DbSet<BatchRun> BatchRuns => Set<BatchRun>();
    public DbSet<BatchRanking> BatchRankings => Set<BatchRanking>();
    public DbSet<PortfolioHistory> PortfolioHistories => Set<PortfolioHistory>();
    public DbSet<AssetHistory> AssetHistories => Set<AssetHistory>();
    public DbSet<Benchmark> Benchmarks => Set<Benchmark>();
    public DbSet<Alert> Alerts => Set<Alert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new UserProfileConfiguration());
        modelBuilder.ApplyConfiguration(new UserSubStrategyConfiguration());
        modelBuilder.ApplyConfiguration(new PortfolioDesignConfiguration());
        modelBuilder.ApplyConfiguration(new UserAssetConfiguration());
        modelBuilder.ApplyConfiguration(new BatchRunConfiguration());
        modelBuilder.ApplyConfiguration(new BatchRankingConfiguration());
        modelBuilder.ApplyConfiguration(new PortfolioHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new AssetHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new BenchmarkConfiguration());
        modelBuilder.ApplyConfiguration(new AlertConfiguration());
    }
}
