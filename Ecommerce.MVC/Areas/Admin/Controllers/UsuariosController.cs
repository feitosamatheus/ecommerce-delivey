using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Spreadsheet;
using Ecommerce.MVC.Config;
using Ecommerce.MVC.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using System.ComponentModel.DataAnnotations;

[Area("Admin")]
[Authorize(Roles = "administrador,gerente")]
public class UsuariosController : Controller
{
    private readonly DatabaseContext _context; // Substitua pelo seu Contexto

    public UsuariosController(DatabaseContext context) => _context = context;

    public async Task<IActionResult> Index(string busca, string role)
    {
        var query = _context.Clientes.AsQueryable();

        if (!string.IsNullOrEmpty(busca))
            query = query.Where(x => x.Nome.Contains(busca) || x.Email.Contains(busca) || x.CPF.Contains(busca));

        if (!string.IsNullOrEmpty(role))
            query = query.Where(x => x.Role == role);

        var model = new UsuariosIndexViewModel
        {
            Itens = await query.OrderByDescending(x => x.DataCadastro).ToListAsync(),
            Busca = busca,
            Role = role
        };

        return View(model);
    }

    public IActionResult Create() => View(new UsuarioFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UsuarioFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var novo = new Cliente
        {
            Nome = model.Nome,
            Email = model.Email,
            CPF = model.CPF,
            Telefone = model.Telefone,
            Role = model.Role,
            Ativo = model.Ativo,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(model.Senha) // Exemplo de Hash
        };

        _context.Add(novo);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Usuário criado com sucesso!";
        return RedirectToAction(nameof(Index));
    }


    // GET: Admin/Usuarios/Edit/5
    public async Task<IActionResult> Edit(Guid id)
    {
        var cliente = await _context.Clientes.FindAsync(id);
        if (cliente == null) return NotFound();

        var model = new UsuarioFormViewModel
        {
            Id = cliente.Id,
            Nome = cliente.Nome,
            Email = cliente.Email,
            CPF = cliente.CPF,
            Telefone = cliente.Telefone,
            Role = cliente.Role,
            Ativo = cliente.Ativo
            // Senha fica vazia por segurança
        };

        return View(model);
    }

    // POST: Admin/Usuarios/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, UsuarioFormViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                var cliente = await _context.Clientes.FindAsync(id);
                if (cliente == null) return NotFound();

                // Atualiza campos básicos
                cliente.Nome = model.Nome;
                cliente.Email = model.Email;
                cliente.CPF = model.CPF;
                cliente.Telefone = model.Telefone;
                cliente.Role = model.Role;
                cliente.Ativo = model.Ativo;

                // Lógica de Senha: Só altera se o campo for preenchido
                if (!string.IsNullOrWhiteSpace(model.Senha))
                {
                    cliente.SenhaHash = BCrypt.Net.BCrypt.HashPassword(model.Senha);
                }

                _context.Update(cliente);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Usuário atualizado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Erro de concorrência ao salvar.";
            }
        }
        return View(model);
    }

    // POST: Admin/Usuarios/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var cliente = await _context.Clientes.FindAsync(id);
        if (cliente == null) return Json(new { success = false, message = "Usuário não encontrado." });

        // Se você tiver FKs (pedidos vinculados), pode preferir Inativar em vez de Deletar
        // cliente.Ativo = false; 

        _context.Clientes.Remove(cliente);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Usuário removido permanentemente!";
        return RedirectToAction(nameof(Index));
    }

    public class UsuariosIndexViewModel
    {
        public IEnumerable<Cliente> Itens { get; set; } = new List<Cliente>();
        public string Busca { get; set; }
        public string Role { get; set; }
    }

    public class UsuarioFormViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "O e-mail é obrigatório")]
        [EmailAddress(ErrorMessage = "E-mail inválido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "O CPF é obrigatório")]
        public string CPF { get; set; }

        public string Telefone { get; set; }

        [Required(ErrorMessage = "A Role é obrigatória")]
        public string Role { get; set; } = "Cliente";

        public bool Ativo { get; set; } = true;

        [DataType(DataType.Password)]
        public string Senha { get; set; } // Opcional na edição
    }
}