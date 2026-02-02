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

    public DbSet<Pedido> Pedidos { get; set; }
    public DbSet<PedidoItem> PedidoItens { get; set; }
}