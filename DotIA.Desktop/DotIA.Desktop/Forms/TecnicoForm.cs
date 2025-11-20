using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using DotIA.Desktop.Services;
using WinTimer = System.Windows.Forms.Timer;

namespace DotIA.Desktop.Forms
{
    public partial class TecnicoForm : Form
    {
        private readonly ApiClient _apiClient;
        private readonly int _usuarioId;
        private readonly string _nomeUsuario;

        // Imagens
        private Image _logoImage;
        private Image _botAvatarImage;
        private Image _tecnicoAvatarImage;
        private Image _userAvatarImage;

        // Cores (idênticas ao .cshtml)
        private readonly Color PrimaryGreen = ColorTranslator.FromHtml("#10b981");
        private readonly Color SecondaryGreen = ColorTranslator.FromHtml("#059669");
        private readonly Color PrimaryPurple = ColorTranslator.FromHtml("#8d4bff");
        private readonly Color SecondaryPurple = ColorTranslator.FromHtml("#a855f7");
        private readonly Color DarkBg = ColorTranslator.FromHtml("#1a132f");
        private readonly Color DarkerBg = ColorTranslator.FromHtml("#1e1433");
        private readonly Color CardBg = ColorTranslator.FromHtml("#2c204d");
        private readonly Color BorderColor = ColorTranslator.FromHtml("#3d2e6b");
        private readonly Color TextColor = ColorTranslator.FromHtml("#e5e7eb");

        // Estado
        private TicketDTO _ticketSelecionado = null;
        private List<TicketDTO> _ticketsCache = new List<TicketDTO>();
        private string _ultimoHashSolucao = string.Empty;

        // Timers
        private readonly WinTimer _timerRefresh = new WinTimer();
        private readonly WinTimer _timerPolling = new WinTimer();
        private readonly WinTimer _timerAnimacoes = new WinTimer();

        // Animações
        private float _pulseScale = 1.0f;
        private bool _pulseGrowing = true;

        // Layout
        private TableLayoutPanel workTable;
        private Panel sidebar;
        private Panel sidebarHeader;
        private PictureBox logoIcon;
        private Label lblLogoText;
        private Button btnRefresh;
        private Panel liveIndicator;
        private Label lblLive;
        private Panel statsCard;
        private Label lblTotalTickets;
        private Label lblResolvidosHoje;
        private FlowLayoutPanel ticketsList;
        private Panel workArea;
        private Panel workHeader;
        private Label lblTicketTitle;
        private Panel conversationPanel;
        private FlowLayoutPanel messagesList;
        private Panel solutionArea;
        private Label lblSuccess;
        private Panel emptyState;
        private TextBox txtSolucao;
        private Button btnResponder;
        private Button btnResolver;
        private Button btnSair;

        public TecnicoForm(int usuarioId, string nomeUsuario)
        {
            _usuarioId = usuarioId;
            _nomeUsuario = nomeUsuario;
            _apiClient = new ApiClient();

            CarregarImagens();
            InitializeComponent();
            MontarLayout();
            ConfigurarTimers();
            CarregarTicketsAsync();
        }

        private void CarregarImagens()
        {
            string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");

            string logoPath = Path.Combine(basePath, "dotia-logo.png");
            string botPath = Path.Combine(basePath, "icon-bot.png");
            string userPath = Path.Combine(basePath, "icon-user.png");
            string tecPath = Path.Combine(basePath, "icon-tecnico.png");

            if (File.Exists(logoPath))
                _logoImage = Image.FromFile(logoPath);

            if (File.Exists(botPath))
                _botAvatarImage = Image.FromFile(botPath);

            if (File.Exists(userPath))
                _userAvatarImage = Image.FromFile(userPath);

            if (File.Exists(tecPath))
                _tecnicoAvatarImage = Image.FromFile(tecPath);
        }

        private void InitializeComponent()
        {
            Text = "DotIA - Painel do Técnico";
            WindowState = FormWindowState.Maximized;
            BackColor = DarkBg;
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(1200, 700);
            Font = new Font("Segoe UI", 10f);
        }

        private void MontarLayout()
        {
            SuspendLayout();

            // ✅ TABLElayoutpanel - DIVISÃO FIXA E CONFIÁVEL (não SplitContainer)
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = DarkBg,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
                Padding = new Padding(0)
            };

            // ✅ Coluna 1: 420px FIXO (sidebar) - REDUZIDO
            // ✅ Coluna 2: 100% do resto (chat)
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 420F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Controls.Add(mainLayout);

            // ===== SIDEBAR (COLUNA 0) =====
            sidebar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = DarkBg,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            sidebar.Paint += Sidebar_Paint;
            mainLayout.Controls.Add(sidebar, 0, 0);

            int yPos = 0;

            // Header com logo
            sidebarHeader = new Panel
            {
                Location = new Point(0, yPos),
                Size = new Size(420, 80), // ✅ 420px
                BackColor = Color.Transparent,
                Padding = new Padding(20, 16, 20, 8)
            };
            sidebar.Controls.Add(sidebarHeader);
            yPos += 80;

            logoIcon = new PictureBox
            {
                Size = new Size(45, 45),
                Location = new Point(20, 18),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Image = _logoImage
            };
            logoIcon.Paint += LogoIcon_Paint;
            sidebarHeader.Controls.Add(logoIcon);

            lblLogoText = new Label
            {
                Text = "DotIA Tech",
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(75, 23),
                BackColor = Color.Transparent
            };
            lblLogoText.Paint += LblLogoText_Paint;
            sidebarHeader.Controls.Add(lblLogoText);

