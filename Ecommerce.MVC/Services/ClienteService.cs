using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.MVC.Interfaces;
using Ecommerce.MVC.Models.Clientes;
using Microsoft.EntityFrameworkCore;
using Ecommerce.MVC.Config;
using Ecommerce.MVC.Entities;

namespace Ecommerce.MVC.Services;

public class ClienteService : IClienteService
{
    private readonly DatabaseContext _context;

    public ClienteService(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<bool> RegistrarClienteAsync(RegistrarClienteViewModel model)
    {
        try 
        {
            var novoCliente = new Cliente
            {
                Nome = model.NomeCompleto,
                Email = model.Email,
                CPF = model.CPF.Replace(".", "").Replace("-", ""), 
                Telefone = model.WhatsApp,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(model.Senha), 
                DataCadastro = DateTime.Now
            };

            _context.Clientes.Add(novoCliente);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<Cliente> LoginAsync(LoginClienteViewModel model)
    {
        var cliente = await _context.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Email == model.Email);

        if (cliente is null)
            return null;

        if (!cliente.Ativo)
            return null;
            
        var ok = BCrypt.Net.BCrypt.Verify(model.Senha, cliente.SenhaHash);
        if (!ok)
            return null;

        return cliente;
    }
}