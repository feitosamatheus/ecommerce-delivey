using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce.MVC.Models.Pedidos;

public class PedidoModalFinalizarViewModel
{
    public Guid ClienteId { get; set; }
    public string ClienteNome { get; set; }
    public string ClienteTelefone { get; set; }

    public PedidoCarrinhoResumoViewModel Carrinho { get; set; } = new();

    public List<PedidoRetiradaHorarioViewModel> HorariosRetirada { get; set; } = new();
}