using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ecommerce.MVC.Models.Admin;

public class AdminProdutoFormViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "O nome do produto é obrigatório")]
    [StringLength(150)]
    public string Nome { get; set; }

    [StringLength(500)]
    [Display(Name = "Descrição")]
    public string Descricao { get; set; }

    [Required(ErrorMessage = "Informe o preço")]
    [Display(Name = "Preço")]
    public string Preco { get; set; } = string.Empty;

    [Display(Name = "URL da Imagem")]
    [StringLength(255)]
    public string ImagemUrl { get; set; }

    [Display(Name = "Tempo de preparo (min)")]
    [Range(0, 600)]
    public int TempoPreparoMinutos { get; set; }

    [Range(0, 999, ErrorMessage = "Informe uma hora válida.")]
    public int TempoPreparoHoras { get; set; }

    [Range(0, 59, ErrorMessage = "Informe minutos válidos.")]
    public int TempoPreparoMinutosRestantes { get; set; }

    public int Ordem { get; set; }



    [Required(ErrorMessage = "Selecione uma categoria")]
    [Display(Name = "Categoria")]
    public Guid CategoriaId { get; set; }

    [Display(Name = "Disponível para venda")]
    public bool Ativo { get; set; } = true;

    // --- Listas de Suporte para a View ---

    // Lista de categorias do menu (Sopa, Bebida, etc)
    public IEnumerable<SelectListItem> Categorias { get; set; } = new List<SelectListItem>();

    // Lista de categorias de acompanhamento disponíveis para o "Novo Grupo"
    public IEnumerable<SelectListItem> CategoriasAcompanhamentoDisponiveis { get; set; } = new List<SelectListItem>();

    // Coleção que o Model Binder preencherá com os dados do formulário dinâmico
    public List<CategoriaAcompanhamentoSelecaoViewModel> CategoriasAcompanhamentoSelecionadas { get; set; } = new();

    public string? PersonalizacaoJson { get; set; }
}

// ViewModel Auxiliar para os Grupos de Acompanhamento
public class CategoriaAcompanhamentoSelecaoViewModel
{
    public Guid ProdutoId { get; set; }
    public Guid AcompanhamentoCategoriaId { get; set; }
    public string NomeCategoria { get; set; }
    public bool Obrigatorio { get; set; }
    public int MinSelecionados { get; set; }
    public int MaxSelecionados { get; set; }
    public int Ordem { get; set; }

    public List<AcompanhamentoItemViewModel> Acompanhamentos { get; set; } = new();

    // Se precisar editar os itens individuais dentro do grupo:
    // public List<AcompanhamentoOpcaoViewModel> Acompanhamentos { get; set; } = new();
}