using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Invest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase2Entities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "batch_rankings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sub_estrategia = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ticker = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    posicao = table.Column<int>(type: "integer", nullable: false),
                    score_total = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    score_quantitativo = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    score_qualitativo = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    justificativa = table.Column<string>(type: "text", nullable: true),
                    indicadores = table.Column<string>(type: "jsonb", nullable: true),
                    entrou_hoje = table.Column<bool>(type: "boolean", nullable: false),
                    saiu_hoje = table.Column<bool>(type: "boolean", nullable: false),
                    data_ranking = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_batch_rankings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "batch_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_batch_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "portfolio_designs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    valor_total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_portfolio_designs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    portfolio_design_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ticker = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    classe = table.Column<string>(type: "text", nullable: false),
                    sub_estrategia = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    quantidade = table.Column<int>(type: "integer", nullable: false),
                    preco_medio = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    origem = table.Column<string>(type: "text", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_assets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_sub_strategies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sub_estrategia_acoes = table.Column<string>(type: "text", nullable: false),
                    sub_estrategia_fiis = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_sub_strategies", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_batch_rankings_batch_run_id",
                table: "batch_rankings",
                column: "batch_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_batch_rankings_sub_estrategia_data_ranking",
                table: "batch_rankings",
                columns: new[] { "sub_estrategia", "data_ranking" });

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_designs_user_id",
                table: "portfolio_designs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_assets_ticker",
                table: "user_assets",
                column: "ticker");

            migrationBuilder.CreateIndex(
                name: "IX_user_assets_user_id",
                table: "user_assets",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_sub_strategies_user_id",
                table: "user_sub_strategies",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "batch_rankings");

            migrationBuilder.DropTable(
                name: "batch_runs");

            migrationBuilder.DropTable(
                name: "portfolio_designs");

            migrationBuilder.DropTable(
                name: "user_assets");

            migrationBuilder.DropTable(
                name: "user_sub_strategies");
        }
    }
}
