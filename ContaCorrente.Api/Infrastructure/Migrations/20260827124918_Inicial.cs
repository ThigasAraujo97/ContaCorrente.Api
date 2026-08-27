using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContaCorrente.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Contas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Documento = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Saldo = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadaEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AtualizadaEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Versao = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Movimentacoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Valor = table.Column<long>(type: "INTEGER", nullable: false),
                    SaldoResultante = table.Column<long>(type: "INTEGER", nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    OcorridaEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movimentacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Movimentacoes_Contas_ContaId",
                        column: x => x.ContaId,
                        principalTable: "Contas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contas_Documento",
                table: "Contas",
                column: "Documento",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Movimentacoes_ContaId_OcorridaEm",
                table: "Movimentacoes",
                columns: new[] { "ContaId", "OcorridaEm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Movimentacoes");

            migrationBuilder.DropTable(
                name: "Contas");
        }
    }
}
