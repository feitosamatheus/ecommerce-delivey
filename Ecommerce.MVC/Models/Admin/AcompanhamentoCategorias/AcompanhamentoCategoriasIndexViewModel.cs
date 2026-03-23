namespace Ecommerce.MVC.Models.Admin.AcompanhamentoCategorias;

public class AcompanhamentoCategoriasIndexViewModel
{
    public string? Busca { get; set; }
    public List<AcompanhamentoCategoriaListItemViewModel> Itens { get; set; } = new();
}