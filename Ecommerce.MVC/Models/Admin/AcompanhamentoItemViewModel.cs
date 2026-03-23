namespace Ecommerce.MVC.Models.Admin;

public class AcompanhamentoItemViewModel
{
    public Guid Id { get; set; }
    public Guid AcompanhamentoId { get; set; } // Id do cadastro base
    public string Nome { get; set; }
    public string? Descricao { get; set; }
    public string? Preco { get; set; }
    public bool Ativo { get; set; }
    public int Ordem { get; set; }
    public bool Selecionado { get; set; }
}
