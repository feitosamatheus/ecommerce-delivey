using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ecommerce.MVC.Enums;

namespace Ecommerce.MVC.Entities;

[Table("Categorias")]
public class Categoria
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    [Required]
    [Column(TypeName = "varchar(100)")]
    public string Nome { get; set; }

    [Required]
    public ETipoExibicao TipoExibicao { get; set; }

    public ICollection<Produto> Produtos { get; set; } = new List<Produto>();
}

