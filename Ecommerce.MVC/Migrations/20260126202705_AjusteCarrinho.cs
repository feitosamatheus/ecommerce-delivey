using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AjusteCarrinho : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Acompanhamento_AcompanhamentoCategoria_AcompanhamentoCatego~",
                table: "Acompanhamento");

            migrationBuilder.DropForeignKey(
                name: "FK_ProdutoAcompanhamentoCategorias_AcompanhamentoCategoria_Aco~",
                table: "ProdutoAcompanhamentoCategorias");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AcompanhamentoCategoria",
                table: "AcompanhamentoCategoria");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Acompanhamento",
                table: "Acompanhamento");

            migrationBuilder.RenameTable(
                name: "AcompanhamentoCategoria",
                newName: "AcompanhamentoCategorias");

            migrationBuilder.RenameTable(
                name: "Acompanhamento",
                newName: "Acompanhamentos");

            migrationBuilder.RenameIndex(
                name: "IX_Acompanhamento_AcompanhamentoCategoriaId",
                table: "Acompanhamentos",
                newName: "IX_Acompanhamentos_AcompanhamentoCategoriaId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AcompanhamentoCategorias",
                table: "AcompanhamentoCategorias",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Acompanhamentos",
                table: "Acompanhamentos",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Carrinhos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "varchar(64)", nullable: false),
                    UserId = table.Column<string>(type: "varchar(100)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carrinhos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CarrinhoItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CarrinhoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProdutoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProdutoNomeSnapshot = table.Column<string>(type: "varchar(150)", nullable: false),
                    PrecoBaseSnapshot = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Quantidade = table.Column<int>(type: "integer", nullable: false),
                    Observacao = table.Column<string>(type: "varchar(800)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarrinhoItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarrinhoItems_Carrinhos_CarrinhoId",
                        column: x => x.CarrinhoId,
                        principalTable: "Carrinhos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CarrinhoItemAcompanhamentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CarrinhoItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcompanhamentoId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoriaId = table.Column<Guid>(type: "uuid", nullable: false),
                    NomeSnapshot = table.Column<string>(type: "varchar(150)", nullable: false),
                    PrecoSnapshot = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarrinhoItemAcompanhamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarrinhoItemAcompanhamentos_CarrinhoItems_CarrinhoItemId",
                        column: x => x.CarrinhoItemId,
                        principalTable: "CarrinhoItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarrinhoItemAcompanhamentos_CarrinhoItemId",
                table: "CarrinhoItemAcompanhamentos",
                column: "CarrinhoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CarrinhoItems_CarrinhoId",
                table: "CarrinhoItems",
                column: "CarrinhoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Acompanhamentos_AcompanhamentoCategorias_AcompanhamentoCate~",
                table: "Acompanhamentos",
                column: "AcompanhamentoCategoriaId",
                principalTable: "AcompanhamentoCategorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProdutoAcompanhamentoCategorias_AcompanhamentoCategorias_Ac~",
                table: "ProdutoAcompanhamentoCategorias",
                column: "AcompanhamentoCategoriaId",
                principalTable: "AcompanhamentoCategorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Acompanhamentos_AcompanhamentoCategorias_AcompanhamentoCate~",
                table: "Acompanhamentos");

            migrationBuilder.DropForeignKey(
                name: "FK_ProdutoAcompanhamentoCategorias_AcompanhamentoCategorias_Ac~",
                table: "ProdutoAcompanhamentoCategorias");

            migrationBuilder.DropTable(
                name: "CarrinhoItemAcompanhamentos");

            migrationBuilder.DropTable(
                name: "CarrinhoItems");

            migrationBuilder.DropTable(
                name: "Carrinhos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Acompanhamentos",
                table: "Acompanhamentos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AcompanhamentoCategorias",
                table: "AcompanhamentoCategorias");

            migrationBuilder.RenameTable(
                name: "Acompanhamentos",
                newName: "Acompanhamento");

            migrationBuilder.RenameTable(
                name: "AcompanhamentoCategorias",
                newName: "AcompanhamentoCategoria");

            migrationBuilder.RenameIndex(
                name: "IX_Acompanhamentos_AcompanhamentoCategoriaId",
                table: "Acompanhamento",
                newName: "IX_Acompanhamento_AcompanhamentoCategoriaId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Acompanhamento",
                table: "Acompanhamento",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AcompanhamentoCategoria",
                table: "AcompanhamentoCategoria",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Acompanhamento_AcompanhamentoCategoria_AcompanhamentoCatego~",
                table: "Acompanhamento",
                column: "AcompanhamentoCategoriaId",
                principalTable: "AcompanhamentoCategoria",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProdutoAcompanhamentoCategorias_AcompanhamentoCategoria_Aco~",
                table: "ProdutoAcompanhamentoCategorias",
                column: "AcompanhamentoCategoriaId",
                principalTable: "AcompanhamentoCategoria",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
