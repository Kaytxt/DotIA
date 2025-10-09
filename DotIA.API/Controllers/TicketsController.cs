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
        public async Task<ActionResult<List<TicketDTO>>> ObterTicketsPendentes()
        {
            var tickets = await (from t in _context.Tickets
                                 where t.IdStatus == 1
                                 join s in _context.Solicitantes on t.IdSolicitante equals s.Id
                                 join ch in _context.ChatsHistorico on t.DescricaoProblema equals ch.Pergunta into chatGroup
                                 from chat in chatGroup.DefaultIfEmpty()
                                 select new TicketDTO
                                 {
                                     Id = t.Id,
                                     NomeSolicitante = s.Nome,
                                     DescricaoProblema = t.DescricaoProblema,
                                     RespostaIA = chat != null ? chat.Resposta : "",
                                     Status = "Pendente",
                                     DataAbertura = t.DataAbertura,
                                     Solucao = t.Solucao
                                 })
                                .ToListAsync();

            return Ok(tickets);
        }

        [HttpPost("resolver")]
        public async Task<ActionResult> ResolverTicket([FromBody] ResolverTicketRequest request)
        {
            var ticket = await _context.Tickets.FindAsync(request.TicketId);

            if (ticket == null)
                return NotFound(new { mensagem = "Ticket não encontrado" });

            ticket.Solucao = request.Solucao;
            ticket.IdStatus = 2;
            ticket.DataEncerramento = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { sucesso = true, mensagem = "Ticket resolvido com sucesso" });
        }
    }
}