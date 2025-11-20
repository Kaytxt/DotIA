using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotIA.Mobile.Models;
using DotIA.Mobile.Services;
using System.Collections.ObjectModel;

namespace DotIA.Mobile.ViewModels
{
    public partial class RegistroViewModel : ObservableObject
    {
        private readonly ApiService _apiService;

        [ObservableProperty]
        private string nome = string.Empty;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string senha = string.Empty;

        [ObservableProperty]
        private string confirmacaoSenha = string.Empty;

        [ObservableProperty]
        private DepartamentoDTO? departamentoSelecionado;

        [ObservableProperty]
        private ObservableCollection<DepartamentoDTO> departamentos = new();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private string successMessage = string.Empty;

        public RegistroViewModel(ApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task LoadDepartamentosAsync()
        {
            try
            {
                IsLoading = true;
                var deps = await _apiService.ObterDepartamentosAsync();
                Departamentos = new ObservableCollection<DepartamentoDTO>(deps);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erro ao carregar departamentos: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task RegistrarAsync()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            // Validações
            if (string.IsNullOrWhiteSpace(Nome))
            {
                ErrorMessage = "Nome é obrigatório.";
                return;
            }

            if (string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = "Email é obrigatório.";
                return;
            }

            if (!Email.Contains("@"))
            {
                ErrorMessage = "Email inválido.";
                return;
            }

            if (string.IsNullOrWhiteSpace(Senha))
            {
                ErrorMessage = "Senha é obrigatória.";
                return;
            }

            if (Senha.Length < 6)
            {
                ErrorMessage = "Senha deve ter no mínimo 6 caracteres.";
                return;
            }

            if (Senha != ConfirmacaoSenha)
            {
                ErrorMessage = "As senhas não coincidem.";
                return;
            }

            if (DepartamentoSelecionado == null)
            {
                ErrorMessage = "Selecione um departamento.";
                return;
            }

            IsLoading = true;

            try
            {
                var request = new RegistroRequest
                {
                    Nome = Nome,
                    Email = Email,
                    Senha = Senha,
                    ConfirmacaoSenha = ConfirmacaoSenha,
                    IdDepartamento = DepartamentoSelecionado.Id
                };

                var response = await _apiService.RegistroAsync(request);

                if (response.Sucesso)
                {
                    SuccessMessage = response.Mensagem;

                    // Aguarda 2 segundos e volta para login
                    await Task.Delay(2000);
                    var loginPage = App.Current?.Handler?.MauiContext?.Services.GetService<Views.LoginPage>();
                    if (loginPage != null)
                    {
                        Application.Current!.MainPage = loginPage;
                    }
                }
                else
                {
                    ErrorMessage = response.Mensagem;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erro ao registrar: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task NavigateToLoginAsync()
        {
            var loginPage = App.Current?.Handler?.MauiContext?.Services.GetService<Views.LoginPage>();
            if (loginPage != null)
            {
                Application.Current!.MainPage = loginPage;
            }
        }
    }
}
