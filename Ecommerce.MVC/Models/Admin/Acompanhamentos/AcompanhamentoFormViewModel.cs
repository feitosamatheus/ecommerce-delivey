using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ecommerce.MVC.Models.Admin.Acompanhamentos;

public class AcompanhamentoFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Informe o nome do acompanhamento.")]
    [StringLength(150)]
    public string Nome { get; set; } = null!;

    [StringLength(500)]
    public string? Descricao { get; set; }

    [Range(0, 9999.99, ErrorMessage = "Informe um preço válido.")]
    public decimal Preco { get; set; }

    public bool Ativo { get; set; } = true;

    [Range(0, 999)]
    public int Ordem { get; set; } = 0;

    [Required(ErrorMessage = "Selecione a categoria.")]
    public Guid? AcompanhamentoCategoriaId { get; set; }

    public List<SelectListItem> Categorias { get; set; } = new();
}