namespace Ecommerce.MVC.Enums;

public enum EPedidoStatus
{
    // Pedido criado, mas ainda não finalizado pelo cliente
    Rascunho = 0,

    // Pedido enviado, aguardando pagamento (PIX / Cartão)
    AguardandoPagamento = 1,

    // Pagamento aprovado (PIX confirmado / cartão validado)
    Confirmado = 2,

    // Pedido sendo preparado pela loja/cozinha
    EmPreparo = 3,

    // Pedido pronto para retirada ou saiu para entrega
    Pronto = 4,

    // Pedido entregue ao cliente ou retirado no balcão
    Concluido = 5,

    // Pedido cancelado antes da conclusão
    Cancelado = 6
}
