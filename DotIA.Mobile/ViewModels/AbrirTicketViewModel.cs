using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotIA.Mobile.Models;
using DotIA.Mobile.Services;

namespace DotIA.Mobile.ViewModels
{
    public partial class AbrirTicketViewModel : ObservableObject
    {
        private readonly ApiService _apiService;
        private readonly UserSessionService _userSession;

        [ObservableProperty]
        private string titulo = string.Empty;

        [ObservableProperty]
        private string descricao = string.Empty;

        [ObservableProperty]
        private bool isEnviando = false;

        public AbrirTicketViewModel(ApiService apiService, UserSessionService userSession)
        {
            _apiService = apiService;
            _userSession = userSession;
        }

        [RelayCommand]
        private async Task Cancelar()
        {
            if (Application.Current?.MainPage != null)
                await Application.Current.MainPage.Navigation.PopModalAsync();
        }

        [RelayCommand]
        private async Task Enviar()
        {
            // Validações
            if (string.IsNullOrWhiteSpace(Titulo))
            {
                await Application.Current!.MainPage!.DisplayAlert("Atenção", "Por favor, digite um título para o ticket", "OK");
                return;
            }

            if (Titulo.Trim().Length < 5)
            {
                await Application.Current!.MainPage!.DisplayAlert("Atenção", "O título deve ter pelo menos 5 caracteres", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(Descricao))
            {
                await Application.Current!.MainPage!.DisplayAlert("Atenção", "Por favor, descreva seu problema", "OK");
                return;
            }

            if (Descricao.Trim().Length < 20)
            {
                await Application.Current!.MainPage!.DisplayAlert("Atenção", "Por favor, forneça uma descrição mais detalhada (mínimo 20 caracteres)", "OK");
                return;
            }

            // Verifica autenticação
            if (_userSession.UsuarioId == null)
            {
                await Application.Current!.MainPage!.DisplayAlert("Erro", "Usuário não autenticado.", "OK");
                return;
            }

            IsEnviando = true;

            try
            {
                var request = new AbrirTicketDiretoRequest
                {
                    UsuarioId = _userSession.UsuarioId.Value,
                    Titulo = Titulo.Trim(),
                    Descricao = Descricao.Trim()
                };

                System.Diagnostics.Debug.WriteLine($"📝 AbrirTicket: Enviando - UsuarioId={request.UsuarioId}, Titulo={request.Titulo}");

                var sucesso = await _apiService.AbrirTicketDiretoAsync(request);

                if (sucesso)
                {
                    await Application.Current!.MainPage!.DisplayAlert("Sucesso", "✅ Ticket criado com sucesso! Um técnico irá atendê-lo em breve.", "OK");

                    // Fecha o modal
                    await Application.Current.MainPage.Navigation.PopModalAsync();

                    // Envia mensagem para recarregar o histórico
                    MessagingCenter.Send(this, "TicketCriado");
                }
                else
                {
                    await Application.Current!.MainPage!.DisplayAlert("Erro", "Erro ao criar ticket. Verifique sua conexão e tente novamente.", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ AbrirTicket Exception: {ex.Message}");
                await Application.Current!.MainPage!.DisplayAlert("Erro", $"Erro ao criar ticket: {ex.Message}", "OK");
            }
            finally
            {
                IsEnviando = false;
            }
        }
    }
}
