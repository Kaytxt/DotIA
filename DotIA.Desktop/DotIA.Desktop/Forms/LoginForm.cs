using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.IO;
using DotIA.Desktop.Services;
using DotIA.Desktop.Controls;

namespace DotIA.Desktop.Forms
{
    public partial class LoginForm : Form
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
        private readonly Color BorderColor = Color.FromArgb(77, 141, 75, 255); // rgba(141, 75, 255, 0.3)

        // Animação
        private System.Windows.Forms.Timer pulseTimer;
        private float pulseScale = 1.0f;
        private bool pulseGrowing = true;

        // Controles
        private Panel mainCard;
        private PictureBox logoPicture;
        private Label lblTitulo;
        private Label lblSubtitulo;
        private Label lblEmailLabel, lblSenhaLabel;
        private RoundedTextBox txtEmail, txtSenha;
        private Button btnToggleSenha;
        private CheckBox chkLembrar;
        private RoundedButton btnEntrar;
        private Label lblOu;
        private Button btnCriarConta;
        private Label lblFooter;
        private Label lblErro;
        private Panel loadingOverlay;

        public LoginForm()
        {
            InitializeComponent();
            _apiClient = new ApiClient();
            IniciarAnimacoes();
        }

        private void InitializeComponent()
        {
            // Form - FULLSCREEN
            Text = "DotIA - Login";
            WindowState = FormWindowState.Maximized;
            FormBorderStyle = FormBorderStyle.Sizable;
            BackColor = DarkBg;
            DoubleBuffered = true;
            Font = new Font("Segoe UI", 10f);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(600, 500);
            AutoScroll = true;

            // Background com glow (pintado no OnPaint)
            Paint += LoginForm_Paint;

            // Card centralizado
            mainCard = new Panel
            {
                Size = new Size(480, 780),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.None
            };
            mainCard.Paint += MainCard_Paint;
            Controls.Add(mainCard);

            // Centralizar card no meio da tela
            Resize += (s, e) => CenterCard();
            Load += (s, e) => CenterCard();

            int yPos = 30;

            // Logo (PNG com animação pulse)
            logoPicture = new PictureBox
            {
                Size = new Size(180, 180),
                Location = new Point((mainCard.Width - 180) / 2, yPos),
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

            yPos += 195;

            // Título "DotIA" com gradiente
            lblTitulo = new Label
            {
                Text = "DotIA",
                Font = new Font("Segoe UI", 42f, FontStyle.Bold),
                AutoSize = false,
                Size = new Size(480, 60),
                Location = new Point(0, yPos),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            lblTitulo.Paint += LblTitulo_Paint;
            mainCard.Controls.Add(lblTitulo);

            yPos += 65;

            // Subtítulo
            lblSubtitulo = new Label
            {
                Text = "Sistema de Suporte Inteligente",
                Font = new Font("Segoe UI", 16f),
                ForeColor = TextGray,
                AutoSize = false,
                Size = new Size(480, 30),
                Location = new Point(0, yPos),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            mainCard.Controls.Add(lblSubtitulo);

            yPos += 50;

            // Label de erro (acima dos campos)
            lblErro = new Label
            {
                AutoSize = false,
                Size = new Size(420, 40),
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

            // Email
            lblEmailLabel = new Label
            {
                Text = "✉ Email",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(229, 231, 235),
                AutoSize = true,
                Location = new Point(30, yPos),
                BackColor = Color.Transparent
            };
            mainCard.Controls.Add(lblEmailLabel);

            yPos += 30;

            txtEmail = new RoundedTextBox
            {
                Location = new Point(30, yPos),
                Size = new Size(420, 50),
                PlaceholderText = "seu@email.com",
                BorderRadius = 15
            };
            mainCard.Controls.Add(txtEmail);

            yPos += 70;

            // Senha
            lblSenhaLabel = new Label
            {
                Text = "🔒 Senha",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(229, 231, 235),
                AutoSize = true,
                Location = new Point(30, yPos),
                BackColor = Color.Transparent
            };
            mainCard.Controls.Add(lblSenhaLabel);

            yPos += 30;

            txtSenha = new RoundedTextBox
            {
                Location = new Point(30, yPos),
                Size = new Size(370, 50),
                UseSystemPasswordChar = true,
                BorderRadius = 15
            };
            mainCard.Controls.Add(txtSenha);

            btnToggleSenha = new Button
            {
                Text = "👁",
                Size = new Size(50, 50),
                Location = new Point(400, yPos),
                FlatStyle = FlatStyle.Flat,
                BackColor = DarkerBg,
                ForeColor = TextGray,
                Font = new Font("Segoe UI", 18f),
                Cursor = Cursors.Hand
            };
            btnToggleSenha.FlatAppearance.BorderSize = 2;
            btnToggleSenha.FlatAppearance.BorderColor = BorderColor;
            btnToggleSenha.Click += BtnToggleSenha_Click;
            btnToggleSenha.Paint += BtnToggleSenha_Paint;
            mainCard.Controls.Add(btnToggleSenha);

            yPos += 65;

            // Checkbox Lembrar
            chkLembrar = new CheckBox
            {
                Text = "Lembrar-me",
                Font = new Font("Segoe UI", 10f),
                ForeColor = TextGray,
                AutoSize = true,
                Location = new Point(30, yPos),
                BackColor = Color.Transparent
            };
            mainCard.Controls.Add(chkLembrar);

            yPos += 35;

            // Botão Entrar
            btnEntrar = new RoundedButton
            {
                Text = "ENTRAR →",
                Size = new Size(420, 52),
                Location = new Point(30, yPos),
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                BorderRadius = 15
            };
            btnEntrar.Click += BtnEntrar_Click;
            mainCard.Controls.Add(btnEntrar);

            yPos += 65;

            // Divider "OU"
            lblOu = new Label
            {
                Text = "OU",
                Font = new Font("Segoe UI", 11f),
                ForeColor = TextGray,
                AutoSize = false,
                Size = new Size(420, 30),
                Location = new Point(30, yPos),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            lblOu.Paint += LblOu_Paint;
            mainCard.Controls.Add(lblOu);

            yPos += 40;

            // Botão Criar Conta
            btnCriarConta = new Button
            {
                Text = "👤 CRIAR NOVA CONTA",
                Size = new Size(420, 52),
                Location = new Point(30, yPos),
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(229, 231, 235),
                Cursor = Cursors.Hand
            };
            btnCriarConta.FlatAppearance.BorderSize = 0;
            btnCriarConta.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnCriarConta.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnCriarConta.Paint += BtnCriarConta_Paint;
            btnCriarConta.Click += BtnCriarConta_Click;
            mainCard.Controls.Add(btnCriarConta);

            yPos += 65;

            // Footer
            lblFooter = new Label
            {
                Text = "© 2025 DotIA. Todos os direitos reservados.\nTermos • Privacidade",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(107, 114, 128),
                AutoSize = false,
                Size = new Size(420, 40),
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
                Text = "Carregando...",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                Size = new Size(480, 50),
                Location = new Point(0, 320),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            loadingOverlay.Controls.Add(lblLoading);
            mainCard.Controls.Add(loadingOverlay);

            // Enter key
            txtEmail.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) { e.Handled = true; txtSenha.Focus(); } };
            txtSenha.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) { e.Handled = true; BtnEntrar_Click(null, null); } };
        }

        private void CenterCard()
        {
            int x = Math.Max(10, (ClientSize.Width - mainCard.Width) / 2);
            int y = Math.Max(10, (ClientSize.Height - mainCard.Height) / 2);
            mainCard.Location = new Point(x, y);
        }

        private void LoginForm_Paint(object sender, PaintEventArgs e)
        {
            // Background gradients (animated glows)
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
                // Gradiente do card
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    mainCard.ClientRectangle, CardBg, DarkerBg, 135f))
                {
                    e.Graphics.FillPath(brush, path);
                }

                // Borda com glow
                using (Pen borderPen = new Pen(BorderColor, 2))
                {
                    e.Graphics.DrawPath(borderPen, path);
                }

                // Sombra
                using (GraphicsPath shadowPath = GetRoundedRect(new Rectangle(5, 5, mainCard.Width, mainCard.Height), 30))
                {
                    using (PathGradientBrush shadowBrush = new PathGradientBrush(shadowPath))
                    {
                        shadowBrush.CenterColor = Color.FromArgb(50, 0, 0, 0);
                        shadowBrush.SurroundColors = new[] { Color.Transparent };
                        e.Graphics.FillPath(shadowBrush, shadowPath);
                    }
                }
            }
        }

        private void LogoPicture_Paint(object sender, PaintEventArgs e)
        {
            // Drop shadow na logo
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath shadowPath = new GraphicsPath())
            {
                shadowPath.AddEllipse(new Rectangle(30, 30, 120, 120));
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

            // Gradiente roxo → rosa
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
                // Background vermelho com gradiente
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    lblErro.ClientRectangle,
                    Color.FromArgb(50, 239, 68, 68),
                    Color.FromArgb(50, 220, 38, 38), 135f))
                {
                    e.Graphics.FillPath(brush, path);
                }

                // Borda vermelha
                using (Pen borderPen = new Pen(Color.FromArgb(128, 239, 68, 68), 1))
                {
                    e.Graphics.DrawPath(borderPen, path);
                }
            }
        }

        private void LblOu_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            int lineY = lblOu.Height / 2;
            using (Pen linePen = new Pen(BorderColor, 1))
            {
                e.Graphics.DrawLine(linePen, 0, lineY, 150, lineY);
                e.Graphics.DrawLine(linePen, 270, lineY, 420, lineY);
            }
        }

        private void BtnToggleSenha_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = GetRoundedRect(btnToggleSenha.ClientRectangle, 15))
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
            TextRenderer.DrawText(e.Graphics, btnToggleSenha.Text, btnToggleSenha.Font,
                btnToggleSenha.ClientRectangle, btnToggleSenha.ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private void BtnCriarConta_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = GetRoundedRect(btnCriarConta.ClientRectangle, 15))
            {
                // Background transparente
                using (SolidBrush brush = new SolidBrush(Color.Transparent))
                {
                    e.Graphics.FillPath(brush, path);
                }

                // Borda roxa
                using (Pen borderPen = new Pen(BorderColor, 2))
                {
                    e.Graphics.DrawPath(borderPen, path);
                }
            }

            // Texto
            TextRenderer.DrawText(e.Graphics, btnCriarConta.Text, btnCriarConta.Font,
                btnCriarConta.ClientRectangle, Color.FromArgb(229, 231, 235),
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
                    int newSize = (int)(180 * pulseScale);
                    logoPicture.Size = new Size(newSize, newSize);
                    logoPicture.Location = new Point((mainCard.Width - newSize) / 2, 30);
                }
            };
            pulseTimer.Start();
        }

        private void BtnToggleSenha_Click(object sender, EventArgs e)
        {
            txtSenha.UseSystemPasswordChar = !txtSenha.UseSystemPasswordChar;
            btnToggleSenha.Text = txtSenha.UseSystemPasswordChar ? "👁" : "🙈";
        }

        private async void BtnEntrar_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string senha = txtSenha.Text;

            if (string.IsNullOrEmpty(email))
            {
                MostrarErro("⚠ Por favor, informe seu e-mail");
                return;
            }

            if (string.IsNullOrEmpty(senha))
            {
                MostrarErro("⚠ Por favor, informe sua senha");
                return;
            }

            loadingOverlay.Visible = true;
            loadingOverlay.BringToFront();
            lblErro.Visible = false;

            try
            {
                var response = await _apiClient.LoginAsync(email, senha);

                loadingOverlay.Visible = false;

                if (response.Sucesso)
                {
                    Form proximoForm = null;

                    switch (response.TipoUsuario?.ToUpper())
                    {
                        case "SOLICITANTE":
                            proximoForm = new ChatForm(response.UsuarioId, response.Nome);
                            break;
                        case "TECNICO":
                            proximoForm = new TecnicoForm(response.UsuarioId, response.Nome);
                            break;
                        case "GERENTE":
                            proximoForm = new GerenteForm(response.UsuarioId, response.Nome);
                            break;
                        default:
                            MostrarErro("⚠ Tipo de usuário inválido");
                            return;
                    }

                    Hide();
                    proximoForm.FormClosed += (s, args) => Close();
                    proximoForm.Show();
                }
                else
                {
                    MostrarErro("⚠ " + (response.Mensagem ?? "E-mail ou senha incorretos"));
                }
            }
            catch (Exception ex)
            {
                loadingOverlay.Visible = false;
                MostrarErro($"⚠ Erro ao conectar: {ex.Message}");
            }
        }

        private void BtnCriarConta_Click(object sender, EventArgs e)
        {
            var registroForm = new RegistroForm();
            registroForm.FormClosed += (s, args) => Show();
            Hide();
            registroForm.Show();
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
    }
}
