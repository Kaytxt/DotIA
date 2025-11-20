// DotIA.Desktop/DotIA.Desktop/Services/ApiClient.cs
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
        private const string BASE_URL = "http://localhost:5100";

        // Inicializa uma única vez
        private static readonly HttpClient _httpClient = CreateClient();

        private static HttpClient CreateClient()
        {
            var c = new HttpClient
            {
                BaseAddress = new Uri(BASE_URL),
                Timeout = TimeSpan.FromSeconds(30)
            };
            return c;
        }

        // Construtor vazio – não mexe mais no HttpClient
        public ApiClient() { }


        // ─── LOGIN E REGISTRO ───
        public async Task<LoginResponse> LoginAsync(string email, string senha)
        {
            try
            {
                var request = new { Email = email, Senha = senha };
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/Auth/login", content);
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

        public async Task<RegistroResponse> RegistrarAsync(string nome, string email, string senha, string confirmacaoSenha, int idDepartamento)
        {
            try
            {
                var request = new
                {
                    Nome = nome,
                    Email = email,
                    Senha = senha,
                    ConfirmacaoSenha = confirmacaoSenha,
                    IdDepartamento = idDepartamento
                };
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/Auth/registro", content);
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<RegistroResponse>(result);
                }

                return new RegistroResponse { Sucesso = false, Mensagem = "Erro ao registrar" };
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
                var response = await _httpClient.GetAsync("/api/Auth/departamentos");
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<List<DepartamentoDTO>>(result);
                }

                return new List<DepartamentoDTO>();
            }
            catch
            {
                return new List<DepartamentoDTO>();
            }
        }

        // ─── CHAT ───
        public async Task<ChatResponse> EnviarPerguntaAsync(int usuarioId, string pergunta, int? chatId = null)
        {
            try
            {
                var request = new { UsuarioId = usuarioId, Pergunta = pergunta, ChatId = chatId };
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/Chat/enviar", content);
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
                var response = await _httpClient.GetAsync($"/api/Chat/historico/{usuarioId}");
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

        public async Task<bool> AvaliarRespostaAsync(int usuarioId, string pergunta, string resposta, bool foiUtil, int chatId)
        {
            try
            {
                var request = new { UsuarioId = usuarioId, Pergunta = pergunta, Resposta = resposta, FoiUtil = foiUtil, ChatId = chatId };
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/Chat/avaliar", content);
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
                var response = await _httpClient.GetAsync($"/api/Chat/verificar-resposta/{chatId}");
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<VerificarRespostaResponse>(result);
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
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/Chat/enviar-para-tecnico", content);
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
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/Chat/abrir-ticket-direto", content);
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<AbrirTicketDiretoResponse>(result);
                }

                return new AbrirTicketDiretoResponse { Sucesso = false, Mensagem = "Erro ao abrir ticket" };
            }
            catch (Exception ex)
            {
                return new AbrirTicketDiretoResponse { Sucesso = false, Mensagem = $"Erro: {ex.Message}" };
            }
        }

        public async Task<bool> EditarTituloChatAsync(int chatId, string novoTitulo)
        {
            try
            {
                var request = new { NovoTitulo = novoTitulo };
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"/api/Chat/editar-titulo/{chatId}", content);
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
                var response = await _httpClient.DeleteAsync($"/api/Chat/excluir/{chatId}");
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
                var response = await _httpClient.GetAsync("/api/Tickets/pendentes");
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

        public async Task<bool> ResolverTicketAsync(int ticketId, string solucao, bool marcarComoResolvido)
        {
            try
            {
                var request = new { TicketId = ticketId, Solucao = solucao, MarcarComoResolvido = marcarComoResolvido };
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/Tickets/resolver", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<TicketCompleto> ObterTicketAsync(int ticketId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/Tickets/{ticketId}");
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<TicketCompleto>(result);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // ─── GERENTE ───
        public async Task<DashboardDTO> ObterDashboardAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/Gerente/dashboard");
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<DashboardDTO>(result);
                }

                return new DashboardDTO();
            }
            catch
            {
                return new DashboardDTO();
            }
        }

        public async Task<List<TicketGerenteDTO>> ObterTodosTicketsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/Gerente/tickets/todos");
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<List<TicketGerenteDTO>>(result);
                }

                return new List<TicketGerenteDTO>();
            }
            catch
            {
                return new List<TicketGerenteDTO>();
            }
        }

        public async Task<List<UsuarioDTO>> ObterUsuariosAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/Gerente/usuarios");
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<List<UsuarioDTO>>(result);
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
                var response = await _httpClient.GetAsync($"/api/Gerente/usuarios/{usuarioId}");
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<UsuarioDetalheDTO>(result);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> AtualizarUsuarioAsync(int usuarioId, string nome, string email, int idDepartamento)
        {
            try
            {
                var request = new { Nome = nome, Email = email, IdDepartamento = idDepartamento };
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"/api/Gerente/usuarios/{usuarioId}", content);
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
                var response = await _httpClient.DeleteAsync($"/api/Gerente/usuarios/{usuarioId}");
                return response.IsSuccessStatusCode;
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
                var request = new { NovaSenha = novaSenha };
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"/api/Gerente/usuarios/{usuarioId}/senha", content);
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
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"/api/Gerente/usuarios/{usuarioId}/cargo", content);
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
                var response = await _httpClient.GetAsync($"/api/Gerente/usuarios/{usuarioId}/tickets");
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<List<TicketUsuarioDTO>>(result);
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
                var response = await _httpClient.GetAsync("/api/Gerente/relatorio-departamentos");
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<List<RelatorioDepartamentoDTO>>(result);
                }

                return new List<RelatorioDepartamentoDTO>();
            }
            catch
            {
                return new List<RelatorioDepartamentoDTO>();
            }
        }

        public async Task<bool> ResponderTicketGerenteAsync(int ticketId, string resposta, bool marcarComoResolvido)
        {
            try
            {
                var request = new { TicketId = ticketId, Resposta = resposta, MarcarComoResolvido = marcarComoResolvido };
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/Gerente/tickets/responder", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // DTOs - Modelos de Dados
    // ═══════════════════════════════════════════════════════════════════

    public class LoginResponse
    {
        public bool Sucesso { get; set; }
        public string TipoUsuario { get; set; }
        public int UsuarioId { get; set; }
        public string Nome { get; set; }
        public string Mensagem { get; set; }
    }

    public class RegistroResponse
    {
        public bool Sucesso { get; set; }
        public string Mensagem { get; set; }
        public int UsuarioId { get; set; }
    }

    public class DepartamentoDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; }
    }

    public class ChatResponse
    {
        public bool Sucesso { get; set; }
        public string Resposta { get; set; }
        public DateTime DataHora { get; set; }
        public int ChatId { get; set; }
    }

    public class ChatHistorico
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Pergunta { get; set; }
        public string Resposta { get; set; }
        public DateTime DataHora { get; set; }
        public int Status { get; set; }
        public int? IdTicket { get; set; }
        public string StatusTexto { get; set; }
    }

    public class VerificarRespostaResponse
    {
        public bool TemResposta { get; set; }
        public string Solucao { get; set; }
        public int Status { get; set; }
        public int StatusTicket { get; set; }
        public DateTime? DataResposta { get; set; }
    }

    public class AbrirTicketDiretoResponse
    {
        public bool Sucesso { get; set; }
        public string Mensagem { get; set; }
        public int TicketId { get; set; }
        public int ChatId { get; set; }
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
        public int ChatId { get; set; }
        public string PerguntaOriginal { get; set; }
    }

    public class TicketCompleto
    {
        public TicketSimples Ticket { get; set; }
        public string NomeSolicitante { get; set; }
        public string EmailSolicitante { get; set; }
        public ChatInfo Chat { get; set; }
    }

    public class TicketSimples
    {
        public int Id { get; set; }
        public int IdSolicitante { get; set; }
        public int IdTecnico { get; set; }
        public int IdSubcategoria { get; set; }
        public int IdNivel { get; set; }
        public string DescricaoProblema { get; set; }
        public int IdStatus { get; set; }
        public DateTime DataAbertura { get; set; }
        public DateTime? DataEncerramento { get; set; }
        public string Solucao { get; set; }
    }

    public class ChatInfo
    {
        public int Id { get; set; }
        public string Pergunta { get; set; }
        public string Resposta { get; set; }
        public DateTime DataHora { get; set; }
        public int Status { get; set; }
    }

    // DTOs do Gerente
    public class DashboardDTO
    {
        public int TotalUsuarios { get; set; }
        public int TotalTickets { get; set; }
        public int TicketsAbertos { get; set; }
        public int TicketsResolvidos { get; set; }
        public int TotalChats { get; set; }
        public int ChatsResolvidos { get; set; }
        public int TicketsResolvidosHoje { get; set; }
        public List<TopUsuarioDTO> TopUsuarios { get; set; }
    }

    public class TopUsuarioDTO
    {
        public string Nome { get; set; }
        public int TotalTickets { get; set; }
    }

    public class TicketGerenteDTO
    {
        public int Id { get; set; }
        public int IdSolicitante { get; set; }
        public string NomeSolicitante { get; set; }
        public string EmailSolicitante { get; set; }
        public string Departamento { get; set; }
        public string DescricaoProblema { get; set; }
        public string Status { get; set; }
        public int IdStatus { get; set; }
        public DateTime DataAbertura { get; set; }
        public DateTime? DataEncerramento { get; set; }
        public string Solucao { get; set; }
        public int ChatId { get; set; }
        public string PerguntaOriginal { get; set; }
        public string RespostaIA { get; set; }
    }

    public class UsuarioDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Email { get; set; }
        public string Departamento { get; set; }
        public int IdDepartamento { get; set; }
        public int TotalTickets { get; set; }
        public int TicketsAbertos { get; set; }
        public int TotalChats { get; set; }
    }

    public class UsuarioDetalheDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Email { get; set; }
        public int IdDepartamento { get; set; }
        public string Departamento { get; set; }
    }

    public class TicketUsuarioDTO
    {
        public int Id { get; set; }
        public string DescricaoProblema { get; set; }
        public string Status { get; set; }
        public int IdStatus { get; set; }
        public DateTime DataAbertura { get; set; }
        public DateTime? DataEncerramento { get; set; }
        public string Solucao { get; set; }
        public int ChatId { get; set; }
        public string PerguntaOriginal { get; set; }
        public string RespostaIA { get; set; }
    }

    public class RelatorioDepartamentoDTO
    {
        public string Departamento { get; set; }
        public int TotalUsuarios { get; set; }
        public int TotalTickets { get; set; }
        public int TicketsAbertos { get; set; }
        public int TicketsResolvidos { get; set; }
    }
}