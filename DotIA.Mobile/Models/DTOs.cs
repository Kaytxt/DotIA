using System;

namespace DotIA.Mobile.Models
{
    // ════════════════════════════════════════════════════════
    // LOGIN
    // ════════════════════════════════════════════════════════
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public bool Sucesso { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        public string? TipoUsuario { get; set; }
        public int? UsuarioId { get; set; }
        public string? Nome { get; set; }
    }

    // ════════════════════════════════════════════════════════
    // REGISTRO
    // ════════════════════════════════════════════════════════
    public class RegistroRequest
    {
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public string ConfirmacaoSenha { get; set; } = string.Empty;
        public int IdDepartamento { get; set; }
    }

    public class RegistroResponse
    {
        public bool Sucesso { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        public int UsuarioId { get; set; }
    }

    public class DepartamentoDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
    }

    // ════════════════════════════════════════════════════════
    // CHAT
    // ════════════════════════════════════════════════════════
    public class ChatRequest
    {
        public int UsuarioId { get; set; }
        public string Pergunta { get; set; } = string.Empty;
        public int? ChatId { get; set; } // ✅ Para continuar conversa no mesmo chat
    }

    public class ChatResponse
    {
        public bool Sucesso { get; set; }
        public string Resposta { get; set; } = string.Empty;
        public DateTime DataHora { get; set; }
        public int ChatId { get; set; }
    }

    public class ChatHistoricoDTO
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Pergunta { get; set; } = string.Empty;
        public string Resposta { get; set; } = string.Empty;
        public DateTime DataHora { get; set; }
        public int Status { get; set; }
        public int? IdTicket { get; set; }
        public string StatusTexto { get; set; } = string.Empty;
        public string? Solucao { get; set; } // Soluções do técnico

