namespace DotIA.API.Models
{
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
        public int ChatId { get; set; } // ✅ NOVO: ID do chat criado
    }

    public class AvaliacaoRequest
    {
        public int UsuarioId { get; set; }
        public string Pergunta { get; set; } = string.Empty;
        public string Resposta { get; set; } = string.Empty;
        public bool FoiUtil { get; set; }
        public int ChatId { get; set; } // ✅ NOVO: ID do chat para buscar diretamente
    }

    public class TicketDTO
    {
        public int Id { get; set; }
        public string NomeSolicitante { get; set; } = string.Empty;
        public string DescricaoProblema { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime DataAbertura { get; set; }
        public string? Solucao { get; set; }

        // ✅ NOVOS CAMPOS
        public int ChatId { get; set; }
        public string PerguntaOriginal { get; set; } = string.Empty;
        public string RespostaIA { get; set; } = string.Empty;
    }

    public class ResolverTicketRequest
    {
        public int TicketId { get; set; }
        public string Solucao { get; set; } = string.Empty;
        public bool MarcarComoResolvido { get; set; } = false; // ✅ NOVO: Define se marca como resolvido ou mantém aberto
    }
}