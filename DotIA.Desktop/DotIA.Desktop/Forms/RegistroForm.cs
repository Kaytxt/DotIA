using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using DotIA.Desktop.Services;

namespace DotIA.Desktop.Forms
{
    public partial class RegistroForm : Form // ? CORRIGIDO: Nome da classe estava errado
    {
        private readonly ApiClient _apiClient;

        // Cores - Exatamente iguais ao Web
        private readonly Color PrimaryPurple = ColorTranslator.FromHtml("#8d4bff");
        private readonly Color SecondaryPurple = ColorTranslator.FromHtml("#a855f7");
        private readonly Color DarkBg = ColorTranslator.FromHtml("#1a132f");
        private readonly Color DarkerBg = ColorTranslator.FromHtml("#140e25");
        private readonly Color CardBg = ColorTranslator.FromHtml("#2c204d");

        // Controles
        private Panel scrollPanel;
        private Panel card;
        private Label lblTitulo;
        private Label lblSubtitulo;
        private Label lblErro;
        private RoundedTextBox txtNome;
        private RoundedTextBox txtEmail;
        private RoundedComboBox cboDepartamento;
        private RoundedTextBox txtSenha;
        private RoundedTextBox txtConfirmacaoSenha;
        private Button btnToggleSenha;
        private Button btnToggleConfirmacao;
        private RoundedButton btnCadastrar;
        private RoundedButton btnVoltar;
        private Panel strengthBar;
        private Label lblStrength;

        public RegistroForm()
        {
            InitializeComponent();
            _apiClient = new ApiClient();
            CarregarDepartamentosAsync();
        }

        private void InitializeComponent()
        {
            Text = "DotIA - Cadastro";
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = DarkBg;
            Size = new Size(1400, 900);
            FormBorderStyle = FormBorderStyle.Sizable;
            WindowState = FormWindowState.Maximized;
            MaximizeBox = true;
            DoubleBuffered = true;
            Paint += (s, e) => PaintBackgroundGlow(e.Graphics);

            // Scroll Panel
            scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent
            };
            Controls.Add(scrollPanel);

            // Card principal
            card = new Panel
            {
                Size = new Size(500, 750),
                BackColor = Color.Transparent,
                Location = new Point(40, 30)
            };
            card.Paint += Card_Paint;
            scrollPanel.Controls.Add(card);

            // Logo
            var logo = new Panel
            {
                Size = new Size(80, 80),
                Location = new Point((card.Width - 80) / 2, 25)
            };
            logo.Paint += Logo_Paint;

            // Título
            lblTitulo = new Label
            {
                Text = "Criar Conta",
                Font = new Font("Segoe UI", 36f, FontStyle.Bold), // ? Ajustado de 28 para 36 (igual ao web)
                ForeColor = Color.White,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(card.Width, 45),
                Location = new Point(0, 115)
            };

            lblSubtitulo = new Label
            {
                Text = "Preencha os dados abaixo para se cadastrar",
                Font = new Font("Segoe UI", 15, FontStyle.Regular), // ? Ajustado de 10 para 15
                ForeColor = Color.FromArgb(156, 163, 175),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(card.Width, 26),
                Location = new Point(0, 165)
            };

            // Mensagem de erro
            lblErro = new Label
            {
                Text = "",
                Visible = false,
                AutoSize = false,
                Size = new Size(card.Width - 80, 60),
                Location = new Point(40, 195),
                ForeColor = Color.FromArgb(254, 202, 202),
                BackColor = Color.FromArgb(40, 239, 68, 68),
                Padding = new Padding(15),
                Font = new Font("Segoe UI", 9, FontStyle.Regular)
            };
            lblErro.Paint += (s, e) => PaintRoundedPanel(e.Graphics, lblErro.ClientRectangle, Color.FromArgb(40, 239, 68, 68), 15);

            // Nome
            var lblNome = BuildLabel("??  Nome Completo", new Point(40, 265));
            txtNome = new RoundedTextBox
            {
                Location = new Point(40, 295),
                Size = new Size(card.Width - 80, 44),
                PlaceholderText = "Digite seu nome completo",
                BackColor = DarkerBg,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11)
            };

