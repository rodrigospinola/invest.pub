using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Invest.Domain.Entities;

namespace Invest.Infrastructure.Data.Configurations;

public class PortfolioHistoryConfiguration : IEntityTypeConfiguration<PortfolioHistory>
{
    public void Configure(EntityTypeBuilder<PortfolioHistory> builder)
    {
        builder.ToTable("portfolio_history");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Data).IsRequired().HasColumnType("date");
        builder.Property(x => x.ValorTotal).HasPrecision(18, 2);
        builder.Property(x => x.RentabilidadeNoDia).HasPrecision(18, 4);
        builder.Property(x => x.RentabilidadeAcumulada).HasPrecision(18, 4);
        builder.Property(x => x.DistanciaMeta).HasPrecision(18, 2);
        builder.Property(x => x.AlocacaoRealJson).IsRequired().HasColumnType("jsonb");
        
        builder.HasIndex(x => new { x.UserId, x.Data }).IsUnique();
    }
}
