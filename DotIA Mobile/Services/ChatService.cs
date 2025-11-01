using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DotIA_Mobile.Models;

namespace DotIA_Mobile.Services
{
    public interface IChatService
    {
        Task<ChatResponse> EnviarPerguntaAsync(ChatRequest request);
        Task<List<ChatHistoricoDTO>> ObterHistoricoAsync(int usuarioId);
        Task<bool> AvaliarRespostaAsync(AvaliacaoRequest request);
        Task<bool> EnviarMensagemParaTecnicoAsync(MensagemUsuarioRequest request);
        Task<VerificarRespostaDTO?> VerificarRespostaTecnicoAsync(int chatId);
        Task<DetalhesChat?> ObterDetalhesChatAsync(int chatId);
        Task<bool> EditarTituloChatAsync(int chatId, string novoTitulo);
        Task<bool> ExcluirChatAsync(int chatId);
    }

    public class ChatService : IChatService
    {
        private readonly HttpClient _httpClient;

        public ChatService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(ApiConfig.BaseUrl),
                Timeout = ApiConfig.Timeout
            };
        }

        public async Task<ChatResponse> EnviarPerguntaAsync(ChatRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("/chat/enviar", content);
                var resultJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<ChatResponse>(resultJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new ChatResponse { Sucesso = false, Resposta = "Erro ao processar resposta" };
                }

                return new ChatResponse 
                { 
                    Sucesso = false, 
                    Resposta = $"Erro na requisição: {response.StatusCode}" 
                };
            }
            catch (Exception ex)
            {
                return new ChatResponse 
                { 
                    Sucesso = false, 
                    Resposta = $"Erro de conexão: {ex.Message}" 
                };
            }
        }

        public async Task<List<ChatHistoricoDTO>> ObterHistoricoAsync(int usuarioId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/chat/historico/{usuarioId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var resultJson = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<ChatHistoricoDTO>>(resultJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<ChatHistoricoDTO>();
                }

                return new List<ChatHistoricoDTO>();
            }
            catch
            {
                return new List<ChatHistoricoDTO>();
            }
        }

        public async Task<bool> AvaliarRespostaAsync(AvaliacaoRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("/chat/avaliar", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> EnviarMensagemParaTecnicoAsync(MensagemUsuarioRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("/chat/enviar-para-tecnico", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<VerificarRespostaDTO?> VerificarRespostaTecnicoAsync(int chatId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/chat/verificar-resposta/{chatId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var resultJson = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<VerificarRespostaDTO>(resultJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<DetalhesChat?> ObterDetalhesChatAsync(int chatId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/chat/detalhes/{chatId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var resultJson = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<DetalhesChat>(resultJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> EditarTituloChatAsync(int chatId, string novoTitulo)
        {
            try
            {
                var request = new EditarTituloRequest { NovoTitulo = novoTitulo };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PutAsync($"/chat/editar-titulo/{chatId}", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ExcluirChatAsync(int chatId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/chat/excluir/{chatId}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
