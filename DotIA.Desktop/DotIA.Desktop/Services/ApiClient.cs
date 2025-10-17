using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DotIA.Desktop.Services
{
    public class ApiClient
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string BASE_URL = "https://localhost:5100/api";

        public ApiClient()
        {
            _httpClient.BaseAddress = new Uri(BASE_URL);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        // ─── LOGIN ───
        public async Task<LoginResponse> LoginAsync(string email, string senha)
        {
            try
            {
                var request = new { Email = email, Senha = senha };
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/Auth/login", content);
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<LoginResponse>(result);
                }

                return new LoginResponse { Sucesso = false, Mensagem = "Erro ao fazer login" };
            }
            catch (Exception ex)
            {
                return new LoginResponse { Sucesso = false, Mensagem = $"Erro: {ex.Message}" };
            }
        }

        // ─── CHAT ───
        public async Task<ChatResponse> EnviarPerguntaAsync(int usuarioId, string pergunta)
        {
            try
            {
                var request = new { UsuarioId = usuarioId, Pergunta = pergunta };
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/Chat/enviar", content);
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<ChatResponse>(result);
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
                var response = await _httpClient.GetAsync($"/Chat/historico/{usuarioId}");
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<List<ChatHistorico>>(result);
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
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/Chat/avaliar", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // ─── TICKETS ───
        public async Task<List<TicketDTO>> ObterTicketsPendentesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/Tickets/pendentes");
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<List<TicketDTO>>(result);
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
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/Tickets/resolver", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }

    // DTOs (mesmos do backend)
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
