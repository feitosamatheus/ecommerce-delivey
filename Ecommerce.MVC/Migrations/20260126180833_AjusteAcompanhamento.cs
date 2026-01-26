using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AjusteAcompanhamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcompanhamentoCategoria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: true),
                    Descricao = table.Column<string>(type: "text", nullable: true),
                    Obrigatorio = table.Column<bool>(type: "boolean", nullable: false),
                    MinSelecionados = table.Column<int>(type: "integer", nullable: false),
                    MaxSelecionados = table.Column<int>(type: "integer", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcompanhamentoCategoria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Acompanhamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: true),
                    Descricao = table.Column<string>(type: "text", nullable: true),
                    Preco = table.Column<decimal>(type: "numeric", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    AcompanhamentoCategoriaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Acompanhamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Acompanhamento_AcompanhamentoCategoria_AcompanhamentoCatego~",
                        column: x => x.AcompanhamentoCategoriaId,
                        principalTable: "AcompanhamentoCategoria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProdutoAcompanhamentoCategorias",
                columns: table => new
                {
                    ProdutoId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcompanhamentoCategoriaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProdutoAcompanhamentoCategorias", x => new { x.ProdutoId, x.AcompanhamentoCategoriaId });
                    table.ForeignKey(
                        name: "FK_ProdutoAcompanhamentoCategorias_AcompanhamentoCategoria_Aco~",
                        column: x => x.AcompanhamentoCategoriaId,
                        principalTable: "AcompanhamentoCategoria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProdutoAcompanhamentoCategorias_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Acompanhamento_AcompanhamentoCategoriaId",
                table: "Acompanhamento",
                column: "AcompanhamentoCategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutoAcompanhamentoCategorias_AcompanhamentoCategoriaId",
                table: "ProdutoAcompanhamentoCategorias",
                column: "AcompanhamentoCategoriaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Acompanhamento");

            migrationBuilder.DropTable(
                name: "ProdutoAcompanhamentoCategorias");

            migrationBuilder.DropTable(
                name: "AcompanhamentoCategoria");
        }
    }
}
