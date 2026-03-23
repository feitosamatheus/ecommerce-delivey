using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ecommerce.MVC.Entities;

[Table("ProdutoAcompanhamentos")]
public class ProdutoAcompanhamento
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid ProdutoId { get; set; }

    [Required]
    public Guid AcompanhamentoCategoriaId { get; set; }

    [Required]
    public Guid AcompanhamentoId { get; set; }

    public DateTime DataAdicionado { get; set; } = DateTime.UtcNow;

    public int Ordem { get; set; } = 0;

    public bool Ativo { get; set; } = true;

    [ForeignKey(nameof(ProdutoId) + "," + nameof(AcompanhamentoCategoriaId))]
    public ProdutoAcompanhamentoCategoria ProdutoAcompanhamentoCategoria { get; set; } = null!;

    [ForeignKey(nameof(AcompanhamentoId))]
    public Acompanhamento Acompanhamento { get; set; } = null!;
}
