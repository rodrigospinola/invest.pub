using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Invest.Domain.Entities;

namespace Invest.Infrastructure.Data.Configurations;

public class BatchRankingConfiguration : IEntityTypeConfiguration<BatchRanking>
{
    public void Configure(EntityTypeBuilder<BatchRanking> builder)
    {
        builder.ToTable("batch_rankings");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.BatchRunId).HasColumnName("batch_run_id").IsRequired();
        builder.HasIndex(r => r.BatchRunId);

        builder.Property(r => r.SubEstrategia).HasColumnName("sub_estrategia").HasMaxLength(50).IsRequired();
        builder.Property(r => r.Ticker).HasColumnName("ticker").HasMaxLength(20).IsRequired();
        builder.Property(r => r.Nome).HasColumnName("nome").HasMaxLength(200).IsRequired();
        builder.Property(r => r.Posicao).HasColumnName("posicao").IsRequired();
        builder.Property(r => r.ScoreTotal).HasColumnName("score_total").HasPrecision(5, 2).IsRequired();
        builder.Property(r => r.ScoreQuantitativo).HasColumnName("score_quantitativo").HasPrecision(5, 2).IsRequired();
        builder.Property(r => r.ScoreQualitativo).HasColumnName("score_qualitativo").HasPrecision(5, 2).IsRequired();
        builder.Property(r => r.Justificativa).HasColumnName("justificativa");
        builder.Property(r => r.Indicadores).HasColumnName("indicadores").HasColumnType("jsonb");
        builder.Property(r => r.EntrouHoje).HasColumnName("entrou_hoje").IsRequired();
        builder.Property(r => r.SaiuHoje).HasColumnName("saiu_hoje").IsRequired();
        builder.Property(r => r.DataRanking).HasColumnName("data_ranking").IsRequired();
        builder.HasIndex(r => new { r.SubEstrategia, r.DataRanking });

        builder.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
    }
}
