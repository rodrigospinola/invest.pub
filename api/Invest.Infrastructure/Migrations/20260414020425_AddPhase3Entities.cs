using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Invest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase3Entities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Mensagem = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "asset_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Ticker = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Data = table.Column<DateTime>(type: "date", nullable: false),
                    PrecoFechamento = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DividendoNoDia = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_history", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "benchmarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Data = table.Column<DateTime>(type: "date", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    VariacaoNoDia = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_benchmarks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "portfolio_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Data = table.Column<DateTime>(type: "date", nullable: false),
                    ValorTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RentabilidadeNoDia = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    RentabilidadeAcumulada = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    DistanciaMeta = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AlocacaoRealJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_portfolio_history", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alerts_Status",
                table: "alerts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_alerts_UserId",
                table: "alerts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_asset_history_Ticker_Data",
                table: "asset_history",
                columns: new[] { "Ticker", "Data" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_benchmarks_Nome_Data",
                table: "benchmarks",
                columns: new[] { "Nome", "Data" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_history_UserId_Data",
                table: "portfolio_history",
                columns: new[] { "UserId", "Data" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alerts");

            migrationBuilder.DropTable(
                name: "asset_history");

            migrationBuilder.DropTable(
                name: "benchmarks");

            migrationBuilder.DropTable(
                name: "portfolio_history");
        }
    }
}
