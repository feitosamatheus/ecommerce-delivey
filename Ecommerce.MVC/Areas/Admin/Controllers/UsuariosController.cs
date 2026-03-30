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

    public async Task<IActionResult> Index(string busca, string role, int pagina = 1)
    {
        // 1. Configurações básicas
        const int itensPorPagina = 10;
        var query = _context.Clientes.AsQueryable();

        // 2. Filtros (Sua lógica original mantida)
        if (!string.IsNullOrEmpty(busca))
        {
            query = query.Where(x => x.Nome.Contains(busca) || 
                                    x.Email.Contains(busca) || 
                                    x.CPF.Contains(busca));
        }

        if (!string.IsNullOrEmpty(role))
        {
            query = query.Where(x => x.Role == role);
        }

        // 3. Contagem total ANTES da paginação (para saber o total de registros filtrados)
        int totalRegistros = await query.CountAsync();

        // 4. Execução da Paginação
        // Pulamos (pagina - 1 * itens) e pegamos a quantidade definida
        var itensPaginados = await query
            .OrderByDescending(x => x.DataCadastro)
            .Skip((pagina - 1) * itensPorPagina)
            .Take(itensPorPagina)
            .ToListAsync();

        // 5. Mapeamento para a ViewModel ajustada
        var model = new UsuariosIndexViewModel
        {
            Itens = itensPaginados,
            Busca = busca,
            Role = role, // Usando o nome ajustado na ViewModel
            PaginaAtual = pagina,
            TotalItens = totalRegistros,
            TotalPaginas = (int)Math.Ceiling(totalRegistros / (double)itensPorPagina)
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
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(model.Senha),
            PrimeiroAcessoRedefinir = model.PrimeiroAcessoRedefinir // Exemplo de Hash
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

    [HttpPost]
    [Authorize(Roles = "administrador")] // Segurança extra: apenas admins redefinem senhas
    public async Task<IActionResult> RedefinirSenha([FromBody] RedefinirSenhaRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.NovaSenha))
        {
            return Json(new { success = false, message = "Dados inválidos." });
        }

        var cliente = await _context.Clientes.FindAsync(request.Id);
        if (cliente == null)
        {
            return Json(new { success = false, message = "Usuário não encontrado." });
        }

        try
        {
            // 1. Gera o novo Hash
            cliente.SenhaHash = BCrypt.Net.BCrypt.HashPassword(request.NovaSenha);
            
            // 2. Define se o usuário deve trocar a senha no próximo login
            cliente.PrimeiroAcessoRedefinir = request.ForcarTroca;

            _context.Update(cliente);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Senha atualizada com sucesso!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Erro ao atualizar: " + ex.Message });
        }
    }

    public class UsuariosIndexViewModel
    {
        // A lista de itens que será exibida na página atual
        public IEnumerable<Cliente> Itens { get; set; } = new List<Cliente>();

        // Filtros para persistência na busca
        public string Busca { get; set; }
        
        // Renomeado para RoleSelecionada para clareza na View e facilitar o binding
        public string Role { get; set; }

        // Propriedades de Paginação
        public int PaginaAtual { get; set; }
        public int TotalPaginas { get; set; }
        public int TotalItens { get; set; }
        
        // Itens por página (opcional, caso queira tornar dinâmico)
        public int ItensPorPagina { get; set; } = 10;
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

        public bool PrimeiroAcessoRedefinir { get; set; } = true;

        [DataType(DataType.Password)]
        public string Senha { get; set; } = "temporaria@2026"; // Opcional na edição
    }

    public class RedefinirSenhaRequest
    {
        public Guid Id { get; set; }
        public string NovaSenha { get; set; }
        public bool ForcarTroca { get; set; }
    }
}