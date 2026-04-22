using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Invest.Domain.Entities;

namespace Invest.Infrastructure.Data.Configurations;

public class UserAssetConfiguration : IEntityTypeConfiguration<UserAsset>
{
    public void Configure(EntityTypeBuilder<UserAsset> builder)
    {
        builder.ToTable("user_assets");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.UserId).HasColumnName("user_id").IsRequired();
        builder.HasIndex(a => a.UserId);

        builder.Property(a => a.PortfolioDesignId).HasColumnName("portfolio_design_id");
        builder.Property(a => a.Ticker).HasColumnName("ticker").HasMaxLength(20).IsRequired();
        builder.HasIndex(a => a.Ticker);

        builder.Property(a => a.Nome).HasColumnName("nome").HasMaxLength(200).IsRequired();

        builder.Property(a => a.Classe)
            .HasColumnName("classe")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(a => a.SubEstrategia).HasColumnName("sub_estrategia").HasMaxLength(50);
        builder.Property(a => a.Quantidade).HasColumnName("quantidade").HasPrecision(18, 4).IsRequired();
        builder.Property(a => a.PrecoMedio).HasColumnName("preco_medio").HasPrecision(12, 4).IsRequired();

        builder.Property(a => a.Origem)
            .HasColumnName("origem")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(a => a.Ativo).HasColumnName("ativo").IsRequired();
        builder.Property(a => a.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at").IsRequired();
    }
}
