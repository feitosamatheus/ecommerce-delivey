using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ecommerce.MVC.Models.Admin;

public class ProdutoCategoriaAcompanhamentoItemViewModel
{
    public Guid? AcompanhamentoCategoriaId { get; set; }

        public string NomeCategoria { get; set; } = string.Empty;

        public bool Obrigatorio { get; set; }

        public int MinSelecionados { get; set; }

        public int MaxSelecionados { get; set; }
        public int Ordem { get; set; }

        public List<ProdutoAcompanhamentoSelecionadoItemViewModel> Acompanhamentos { get; set; } = new();
}