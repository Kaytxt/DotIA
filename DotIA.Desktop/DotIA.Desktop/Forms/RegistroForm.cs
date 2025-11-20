using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.IO;
using DotIA.Desktop.Services;
using DotIA.Desktop.Controls;

namespace DotIA.Desktop.Forms
{
    public partial class RegistroForm : Form
    {
        private readonly ApiClient _apiClient;

        // Cores (exatas da web)
        private readonly Color PrimaryPurple = ColorTranslator.FromHtml("#8d4bff");
        private readonly Color SecondaryPurple = ColorTranslator.FromHtml("#a855f7");
        private readonly Color PinkAccent = ColorTranslator.FromHtml("#ec4899");
        private readonly Color DarkBg = ColorTranslator.FromHtml("#1a132f");
        private readonly Color DarkerBg = ColorTranslator.FromHtml("#140e25");
        private readonly Color CardBg = ColorTranslator.FromHtml("#2c204d");
        private readonly Color TextGray = ColorTranslator.FromHtml("#9ca3af");
        private readonly Color BorderColor = Color.FromArgb(77, 141, 75, 255);

        // Animação
        private System.Windows.Forms.Timer pulseTimer;
        private float pulseScale = 1.0f;
        private bool pulseGrowing = true;

        // Controles
        private Panel scrollPanel;
        private Panel mainCard;
        private PictureBox logoPicture;
        private Label lblTitulo;
        private Label lblSubtitulo;
        private Label lblNomeLabel, lblEmailLabel, lblDeptLabel, lblSenhaLabel, lblConfirmLabel;
        private RoundedTextBox txtNome, txtEmail, txtSenha, txtConfirmacaoSenha;
        private RoundedComboBox cboDepartamento;
        private Button btnToggleSenha, btnToggleConfirmacao;
        private Panel strengthContainer;
        private Panel strengthBar;
        private Label lblStrengthText;
        private Label lblSenhasDiferentes;
        private RoundedButton btnCadastrar;
        private Button btnVoltar;
        private Label lblFooter;
        private Label lblErro;
        private Panel loadingOverlay;

        public RegistroForm()
        {
            InitializeComponent();
            _apiClient = new ApiClient();
            IniciarAnimacoes();
            CarregarDepartamentosAsync();
        }

        private void InitializeComponent()
        {
            // Form - FULLSCREEN
            Text = "DotIA - Cadastro";
            WindowState = FormWindowState.Maximized;
            FormBorderStyle = FormBorderStyle.Sizable;
            BackColor = DarkBg;
            DoubleBuffered = true;
            Font = new Font("Segoe UI", 10f);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(800, 600);

            Paint += RegistroForm_Paint;

            // Scroll Panel (para conter o card)
            scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent
            };
            Controls.Add(scrollPanel);

            // Card centralizado (550px width máximo como na web)
            mainCard = new Panel
            {
                Size = new Size(550, 960),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.None
            };
            mainCard.Paint += MainCard_Paint;
            scrollPanel.Controls.Add(mainCard);

            Resize += (s, e) => CenterCard();
            Load += (s, e) => CenterCard();

            int yPos = 30;

            // Logo (PNG)
            logoPicture = new PictureBox
            {
                Size = new Size(120, 120),
                Location = new Point((mainCard.Width - 120) / 2, yPos),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };

            // Carregar logo PNG
            string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "dotia-logo.png");
            if (File.Exists(logoPath))
            {
                logoPicture.Image = Image.FromFile(logoPath);
            }

            logoPicture.Paint += LogoPicture_Paint;
            mainCard.Controls.Add(logoPicture);

            yPos += 135;

