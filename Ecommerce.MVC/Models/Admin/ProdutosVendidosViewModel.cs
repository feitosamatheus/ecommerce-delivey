namespace Ecommerce.MVC.Models.Admin;

public class ProdutosVendidosViewModel
{
    public Guid ProdutoId { get; set; }
    public string NomeProduto { get; set; }
    public int QuantidadeTotal { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal TotalVendido { get; set; }
    public int NumeroPedidos { get; set; }
    public DateTime UltimaVenda { get; set; }
}

public class DashboardProdutosVendidosViewModel
{
    public List<ProdutosVendidosViewModel> Produtos { get; set; } = new();
    public int TotalProdutosVendidos { get; set; }
    public decimal TotalReceita { get; set; }
    public int TotalPedidos { get; set; }
}

public class DetalhesProdutoViewModel
{
    public string NomeProduto { get; set; }
    public int QuantidadeTotal { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal TotalVendido { get; set; }
    public int NumeroPedidos { get; set; }
    public DateTime UltimaVenda { get; set; }
    public List<PedidoProdutoViewModel> Pedidos { get; set; } = new();
}

public class PedidoProdutoViewModel
{
    public Guid PedidoId { get; set; }
    public string CodigoPedido { get; set; }
    public string NomeCliente { get; set; }
    public DateTime DataPedido { get; set; }
    public int QuantidadeProduto { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal TotalProduto { get; set; }
    public string StatusPedido { get; set; }
    public string MetodoEntrega { get; set; }
}
