using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce.MVC.Models.Produtos
{
    public class EnderecoResumoVm
    {
        public Guid Id { get; set; }
        public string Cep { get; set; }
        public string Logradouro { get; set; }
        public string Numero { get; set; }
        public string Complemento { get; set; }
        public string Bairro { get; set; }
        public string Cidade { get; set; }
        public string Estado { get; set; }

        public bool EhPrincipal { get; set; }

        public string Texto => $"{Logradouro}, {Numero} {(string.IsNullOrWhiteSpace(Complemento) ? "" : Complemento + " - ")}{Bairro} - {Cidade}/{Estado}";
    }
}