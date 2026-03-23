using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ecommerce.MVC.Models.Admin;

public class ProdutoAcompanhamentoSelecionadoItemViewModel
{
    public Guid AcompanhamentoId { get; set; }

    public string Nome { get; set; } = string.Empty;

    public bool Selecionado { get; set; }
}