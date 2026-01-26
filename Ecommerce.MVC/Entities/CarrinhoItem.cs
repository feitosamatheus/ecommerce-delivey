using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce.MVC.Entities
{
    public class CarrinhoItem
    {
        [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CarrinhoId { get; set; }

    [ForeignKey(nameof(CarrinhoId))]
    public Carrinho Carrinho { get; set; } = null!;

    [Required]
    public Guid ProdutoId { get; set; }

    // Snapshot (não depende do preço atual)
    [Required]
    [Column(TypeName = "varchar(150)")]
    public string ProdutoNomeSnapshot { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PrecoBaseSnapshot { get; set; }

    [Required]
    public int Quantidade { get; set; } = 1;

    [Column(TypeName = "varchar(800)")]
    public string Observacao { get; set; }

    public ICollection<CarrinhoItemAcompanhamento> Acompanhamentos { get; set; } = new List<CarrinhoItemAcompanhamento>();
    }
}