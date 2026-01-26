using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ecommerce.MVC.Entities;

[Table("Categorias")]
public class Categoria
{
    [Key]
    public Guid Id { get; private set; } = Guid.NewGuid();

    [Required]
    [Column(TypeName = "varchar(100)")]
    public string Nome { get; set; }

    // Relacionamento: uma categoria pode ter vários produtos
    public ICollection<Produto> Produtos { get; set; } = new List<Produto>();
}

