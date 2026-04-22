using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Invest.Domain.Entities;

namespace Invest.Infrastructure.Data.Configurations;

public class BenchmarkConfiguration : IEntityTypeConfiguration<Benchmark>
{
    public void Configure(EntityTypeBuilder<Benchmark> builder)
    {
        builder.ToTable("benchmarks");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Nome).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Data).IsRequired().HasColumnType("date");
        builder.Property(x => x.Valor).HasPrecision(18, 4);
        builder.Property(x => x.VariacaoNoDia).HasPrecision(18, 4);
        
        builder.HasIndex(x => new { x.Nome, x.Data }).IsUnique();
    }
}