            // Título "Criar Conta"
            lblTitulo = new Label
            {
                Text = "Criar Conta",
                Font = new Font("Segoe UI", 36f, FontStyle.Bold),
                AutoSize = false,
                Size = new Size(550, 50),
                Location = new Point(0, yPos),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            lblTitulo.Paint += LblTitulo_Paint;
            mainCard.Controls.Add(lblTitulo);

            yPos += 55;

            // Subtítulo
            lblSubtitulo = new Label
            {
                Text = "Preencha os dados abaixo para se cadastrar",
                Font = new Font("Segoe UI", 15f),
                ForeColor = TextGray,
                AutoSize = false,
                Size = new Size(550, 30),
                Location = new Point(0, yPos),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            mainCard.Controls.Add(lblSubtitulo);

            yPos += 50;

            // Label de erro
            lblErro = new Label
            {
                AutoSize = false,
                Size = new Size(490, 40),
                Location = new Point(30, yPos),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(254, 202, 202),
                BackColor = Color.Transparent,
                Visible = false,
                Padding = new Padding(15, 10, 15, 10)
            };
            lblErro.Paint += LblErro_Paint;
            mainCard.Controls.Add(lblErro);

            yPos += 50;

            // Nome Completo
            lblNomeLabel = CreateLabel("👤 Nome Completo", new Point(30, yPos));
            mainCard.Controls.Add(lblNomeLabel);
            yPos += 30;

            txtNome = new RoundedTextBox
            {
                Location = new Point(30, yPos),
                Size = new Size(490, 48),
                PlaceholderText = "Digite seu nome completo",
                BorderRadius = 15
            };
            mainCard.Controls.Add(txtNome);
            yPos += 63;

            // Email
            lblEmailLabel = CreateLabel("✉ Email", new Point(30, yPos));
            mainCard.Controls.Add(lblEmailLabel);
            yPos += 30;

            txtEmail = new RoundedTextBox
            {
                Location = new Point(30, yPos),
                Size = new Size(490, 48),
                PlaceholderText = "seu@email.com",
                BorderRadius = 15
            };
            mainCard.Controls.Add(txtEmail);
            yPos += 63;

            // Departamento
            lblDeptLabel = CreateLabel("🏢 Departamento", new Point(30, yPos));
            mainCard.Controls.Add(lblDeptLabel);
            yPos += 30;

            cboDepartamento = new RoundedComboBox
            {
                Location = new Point(30, yPos),
                Size = new Size(490, 48),
                BorderRadius = 15
            };
            mainCard.Controls.Add(cboDepartamento);
            yPos += 63;

            // Senha
            lblSenhaLabel = CreateLabel("🔒 Senha", new Point(30, yPos));
            mainCard.Controls.Add(lblSenhaLabel);
            yPos += 30;

            txtSenha = new RoundedTextBox
            {
                Location = new Point(30, yPos),
                Size = new Size(440, 48),
                UseSystemPasswordChar = true,
                BorderRadius = 15
            };
            txtSenha.TextChanged += TxtSenha_TextChanged;
            mainCard.Controls.Add(txtSenha);

            btnToggleSenha = CreateToggleButton(new Point(470, yPos));
            btnToggleSenha.Click += (s, e) => TogglePassword(txtSenha, btnToggleSenha);
            mainCard.Controls.Add(btnToggleSenha);

            yPos += 56;

            // Strength bar
            strengthContainer = new Panel
            {
                Size = new Size(490, 4),
                Location = new Point(30, yPos),
                BackColor = Color.FromArgb(40, 255, 255, 255)
            };
            mainCard.Controls.Add(strengthContainer);

            strengthBar = new Panel
            {
                Size = new Size(0, 4),
                Location = new Point(0, 0),
                BackColor = Color.Red
            };
            strengthBar.Paint += StrengthBar_Paint;
            strengthContainer.Controls.Add(strengthBar);

            yPos += 8;

            lblStrengthText = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9f),
                ForeColor = TextGray,
                AutoSize = true,
                Location = new Point(30, yPos),
                BackColor = Color.Transparent
            };
            mainCard.Controls.Add(lblStrengthText);

            yPos += 30;

            // Confirmar Senha
            lblConfirmLabel = CreateLabel("🔐 Confirmar Senha", new Point(30, yPos));
            mainCard.Controls.Add(lblConfirmLabel);
            yPos += 30;

            txtConfirmacaoSenha = new RoundedTextBox
            {
                Location = new Point(30, yPos),
                Size = new Size(440, 48),
                UseSystemPasswordChar = true,
                BorderRadius = 15
            };
            txtConfirmacaoSenha.TextChanged += TxtConfirmacaoSenha_TextChanged;
            mainCard.Controls.Add(txtConfirmacaoSenha);

            btnToggleConfirmacao = CreateToggleButton(new Point(470, yPos));
            btnToggleConfirmacao.Click += (s, e) => TogglePassword(txtConfirmacaoSenha, btnToggleConfirmacao);
            mainCard.Controls.Add(btnToggleConfirmacao);

            yPos += 56;

            // Aviso senhas diferentes
            lblSenhasDiferentes = new Label
            {
                Text = "⚠ As senhas não coincidem",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(239, 68, 68),
                AutoSize = true,
                Location = new Point(30, yPos),
                BackColor = Color.Transparent,
                Visible = false
            };
            mainCard.Controls.Add(lblSenhasDiferentes);