            // Botão Refresh
            btnRefresh = new Button
            {
                Text = "⟳  Atualizar",
                Location = new Point(15, yPos),
                Size = new Size(390, 48), // ✅ 390px (420 - 30 margens)
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = PrimaryGreen,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnRefresh.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnRefresh.Paint += BtnRefresh_Paint;
            btnRefresh.Click += async (s, e) =>
            {
                btnRefresh.Enabled = false;
                await CarregarTicketsAsync();
                btnRefresh.Enabled = true;
                Notificar("✅ Atualizado!");
            };
            sidebar.Controls.Add(btnRefresh);
            yPos += 58;

            // Live indicator
            liveIndicator = new Panel
            {
                Location = new Point(15, yPos),
                Size = new Size(390, 36), // ✅ 390px
                BackColor = Color.Transparent
            };
            liveIndicator.Paint += LiveIndicator_Paint;

            lblLive = new Label
            {
                Text = "Sincronizando automaticamente",
                Location = new Point(28, 10),
                AutoSize = true,
                ForeColor = PrimaryGreen,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            liveIndicator.Controls.Add(lblLive);
            sidebar.Controls.Add(liveIndicator);
            yPos += 46;

            // Stats Card
            statsCard = new Panel
            {
                Location = new Point(20, yPos),
                Size = new Size(380, 100), // ✅ 380px
                BackColor = Color.Transparent
            };
            statsCard.Paint += StatsCard_Paint;

            var lblPendentes = new Label
            {
                Text = "Tickets Pendentes",
                Location = new Point(15, 15),
                AutoSize = true,
                ForeColor = Color.Silver,
                Font = new Font("Segoe UI", 9f),
                BackColor = Color.Transparent
            };
            statsCard.Controls.Add(lblPendentes);

            lblTotalTickets = new Label
            {
                Text = "0",
                Location = new Point(15, 35),
                AutoSize = true,
                ForeColor = PrimaryGreen,
                Font = new Font("Segoe UI", 24f, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            statsCard.Controls.Add(lblTotalTickets);

            var lblResolvidosText = new Label
            {
                Text = "Resolvidos Hoje",
                Location = new Point(200, 15), // ✅ Ajustado para 380px
                AutoSize = true,
                ForeColor = Color.Silver,
                Font = new Font("Segoe UI", 9f),
                BackColor = Color.Transparent
            };
            statsCard.Controls.Add(lblResolvidosText);

            lblResolvidosHoje = new Label
            {
                Text = "0",
                Location = new Point(200, 35), // ✅ Ajustado
                AutoSize = true,
                ForeColor = ColorTranslator.FromHtml("#34d399"),
                Font = new Font("Segoe UI", 24f, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            statsCard.Controls.Add(lblResolvidosHoje);

            sidebar.Controls.Add(statsCard);
            yPos += 110;

            // Título da lista
            var lblTicketsTitle = new Label
            {
                Text = "📋 Tickets Pendentes",
                Location = new Point(20, yPos),
                AutoSize = true,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            sidebar.Controls.Add(lblTicketsTitle);
            yPos += 35;

            // Lista de Tickets
            var ticketsContainer = new Panel
            {
                Location = new Point(0, yPos),
                Size = new Size(420, Height - yPos - 80), // ✅ 420px
                AutoScroll = true,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
            };

            ticketsList = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Padding = new Padding(20, 0, 20, 10)
            };
            ticketsContainer.Controls.Add(ticketsList);
            sidebar.Controls.Add(ticketsContainer);

            // Footer (Botão Sair)
            var footerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                BackColor = CardBg
            };
            footerPanel.Paint += FooterPanel_Paint;

            btnSair = new Button
            {
                Text = "🚪 Sair",
                Location = new Point(20, 18),
                Size = new Size(380, 45), // ✅ 380px
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSair.FlatAppearance.BorderColor = BorderColor;
            btnSair.FlatAppearance.BorderSize = 2;
            btnSair.Paint += BtnSair_Paint;
            btnSair.Click += (s, e) =>
            {
                this.Hide();
                var loginForm = new LoginForm();
                loginForm.FormClosed += (sender, args) => this.Close();
                loginForm.Show();
            };
            footerPanel.Controls.Add(btnSair);
            sidebar.Controls.Add(footerPanel);

            // ===== WORK AREA (COLUNA 1) =====
            workArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = DarkerBg,
                Padding = new Padding(10, 0, 0, 0), // ✅ Margem esquerda para não encostar na sidebar
                Margin = new Padding(0)
            };
            mainLayout.Controls.Add(workArea, 1, 0); // ✅ Adiciona na coluna 1, linha 0

            // ✅ TableLayoutPanel para organizar header, messages e solution
            workTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = DarkerBg,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(0)
            };
            workTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));   // Header fixo
            workTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // Messages flexível
            workTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));  // ✅ Solution fixo - ajustado para 220px
            workArea.Controls.Add(workTable);

            // Work Header (ROW 0)
            workHeader = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = DarkBg,
                Visible = false
            };
            workHeader.Paint += WorkHeader_Paint;

            lblTicketTitle = new Label
            {
                Text = "Ticket #0",
                Location = new Point(30, 25),
                AutoSize = true,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            workHeader.Controls.Add(lblTicketTitle);
            workTable.Controls.Add(workHeader, 0, 0);

            // Conversation Panel (ROW 1)
            conversationPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = DarkerBg,
                AutoScroll = false,
                Padding = new Padding(0)
            };

