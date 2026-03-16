using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.MVC.Migrations
{
    /// <inheritdoc />
    public partial class pedidoPagamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdClientePagamento",
                table: "Clientes",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PedidoPagamentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PedidoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Gateway = table.Column<string>(type: "text", nullable: true),
                    TipoPagamento = table.Column<string>(type: "text", nullable: true),
                    GatewayCustomerId = table.Column<string>(type: "text", nullable: true),
                    GatewayPaymentId = table.Column<string>(type: "text", nullable: true),
                    Valor = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PixPayload = table.Column<string>(type: "text", nullable: true),
                    PixEncodedImage = table.Column<string>(type: "text", nullable: true),
                    PixExpirationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InvoiceUrl = table.Column<string>(type: "text", nullable: true),
                    CriadoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoPagamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidoPagamentos_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PedidoPagamentos_PedidoId",
                table: "PedidoPagamentos",
                column: "PedidoId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PedidoPagamentos");

            migrationBuilder.DropColumn(
                name: "IdClientePagamento",
                table: "Clientes");
        }
    }
}
