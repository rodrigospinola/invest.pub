using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Invest.Domain.Entities;

namespace Invest.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");

        builder.Property(u => u.Nome).HasColumnName("nome").HasMaxLength(255).IsRequired();
        builder.Property(u => u.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").IsRequired();
        builder.Property(u => u.Telefone).HasColumnName("telefone").HasMaxLength(20);

        builder.Property(u => u.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.Property(u => u.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(u => u.OnboardingStep)
            .HasColumnName("onboarding_step")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(u => u.RefreshToken).HasColumnName("refresh_token");
        builder.Property(u => u.RefreshTokenExpiresAt).HasColumnName("refresh_token_expires_at");
    }
}
