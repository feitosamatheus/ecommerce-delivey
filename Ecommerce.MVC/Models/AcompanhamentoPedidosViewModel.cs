using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce.MVC.Models
{
    public class AcompanhamentoPedidosViewModel
    {
        public List<AcompanhamentoPedidoItemVm> Pedidos { get; set; } = new();
    }
}