using Ecommerce.MVC.Entities;

namespace Ecommerce.MVC.Areas.Admin.Services;

public interface IPedidoExportService
{
    byte[] GerarPdf(Pedido pedido);
    byte[] GerarExcel(Pedido pedido);
}
