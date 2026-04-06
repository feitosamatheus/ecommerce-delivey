using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.MVC.Migrations
{
    /// <inheritdoc />
    public partial class adicionandoExpiracaoPixApp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PixExpirationDateApplication",
                table: "PedidoPagamentos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarrinhoItems_ProdutoId",
                table: "CarrinhoItems",
                column: "ProdutoId");

            migrationBuilder.AddForeignKey(
                name: "FK_CarrinhoItems_Produtos_ProdutoId",
                table: "CarrinhoItems",
                column: "ProdutoId",
                principalTable: "Produtos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CarrinhoItems_Produtos_ProdutoId",
                table: "CarrinhoItems");

            migrationBuilder.DropIndex(
                name: "IX_CarrinhoItems_ProdutoId",
                table: "CarrinhoItems");

            migrationBuilder.DropColumn(
                name: "PixExpirationDateApplication",
                table: "PedidoPagamentos");
        }
    }
}
