using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContaCorrente.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaFormaPagamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FormaPagamento",
                table: "Movimentacoes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Movimentacoes_ContaId_FormaPagamento",
                table: "Movimentacoes",
                columns: new[] { "ContaId", "FormaPagamento" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Movimentacoes_ContaId_FormaPagamento",
                table: "Movimentacoes");

            migrationBuilder.DropColumn(
                name: "FormaPagamento",
                table: "Movimentacoes");
        }
    }
}
