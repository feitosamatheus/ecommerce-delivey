using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce.MVC.Entities;

    [Table("Carrinhos")]
    public class Carrinho
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Token para usuário anônimo (cookie)
        [Required]
        [Column(TypeName = "varchar(64)")]
        public string Token { get; set; } = null!;

        // Se tiver autenticação depois (opcional)
        [Column(TypeName = "varchar(100)")]
        public string? UserId { get; set; }

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public ICollection<CarrinhoItem> Itens { get; set; } = new List<CarrinhoItem>();

    }