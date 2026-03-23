namespace Ecommerce.MVC.Models.Admin.Acompanhamentos;

public class AcompanhamentoListItemViewModel
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = null!;
    public string? Descricao { get; set; }
    public decimal Preco { get; set; }
    public bool Ativo { get; set; }
    public int Ordem { get; set; }
    public string CategoriaNome { get; set; } = null!;
}