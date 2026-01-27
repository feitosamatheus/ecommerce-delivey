namespace Ecommerce.MVC.Models.Carrinhos;

public class CarrinhoItemViewModel
{
    public Guid ItemId { get; set; }
    public string Nome { get; set; } = null!;
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Total { get; set; }

    public List<CarrinhoItemAcompanhamentoViewModel> Acompanhamentos { get; set; } = new();
}