using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce.MVC.Entities;

public class AcompanhamentoCategoria
{
     public Guid Id { get; set; }

    public string Nome { get; set; } = null!;

    public string Descricao { get; set; }

    public bool Obrigatorio { get; set; } = false;
    public int MinSelecionados { get; set; } = 0;
    public int MaxSelecionados { get; set; } = 1;

    public int Ordem { get; set; } = 0;

    public ICollection<Acompanhamento> Acompanhamentos { get; set; } = new List<Acompanhamento>();

    public ICollection<ProdutoAcompanhamentoCategoria> Produtos { get; set; } = new List<ProdutoAcompanhamentoCategoria>();
}