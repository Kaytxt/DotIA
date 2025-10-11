using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DotIA.API.Data;
using DotIA.API.Models;

namespace DotIA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TicketsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("pendentes")]
        public async Task<ActionResult<List<TicketDTO>>> ObterTicketsPendentes()
        {
            try
            {
                var tickets = await _context.Tickets
                    .Where(t => t.IdStatus == 1) // Status "Pendente"
                    .Join(_context.Solicitantes,
                        ticket => ticket.IdSolicitante,
                        solicitante => solicitante.Id,
                        (ticket, solicitante) => new TicketDTO
                        {
                            Id = ticket.Id,
                            NomeSolicitante = solicitante.Nome,
                            DescricaoProblema = ticket.DescricaoProblema,
                            Status = "Pendente",
                            DataAbertura = ticket.DataAbertura,
                            Solucao = ticket.Solucao
                        })
                    .OrderByDescending(t => t.DataAbertura)
                    .ToListAsync();

                return Ok(tickets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = $"Erro ao buscar tickets: {ex.Message}" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TicketDTO>> ObterTicket(int id)
        {
            try
            {
                var ticket = await _context.Tickets
                    .Where(t => t.Id == id)
                    .Join(_context.Solicitantes,
                        ticket => ticket.IdSolicitante,
                        solicitante => solicitante.Id,
                        (ticket, solicitante) => new TicketDTO
                        {
                            Id = ticket.Id,
                            NomeSolicitante = solicitante.Nome,
                            DescricaoProblema = ticket.DescricaoProblema,
                            Status = ticket.IdStatus == 1 ? "Pendente" : "Resolvido",
                            DataAbertura = ticket.DataAbertura,
                            Solucao = ticket.Solucao
                        })
                    .FirstOrDefaultAsync();

                if (ticket == null)
                {
                    return NotFound(new { mensagem = "Ticket não encontrado" });
                }

                return Ok(ticket);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = $"Erro ao buscar ticket: {ex.Message}" });
            }
        }

        [HttpPost("resolver")]
        public async Task<ActionResult> ResolverTicket([FromBody] ResolverTicketRequest request)
        {
            try
            {
                var ticket = await _context.Tickets.FindAsync(request.TicketId);

                if (ticket == null)
                {
                    return NotFound(new { mensagem = "Ticket não encontrado" });
                }

                ticket.Solucao = request.Solucao;
                ticket.IdStatus = 2; // Status "Resolvido"
                ticket.DataEncerramento = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { sucesso = true, mensagem = "Ticket resolvido com sucesso" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = $"Erro ao resolver ticket: {ex.Message}" });
            }
        }

        [HttpGet("usuario/{usuarioId}")]
        public async Task<ActionResult<List<TicketDTO>>> ObterTicketsPorUsuario(int usuarioId)
        {
            try
            {
                var tickets = await _context.Tickets
                    .Where(t => t.IdSolicitante == usuarioId)
                    .Join(_context.Solicitantes,
                        ticket => ticket.IdSolicitante,
                        solicitante => solicitante.Id,
                        (ticket, solicitante) => new TicketDTO
                        {
                            Id = ticket.Id,
                            NomeSolicitante = solicitante.Nome,
                            DescricaoProblema = ticket.DescricaoProblema,
                            Status = ticket.IdStatus == 1 ? "Pendente" : "Resolvido",
                            DataAbertura = ticket.DataAbertura,
                            Solucao = ticket.Solucao
                        })
                    .OrderByDescending(t => t.DataAbertura)
                    .ToListAsync();

                return Ok(tickets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = $"Erro ao buscar tickets: {ex.Message}" });
            }
        }
    }

    // ???????????????????????????????????????????????????????????
    // MODELS (adicionar em Models/TicketModels.cs)
    // ???????????????????????????????????????????????????????????

    public class TicketDTO
    {
        public int Id { get; set; }
        public string NomeSolicitante { get; set; }
        public string DescricaoProblema { get; set; }
        public string Status { get; set; }
        public DateTime DataAbertura { get; set; }
        public string? Solucao { get; set; }
    }

    public class ResolverTicketRequest
    {
        public int TicketId { get; set; }
        public string Solucao { get; set; }
    }
}