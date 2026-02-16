using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddPrimeiroAcessoToCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PrimeiroAcesso",
                table: "Clientes",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrimeiroAcesso",
                table: "Clientes");
        }
    }
}
