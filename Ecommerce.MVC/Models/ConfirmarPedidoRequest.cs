using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce.MVC.Models;

public class ConfirmarPedidoRequest
{
    public DateTime HorarioRetirada { get; set; }
    public string Observacao { get; set; }
}