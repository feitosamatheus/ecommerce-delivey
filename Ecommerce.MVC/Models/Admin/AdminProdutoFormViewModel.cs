using System;
using System.ComponentModel.DataAnnotations;

namespace Ecommerce.MVC.Models.Admin;

public class AdminProdutoFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    [StringLength(150)]
    public string Nome { get; set; }

    [StringLength(500)]
    public string Descricao { get; set; }

    [Required]
    [Range(0.01, 999999)]
    public decimal Preco { get; set; }

    [Display(Name = "Imagem (URL)")]
    [StringLength(255)]
    public string ImagemUrl { get; set; }

    [Display(Name = "Tempo de preparo (min)")]
    [Range(0, 600)]
    public int TempoPreparoMinutos { get; set; }

    [Required]
    [Display(Name = "Categoria")]
    public Guid CategoriaId { get; set; }
}

