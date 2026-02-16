using Ecommerce.MVC.Entities;

namespace Ecommerce.MVC.Models.Clientes;

public sealed record LoginResultadoViewModel(Cliente Cliente, bool FoiPrimeiroAcesso);
