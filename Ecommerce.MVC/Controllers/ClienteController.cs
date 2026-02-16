using Ecommerce.MVC.Entities;
using Ecommerce.MVC.Helpers;
using Ecommerce.MVC.Interfaces;
using Ecommerce.MVC.Models.Clientes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Linq;
using System.Security.Claims;

public class ClienteController : Controller
{
    private readonly IClienteService _clienteService;
    private readonly ICarrinhoService _carrinhoService;

    public ClienteController(IClienteService clienteService, ICarrinhoService carrinhoService)
    {
        _clienteService = clienteService;
        _carrinhoService = carrinhoService;
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
        await SignInClienteAsync(cliente.Id, cliente.Nome, cliente.Email);

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

        await SignInClienteAsync(result.Cliente.Id, result.Cliente.Nome, result.Cliente.Email);

        var token = CartTokenHelper.GetOrCreateToken(HttpContext);

        await _carrinhoService.UnificarCarrinhoAsync(result.Cliente.Id, token);

        await SignInClienteAsync(result.Cliente.Id, result.Cliente.Nome, result.Cliente.Email);

        CartTokenHelper.ClearToken(HttpContext);

        return Json(new { 
            success = true, 
            message = "Login realizado com sucesso!" ,
            foiPrimeiroAcesso = result.FoiPrimeiroAcesso
        });
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }

    private async Task SignInClienteAsync(Guid id, string nome, string email)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Name, nome),
            new Claim(ClaimTypes.Email, email)
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
}