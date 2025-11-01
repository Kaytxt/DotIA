using DotIA_Mobile.Models;
using DotIA_Mobile.Services;

namespace DotIA_Mobile.Views
{
    public partial class RegistroPage : ContentPage
    {
        private readonly IAuthService _authService;
        private List<DepartamentoDTO> _departamentos;

        public RegistroPage()
        {
            InitializeComponent();
            _authService = new AuthService();
            _departamentos = new List<DepartamentoDTO>();
            CarregarDepartamentos();
        }

        private async void CarregarDepartamentos()
        {
            try
            {
                _departamentos = await _authService.ObterDepartamentosAsync();
                
                if (_departamentos.Any())
                {
                    pickerDepartamento.ItemsSource = _departamentos.Select(d => d.Nome).ToList();
                }
                else
                {
                    // Departamentos padrão se a API não retornar
                    _departamentos = new List<DepartamentoDTO>
                    {
                        new DepartamentoDTO { Id = 1, Nome = "TI" },
                        new DepartamentoDTO { Id = 2, Nome = "RH" },
                        new DepartamentoDTO { Id = 3, Nome = "Financeiro" },
                        new DepartamentoDTO { Id = 4, Nome = "Comercial" }
                    };
                    pickerDepartamento.ItemsSource = _departamentos.Select(d => d.Nome).ToList();
                }
            }
            catch
            {
                // Fallback para departamentos padrão
                _departamentos = new List<DepartamentoDTO>
                {
                    new DepartamentoDTO { Id = 1, Nome = "TI" },
                    new DepartamentoDTO { Id = 2, Nome = "RH" },
                    new DepartamentoDTO { Id = 3, Nome = "Financeiro" },
                    new DepartamentoDTO { Id = 4, Nome = "Comercial" }
                };
                pickerDepartamento.ItemsSource = _departamentos.Select(d => d.Nome).ToList();
            }
        }

        private async void OnRegistrarClicked(object sender, EventArgs e)
        {
            // Validações
            if (string.IsNullOrWhiteSpace(txtNome.Text))
            {
                MostrarErro("⚠️ Digite seu nome");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                MostrarErro("⚠️ Digite seu e-mail");
                return;
            }

            if (pickerDepartamento.SelectedIndex == -1)
            {
                MostrarErro("⚠️ Selecione um departamento");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtSenha.Text))
            {
                MostrarErro("⚠️ Digite sua senha");
                return;
            }

            if (txtSenha.Text.Length < 6)
            {
                MostrarErro("⚠️ A senha deve ter no mínimo 6 caracteres");
                return;
            }

            if (txtSenha.Text != txtConfirmaSenha.Text)
            {
                MostrarErro("⚠️ As senhas não coincidem");
                return;
            }

            // Mostrar loading
            btnRegistrar.IsEnabled = false;
            loadingIndicator.IsVisible = true;
            loadingIndicator.IsRunning = true;
            lblMensagem.Text = string.Empty;

            try
            {
                var departamentoSelecionado = _departamentos[pickerDepartamento.SelectedIndex];

                var request = new RegistroRequest
                {
                    Nome = txtNome.Text.Trim(),
                    Email = txtEmail.Text.Trim(),
                    Senha = txtSenha.Text,
                    ConfirmacaoSenha = txtConfirmaSenha.Text,
                    IdDepartamento = departamentoSelecionado.Id
                };

                var response = await _authService.RegistrarAsync(request);

                if (response.Sucesso)
                {
                    await DisplayAlert("✅ Sucesso!", "Conta criada com sucesso! Faça login para continuar.", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    MostrarErro($"❌ {response.Mensagem}");
                }
            }
            catch (Exception ex)
            {
                MostrarErro($"❌ Erro: {ex.Message}");
            }
            finally
            {
                btnRegistrar.IsEnabled = true;
                loadingIndicator.IsVisible = false;
                loadingIndicator.IsRunning = false;
            }
        }

        private async void OnVoltarLoginClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private void MostrarErro(string mensagem)
        {
            lblMensagem.Text = mensagem;
            lblMensagem.TextColor = Color.FromArgb("#dc2626");
        }
    }
}
