using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ecommerce.MVC.Models.Admin.Acompanhamentos;

public class AcompanhamentosIndexViewModel
{
    public string? Busca { get; set; }
    public Guid? CategoriaId { get; set; }

    public List<SelectListItem> Categorias { get; set; } = new();
    public List<AcompanhamentoListItemViewModel> Itens { get; set; } = new();
}