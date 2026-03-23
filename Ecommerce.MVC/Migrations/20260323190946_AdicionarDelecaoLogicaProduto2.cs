using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarDelecaoLogicaProduto2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Acompanhamentos_ProdutoAcompanhamentoCategorias_ProdutoAcom~",
                table: "Acompanhamentos");

            migrationBuilder.DropIndex(
                name: "IX_Acompanhamentos_ProdutoAcompanhamentoCategoriaProdutoId_Pro~",
                table: "Acompanhamentos");

            migrationBuilder.DropColumn(
                name: "ProdutoAcompanhamentoCategoriaAcompanhamentoCategoriaId",
                table: "Acompanhamentos");

            migrationBuilder.DropColumn(
                name: "ProdutoAcompanhamentoCategoriaProdutoId",
                table: "Acompanhamentos");

            migrationBuilder.CreateTable(
                name: "ProdutoAcompanhamentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProdutoId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcompanhamentoCategoriaId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcompanhamentoId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataAdicionado = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProdutoAcompanhamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProdutoAcompanhamentos_Acompanhamentos_AcompanhamentoId",
                        column: x => x.AcompanhamentoId,
                        principalTable: "Acompanhamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProdutoAcompanhamentos_ProdutoAcompanhamentoCategorias_Prod~",
                        columns: x => new { x.ProdutoId, x.AcompanhamentoCategoriaId },
                        principalTable: "ProdutoAcompanhamentoCategorias",
                        principalColumns: new[] { "ProdutoId", "AcompanhamentoCategoriaId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProdutoAcompanhamentos_AcompanhamentoId",
                table: "ProdutoAcompanhamentos",
                column: "AcompanhamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutoAcompanhamentos_ProdutoId_AcompanhamentoCategoriaId",
                table: "ProdutoAcompanhamentos",
                columns: new[] { "ProdutoId", "AcompanhamentoCategoriaId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProdutoAcompanhamentos");

            migrationBuilder.AddColumn<Guid>(
                name: "ProdutoAcompanhamentoCategoriaAcompanhamentoCategoriaId",
                table: "Acompanhamentos",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProdutoAcompanhamentoCategoriaProdutoId",
                table: "Acompanhamentos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Acompanhamentos_ProdutoAcompanhamentoCategoriaProdutoId_Pro~",
                table: "Acompanhamentos",
                columns: new[] { "ProdutoAcompanhamentoCategoriaProdutoId", "ProdutoAcompanhamentoCategoriaAcompanhamentoCategoriaId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Acompanhamentos_ProdutoAcompanhamentoCategorias_ProdutoAcom~",
                table: "Acompanhamentos",
                columns: new[] { "ProdutoAcompanhamentoCategoriaProdutoId", "ProdutoAcompanhamentoCategoriaAcompanhamentoCategoriaId" },
                principalTable: "ProdutoAcompanhamentoCategorias",
                principalColumns: new[] { "ProdutoId", "AcompanhamentoCategoriaId" });
        }
    }
}
