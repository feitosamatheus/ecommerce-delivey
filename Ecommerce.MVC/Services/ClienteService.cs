using Ecommerce.MVC.Config;
using Ecommerce.MVC.Entities;
using Ecommerce.MVC.Helpers;
using Ecommerce.MVC.Interfaces;
using Ecommerce.MVC.Models.Clientes;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce.MVC.Services;

public class ClienteService : IClienteService
{
    private readonly DatabaseContext _context;

    public ClienteService(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<ServiceResult<Cliente>> RegistrarClienteAsync(RegistrarClienteViewModel model)
    {
        var cpf = model.CPF?.Replace(".", "").Replace("-", "").Trim();
        var email = model.Email?.Trim().ToLowerInvariant();

        if (!CpfEhValido(cpf))
            return ServiceResult<Cliente>.Fail("CPF inválido.");

        var cpfExiste = await _context.Clientes.AnyAsync(c => c.CPF == cpf);
        if (cpfExiste)
            return ServiceResult<Cliente>.Fail("CPF já cadastrado.");

        var emailExiste = await _context.Clientes
            .AnyAsync(c => c.Email.ToLower() == email);

        if (emailExiste)
            return ServiceResult<Cliente>.Fail("E-mail já cadastrado.");

        var novoCliente = new Cliente
        {
            Nome = model.NomeCompleto,
            Email = email,
            CPF = cpf,
            Telefone = model.WhatsApp,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(model.Senha),
            DataCadastro = DateTime.Now
        };

        _context.Clientes.Add(novoCliente);
        await _context.SaveChangesAsync();

        return ServiceResult<Cliente>.Ok(novoCliente, "Cliente cadastrado com sucesso.");
    }

    public async Task<LoginResultadoViewModel> LoginAsync(LoginClienteViewModel model)
    {
        // Precisa ser "tracked" para atualizar sem gambiarra
        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.Email == model.Email);

        if (cliente is null) return null;
        if (!cliente.Ativo) return null;

        var ok = BCrypt.Net.BCrypt.Verify(model.Senha, cliente.SenhaHash);
        if (!ok) return null;

        // Snapshot do valor ANTES de alterar
        var foiPrimeiroAcesso = cliente.PrimeiroAcesso;

        if (foiPrimeiroAcesso)
        {
            cliente.PrimeiroAcesso = false;
            await _context.SaveChangesAsync();
        }

        return new LoginResultadoViewModel(cliente, foiPrimeiroAcesso);
    }

    private bool CpfEhValido(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return false;

        if (cpf.Length != 11 || cpf.Distinct().Count() == 1)
            return false;

        var numeros = cpf.Select(c => int.Parse(c.ToString())).ToArray();

        // Primeiro d�gito
        var soma1 = 0;
        for (int i = 0; i < 9; i++)
            soma1 += numeros[i] * (10 - i);

        var resto1 = soma1 % 11;
        var digito1 = resto1 < 2 ? 0 : 11 - resto1;

        if (numeros[9] != digito1)
            return false;

        // Segundo d�gito
        var soma2 = 0;
        for (int i = 0; i < 10; i++)
            soma2 += numeros[i] * (11 - i);

        var resto2 = soma2 % 11;
        var digito2 = resto2 < 2 ? 0 : 11 - resto2;

        return numeros[10] == digito2;
    }
}