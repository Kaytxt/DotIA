namespace DotIA.API.Models
{
    // ═══════════════════════════════════════════════════════════
    // CHAT REQUEST/RESPONSE
    // ═══════════════════════════════════════════════════════════

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
}