using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Invest.Domain.Entities;

namespace Invest.Infrastructure.Data.Configurations;

public class UserSubStrategyConfiguration : IEntityTypeConfiguration<UserSubStrategy>
{
    public void Configure(EntityTypeBuilder<UserSubStrategy> builder)
    {
        builder.ToTable("user_sub_strategies");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.UserId).HasColumnName("user_id").IsRequired();
        builder.HasIndex(s => s.UserId).IsUnique();

        builder.Property(s => s.SubEstrategiaAcoes)
            .HasColumnName("sub_estrategia_acoes")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(s => s.SubEstrategiaFiis)
            .HasColumnName("sub_estrategia_fiis")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();
    }
}