            yPos += 35;

            // Botão Cadastrar
            btnCadastrar = new RoundedButton
            {
                Text = "CRIAR CONTA ✓",
                Size = new Size(490, 50),
                Location = new Point(30, yPos),
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                BorderRadius = 15
            };
            btnCadastrar.Click += BtnCadastrar_Click;
            mainCard.Controls.Add(btnCadastrar);

            yPos += 63;

            // Botão Voltar
            btnVoltar = new Button
            {
                Text = "← VOLTAR PARA O LOGIN",
                Size = new Size(490, 50),
                Location = new Point(30, yPos),
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(229, 231, 235),
                Cursor = Cursors.Hand
            };
            btnVoltar.FlatAppearance.BorderSize = 0;
            btnVoltar.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnVoltar.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnVoltar.Paint += BtnVoltar_Paint;
            btnVoltar.Click += BtnVoltar_Click;
            mainCard.Controls.Add(btnVoltar);

            yPos += 30;

            // Footer
            lblFooter = new Label
            {
                Text = "© 2025 DotIA. Todos os direitos reservados.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(107, 114, 128),
                AutoSize = false,
                Size = new Size(490, 25),
                Location = new Point(30, yPos),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            mainCard.Controls.Add(lblFooter);

            // Loading overlay
            loadingOverlay = new Panel
            {
                Size = mainCard.Size,
                Location = new Point(0, 0),
                BackColor = Color.FromArgb(200, CardBg.R, CardBg.G, CardBg.B),
                Visible = false
            };
            var lblLoading = new Label
            {
                Text = "Criando conta...",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                Size = new Size(550, 50),
                Location = new Point(0, 400),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            loadingOverlay.Controls.Add(lblLoading);
            mainCard.Controls.Add(loadingOverlay);

            // Enter key navigation
            txtNome.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) { e.Handled = true; txtEmail.Focus(); } };
            txtEmail.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) { e.Handled = true; cboDepartamento.Focus(); } };
            txtSenha.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) { e.Handled = true; txtConfirmacaoSenha.Focus(); } };
            txtConfirmacaoSenha.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) { e.Handled = true; BtnCadastrar_Click(null, null); } };
        }

        private Label CreateLabel(string text, Point location)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(229, 231, 235),
                AutoSize = true,
                Location = location,
                BackColor = Color.Transparent
            };
        }

        private Button CreateToggleButton(Point location)
        {
            var btn = new Button
            {
                Text = "👁",
                Size = new Size(50, 48),
                Location = location,
                FlatStyle = FlatStyle.Flat,
                BackColor = DarkerBg,
                ForeColor = TextGray,
                Font = new Font("Segoe UI", 16f),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 2;
            btn.FlatAppearance.BorderColor = BorderColor;
            btn.Paint += BtnToggle_Paint;
            return btn;
        }

        private void CenterCard()
        {
            int x = (scrollPanel.ClientSize.Width - mainCard.Width) / 2;
            int y = Math.Max(20, (scrollPanel.ClientSize.Height - mainCard.Height) / 2);
            mainCard.Location = new Point(x, y);
        }

        private void RegistroForm_Paint(object sender, PaintEventArgs e)
        {
            // Background glows
            using (GraphicsPath path1 = new GraphicsPath())
            {
                path1.AddEllipse(new Rectangle(-200, ClientSize.Height / 3, 800, 800));
                using (PathGradientBrush brush1 = new PathGradientBrush(path1))
                {
                    brush1.CenterColor = Color.FromArgb(38, PrimaryPurple);
                    brush1.SurroundColors = new[] { Color.Transparent };
                    e.Graphics.FillPath(brush1, path1);
                }
            }

            using (GraphicsPath path2 = new GraphicsPath())
            {
                path2.AddEllipse(new Rectangle(ClientSize.Width - 600, -200, 800, 800));
                using (PathGradientBrush brush2 = new PathGradientBrush(path2))
                {
                    brush2.CenterColor = Color.FromArgb(38, SecondaryPurple);
                    brush2.SurroundColors = new[] { Color.Transparent };
                    e.Graphics.FillPath(brush2, path2);
                }
            }
        }

        private void MainCard_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = GetRoundedRect(mainCard.ClientRectangle, 30))
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    mainCard.ClientRectangle, CardBg, DarkerBg, 135f))
                {
                    e.Graphics.FillPath(brush, path);
                }

                using (Pen borderPen = new Pen(BorderColor, 2))
                {
                    e.Graphics.DrawPath(borderPen, path);
                }
            }
        }

        private void LogoPicture_Paint(object sender, PaintEventArgs e)
        {
            // Drop shadow na logo
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath shadowPath = new GraphicsPath())
            {
                shadowPath.AddEllipse(new Rectangle(20, 20, 80, 80));
                using (PathGradientBrush shadowBrush = new PathGradientBrush(shadowPath))
                {
                    shadowBrush.CenterColor = Color.FromArgb(100, PrimaryPurple);
                    shadowBrush.SurroundColors = new[] { Color.Transparent };
                    e.Graphics.FillPath(shadowBrush, shadowPath);
                }
            }
        }

        private void LblTitulo_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            using (LinearGradientBrush brush = new LinearGradientBrush(
                lblTitulo.ClientRectangle, SecondaryPurple, PinkAccent, 135f))
            {
                using (StringFormat sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                })
                {
                    e.Graphics.DrawString(lblTitulo.Text, lblTitulo.Font, brush, lblTitulo.ClientRectangle, sf);
                }
            }
        }

        private void LblErro_Paint(object sender, PaintEventArgs e)
        {
            if (!lblErro.Visible) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = GetRoundedRect(lblErro.ClientRectangle, 15))
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    lblErro.ClientRectangle,
                    Color.FromArgb(50, 239, 68, 68),
                    Color.FromArgb(50, 220, 38, 38), 135f))
                {
                    e.Graphics.FillPath(brush, path);
                }

                using (Pen borderPen = new Pen(Color.FromArgb(128, 239, 68, 68), 1))
                {
                    e.Graphics.DrawPath(borderPen, path);
                }
            }
        }

        private void BtnToggle_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Button btn = (Button)sender;
            using (GraphicsPath path = GetRoundedRect(btn.ClientRectangle, 15))
            {
                using (SolidBrush brush = new SolidBrush(DarkerBg))
                {
                    e.Graphics.FillPath(brush, path);
                }

                using (Pen borderPen = new Pen(BorderColor, 2))
                {
                    e.Graphics.DrawPath(borderPen, path);
                }
            }

            // Desenha o emoji
            TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font,
                btn.ClientRectangle, btn.ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private void StrengthBar_Paint(object sender, PaintEventArgs e)
        {
            if (strengthBar.Width == 0) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = GetRoundedRect(strengthBar.ClientRectangle, 10))
            {
                using (SolidBrush brush = new SolidBrush(strengthBar.BackColor))
                {
                    e.Graphics.FillPath(brush, path);
                }
            }
        }

        private void BtnVoltar_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = GetRoundedRect(btnVoltar.ClientRectangle, 15))
            {
                using (Pen borderPen = new Pen(BorderColor, 2))
                {
                    e.Graphics.DrawPath(borderPen, path);
                }
            }

            TextRenderer.DrawText(e.Graphics, btnVoltar.Text, btnVoltar.Font,
                btnVoltar.ClientRectangle, Color.FromArgb(229, 231, 235),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private GraphicsPath GetRoundedRect(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;

            rect.Inflate(-1, -1);

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        private void IniciarAnimacoes()
        {
            pulseTimer = new System.Windows.Forms.Timer { Interval = 30 };
            pulseTimer.Tick += (s, e) =>
            {
                if (pulseGrowing)
                {
                    pulseScale += 0.002f;
                    if (pulseScale >= 1.05f) pulseGrowing = false;
                }
                else
                {
                    pulseScale -= 0.002f;
                    if (pulseScale <= 1.0f) pulseGrowing = true;
                }

                if (logoPicture != null)
                {
                    int newSize = (int)(120 * pulseScale);
                    logoPicture.Size = new Size(newSize, newSize);
                    logoPicture.Location = new Point((mainCard.Width - newSize) / 2, 30);
                }
            };
            pulseTimer.Start();
        }

        private void TogglePassword(RoundedTextBox textBox, Button button)
        {
            textBox.UseSystemPasswordChar = !textBox.UseSystemPasswordChar;
            button.Text = textBox.UseSystemPasswordChar ? "👁" : "🙈";
        }

        private void TxtSenha_TextChanged(object sender, EventArgs e)
        {
            string senha = txtSenha.Text;
            int forca = 0;

            if (senha.Length >= 6) forca++;
            if (senha.Length >= 8) forca++;
            if (Regex.IsMatch(senha, @"[A-Z]")) forca++;
            if (Regex.IsMatch(senha, @"[0-9]")) forca++;
            if (Regex.IsMatch(senha, @"[^A-Za-z0-9]")) forca++;

            if (forca <= 2)
            {
                strengthBar.Width = (int)(490 * 0.33);
                strengthBar.BackColor = Color.FromArgb(239, 68, 68);
                lblStrengthText.Text = "Senha fraca";
                lblStrengthText.ForeColor = Color.FromArgb(239, 68, 68);
            }
            else if (forca <= 3)
            {
                strengthBar.Width = (int)(490 * 0.66);
                strengthBar.BackColor = Color.FromArgb(251, 191, 36);
                lblStrengthText.Text = "Senha média";
                lblStrengthText.ForeColor = Color.FromArgb(251, 191, 36);
            }
            else
            {
                strengthBar.Width = 490;
                strengthBar.BackColor = Color.FromArgb(16, 185, 129);
                lblStrengthText.Text = "Senha forte";
                lblStrengthText.ForeColor = Color.FromArgb(16, 185, 129);
            }

            strengthBar.Invalidate();
        }

        private void TxtConfirmacaoSenha_TextChanged(object sender, EventArgs e)
        {
            string senha = txtSenha.Text;
            string confirmacao = txtConfirmacaoSenha.Text;

            if (confirmacao.Length > 0 && senha != confirmacao)
            {
                lblSenhasDiferentes.Visible = true;
                btnCadastrar.Enabled = false;
            }
            else
            {
                lblSenhasDiferentes.Visible = false;
                btnCadastrar.Enabled = true;
            }
        }

        private async void CarregarDepartamentosAsync()
        {
            try
            {
                var departamentos = await _apiClient.ObterDepartamentosAsync();

                cboDepartamento.Items.Clear();
                cboDepartamento.Items.Add(new DepartamentoItem { Id = 0, Nome = "Selecione seu departamento" });

                foreach (var dept in departamentos)
                {
                    cboDepartamento.Items.Add(new DepartamentoItem
                    {
                        Id = dept.Id,
                        Nome = dept.Nome
                    });
                }

                if (cboDepartamento.Items.Count > 0)
                    cboDepartamento.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MostrarErro($"⚠ Erro ao carregar departamentos: {ex.Message}");
            }
        }

        private async void BtnCadastrar_Click(object sender, EventArgs e)
        {
            string nome = txtNome.Text.Trim();
            string email = txtEmail.Text.Trim();
            string senha = txtSenha.Text;
            string confirmacao = txtConfirmacaoSenha.Text;

            if (string.IsNullOrEmpty(nome))
            {
                MostrarErro("⚠ Por favor, informe seu nome completo");
                return;
            }

            if (string.IsNullOrEmpty(email) || !IsValidEmail(email))
            {
                MostrarErro("⚠ Por favor, informe um e-mail válido");
                return;
            }

            if (cboDepartamento.SelectedIndex <= 0)
            {
                MostrarErro("⚠ Por favor, selecione um departamento");
                return;
            }

            if (string.IsNullOrEmpty(senha) || senha.Length < 6)
            {
                MostrarErro("⚠ A senha deve ter no mínimo 6 caracteres");
                return;
            }

            if (senha != confirmacao)
            {
                MostrarErro("⚠ As senhas não coincidem");
                return;
            }

            loadingOverlay.Visible = true;
            loadingOverlay.BringToFront();
            lblErro.Visible = false;

            try
            {
                var deptItem = (DepartamentoItem)cboDepartamento.SelectedItem;
                var response = await _apiClient.RegistrarAsync(nome, email, senha, confirmacao, deptItem.Id);

                loadingOverlay.Visible = false;

                if (response.Sucesso)
                {
                    MessageBox.Show("✓ Conta criada com sucesso! Faça login para continuar.",
                        "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Close();
                }
                else
                {
                    MostrarErro("⚠ " + (response.Mensagem ?? "Erro ao criar conta"));
                }
            }
            catch (Exception ex)
            {
                loadingOverlay.Visible = false;
                MostrarErro($"⚠ Erro ao conectar: {ex.Message}");
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void BtnVoltar_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void MostrarErro(string mensagem)
        {
            lblErro.Text = mensagem;
            lblErro.Visible = true;
            lblErro.BringToFront();

            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 5000 };
            timer.Tick += (s, e) =>
            {
                lblErro.Visible = false;
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                pulseTimer?.Stop();
                pulseTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        private class DepartamentoItem
        {
            public int Id { get; set; }
            public string Nome { get; set; }

            public override string ToString() => Nome;
        }
    }
}
