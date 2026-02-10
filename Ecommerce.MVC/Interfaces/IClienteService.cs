using Ecommerce.MVC.Entities;
using Ecommerce.MVC.Helpers;
using Ecommerce.MVC.Models.Clientes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce.MVC.Interfaces;
public interface IClienteService
{
    Task<ServiceResult<Cliente>> RegistrarClienteAsync(RegistrarClienteViewModel model);
    Task<Cliente> LoginAsync(LoginClienteViewModel model);
}