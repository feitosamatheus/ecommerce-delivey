using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.MVC.Migrations
{
    /// <inheritdoc />
    public partial class addexcluido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Excluido",
                table: "PedidoPagamentos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExcluidoEmUtc",
                table: "PedidoPagamentos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExcluidoPor",
                table: "PedidoPagamentos",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Excluido",
                table: "PedidoPagamentos");

            migrationBuilder.DropColumn(
                name: "ExcluidoEmUtc",
                table: "PedidoPagamentos");

            migrationBuilder.DropColumn(
                name: "ExcluidoPor",
                table: "PedidoPagamentos");
        }
    }
}
