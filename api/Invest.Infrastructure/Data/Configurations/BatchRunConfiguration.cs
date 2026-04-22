using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Invest.Domain.Entities;

namespace Invest.Infrastructure.Data.Configurations;

public class BatchRunConfiguration : IEntityTypeConfiguration<BatchRun>
{
    public void Configure(EntityTypeBuilder<BatchRun> builder)
    {
        builder.ToTable("batch_runs");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(r => r.StartedAt).HasColumnName("started_at").IsRequired();
        builder.Property(r => r.CompletedAt).HasColumnName("completed_at");
        builder.Property(r => r.ErrorMessage).HasColumnName("error_message");
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
    }
}
