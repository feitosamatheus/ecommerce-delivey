using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarCamposExclusaoEValidacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ExcluidoPorId",
                table: "PedidoPagamentos",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "ValidadoPor",
                table: "PedidoPagamentos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ValidadoPorId",
                table: "PedidoPagamentos",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExcluidoPorId",
                table: "PedidoPagamentos");

            migrationBuilder.DropColumn(
                name: "ValidadoPor",
                table: "PedidoPagamentos");

            migrationBuilder.DropColumn(
                name: "ValidadoPorId",
                table: "PedidoPagamentos");
        }
    }
}
