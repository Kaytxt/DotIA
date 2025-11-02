using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using DotIA.Desktop.Services;

namespace DotIA.Desktop.Forms
{
    public partial class LoginForm : Form
    {
        private readonly ApiClient _apiClient;

        // Cores (equivalentes às do cshtml)
        private readonly Color PrimaryPurple = ColorTranslator.FromHtml("#8d4bff");
        private readonly Color SecondaryPurple = ColorTranslator.FromHtml("#a855f7");
        private readonly Color DarkBg = ColorTranslator.FromHtml("#1a132f");
        private readonly Color DarkerBg = ColorTranslator.FromHtml("#140e25");
        private readonly Color CardBg = ColorTranslator.FromHtml("#2c204d");

        // Controles
        private Panel card;
        private Label lblTitulo;
        private Label lblSubtitulo;
        private Label lblErro; // para mensagens
        private TextBox txtEmail;
        private TextBox txtSenha;
        private Button btnToggleSenha;
        private CheckBox chkLembrar;
        private Button btnLogin;
        private Button btnRegistro;
        private Label lblDivider;
        private LinkLabel linkTermos;
        private LinkLabel linkPriv;

        public LoginForm()
        {
            InitializeComponent();
            _apiClient = new ApiClient();
        }

        private void InitializeComponent()
        {
            // Form
            Text = "DotIA - Login";
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = DarkBg;
            Size = new Size(520, 720);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            DoubleBuffered = true;
            Paint += (s, e) => PaintBackgroundGlow(e.Graphics);

            // Card
            card = new Panel
            {
                Size = new Size(420, 590),
                BackColor = Color.Transparent
            };
            CenterControl(card);
            card.Paint += Card_Paint; // desenha fundo/borda arredondada

            // Ícone “logo”
            var logo = new Panel
            {
                Size = new Size(100, 100),
                Location = new Point((card.Width - 100) / 2, 26)
            };
            logo.Paint += (s, e) =>
            {
                using var path = Rounded(logo.ClientRectangle, 25);
                using var lg = new LinearGradientBrush(logo.ClientRectangle, PrimaryPurple, SecondaryPurple, 45f);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillPath(lg, path);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using var f = new Font("Segoe UI Emoji", 48, FontStyle.Regular);
                e.Graphics.DrawString("🤖", f, Brushes.White, logo.ClientRectangle, sf);
                // leve sombra
                using var shadow = new SolidBrush(Color.FromArgb(64, PrimaryPurple));
                e.Graphics.FillEllipse(shadow, 8, logo.Height - 8, 60, 10);
            };

            // Título e subtítulo
            lblTitulo = new Label
            {
                Text = "DotIA",
                Font = new Font("Segoe UI", 26f, FontStyle.Bold), // <= aqui
                ForeColor = Color.White,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(card.Width, 48),
                Location = new Point(0, 138)
            };

            lblSubtitulo = new Label
            {
                Text = "Sistema de Suporte Inteligente",
                Font = new Font("Segoe UI", 11, FontStyle.Regular),
                ForeColor = Color.FromArgb(180, 180, 190),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(card.Width, 24),
                Location = new Point(0, 184)
            };

            // Mensagem de erro/sucesso (oculta por padrão)
            lblErro = new Label
            {
                Text = "",
                Visible = false,
                AutoSize = false,
                Size = new Size(card.Width - 60, 50),
                Location = new Point(30, 215),
                ForeColor = Color.FromArgb(255, 210, 210),
                BackColor = Color.FromArgb(40, 239, 68, 68),
                BorderStyle = BorderStyle.None,
                Padding = new Padding(10),
            };

            // Email
            var lblEmail = BuildLabel("Email", new Point(30, 272));
            txtEmail = BuildTextBox(new Point(30, 298), "seu@email.com");
            // adiciona ícone (opcional simples): prefixo via label
            var icoMail = BuildIcon("✉", new Point(38, 306));
            ControlsToCard(icoMail);

            // Senha + botão olho
            var lblSenha = BuildLabel("Senha", new Point(30, 350));
            txtSenha = BuildTextBox(new Point(30, 376), "••••••••");
            txtSenha.PasswordChar = '●';

            btnToggleSenha = new Button
            {
                FlatStyle = FlatStyle.Flat,
                Size = new Size(36, 36),
                Location = new Point(card.Width - 30 - 36, 380),
                Text = "👁",
                Font = new Font("Segoe UI Emoji", 10),
                BackColor = Color.Transparent,
                ForeColor = Color.Silver,
                TabStop = false,
                Cursor = Cursors.Hand
            };
            btnToggleSenha.FlatAppearance.BorderSize = 0;
            btnToggleSenha.Click += (s, e) =>
            {
                if (txtSenha.PasswordChar == '\0')
                {
                    txtSenha.PasswordChar = '●';
                    btnToggleSenha.Text = "👁";
                }
                else
                {
                    txtSenha.PasswordChar = '\0';
                    btnToggleSenha.Text = "🚫";
                }
            };

            // Lembrar-me
            chkLembrar = new CheckBox
            {
                Text = "Lembrar-me",
                ForeColor = Color.FromArgb(200, 200, 210),
                Font = new Font("Segoe UI", 9),
                Location = new Point(30, 424),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            // Botão Entrar (gradiente)
            btnLogin = new Button
            {
                Text = "Entrar  ➜",
                Location = new Point(30, 460),
                Size = new Size(card.Width - 60, 46),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Paint += (s, e) => PaintGradientButton(e.Graphics, btnLogin.ClientRectangle, PrimaryPurple, SecondaryPurple, btnLogin.Text);
            btnLogin.Click += BtnLogin_Click;
            txtSenha.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) { e.Handled = true; BtnLogin_Click(s, e); } };

            // Divider “OU”
            lblDivider = new Label
            {
                Text = "OU",
                AutoSize = false,
                Size = new Size(card.Width - 60, 20),
                Location = new Point(30, 515),
                ForeColor = Color.FromArgb(160, 160, 170),
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblDivider.Paint += (s, e) =>
            {
                var r = lblDivider.ClientRectangle;
                using var p = new Pen(Color.FromArgb(80, PrimaryPurple), 1);
                e.Graphics.DrawLine(p, 0, r.Height / 2, (r.Width / 2) - 18, r.Height / 2);
                e.Graphics.DrawLine(p, (r.Width / 2) + 18, r.Height / 2, r.Width, r.Height / 2);
            };

            // Botão Registro (outline)
            btnRegistro = new Button
            {
                Text = "Criar Nova Conta",
                Location = new Point(30, 542),
                Size = new Size(card.Width - 60, 44),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = PrimaryPurple,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRegistro.FlatAppearance.BorderSize = 2;
            btnRegistro.FlatAppearance.BorderColor = Color.FromArgb(120, PrimaryPurple);
            btnRegistro.Click += BtnRegistro_Click;

            // Footer
            var lblCopy = new Label
            {
                Text = "© 2025 DotIA. Todos os direitos reservados.",
                AutoSize = false,
                Size = new Size(card.Width, 20),
                Location = new Point(0, 590),
                ForeColor = Color.FromArgb(130, 130, 145),
                TextAlign = ContentAlignment.MiddleCenter
            };
            linkTermos = new LinkLabel
            {
                Text = "Termos",
                AutoSize = true,
                LinkColor = SecondaryPurple,
                ActiveLinkColor = PrimaryPurple,
                Location = new Point(card.Left + card.Width / 2 - 50, card.Top + 612),
                BackColor = Color.Transparent
            };
            linkPriv = new LinkLabel
            {
                Text = "Privacidade",
                AutoSize = true,
                LinkColor = SecondaryPurple,
                ActiveLinkColor = PrimaryPurple,
                Location = new Point(card.Left + card.Width / 2 + 10, card.Top + 612),
                BackColor = Color.Transparent
            };

            // Adiciona no card
            ControlsToCard(
                logo, lblTitulo, lblSubtitulo, lblErro,
                lblEmail, txtEmail,
                lblSenha, txtSenha, btnToggleSenha,
                chkLembrar, btnLogin, lblDivider, btnRegistro, lblCopy
            );

            // Adiciona no form
            Controls.Add(card);
            Controls.Add(linkTermos);
            Controls.Add(linkPriv);
            Resize += (s, e) =>
            {
                CenterControl(card);
                linkTermos.Location = new Point(card.Left + card.Width / 2 - 50, card.Bottom + 22);
                linkPriv.Location = new Point(card.Left + card.Width / 2 + 10, card.Bottom + 22);
            };
        }

        // Helpers de UI ---------------------------------------------------

        private Label BuildLabel(string text, Point location)
        {
            return new Label
            {
                Text = text,
                ForeColor = Color.FromArgb(230, 230, 235),
                // antes: FontStyle.Medium
                Font = new Font("Segoe UI", 10f, FontStyle.Regular), // ou "Segoe UI Semibold", 10f, Regular
                AutoSize = true,
                Location = location,
                BackColor = Color.Transparent
            };
        }


        private TextBox BuildTextBox(Point location, string placeholder)
        {
            var tb = new TextBox
            {
                Location = location,
                Size = new Size(card.Width - 60, 34),
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White,
                BackColor = DarkerBg,
                BorderStyle = BorderStyle.FixedSingle
            };

            // PlaceholderText existe em .NET 6+
#if NET6_0_OR_GREATER
            tb.PlaceholderText = placeholder;
#endif

            tb.GotFocus += (s, e) => tb.BackColor = DarkerBg;
            tb.LostFocus += (s, e) => tb.BackColor = DarkerBg;

            // borda roxa ao focar
            tb.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(90, PrimaryPurple), 2);
                var r = new Rectangle(0, 0, tb.Width - 1, tb.Height - 1);
                e.Graphics.DrawRectangle(pen, r);
            };
            return tb;
        }

