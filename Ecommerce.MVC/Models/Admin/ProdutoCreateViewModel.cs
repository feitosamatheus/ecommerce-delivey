using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ecommerce.MVC.Models.Admin
{
    public class ProdutoCreateViewModel
    {
        [Required(ErrorMessage = "Informe o nome do produto.")]
        public string Nome { get; set; } = string.Empty;

        public string? Descricao { get; set; }

        [Required(ErrorMessage = "Informe o preço.")]
        public string Preco { get; set; } = string.Empty;

        public int TempoPreparoMinutos { get; set; }

        [Required(ErrorMessage = "Selecione a categoria do produto.")]
        public Guid CategoriaId { get; set; }

        public bool Ativo { get; set; } = true;

        public int Ordem { get; set; }

        [Range(0, 999, ErrorMessage = "Informe uma hora válida.")]
        public int TempoPreparoHoras { get; set; }

        [Range(0, 59, ErrorMessage = "Informe minutos válidos.")]
        public int TempoPreparoMinutosRestantes { get; set; }


        public List<SelectListItem> Categorias { get; set; } = new();

        public List<SelectListItem> CategoriasAcompanhamentoDisponiveis { get; set; } = new();

        public List<ProdutoCategoriaAcompanhamentoItemViewModel> CategoriasAcompanhamentoSelecionadas { get; set; } = new();
    }
}