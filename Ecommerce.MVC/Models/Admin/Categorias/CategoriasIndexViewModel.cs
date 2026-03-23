using Ecommerce.MVC.Entities;

namespace Ecommerce.MVC.Models.Admin.Categorias;

public class CategoriasIndexViewModel
{
    public List<Categoria> Categorias { get; set; } = new();
    public string? Busca { get; set; }
}