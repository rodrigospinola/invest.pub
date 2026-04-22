using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Invest.Domain.Entities;

namespace Invest.Infrastructure.Data.Configurations;

public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("alerts");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Titulo).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Mensagem).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.Tipo).IsRequired().HasConversion<string>();
        builder.Property(x => x.Status).IsRequired().HasConversion<string>();
        builder.Property(x => x.MetadataJson).HasColumnType("jsonb");
        
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Status);
    }
}
