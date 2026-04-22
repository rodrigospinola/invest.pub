using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Invest.Domain.Entities;

namespace Invest.Infrastructure.Data.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.UserId).HasColumnName("user_id").IsRequired();
        builder.HasIndex(p => p.UserId).IsUnique();

        builder.Property(p => p.Perfil)
            .HasColumnName("perfil")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(p => p.ValorTotal).HasColumnName("valor_total").HasPrecision(12, 2).IsRequired();

        builder.Property(p => p.Faixa)
            .HasColumnName("faixa")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(p => p.TemCarteiraExistente).HasColumnName("tem_carteira_existente").IsRequired();
        builder.Property(p => p.CarteiraAnteriorJson).HasColumnName("carteira_anterior");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").IsRequired();
    }
}
