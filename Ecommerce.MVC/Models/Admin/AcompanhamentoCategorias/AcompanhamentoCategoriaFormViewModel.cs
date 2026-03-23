using System.ComponentModel.DataAnnotations;

namespace Ecommerce.MVC.Models.Admin.AcompanhamentoCategorias;

public class AcompanhamentoCategoriaFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Informe o nome da categoria.")]
    [StringLength(150)]
    public string Nome { get; set; } = null!;

    [StringLength(500)]
    public string? Descricao { get; set; }
}