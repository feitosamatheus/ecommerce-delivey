using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.MVC.Config;
using Ecommerce.MVC.Entities;
using Ecommerce.MVC.Interfaces;
using Ecommerce.MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.MVC.Services
{
    public class EnderecoService : IEnderecoService
    {
        private readonly DatabaseContext _db;

        public EnderecoService(DatabaseContext db)
        {
            _db = db;
        }

        public async Task<Guid> CriarAsync(Guid clienteId, EnderecoFormViewModel vm, CancellationToken ct)
        {
            if (clienteId == Guid.Empty)
                throw new InvalidOperationException("Cliente inválido.");

            if (vm is null)
                throw new InvalidOperationException("Dados do endereço inválidos.");

            // Validações mínimas
            var cep = NormalizeCep(vm.Cep);
            if (string.IsNullOrWhiteSpace(cep) || cep.Length < 8)
                throw new InvalidOperationException("CEP inválido.");

            if (string.IsNullOrWhiteSpace(vm.Logradouro))
                throw new InvalidOperationException("Logradouro é obrigatório.");

            if (string.IsNullOrWhiteSpace(vm.Numero))
                throw new InvalidOperationException("Número é obrigatório.");

            if (string.IsNullOrWhiteSpace(vm.Bairro))
                throw new InvalidOperationException("Bairro é obrigatório.");

            if (string.IsNullOrWhiteSpace(vm.Cidade))
                throw new InvalidOperationException("Cidade é obrigatória.");

            var uf = (vm.Estado ?? string.Empty).Trim().ToUpperInvariant();
            if (uf.Length != 2)
                throw new InvalidOperationException("UF inválida.");

            // Garante que o cliente existe
            var clienteExiste = await _db.Set<Cliente>()
                .AsNoTracking()
                .AnyAsync(c => c.Id == clienteId, ct);

            if (!clienteExiste)
                throw new InvalidOperationException("Cliente não encontrado.");

            var endereco = new Endereco
            {
                ClienteId = clienteId,
                Cep = cep,
                Logradouro = vm.Logradouro.Trim(),
                Numero = vm.Numero.Trim(),
                Complemento = string.IsNullOrWhiteSpace(vm.Complemento)
                    ? null
                    : vm.Complemento.Trim(),
                Bairro = vm.Bairro.Trim(),
                Cidade = vm.Cidade.Trim(),
                Estado = uf,

                // regras simples
                EhPrincipal = false,

                CriadoEm = DateTime.UtcNow,
                AtualizadoEm = null
            };

            _db.Set<Endereco>().Add(endereco);
            await _db.SaveChangesAsync(ct);

            return endereco.Id;
        }

        public async Task RemoverAsync(Guid clienteId, Guid enderecoId, CancellationToken ct)
        {
            if (clienteId == Guid.Empty) throw new InvalidOperationException("Cliente inválido.");
            if (enderecoId == Guid.Empty) throw new InvalidOperationException("Endereço inválido.");

            var endereco = await _db.Set<Endereco>()
                .FirstOrDefaultAsync(e => e.Id == enderecoId && e.ClienteId == clienteId, ct);

            if (endereco == null)
                throw new InvalidOperationException("Endereço não encontrado.");

            _db.Set<Endereco>().Remove(endereco);
            await _db.SaveChangesAsync(ct);
        }


        private static string NormalizeCep(string cep)
        {
            if (string.IsNullOrWhiteSpace(cep))
                return null;

            return new string(cep.Where(char.IsDigit).ToArray());
        }
    }
}