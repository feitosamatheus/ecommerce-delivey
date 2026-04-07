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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;

public class ClienteController : Controller
{
    private readonly IClienteService _clienteService;
    private readonly ICarrinhoService _carrinhoService;
    private readonly DatabaseContext _context;
    private readonly IConfiguration _configuration;

    public ClienteController(IClienteService clienteService, ICarrinhoService carrinhoService, DatabaseContext context, IConfiguration configuration)
    {
        _clienteService = clienteService;
        _carrinhoService = carrinhoService;
        _context = context;
        _configuration = configuration;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarPrimeiroAcessoComoConcluido()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var cliente = await _context.Clientes.FindAsync(Guid.Parse(userId));

        if (cliente == null)
            return NotFound();

        cliente.PrimeiroAcessoRedefinir = false;

        await _context.SaveChangesAsync();

        return Json(new { success = true });
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

    private async Task SignInClienteAsync(Guid id, string nome, string email, string roles)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Name, nome),
            new Claim(ClaimTypes.Email, email)
        };

        if (!string.IsNullOrWhiteSpace(roles))
        {
            var listaRoles = roles
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var role in listaRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }


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

            if (model == null || string.IsNullOrWhiteSpace(model.NovaSenha))
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

            // bool senhaValida = BCrypt.Net.BCrypt.Verify(model.SenhaAtual, cliente.SenhaHash);
            // if (!senhaValida)
            // {
            //     return Json(new { success = false, message = "A senha atual informada está incorreta." });
            // }

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EsqueciMinhaSenha([FromForm] EsqueciMinhaSenhaRequest request)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = "E-mail inválido." });

        var email = request.Email?.Trim().ToLower();

        if (string.IsNullOrWhiteSpace(email))
            return Json(new { success = false, message = "E-mail inválido." });

        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(x => x.Email != null && x.Email.ToLower() == email && x.Ativo);

        // Não expõe se o e-mail existe ou não
        if (cliente == null)
        {
            return Json(new
            {
                success = true,
                message = "Se o e-mail estiver cadastrado, uma nova senha foi enviada."
            });
        }

        var novaSenhaTemporaria = GerarSenhaTemporaria();

        try
        {
            await EnviarEmailNovaSenha(cliente.Email, cliente.Nome, novaSenhaTemporaria);

            cliente.SenhaHash = BCrypt.Net.BCrypt.HashPassword(novaSenhaTemporaria);

            // força redefinição no próximo login
            cliente.PrimeiroAcessoRedefinir = true;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Se o e-mail estiver cadastrado, uma nova senha foi enviada."
            });
        }
        catch
        {
            return Json(new
            {
                success = false,
                message = "Não foi possível enviar o e-mail no momento."
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> VerificarRedefinicaoObrigatoria()
    {
        var emailUsuario = User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrEmpty(emailUsuario))
            return Json(new { obrigatorio = false });

        var cliente = await _context.Clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == emailUsuario && x.Ativo);

        if (cliente == null)
            return Json(new { obrigatorio = false });

        return Json(new { obrigatorio = cliente.PrimeiroAcessoRedefinir });
    }

    private string GerarSenhaTemporaria()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
        var random = new Random();

        return new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[random.Next(s.Length)])
            .ToArray());
    }

    private async Task EnviarEmailNovaSenha(string emailDestino, string nomeCliente, string novaSenha)
    {
        var host = _configuration["Email:Smtp:Host"];
        var porta = int.Parse(_configuration["Email:Smtp:Port"]);
        var usuario = _configuration["Email:Smtp:User"];
        var senha = _configuration["Email:Smtp:Pass"];
        var remetente = _configuration["Email:Smtp:From"];

        using var client = new SmtpClient(host, porta)
        {
            Credentials = new NetworkCredential(usuario, senha),
            EnableSsl = true
        };

        var assunto = "Redefinição de senha - GastroFlow";

        var corpo = $@"
            <div style='margin:0; padding:0; background-color:#f5f5f5; font-family: Arial, Helvetica, sans-serif;'>
                <div style='max-width:600px; margin:0 auto; padding:32px 16px;'>
                    <div style='background-color:#ffffff; border-radius:12px; overflow:hidden; box-shadow:0 4px 18px rgba(0,0,0,0.08);'>
                        
                        <div style='background: linear-gradient(135deg, #8E4F1E, #D98C1A); padding:24px; text-align:center;'>
                            <h1 style='margin:0; color:#ffffff; font-size:22px;'>GastroFlow</h1>
                            <p style='margin:8px 0 0 0; color:#fff4e8; font-size:14px;'>
                                Recuperação de acesso
                            </p>
                        </div>

                        <div style='padding:32px 24px; color:#333333;'>
                            <p style='margin:0 0 16px 0; font-size:15px;'>
                                Olá, <strong>{nomeCliente}</strong>.
                            </p>

                            <p style='margin:0 0 16px 0; font-size:15px; line-height:1.6;'>
                                Recebemos uma solicitação para redefinir sua senha de acesso.
                            </p>

                            <p style='margin:0 0 12px 0; font-size:15px; line-height:1.6;'>
                                Utilize a senha temporária abaixo para entrar no sistema:
                            </p>

                            <div style='margin:24px 0; text-align:center;'>
                                <span style='display:inline-block; background-color:#fff7ed; border:1px dashed #D98C1A; color:#8E4F1E; font-size:28px; font-weight:bold; letter-spacing:6px; padding:14px 24px; border-radius:10px;'>
                                    {novaSenha}
                                </span>
                            </div>

                            <p style='margin:0 0 16px 0; font-size:15px; line-height:1.6;'>
                                Por segurança, no seu próximo acesso será solicitado o cadastro de uma nova senha definitiva.
                            </p>

                            <p style='margin:0; font-size:14px; color:#666666; line-height:1.6;'>
                                Caso você não tenha solicitado essa alteração, recomendamos entrar em contato com o suporte.
                            </p>
                        </div>

                        <div style='background-color:#fafafa; border-top:1px solid #eeeeee; padding:18px 24px; text-align:center;'>
                            <p style='margin:0; font-size:12px; color:#888888;'>
                                Esta é uma mensagem automática. Não responda este e-mail.
                            </p>
                        </div>
                    </div>
                </div>
            </div>
        ";

        using var mail = new MailMessage
        {
            From = new MailAddress(remetente, "GastroFlow"),
            Subject = assunto,
            Body = corpo,
            IsBodyHtml = true
        };

        mail.To.Add(emailDestino);

        await client.SendMailAsync(mail);
    }

    public class EsqueciMinhaSenhaRequest
    {
        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        public string Email { get; set; }
    }
}