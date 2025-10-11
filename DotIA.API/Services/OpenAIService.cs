using System.Text;
using System.Text.Json;

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
            var endpoint = _configuration["AzureOpenAI:Endpoint"];
            var apiKey = _configuration["AzureOpenAI:ApiKey"];

            var mensagens = new[]
            {
                new {
                    role = "system",
                    content = "Você é DotIA, assistente especializada em TI. Responda apenas perguntas técnicas."
                },
                new { role = "user", content = pergunta }
            };

            var payload = new
            {
                messages = mensagens,
                temperature = 0.7,
                max_tokens = 1000
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("api-key", apiKey);

            var response = await _httpClient.PostAsync(endpoint, content);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var resultado = JsonSerializer.Deserialize<OpenAIResponse>(json);
                return resultado.choices[0].message.content;
            }

            throw new Exception($"Erro API: {response.StatusCode}");
        }
    }

    public class OpenAIResponse
    {
        public Choice[] choices { get; set; }
    }

    public class Choice
    {
        public Message message { get; set; }
    }

    public class Message
    {
        public string content { get; set; }
    }
}