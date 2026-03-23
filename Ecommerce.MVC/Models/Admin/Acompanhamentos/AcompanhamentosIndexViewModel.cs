using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ecommerce.MVC.Models.Admin.Acompanhamentos;

public class AcompanhamentosIndexViewModel
{
    // Lista de itens que serão exibidos na tabela (após o Skip/Take)
    public IEnumerable<AcompanhamentoListItemViewModel> Itens { get; set; } = new List<AcompanhamentoListItemViewModel>();

    // Lista para carregar o <select> de categorias no filtro
    public IEnumerable<SelectListItem> Categorias { get; set; } = new List<SelectListItem>();

    // Propriedades de Filtro (Mantêm o estado nos inputs após o POST/GET)
    public string? Busca { get; set; }
    public Guid? CategoriaId { get; set; }

    // Propriedades de Paginação
    public int PaginaAtual { get; set; } = 1;
    public int TotalPaginas { get; set; }
    public int TotalItens { get; set; }
    public int TamanhoPagina { get; set; } = 5;

    // Propriedades de Ordenação (Opcional, caso queira usar as setinhas na tabela)
    public string SortColumn { get; set; } = "Nome";
    public string SortDirection { get; set; } = "asc";
}