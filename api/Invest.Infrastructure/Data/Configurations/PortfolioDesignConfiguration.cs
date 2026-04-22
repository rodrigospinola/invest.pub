using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Invest.Domain.Entities;

namespace Invest.Infrastructure.Data.Configurations;

public class PortfolioDesignConfiguration : IEntityTypeConfiguration<PortfolioDesign>
{
    public void Configure(EntityTypeBuilder<PortfolioDesign> builder)
    {
        builder.ToTable("portfolio_designs");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.UserId).HasColumnName("user_id").IsRequired();
        builder.HasIndex(p => p.UserId);

        builder.Property(p => p.BatchRunId).HasColumnName("batch_run_id").IsRequired();

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(p => p.ValorTotal).HasColumnName("valor_total").HasPrecision(12, 2).IsRequired();
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
    }
}
