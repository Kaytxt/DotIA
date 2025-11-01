using DotIA_Mobile.Models;
using DotIA_Mobile.Services;

namespace DotIA_Mobile.Views
{
    public partial class LoginPage : ContentPage
    {
        private readonly IAuthService _authService;

        public LoginPage()
        {
            InitializeComponent();
            _authService = new AuthService();
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            // Validações
            if (string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                lblMensagem.Text = "⚠️ Digite seu e-mail";
                return;
            }

            if (string.IsNullOrWhiteSpace(txtSenha.Text))
            {
                lblMensagem.Text = "⚠️ Digite sua senha";
                return;
            }

            // Mostrar loading
            btnEntrar.IsEnabled = false;
            loadingIndicator.IsVisible = true;
            loadingIndicator.IsRunning = true;
            lblMensagem.Text = string.Empty;

            try
            {
                var request = new LoginRequest
                {
                    Email = txtEmail.Text.Trim(),
                    Senha = txtSenha.Text
                };

                var response = await _authService.LoginAsync(request);

                if (response.Sucesso)
                {
                    // Salvar sessão do usuário
                    UserSession.Login(
                        response.UsuarioId ?? 0,
                        response.Nome ?? "",
                        response.TipoUsuario ?? "",
                        txtEmail.Text.Trim()
                    );

                    // Navegar para a página principal
                    await Navigation.PushAsync(new ChatPage());
                }
                else
                {
                    lblMensagem.Text = $"❌ {response.Mensagem}";
                }
            }
            catch (Exception ex)
            {
                lblMensagem.Text = $"❌ Erro: {ex.Message}";
            }
            finally
            {
                // Esconder loading
                btnEntrar.IsEnabled = true;
                loadingIndicator.IsVisible = false;
                loadingIndicator.IsRunning = false;
            }
        }

        private async void OnRegistrarClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RegistroPage());
        }
    }
}
