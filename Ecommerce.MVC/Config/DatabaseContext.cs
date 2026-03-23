using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ecommerce.MVC.Entities;

namespace Ecommerce.MVC.Config;

public class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

    public DbSet<Cliente> Clientes { get; set; }
    public DbSet<Produto> Produtos { get; set; }
    public DbSet<Categoria> Categorias { get; set; }
    public DbSet<Acompanhamento> Acompanhamentos { get; set; }
    public DbSet<AcompanhamentoCategoria> AcompanhamentoCategorias { get; set; }
    public DbSet<Carrinho> Carrinhos { get; set; }
    public DbSet<CarrinhoItem> CarrinhoItems { get; set; }
    public DbSet<CarrinhoItemAcompanhamento> CarrinhoItemAcompanhamentos { get; set; }
    public DbSet<ProdutoAcompanhamentoCategoria> ProdutoAcompanhamentoCategorias { get; set; }
    public DbSet<ProdutoAcompanhamento> ProdutoAcompanhamentos { get; set; }

    public DbSet<Pedido> Pedidos { get; set; }
    public DbSet<PedidoPagamento> PedidoPagamentos { get; set; }
    public DbSet<PedidoItem> PedidoItens { get; set; }
    public DbSet<PedidoSequencial> PedidoSequenciais { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Pedido>()
            .HasMany(p => p.Pagamentos)
            .WithOne(pp => pp.Pedido)
            .HasForeignKey(pp => pp.PedidoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PedidoPagamento>()
            .HasIndex(pp => pp.PedidoId);

        modelBuilder.Entity<PedidoPagamento>()
            .Property(pp => pp.Status)
            .HasConversion<int>();

        modelBuilder.Entity<PedidoPagamento>()
            .Property(pp => pp.TipoCobranca)
            .HasConversion<int>();

        modelBuilder.Entity<Pedido>()
            .Property(p => p.Status)
            .HasConversion<int>();

        modelBuilder.Entity<Produto>().HasQueryFilter(p => p.Ativo);
    }
}