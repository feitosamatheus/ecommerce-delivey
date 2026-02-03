using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Ecommerce.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddSequencial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Endereco");

            migrationBuilder.DropTable(
                name: "PedidoEnderecos");

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "Pedidos",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PedidoSequenciais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ano = table.Column<int>(type: "integer", nullable: false),
                    UltimoNumero = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoSequenciais", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PedidoSequenciais");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "Pedidos");

            migrationBuilder.CreateTable(
                name: "Endereco",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Bairro = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Cep = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    Cidade = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Complemento = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EhCobranca = table.Column<bool>(type: "boolean", nullable: false),
                    EhEntrega = table.Column<bool>(type: "boolean", nullable: false),
                    EhPrincipal = table.Column<bool>(type: "boolean", nullable: false),
                    Estado = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Logradouro = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Numero = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Endereco", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Endereco_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PedidoEnderecos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PedidoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Bairro = table.Column<string>(type: "text", nullable: true),
                    Cep = table.Column<string>(type: "text", nullable: true),
                    Cidade = table.Column<string>(type: "text", nullable: true),
                    Complemento = table.Column<string>(type: "text", nullable: true),
                    Estado = table.Column<string>(type: "text", nullable: true),
                    Logradouro = table.Column<string>(type: "text", nullable: true),
                    Numero = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoEnderecos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidoEnderecos_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Endereco_ClienteId",
                table: "Endereco",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoEnderecos_PedidoId",
                table: "PedidoEnderecos",
                column: "PedidoId",
                unique: true);
        }
    }
}
