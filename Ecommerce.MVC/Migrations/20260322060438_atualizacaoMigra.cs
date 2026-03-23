using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.MVC.Migrations
{
    /// <inheritdoc />
    public partial class atualizacaoMigra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxSelecionados",
                table: "AcompanhamentoCategorias");

            migrationBuilder.DropColumn(
                name: "MinSelecionados",
                table: "AcompanhamentoCategorias");

            migrationBuilder.DropColumn(
                name: "Obrigatorio",
                table: "AcompanhamentoCategorias");

            migrationBuilder.DropColumn(
                name: "Ordem",
                table: "AcompanhamentoCategorias");

            migrationBuilder.AddColumn<int>(
                name: "MaxSelecionados",
                table: "ProdutoAcompanhamentoCategorias",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinSelecionados",
                table: "ProdutoAcompanhamentoCategorias",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Obrigatorio",
                table: "ProdutoAcompanhamentoCategorias",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Ordem",
                table: "ProdutoAcompanhamentoCategorias",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ProdutoAcompanhamentoCategoriaAcompanhamentoCategoriaId",
                table: "Acompanhamentos",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProdutoAcompanhamentoCategoriaProdutoId",
                table: "Acompanhamentos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Acompanhamentos_ProdutoAcompanhamentoCategoriaProdutoId_Pro~",
                table: "Acompanhamentos",
                columns: new[] { "ProdutoAcompanhamentoCategoriaProdutoId", "ProdutoAcompanhamentoCategoriaAcompanhamentoCategoriaId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Acompanhamentos_ProdutoAcompanhamentoCategorias_ProdutoAcom~",
                table: "Acompanhamentos",
                columns: new[] { "ProdutoAcompanhamentoCategoriaProdutoId", "ProdutoAcompanhamentoCategoriaAcompanhamentoCategoriaId" },
                principalTable: "ProdutoAcompanhamentoCategorias",
                principalColumns: new[] { "ProdutoId", "AcompanhamentoCategoriaId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Acompanhamentos_ProdutoAcompanhamentoCategorias_ProdutoAcom~",
                table: "Acompanhamentos");

            migrationBuilder.DropIndex(
                name: "IX_Acompanhamentos_ProdutoAcompanhamentoCategoriaProdutoId_Pro~",
                table: "Acompanhamentos");

            migrationBuilder.DropColumn(
                name: "MaxSelecionados",
                table: "ProdutoAcompanhamentoCategorias");

            migrationBuilder.DropColumn(
                name: "MinSelecionados",
                table: "ProdutoAcompanhamentoCategorias");

            migrationBuilder.DropColumn(
                name: "Obrigatorio",
                table: "ProdutoAcompanhamentoCategorias");

            migrationBuilder.DropColumn(
                name: "Ordem",
                table: "ProdutoAcompanhamentoCategorias");

            migrationBuilder.DropColumn(
                name: "ProdutoAcompanhamentoCategoriaAcompanhamentoCategoriaId",
                table: "Acompanhamentos");

            migrationBuilder.DropColumn(
                name: "ProdutoAcompanhamentoCategoriaProdutoId",
                table: "Acompanhamentos");

            migrationBuilder.AddColumn<int>(
                name: "MaxSelecionados",
                table: "AcompanhamentoCategorias",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinSelecionados",
                table: "AcompanhamentoCategorias",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Obrigatorio",
                table: "AcompanhamentoCategorias",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Ordem",
                table: "AcompanhamentoCategorias",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
