using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.MVC.Entities;
using Ecommerce.MVC.Models.Clientes;

namespace Ecommerce.MVC.Interfaces;
public interface IClienteService
{
    Task<bool> RegistrarClienteAsync(RegistrarClienteViewModel model);
    Task<Cliente> LoginAsync(LoginClienteViewModel model);
}