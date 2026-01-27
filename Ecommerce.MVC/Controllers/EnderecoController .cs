using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Ecommerce.MVC.Interfaces;
using Ecommerce.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Ecommerce.MVC.Controllers
{
    public class EnderecoController  : Controller
    {
        private readonly IEnderecoService _enderecoService;

        public EnderecoController(IEnderecoService enderecoService)
        {
            _enderecoService = enderecoService;
        }

        [HttpGet]
        public IActionResult NovoPartial()
        {
            // se quiser, pode pré-preencher algo via model
            return PartialView("_EnderecoForm", new EnderecoFormViewModel());
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Criar([FromBody] EnderecoFormViewModel vm, CancellationToken ct)
        {
            var clienteId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var enderecoId = await _enderecoService.CriarAsync(clienteId, vm, ct);

            return Ok(new { ok = true, enderecoId });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Remover([FromBody] RemoverEnderecoRequest req, CancellationToken ct)
        {
            var clienteId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            await _enderecoService.RemoverAsync(clienteId, req.EnderecoId, ct);

            return Ok(new { ok = true });
        }
    }
}