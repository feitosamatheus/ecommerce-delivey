using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.MVC.Entities;

[Table("ProdutoAcompanhamentoCategorias")]
[PrimaryKey(nameof(ProdutoId), nameof(AcompanhamentoCategoriaId))]
public class ProdutoAcompanhamentoCategoria
{
    [Required]
    public Guid ProdutoId { get; set; }

    public Produto Produto { get; set; } = null!;

    [Required]
    public Guid AcompanhamentoCategoriaId { get; set; }

    public AcompanhamentoCategoria Categoria { get; set; } = null!;

    public bool Obrigatorio { get; set; } = false;

    public int MinSelecionados { get; set; } = 0;

    public int MaxSelecionados { get; set; } = 1;

    public int Ordem { get; set; } = 0;

    public ICollection<Acompanhamento> Acompanhamentos { get; set; } = new List<Acompanhamento>();
}