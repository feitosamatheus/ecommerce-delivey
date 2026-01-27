using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce.MVC.Models
{
    public class EnderecoFormViewModel
    {
        public string Cep { get; set; }
        public string Logradouro { get; set; }
        public string Numero { get; set; }
        public string Complemento { get; set; }
        public string Bairro { get; set; }
        public string Cidade { get; set; }
        public string Estado { get; set; }

        public bool DefinirComoPrincipal { get; set; } = true;
        public bool EhEntrega { get; set; } = true;
        public bool EhCobranca { get; set; } = false;
    }
}