            messagesList = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Padding = new Padding(30, 20, 30, 20),
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            // Empty State
            emptyState = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            var emptyIcon = new Label
            {
                Text = "📋",
                Font = new Font("Segoe UI Emoji", 60f),
                ForeColor = ColorTranslator.FromHtml("#8b5cf6"),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            var emptyText = new Label
            {
                Text = "Selecione um Ticket",
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = TextColor,
                AutoSize = true,
                BackColor = Color.Transparent
            };

            var emptySubtext = new Label
            {
                Text = "Escolha um ticket da lista ao lado\npara visualizar os detalhes e responder.",
                Font = new Font("Segoe UI", 11f),
                ForeColor = Color.Silver,
                AutoSize = true,
                TextAlign = ContentAlignment.TopCenter,
                BackColor = Color.Transparent
            };

            emptyState.Controls.Add(emptyIcon);
            emptyState.Controls.Add(emptyText);
            emptyState.Controls.Add(emptySubtext);

            conversationPanel.Controls.Add(emptyState);
            conversationPanel.Controls.Add(messagesList);
            messagesList.Visible = false;
            conversationPanel.Resize += (s, e) => CenterEmptyState();
            conversationPanel.Layout += (s, e) => CenterEmptyState();
            workTable.Controls.Add(conversationPanel, 0, 1);

            // Solution Area (ROW 2) - ✅ LAYOUT COM BOTÕES EMBAIXO
            solutionArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = DarkBg,
                Visible = false,
                Padding = new Padding(30, 20, 30, 20)
            };
            solutionArea.Paint += SolutionArea_Paint;

