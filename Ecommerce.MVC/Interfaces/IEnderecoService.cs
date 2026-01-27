using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.MVC.Models;

namespace Ecommerce.MVC.Interfaces;

    public interface IEnderecoService
    {
        Task<Guid> CriarAsync(Guid clienteId, EnderecoFormViewModel vm, CancellationToken ct);
        Task RemoverAsync(Guid clienteId, Guid enderecoId, CancellationToken ct);
    }
