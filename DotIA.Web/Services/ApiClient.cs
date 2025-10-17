using System.Text;
using System.Text.Json;

namespace DotIA.Web.Services
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public ApiClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"]
                ?? throw new ArgumentException("ApiSettings:BaseUrl não configurado");
            _httpClient.BaseAddress = new Uri(_baseUrl);
        }

        // ═══════════════════════════════════════════════════════════
        // LOGIN
        // ═══════════════════════════════════════════════════════════
        public async Task<LoginResponse> LoginAsync(string email, string senha)
        {
            try
            {
                var request = new { Email = email, Senha = senha };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/Auth/login", content);
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<LoginResponse>(result, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new LoginResponse { Sucesso = false, Mensagem = "Erro ao processar resposta" };
                }

                return new LoginResponse { Sucesso = false, Mensagem = "Erro ao fazer login" };
            }
            catch (Exception ex)
            {
                return new LoginResponse { Sucesso = false, Mensagem = $"Erro: {ex.Message}" };
            }
        }

        // ═══════════════════════════════════════════════════════════
        // CHAT
        // ═══════════════════════════════════════════════════════════
        public async Task<ChatResponse> EnviarPerguntaAsync(int usuarioId, string pergunta)
        {
            try
            {
                var request = new { UsuarioId = usuarioId, Pergunta = pergunta };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/Chat/enviar", content);
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<ChatResponse>(result, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new ChatResponse { Sucesso = false, Resposta = "Erro ao processar resposta" };
                }

                return new ChatResponse { Sucesso = false, Resposta = "Erro ao enviar pergunta" };
            }
            catch (Exception ex)
            {
                return new ChatResponse { Sucesso = false, Resposta = $"Erro: {ex.Message}" };
            }
        }

        public async Task<List<ChatHistorico>> ObterHistoricoAsync(int usuarioId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/Chat/historico/{usuarioId}");
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<List<ChatHistorico>>(result, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<ChatHistorico>();
                }

                return new List<ChatHistorico>();
            }
            catch
            {
                return new List<ChatHistorico>();
            }
        }

        public async Task<bool> AvaliarRespostaAsync(int usuarioId, string pergunta, string resposta, bool foiUtil)
        {
            try
            {
                var request = new { UsuarioId = usuarioId, Pergunta = pergunta, Resposta = resposta, FoiUtil = foiUtil };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/Chat/avaliar", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // TICKETS
        // ═══════════════════════════════════════════════════════════
        public async Task<List<TicketDTO>> ObterTicketsPendentesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/Tickets/pendentes");
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<List<TicketDTO>>(result, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<TicketDTO>();
                }

                return new List<TicketDTO>();
            }
            catch
            {
                return new List<TicketDTO>();
            }
        }

        public async Task<bool> ResolverTicketAsync(int ticketId, string solucao)
        {
            try
            {
                var request = new { TicketId = ticketId, Solucao = solucao };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/Tickets/resolver", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // DTOs COM NULLABLE CORRIGIDO
    // ═══════════════════════════════════════════════════════════

    public class LoginResponse
    {
        public bool Sucesso { get; set; }
        public string TipoUsuario { get; set; } = string.Empty;
        public int UsuarioId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Mensagem { get; set; } = string.Empty;
    }

    public class ChatResponse
    {
        public bool Sucesso { get; set; }
        public string Resposta { get; set; } = string.Empty;
        public DateTime DataHora { get; set; }
    }

    public class ChatHistorico
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Pergunta { get; set; } = string.Empty;
        public string Resposta { get; set; } = string.Empty;
        public DateTime DataHora { get; set; }
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
}