using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Invest.Domain.Entities;

namespace Invest.Infrastructure.Data.Configurations;

public class AssetHistoryConfiguration : IEntityTypeConfiguration<AssetHistory>
{
    public void Configure(EntityTypeBuilder<AssetHistory> builder)
    {
        builder.ToTable("asset_history");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Ticker).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Data).IsRequired().HasColumnType("date");
        builder.Property(x => x.PrecoFechamento).HasPrecision(18, 2);
        builder.Property(x => x.DividendoNoDia).HasPrecision(18, 4);
        
        builder.HasIndex(x => new { x.Ticker, x.Data }).IsUnique();
    }
}
