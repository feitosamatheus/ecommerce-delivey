namespace Ecommerce.MVC.Models.Admin.AcompanhamentoCategorias;

public class AcompanhamentoCategoriaListItemViewModel
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = null!;
    public string? Descricao { get; set; }
    public int QuantidadeAcompanhamentos { get; set; }
}