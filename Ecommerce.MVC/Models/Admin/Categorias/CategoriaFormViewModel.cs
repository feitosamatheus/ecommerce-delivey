using System;
using System.ComponentModel.DataAnnotations;
using Ecommerce.MVC.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ecommerce.MVC.Models.Admin.Categorias;

public class CategoriaFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Informe o nome da categoria.")]
    [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecione o tipo de exibição.")]
    public ETipoExibicao TipoExibicao { get; set; }

    [Required(ErrorMessage = "Informe a ordem de exibição.")]
    [Range(0, int.MaxValue, ErrorMessage = "Informe uma ordem válida.")]
    public int Ordem { get; set; }

    public List<SelectListItem> TiposExibicao { get; set; } = new();
}