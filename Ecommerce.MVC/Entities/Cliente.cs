using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BCrypt.Net;

namespace Ecommerce.MVC.Entities;

[Table("Clientes")]
public class Cliente
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    [Required]
    [Column(TypeName = "varchar(150)")]
    public string Nome { get; set; }

    [Required]
    [Column(TypeName = "varchar(100)")]
    public string Email { get; set; }

    [Required]
    [Column(TypeName = "varchar(14)")] 
    public string CPF { get; set; }

    [Column(TypeName = "varchar(20)")]
    public string Telefone { get; set; }

    [Required]
    public string SenhaHash { get; set; }

    //novo
    public string IdClientePagamento { get; set; }
    
    [Required]
    public string Role { get; set; } = "cliente";

    [Column(TypeName = "timestamp without time zone")]
    public DateTime DataCadastro { get; set; } = DateTime.Now;

    public bool Ativo { get; set; } = true;
    
    public bool PrimeiroAcesso { get; set; } = true;

    public bool PrimeiroAcessoRedefinir { get; set; } = true;

    public bool RecebeMarketing { get; set; }

}