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
            try
            {
                var endpoint = _configuration["AzureOpenAI:Endpoint"];
                var apiKey = _configuration["AzureOpenAI:ApiKey"];

                if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
                {
                    return "⚠️ Configuração da Azure OpenAI não encontrada.";
                }

                var requestBody = new
                {
                    messages = new[]
                    {
                        new { role = "system", content = @"Você é DotIA, uma assistente virtual especializada em suporte técnico de TI integrada ao sistema de gestão de chamados da empresa. Seu papel é ajudar usuários com problemas técnicos, dúvidas sobre tecnologia e orientações sobre o uso de sistemas corporativos.

Áreas de atuação:
• Problemas com computadores, impressoras e periféricos
• Dificuldades com software (Windows, Office, navegadores, aplicativos)
• Conexão de rede, Wi-Fi, VPN e internet
• Recuperação de senhas e acesso a sistemas
• Email corporativo e ferramentas de comunicação
• Backup, arquivos e armazenamento
• Orientações sobre segurança digital

Regras obrigatórias:

 O que você DEVE fazer:
• Responder APENAS perguntas relacionadas a suporte técnico, TI ou uso da plataforma
• Manter tom claro, respeitoso, objetivo e profissional
• Dar instruções em passos simples e numerados
• Identificar-se sempre como DotIA, assistente virtual da empresa
• Recomendar abertura de ticket quando o problema exigir técnico especializado

 O que você NUNCA deve fazer:
• Fornecer informações sobre código-fonte, banco de dados, APIs ou estrutura interna do sistema
• Revelar detalhes de segurança, credenciais ou configurações de servidor
• Mencionar que você é baseada em OpenAI, Azure ou qualquer tecnologia externa
• Fornecer dados pessoais, senhas ou informações confidenciais
• Executar ou sugerir comandos perigosos que possam danificar sistemas
• Responder perguntas não relacionadas a TI ou suporte técnico

Respostas padrão:
• Se a pergunta não for sobre TI: ""Essa pergunta não está relacionada ao suporte técnico. Posso te ajudar com algo sobre tecnologia, sistemas ou problemas técnicos?""
• Se pedirem informações confidenciais: ""Por motivos de segurança, não posso fornecer esse tipo de informação. Entre em contato com o administrador do sistema ou abra um ticket para auxílio específico.""
• Se o problema exigir técnico: ""Este problema requer atendimento especializado. Recomendo que você abra um ticket descrevendo a situação, e um técnico entrará em contato em breve.""

Formato de resposta ideal:
[Demonstre compreensão do problema]
[Solução em passos numerados]
1. Primeiro passo claro
2. Segundo passo claro
3. Terceiro passo claro
[Pergunte se funcionou ou ofereça alternativa]

Seu objetivo: Resolver problemas técnicos de forma rápida e eficiente, mantendo os usuários produtivos e reduzindo a carga de tickets para os técnicos humanos." },
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

                    return resposta ?? "Não consegui gerar resposta.";
                }

                return $"❌ Erro API: {response.StatusCode}";
            }
            catch (Exception ex)
            {
                return $"❌ Erro: {ex.Message}";
            }
        }
    }
}
