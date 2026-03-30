using Ecommerce.MVC.Config;
using Ecommerce.MVC.Entities;
using Ecommerce.MVC.Helpers;
using Ecommerce.MVC.Interfaces;
using Ecommerce.MVC.Models.Clientes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;

public class ClienteController : Controller
{
    private readonly IClienteService _clienteService;
    private readonly ICarrinhoService _carrinhoService;
    private readonly DatabaseContext _context;

    public ClienteController(IClienteService clienteService, ICarrinhoService carrinhoService, DatabaseContext context)
    {
        _clienteService = clienteService;
        _carrinhoService = carrinhoService;
        _context = context;
    }

    [HttpGet]
    public IActionResult BuscarModalAutenticacaoCliente()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("home", "index");

        return PartialView("Partials/_ModalAutenticacaoCliente");
    }

    [HttpGet]
    public IActionResult AcompanhamentoClienteModal()
    {
        return PartialView("Partials/_ClienteAcompanhamento");
    }

    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] RegistrarClienteViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var erros = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);

            return Json(new
            {
                success = false,
                message = "Erro na validação.",
                errors = erros
            });
        }

        var result = await _clienteService.RegistrarClienteAsync(model);

        if (!result.Success)
        {
            return Json(new
            {
                success = false,
                message = result.Message
            });
        }

        var cliente = result.Data;

        // 1) AutI: autentica o cliente recém-criado
        await SignInClienteAsync(cliente.Id, cliente.Nome, cliente.Email, cliente.Role);

        // 2) Carrinho: unifica o carrinho anônimo (se existir) com o carrinho do cliente
        var token = CartTokenHelper.GetOrCreateToken(HttpContext);
        await _carrinhoService.UnificarCarrinhoAsync(cliente.Id, token);

        // 3) Token: limpa o token anônimo após unificar
        CartTokenHelper.ClearToken(HttpContext);

        return Json(new
        {
            success = true,
            message = result.Message
        });
    }

    [HttpPost]
    [EnableRateLimiting("LoginPolicy")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([FromBody] LoginClienteViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var erros = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);

            return Json(new { success = false, message = "Erro na validação.", errors = erros });
        }

        var result = await _clienteService.LoginAsync(model);

        if (result is null)
            return Json(new
            {
                success = false,
                message = "Não foi possível autenticar. Verifique os dados e tente novamente."
            });

        await SignInClienteAsync(result.Cliente.Id, result.Cliente.Nome, result.Cliente.Email, result.Cliente.Role);

        var token = CartTokenHelper.GetOrCreateToken(HttpContext);

        await _carrinhoService.UnificarCarrinhoAsync(result.Cliente.Id, token);

        await SignInClienteAsync(result.Cliente.Id, result.Cliente.Nome, result.Cliente.Email, result.Cliente.Role);

        CartTokenHelper.ClearToken(HttpContext);

        return Json(new { 
            success = true, 
            message = "Login realizado com sucesso!"
        });
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }

    private async Task SignInClienteAsync(Guid id, string nome, string email, string role)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Name, nome),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
            });
    }

    [HttpPost]
    public async Task<IActionResult> AlterarSenha([FromBody] AlterarSenhaViewModel model)
    {
        try
        {

            if (model == null || string.IsNullOrWhiteSpace(model.SenhaAtual) || string.IsNullOrWhiteSpace(model.NovaSenha))
            {
                return Json(new { success = false, message = "Os campos de senha são obrigatórios." });
            }

            if (model.NovaSenha.Length < 6) 
            {
                return Json(new { success = false, message = "A nova senha deve ter pelo menos 6 caracteres." });
            }

            var clienteIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                 ?? User.FindFirst("Id")?.Value;

            if (string.IsNullOrEmpty(clienteIdClaim))
            {
                return Json(new { success = false, message = "Sessão expirada. Refaça o login." });
            }

            var clienteId = Guid.Parse(clienteIdClaim);

            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == clienteId);

            if (cliente == null)
            {
                return Json(new { success = false, message = "Usuário não encontrado no sistema." });
            }

            bool senhaValida = BCrypt.Net.BCrypt.Verify(model.SenhaAtual, cliente.SenhaHash);
            if (!senhaValida)
            {
                return Json(new { success = false, message = "A senha atual informada está incorreta." });
            }

            cliente.SenhaHash = BCrypt.Net.BCrypt.HashPassword(model.NovaSenha);
            cliente.PrimeiroAcessoRedefinir = false;

            _context.Clientes.Update(cliente);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Senha atualizada com sucesso!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Erro interno. Tente novamente mais tarde." });
        }
    }

    public class AlterarSenhaViewModel
    {
        public string SenhaAtual { get; set; }
        public string NovaSenha { get; set; }
    }

}