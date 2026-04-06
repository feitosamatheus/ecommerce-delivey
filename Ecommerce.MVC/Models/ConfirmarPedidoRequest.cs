using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.MVC.Enums;

namespace Ecommerce.MVC.Models;

public class ConfirmarPedidoRequest
{
    public DateTime HorarioRetirada { get; set; }
    public ETipoCobrancaPedido TipoCobranca { get; set; } = ETipoCobrancaPedido.Saldo;
    public string Observacao { get; set; }
    public string TipoPagamento { get; set; }

}