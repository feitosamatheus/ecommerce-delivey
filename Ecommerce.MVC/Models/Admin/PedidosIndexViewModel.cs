using Ecommerce.MVC.Entities;

namespace Ecommerce.MVC.Models.Admin.Pedidos;

public class PedidosIndexViewModel
{
    public IEnumerable<Pedido> Itens { get; set; } = new List<Pedido>();

    public int PaginaAtual { get; set; }
    public int TamanhoPagina { get; set; }
    public int TotalRegistros { get; set; }
    public int TotalPaginas => (int)Math.Ceiling((double)TotalRegistros / TamanhoPagina);

    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public string? Status { get; set; }
    public string? TipoData { get; set; }
    public string? NumeroPedido { get; set; }
    public string? Cliente { get; set; }
    
    public string SortColumn { get; set; } = "CriadoEmUtc";
    public string SortDirection { get; set; } = "desc";

    public decimal ResumoTotalPedidos { get; set; }
    public decimal ResumoTotalPago { get; set; }
    public decimal ResumoTotalEmAberto { get; set; }
    public int ResumoQtdAguardandoPagamento { get; set; }
    public int ResumoQtdQuitados { get; set; }

    public int ResumoQtdConfirmados { get; set; }
    public int ResumoQtdEmPreparo { get; set; }
    public int ResumoQtdProntos { get; set; }
    public int ResumoQtdCancelados { get; set; }
}