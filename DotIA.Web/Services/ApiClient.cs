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
            _baseUrl = configuration["ApiSettings:BaseUrl"];
            _httpClient.BaseAddress = new Uri(_baseUrl);
        }

        // Login
        public async Task<LoginResponse> LoginAsync(string email, string senha)
        {
            var request = new { Email = email, Senha = senha };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/Auth/login", content);
            var result = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<LoginResponse>(result, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            return new LoginResponse { Sucesso = false };
        }

        // Chat
        public async Task<ChatResponse> EnviarPerguntaAsync(int usuarioId, string pergunta)
        {
            var request = new { UsuarioId = usuarioId, Pergunta = pergunta };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/Chat/enviar", content);
            var result = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<ChatResponse>(result, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            return new ChatResponse { Sucesso = false };
        }

        public async Task<List<ChatHistorico>> ObterHistoricoAsync(int usuarioId)
        {
            var response = await _httpClient.GetAsync($"/Chat/historico/{usuarioId}");
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

        public async Task<bool> AvaliarRespostaAsync(int usuarioId, string pergunta, string resposta, bool foiUtil)
        {
            var request = new { UsuarioId = usuarioId, Pergunta = pergunta, Resposta = resposta, FoiUtil = foiUtil };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/Chat/avaliar", content);
            return response.IsSuccessStatusCode;
        }

        // Tickets
        public async Task<List<TicketDTO>> ObterTicketsPendentesAsync()
        {
            var response = await _httpClient.GetAsync("/Tickets/pendentes");
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

        public async Task<bool> ResolverTicketAsync(int ticketId, string solucao)
        {
            var request = new { TicketId = ticketId, Solucao = solucao };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/Tickets/resolver", content);
            return response.IsSuccessStatusCode;
        }
    }

    // DTOs
    public class LoginResponse
    {
        public bool Sucesso { get; set; }
        public string TipoUsuario { get; set; }
        public int UsuarioId { get; set; }
        public string Nome { get; set; }
        public string Mensagem { get; set; }
    }

    public class ChatResponse
    {
        public bool Sucesso { get; set; }
        public string Resposta { get; set; }
        public DateTime DataHora { get; set; }
    }

    public class ChatHistorico
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Pergunta { get; set; }
        public string Resposta { get; set; }
        public DateTime DataHora { get; set; }
    }

    public class TicketDTO
    {
        public int Id { get; set; }
        public string NomeSolicitante { get; set; }
        public string DescricaoProblema { get; set; }
        public string RespostaIA { get; set; }
        public string Status { get; set; }
        public DateTime DataAbertura { get; set; }
        public string Solucao { get; set; }
    }
}
