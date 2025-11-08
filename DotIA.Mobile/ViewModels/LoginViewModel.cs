using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotIA.Mobile.Models;
using DotIA.Mobile.Services;
using DotIA.Mobile.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DotIA.Mobile.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly ApiService _apiService;
        private readonly UserSessionService _userSession;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string senha = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        public LoginViewModel(ApiService apiService, UserSessionService userSession)
        {
            _apiService = apiService;
            _userSession = userSession;
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            System.Diagnostics.Debug.WriteLine("=== LOGIN INICIADO ===");
            System.Diagnostics.Debug.WriteLine($"Email: {Email}");
            System.Diagnostics.Debug.WriteLine($"Senha: {(string.IsNullOrWhiteSpace(Senha) ? "vazia" : "preenchida")}");

            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Senha))
            {
                ErrorMessage = "Por favor, preencha todos os campos.";
                System.Diagnostics.Debug.WriteLine("Erro: Campos vazios");
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                System.Diagnostics.Debug.WriteLine("Criando request...");
                var request = new LoginRequest
                {
                    Email = Email,
                    Senha = Senha
                };

                System.Diagnostics.Debug.WriteLine("Chamando API...");
                var response = await _apiService.LoginAsync(request);
                System.Diagnostics.Debug.WriteLine($"Resposta recebida - Sucesso: {response.Sucesso}");

                if (response.Sucesso)
                {
                    System.Diagnostics.Debug.WriteLine($"=== LOGIN BEM-SUCEDIDO ===");
                    System.Diagnostics.Debug.WriteLine($"TipoUsuario: '{response.TipoUsuario}'");
                    System.Diagnostics.Debug.WriteLine($"UsuarioId: {response.UsuarioId}");
                    System.Diagnostics.Debug.WriteLine($"Nome: {response.Nome}");

                    // Salva sessão
                    _userSession.SetUserSession(
                        response.UsuarioId!.Value,
                        response.Nome!,
                        Email,
                        response.TipoUsuario!
                    );

                    System.Diagnostics.Debug.WriteLine($"Sessão salva com sucesso!");

                    // Navega para a tela apropriada
                    if (response.TipoUsuario == "Solicitante")
                    {
                        System.Diagnostics.Debug.WriteLine("→ Obtendo ChatPage...");
                        var chatPage = App.Current?.Handler?.MauiContext?.Services.GetService<ChatPage>();
                        if (chatPage != null)
                        {
                            System.Diagnostics.Debug.WriteLine("✓ ChatPage obtida!");
                            Application.Current!.MainPage = chatPage;
                            System.Diagnostics.Debug.WriteLine("✓ Navegação para ChatPage concluída!");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("✗ ERRO: ChatPage é NULL!");
                        }
                    }
                    else if (response.TipoUsuario == "Tecnico")
                    {
                        System.Diagnostics.Debug.WriteLine("→ Obtendo TecnicoPage...");
                        var tecnicoPage = App.Current?.Handler?.MauiContext?.Services.GetService<TecnicoPage>();
                        if (tecnicoPage != null)
                        {
                            System.Diagnostics.Debug.WriteLine("✓ TecnicoPage obtida!");
                            Application.Current!.MainPage = tecnicoPage;
                            System.Diagnostics.Debug.WriteLine("✓ Navegação para TecnicoPage concluída!");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("✗ ERRO: TecnicoPage é NULL!");
                        }
                    }
                    else if (response.TipoUsuario == "Gerente")
                    {
                        System.Diagnostics.Debug.WriteLine("→ Obtendo GerentePage...");
                        var gerentePage = App.Current?.Handler?.MauiContext?.Services.GetService<GerentePage>();
                        if (gerentePage != null)
                        {
                            System.Diagnostics.Debug.WriteLine("✓ GerentePage obtida!");
                            Application.Current!.MainPage = gerentePage;
                            System.Diagnostics.Debug.WriteLine("✓ Navegação para GerentePage concluída!");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("✗ ERRO: GerentePage é NULL!");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"✗ ERRO: TipoUsuario '{response.TipoUsuario}' não reconhecido!");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Login falhou: {response.Mensagem}");
                    ErrorMessage = response.Mensagem;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EXCEÇÃO: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Mensagem: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                ErrorMessage = $"Erro ao fazer login: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                System.Diagnostics.Debug.WriteLine("=== LOGIN FINALIZADO ===");
            }
        }

        [RelayCommand]
        private async Task NavigateToRegistroAsync()
        {
            var registroPage = App.Current?.Handler?.MauiContext?.Services.GetService<RegistroPage>();
            if (registroPage != null)
            {
                Application.Current!.MainPage = new NavigationPage(registroPage);
            }
        }
    }
}
