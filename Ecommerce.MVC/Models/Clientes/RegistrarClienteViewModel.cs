using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Ecommerce.MVC.Models.Clientes;

public class RegistrarClienteViewModel
{
    [Required(ErrorMessage = "O nome completo é obrigatório.")]
    [Display(Name = "Nome Completo")]
    public string NomeCompleto { get; set; }

    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "Insira um e-mail válido.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "O CPF é obrigatório.")]
    [RegularExpression(@"^\d{3}\.\d{3}\.\d{3}-\d{2}$", ErrorMessage = "Formato de CPF inválido.")]
    public string CPF { get; set; }

    [Required(ErrorMessage = "O WhatsApp é obrigatório.")]
    [Phone(ErrorMessage = "Número de telefone inválido.")]
    public string WhatsApp { get; set; }

    [Required(ErrorMessage = "A senha é obrigatória.")]
    [StringLength(8, MinimumLength = 6, ErrorMessage = "A senha deve ter entre 6 e 8 caracteres.")]
    [DataType(DataType.Password)]
    public string Senha { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirmar Senha")]
    [Compare("Senha", ErrorMessage = "As senhas não coincidem.")]
    public string ConfirmarSenha { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "Você deve aceitar os termos de uso.")]
    public bool AceitaTermos { get; set; }

    public bool AceitaMarketing { get; set; }
}