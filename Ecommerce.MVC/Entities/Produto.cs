using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ecommerce.MVC.Entities;

[Table("Produtos")]
public class Produto
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    [Required]
    [Column(TypeName = "varchar(150)")]
    public string Nome { get; set; }

    [Column(TypeName = "varchar(500)")]
    public string Descricao { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Preco { get; set; }

    [Column(TypeName = "varchar(255)")]
    public string ImagemUrl { get; set; }

    // Chave estrangeira
    [Required]
    public Guid CategoriaId { get; set; }

    // Navegação
    [ForeignKey("CategoriaId")]
    public Categoria Categoria { get; set; }

    public ICollection<ProdutoAcompanhamentoCategoria> AcompanhamentoCategorias { get; set; } = new List<ProdutoAcompanhamentoCategoria>();
}
