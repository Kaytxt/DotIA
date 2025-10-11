namespace DotIA.API.Services
{
    public interface IOpenAIService
    {
        Task<string> ObterRespostaAsync(string pergunta);
    }

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
                    return "Erro: Configuração da API não encontrada.";
                }

                var requestBody = new
                {
                    messages = new[]
                    {
                        new { role = "system", content = "Você é um assistente de TI prestativo." },
                        new { role = "user", content = pergunta }
                    },
                    max_tokens = 800,
                    temperature = 0.7
                };

                var content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(requestBody),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                content.Headers.Add("api-key", apiKey);

                var response = await _httpClient.PostAsync(endpoint, content);
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(result);
                    return doc.RootElement
                        .GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString() ?? "Sem resposta";
                }

                return $"Erro na API: {response.StatusCode}";
            }
            catch (Exception ex)
            {
                return $"Erro ao processar: {ex.Message}";
            }
        }
    }
}