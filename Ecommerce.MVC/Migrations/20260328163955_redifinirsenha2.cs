using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.MVC.Migrations
{
    /// <inheritdoc />
    public partial class redifinirsenha2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PrimeiroAcessoRedefinir",
                table: "Clientes",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrimeiroAcessoRedefinir",
                table: "Clientes");
        }
    }
}
