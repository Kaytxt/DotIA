using System.Text;
using System.Text.Json;

namespace DotIA.Web.Services
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

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

                var response = await _httpClient.PostAsync("api/Auth/login", content);
                var result = await response.Content.ReadAsStringAsync();

                // Tenta deserializar a resposta
                if (!string.IsNullOrEmpty(result))
                {
                    try
                    {
                        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(result, _jsonOpts);
                        if (loginResponse != null)
                        {
                            // Se Sucesso = true, valida se tem UsuarioId
                            if (loginResponse.Sucesso)
                            {
                                if (loginResponse.UsuarioId.HasValue)
                                {
                                    return loginResponse;
                                }
                                return new LoginResponse { Sucesso = false, Mensagem = "Erro ao processar resposta do servidor" };
                            }

                            // Se Sucesso = false, retorna a mensagem de erro da API
                            if (!string.IsNullOrEmpty(loginResponse.Mensagem))
                            {
                                return loginResponse;
                            }
                        }
                    }
                    catch
                    {
                        // Se não conseguir deserializar, continua para mensagem padrão
                    }
                }

                // Mensagem padrão se algo der errado
                return new LoginResponse { Sucesso = false, Mensagem = "Email ou senha inválidos, tente novamente" };
            }
            catch (Exception ex)
            {
                return new LoginResponse { Sucesso = false, Mensagem = $"Erro: {ex.Message}" };
            }
        }

        // ═══════════════════════════════════════════════════════════
        // REGISTRO
        // ═══════════════════════════════════════════════════════════
        public async Task<RegistroResponse> RegistrarUsuarioAsync(dynamic request)
        {
            try
            {
                var requestObj = new
                {
                    Nome = request.Nome,
                    Email = request.Email,
                    Senha = request.Senha,
                    ConfirmacaoSenha = request.ConfirmacaoSenha,
                    IdDepartamento = request.IdDepartamento
                };

                var json = JsonSerializer.Serialize(requestObj);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/Auth/registro", content);
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<RegistroResponse>(result, _jsonOpts)
                           ?? new RegistroResponse { Sucesso = false, Mensagem = "Erro ao processar resposta" };
                }

                return new RegistroResponse { Sucesso = false, Mensagem = "Erro ao realizar cadastro" };
            }
            catch (Exception ex)
            {
                return new RegistroResponse { Sucesso = false, Mensagem = $"Erro: {ex.Message}" };
            }
        }

        public async Task<List<DepartamentoDTO>> ObterDepartamentosAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/Auth/departamentos");
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<List<DepartamentoDTO>>(result, _jsonOpts)
                           ?? new List<DepartamentoDTO>();
                }

                return new List<DepartamentoDTO>();
            }
            catch
            {
                return new List<DepartamentoDTO>();
            }
        }

        // ═══════════════════════════════════════════════════════════
        // CHAT
        // ═══════════════════════════════════════════════════════════
        public async Task<ChatResponse> EnviarPerguntaAsync(int usuarioId, string pergunta, int? chatId = null)
        {
            try
            {
                var request = new { UsuarioId = usuarioId, Pergunta = pergunta, ChatId = chatId };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/Chat/enviar", content);
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<ChatResponse>(result, _jsonOpts)
                           ?? new ChatResponse { Sucesso = false, Resposta = "Erro ao processar resposta" };
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
                var response = await _httpClient.GetAsync($"api/Chat/historico/{usuarioId}");
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<List<ChatHistorico>>(result, _jsonOpts)
                           ?? new List<ChatHistorico>();
                }

                return new List<ChatHistorico>();
            }
            catch
            {
                return new List<ChatHistorico>();
            }
        }

        public async Task<bool> AvaliarRespostaAsync(int usuarioId, string pergunta, string resposta, bool foiUtil, int chatId)
        {
            try
            {
                var request = new
                {
                    UsuarioId = usuarioId,
                    Pergunta = pergunta,
                    Resposta = resposta,
                    FoiUtil = foiUtil,
                    ChatId = chatId
                };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/Chat/avaliar", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<VerificarRespostaResponse> VerificarRespostaAsync(int chatId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/Chat/verificar-resposta/{chatId}");
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<VerificarRespostaResponse>(result, _jsonOpts)
                           ?? new VerificarRespostaResponse { TemResposta = false };
                }

                return new VerificarRespostaResponse { TemResposta = false };
            }
            catch
            {
                return new VerificarRespostaResponse { TemResposta = false };
            }
        }

        public async Task<bool> EnviarMensagemParaTecnicoAsync(int chatId, string mensagem)
        {
            try
            {
                var request = new { ChatId = chatId, Mensagem = mensagem };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/Chat/enviar-para-tecnico", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> EditarTituloChatAsync(int chatId, string novoTitulo)
        {
            try
            {
                var request = new { NovoTitulo = novoTitulo };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"api/Chat/editar-titulo/{chatId}", content);
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
                var response = await _httpClient.DeleteAsync($"api/Chat/excluir/{chatId}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<AbrirTicketDiretoResponse> AbrirTicketDiretoAsync(int usuarioId, string titulo, string descricao)
        {
            try
            {
                var request = new { UsuarioId = usuarioId, Titulo = titulo, Descricao = descricao };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/Chat/abrir-ticket-direto", content);
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<AbrirTicketDiretoResponse>(result, _jsonOpts)
                           ?? new AbrirTicketDiretoResponse { Sucesso = false, Mensagem = "Erro ao processar resposta" };
                }

                return new AbrirTicketDiretoResponse { Sucesso = false, Mensagem = "Erro ao abrir ticket" };
            }
            catch (Exception ex)
            {
                return new AbrirTicketDiretoResponse { Sucesso = false, Mensagem = $"Erro: {ex.Message}" };
            }
        }

        // ═══════════════════════════════════════════════════════════
        // TICKETS (usuário)
        // ═══════════════════════════════════════════════════════════
        public async Task<List<TicketDTO>> ObterTicketsPendentesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/Tickets/pendentes");
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<List<TicketDTO>>(result, _jsonOpts)
                           ?? new List<TicketDTO>();
                }

                return new List<TicketDTO>();
            }
            catch
            {
                return new List<TicketDTO>();
            }
        }

        public async Task<bool> ResolverTicketAsync(int ticketId, string solucao, bool marcarComoResolvido)
        {
            try
            {
                var request = new
                {
                    TicketId = ticketId,
                    Solucao = solucao,
                    MarcarComoResolvido = marcarComoResolvido
                };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/Tickets/resolver", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<object?> ObterTicketAsync(int ticketId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/Tickets/{ticketId}");
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<object>(result, _jsonOpts);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // GERENTE
        // ═══════════════════════════════════════════════════════════
        public async Task<DashboardDTO> ObterDashboardAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/Gerente/dashboard");
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<DashboardDTO>(result, _jsonOpts)
                           ?? new DashboardDTO();
                }

                return new DashboardDTO();
            }
            catch
            {
                return new DashboardDTO();
            }
        }

        public async Task<List<UsuarioDTO>> ObterUsuariosAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/Gerente/usuarios");
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<List<UsuarioDTO>>(result, _jsonOpts)
                           ?? new List<UsuarioDTO>();
                }

                return new List<UsuarioDTO>();
            }
            catch
            {
                return new List<UsuarioDTO>();
            }
        }

        public async Task<UsuarioDetalheDTO> ObterUsuarioAsync(int usuarioId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/Gerente/usuarios/{usuarioId}");
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<UsuarioDetalheDTO>(result, _jsonOpts)
                           ?? new UsuarioDetalheDTO();
                }

                return new UsuarioDetalheDTO();
            }
            catch
            {
                return new UsuarioDetalheDTO();
            }
        }

        public async Task<bool> AtualizarUsuarioAsync(int usuarioId, string nome, string email, int idDepartamento)
        {
            try
            {
                var request = new { Nome = nome, Email = email, IdDepartamento = idDepartamento };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"api/Gerente/usuarios/{usuarioId}", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ExcluirUsuarioAsync(int usuarioId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/Gerente/usuarios/{usuarioId}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<TicketUsuarioDTO>> ObterTicketsUsuarioAsync(int usuarioId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/Gerente/usuarios/{usuarioId}/tickets");
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<List<TicketUsuarioDTO>>(result, _jsonOpts)
                           ?? new List<TicketUsuarioDTO>();
                }

                return new List<TicketUsuarioDTO>();
            }
            catch
            {
                return new List<TicketUsuarioDTO>();
            }
        }

        public async Task<List<RelatorioDepartamentoDTO>> ObterRelatorioDepartamentosAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/Gerente/relatorio-departamentos");
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<List<RelatorioDepartamentoDTO>>(result, _jsonOpts)
                           ?? new List<RelatorioDepartamentoDTO>();
                }

                return new List<RelatorioDepartamentoDTO>();
            }
            catch
            {
                return new List<RelatorioDepartamentoDTO>();
            }
        }

        // >>> Estes eram os métodos que estavam fora da classe <<<
        // Obter todos os tickets
        public async Task<List<TicketGerenteDTO>> ObterTodosTicketsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/Gerente/tickets/todos");
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<List<TicketGerenteDTO>>(result, _jsonOpts)
                           ?? new List<TicketGerenteDTO>();
                }

                return new List<TicketGerenteDTO>();
            }
            catch
            {
                return new List<TicketGerenteDTO>();
            }
        }

        // Obter tickets abertos
        public async Task<List<TicketGerenteDTO>> ObterTicketsAbertosAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/Gerente/tickets/abertos");
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<List<TicketGerenteDTO>>(result, _jsonOpts)
                           ?? new List<TicketGerenteDTO>();
                }

                return new List<TicketGerenteDTO>();
            }
            catch
            {
                return new List<TicketGerenteDTO>();
            }
        }

        // Responder ticket como gerente
        public async Task<bool> ResponderTicketGerenteAsync(int ticketId, string resposta, bool marcarComoResolvido)
        {
            try
            {
                var request = new
                {
                    TicketId = ticketId,
                    Resposta = resposta,
                    MarcarComoResolvido = marcarComoResolvido
                };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/Gerente/tickets/responder", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // Alterar senha do usuário
        public async Task<bool> AlterarSenhaUsuarioAsync(int usuarioId, string novaSenha)
        {
            try
            {
                var request = new { NovaSenha = novaSenha };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"api/Gerente/usuarios/{usuarioId}/senha", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> AlterarCargoUsuarioAsync(int usuarioId, string cargo)
        {
            try
            {
                var request = new { Cargo = cargo };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"api/Gerente/usuarios/{usuarioId}/cargo", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // DTOs
    // ═══════════════════════════════════════════════════════════

    public class LoginResponse
    {
        public bool Sucesso { get; set; }
        public string TipoUsuario { get; set; } = string.Empty;
        public int? UsuarioId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Mensagem { get; set; } = string.Empty;
    }

    public class RegistroResponse
    {
        public bool Sucesso { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        public int UsuarioId { get; set; }
    }

    public class DepartamentoDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
    }

    public class ChatResponse
    {
        public bool Sucesso { get; set; }
        public string Resposta { get; set; } = string.Empty;
        public DateTime DataHora { get; set; }
        public int ChatId { get; set; }
    }

    public class ChatHistorico
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Pergunta { get; set; } = string.Empty;
        public string Resposta { get; set; } = string.Empty;
        public DateTime DataHora { get; set; }
        public int Status { get; set; }
        public int? IdTicket { get; set; }
        public string StatusTexto { get; set; } = string.Empty;
    }

    public class VerificarRespostaResponse
    {
        public bool TemResposta { get; set; }
        public string Solucao { get; set; } = string.Empty;
        public int Status { get; set; }
        public int StatusTicket { get; set; }
        public DateTime? DataResposta { get; set; }
    }

    public class TicketDTO
    {
        public int Id { get; set; }
        public string NomeSolicitante { get; set; } = string.Empty;
        public string DescricaoProblema { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime DataAbertura { get; set; }
        public string? Solucao { get; set; }
        public int ChatId { get; set; }
        public string PerguntaOriginal { get; set; } = string.Empty;
        public string RespostaIA { get; set; } = string.Empty;
    }

    // DTOs do gerente
    public class DashboardDTO
    {
        public int TotalUsuarios { get; set; }
        public int TotalTickets { get; set; }
        public int TicketsAbertos { get; set; }
        public int TicketsResolvidos { get; set; }
        public int TotalChats { get; set; }
        public int ChatsResolvidos { get; set; }
        public int TicketsResolvidosHoje { get; set; }
        public List<TopUsuarioDTO> TopUsuarios { get; set; } = new List<TopUsuarioDTO>();
    }

    public class TicketGerenteDTO
    {
        public int Id { get; set; }
        public int IdSolicitante { get; set; }
        public string NomeSolicitante { get; set; } = string.Empty;
        public string EmailSolicitante { get; set; } = string.Empty;
        public string Departamento { get; set; } = string.Empty;
        public string DescricaoProblema { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int IdStatus { get; set; }
        public DateTime DataAbertura { get; set; }
        public DateTime? DataEncerramento { get; set; }
        public string? Solucao { get; set; }
        public int ChatId { get; set; }
        public string PerguntaOriginal { get; set; } = string.Empty;
        public string RespostaIA { get; set; } = string.Empty;
    }

    public class TopUsuarioDTO
    {
        public string Nome { get; set; } = string.Empty;
        public int TotalTickets { get; set; }
    }

    public class UsuarioDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Departamento { get; set; } = string.Empty;
        public int IdDepartamento { get; set; }
        public int TotalTickets { get; set; }
        public int TicketsAbertos { get; set; }
        public int TotalChats { get; set; }
    }

    public class UsuarioDetalheDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int IdDepartamento { get; set; }
        public string Departamento { get; set; } = string.Empty;
    }

    public class TicketUsuarioDTO
    {
        public int Id { get; set; }
        public string DescricaoProblema { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int IdStatus { get; set; }
        public DateTime DataAbertura { get; set; }
        public DateTime? DataEncerramento { get; set; }
        public string? Solucao { get; set; }
        public int ChatId { get; set; }
        public string PerguntaOriginal { get; set; } = string.Empty;
        public string RespostaIA { get; set; } = string.Empty;
    }

    public class RelatorioDepartamentoDTO
    {
        public string Departamento { get; set; } = string.Empty;
        public int TotalUsuarios { get; set; }
        public int TotalTickets { get; set; }
        public int TicketsAbertos { get; set; }
        public int TicketsResolvidos { get; set; }
    }

    public class AbrirTicketDiretoResponse
    {
        public bool Sucesso { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        public int TicketId { get; set; }
        public int ChatId { get; set; }
    }
}