using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.MVC.Migrations
{
    /// <inheritdoc />
    public partial class MudandoTipo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Converte string para uuid usando cast explícito
            migrationBuilder.Sql(@"
        ALTER TABLE ""Carrinhos""
        ALTER COLUMN ""UserId"" TYPE uuid
        USING NULLIF(""UserId"", '')::uuid;
    ");

            // Garante que continue nullable
            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Carrinhos",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldNullable: true);
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
        ALTER TABLE ""Carrinhos""
        ALTER COLUMN ""UserId"" TYPE varchar(100)
        USING ""UserId""::text;
    ");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Carrinhos",
                type: "varchar(100)",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }

    }
}
