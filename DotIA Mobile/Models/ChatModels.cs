namespace DotIA_Mobile.Models
{
    public class ChatRequest
    {
        public int UsuarioId { get; set; }
        public string Pergunta { get; set; } = string.Empty;
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

    public class EditarTituloRequest
    {
        public string NovoTitulo { get; set; } = string.Empty;
    }

    public class DetalhesChat
    {
        public ChatHistoricoDTO Chat { get; set; } = new();
        public TicketInfo? Ticket { get; set; }
    }

    public class TicketInfo
    {
        public int Id { get; set; }
        public string? Solucao { get; set; }
        public int IdStatus { get; set; }
        public DateTime? DataEncerramento { get; set; }
    }

    public class VerificarRespostaDTO
    {
        public bool TemResposta { get; set; }
        public string? Solucao { get; set; }
        public int Status { get; set; }
        public int? StatusTicket { get; set; }
        public DateTime? DataResposta { get; set; }
    }
}
