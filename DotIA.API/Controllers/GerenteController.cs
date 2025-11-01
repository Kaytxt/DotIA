using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DotIA.API.Data;
using DotIA.API.Models;
using TabelasDoBanco;

namespace DotIA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GerenteController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public GerenteController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ═══════════════════════════════════════════════════════════
        // DASHBOARD - ESTATÍSTICAS
        // ═══════════════════════════════════════════════════════════

        [HttpGet("dashboard")]
        public async Task<ActionResult> ObterDashboard()
        {
            try
            {
                var totalUsuarios = await _context.Solicitantes.CountAsync();
                var totalTickets = await _context.Tickets.CountAsync();
                var ticketsAbertos = await _context.Tickets.CountAsync(t => t.IdStatus == 1);
                var ticketsResolvidos = await _context.Tickets.CountAsync(t => t.IdStatus == 2);
                var totalChats = await _context.ChatsHistorico.CountAsync();
                var chatsResolvidos = await _context.ChatsHistorico.CountAsync(c => c.Status == 2 || c.Status == 4);

                // Tickets resolvidos hoje
                var hoje = DateTime.UtcNow.Date;
                var ticketsResolvidosHoje = await _context.Tickets
                    .CountAsync(t => t.IdStatus == 2 && t.DataEncerramento.HasValue && t.DataEncerramento.Value.Date == hoje);

                // Top 5 usuários com mais tickets
                var topUsuarios = await _context.Tickets
                    .GroupBy(t => t.IdSolicitante)
                    .Select(g => new
                    {
                        IdSolicitante = g.Key,
                        TotalTickets = g.Count()
                    })
                    .OrderByDescending(x => x.TotalTickets)
                    .Take(5)
                    .ToListAsync();

                var topUsuariosComNomes = new List<object>();
                foreach (var item in topUsuarios)
                {
                    var usuario = await _context.Solicitantes.FindAsync(item.IdSolicitante);
                    if (usuario != null)
                    {
                        topUsuariosComNomes.Add(new
                        {
                            nome = usuario.Nome,
                            totalTickets = item.TotalTickets
                        });
                    }
                }

                return Ok(new
                {
                    totalUsuarios,
                    totalTickets,
                    ticketsAbertos,
                    ticketsResolvidos,
                    totalChats,
                    chatsResolvidos,
                    ticketsResolvidosHoje,
                    topUsuarios = topUsuariosComNomes
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = $"Erro ao obter dashboard: {ex.Message}" });
            }
        }

        // ═══════════════════════════════════════════════════════════
        // GERENCIAMENTO DE USUÁRIOS
        // ═══════════════════════════════════════════════════════════

        [HttpGet("usuarios")]
        public async Task<ActionResult> ListarUsuarios()
        {
            try
            {
                var usuarios = await (
                    from solicitante in _context.Solicitantes
                    join departamento in _context.Departamentos on solicitante.IdDepartamento equals departamento.Id
                    select new
                    {
                        id = solicitante.Id,
                        nome = solicitante.Nome,
                        email = solicitante.Email,
                        departamento = departamento.Nome,
                        idDepartamento = departamento.Id,
                        totalTickets = _context.Tickets.Count(t => t.IdSolicitante == solicitante.Id),
                        ticketsAbertos = _context.Tickets.Count(t => t.IdSolicitante == solicitante.Id && t.IdStatus == 1),
                        totalChats = _context.ChatsHistorico.Count(c => c.IdSolicitante == solicitante.Id)
                    }
                ).OrderBy(u => u.nome).ToListAsync();

                return Ok(usuarios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = $"Erro ao listar usuários: {ex.Message}" });
            }
        }

        [HttpGet("usuarios/{usuarioId}")]
        public async Task<ActionResult> ObterUsuario(int usuarioId)
        {
            try
            {
                var usuario = await _context.Solicitantes.FindAsync(usuarioId);

                if (usuario == null)
                {
                    return NotFound(new { erro = "Usuário não encontrado" });
                }

                var departamento = await _context.Departamentos.FindAsync(usuario.IdDepartamento);

                return Ok(new
                {
                    id = usuario.Id,
                    nome = usuario.Nome,
                    email = usuario.Email,
                    idDepartamento = usuario.IdDepartamento,
                    departamento = departamento?.Nome
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = $"Erro ao obter usuário: {ex.Message}" });
            }
        }

        [HttpPut("usuarios/{usuarioId}")]
        public async Task<ActionResult> AtualizarUsuario(int usuarioId, [FromBody] AtualizarUsuarioRequest request)
        {
            try
            {
                var usuario = await _context.Solicitantes.FindAsync(usuarioId);

                if (usuario == null)
                {
                    return NotFound(new { erro = "Usuário não encontrado" });
                }

                // Verifica se o email já está em uso por outro usuário
                if (!string.IsNullOrEmpty(request.Email) && request.Email != usuario.Email)
                {
                    var emailExiste = await _context.Solicitantes
                        .AnyAsync(s => s.Email == request.Email && s.Id != usuarioId);

                    if (emailExiste)
                    {
                        return BadRequest(new { erro = "Este email já está em uso por outro usuário" });
                    }

                    usuario.Email = request.Email;
                }

                if (!string.IsNullOrEmpty(request.Nome))
                {
                    usuario.Nome = request.Nome;
                }

                if (request.IdDepartamento > 0)
                {
                    var departamentoExiste = await _context.Departamentos.AnyAsync(d => d.Id == request.IdDepartamento);
                    if (!departamentoExiste)
                    {
                        return BadRequest(new { erro = "Departamento inválido" });
                    }
                    usuario.IdDepartamento = request.IdDepartamento;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    sucesso = true,
                    mensagem = "Usuário atualizado com sucesso"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = $"Erro ao atualizar usuário: {ex.Message}" });
            }
        }

        [HttpDelete("usuarios/{usuarioId}")]
        public async Task<ActionResult> ExcluirUsuario(int usuarioId)
        {
            try
            {
                var usuario = await _context.Solicitantes.FindAsync(usuarioId);

                if (usuario == null)
                {
                    return NotFound(new { erro = "Usuário não encontrado" });
                }

                // Remove todos os chats do usuário
                var chats = await _context.ChatsHistorico
                    .Where(c => c.IdSolicitante == usuarioId)
                    .ToListAsync();
                _context.ChatsHistorico.RemoveRange(chats);

                // Remove todos os tickets do usuário
                var tickets = await _context.Tickets
                    .Where(t => t.IdSolicitante == usuarioId)
                    .ToListAsync();
                _context.Tickets.RemoveRange(tickets);

                // Remove histórico útil
                var historico = await _context.HistoricoUtil
                    .Where(h => h.IdSolicitante == usuarioId)
                    .ToListAsync();
                _context.HistoricoUtil.RemoveRange(historico);

                // Remove o usuário
                _context.Solicitantes.Remove(usuario);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    sucesso = true,
                    mensagem = "Usuário excluído com sucesso"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = $"Erro ao excluir usuário: {ex.Message}" });
            }
        }

        // ═══════════════════════════════════════════════════════════
        // TICKETS POR USUÁRIO
        // ═══════════════════════════════════════════════════════════

        [HttpGet("usuarios/{usuarioId}/tickets")]
        public async Task<ActionResult> ObterTicketsUsuario(int usuarioId)
        {
            try
            {
                var tickets = await (
                    from ticket in _context.Tickets
                    join chat in _context.ChatsHistorico on ticket.Id equals chat.IdTicket into chatGroup
                    from chat in chatGroup.DefaultIfEmpty()
                    where ticket.IdSolicitante == usuarioId
                    orderby ticket.DataAbertura descending
                    select new
                    {
                        id = ticket.Id,
                        descricaoProblema = ticket.DescricaoProblema,
                        status = ticket.IdStatus == 1 ? "Pendente" :
                                 ticket.IdStatus == 2 ? "Resolvido" : "Desconhecido",
                        idStatus = ticket.IdStatus,
                        dataAbertura = ticket.DataAbertura,
                        dataEncerramento = ticket.DataEncerramento,
                        solucao = ticket.Solucao,
                        chatId = chat != null ? chat.Id : 0,
                        perguntaOriginal = chat != null ? chat.Pergunta : "",
                        respostaIA = chat != null ? chat.Resposta : ""
                    }
                ).ToListAsync();

                return Ok(tickets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = $"Erro ao obter tickets do usuário: {ex.Message}" });
            }
        }

        // ═══════════════════════════════════════════════════════════
        // RELATÓRIOS
        // ═══════════════════════════════════════════════════════════

        [HttpGet("relatorio-departamentos")]
        public async Task<ActionResult> RelatorioPorDepartamento()
        {
            try
            {
                var relatorio = await (
                    from dept in _context.Departamentos
                    select new
                    {
                        departamento = dept.Nome,
                        totalUsuarios = _context.Solicitantes.Count(s => s.IdDepartamento == dept.Id),
                        totalTickets = _context.Tickets.Count(t => _context.Solicitantes
                            .Any(s => s.Id == t.IdSolicitante && s.IdDepartamento == dept.Id)),
                        ticketsAbertos = _context.Tickets.Count(t => t.IdStatus == 1 && _context.Solicitantes
                            .Any(s => s.Id == t.IdSolicitante && s.IdDepartamento == dept.Id)),
                        ticketsResolvidos = _context.Tickets.Count(t => t.IdStatus == 2 && _context.Solicitantes
                            .Any(s => s.Id == t.IdSolicitante && s.IdDepartamento == dept.Id))
                    }
                ).ToListAsync();

                return Ok(relatorio);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = $"Erro ao gerar relatório: {ex.Message}" });
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // MODELS
    // ═══════════════════════════════════════════════════════════

    public class AtualizarUsuarioRequest
    {
        public string? Nome { get; set; }
        public string? Email { get; set; }
        public int IdDepartamento { get; set; }
    }
}