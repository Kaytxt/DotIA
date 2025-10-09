using System;
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
        private Label lblMensagem;

        public LoginForm()
        {
            InitializeComponent();
            _apiClient = new ApiClient();
        }

        private void InitializeComponent()
        {
            this.Text = "DotIA - Login";
            this.Size = new System.Drawing.Size(400, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = System.Drawing.Color.FromArgb(26, 19, 47);

            // Label Email
            Label lblEmail = new Label
            {
                Text = "Email:",
                Location = new System.Drawing.Point(50, 50),
                Size = new System.Drawing.Size(300, 20),
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold)
            };

            // TextBox Email
            txtEmail = new TextBox
            {
                Location = new System.Drawing.Point(50, 75),
                Size = new System.Drawing.Size(300, 25),
                Font = new System.Drawing.Font("Segoe UI", 10)
            };

            // Label Senha
            Label lblSenha = new Label
            {
                Text = "Senha:",
                Location = new System.Drawing.Point(50, 110),
                Size = new System.Drawing.Size(300, 20),
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold)
            };

            // TextBox Senha
            txtSenha = new TextBox
            {
                Location = new System.Drawing.Point(50, 135),
                Size = new System.Drawing.Size(300, 25),
                Font = new System.Drawing.Font("Segoe UI", 10),
                UseSystemPasswordChar = true
            };

            // Botão Login
            btnLogin = new Button
            {
                Text = "LOGIN",
                Location = new System.Drawing.Point(125, 180),
                Size = new System.Drawing.Size(150, 40),
                BackColor = System.Drawing.Color.FromArgb(122, 31, 201),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 12, System.Drawing.FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLogin.Click += BtnLogin_Click;

            // Label Mensagem
            lblMensagem = new Label
            {
                Location = new System.Drawing.Point(50, 230),
                Size = new System.Drawing.Size(300, 20),
                ForeColor = System.Drawing.Color.Red,
                Font = new System.Drawing.Font("Segoe UI", 9),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Visible = false
            };

            this.Controls.Add(lblEmail);
            this.Controls.Add(txtEmail);
            this.Controls.Add(lblSenha);
            this.Controls.Add(txtSenha);
            this.Controls.Add(btnLogin);
            this.Controls.Add(lblMensagem);
        }

        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string senha = txtSenha.Text;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(senha))
            {
                MostrarMensagem("Preencha todos os campos!");
                return;
            }

            btnLogin.Enabled = false;
            btnLogin.Text = "Aguarde...";

            var resultado = await _apiClient.LoginAsync(email, senha);

            btnLogin.Enabled = true;
            btnLogin.Text = "LOGIN";

            if (resultado.Sucesso)
            {
                if (resultado.TipoUsuario == "Solicitante")
                {
                    var chatForm = new ChatForm(resultado.UsuarioId, resultado.Nome);
                    chatForm.Show();
                    this.Hide();
                }
                else if (resultado.TipoUsuario == "Tecnico")
                {
                    var tecnicoForm = new TecnicoForm();
                    tecnicoForm.Show();
                    this.Hide();
                }
            }
            else
            {
                MostrarMensagem(resultado.Mensagem ?? "Email ou senha inválidos");
            }
        }

        private void MostrarMensagem(string mensagem)
        {
            lblMensagem.Text = mensagem;
            lblMensagem.Visible = true;
        }
    }
}