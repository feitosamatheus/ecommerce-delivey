using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce.MVC.Entities
{
    public class Endereco
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // 🔗 Relacionamento com Cliente
        [Required]
        public Guid ClienteId { get; set; }

        // Navegação (1 Cliente → N Endereços)
        public Cliente Cliente { get; set; }

        // 📍 Dados do endereço
        [Required, MaxLength(9)]
        public string Cep { get; set; }

        [Required, MaxLength(120)]
        public string Logradouro { get; set; }

        [Required, MaxLength(10)]
        public string Numero { get; set; }

        [MaxLength(80)]
        public string Complemento { get; set; }

        [Required, MaxLength(80)]
        public string Bairro { get; set; }

        [Required, MaxLength(80)]
        public string Cidade { get; set; }

        [Required, MaxLength(2)]
        public string Estado { get; set; }

        // 🏷️ Tipo do endereço
        public bool EhPrincipal { get; set; } = false;
        public bool EhEntrega { get; set; } = true;
        public bool EhCobranca { get; set; } = false;

        // 📅 Auditoria
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
        public DateTime? AtualizadoEm { get; set; }

        // 🔎 Helper
        [NotMapped]
        public string EnderecoCompleto =>
            $"{Logradouro}, {Numero} {Complemento} - {Bairro}, {Cidade}/{Estado} - CEP {Cep}";
    }
}