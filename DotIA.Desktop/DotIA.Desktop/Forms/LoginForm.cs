using System;
using System.Drawing;
using System.Windows.Forms;
using DotIA.Desktop.Services;

namespace DotIA.Desktop.Forms
{
    public partial class LoginForm : Form
    {
        private readonly ApiClient _apiClient;
        private TextBox txtEmail;
        private TextBox txtSenha;
        private Button btnLogin;
        private Label lblTitulo;
        private Label lblEmail;
        private Label lblSenha;
        private CheckBox chkMostrarSenha;

        public LoginForm()
        {
            InitializeComponent();
            _apiClient = new ApiClient();
        }

        private void InitializeComponent()
        {
            this.Text = "DotIA - Login";
            this.Size = new Size(400, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(26, 19, 47);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // Logo/Título
            lblTitulo = new Label
            {
                Text = "🤖 DotIA",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = Color.FromArgb(162, 55, 240),
                Location = new Point(120, 50),
                Size = new Size(160, 50),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Label Email
            lblEmail = new Label
            {
                Text = "Email:",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Location = new Point(50, 140),
                Size = new Size(300, 20)
            };

            // TextBox Email
            txtEmail = new TextBox
            {
                Location = new Point(50, 165),
                Size = new Size(300, 30),
                Font = new Font("Segoe UI", 10),
                Text = "teste@teste.com" // Valor padrão para testes
            };

            // Label Senha
            lblSenha = new Label
            {
                Text = "Senha:",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Location = new Point(50, 210),
                Size = new Size(300, 20)
            };

            // TextBox Senha
            txtSenha = new TextBox
            {
                Location = new Point(50, 235),
                Size = new Size(300, 30),
                Font = new Font("Segoe UI", 10),
                PasswordChar = '●',
                Text = "123456" // Valor padrão para testes
            };
            txtSenha.KeyPress += TxtSenha_KeyPress;

            // CheckBox Mostrar Senha
            chkMostrarSenha = new CheckBox
            {
                Text = "Mostrar senha",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9),
                Location = new Point(50, 270),
                Size = new Size(120, 20)
            };
            chkMostrarSenha.CheckedChanged += ChkMostrarSenha_CheckedChanged;

            // Botão Login
            btnLogin = new Button
            {
                Text = "Entrar",
                Location = new Point(50, 320),
                Size = new Size(300, 45),
                BackColor = Color.FromArgb(162, 55, 240),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += BtnLogin_Click;

            // Adicionar controles
            this.Controls.Add(lblTitulo);
            this.Controls.Add(lblEmail);
            this.Controls.Add(txtEmail);
            this.Controls.Add(lblSenha);
            this.Controls.Add(txtSenha);
            this.Controls.Add(chkMostrarSenha);
            this.Controls.Add(btnLogin);
        }

        private void ChkMostrarSenha_CheckedChanged(object sender, EventArgs e)
        {
            txtSenha.PasswordChar = chkMostrarSenha.Checked ? '\0' : '●';
        }

        private void TxtSenha_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                BtnLogin_Click(sender, e);
            }
        }

        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string senha = txtSenha.Text;

            // Validações
            if (string.IsNullOrEmpty(email))
            {
                MessageBox.Show("Por favor, informe o email.", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEmail.Focus();
                return;
            }

            if (string.IsNullOrEmpty(senha))
            {
                MessageBox.Show("Por favor, informe a senha.", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtSenha.Focus();
                return;
            }

            // Desabilitar botão durante requisição
            btnLogin.Enabled = false;
            btnLogin.Text = "Aguarde...";
            this.Cursor = Cursors.WaitCursor;

            try
            {
                var resposta = await _apiClient.LoginAsync(email, senha);

                if (resposta.Sucesso)
                {
                    MessageBox.Show($"Bem-vindo, {resposta.Nome}!", "Sucesso",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Abrir tela correspondente
                    if (resposta.TipoUsuario == "Solicitante")
                    {
                        var chatForm = new ChatForm(resposta.UsuarioId, resposta.Nome);
                        chatForm.FormClosed += (s, args) => this.Show();
                        chatForm.Show();
                        this.Hide();
                    }
                    else if (resposta.TipoUsuario == "Tecnico")
                    {
                        MessageBox.Show("Tela de técnico em desenvolvimento.", "Info");
                        // var tecnicoForm = new TecnicoForm(resposta.UsuarioId, resposta.Nome);
                        // tecnicoForm.Show();
                        // this.Hide();
                    }
                }
                else
                {
                    MessageBox.Show(resposta.Mensagem ?? "Erro ao fazer login", "Erro",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro de conexão: {ex.Message}\n\nVerifique se a API está rodando.",
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnLogin.Enabled = true;
                btnLogin.Text = "Entrar";
                this.Cursor = Cursors.Default;
            }
        }
    }
}