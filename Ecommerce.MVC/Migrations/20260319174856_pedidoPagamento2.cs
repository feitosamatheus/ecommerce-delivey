using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.MVC.Migrations
{
    /// <inheritdoc />
    public partial class pedidoPagamento2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PedidoPagamentos_PedidoId",
                table: "PedidoPagamentos");

            migrationBuilder.AddColumn<DateTime>(
                name: "PagoEmUtc",
                table: "PedidoPagamentos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Sequencia",
                table: "PedidoPagamentos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TipoCobranca",
                table: "PedidoPagamentos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PedidoPagamentos_PedidoId",
                table: "PedidoPagamentos",
                column: "PedidoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PedidoPagamentos_PedidoId",
                table: "PedidoPagamentos");

            migrationBuilder.DropColumn(
                name: "PagoEmUtc",
                table: "PedidoPagamentos");

            migrationBuilder.DropColumn(
                name: "Sequencia",
                table: "PedidoPagamentos");

            migrationBuilder.DropColumn(
                name: "TipoCobranca",
                table: "PedidoPagamentos");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoPagamentos_PedidoId",
                table: "PedidoPagamentos",
                column: "PedidoId",
                unique: true);
        }
    }
}
