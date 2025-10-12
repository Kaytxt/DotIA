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
    }

    public class AvaliacaoRequest
    {
        public int UsuarioId { get; set; }
        public string Pergunta { get; set; } = string.Empty;
        public string Resposta { get; set; } = string.Empty;
        public bool FoiUtil { get; set; }
    }

    public class TicketDTO
    {
        public int Id { get; set; }
        public string NomeSolicitante { get; set; } = string.Empty;
        public string DescricaoProblema { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime DataAbertura { get; set; }
        public string? Solucao { get; set; }
    }

    public class ResolverTicketRequest
    {
        public int TicketId { get; set; }
        public string Solucao { get; set; } = string.Empty;
    }
}