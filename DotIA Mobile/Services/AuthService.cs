using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DotIA_Mobile.Models;

namespace DotIA_Mobile.Services
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<RegistroResponse> RegistrarAsync(RegistroRequest request);
        Task<List<DepartamentoDTO>> ObterDepartamentosAsync();
    }

    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;

        public AuthService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(ApiConfig.BaseUrl),
                Timeout = ApiConfig.Timeout
            };
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("/auth/login", content);
                var resultJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<LoginResponse>(resultJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new LoginResponse { Sucesso = false, Mensagem = "Erro ao processar resposta" };
                }

                return new LoginResponse 
                { 
                    Sucesso = false, 
                    Mensagem = $"Erro na requisição: {response.StatusCode}" 
                };
            }
            catch (Exception ex)
            {
                return new LoginResponse 
                { 
                    Sucesso = false, 
                    Mensagem = $"Erro de conexão: {ex.Message}" 
                };
            }
        }

        public async Task<RegistroResponse> RegistrarAsync(RegistroRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("/auth/registrar", content);
                var resultJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<RegistroResponse>(resultJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new RegistroResponse { Sucesso = false, Mensagem = "Erro ao processar resposta" };
                }

                return new RegistroResponse 
                { 
                    Sucesso = false, 
                    Mensagem = $"Erro na requisição: {response.StatusCode}" 
                };
            }
            catch (Exception ex)
            {
                return new RegistroResponse 
                { 
                    Sucesso = false, 
                    Mensagem = $"Erro de conexão: {ex.Message}" 
                };
            }
        }

        public async Task<List<DepartamentoDTO>> ObterDepartamentosAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/auth/departamentos");
                
                if (response.IsSuccessStatusCode)
                {
                    var resultJson = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<DepartamentoDTO>>(resultJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<DepartamentoDTO>();
                }

                return new List<DepartamentoDTO>();
            }
            catch
            {
                return new List<DepartamentoDTO>();
            }
        }
    }
}
