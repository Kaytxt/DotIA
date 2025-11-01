using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace DotIA_Mobile
{
    public partial class MainPage : ContentPage
    {
        private readonly HttpClient _httpClient;

        public MainPage()
        {
            InitializeComponent();

            _httpClient = new HttpClient
            {
                // ⚠️ ⚠️ ⚠️ USE ESTE IP PARA EMULADOR:
                BaseAddress = new Uri("http://10.0.2.2:5100/api"),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }
        private async void OnLoginClicked(object sender, EventArgs e)
        {
            try
            {
                await DisplayAlert("TESTE", "Iniciando teste de conexão...", "OK");

                // TESTE 1: Endpoint PÚBLICO (não sua API)
                var publicHttpClient = new HttpClient();
                var publicResponse = await publicHttpClient.GetAsync("https://httpbin.org/get");
                var publicResult = await publicResponse.Content.ReadAsStringAsync();

                await DisplayAlert("TESTE PÚBLICO", $"✅ Conexão HTTP funciona!\nStatus: {publicResponse.StatusCode}", "OK");

                // TESTE 2: Sua API com localhost
                await DisplayAlert("TESTE", "Agora testando sua API...", "OK");

                var localHttpClient = new HttpClient
                {
                    BaseAddress = new Uri("http://10.0.2.2:5100/api"),
                    Timeout = TimeSpan.FromSeconds(10)
                };

                var localResponse = await localHttpClient.GetAsync("");
                var localResult = await localResponse.Content.ReadAsStringAsync();

                await DisplayAlert("TESTE API", $"✅ Sua API responde!\nStatus: {localResponse.StatusCode}", "OK");

            }
            catch (Exception ex)
            {
                await DisplayAlert("ERRO DETALHADO", $"Falha: {ex.GetType().Name}\n{ex.Message}", "OK");
            }
        }
    }

    public class LoginResponse
    {
        public bool Sucesso { get; set; }
        public string TipoUsuario { get; set; }
        public int UsuarioId { get; set; }
        public string Nome { get; set; }
        public string Mensagem { get; set; }
    }
}