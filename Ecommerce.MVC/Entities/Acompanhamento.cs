using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce.MVC.Entities;
public class Acompanhamento
{
    public Guid Id { get; set; }

    public string Nome { get; set; } = null!;

    public string Descricao { get; set; }

    public decimal Preco { get; set; } = 0m;

    public bool Ativo { get; set; } = true;

    public int Ordem { get; set; } = 0;

    public Guid AcompanhamentoCategoriaId { get; set; }
    public AcompanhamentoCategoria Categoria { get; set; } = null!;
}