using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DotIA.API.Data;
using DotIA.API.Models;
using DotIA.API.Services;
using TabelasDoBanco;

namespace DotIA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IOpenAIService _openAIService;

        public ChatController(ApplicationDbContext context, IOpenAIService openAIService)
        {
            _context = context;
            _openAIService = openAIService;
        }

        [HttpPost("enviar")]
        public async Task<ActionResult<ChatResponse>> EnviarPergunta([FromBody] ChatRequest request)
        {
            try
            {
                var resposta = await _openAIService.ObterRespostaAsync(request.Pergunta);

                var historico = new ChatHistorico
                {
                    IdSolicitante = request.UsuarioId,
                    Titulo = request.Pergunta.Length > 30
                        ? request.Pergunta.Substring(0, 30) + "..."
                        : request.Pergunta,
                    Pergunta = request.Pergunta,
                    Resposta = resposta,
                    DataHora = DateTime.UtcNow // ✅ CORRIGIDO: Era DateTime.Now
                };

                _context.ChatsHistorico.Add(historico);
                await _context.SaveChangesAsync();

                return Ok(new ChatResponse
                {
                    Sucesso = true,
                    Resposta = resposta,
                    DataHora = DateTime.UtcNow // ✅ CORRIGIDO: Era DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ChatResponse
                {
                    Sucesso = false,
                    Resposta = $"Erro: {ex.Message}"
                });
            }
        }

        [HttpGet("historico/{usuarioId}")]
        public async Task<ActionResult> ObterHistorico(int usuarioId)
        {
            var historico = await _context.ChatsHistorico
                .Where(h => h.IdSolicitante == usuarioId)
                .OrderByDescending(h => h.DataHora)
                .Select(h => new
                {
                    h.Id,
                    h.Titulo,
                    h.Pergunta,
                    h.Resposta,
                    h.DataHora
                })
                .ToListAsync();

            return Ok(historico);
        }

        [HttpPost("avaliar")]
        public async Task<ActionResult> AvaliarResposta([FromBody] AvaliacaoRequest request)
        {
            try
            {
                if (request.FoiUtil)
                {
                    // Salva como útil
                    _context.HistoricoUtil.Add(new HistoricoUtil
                    {
                        IdSolicitante = request.UsuarioId,
                        Pergunta = request.Pergunta,
                        Resposta = request.Resposta,
                        DataHora = DateTime.UtcNow // ✅ CORRIGIDO: Era DateTime.Now
                    });
                }
                else
                {
                    // Cria ticket para técnico resolver
                    _context.Tickets.Add(new Ticket
                    {
                        IdSolicitante = request.UsuarioId,
                        IdTecnico = 1,
                        IdSubcategoria = 1,
                        IdNivel = 1,
                        DescricaoProblema = request.Pergunta,
                        IdStatus = 1,
                        DataAbertura = DateTime.UtcNow // ✅ CORRIGIDO: Era DateTime.Now
                    });
                }

                await _context.SaveChangesAsync();
                return Ok(new { sucesso = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { sucesso = false, erro = ex.Message });
            }
        }
    }
}