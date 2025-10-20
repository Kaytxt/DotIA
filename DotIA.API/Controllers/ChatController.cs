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
                    DataHora = DateTime.UtcNow,
                    Status = 1 // Em andamento
                };

                _context.ChatsHistorico.Add(historico);
                await _context.SaveChangesAsync();

                return Ok(new ChatResponse
                {
                    Sucesso = true,
                    Resposta = resposta,
                    DataHora = DateTime.UtcNow,
                    ChatId = historico.Id
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
            try
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
                        h.DataHora,
                        Status = h.Status, // ✅ Garantir que retorna o status
                        h.IdTicket,
                        StatusTexto = h.Status == 1 ? "Em andamento" :
                                      h.Status == 2 ? "Concluído" :
                                      h.Status == 3 ? "Pendente" :
                                      h.Status == 4 ? "Resolvido" : "Desconhecido"
                    })
                    .ToListAsync();

                return Ok(historico);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = ex.Message });
            }
        }

        [HttpPost("avaliar")]
        public async Task<ActionResult> AvaliarResposta([FromBody] AvaliacaoRequest request)
        {
            try
            {
                // ✅ Buscar o chat pelo ID se fornecido, senão busca pela pergunta/resposta
                ChatHistorico chat = null;

                if (request.ChatId > 0)
                {
                    chat = await _context.ChatsHistorico.FindAsync(request.ChatId);
                }
                else
                {
                    chat = await _context.ChatsHistorico
                        .Where(c => c.IdSolicitante == request.UsuarioId)
                        .OrderByDescending(c => c.DataHora)
                        .FirstOrDefaultAsync(c => c.Pergunta == request.Pergunta && c.Resposta == request.Resposta);
                }

                if (request.FoiUtil)
                {
                    // Salva como útil
                    _context.HistoricoUtil.Add(new HistoricoUtil
                    {
                        IdSolicitante = request.UsuarioId,
                        Pergunta = request.Pergunta,
                        Resposta = request.Resposta,
                        DataHora = DateTime.UtcNow
                    });

                    // ✅ ATUALIZA STATUS DO CHAT PARA CONCLUÍDO
                    if (chat != null)
                    {
                        chat.Status = 2; // Concluído
                    }
                }
                else
                {
                    // Cria ticket para técnico resolver
                    var ticket = new Ticket
                    {
                        IdSolicitante = request.UsuarioId,
                        IdTecnico = 1,
                        IdSubcategoria = 1,
                        IdNivel = 1,
                        DescricaoProblema = request.Pergunta,
                        IdStatus = 1, // Pendente
                        DataAbertura = DateTime.UtcNow
                    };

                    _context.Tickets.Add(ticket);
                    await _context.SaveChangesAsync(); // Salva para obter o ID

                    // ✅ ATUALIZA STATUS DO CHAT PARA PENDENTE E VINCULA TICKET
                    if (chat != null)
                    {
                        chat.Status = 3; // Pendente com Técnico
                        chat.IdTicket = ticket.Id;
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    sucesso = true,
                    ticketId = chat?.IdTicket,
                    chatId = chat?.Id,
                    novoStatus = chat?.Status
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { sucesso = false, erro = ex.Message });
            }
        }

        // ✅ ENDPOINT: Verificar se há resposta do técnico
        [HttpGet("verificar-resposta/{chatId}")]
        public async Task<ActionResult> VerificarRespostaTecnico(int chatId)
        {
            try
            {
                var chat = await _context.ChatsHistorico.FindAsync(chatId);

                if (chat == null)
                {
                    return NotFound(new { erro = "Chat não encontrado" });
                }

                // Se tem ticket vinculado, buscar a solução
                if (chat.IdTicket.HasValue)
                {
                    var ticket = await _context.Tickets.FindAsync(chat.IdTicket.Value);

                    if (ticket != null && !string.IsNullOrEmpty(ticket.Solucao))
                    {
                        return Ok(new
                        {
                            temResposta = true,
                            solucao = ticket.Solucao,
                            status = chat.Status,
                            statusTicket = ticket.IdStatus,
                            dataResposta = ticket.DataEncerramento
                        });
                    }
                }

                return Ok(new { temResposta = false, status = chat.Status });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = ex.Message });
            }
        }

        // ✅ ENDPOINT: Obter detalhes completos de um chat
        [HttpGet("detalhes/{chatId}")]
        public async Task<ActionResult> ObterDetalhesChat(int chatId)
        {
            try
            {
                var chat = await _context.ChatsHistorico
                    .Where(c => c.Id == chatId)
                    .Select(c => new
                    {
                        c.Id,
                        c.Titulo,
                        c.Pergunta,
                        c.Resposta,
                        c.DataHora,
                        Status = c.Status,
                        c.IdTicket,
                        StatusTexto = c.Status == 1 ? "Em andamento" :
                                      c.Status == 2 ? "Concluído" :
                                      c.Status == 3 ? "Pendente" :
                                      c.Status == 4 ? "Resolvido" : "Desconhecido"
                    })
                    .FirstOrDefaultAsync();

                if (chat == null)
                {
                    return NotFound(new { erro = "Chat não encontrado" });
                }

                // Se tem ticket, buscar informações do ticket
                object ticketInfo = null;
                if (chat.IdTicket.HasValue)
                {
                    var ticket = await _context.Tickets.FindAsync(chat.IdTicket.Value);
                    if (ticket != null)
                    {
                        ticketInfo = new
                        {
                            ticket.Id,
                            ticket.Solucao,
                            ticket.IdStatus,
                            ticket.DataEncerramento
                        };
                    }
                }

                return Ok(new { chat, ticket = ticketInfo });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = ex.Message });
            }
        }
    }
}