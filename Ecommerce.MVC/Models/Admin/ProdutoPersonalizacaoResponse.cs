namespace Ecommerce.MVC.Models.Admin;

public class ProdutoPersonalizacaoResponse
{
    public Guid ProdutoId { get; set; }
    public string? NomeProduto { get; set; }
    public List<ProdutoAcompanhamentoCategoriaJsonViewModel> ProdutoAcompanhamentoCategorias { get; set; } = new();
}

public class ProdutoAcompanhamentoCategoriaJsonViewModel
{
    public Guid Id { get; set; }
    public Guid ProdutoId { get; set; }
    public Guid AcompanhamentoCategoriaId { get; set; }

    public string NomeCategoria { get; set; } = string.Empty;
    public string? DescricaoCategoria { get; set; }

    public bool Obrigatorio { get; set; }
    public int MinSelecionados { get; set; }
    public int MaxSelecionados { get; set; }
    public int Ordem { get; set; }

    public List<ProdutoAcompanhamentoJsonViewModel> ProdutoAcompanhamentos { get; set; } = new();
}

public class ProdutoAcompanhamentoJsonViewModel
{
    public Guid Id { get; set; }
    public Guid ProdutoId { get; set; }
    public Guid AcompanhamentoCategoriaId { get; set; }
    public Guid AcompanhamentoId { get; set; }

    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }

    public decimal Preco { get; set; }
    public bool Ativo { get; set; }
    public int Ordem { get; set; }

    public DateTime? DataAdicionado { get; set; }
}