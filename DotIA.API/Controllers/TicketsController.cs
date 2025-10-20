using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DotIA.API.Data;
using DotIA.API.Models;
using TabelasDoBanco;

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
        public async Task<ActionResult> ObterTicketsPendentes()
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
                return StatusCode(500, new { erro = ex.Message });
            }
        }

        [HttpPost("resolver")]
        public async Task<ActionResult> ResolverTicket([FromBody] ResolverTicketRequest request)
        {
            try
            {
                var ticket = await _context.Tickets
                    .FirstOrDefaultAsync(t => t.Id == request.TicketId);

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
                return StatusCode(500, new { erro = ex.Message });
            }
        }
    }
}