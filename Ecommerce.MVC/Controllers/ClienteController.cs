using Microsoft.AspNetCore.Mvc;
using Ecommerce.MVC.Models.Clientes;
using System.Linq;
using Ecommerce.MVC.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

public class ClienteController : Controller
{
    private readonly IClienteService _clienteService;

    public ClienteController(IClienteService clienteService)
    {
        _clienteService = clienteService;
    }

    [HttpGet]
    public IActionResult ContaClienteModal()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return PartialView("Partials/_ClienteConta");
        }

        return PartialView("Partials/_ClienteLogin");
    }

    [HttpGet]
    public IActionResult AcompanhamentoClienteModal()
    {
        return PartialView("Partials/_ClienteAcompanhamento");
    }

    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] RegistrarClienteViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _clienteService.RegistrarClienteAsync(model);

            return Json(new { success = true, message = "Cadastro realizado com sucesso!" });
        }

        var erros = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
        return Json(new { success = false, message = "Erro na validação.", errors = erros });
    }

    [HttpPost]
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
            return Json(new { success = false, message = "Credenciais  inválidas"});

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, result.Id.ToString()),
            new Claim(ClaimTypes.Name, result.Nome),
            new Claim(ClaimTypes.Email, result.Email)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
            });

        return Json(new { success = true, message = "Login realizado com sucesso!" });
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }
}