using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce.MVC.Entities
{
    [Table("CarrinhoItemAcompanhamentos")]
public class CarrinhoItemAcompanhamento
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CarrinhoItemId { get; set; }

    [ForeignKey(nameof(CarrinhoItemId))]
    public CarrinhoItem Item { get; set; } = null!;

    [Required]
    public Guid AcompanhamentoId { get; set; }

    [Required]
    public Guid CategoriaId { get; set; }

    // Snapshot
    [Required]
    [Column(TypeName = "varchar(150)")]
    public string NomeSnapshot { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PrecoSnapshot { get; set; }
}
}