            // ✅ BOTÕES EMBAIXO
            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 5, 0, 0)
            };

            btnResolver = new Button
            {
                Text = "✓ Resolver e Fechar",
                Width = 200,
                Height = 45,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };
            btnResolver.FlatAppearance.BorderSize = 0;
            btnResolver.Paint += BtnResolver_Paint;
            btnResolver.Click += async (s, e) => await ResponderTicketAsync(true);
            btnPanel.Controls.Add(btnResolver);

            btnResponder = new Button
            {
                Text = "💬 Enviar Mensagem",
                Width = 200,
                Height = 45,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 12, 0)
            };
            btnResponder.FlatAppearance.BorderSize = 0;
            btnResponder.Paint += BtnResponder_Paint;
            btnResponder.Click += async (s, e) => await ResponderTicketAsync(false);
            btnPanel.Controls.Add(btnResponder);

            solutionArea.Controls.Add(btnPanel);

            lblSuccess = new Label
            {
                Dock = DockStyle.Top,
                Height = 40,
                Visible = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            lblSuccess.Paint += LblSuccess_Paint;
            solutionArea.Controls.Add(lblSuccess);

            var lblSolucaoTitle = new Label
            {
                Text = "💬 Digite sua resposta",
                Dock = DockStyle.Top,
                Height = 28,
                ForeColor = PrimaryGreen,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            solutionArea.Controls.Add(lblSolucaoTitle);

            // ✅ CONTAINER PARA TEXTBOX COM BORDAS ARREDONDADAS
            var txtContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(8)
            };
            txtContainer.Paint += TxtContainer_Paint;

            txtSolucao = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                BackColor = CardBg,
                ForeColor = TextColor,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10f)
            };
            txtSolucao.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !e.Shift)
                {
                    e.SuppressKeyPress = true;
                    _ = ResponderTicketAsync(false);
                }
            };
            txtContainer.Controls.Add(txtSolucao);
            solutionArea.Controls.Add(txtContainer);

            workTable.Controls.Add(solutionArea, 0, 2);
            ResumeLayout();
        }

        // ===== Paint Handlers =====

        private void Sidebar_Paint(object sender, PaintEventArgs e)
        {
            // Borda direita
            using (Pen pen = new Pen(BorderColor, 1))
            {
                e.Graphics.DrawLine(pen, sidebar.Width - 1, 0, sidebar.Width - 1, sidebar.Height);
            }
        }

        private void LogoIcon_Paint(object sender, PaintEventArgs e)
        {
            if (_logoImage == null) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Neon glow effect verde
            using (GraphicsPath shadowPath = new GraphicsPath())
            {
                shadowPath.AddEllipse(new Rectangle(-10, -10, 65, 65));
                using (PathGradientBrush shadowBrush = new PathGradientBrush(shadowPath))
                {
                    shadowBrush.CenterColor = Color.FromArgb(70, PrimaryGreen);
                    shadowBrush.SurroundColors = new[] { Color.Transparent };
                    e.Graphics.FillPath(shadowBrush, shadowPath);
                }
            }
        }

        private void LblLogoText_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            using (LinearGradientBrush brush = new LinearGradientBrush(
                lblLogoText.ClientRectangle, PrimaryGreen, ColorTranslator.FromHtml("#06b6d4"), 135f))
            {
                using (StringFormat sf = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center
                })
                {
                    e.Graphics.DrawString(lblLogoText.Text, lblLogoText.Font, brush, lblLogoText.ClientRectangle, sf);
                }
            }
        }

        private void BtnRefresh_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = GetRoundedRect(btnRefresh.ClientRectangle, 12))
            {
                using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(25, PrimaryGreen)))
                {
                    e.Graphics.FillPath(bgBrush, path);
                }

                using (Pen borderPen = new Pen(PrimaryGreen, 2))
                {
                    e.Graphics.DrawPath(borderPen, path);
                }
            }

            TextRenderer.DrawText(e.Graphics, btnRefresh.Text, btnRefresh.Font,
                btnRefresh.ClientRectangle, btnRefresh.ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private void LiveIndicator_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = GetRoundedRect(liveIndicator.ClientRectangle, 10))
            {
                using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(25, PrimaryGreen)))
                {
                    e.Graphics.FillPath(bgBrush, path);
                }

                using (Pen borderPen = new Pen(Color.FromArgb(76, PrimaryGreen), 1))
                {
                    e.Graphics.DrawPath(borderPen, path);
                }
            }

            // Pulsing dot
            float dotScale = 1.0f + (_pulseScale - 1.0f) * 0.5f;
            int dotSize = (int)(8 * dotScale);
            int dotX = 14 - dotSize / 2;
            int dotY = 14 - dotSize / 2;

            using (SolidBrush dotBrush = new SolidBrush(PrimaryGreen))
            {
                e.Graphics.FillEllipse(dotBrush, dotX, dotY, dotSize, dotSize);
            }
        }

        private void StatsCard_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = GetRoundedRect(statsCard.ClientRectangle, 12))
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    statsCard.ClientRectangle, CardBg, DarkerBg, 135f))
                {
                    e.Graphics.FillPath(brush, path);
                }

                using (Pen borderPen = new Pen(BorderColor, 1))
                {
                    e.Graphics.DrawPath(borderPen, path);
                }
            }
        }

        private void FooterPanel_Paint(object sender, PaintEventArgs e)
        {
            // Borda superior
            using (Pen pen = new Pen(BorderColor, 1))
            {
                e.Graphics.DrawLine(pen, 0, 0, ((Panel)sender).Width, 0);
            }
        }

        private void BtnSair_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = GetRoundedRect(btnSair.ClientRectangle, 12))
            {
                using (SolidBrush bgBrush = new SolidBrush(Color.Transparent))
                {
                    e.Graphics.FillPath(bgBrush, path);
                }

                using (Pen borderPen = new Pen(BorderColor, 2))
                {
                    e.Graphics.DrawPath(borderPen, path);
                }
            }

            TextRenderer.DrawText(e.Graphics, btnSair.Text, btnSair.Font,
                btnSair.ClientRectangle, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private void WorkHeader_Paint(object sender, PaintEventArgs e)
        {
            // Borda inferior
            using (Pen pen = new Pen(BorderColor, 1))
            {
                e.Graphics.DrawLine(pen, 0, workHeader.Height - 1, workHeader.Width, workHeader.Height - 1);
            }
        }

        private void SolutionArea_Paint(object sender, PaintEventArgs e)
        {
            // Borda superior
            using (Pen pen = new Pen(BorderColor, 1))
            {
                e.Graphics.DrawLine(pen, 0, 0, solutionArea.Width, 0);
            }
        }

        private void TxtContainer_Paint(object sender, PaintEventArgs e)
        {
            Panel container = (Panel)sender;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = GetRoundedRect(container.ClientRectangle, 12))
            {
                // Fundo
                using (SolidBrush bgBrush = new SolidBrush(CardBg))
                {
                    e.Graphics.FillPath(bgBrush, path);
                }

                // Borda verde
                using (Pen borderPen = new Pen(PrimaryGreen, 2))
                {
                    e.Graphics.DrawPath(borderPen, path);
                }
            }
        }

        private void LblSuccess_Paint(object sender, PaintEventArgs e)
        {
            if (!lblSuccess.Visible) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = GetRoundedRect(lblSuccess.ClientRectangle, 10))
            {
                using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(40, PrimaryGreen)))
                {
                    e.Graphics.FillPath(bgBrush, path);
                }

                using (Pen borderPen = new Pen(PrimaryGreen, 1))
                {
                    e.Graphics.DrawPath(borderPen, path);
                }
            }
        }

        private void BtnResponder_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = GetRoundedRect(btnResponder.ClientRectangle, 10))
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    btnResponder.ClientRectangle,
                    ColorTranslator.FromHtml("#3b82f6"),
                    ColorTranslator.FromHtml("#2563eb"), 135f))
                {
                    e.Graphics.FillPath(brush, path);
                }
            }

            TextRenderer.DrawText(e.Graphics, btnResponder.Text, btnResponder.Font,
                btnResponder.ClientRectangle, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private void BtnResolver_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = GetRoundedRect(btnResolver.ClientRectangle, 10))
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    btnResolver.ClientRectangle, PrimaryGreen, SecondaryGreen, 135f))
                {
                    e.Graphics.FillPath(brush, path);
                }
            }

            TextRenderer.DrawText(e.Graphics, btnResolver.Text, btnResolver.Font,
                btnResolver.ClientRectangle, Color.White,
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

        // ===== Timers e Animações =====

        private void ConfigurarTimers()
        {
            _timerRefresh.Interval = 5000;
            _timerRefresh.Tick += async (s, e) => await CarregarTicketsAsync(true);
            _timerRefresh.Start();

            _timerPolling.Interval = 3000;
            _timerPolling.Tick += async (s, e) => await PollingTicketAtualAsync();

            _timerAnimacoes.Interval = 30;
            _timerAnimacoes.Tick += AnimacoesTick;
            _timerAnimacoes.Start();
        }

        private void AnimacoesTick(object sender, EventArgs e)
        {
            // Pulse animation (for live dot)
            if (_pulseGrowing)
            {
                _pulseScale += 0.002f;
                if (_pulseScale >= 1.05f) _pulseGrowing = false;
            }
            else
            {
                _pulseScale -= 0.002f;
                if (_pulseScale <= 1.0f) _pulseGrowing = true;
            }

            liveIndicator?.Invalidate();
        }

        // ===== Tickets =====

        private async System.Threading.Tasks.Task CarregarTicketsAsync(bool silencioso = false)
        {
            try
            {
                var tickets = await _apiClient.ObterTicketsPendentesAsync();
                _ticketsCache = tickets ?? new List<TicketDTO>();

                ticketsList.SuspendLayout();
                ticketsList.Controls.Clear();

                lblTotalTickets.Text = _ticketsCache.Count.ToString();

                if (_ticketsCache.Count == 0)
                {
                    var emptyLabel = new Label
                    {
                        Text = "Nenhum ticket pendente!\n😊",
                        ForeColor = Color.FromArgb(156, 163, 175),
                        Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                        AutoSize = true,
                        TextAlign = ContentAlignment.TopCenter,
                        Padding = new Padding(0, 40, 0, 0)
                    };
                    ticketsList.Controls.Add(emptyLabel);
                }
                else
                {
                    foreach (var ticket in _ticketsCache)
                    {
                        var card = CriarTicketCard(ticket);
                        ticketsList.Controls.Add(card);
                    }
                }

                ticketsList.ResumeLayout();

                if (!silencioso)
                    Notificar("✅ Tickets atualizados!");
            }
            catch (Exception ex)
            {
                if (!silencioso)
                    MessageBox.Show("Erro ao carregar tickets: " + ex.Message, "Erro",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Panel CriarTicketCard(TicketDTO ticket)
        {
            var card = new Panel
            {
                Size = new Size(380, 110), // ✅ 380px (420 - 40 margens)
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 12),
                Cursor = Cursors.Hand,
                Tag = ticket
            };
            card.Paint += TicketCard_Paint;
            card.Click += async (s, e) => await SelecionarTicketAsync(ticket);

            var lblId = new Label
            {
                Text = $"Ticket #{ticket.Id}",
                Location = new Point(15, 15),
                AutoSize = true,
                ForeColor = PrimaryGreen,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            lblId.Click += async (s, e) => await SelecionarTicketAsync(ticket);
            card.Controls.Add(lblId);

            var lblNome = new Label
            {
                Text = $"👤 {ticket.NomeSolicitante}",
                Location = new Point(15, 38),
                AutoSize = true,
                ForeColor = Color.Silver,
                Font = new Font("Segoe UI", 9f),
                BackColor = Color.Transparent
            };
            lblNome.Click += async (s, e) => await SelecionarTicketAsync(ticket);
            card.Controls.Add(lblNome);

            string desc = ticket.DescricaoProblema.Length > 55 ? // ✅ 55 chars (reduzido de 75)
                ticket.DescricaoProblema.Substring(0, 55) + "..." : ticket.DescricaoProblema;

            var lblDesc = new Label
            {
                Text = desc,
                Location = new Point(15, 58),
                AutoSize = false,
                Size = new Size(350, 20), // ✅ 350px (380 - 30px margens)
                ForeColor = Color.Silver,
                Font = new Font("Segoe UI", 8f),
                BackColor = Color.Transparent
            };
            lblDesc.Click += async (s, e) => await SelecionarTicketAsync(ticket);
            card.Controls.Add(lblDesc);

            var lblData = new Label
            {
                Text = $"📅 {ticket.DataAbertura:dd/MM/yyyy}",
                Location = new Point(15, 80),
                AutoSize = true,
                ForeColor = Color.Silver,
                Font = new Font("Segoe UI", 8f),
                BackColor = Color.Transparent
            };
            lblData.Click += async (s, e) => await SelecionarTicketAsync(ticket);
            card.Controls.Add(lblData);

            return card;
        }

        private void TicketCard_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Panel card = (Panel)sender;
            TicketDTO ticket = card.Tag as TicketDTO;
            bool isSelected = _ticketSelecionado != null && ticket != null && ticket.Id == _ticketSelecionado.Id;

            using (GraphicsPath path = GetRoundedRect(card.ClientRectangle, 12))
            {
                Color bgColor = isSelected ? BorderColor : CardBg;
                using (SolidBrush brush = new SolidBrush(bgColor))
                {
                    e.Graphics.FillPath(brush, path);
                }

                if (isSelected)
                {
                    using (Pen borderPen = new Pen(PrimaryGreen, 2))
                    {
                        e.Graphics.DrawPath(borderPen, path);
                    }
                }
                else
                {
                    using (Pen borderPen = new Pen(BorderColor, 1))
                    {
                        e.Graphics.DrawPath(borderPen, path);
                    }
                }
            }
        }

        private async System.Threading.Tasks.Task SelecionarTicketAsync(TicketDTO ticket)
        {
            _ticketSelecionado = ticket;
            _ultimoHashSolucao = string.Empty;

            // Atualiza visual dos cards
            foreach (Control ctrl in ticketsList.Controls)
            {
                if (ctrl is Panel panel)
                    panel.Invalidate();
            }
            if (emptyState != null)
            {
                emptyState.Visible = false;
                emptyState.SendToBack();
            }

            messagesList.Controls.Clear();
            messagesList.Visible = true;
            messagesList.BringToFront();

            workHeader.Visible = true;
            lblTicketTitle.Text = $"Ticket #{ticket.Id} - {ticket.NomeSolicitante}";

            // ✅ ORDEM CRONOLÓGICA: Coleta todas mensagens com timestamp
            var todasMensagens = new List<MensagemComTimestamp>();

            // Parsea perguntas
            ParsearMensagensSimples(ticket.PerguntaOriginal, "user", ticket.NomeSolicitante, ticket.DataAbertura, todasMensagens);

            // Parsea respostas da IA
            string respostaPadrao = ticket.RespostaIA ?? "A resposta automática não foi suficiente.";
            ParsearMensagensSimples(respostaPadrao, "bot", "DotIA", ticket.DataAbertura, todasMensagens);

            // Parsea conversas técnico/usuário
            if (!string.IsNullOrEmpty(ticket.Solucao))
            {
                ParsearMensagensTecnico(ticket.Solucao, ticket.NomeSolicitante, todasMensagens);
            }

            // ✅ ORDENA por timestamp
            todasMensagens.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

            // ✅ EXIBE em ordem cronológica
            foreach (var msg in todasMensagens)
            {
                AdicionarMensagem(msg.Tipo, msg.Conteudo, msg.Autor);
            }

            solutionArea.Visible = true;
            txtSolucao.Clear();
            lblSuccess.Visible = false;

            _ultimoHashSolucao = ticket.Solucao?.Length + "_" + ticket.Solucao?.Substring(0, Math.Min(50, ticket.Solucao?.Length ?? 0));
            _timerPolling.Start();
        }

        // ✅ Classe auxiliar para ordenação cronológica
        private class MensagemComTimestamp
        {
            public string Tipo { get; set; }
            public string Conteudo { get; set; }
            public string Autor { get; set; }
            public DateTime Timestamp { get; set; }
        }

        // ✅ Parsea mensagens simples com formato [dd/MM/yyyy HH:mm]
        private void ParsearMensagensSimples(string texto, string tipo, string autor, DateTime dataDefault, List<MensagemComTimestamp> lista)
        {
            if (string.IsNullOrWhiteSpace(texto)) return;

            var regex = new System.Text.RegularExpressions.Regex(@"\[(\d{2}/\d{2}/\d{4}\s\d{2}:\d{2})\]\s*(.+?)(?=\n\n\[|$)", System.Text.RegularExpressions.RegexOptions.Singleline);
            var matches = regex.Matches(texto);

            if (matches.Count > 0)
            {
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    var dataStr = match.Groups[1].Value;
                    var conteudo = match.Groups[2].Value.Trim();

                    var parts = dataStr.Split(new[] { '/', ' ', ':' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 5)
                    {
                        int dia = int.Parse(parts[0]);
                        int mes = int.Parse(parts[1]);
                        int ano = int.Parse(parts[2]);
                        int hora = int.Parse(parts[3]);
                        int min = int.Parse(parts[4]);

                        var timestamp = new DateTime(ano, mes, dia, hora, min, 0);
                        lista.Add(new MensagemComTimestamp { Tipo = tipo, Conteudo = conteudo, Autor = autor, Timestamp = timestamp });
                    }
                }
            }
            else
            {
                // Mensagem única sem timestamp
                lista.Add(new MensagemComTimestamp { Tipo = tipo, Conteudo = texto.Trim(), Autor = autor, Timestamp = dataDefault });
            }
        }

        // ✅ Parsea mensagens técnico/usuário com formato [TÉCNICO - dd/MM/yyyy HH:mm] ou [USUÁRIO - dd/MM/yyyy HH:mm]
        private void ParsearMensagensTecnico(string solucao, string nomeUsuario, List<MensagemComTimestamp> lista)
        {
            var mensagens = solucao.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var m in mensagens)
            {
                if (string.IsNullOrWhiteSpace(m)) continue;

                var usuarioMatch = System.Text.RegularExpressions.Regex.Match(m, @"^\[USUÁRIO\s*-\s*(\d{2}/\d{2}/\d{4}\s\d{2}:\d{2})\]\s*(.+)$", System.Text.RegularExpressions.RegexOptions.Singleline);
                var tecnicoMatch = System.Text.RegularExpressions.Regex.Match(m, @"^\[TÉCNICO\s*-\s*(\d{2}/\d{2}/\d{4}\s\d{2}:\d{2})\]\s*(.+)$", System.Text.RegularExpressions.RegexOptions.Singleline);

                if (usuarioMatch.Success)
                {
                    var dataStr = usuarioMatch.Groups[1].Value;
                    var conteudo = usuarioMatch.Groups[2].Value.Trim();
                    var timestamp = ParsearData(dataStr);
                    lista.Add(new MensagemComTimestamp { Tipo = "user", Conteudo = conteudo, Autor = nomeUsuario, Timestamp = timestamp });
                }
                else if (tecnicoMatch.Success)
                {
                    var dataStr = tecnicoMatch.Groups[1].Value;
                    var conteudo = tecnicoMatch.Groups[2].Value.Trim();
                    var timestamp = ParsearData(dataStr);
                    lista.Add(new MensagemComTimestamp { Tipo = "tech", Conteudo = conteudo, Autor = "Você (Técnico)", Timestamp = timestamp });
                }
            }
        }

        private DateTime ParsearData(string dataStr)
        {
            var parts = dataStr.Split(new[] { '/', ' ', ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 5)
            {
                int dia = int.Parse(parts[0]);
                int mes = int.Parse(parts[1]);
                int ano = int.Parse(parts[2]);
                int hora = int.Parse(parts[3]);
                int min = int.Parse(parts[4]);
                return new DateTime(ano, mes, dia, hora, min, 0);
            }
            return DateTime.Now;
        }

        private void AdicionarMensagem(string tipo, string conteudo, string autor)
        {
            // ✅ Força atualização do layout antes de calcular larguras
            messagesList.PerformLayout();
            Application.DoEvents();

            int containerWidth = messagesList.ClientSize.Width - 60;
            int maxWidth = (int)(containerWidth * 0.7);

            // Avatar com círculo ao redor
            int avatarSize = tipo == "user" ? 40 : 44;
            var avatarContainer = new Panel
            {
                Size = new Size(avatarSize + 12, avatarSize + 12),
                BackColor = Color.Transparent
            };
            avatarContainer.Paint += (s, e) => AvatarCircle_Paint(e, tipo, avatarSize);

            var avatar = new PictureBox
            {
                Size = new Size(avatarSize, avatarSize),
                Location = new Point(6, 6),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };

            if (tipo == "user")
            {
                avatar.Image = _userAvatarImage;
            }
            else if (tipo == "bot")
            {
                avatar.Image = _botAvatarImage;
                avatar.Paint += BotAvatar_Paint;
            }
            else if (tipo == "tech")
            {
                avatar.Image = _tecnicoAvatarImage;
                avatar.Paint += TechAvatar_Paint;
            }

            avatarContainer.Controls.Add(avatar);

            // ✅ Calcula largura dinâmica baseada no conteúdo
            using (var lblTemp = new Label
            {
                Text = conteudo,
                Font = new Font("Segoe UI", 10f),
                AutoSize = true,
                MaximumSize = new Size(maxWidth - 40, 0)
            })
            {
                int contentWidth = Math.Min(Math.Max(lblTemp.PreferredWidth + 40, 200), maxWidth);

                // Message box
                var messageBox = new Panel
                {
                    AutoSize = false,
                    Width = contentWidth,
                    MinimumSize = new Size(200, 0),
                    Padding = new Padding(20, 16, 20, 16),
                    BackColor = Color.Transparent
                };
                messageBox.Tag = tipo;
                messageBox.Paint += MessageBox_Paint;

                // ✅ Mensagem primeiro (fica embaixo com Dock.Top)
                var lblConteudo = new Label
                {
                    Text = conteudo,
                    Dock = DockStyle.Top,
                    ForeColor = TextColor,
                    Font = new Font("Segoe UI", 10f),
                    AutoSize = true,
                    MaximumSize = new Size(contentWidth - 40, 0),
                    BackColor = Color.Transparent
                };
                messageBox.Controls.Add(lblConteudo);

                // ✅ Nome depois (fica em cima com Dock.Top)
                var lblAutor = new Label
                {
                    Text = autor,
                    Dock = DockStyle.Top,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    AutoSize = true,
                    BackColor = Color.Transparent,
                    Padding = new Padding(0, 0, 0, 8)
                };
                messageBox.Controls.Add(lblAutor);

                // Ajusta altura do messageBox
                int contentHeight = lblAutor.PreferredHeight + lblConteudo.PreferredHeight + 32;
                messageBox.Height = contentHeight;

                // ✅ Container da linha
                var messageRow = new Panel
                {
                    Width = containerWidth,
                    Height = Math.Max(avatarContainer.Height, messageBox.Height) + 20,
                    BackColor = Color.Transparent,
                    Margin = new Padding(0, 0, 0, 20),
                    Anchor = AnchorStyles.Left | AnchorStyles.Top
                };

                // ✅ POSICIONAMENTO: Técnico à DIREITA, Bot/User à ESQUERDA
                if (tipo == "tech")
                {
                    // Técnico: à direita
                    int avatarX = containerWidth - avatarContainer.Width;
                    int messageBoxX = avatarX - messageBox.Width - 12;

                    messageBox.Location = new Point(messageBoxX, 0);
                    avatarContainer.Location = new Point(avatarX, 0);
                }
                else
                {
                    // Bot/User: à esquerda
                    avatarContainer.Location = new Point(0, 0);
                    messageBox.Location = new Point(avatarContainer.Width + 12, 0);
                }

                messageRow.Controls.Add(avatarContainer);
                messageRow.Controls.Add(messageBox);
                messagesList.Controls.Add(messageRow);
            }

            ScrollToBottom();
        }

        private void AvatarCircle_Paint(PaintEventArgs e, string tipo, int avatarSize)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle circleRect = new Rectangle(0, 0, avatarSize + 12, avatarSize + 12);

            using (GraphicsPath circlePath = new GraphicsPath())
            {
                circlePath.AddEllipse(circleRect);

                using (PathGradientBrush circleBrush = new PathGradientBrush(circlePath))
                {
                    if (tipo == "user")
                    {
                        circleBrush.CenterColor = Color.FromArgb(30, 255, 255, 255);
                        circleBrush.SurroundColors = new[] { Color.FromArgb(10, 255, 255, 255) };
                    }
                    else if (tipo == "bot")
                    {
                        circleBrush.CenterColor = Color.FromArgb(70, PrimaryPurple);
                        circleBrush.SurroundColors = new[] { Color.FromArgb(25, SecondaryPurple) };
                    }
                    else if (tipo == "tech")
                    {
                        circleBrush.CenterColor = Color.FromArgb(70, PrimaryGreen);
                        circleBrush.SurroundColors = new[] { Color.FromArgb(25, SecondaryGreen) };
                    }

                    e.Graphics.FillPath(circleBrush, circlePath);
                }

                using (Pen borderPen = new Pen(Color.FromArgb(15, 255, 255, 255), 1))
                {
                    e.Graphics.DrawPath(borderPen, circlePath);
                }
            }
        }

        private void MessageBox_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Panel box = (Panel)sender;
            string tipo = box.Tag as string;

            using (GraphicsPath path = GetRoundedRect(box.ClientRectangle, 16))
            {
                if (tipo == "user")
                {
                    using (SolidBrush brush = new SolidBrush(CardBg))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                    using (Pen borderPen = new Pen(PrimaryPurple, 1))
                    {
                        e.Graphics.DrawPath(borderPen, path);
                    }
                }
                else if (tipo == "bot")
                {
                    using (SolidBrush brush = new SolidBrush(CardBg))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                    using (Pen borderPen = new Pen(ColorTranslator.FromHtml("#3b82f6"), 1))
                    {
                        e.Graphics.DrawPath(borderPen, path);
                    }
                }
                else if (tipo == "tech")
                {
                    using (LinearGradientBrush brush = new LinearGradientBrush(
                        box.ClientRectangle,
                        Color.FromArgb(40, PrimaryGreen),
                        Color.FromArgb(30, SecondaryGreen), 135f))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                    using (Pen borderPen = new Pen(PrimaryGreen, 1))
                    {
                        e.Graphics.DrawPath(borderPen, path);
                    }
                }
            }
        }

        private void BotAvatar_Paint(object sender, PaintEventArgs e)
        {
            PictureBox pb = (PictureBox)sender;
            if (pb.Image == null) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        }

        private void TechAvatar_Paint(object sender, PaintEventArgs e)
        {
            PictureBox pb = (PictureBox)sender;
            if (pb.Image == null) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        }

        private void ScrollToBottom()
        {
            messagesList.PerformLayout();
            Application.DoEvents();

            if (messagesList.VerticalScroll.Visible)
            {
                messagesList.AutoScrollPosition = new Point(0, messagesList.VerticalScroll.Maximum);
                messagesList.PerformLayout();
            }
        }

        // ===== Polling =====

        private async System.Threading.Tasks.Task PollingTicketAtualAsync()
        {
            if (_ticketSelecionado == null) return;

            try
            {
                var ticketsAtualizados = await _apiClient.ObterTicketsPendentesAsync();
                var ticketAtualizado = ticketsAtualizados?.FirstOrDefault(t => t.Id == _ticketSelecionado.Id);

                if (ticketAtualizado != null && !string.IsNullOrEmpty(ticketAtualizado.Solucao))
                {
                    string hashAtual = ticketAtualizado.Solucao.Length + "_" + ticketAtualizado.Solucao.Substring(0, Math.Min(50, ticketAtualizado.Solucao.Length));

                    if (hashAtual != _ultimoHashSolucao)
                    {
                        _ultimoHashSolucao = hashAtual;
                        await SelecionarTicketAsync(ticketAtualizado);
                        Notificar("💬 Nova mensagem do usuário!");
                    }
                }

                // Se ticket foi resolvido, para o polling
                if (ticketAtualizado != null && ticketAtualizado.Status == "2")
                {
                    _timerPolling.Stop();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro no polling: {ex.Message}");
            }
        }

        // ===== Responder =====

        private void CenterEmptyState()
        {
            if (emptyState == null || conversationPanel == null || !emptyState.Visible) return;

            var controls = emptyState.Controls.OfType<Label>().ToList();
            if (controls.Count != 3) return;

            var emptyIcon = controls[0];
            var emptyText = controls[1];
            var emptySubtext = controls[2];

            int panelWidth = conversationPanel.Width;
            int panelHeight = conversationPanel.Height;

            // Calcula altura total dos elementos
            int spacing = 30;
            int totalHeight = emptyIcon.Height + spacing + emptyText.Height + spacing + emptySubtext.Height;

            // Posição vertical centralizada
            int startY = Math.Max(50, (panelHeight - totalHeight) / 2);

            // Centraliza ícone
            emptyIcon.Left = (panelWidth - emptyIcon.Width) / 2;
            emptyIcon.Top = startY;

            // Centraliza texto
            emptyText.Left = (panelWidth - emptyText.Width) / 2;
            emptyText.Top = emptyIcon.Bottom + spacing;

            // Centraliza subtexto
            emptySubtext.Left = (panelWidth - emptySubtext.Width) / 2;
            emptySubtext.Top = emptyText.Bottom + spacing;
        }

        private async System.Threading.Tasks.Task ResponderTicketAsync(bool marcarComoResolvido)
        {
            if (_ticketSelecionado == null) return;

            var solucao = txtSolucao.Text.Trim();

            if (marcarComoResolvido)
            {
                if (MessageBox.Show("Tem certeza que deseja fechar este ticket? O cliente será notificado.",
                    "Confirmação", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
            }
            else
            {
                if (string.IsNullOrEmpty(solucao))
                {
                    MessageBox.Show("Por favor, escreva uma mensagem para o cliente.", "Atenção",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            btnResponder.Enabled = false;
            btnResolver.Enabled = false;

            try
            {
                var ok = await _apiClient.ResolverTicketAsync(_ticketSelecionado.Id, solucao, marcarComoResolvido);

                if (ok)
                {
                    if (!string.IsNullOrEmpty(solucao))
                        AdicionarMensagem("tech", solucao, "Você (Técnico)");

                    lblSuccess.Text = marcarComoResolvido ?
                        "✓ Ticket resolvido com sucesso!" : "✓ Mensagem enviada!";
                    lblSuccess.ForeColor = PrimaryGreen;
                    lblSuccess.Visible = true;
                    txtSolucao.Clear();

                    if (marcarComoResolvido)
                    {
                        await System.Threading.Tasks.Task.Delay(1500);
                        await CarregarTicketsAsync();
                        _timerPolling.Stop();
                        MostrarEmptyState();
                        _ticketSelecionado = null;
                    }
                    else
                    {
                        await System.Threading.Tasks.Task.Delay(1500);
                        lblSuccess.Visible = false;
                    }
                }
                else
                {
                    MessageBox.Show("Erro ao processar resposta.", "Erro",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro: " + ex.Message, "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnResponder.Enabled = true;
                btnResolver.Enabled = true;
            }
        }

        private void MostrarEmptyState()
        {
            if (emptyState != null)
            {
                emptyState.Visible = true;
                emptyState.BringToFront();
            }

            messagesList.Visible = false;
            messagesList.SendToBack();
            workHeader.Visible = false;
            solutionArea.Visible = false;
        }

        // ===== Notificações =====

        private void Notificar(string texto)
        {
            var t = new Label
            {
                Text = texto,
                AutoSize = true,
                ForeColor = Color.White,
                BackColor = PrimaryGreen,
                Padding = new Padding(15, 10, 15, 10),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Location = new Point(Width - 390, 20)
            };

            Controls.Add(t);
            t.BringToFront();

            var timer = new WinTimer { Interval = 3000 };
            timer.Tick += (s, e) =>
            {
                if (Controls.Contains(t)) Controls.Remove(t);
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timerRefresh?.Stop();
                _timerRefresh?.Dispose();
                _timerPolling?.Stop();
                _timerPolling?.Dispose();
                _timerAnimacoes?.Stop();
                _timerAnimacoes?.Dispose();

                _logoImage?.Dispose();
                _botAvatarImage?.Dispose();
                _tecnicoAvatarImage?.Dispose();
                _userAvatarImage?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}