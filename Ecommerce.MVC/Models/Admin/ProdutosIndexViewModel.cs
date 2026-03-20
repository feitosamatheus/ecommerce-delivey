using Ecommerce.MVC.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ecommerce.MVC.Models.Admin;

public class ProdutosIndexViewModel
{
    // 1. A Lista de dados que será exibida na tabela
    public IEnumerable<Produto> Produtos { get; set; } = new List<Produto>();

    // 2. Dados para o Dropdown de Categorias nos Filtros
    public IEnumerable<SelectListItem> Categorias { get; set; } = new List<SelectListItem>();

    // 3. Propriedades de Paginação (Igual ao seu PedidosIndexViewModel)
    public int PaginaAtual { get; set; }
    public int TotalPaginas { get; set; }
    public int TotalItens { get; set; }
    public int TamanhoPagina { get; set; }

    // 4. Propriedades de Ordenação
    public string SortColumn { get; set; } = "Nome";
    public string SortDirection { get; set; } = "asc";

    // 5. Mantém os valores dos filtros para persistência na UI
    public string? Busca { get; set; }
    public Guid? CategoriaId { get; set; } 

    // Helper para verificar se existem itens
    public bool TemProdutos => Produtos != null && Produtos.Any();
}
