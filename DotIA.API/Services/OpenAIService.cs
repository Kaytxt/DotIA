using System.Text;
using System.Text.Json;

namespace DotIA.API.Services
{
    // Interface do servi�o
    public interface IOpenAIService
    {
        Task<string> ObterRespostaAsync(string pergunta);
    }

    // Implementa��o do servi�o
    public class OpenAIService : IOpenAIService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public OpenAIService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> ObterRespostaAsync(string pergunta)
        {
            try
            {
                var endpoint = _configuration["AzureOpenAI:Endpoint"];
                var apiKey = _configuration["AzureOpenAI:ApiKey"];

                if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
                {
                    return "?? Configura��o da Azure OpenAI n�o encontrada. Verifique o appsettings.json";
                }

                var requestBody = new
                {
                    messages = new[]
                    {
                        new { role = "system", content = "Voc� � um assistente t�cnico de TI especializado em ajudar usu�rios com problemas de tecnologia. Seja claro, objetivo e prestativo." },
                        new { role = "user", content = pergunta }
                    },
                    max_tokens = 800,
                    temperature = 0.7
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                content.Headers.Add("api-key", apiKey);

                var response = await _httpClient.PostAsync(endpoint, content);
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(result);
                    var resposta = doc.RootElement
                        .GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString();

                    return resposta ?? "Desculpe, n�o consegui gerar uma resposta.";
                }

                return $"? Erro na API da OpenAI: {response.StatusCode} - {result}";
            }
            catch (HttpRequestException ex)
            {
                return $"? Erro de conex�o com a API: {ex.Message}";
            }
            catch (JsonException ex)
            {
                return $"? Erro ao processar resposta: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"? Erro inesperado: {ex.Message}";
            }
        }
    }
}