            // Email
            var lblEmail = BuildLabel("??  Email", new Point(40, 355));
            txtEmail = new RoundedTextBox
            {
                Location = new Point(40, 385),
                Size = new Size(card.Width - 80, 44),
                PlaceholderText = "seu@email.com",
                BackColor = DarkerBg,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11)
            };

            // Departamento
            var lblDept = BuildLabel("??  Departamento", new Point(40, 445));
            cboDepartamento = new RoundedComboBox
            {
                Location = new Point(40, 475),
                Size = new Size(card.Width - 80, 44),
                BackColor = DarkerBg,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11)
            };

            // Senha
            var lblSenha = BuildLabel("??  Senha", new Point(40, 535));
            txtSenha = new RoundedTextBox
            {
                Location = new Point(40, 565),
                Size = new Size(card.Width - 80, 44),
                PlaceholderText = "Mínimo 6 caracteres",
                PasswordChar = '?',
                BackColor = DarkerBg,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11)
            };
            txtSenha.TextChanged += TxtSenha_TextChanged;

            btnToggleSenha = CreateToggleButton(new Point(card.Width - 90, 573));
            btnToggleSenha.Click += (s, e) => TogglePassword(txtSenha, btnToggleSenha);

            // Barra de força da senha
            strengthBar = new Panel
            {
                Location = new Point(40, 615),
                Size = new Size(card.Width - 80, 6),
                BackColor = Color.FromArgb(40, 255, 255, 255)
            };

            lblStrength = new Label
            {
                Location = new Point(40, 625),
                AutoSize = true,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Silver
            };

            // Confirmação de Senha
            var lblConfirm = BuildLabel("??  Confirmar Senha", new Point(40, 655));
            txtConfirmacaoSenha = new RoundedTextBox
            {
                Location = new Point(40, 685),
                Size = new Size(card.Width - 80, 44),
                PlaceholderText = "Digite a senha novamente",
                PasswordChar = '?',
                BackColor = DarkerBg,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11)
            };
            txtConfirmacaoSenha.TextChanged += TxtConfirmacaoSenha_TextChanged;

            btnToggleConfirmacao = CreateToggleButton(new Point(card.Width - 90, 693));
            btnToggleConfirmacao.Click += (s, e) => TogglePassword(txtConfirmacaoSenha, btnToggleConfirmacao);

            // Botão Cadastrar (gradiente)
            btnCadastrar = new RoundedButton
            {
                Text = "CRIAR CONTA  ?", // ? Uppercase como no web
                Location = new Point(40, 750),
                Size = new Size(card.Width - 80, 50),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White
            };
            btnCadastrar.Click += BtnCadastrar_Click;

            // Botão Voltar (outline)
            btnVoltar = new RoundedButton
            {
                Text = "?  VOLTAR PARA O LOGIN", // ? Uppercase e ícone ajustado
                Location = new Point(40, 810),
                Size = new Size(card.Width - 80, 50), // ? Altura ajustada de 48 para 50
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White, // ? Cor ajustada
                IsOutline = true
            };
            btnVoltar.Click += (s, e) => this.Close();

            // Adiciona controles ao card
            card.Controls.Add(logo);
            card.Controls.Add(lblTitulo);
            card.Controls.Add(lblSubtitulo);
            card.Controls.Add(lblErro);
            card.Controls.Add(lblNome);
            card.Controls.Add(txtNome);
            card.Controls.Add(lblEmail);
            card.Controls.Add(txtEmail);
            card.Controls.Add(lblDept);
            card.Controls.Add(cboDepartamento);
            card.Controls.Add(lblSenha);
            card.Controls.Add(txtSenha);
            card.Controls.Add(btnToggleSenha);
            card.Controls.Add(strengthBar);
            card.Controls.Add(lblStrength);
            card.Controls.Add(lblConfirm);
            card.Controls.Add(txtConfirmacaoSenha);
            card.Controls.Add(btnToggleConfirmacao);
            card.Controls.Add(btnCadastrar);
            card.Controls.Add(btnVoltar);
        }

        private Button CreateToggleButton(Point location)
        {
            var btn = new Button
            {
                Size = new Size(36, 36),
                Location = location,
                Text = "??",
                Font = new Font("Segoe UI Emoji", 12),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(156, 163, 175),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                TabStop = false
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private async void CarregarDepartamentosAsync()
        {
            try
            {
                var departamentos = await _apiClient.ObterDepartamentosAsync();
                cboDepartamento.Items.Clear();
                cboDepartamento.Items.Add("Selecione seu departamento");
                cboDepartamento.SelectedIndex = 0;

                foreach (var dept in departamentos)
                {
                    cboDepartamento.Items.Add(dept);
                    cboDepartamento.DisplayMember = "Nome";
                    cboDepartamento.ValueMember = "Id";
                }
            }
            catch { }
        }

        private void TxtSenha_TextChanged(object sender, EventArgs e)
        {
            VerificarForcaSenha();
        }

        private void TxtConfirmacaoSenha_TextChanged(object sender, EventArgs e)
        {
            if (txtConfirmacaoSenha.Text.Length > 0 && txtSenha.Text != txtConfirmacaoSenha.Text)
            {
                lblStrength.Text = "? As senhas não coincidem";
                lblStrength.ForeColor = Color.FromArgb(239, 68, 68);
            }
            else if (txtConfirmacaoSenha.Text.Length > 0)
            {
                lblStrength.Text = "? As senhas coincidem";
                lblStrength.ForeColor = Color.FromArgb(16, 185, 129);
            }
        }

        private void VerificarForcaSenha()
        {
            string senha = txtSenha.Text;
            int forca = 0;

            if (senha.Length >= 6) forca++;
            if (senha.Length >= 8) forca++;
            if (System.Text.RegularExpressions.Regex.IsMatch(senha, "[A-Z]")) forca++;
            if (System.Text.RegularExpressions.Regex.IsMatch(senha, "[0-9]")) forca++;
            if (System.Text.RegularExpressions.Regex.IsMatch(senha, "[^A-Za-z0-9]")) forca++;

            strengthBar.Controls.Clear();
            var bar = new Panel { Dock = DockStyle.Left, Height = 6 };
            bar.Paint += (s, e) =>
            {
                using var path = RoundedRect(bar.ClientRectangle, 10);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var br = new SolidBrush(bar.BackColor);
                e.Graphics.FillPath(br, path);
            };

            if (forca <= 2)
            {
                bar.Width = (strengthBar.Width / 3);
                bar.BackColor = Color.FromArgb(239, 68, 68);
                lblStrength.Text = "Senha fraca";
                lblStrength.ForeColor = Color.FromArgb(239, 68, 68);
            }
            else if (forca <= 3)
            {
                bar.Width = (strengthBar.Width * 2 / 3);
                bar.BackColor = Color.FromArgb(251, 191, 36);
                lblStrength.Text = "Senha média";
                lblStrength.ForeColor = Color.FromArgb(251, 191, 36);
            }
            else
            {
                bar.Width = strengthBar.Width;
                bar.BackColor = Color.FromArgb(16, 185, 129);
                lblStrength.Text = "Senha forte";
                lblStrength.ForeColor = Color.FromArgb(16, 185, 129);
            }

            strengthBar.Controls.Add(bar);
        }

        private async void BtnCadastrar_Click(object sender, EventArgs e)
        {
            string nome = txtNome.Text.Trim();
            string email = txtEmail.Text.Trim();
            string senha = txtSenha.Text;
            string confirmacao = txtConfirmacaoSenha.Text;

            if (string.IsNullOrEmpty(nome) || nome.Length < 3)
            {
                ShowError("Por favor, informe seu nome completo.");
                return;
            }

            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
            {
                ShowError("Por favor, informe um email válido.");
                return;
            }

            if (cboDepartamento.SelectedIndex <= 0)
            {
                ShowError("Por favor, selecione um departamento.");
                return;
            }

            if (string.IsNullOrEmpty(senha) || senha.Length < 6)
            {
                ShowError("A senha deve ter no mínimo 6 caracteres.");
                return;
            }

            if (senha != confirmacao)
            {
                ShowError("As senhas não coincidem.");
                return;
            }

            btnCadastrar.Enabled = false;
            btnCadastrar.Text = "Criando conta...";

            try
            {
                var dept = (DepartamentoDTO)cboDepartamento.SelectedItem;
                var resposta = await _apiClient.RegistrarAsync(nome, email, senha, confirmacao, dept.Id);

                if (resposta.Sucesso)
                {
                    MessageBox.Show("? Cadastro realizado com sucesso!\n\nVocê já pode fazer login.",
                        "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
                else
                {
                    ShowError(resposta.Mensagem ?? "Erro ao realizar cadastro.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Erro de conexão: {ex.Message}");
            }
            finally
            {
                btnCadastrar.Enabled = true;
                btnCadastrar.Text = "Criar Conta  ?";
            }
        }

        // Helpers de UI
        private Label BuildLabel(string text, Point location)
        {
            return new Label
            {
                Text = text,
                ForeColor = Color.FromArgb(229, 231, 235),
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                AutoSize = true,
                Location = location,
                BackColor = Color.Transparent
            };
        }

        private void TogglePassword(RoundedTextBox tb, Button btn)
        {
            if (tb.PasswordChar == '\0')
            {
                tb.PasswordChar = '?';
                btn.Text = "??";
            }
            else
            {
                tb.PasswordChar = '\0';
                btn.Text = "??";
            }
        }

        private void ShowError(string msg)
        {
            lblErro.Text = "  ?  " + msg;
            lblErro.Visible = true;
        }

        // Métodos de pintura
        private void Logo_Paint(object sender, PaintEventArgs e)
        {
            var p = (Panel)sender;
            using var path = RoundedRect(p.ClientRectangle, 20);
            using var lg = new LinearGradientBrush(p.ClientRectangle, PrimaryPurple, SecondaryPurple, 45f);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.FillPath(lg, path);

            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var f = new Font("Segoe UI Emoji", 40, FontStyle.Regular);
            e.Graphics.DrawString("??", f, Brushes.White, p.ClientRectangle, sf);

            // Sombra
            using var shadow = new SolidBrush(Color.FromArgb(80, 0, 0, 0));
            var shadowRect = new Rectangle(10, p.Height - 5, p.Width - 20, 8);
            using var shadowPath = new GraphicsPath();
            shadowPath.AddEllipse(shadowRect);
            e.Graphics.FillPath(shadow, shadowPath);
        }

        private void Card_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = card.ClientRectangle;
            r.Height -= 30;

            using var path = RoundedRect(r, 30);
            using var lg = new LinearGradientBrush(r, CardBg, DarkerBg, 135f);
            e.Graphics.FillPath(lg, path);

            // Borda com brilho
            using var pen = new Pen(Color.FromArgb(100, PrimaryPurple), 2f);
            e.Graphics.DrawPath(pen, path);

            // Sombra do card
            using var shadow = new SolidBrush(Color.FromArgb(60, 0, 0, 0));
            var shadowRect = new Rectangle(r.X + 10, r.Bottom + 5, r.Width - 20, 20);
            using var shadowPath = new GraphicsPath();
            shadowPath.AddEllipse(shadowRect);
            e.Graphics.FillPath(shadow, shadowPath);
        }

        private void PaintBackgroundGlow(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Glow 1
            using var b1 = new PathGradientBrush(new[] {
                new Point(0, Height/2),
                new Point(Width/3, 0),
                new Point(Width/3, Height)
            });
            b1.CenterColor = Color.FromArgb(50, PrimaryPurple);
            b1.SurroundColors = new[] { Color.Transparent, Color.Transparent, Color.Transparent };

            // Glow 2
            using var b2 = new PathGradientBrush(new[] {
                new Point(Width, Height),
                new Point(Width*2/3, Height/2),
                new Point(Width, Height/3)
            });
            b2.CenterColor = Color.FromArgb(40, SecondaryPurple);
            b2.SurroundColors = new[] { Color.Transparent, Color.Transparent, Color.Transparent };

            g.FillRectangle(new SolidBrush(DarkBg), ClientRectangle);
            g.FillRectangle(b1, ClientRectangle);
            g.FillRectangle(b2, ClientRectangle);
        }

        private void PaintRoundedPanel(Graphics g, Rectangle rect, Color color, int radius)
        {
            using var path = RoundedRect(rect, radius);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var br = new SolidBrush(color);
            g.FillPath(br, path);
        }

        private GraphicsPath RoundedRect(Rectangle r, int radius)
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
    }

    // Controles customizados arredondados
    public class RoundedTextBox : TextBox
    {
        public string PlaceholderText { get; set; }

        public RoundedTextBox()
        {
            BorderStyle = BorderStyle.None;
            Padding = new Padding(15, 12, 15, 12);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            DrawRoundedBorder(e.Graphics);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == 0xF || m.Msg == 0x133)
            {
                using var g = CreateGraphics();
                DrawRoundedBorder(g);
            }
        }

        private void DrawRoundedBorder(Graphics g)
        {
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = RoundedRect(rect, 15);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using var bg = new SolidBrush(BackColor);
            g.FillPath(bg, path);

            var borderColor = Focused
                ? Color.FromArgb(141, 75, 255)
                : Color.FromArgb(61, 46, 107);
            using var pen = new Pen(borderColor, 2);
            g.DrawPath(pen, path);
        }

        private GraphicsPath RoundedRect(Rectangle r, int radius)
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
    }

    public class RoundedComboBox : ComboBox
    {
        public RoundedComboBox()
        {
            DrawMode = DrawMode.OwnerDrawFixed;
            DropDownStyle = ComboBoxStyle.DropDownList;
            FlatStyle = FlatStyle.Flat;
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            e.DrawBackground();
            using var br = new SolidBrush(e.ForeColor);
            e.Graphics.DrawString(GetItemText(Items[e.Index]), e.Font, br, e.Bounds);
            e.DrawFocusRectangle();
        }
    }

    public class RoundedButton : Button
    {
        public bool IsOutline { get; set; }

        public RoundedButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = RoundedRect(rect, 15);

            if (IsOutline)
            {
                using var br = new SolidBrush(BackColor);
                e.Graphics.FillPath(br, path);
                using var pen = new Pen(Color.FromArgb(100, 141, 75, 255), 2);
                e.Graphics.DrawPath(pen, path);
            }
            else
            {
                var c1 = ColorTranslator.FromHtml("#8d4bff");
                var c2 = ColorTranslator.FromHtml("#a855f7");
                using var lg = new LinearGradientBrush(rect, c1, c2, 135f);
                e.Graphics.FillPath(lg, path);
            }

            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(Text, Font, new SolidBrush(ForeColor), ClientRectangle, sf);
        }

        private GraphicsPath RoundedRect(Rectangle r, int radius)
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
    }
}