        private Label BuildIcon(string text, Point location)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Location = location,
                ForeColor = Color.Silver,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI Emoji", 12, FontStyle.Regular)
            };
        }

        private void ControlsToCard(params Control[] c)
        {
            foreach (var ctrl in c) card.Controls.Add(ctrl);
        }

        private static GraphicsPath Rounded(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void Card_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, card.Width, card.Height - 20);
            using var path = Rounded(r, 28);

            // fundo em gradiente simulando vidro
            using var lg = new LinearGradientBrush(r, CardBg, DarkerBg, 135f);
            e.Graphics.FillPath(lg, path);

            // borda
            using var pen = new Pen(Color.FromArgb(120, PrimaryPurple), 1.6f);
            e.Graphics.DrawPath(pen, path);

            // sombra
            using var shadow = new SolidBrush(Color.FromArgb(80, 0, 0, 0));
            var sr = new Rectangle(12, r.Bottom - 6, r.Width - 24, 10);
            e.Graphics.FillEllipse(shadow, sr);
        }

        private void PaintGradientButton(Graphics g, Rectangle r, Color c1, Color c2, string text)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = Rounded(new Rectangle(0, 0, r.Width, r.Height), 14);
            using var lg = new LinearGradientBrush(r, c1, c2, 135f);
            g.FillPath(lg, path);
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var f = new Font("Segoe UI", 11, FontStyle.Bold);
            g.DrawString(text, f, Brushes.White, r, sf);
        }

        private void PaintBackgroundGlow(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            // “glows” fixos (radiais) no fundo – simples e leve
            using var b1 = new PathGradientBrush(new[] {
                new Point(0, Height/2), new Point(Width/3, 0), new Point(Width/3, Height)
            })
            { CenterColor = Color.FromArgb(60, PrimaryPurple), SurroundColors = new[] { Color.Transparent, Color.Transparent, Color.Transparent } };

            using var b2 = new PathGradientBrush(new[] {
                new Point(Width, Height), new Point(Width*2/3, Height/2), new Point(Width, Height/3)
            })
            { CenterColor = Color.FromArgb(50, SecondaryPurple), SurroundColors = new[] { Color.Transparent, Color.Transparent, Color.Transparent } };

            g.FillRectangle(new SolidBrush(DarkBg), ClientRectangle);
            g.FillRectangle(b1, ClientRectangle);
            g.FillRectangle(b2, ClientRectangle);
        }

        private void CenterControl(Control c)
        {
            c.Location = new Point((ClientSize.Width - c.Width) / 2, (ClientSize.Height - c.Height) / 2 - 20);
        }

        // Navegação -------------------------------------------------------

        private void BtnRegistro_Click(object sender, EventArgs e)
        {
            var registroForm = new RegistroForm();
            registroForm.FormClosed += (s, a) => this.Show();
            registroForm.Show();
            this.Hide();
        }

        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string senha = txtSenha.Text;

            if (string.IsNullOrEmpty(email))
            {
                ShowError("Por favor, informe o email.");
                txtEmail.Focus();
                return;
            }
            if (string.IsNullOrEmpty(senha))
            {
                ShowError("Por favor, informe a senha.");
                txtSenha.Focus();
                return;
            }

            btnLogin.Enabled = false;
            btnLogin.Text = "Aguarde…";
            Cursor = Cursors.WaitCursor;

            try
            {
                var resposta = await _apiClient.LoginAsync(email, senha);

                if (resposta.Sucesso)
                {
                    lblErro.Visible = false;
                    MessageBox.Show($"Bem-vindo, {resposta.Nome}!", "Sucesso",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    if (resposta.TipoUsuario == "Solicitante")
                    {
                        var chatForm = new ChatForm(resposta.UsuarioId, resposta.Nome);
                        chatForm.FormClosed += (s2, a2) => this.Show();
                        chatForm.Show();
                        Hide();
                    }
                    else if (resposta.TipoUsuario == "Tecnico")
                    {
                        var tecnicoForm = new TecnicoForm(resposta.UsuarioId, resposta.Nome);
                        tecnicoForm.FormClosed += (s2, a2) => this.Show();
                        tecnicoForm.Show();
                        Hide();
                    }
                    else if (resposta.TipoUsuario == "Gerente")
                    {
                        var gerenteForm = new GerenteForm(resposta.UsuarioId, resposta.Nome);
                        gerenteForm.FormClosed += (s2, a2) => this.Show();
                        gerenteForm.Show();
                        Hide();
                    }
                }
                else
                {
                    ShowError(resposta.Mensagem ?? "Erro ao fazer login.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Erro de conexão: {ex.Message}\n\nVerifique se a API está rodando.");
            }
            finally
            {
                btnLogin.Enabled = true;
                btnLogin.Text = "Entrar  ➜";
                Cursor = Cursors.Default;
            }
        }

        private void ShowError(string msg)
        {
            lblErro.Text = "  ⚠  " + msg;
            lblErro.Visible = true;
        }
    }
}
