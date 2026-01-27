
namespace Ecommerce.MVC.Models.Carrinhos;

public class CarrinhoViewModel
{
    public Guid CarrinhoId { get; set; }

    public List<CarrinhoItemViewModel> Itens { get; set; } = new();

    public decimal Subtotal => Itens.Sum(i => i.Total);
    public decimal Entrega { get; set; }
    public decimal Total => Subtotal + Entrega;

    public bool EstaVazio => !Itens.Any();
}