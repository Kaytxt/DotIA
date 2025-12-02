using System.Net.Http.Json;
using System.Text;
using DotIA.Mobile.Models;
using Newtonsoft.Json;

namespace DotIA.Mobile.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://dotia-api.azurewebsites.net";
        // 10.0.2.2 aponta pro localhost da máquina host quando roda no emulador
        // pra testar no celular físico tem que trocar pro IP da rede tipo 192.168.x.x

        public ApiService()
        {
#if DEBUG
            // Bypass SSL validation em modo Debug (apenas para desenvolvimento)
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(60)
            };
#else
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(60)
            };
#endif

            System.Diagnostics.Debug.WriteLine($"ApiService criado com BaseUrl: {BaseUrl}");
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== API SERVICE - LOGIN ===");
                System.Diagnostics.Debug.WriteLine($"Base URL: {_httpClient.BaseAddress}");
                System.Diagnostics.Debug.WriteLine($"Endpoint: /api/auth/login");
                System.Diagnostics.Debug.WriteLine($"Email: {request.Email}");

                var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);

                System.Diagnostics.Debug.WriteLine($"Status Code: {response.StatusCode}");

                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

                System.Diagnostics.Debug.WriteLine($"Resultado: {(result?.Sucesso == true ? "Sucesso" : "Falha")}");

                return result ?? new LoginResponse { Sucesso = false, Mensagem = "Erro ao processar resposta" };
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERRO HTTP: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"InnerException: {ex.InnerException?.Message}");
                return new LoginResponse { Sucesso = false, Mensagem = $"Erro de conexão: {ex.Message}" };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERRO GERAL: {ex.Message}");
                return new LoginResponse { Sucesso = false, Mensagem = $"Erro: {ex.Message}" };
            }
        }

        public async Task<RegistroResponse> RegistroAsync(RegistroRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/auth/registro", request);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<RegistroResponse>();
                return result ?? new RegistroResponse { Sucesso = false, Mensagem = "Erro ao processar resposta" };
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
                var response = await _httpClient.GetAsync("/api/auth/departamentos");
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<List<DepartamentoDTO>>();
                return result ?? new List<DepartamentoDTO>();
            }
            catch
            {
                return new List<DepartamentoDTO>();
            }
        }

        // ════════════════════════════════════════════════════════
        // CHAT (SOLICITANTE)
        // ════════════════════════════════════════════════════════

        public async Task<ChatResponse> EnviarPerguntaAsync(ChatRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/chat/enviar", request);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<ChatResponse>();
                return result ?? new ChatResponse { Sucesso = false, Resposta = "Erro ao processar resposta" };
            }
            catch (Exception ex)
            {
                return new ChatResponse { Sucesso = false, Resposta = $"Erro: {ex.Message}" };
            }
        }

        public async Task<List<ChatHistoricoDTO>> ObterHistoricoChatAsync(int usuarioId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/chat/historico/{usuarioId}");
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<List<ChatHistoricoDTO>>();
                return result ?? new List<ChatHistoricoDTO>();
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
                var response = await _httpClient.PostAsJsonAsync("/api/chat/avaliar", request);
                response.EnsureSuccessStatusCode();
                return true;
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
                var response = await _httpClient.PostAsJsonAsync("/api/chat/enviar-para-tecnico", request);
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> AbrirTicketDiretoAsync(AbrirTicketDiretoRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/chat/abrir-ticket-direto", request);
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<VerificarRespostaResponse?> VerificarRespostaTecnicoAsync(int chatId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/chat/verificar-resposta/{chatId}");
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<VerificarRespostaResponse>();
                return result;
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
                var response = await _httpClient.PutAsJsonAsync($"/api/chat/editar-titulo/{chatId}", request);
                response.EnsureSuccessStatusCode();
                return true;
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
                var response = await _httpClient.DeleteAsync($"/api/chat/excluir/{chatId}");
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ════════════════════════════════════════════════════════
        // TICKETS (TÉCNICO)
        // ════════════════════════════════════════════════════════

        public async Task<List<TicketDTO>> ObterTicketsPendentesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/tickets/pendentes");
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<List<TicketDTO>>();
                return result ?? new List<TicketDTO>();
            }
            catch
            {
                return new List<TicketDTO>();
            }
        }

        public async Task<bool> ResolverTicketAsync(ResolverTicketRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/tickets/resolver", request);
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ════════════════════════════════════════════════════════
        // GERENTE - TICKETS
        // ════════════════════════════════════════════════════════

        public async Task<List<TicketGerenteDTO>> ObterTodosTicketsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/gerente/tickets/todos");
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<List<TicketGerenteDTO>>();
                return result ?? new List<TicketGerenteDTO>();
            }
            catch
            {
                return new List<TicketGerenteDTO>();
            }
        }

        public async Task<List<TicketGerenteDTO>> ObterTicketsAbertosAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/gerente/tickets/abertos");
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<List<TicketGerenteDTO>>();
                return result ?? new List<TicketGerenteDTO>();
            }
            catch
            {
                return new List<TicketGerenteDTO>();
            }
        }

        public async Task<List<UsuarioGerenteDTO>> ObterUsuariosAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/gerente/usuarios");
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<List<UsuarioGerenteDTO>>();
                return result ?? new List<UsuarioGerenteDTO>();
            }
            catch
            {
                return new List<UsuarioGerenteDTO>();
            }
        }

        public async Task<bool> ResponderTicketGerenteAsync(ResponderTicketGerenteRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/gerente/tickets/responder", request);
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> AtualizarUsuarioAsync(int usuarioId, AtualizarUsuarioRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"/api/gerente/usuarios/{usuarioId}", request);
                response.EnsureSuccessStatusCode();
                return true;
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
                var response = await _httpClient.DeleteAsync($"/api/gerente/usuarios/{usuarioId}");
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> AlterarSenhaUsuarioAsync(int usuarioId, string novaSenha)
        {
            try
            {
                var request = new AlterarSenhaRequest { NovaSenha = novaSenha };
                var response = await _httpClient.PutAsJsonAsync($"/api/gerente/usuarios/{usuarioId}/senha", request);
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<DashboardDTO?> ObterDashboardAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/gerente/dashboard");
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<DashboardDTO>();
                return result;
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<RelatorioDepartamentoDTO>> ObterRelatorioDepartamentosAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/gerente/relatorio-departamentos");
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<List<RelatorioDepartamentoDTO>>();
                return result ?? new List<RelatorioDepartamentoDTO>();
            }
            catch
            {
                return new List<RelatorioDepartamentoDTO>();
            }
        }

        public async Task<bool> AlterarCargoUsuarioAsync(int usuarioId, string cargo)
        {
            try
            {
                var request = new AlterarCargoRequest { Cargo = cargo };
                var response = await _httpClient.PutAsJsonAsync($"/api/gerente/usuarios/{usuarioId}/cargo", request);
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<TicketGerenteDTO>> ObterTicketsUsuarioAsync(int usuarioId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/gerente/usuarios/{usuarioId}/tickets");
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<List<TicketGerenteDTO>>();
                return result ?? new List<TicketGerenteDTO>();
            }
            catch
            {
                return new List<TicketGerenteDTO>();
            }
        }
    }
}

