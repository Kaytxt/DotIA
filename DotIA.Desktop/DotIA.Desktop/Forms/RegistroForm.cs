// DotIA.Desktop/DotIA.Desktop/Forms/RegistroForm.cs
using System;
using System.Drawing;
using System.Windows.Forms;
using DotIA.Desktop.Services;

namespace DotIA.Desktop.Forms
{
    public partial class RegistroForm : Form
    {
        private readonly ApiClient _apiClient;
        private TextBox txtNome;
        private TextBox txtEmail;
        private TextBox txtSenha;
        private TextBox txtConfirmaSenha;
        private ComboBox cboDepartamento;
        private Button btnRegistrar;
        private Button btnVoltar;
        private Label lblTitulo;
        private List<DepartamentoDTO> departamentos;

        public RegistroForm()
        {
            InitializeComponent();
            _apiClient = new ApiClient();
            CarregarDepartamentos();
        }

        private void InitializeComponent()
        {
            this.Text = "DotIA - Cadastro";
            this.Size = new Size(450, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(26, 19, 47);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // Logo/Título
            lblTitulo = new Label
            {
                Text = "?? Criar Conta",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = Color.FromArgb(162, 55, 240),
                Location = new Point(100, 40),
                Size = new Size(250, 50),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Label e TextBox Nome
            Label lblNome = new Label
            {
                Text = "Nome Completo:",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Location = new Point(50, 120),
                Size = new Size(300, 20)
            };

            txtNome = new TextBox
            {
                Location = new Point(50, 145),
                Size = new Size(350, 30),
                Font = new Font("Segoe UI", 10)
            };

            // Label e TextBox Email
            Label lblEmail = new Label
            {
                Text = "Email:",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Location = new Point(50, 190),
                Size = new Size(300, 20)
            };

            txtEmail = new TextBox
            {
                Location = new Point(50, 215),
                Size = new Size(350, 30),
                Font = new Font("Segoe UI", 10)
            };

            // Label e ComboBox Departamento
            Label lblDepartamento = new Label
            {
                Text = "Departamento:",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Location = new Point(50, 260),
                Size = new Size(300, 20)
            };

            cboDepartamento = new ComboBox
            {
                Location = new Point(50, 285),
                Size = new Size(350, 30),
                Font = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Label e TextBox Senha
            Label lblSenha = new Label
            {
                Text = "Senha:",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Location = new Point(50, 330),
                Size = new Size(300, 20)
            };

            txtSenha = new TextBox
            {
                Location = new Point(50, 355),
                Size = new Size(350, 30),
                Font = new Font("Segoe UI", 10),
                PasswordChar = '?'
            };

            // Label e TextBox Confirmar Senha
            Label lblConfirmaSenha = new Label
            {
                Text = "Confirmar Senha:",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Location = new Point(50, 400),
                Size = new Size(300, 20)
            };

            txtConfirmaSenha = new TextBox
            {
                Location = new Point(50, 425),
                Size = new Size(350, 30),
                Font = new Font("Segoe UI", 10),
                PasswordChar = '?'
            };

            // Botão Registrar
            btnRegistrar = new Button
            {
                Text = "Criar Conta",
                Location = new Point(50, 480),
                Size = new Size(350, 40),
                BackColor = Color.FromArgb(162, 55, 240),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRegistrar.Click += BtnRegistrar_Click;

            // Botão Voltar
            btnVoltar = new Button
            {
                Text = "Voltar ao Login",
                Location = new Point(50, 530),
                Size = new Size(350, 35),
                BackColor = Color.FromArgb(70, 60, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10),
                Cursor = Cursors.Hand
            };
            btnVoltar.Click += (s, e) => {
                var loginForm = new LoginForm();
                loginForm.Show();
                this.Close();
            };

            // Adicionar controles
            this.Controls.Add(lblTitulo);
            this.Controls.Add(lblNome);
            this.Controls.Add(txtNome);
            this.Controls.Add(lblEmail);
            this.Controls.Add(txtEmail);
            this.Controls.Add(lblDepartamento);
            this.Controls.Add(cboDepartamento);
            this.Controls.Add(lblSenha);
            this.Controls.Add(txtSenha);
            this.Controls.Add(lblConfirmaSenha);
            this.Controls.Add(txtConfirmaSenha);
            this.Controls.Add(btnRegistrar);
            this.Controls.Add(btnVoltar);
        }

        private async void CarregarDepartamentos()
        {
            try
            {
                departamentos = await _apiClient.ObterDepartamentosAsync();
                cboDepartamento.Items.Clear();
                cboDepartamento.Items.Add("Selecione um departamento");
                foreach (var dept in departamentos)
                {
                    cboDepartamento.Items.Add(dept.Nome);
                }
                cboDepartamento.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar departamentos: {ex.Message}",
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnRegistrar_Click(object sender, EventArgs e)
        {
            // Validações
            if (string.IsNullOrWhiteSpace(txtNome.Text))
            {
                MessageBox.Show("Por favor, informe seu nome completo.", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNome.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtEmail.Text) || !txtEmail.Text.Contains("@"))
            {
                MessageBox.Show("Por favor, informe um email válido.", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEmail.Focus();
                return;
            }

            if (cboDepartamento.SelectedIndex <= 0)
            {
                MessageBox.Show("Por favor, selecione um departamento.", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cboDepartamento.Focus();
                return;
            }

            if (txtSenha.Text.Length < 6)
            {
                MessageBox.Show("A senha deve ter no mínimo 6 caracteres.", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtSenha.Focus();
                return;
            }

            if (txtSenha.Text != txtConfirmaSenha.Text)
            {
                MessageBox.Show("As senhas não coincidem.", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtConfirmaSenha.Focus();
                return;
            }

            btnRegistrar.Enabled = false;
            btnRegistrar.Text = "Aguarde...";
            this.Cursor = Cursors.WaitCursor;

            try
            {
                var departamentoSelecionado = departamentos[cboDepartamento.SelectedIndex - 1];

                var resposta = await _apiClient.RegistrarAsync(
                    txtNome.Text.Trim(),
                    txtEmail.Text.Trim(),
                    txtSenha.Text,
                    txtConfirmaSenha.Text,
                    departamentoSelecionado.Id
                );

                if (resposta.Sucesso)
                {
                    MessageBox.Show("Cadastro realizado com sucesso! Você já pode fazer login.",
                        "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    var loginForm = new LoginForm();
                    loginForm.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show(resposta.Mensagem ?? "Erro ao realizar cadastro", "Erro",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro de conexão: {ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnRegistrar.Enabled = true;
                btnRegistrar.Text = "Criar Conta";
                this.Cursor = Cursors.Default;
            }
        }
    }
}