        // Propriedade computada para exibir título ou pergunta
        public string TituloExibicao
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Titulo))
                    return Titulo;

                if (!string.IsNullOrWhiteSpace(Pergunta))
                {
                    // Retorna os primeiros 50 caracteres da pergunta
                    return Pergunta.Length > 50 ? Pergunta.Substring(0, 50) + "..." : Pergunta;
                }

                return "Chat sem título";
            }
        }
    }

    public class AvaliacaoRequest
    {
        public int UsuarioId { get; set; }
        public string Pergunta { get; set; } = string.Empty;
        public string Resposta { get; set; } = string.Empty;
        public bool FoiUtil { get; set; }
        public int ChatId { get; set; }
    }

    public class MensagemUsuarioRequest
    {
        public int ChatId { get; set; }
        public string Mensagem { get; set; } = string.Empty;
    }

    public class AbrirTicketDiretoRequest
    {
        public int UsuarioId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
    }

    public class EditarTituloRequest
    {
        public string NovoTitulo { get; set; } = string.Empty;
    }

    public class VerificarRespostaResponse
    {
        public bool TemResposta { get; set; }
        public string? Solucao { get; set; }
        public int Status { get; set; }
        public int StatusTicket { get; set; }
        public DateTime? DataResposta { get; set; }
    }

    // ════════════════════════════════════════════════════════
    // TICKETS (TÉCNICO)
    // ════════════════════════════════════════════════════════
    public class TicketDTO
    {
        public int Id { get; set; }
        public string NomeSolicitante { get; set; } = string.Empty;
        public string DescricaoProblema { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime DataAbertura { get; set; }
        public string? Solucao { get; set; }
        public int ChatId { get; set; }
        public string PerguntaOriginal { get; set; } = string.Empty;
        public string RespostaIA { get; set; } = string.Empty;
    }

    public class ResolverTicketRequest
    {
        public int TicketId { get; set; }
        public string Solucao { get; set; } = string.Empty;
        public bool MarcarComoResolvido { get; set; } = false;
    }

    // ════════════════════════════════════════════════════════
    // GERENTE - DASHBOARD
    // ════════════════════════════════════════════════════════
    public class DashboardResponse
    {
        public int TotalUsuarios { get; set; }
        public int TotalTickets { get; set; }
        public int TicketsAbertos { get; set; }
        public int TicketsResolvidos { get; set; }
        public int TotalChats { get; set; }
        public int ChatsResolvidos { get; set; }
        public int TicketsResolvidosHoje { get; set; }
        public List<TopUsuarioDTO> TopUsuarios { get; set; } = new();
    }

    public class TopUsuarioDTO
    {
        public string Nome { get; set; } = string.Empty;
        public int TotalTickets { get; set; }
    }

    public class TicketGerenteDTO
    {
        public int Id { get; set; }
        public int IdSolicitante { get; set; }
        public string NomeSolicitante { get; set; } = string.Empty;
        public string EmailSolicitante { get; set; } = string.Empty;
        public string Departamento { get; set; } = string.Empty;
        public string DescricaoProblema { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int IdStatus { get; set; }
        public DateTime DataAbertura { get; set; }
        public DateTime? DataEncerramento { get; set; }
        public string? Solucao { get; set; }
        public int ChatId { get; set; }
        public string PerguntaOriginal { get; set; } = string.Empty;
        public string RespostaIA { get; set; } = string.Empty;
    }

    public class UsuarioDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Departamento { get; set; } = string.Empty;
        public int IdDepartamento { get; set; }
        public int TotalTickets { get; set; }
        public int TicketsAbertos { get; set; }
        public int TotalChats { get; set; }
    }

    // Alias para UsuarioDTO usado no contexto de gerente
    public class UsuarioGerenteDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Departamento { get; set; } = string.Empty;
        public int IdDepartamento { get; set; }
        public int TotalTickets { get; set; }
        public int TicketsAbertos { get; set; }
        public int TotalChats { get; set; }
    }

    public class AtualizarUsuarioRequest
    {
        public string? Nome { get; set; }
        public string? Email { get; set; }
        public int IdDepartamento { get; set; }
    }

    public class AlterarSenhaRequest
    {
        public string NovaSenha { get; set; } = string.Empty;
    }

    public class ResponderTicketGerenteRequest
    {
        public int TicketId { get; set; }
        public string Resposta { get; set; } = string.Empty;
        public bool MarcarComoResolvido { get; set; } = false;
    }

    public class AlterarCargoRequest
    {
        public string Cargo { get; set; } = string.Empty; // Solicitante, Tecnico, Gerente
    }

    public class DashboardDTO
    {
        public int TotalUsuarios { get; set; }
        public int TotalTickets { get; set; }
        public int TicketsAbertos { get; set; }
        public int TicketsResolvidos { get; set; }
        public int TotalChats { get; set; }
        public int ChatsResolvidos { get; set; }
        public int TicketsResolvidosHoje { get; set; }
        public List<RankingUsuarioDTO> TopUsuarios { get; set; } = new();
    }

    public class RankingUsuarioDTO
    {
        public string Nome { get; set; } = string.Empty;
        public int TotalTickets { get; set; }
    }

    public class RelatorioDepartamentoDTO
    {
        public string Departamento { get; set; } = string.Empty;
        public int TotalUsuarios { get; set; }
        public int TotalTickets { get; set; }
        public int TicketsAbertos { get; set; }
        public int TicketsResolvidos { get; set; }
    }

    // ════════════════════════════════════════════════════════
    // MENSAGEM DE CHAT (Para UI estilo ChatGPT)
    // ════════════════════════════════════════════════════════
    public class ChatMensagem
    {
        public string Texto { get; set; } = string.Empty;
        public bool IsUsuario { get; set; }  // true = usuário, false = IA/Técnico
        public DateTime DataHora { get; set; }
        public string NomeRemetente { get; set; } = "DotIA 🤖"; // "DotIA 🤖" ou "Técnico 🔧"
    }
}
