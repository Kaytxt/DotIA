using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using DotIA.Desktop.Services;
using DotIA.Desktop.Controls;
using ThreadTimer = System.Threading.Timer;
using WinTimer = System.Windows.Forms.Timer;

namespace DotIA.Desktop.Forms
{
    public partial class ChatForm : Form
    {
        private readonly ApiClient _apiClient;
        private readonly int _usuarioId;
        private readonly string _nomeUsuario;

        // Estado
        private int _chatIdAtual = 0;
        private int? _chatStatusAtual = null;
        private readonly HashSet<string> _mensagensTecnicoProcessadas = new HashSet<string>();

        // Timers
        private readonly WinTimer _timerHistorico = new WinTimer();
        private readonly WinTimer _timerPollingTecnico = new WinTimer();
        private readonly WinTimer _timerAnimacoes = new WinTimer();

        // Animações
        private float _pulseScale = 1.0f;
        private bool _pulseGrowing = true;
        private float _floatOffset = 0f;
        private bool _floatUp = true;

        // Imagens
        private Image _logoImage;
        private Image _botAvatarImage;
        private Image _tecnicoAvatarImage;
        private Image _userAvatarImage;  


        // Paleta (cores exatas da web)
        private readonly Color PrimaryPurple = ColorTranslator.FromHtml("#8d4bff");
        private readonly Color SecondaryPurple = ColorTranslator.FromHtml("#a855f7");
        private readonly Color PinkAccent = ColorTranslator.FromHtml("#ec4899");
        private readonly Color DarkBg = ColorTranslator.FromHtml("#1a132f");
        private readonly Color DarkerBg = ColorTranslator.FromHtml("#1e1433");
        private readonly Color CardBg = ColorTranslator.FromHtml("#2c204d");
        private readonly Color BorderColor = ColorTranslator.FromHtml("#3d2e6b");
        private readonly Color TextColor = ColorTranslator.FromHtml("#e5e7eb");
        private readonly Color GreenTech = ColorTranslator.FromHtml("#10b981");

        // Layout
        private Panel sidebarPanel;
        private Panel sidebarHeader;
        private PictureBox logoIcon;
        private Label lblLogoText;
        private Button btnRefresh;
        private Panel liveIndicator;
        private Label lblLive;
        private RoundedButton btnNovoChat;
        private Button btnAbrirTicket;
        private Panel historyContainer;
        private FlowLayoutPanel historyList;
        private Panel userInfoPanel;

        private Panel chatArea;
        private Panel chatHeader;
        private Label lblChatTitle;
        private Button btnRefreshChat;
        private Panel messagesContainer;
        private Panel welcomePanel;
        private Panel welcomeIconContainer;
        private PictureBox welcomeIcon;
        private Label welcomeTitle;
        private Label welcomeText;
        private FlowLayoutPanel messagesList;
        private Panel feedbackArea;
        private Button btnFeedbackUtil;
        private Button btnFeedbackNaoUtil;
        private Panel typingIndicator;
        private Panel inputArea;
        private Panel blockedNotice;
        private Panel inputWrapper;
        private TextBox txtMensagem;
        private Button btnEnviar;
        private Panel _hoveredHistoryItem = null;

        // Context menu
        private ContextMenuStrip menuHistorico;

        // Dados
        private List<ChatHistorico> _historicoCache = new List<ChatHistorico>();
        private string _ultimaPergunta = string.Empty;
        private string _ultimaResposta = string.Empty;

        public ChatForm(int usuarioId, string nomeUsuario)
        {
            _usuarioId = usuarioId;
            _nomeUsuario = nomeUsuario;
            _apiClient = new ApiClient();

            CarregarImagens();
            InitializeComponent();
            MontarLayout();
            ConfigurarTimers();

            CarregarHistoricoAsync();
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
            Text = "DotIA - Chat";
            WindowState = FormWindowState.Maximized;
            BackColor = DarkBg;
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(1000, 600);
            Font = new Font("Segoe UI", 10f);
        }

        private void MontarLayout()
        {
            // Container principal (sidebar | chat area)
            SuspendLayout();

            // ===== SIDEBAR =====
            sidebarPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 300,
                BackColor = DarkBg
            };
            sidebarPanel.Paint += SidebarPanel_Paint;
            Controls.Add(sidebarPanel);

            int yPos = 0;

            // Header com logo
            sidebarHeader = new Panel
            {
                Location = new Point(0, yPos),
                Size = new Size(300, 80),
                BackColor = Color.Transparent
            };
            sidebarPanel.Controls.Add(sidebarHeader);
            yPos += 80;

            logoIcon = new PictureBox
            {
                Size = new Size(56, 56),
                Location = new Point(20, 12),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Image = _logoImage
            };
            logoIcon.Paint += LogoIcon_Paint;
            sidebarHeader.Controls.Add(logoIcon);

            lblLogoText = new Label
            {
                Text = "DotIA",
                Font = new Font("Segoe UI", 24f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(86, 25),
                BackColor = Color.Transparent
            };
            lblLogoText.Paint += LblLogoText_Paint;
            sidebarHeader.Controls.Add(lblLogoText);

            // Botão Refresh
            btnRefresh = new Button
            {
                Text = "⟳  Atualizar",
                Location = new Point(15, yPos),
                Size = new Size(270, 44),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = PrimaryPurple,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.FlatAppearance.MouseOverBackColor = Color.Transparent; // ✅ Remove hover branco
            btnRefresh.FlatAppearance.MouseDownBackColor = Color.Transparent; // ✅ Remove click branco
            btnRefresh.Paint += BtnRefresh_Paint;
            btnRefresh.Click += async (s, e) =>
            {
                btnRefresh.Enabled = false;
                await CarregarHistoricoAsync(true);
                btnRefresh.Enabled = true;
                Notificar("✅ Atualizado!");
            };
            sidebarPanel.Controls.Add(btnRefresh);
            yPos += 59;

            // Live indicator
            liveIndicator = new Panel
            {
                Location = new Point(15, yPos),
                Size = new Size(270, 36),
                BackColor = Color.Transparent
            };
            liveIndicator.Paint += LiveIndicator_Paint;

            lblLive = new Label
            {
                Text = "Sincronizando automaticamente",
                Location = new Point(28, 10),
                AutoSize = true,
                ForeColor = GreenTech,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            liveIndicator.Controls.Add(lblLive);
            sidebarPanel.Controls.Add(liveIndicator);
            yPos += 46;

            // Novo Chat
            btnNovoChat = new RoundedButton
            {
                Text = "＋  Novo Chat",
                Location = new Point(20, yPos),
                Size = new Size(260, 48),
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                BorderRadius = 12
            };
            btnNovoChat.Click += (s, e) => NovoChat();
            sidebarPanel.Controls.Add(btnNovoChat);
            yPos += 60;

            // Abrir Ticket
            btnAbrirTicket = new Button
            {
                Text = "🎫  Abrir Ticket",
                Location = new Point(20, yPos),
                Size = new Size(260, 48),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAbrirTicket.FlatAppearance.BorderSize = 0;
            btnAbrirTicket.FlatAppearance.MouseOverBackColor = Color.Transparent; // ✅ Remove hover branco
            btnAbrirTicket.FlatAppearance.MouseDownBackColor = Color.Transparent; // ✅ Remove click branco
            btnAbrirTicket.Paint += BtnAbrirTicket_Paint;
            btnAbrirTicket.Click += (s, e) => AbrirTicketDiretoDialog();
            sidebarPanel.Controls.Add(btnAbrirTicket);
            yPos += 60;

            // Histórico
            historyContainer = new Panel
            {
                Location = new Point(0, yPos),
                Size = new Size(300, Height - yPos - 80),
                AutoScroll = true,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
            };

            historyList = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Padding = new Padding(20, 10, 20, 10)
            };
            historyContainer.Controls.Add(historyList);
            sidebarPanel.Controls.Add(historyContainer);

            // Context menu
            menuHistorico = new ContextMenuStrip();
            var miEditar = new ToolStripMenuItem("✏ Editar título", null, async (s, e) => await EditarTituloSelecionadoAsync());
            var miExcluir = new ToolStripMenuItem("🗑 Excluir", null, async (s, e) => await ExcluirSelecionadoAsync());
            menuHistorico.Items.Add(miEditar);
            menuHistorico.Items.Add(miExcluir);

            // User info (no fundo)
            userInfoPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                BackColor = CardBg
            };
            userInfoPanel.Paint += UserInfoPanel_Paint;

            var lblUserName = new Label
            {
                Text = _nomeUsuario,
                Location = new Point(65, 18),
                AutoSize = true,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            userInfoPanel.Controls.Add(lblUserName);

            var lblOnline = new Label
            {
                Text = "● Online",
                Location = new Point(65, 40),
                AutoSize = true,
                ForeColor = GreenTech,
                Font = new Font("Segoe UI", 9f),
                BackColor = Color.Transparent
            };
            userInfoPanel.Controls.Add(lblOnline);

            sidebarPanel.Controls.Add(userInfoPanel);

            // ===== CHAT AREA =====
            chatArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = DarkerBg
            };
            Controls.Add(chatArea);
            chatArea.BringToFront();

            // Chat header (FIXO no topo)
            chatHeader = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(chatArea.Width, 64),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = DarkBg
            };
            chatHeader.Paint += ChatHeader_Paint;

            lblChatTitle = new Label
            {
                Text = "DotIA - Assistente Inteligente",
                Location = new Point(20, 20),
                AutoSize = true,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            chatHeader.Controls.Add(lblChatTitle);

            btnRefreshChat = new Button
            {
                Text = "⟳",
                Size = new Size(40, 40),
                Location = new Point(chatHeader.Width - 110, 12),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 16f),
                Cursor = Cursors.Hand
            };
            btnRefreshChat.FlatAppearance.BorderSize = 0;
            btnRefreshChat.Paint += BtnRefreshChat_Paint;
            btnRefreshChat.Click += async (s, e) => await CarregarHistoricoAsync(true);
            chatHeader.Controls.Add(btnRefreshChat);

            var btnLogout = new Button
            {
                Text = "🚪",
                Size = new Size(40, 40),
                Location = new Point(chatHeader.Width - 60, 12),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = ColorTranslator.FromHtml("#ef4444"),
                Font = new Font("Segoe UI", 20f),
                Cursor = Cursors.Hand
            };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.Paint += BtnLogout_Paint;
            btnLogout.Click += (s, e) =>
            {
                var result = MessageBox.Show("Deseja sair e voltar ao login?", "Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    this.Hide();
                    var loginForm = new LoginForm();
                    loginForm.FormClosed += (sender, args) => this.Close();
                    loginForm.Show();
                }
            };
            chatHeader.Controls.Add(btnLogout);
            chatArea.Controls.Add(chatHeader);

            // Input area (FIXO no fundo) - adicionado ANTES para calcular posição do messagesContainer
            inputArea = new Panel
            {
                Location = new Point(0, 600), // Será recalculado no resize
                Size = new Size(chatArea.Width, 70),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = DarkBg
            };
            inputArea.Paint += InputArea_Paint;

            // Messages container (FIXO entre header e input)
            messagesContainer = new Panel
            {
                Location = new Point(0, 64),
                Size = new Size(chatArea.Width, 536), // Será recalculado no resize
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = false,
                BackColor = DarkerBg,
                Padding = new Padding(0)
            };

            // Welcome screen
            welcomePanel = new Panel
            {
                Size = new Size(600, 400),
                BackColor = Color.Transparent
            };
            CenterWelcomePanel();
            messagesContainer.Resize += (s, e) => CenterWelcomePanel();

            // Container para glow effect do welcomeIcon
            welcomeIconContainer = new Panel
            {
                Size = new Size(180, 180),
                Location = new Point(210, -10),
                BackColor = Color.Transparent
            };
            welcomeIconContainer.Paint += WelcomeIcon_Paint;

            welcomeIcon = new PictureBox
            {
                Size = new Size(160, 160),
                Location = new Point(10, 10),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Image = _botAvatarImage
            };

            welcomeIconContainer.Controls.Add(welcomeIcon);
            welcomePanel.Controls.Add(welcomeIconContainer);

            welcomeTitle = new Label
            {
                Text = "Bem-vindo ao DotIA!",
                Font = new Font("Segoe UI", 36f, FontStyle.Bold),
                AutoSize = false,
                Size = new Size(600, 60),
                Location = new Point(0, 180),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            welcomeTitle.Paint += WelcomeTitle_Paint;
            welcomePanel.Controls.Add(welcomeTitle);

            welcomeText = new Label
            {
                Text = "Sou sua assistente virtual especializada em suporte técnico de TI.\n" +
                       "Faça qualquer pergunta sobre tecnologia, problemas técnicos\n" +
                       "ou funcionamento do sistema. Estou aqui para ajudar! 🚀",
                Font = new Font("Segoe UI", 12f),
                ForeColor = TextColor,
                AutoSize = false,
                Size = new Size(600, 100),
                Location = new Point(0, 250),
                TextAlign = ContentAlignment.TopCenter,
                BackColor = Color.Transparent
            };
            welcomePanel.Controls.Add(welcomeText);
            messagesContainer.Controls.Add(welcomePanel);

            // Messages list
            messagesList = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Visible = false,
                Padding = new Padding(30, 20, 30, 20)
            };
            messagesContainer.Controls.Add(messagesList);
            messagesList.BringToFront();

            // Feedback area (Dock.Bottom no chatArea - fica entre mensagens e input)
            feedbackArea = new Panel
            {
                Height = 0,
                Visible = false,
                BackColor = Color.Transparent
            };
            feedbackArea.Paint += FeedbackArea_Paint;

            btnFeedbackUtil = new Button
            {
                Text = "",
                Size = new Size(180, 58), // ✅ MUDOU de 170x56 para 180x58
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold), // ✅ MUDOU de 12f para 13f
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = "👍 Útil"
            };
            btnFeedbackUtil.FlatAppearance.BorderSize = 0;
            btnFeedbackUtil.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnFeedbackUtil.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnFeedbackUtil.FlatAppearance.BorderColor = BorderColor;
            btnFeedbackUtil.Paint += BtnFeedback_Paint;
            btnFeedbackUtil.Click += async (s, e) => await AvaliarChatAsync(true);
            feedbackArea.Controls.Add(btnFeedbackUtil);

            btnFeedbackNaoUtil = new Button
            {
                Text = "",
                Size = new Size(180, 58), // ✅ MUDOU de 170x60 para 180x58
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold), // ✅ MUDOU de 12f para 13f
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = "👎 Não ajudou"
            };
            btnFeedbackNaoUtil.FlatAppearance.BorderSize = 0;
            btnFeedbackNaoUtil.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnFeedbackNaoUtil.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnFeedbackNaoUtil.FlatAppearance.BorderColor = BorderColor;
            btnFeedbackNaoUtil.Paint += BtnFeedback_Paint;
            btnFeedbackNaoUtil.Click += async (s, e) => await AvaliarChatAsync(false);
            feedbackArea.Controls.Add(btnFeedbackNaoUtil);

            // Typing indicator
            typingIndicator = new Panel
            {
                Height = 60,
                Visible = false,
                BackColor = Color.Transparent,
                Padding = new Padding(30, 10, 30, 10),
                Margin = new Padding(0, 10, 0, 10),
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };

            var typingLabel = new Label
            {
                Text = "DotIA está digitando...",
                ForeColor = Color.Silver,
                Font = new Font("Segoe UI", 11f),
                AutoSize = true,
                Location = new Point(35, 20),
                BackColor = Color.Transparent
            };
            typingIndicator.Controls.Add(typingLabel);

            chatArea.Controls.Add(messagesContainer);
            chatArea.Controls.Add(inputArea);

            // Feedback area (overlay absoluto POR CIMA de tudo)
            feedbackArea.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            chatArea.Controls.Add(feedbackArea);
            feedbackArea.BringToFront();

            // Recalcula layout quando redimensiona
            chatArea.Resize += (s, e) =>
            {
                RecalcularLayout();
            };

            // Calcula layout inicial
            RecalcularLayout();

            // Blocked notice (overlay absoluto acima do input)
            blockedNotice = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(chatArea.Width, 40),
                Height = 0,
                Visible = false,
                BackColor = Color.Transparent,
                Padding = new Padding(20, 5, 20, 5),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            blockedNotice.Paint += BlockedNotice_Paint;

            var lblBlocked = new Label
            {
                Text = "🔒 Técnico ativo - suas mensagens vão para ele",
                Dock = DockStyle.Fill,
                ForeColor = Color.LightSkyBlue,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            blockedNotice.Controls.Add(lblBlocked);
            chatArea.Controls.Add(blockedNotice);

            // Input wrapper (Dock.Fill - preenche resto do inputArea)
            inputWrapper = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(20, 10, 20, 10)
            };
            // ✅ CORREÇÃO: Remove o Paint do inputWrapper (ele só serve de padding)

            // Container interno para input (com bordas arredondadas roxas)
            var inputInnerWrapper = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                BackColor = Color.Transparent // ✅ CORREÇÃO: Transparente
            };
            inputInnerWrapper.Paint += InputInnerWrapper_Paint; // ✅ CORREÇÃO: Agora ele tem o Paint

            txtMensagem = new TextBox
            {
                Location = new Point(20, 18),
                Size = new Size(inputInnerWrapper.Width - 185, 26),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
                BorderStyle = BorderStyle.None,
                BackColor = DarkBg, // ✅ Mantém a cor de fundo escura consistente
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 12f),
                Multiline = false
            };
            txtMensagem.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    _ = EnviarMensagemAsync();
                }
            };
            inputInnerWrapper.Controls.Add(txtMensagem);

            btnEnviar = new Button
            {
                Text = "Enviar ➜",
                Size = new Size(140, 42),
                Location = new Point(inputInnerWrapper.Width - 150, 10),
                Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent, // ✅ CORREÇÃO: Transparente para usar Paint
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnEnviar.FlatAppearance.BorderSize = 0;
            btnEnviar.Paint += BtnEnviar_Paint;
            btnEnviar.Click += async (s, e) => await EnviarMensagemAsync();
            inputInnerWrapper.Controls.Add(btnEnviar);

            inputWrapper.Controls.Add(inputInnerWrapper);
            inputArea.Controls.Add(inputWrapper);

            ResumeLayout();
        }

        // ===== Layout =====

        private void RecalcularLayout()
        {
            if (chatArea == null || inputArea == null || messagesContainer == null || chatHeader == null)
                return;

            int chatWidth = chatArea.ClientSize.Width;
            int chatHeight = chatArea.ClientSize.Height;

            // Header fixo no topo
            chatHeader.Location = new Point(0, 0);
            chatHeader.Size = new Size(chatWidth, 64);

            // Input fixo no fundo (sempre 70px)
            int inputHeight = 70;
            inputArea.Location = new Point(0, chatHeight - inputHeight);
            inputArea.Size = new Size(chatWidth, inputHeight);

            // Messages container preenche espaço entre header e input
            int messagesY = 64;
            int messagesHeight = chatHeight - 64 - inputHeight;
            messagesContainer.Location = new Point(0, messagesY);
            messagesContainer.Size = new Size(chatWidth, messagesHeight);

            // Feedback area (overlay) - posicionado acima do input quando visível
            if (feedbackArea.Visible && feedbackArea.Height > 0)
            {
                int feedbackY = chatHeight - inputHeight - feedbackArea.Height;
                feedbackArea.Location = new Point(0, feedbackY);
                feedbackArea.Size = new Size(chatWidth, feedbackArea.Height);
                feedbackArea.BringToFront();
            }

            // Blocked notice (overlay) - posicionado acima do input quando visível
            if (blockedNotice != null && blockedNotice.Visible && blockedNotice.Height > 0)
            {
                int blockedY = chatHeight - inputHeight - blockedNotice.Height;
                blockedNotice.Location = new Point(0, blockedY);
                blockedNotice.Size = new Size(chatWidth, blockedNotice.Height);
                blockedNotice.SendToBack();

            }
        }

        // ===== Paint Handlers =====

        private void SidebarPanel_Paint(object sender, PaintEventArgs e)
        {
            // Borda direita
            using (Pen pen = new Pen(BorderColor, 1))
            {
                e.Graphics.DrawLine(pen, sidebarPanel.Width - 1, 0, sidebarPanel.Width - 1, sidebarPanel.Height);
            }
        }

        private void LogoIcon_Paint(object sender, PaintEventArgs e)
        {
            if (_logoImage == null) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Neon glow effect
            using (GraphicsPath shadowPath = new GraphicsPath())
            {
                shadowPath.AddEllipse(new Rectangle(-10, -10, 76, 76));
                using (PathGradientBrush shadowBrush = new PathGradientBrush(shadowPath))
                {
                    shadowBrush.CenterColor = Color.FromArgb(70, PrimaryPurple);
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
                lblLogoText.ClientRectangle, SecondaryPurple, PinkAccent, 135f))
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

            // ✅ CORREÇÃO: Usa 12px como o Novo Chat
            using (GraphicsPath path = GetRoundedRect(btnRefresh.ClientRectangle, 12))
            {
                using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(25, PrimaryPurple)))
                {
                    e.Graphics.FillPath(bgBrush, path);
                }

                using (Pen borderPen = new Pen(PrimaryPurple, 2))
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
                using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(25, GreenTech)))
                {
                    e.Graphics.FillPath(bgBrush, path);
                }

                using (Pen borderPen = new Pen(Color.FromArgb(76, GreenTech), 1))
                {
                    e.Graphics.DrawPath(borderPen, path);
                }
            }

            // Pulsing dot
            float dotScale = 1.0f + (_pulseScale - 1.0f) * 0.5f;
            int dotSize = (int)(8 * dotScale);
            int dotX = 14 - dotSize / 2;
            int dotY = 14 - dotSize / 2;

            using (SolidBrush dotBrush = new SolidBrush(GreenTech))
            {
                e.Graphics.FillEllipse(dotBrush, dotX, dotY, dotSize, dotSize);
            }
        }

        private void BtnAbrirTicket_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // ✅ CORREÇÃO: Usa 12px como o Novo Chat
            using (GraphicsPath path = GetRoundedRect(btnAbrirTicket.ClientRectangle, 12))
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    btnAbrirTicket.ClientRectangle,
                    ColorTranslator.FromHtml("#fbbf24"),
                    ColorTranslator.FromHtml("#f59e0b"), 135f))
                {
                    e.Graphics.FillPath(brush, path);
                }
            }

            TextRenderer.DrawText(e.Graphics, btnAbrirTicket.Text, btnAbrirTicket.Font,
                btnAbrirTicket.ClientRectangle, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private void UserInfoPanel_Paint(object sender, PaintEventArgs e)
        {
            // Borda superior
            using (Pen pen = new Pen(BorderColor, 1))
            {
                e.Graphics.DrawLine(pen, 0, 0, userInfoPanel.Width, 0);
            }

            // Avatar gradient circle
            Rectangle avatarRect = new Rectangle(15, 20, 40, 40);
            using (GraphicsPath circlePath = new GraphicsPath())
            {
                circlePath.AddEllipse(avatarRect);
                using (LinearGradientBrush avatarBrush = new LinearGradientBrush(
                    avatarRect, PrimaryPurple, SecondaryPurple, 135f))
                {
                    e.Graphics.FillPath(avatarBrush, circlePath);
                }
            }

            // Draw emoji
            TextRenderer.DrawText(e.Graphics, "👤", new Font("Segoe UI Emoji", 20f),
                avatarRect, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private void ChatHeader_Paint(object sender, PaintEventArgs e)
        {
            // Borda inferior
            using (Pen pen = new Pen(BorderColor, 1))
            {
                e.Graphics.DrawLine(pen, 0, chatHeader.Height - 1, chatHeader.Width, chatHeader.Height - 1);
            }
        }

        private void BtnRefreshChat_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = GetRoundedRect(btnRefreshChat.ClientRectangle, 10))
            {
                using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(20, 255, 255, 255)))
                {
                    e.Graphics.FillPath(bgBrush, path);
                }
            }

            TextRenderer.DrawText(e.Graphics, btnRefreshChat.Text, btnRefreshChat.Font,
                btnRefreshChat.ClientRectangle, btnRefreshChat.ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private void BtnLogout_Paint(object sender, PaintEventArgs e)
        {
            Button btn = (Button)sender;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = GetRoundedRect(btn.ClientRectangle, 10))
            {
                using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(20, 239, 68, 68)))
                {
                    e.Graphics.FillPath(bgBrush, path);
                }
            }

            TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font,
                btn.ClientRectangle, btn.ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private void WelcomeIcon_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Floating effect offset
            int floatY = (int)_floatOffset;

            // Neon glow effect
            Panel container = (Panel)sender;
            using (GraphicsPath glowPath = new GraphicsPath())
            {
                Rectangle glowRect = new Rectangle(0, 0 + floatY, container.Width, container.Height);
                glowPath.AddEllipse(glowRect);
                using (PathGradientBrush glowBrush = new PathGradientBrush(glowPath))
                {
                    glowBrush.CenterColor = Color.FromArgb(80, PrimaryPurple);
                    glowBrush.SurroundColors = new[] { Color.Transparent };
                    e.Graphics.FillPath(glowBrush, glowPath);
                }
            }
        }

        private void WelcomeTitle_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            using (LinearGradientBrush brush = new LinearGradientBrush(
                welcomeTitle.ClientRectangle, SecondaryPurple, PinkAccent, 135f))
            {
                using (StringFormat sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                })
                {
                    e.Graphics.DrawString(welcomeTitle.Text, welcomeTitle.Font, brush, welcomeTitle.ClientRectangle, sf);
                }
            }
        }

        private void FeedbackArea_Paint(object sender, PaintEventArgs e)
        {
            if (!feedbackArea.Visible) return;

        }

        private void BtnFeedback_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            Button btn = (Button)sender;

            // ✅ Pega o texto do Tag
            string textoBtn = btn.Tag?.ToString() ?? "";

            // Sombra suave embaixo do botão
            Rectangle shadowRect = new Rectangle(
                btn.ClientRectangle.X + 2,
                btn.ClientRectangle.Y + 3,
                btn.ClientRectangle.Width,
                btn.ClientRectangle.Height);

            using (GraphicsPath shadowPath = GetRoundedRect(shadowRect, 18))
            {
                using (PathGradientBrush shadowBrush = new PathGradientBrush(shadowPath))
                {
                    shadowBrush.CenterColor = Color.FromArgb(80, 0, 0, 0);
                    shadowBrush.SurroundColors = new[] { Color.Transparent };
                    e.Graphics.FillPath(shadowBrush, shadowPath);
                }
            }

            // Botão principal com cantos bem arredondados
            using (GraphicsPath path = GetRoundedRect(btn.ClientRectangle, 18))
            {
                // Gradiente de fundo roxo
                using (LinearGradientBrush bgBrush = new LinearGradientBrush(
                    btn.ClientRectangle,
                    Color.FromArgb(40, PrimaryPurple),
                    Color.FromArgb(30, SecondaryPurple), 135f))
                {
                    e.Graphics.FillPath(bgBrush, path);
                }

                // Borda com gradiente roxo brilhante
                using (LinearGradientBrush borderBrush = new LinearGradientBrush(
                    btn.ClientRectangle, PrimaryPurple, SecondaryPurple, 135f))
                using (Pen borderPen = new Pen(borderBrush, 2))
                {
                    e.Graphics.DrawPath(borderPen, path);
                }

                // Brilho interno sutil no topo
                Rectangle highlightRect = btn.ClientRectangle;
                highlightRect.Inflate(-3, -3);
                highlightRect.Height = highlightRect.Height / 4;

                using (LinearGradientBrush highlightBrush = new LinearGradientBrush(
                    highlightRect,
                    Color.FromArgb(50, 255, 255, 255),
                    Color.FromArgb(0, 255, 255, 255), 90f))
                using (GraphicsPath highlightPath = GetRoundedRect(highlightRect, 15))
                {
                    e.Graphics.FillPath(highlightBrush, highlightPath);
                }
            }

            // ✅ MUDOU: Desenha o texto do Tag com fonte de emoji
            using (Font emojiFont = new Font("Segoe UI Emoji", 13f, FontStyle.Bold))
            using (StringFormat sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            })
            {
                e.Graphics.DrawString(textoBtn, emojiFont,
                    new SolidBrush(btn.ForeColor),
                    btn.ClientRectangle, sf);
            }
        }

        private void InputArea_Paint(object sender, PaintEventArgs e)
        {
            // Borda superior
            using (Pen pen = new Pen(BorderColor, 1))
            {
                e.Graphics.DrawLine(pen, 0, 0, inputArea.Width, 0);
            }
        }

        private void BlockedNotice_Paint(object sender, PaintEventArgs e)
        {
            if (!blockedNotice.Visible) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = GetRoundedRect(blockedNotice.ClientRectangle, 12))
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    blockedNotice.ClientRectangle,
                    Color.FromArgb(50, 59, 130, 246),
                    Color.FromArgb(50, 37, 99, 235), 135f))
                {
                    e.Graphics.FillPath(brush, path);
                }

                using (Pen borderPen = new Pen(Color.FromArgb(59, 130, 246), 1))
                {
                    e.Graphics.DrawPath(borderPen, path);
                }
            }
        }

        // ✅ NOVO: Paint para o input interno (borda roxa arredondada)
        private void InputInnerWrapper_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = GetRoundedRect(((Panel)sender).ClientRectangle, 20))
            {
                using (SolidBrush brush = new SolidBrush(DarkBg))
                {
                    e.Graphics.FillPath(brush, path);
                }

                using (Pen borderPen = new Pen(PrimaryPurple, 2))
                {
                    e.Graphics.DrawPath(borderPen, path);
                }
            }
        }

        private void BtnEnviar_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = GetRoundedRect(btnEnviar.ClientRectangle, 12))
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    btnEnviar.ClientRectangle, PrimaryPurple, SecondaryPurple, 135f))
                {
                    e.Graphics.FillPath(brush, path);
                }
            }

            TextRenderer.DrawText(e.Graphics, btnEnviar.Text, btnEnviar.Font,
                btnEnviar.ClientRectangle, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        // Helper para rounded rectangles
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

        // ✅ NOVO: Helper para botões 100% arredondados (pílula perfeita)
        private GraphicsPath GetRoundedRectPerfect(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();

            rect.Inflate(-1, -1);

            // Usa o menor lado do retângulo como base para o raio máximo
            int maxRadius = Math.Min(rect.Width, rect.Height) / 2;
            int effectiveRadius = Math.Min(radius, maxRadius);
            int diameter = effectiveRadius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        private void CenterWelcomePanel()
        {
            welcomePanel.Location = new Point(
                Math.Max(0, (messagesContainer.Width - welcomePanel.Width) / 2),
                Math.Max(0, (messagesContainer.Height - welcomePanel.Height) / 2)
            );
        }

        // ===== Timers e Animações =====

        private void ConfigurarTimers()
        {
            _timerHistorico.Interval = 5000;
            _timerHistorico.Tick += async (s, e) => await CarregarHistoricoAsync(true);
            _timerHistorico.Start();

            _timerPollingTecnico.Interval = 5000;
            _timerPollingTecnico.Tick += async (s, e) => await PollingTecnicoTickAsync();

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

            // Float animation (for welcome icon)
            if (_floatUp)
            {
                _floatOffset += 0.2f;
                if (_floatOffset >= 20f) _floatUp = false;
            }
            else
            {
                _floatOffset -= 0.2f;
                if (_floatOffset <= 0f) _floatUp = true;
            }

            liveIndicator?.Invalidate();
            welcomeIconContainer?.Invalidate();
        }

        private void IniciarPolling()
        {
            _timerPollingTecnico.Start();
            blockedNotice.Visible = true;
            blockedNotice.Height = 40;
            RecalcularLayout();
        }

        private void PararPolling()
        {
            _timerPollingTecnico.Stop();
            blockedNotice.Visible = false;
            blockedNotice.Height = 0;
            RecalcularLayout();
        }

        // ===== Histórico =====

        private async System.Threading.Tasks.Task CarregarHistoricoAsync(bool silencioso = false)
        {
            try
            {
                var hist = await _apiClient.ObterHistoricoAsync(_usuarioId);
                _historicoCache = hist ?? new List<ChatHistorico>();

                historyList.SuspendLayout();
                historyList.Controls.Clear();

                if (_historicoCache.Count == 0)
                {
                    var emptyLabel = new Label
                    {
                        Text = "Nenhum histórico ainda.\nComece uma conversa!",
                        ForeColor = Color.FromArgb(156, 163, 175),
                        Font = new Font("Segoe UI", 10f),
                        AutoSize = true,
                        TextAlign = ContentAlignment.TopCenter,
                        Padding = new Padding(0, 20, 0, 0)
                    };
                    historyList.Controls.Add(emptyLabel);
                }
                else
                {
                    foreach (var h in _historicoCache)
                    {
                        var item = CriarHistoryItem(h);
                        historyList.Controls.Add(item);
                    }
                }

                historyList.ResumeLayout();

                if (!silencioso)
                    Notificar("Histórico carregado.");
            }
            catch (Exception ex)
            {
                if (!silencioso)
                    MessageBox.Show("Erro ao carregar histórico: " + ex.Message, "Erro",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Panel CriarHistoryItem(ChatHistorico h)
        {
            var item = new Panel
            {
                Size = new Size(260, 70),
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 8),
                Cursor = Cursors.Hand,
                Tag = h
            };
            item.Paint += HistoryItem_Paint;
            item.Click += async (s, e) => await CarregarChatAsync(h);
            item.ContextMenuStrip = menuHistorico;

            string titulo = string.IsNullOrWhiteSpace(h.Titulo) ?
                (h.Pergunta.Length > 30 ? h.Pergunta.Substring(0, 30) + "..." : h.Pergunta) :
                (h.Titulo.Length > 25 ? h.Titulo.Substring(0, 25) + "..." : h.Titulo);

            var lblTitulo = new Label
            {
                Text = titulo,
                Location = new Point(12, 12),
                AutoSize = true,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            lblTitulo.Click += async (s, e) => await CarregarChatAsync(h);
            item.Controls.Add(lblTitulo);

            var lblStatus = new Label
            {
                Text = GetStatusText(h.Status),
                Location = new Point(12, 38),
                AutoSize = true,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                BackColor = Color.Transparent,
                Padding = new Padding(6, 2, 6, 2)
            };
            lblStatus.ForeColor = GetStatusColor(h.Status);
            lblStatus.Click += async (s, e) => await CarregarChatAsync(h);
            item.Controls.Add(lblStatus);
            var btnEdit = new Button
            {
                Text = "✏",
                Size = new Size(28, 28),
                Location = new Point(188, 20),
                Visible = false,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = ColorTranslator.FromHtml("#60a5fa"),
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Tag = h
            };
            btnEdit.FlatAppearance.BorderSize = 0;
            btnEdit.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnEdit.Paint += BtnHoverEdit_Paint;
            btnEdit.Click += async (s, e) =>
            {
                var chat = (ChatHistorico)((Button)s).Tag;
                await EditarTituloAsync(chat);
            };
            item.Controls.Add(btnEdit);

            var btnDelete = new Button
            {
                Text = "🗑",
                Size = new Size(28, 28),
                Location = new Point(222, 20),
                Visible = false,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = ColorTranslator.FromHtml("#ef4444"),
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Tag = h
            };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnDelete.Paint += BtnHoverDelete_Paint;
            btnDelete.Click += async (s, e) =>
            {
                var chat = (ChatHistorico)((Button)s).Tag;
                await ExcluirAsync(chat);
            };
            item.Controls.Add(btnDelete);

            // Mouse events
            item.MouseEnter += (s, e) =>
            {
                btnEdit.Visible = true;
                btnDelete.Visible = true;
            };

            item.MouseLeave += (s, e) =>
            {
                Point mousePos = item.PointToClient(Cursor.Position);
                if (!item.ClientRectangle.Contains(mousePos))
                {
                    btnEdit.Visible = false;
                    btnDelete.Visible = false;
                }
            };

            return item;
        }

        private void HistoryItem_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Panel item = (Panel)sender;
            ChatHistorico h = item.Tag as ChatHistorico;
            Color bgColor = (h != null && h.Id == _chatIdAtual) ? BorderColor : CardBg;

            using (GraphicsPath path = GetRoundedRect(item.ClientRectangle, 12))
            {
                // ✅ CORREÇÃO: Usa bgColor em vez de item.BackColor
                using (SolidBrush brush = new SolidBrush(bgColor))
                {
                    e.Graphics.FillPath(brush, path);
                }

                // Sempre desenha borda arredondada
                if (h != null && h.Id == _chatIdAtual)
                {
                    // Item selecionado - borda roxa grossa
                    using (Pen borderPen = new Pen(SecondaryPurple, 2))
                    {
                        e.Graphics.DrawPath(borderPen, path);
                    }
                }
                else
                {
                    // Item não selecionado - borda sutil
                    using (Pen borderPen = new Pen(BorderColor, 1))
                    {
                        e.Graphics.DrawPath(borderPen, path);
                    }
                }
            }
        }

        private void BtnHoverEdit_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            Button btn = (Button)sender;

            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(btn.ClientRectangle);

                using (SolidBrush brush = new SolidBrush(Color.FromArgb(50, 96, 165, 250)))
                {
                    e.Graphics.FillPath(brush, path);
                }

                using (Pen borderPen = new Pen(ColorTranslator.FromHtml("#60a5fa"), 2))
                {
                    e.Graphics.DrawPath(borderPen, path);
                }
            }

            // ✅ Centraliza melhor o emoji
            using (StringFormat sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            })
            {
                e.Graphics.DrawString(btn.Text, btn.Font,
                    new SolidBrush(btn.ForeColor),
                    btn.ClientRectangle, sf);
            }
        }

        private void BtnHoverDelete_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            Button btn = (Button)sender;

            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(btn.ClientRectangle);

                using (SolidBrush brush = new SolidBrush(Color.FromArgb(50, 239, 68, 68)))
                {
                    e.Graphics.FillPath(brush, path);
                }

                using (Pen borderPen = new Pen(ColorTranslator.FromHtml("#ef4444"), 2))
                {
                    e.Graphics.DrawPath(borderPen, path);
                }
            }

            // ✅ Centraliza melhor o emoji
            using (StringFormat sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            })
            {
                e.Graphics.DrawString(btn.Text, btn.Font,
                    new SolidBrush(btn.ForeColor),
                    btn.ClientRectangle, sf);
            }
        }

        private string GetStatusText(int status)
        {
            return status switch
            {
                2 => "Concluído",
                3 => "Pendente",
                4 => "Resolvido",
                _ => "Em andamento"
            };
        }

        private Color GetStatusColor(int status)
        {
            return status switch
            {
                2 => GreenTech,
                3 => ColorTranslator.FromHtml("#fbbf24"),
                4 => ColorTranslator.FromHtml("#8b5cf6"),
                _ => ColorTranslator.FromHtml("#3b82f6")
            };
        }

        private ChatHistorico _chatSelecionadoMenu;

        private async System.Threading.Tasks.Task EditarTituloSelecionadoAsync()
        {
            if (historyList.ContextMenuStrip == null || menuHistorico.SourceControl == null)
                return;

            var item = menuHistorico.SourceControl as Panel;
            if (item == null || !(item.Tag is ChatHistorico h))
                return;

            _chatSelecionadoMenu = h;

            var novo = Prompt("Novo título do chat:", h.Titulo ?? "");
            if (string.IsNullOrWhiteSpace(novo)) return;

            var ok = await _apiClient.EditarTituloChatAsync(h.Id, novo);
            if (ok)
            {
                Notificar("Nome atualizado!");
                await CarregarHistoricoAsync(true);
                if (h.Id == _chatIdAtual) lblChatTitle.Text = novo;
            }
            else
            {
                Notificar("Falha ao atualizar nome.");
            }
        }

        private async System.Threading.Tasks.Task ExcluirSelecionadoAsync()
        {
            var item = menuHistorico.SourceControl as Panel;
            if (item == null || !(item.Tag is ChatHistorico h))
                return;

            if (MessageBox.Show("Excluir esta conversa?", "Confirmação",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            var ok = await _apiClient.ExcluirChatAsync(h.Id);
            if (ok)
            {
                Notificar("Chat excluído.");
                if (h.Id == _chatIdAtual) NovoChat();
                await CarregarHistoricoAsync(true);
            }
            else
            {
                Notificar("Falha ao excluir.");
            }
        }

        private async System.Threading.Tasks.Task EditarTituloAsync(ChatHistorico h)
        {
            var novo = Prompt("Novo título do chat:", h.Titulo ?? "");
            if (string.IsNullOrWhiteSpace(novo)) return;

            var ok = await _apiClient.EditarTituloChatAsync(h.Id, novo);
            if (ok)
            {
                Notificar("Nome atualizado!");
                await CarregarHistoricoAsync(true);
                if (h.Id == _chatIdAtual) lblChatTitle.Text = novo;
            }
            else
            {
                Notificar("Falha ao atualizar nome.");
            }
        }

        private async System.Threading.Tasks.Task ExcluirAsync(ChatHistorico h)
        {
            string titulo = string.IsNullOrWhiteSpace(h.Titulo) ?
                h.Pergunta.Substring(0, Math.Min(30, h.Pergunta.Length)) : h.Titulo;

            if (MessageBox.Show($"Excluir conversa '{titulo}'?", "Confirmação",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            var ok = await _apiClient.ExcluirChatAsync(h.Id);
            if (ok)
            {
                Notificar("Chat excluído.");
                if (h.Id == _chatIdAtual) NovoChat();
                await CarregarHistoricoAsync(true);
            }
            else
            {
                Notificar("Falha ao excluir.");
            }
        }

        // ===== Chat =====

        private void ParsearMensagensChat(string texto, string tipo)
        {
            if (string.IsNullOrWhiteSpace(texto)) return;

            // Regex para detectar timestamps [dd/MM/yyyy HH:mm]
            var regexTimestamp = new System.Text.RegularExpressions.Regex(
                @"\[(\d{2}/\d{2}/\d{4}\s\d{2}:\d{2})\]",
                System.Text.RegularExpressions.RegexOptions.None);

            var matches = regexTimestamp.Matches(texto);

            if (matches.Count > 0)
            {
                // Tem timestamps - mensagens concatenadas
                var partes = regexTimestamp.Split(texto);
                for (int i = 1; i < partes.Length; i += 2)
                {
                    // i é o timestamp, i+1 é o conteúdo
                    if (i + 1 < partes.Length)
                    {
                        var mensagem = partes[i + 1].Trim();
                        if (!string.IsNullOrWhiteSpace(mensagem))
                        {
                            AdicionarMensagem(mensagem, tipo);
                        }
                    }
                }
            }
            else
            {
                // Mensagem única (primeira vez)
                AdicionarMensagem(texto, tipo);
            }
        }

        private async System.Threading.Tasks.Task CarregarChatAsync(ChatHistorico h)
        {
            messagesList.SuspendLayout();
            messagesList.Controls.Clear();

            welcomePanel.Visible = false;
            welcomePanel.SendToBack();

            messagesList.Visible = true;
            messagesList.BringToFront();

            _mensagensTecnicoProcessadas.Clear();

            _chatIdAtual = h.Id;
            _chatStatusAtual = h.Status;

            // ✅ CORREÇÃO: Processa perguntas e respostas concatenadas (mesmo padrão do Mobile)
            ParsearMensagensChat(h.Pergunta, "user");
            ParsearMensagensChat(h.Resposta, "bot");

            _ultimaPergunta = h.Pergunta;
            _ultimaResposta = h.Resposta;

            lblChatTitle.Text = string.IsNullOrWhiteSpace(h.Titulo) ? "Chat" : h.Titulo;

            RenderFeedbackPorStatus(h.Status);

            if (h.IdTicket.HasValue)
            {
                var vr = await _apiClient.VerificarRespostaAsync(h.Id);
                if (vr != null && vr.TemResposta && !string.IsNullOrWhiteSpace(vr.Solucao))
                {
                    ProcessarMensagensTecnico(vr.Solucao);
                    if (vr.StatusTicket == 1 && h.Status == 3)
                        IniciarPolling();
                }
                else if (h.Status == 3)
                {
                    IniciarPolling();
                }
            }
            else
            {
                PararPolling();
            }

            messagesList.ResumeLayout();
            ScrollToBottom();

            // Atualizar histórico visual
            await CarregarHistoricoAsync(true);
        }

        private void RenderFeedbackPorStatus(int status)
        {
            feedbackArea.Visible = false;
            feedbackArea.Height = 0;
            blockedNotice.Visible = false;
            blockedNotice.Height = 0;

            // Habilita input por padrão
            txtMensagem.Enabled = true;
            btnEnviar.Enabled = true;
            txtMensagem.BackColor = DarkBg; // ✅ Sempre usa DarkBg

            if (status == 1)
            {
                AtualizarLayoutFeedbackArea();
                feedbackArea.Height = 80;
                feedbackArea.Visible = true;
                btnFeedbackUtil.Enabled = true;
                btnFeedbackNaoUtil.Visible = true;
                btnFeedbackUtil.Tag = "👍 Útil";  // ✅ USAR TAG
                btnFeedbackUtil.ForeColor = TextColor;  // ✅ Restaura cor original
                btnFeedbackUtil.Invalidate();  // ✅ Força redesenho
                txtMensagem.Text = "";
            }
            else if (status == 2)
            {
                AtualizarLayoutFeedbackArea();
                feedbackArea.Height = 80;
                feedbackArea.Visible = true;
                btnFeedbackUtil.Enabled = false;
                btnFeedbackUtil.Tag = "✅ Marcado\ncomo útil";
                btnFeedbackUtil.ForeColor = ColorTranslator.FromHtml("#10b981");
                btnFeedbackNaoUtil.Visible = false;

                // Bloqueia input
                txtMensagem.Enabled = false;
                btnEnviar.Enabled = false;
                txtMensagem.BackColor = DarkBg; // ✅ Mantém DarkBg mesmo desabilitado
                txtMensagem.Text = "✓ Chat resolvido - Inicie nova conversa";
            }
            else if (status == 3)
            {
                blockedNotice.Visible = true;
                blockedNotice.Height = 40;
                txtMensagem.Text = ""; // ✅ Limpa o texto
            }
            else if (status == 4)
            {
                AtualizarLayoutFeedbackArea();
                feedbackArea.Height = 80;
                feedbackArea.Visible = true;
                btnFeedbackUtil.Enabled = false;
                btnFeedbackUtil.Tag = "✅ Resolvido\npelo técnico";
                btnFeedbackUtil.ForeColor = ColorTranslator.FromHtml("#8b5cf6");
                btnFeedbackNaoUtil.Visible = false;

                // Bloqueia input
                txtMensagem.Enabled = false;
                btnEnviar.Enabled = false;
                txtMensagem.BackColor = DarkBg; // ✅ Mantém DarkBg mesmo desabilitado
                txtMensagem.Text = "✓ Chat resolvido - Inicie nova conversa";
            }

            // Recalcula layout para ajustar tudo
            RecalcularLayout();
        }

        private void AtualizarLayoutFeedbackArea()
        {
            int panelWidth = chatArea.ClientSize.Width - 60;

            // ✅ MUDOU: Atualiza para novos tamanhos
            int totalButtonsWidth = 180 + 20 + 180; // 380 total (era 170 + 20 + 170)
            int startX = Math.Max(20, (panelWidth - totalButtonsWidth) / 2);

            btnFeedbackUtil.Location = new Point(startX, 11);
            btnFeedbackNaoUtil.Location = new Point(startX + 200, 11); // ✅ MUDOU de 190 para 200
        }

        private void AdicionarMensagem(string texto, string tipo)
        {
            int maxWidth = (int)((messagesContainer.Width - 60) * 0.7);

            // Avatar com círculo ao redor
            int avatarSize = tipo == "user" ? 44 : 54;
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
                avatar.Paint += UserAvatar_Paint;
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

            // Bubble com texto
            var lblTexto = new Label
            {
                Text = texto,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 11f),
                AutoSize = true,
                MaximumSize = new Size(maxWidth - avatarSize - 70, 0),
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };

            var bubble = new Panel
            {
                AutoSize = false,
                Width = lblTexto.PreferredWidth + 40,
                Height = lblTexto.PreferredHeight + 32,
                Padding = new Padding(20, 16, 20, 16),
                BackColor = Color.Transparent
            };
            bubble.Tag = tipo;
            bubble.Paint += MessageBubble_Paint;

            lblTexto.Location = new Point(20, 16);
            bubble.Controls.Add(lblTexto);

            // Container da linha (avatar + bubble)
            var messageRow = new Panel
            {
                Width = messagesList.ClientSize.Width - 60, // Subtrai padding
                Height = Math.Max(avatarContainer.Height, bubble.Height) + 25,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 10)
            };

            // Posiciona avatar e bubble
            if (tipo == "user")
            {
                // Usuário: bubble à direita, avatar depois
                int bubbleX = messageRow.Width - bubble.Width - avatarContainer.Width - 10;
                bubble.Location = new Point(bubbleX, 0);
                avatarContainer.Location = new Point(messageRow.Width - avatarContainer.Width, 0);
            }
            else
            {
                // Bot/Tech: avatar à esquerda, bubble depois
                avatarContainer.Location = new Point(0, 0);
                bubble.Location = new Point(avatarContainer.Width + 10, 0);
            }

            messageRow.Controls.Add(avatarContainer);
            messageRow.Controls.Add(bubble);
            messagesList.Controls.Add(messageRow);
        }

        private void AvatarCircle_Paint(PaintEventArgs e, string tipo, int avatarSize)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Círculo ao redor do avatar (inset -6px na web)
            Rectangle circleRect = new Rectangle(0, 0, avatarSize + 12, avatarSize + 12);

            using (GraphicsPath circlePath = new GraphicsPath())
            {
                circlePath.AddEllipse(circleRect);

                // Background do círculo com gradiente radial
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
                        circleBrush.CenterColor = Color.FromArgb(70, GreenTech);
                        circleBrush.SurroundColors = new[] { Color.FromArgb(25, ColorTranslator.FromHtml("#34d399")) };
                    }

                    e.Graphics.FillPath(circleBrush, circlePath);
                }

                // Borda suave
                using (Pen borderPen = new Pen(Color.FromArgb(15, 255, 255, 255), 1))
                {
                    e.Graphics.DrawPath(borderPen, circlePath);
                }

                // Shadow
                using (PathGradientBrush shadowBrush = new PathGradientBrush(circlePath))
                {
                    shadowBrush.CenterColor = Color.FromArgb(90, 0, 0, 0);
                    shadowBrush.SurroundColors = new[] { Color.Transparent };
                    e.Graphics.FillPath(shadowBrush, circlePath);
                }
            }
        }

        private void MessageBubble_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Panel bubble = (Panel)sender;
            string tipo = bubble.Tag as string;

            // Criar caminho com cantos arredondados (18px, mas um canto 4px)
            GraphicsPath path = new GraphicsPath();
            Rectangle rect = bubble.ClientRectangle;
            rect.Inflate(-1, -1);

            int radius = 18;
            int smallRadius = 4;

            if (tipo == "user")
            {
                // User: canto inferior direito é pequeno (4px)
                path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
                path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
                path.AddArc(rect.Right - smallRadius * 2, rect.Bottom - smallRadius * 2, smallRadius * 2, smallRadius * 2, 0, 90);
                path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            }
            else
            {
                // Bot/Tech: canto inferior esquerdo é pequeno (4px)
                path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
                path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
                path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
                path.AddArc(rect.X, rect.Bottom - smallRadius * 2, smallRadius * 2, smallRadius * 2, 90, 90);
            }
            path.CloseFigure();

            // Preencher com cor/gradiente
            if (tipo == "user")
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    rect, PrimaryPurple, ColorTranslator.FromHtml("#7c3aed"), 135f))
                {
                    e.Graphics.FillPath(brush, path);
                }
            }
            else if (tipo == "tech")
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    rect, ColorTranslator.FromHtml("#059669"), ColorTranslator.FromHtml("#047857"), 135f))
                {
                    e.Graphics.FillPath(brush, path);
                }
            }
            else
            {
                using (SolidBrush brush = new SolidBrush(CardBg))
                {
                    e.Graphics.FillPath(brush, path);
                }
            }

            path.Dispose();
        }

        private void BotAvatar_Paint(object sender, PaintEventArgs e)
        {
            // Neon glow para PNG do bot
            PictureBox pb = (PictureBox)sender;
            if (pb.Image == null) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        }


        private void TechAvatar_Paint(object sender, PaintEventArgs e)
        {
            // Neon glow verde para PNG do técnico
            PictureBox pb = (PictureBox)sender;
            if (pb.Image == null) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        }

        private void UserAvatar_Paint(object sender, PaintEventArgs e)
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

        // ===== Envio =====

        private async System.Threading.Tasks.Task EnviarMensagemAsync()
        {
            var msg = txtMensagem.Text.Trim();
            if (string.IsNullOrEmpty(msg)) return;

            welcomePanel.Visible = false;
            welcomePanel.SendToBack();

            messagesList.Visible = true;
            messagesList.BringToFront();

            AdicionarMensagem(msg, "user");
            txtMensagem.Clear();
            ScrollToBottom();

            btnEnviar.Enabled = false;

            // Adiciona typingIndicator ao messagesList
            typingIndicator.Width = messagesList.ClientSize.Width - 60;
            if (!messagesList.Controls.Contains(typingIndicator))
            {
                messagesList.Controls.Add(typingIndicator);
            }
            typingIndicator.Visible = true;
            typingIndicator.BringToFront();
            ScrollToBottom();

            try
            {
                if (_chatStatusAtual == 3 && _chatIdAtual > 0)
                {
                    var ok = await _apiClient.EnviarMensagemParaTecnicoAsync(_chatIdAtual, msg);

                    // Remove typingIndicator
                    if (messagesList.Controls.Contains(typingIndicator))
                    {
                        messagesList.Controls.Remove(typingIndicator);
                    }
                    typingIndicator.Visible = false;
                    btnEnviar.Enabled = true;

                    if (ok)
                        Notificar("Mensagem enviada ao técnico.");
                    else
                        Notificar("Falha ao enviar ao técnico.");
                    return;
                }

                var resp = await _apiClient.EnviarPerguntaAsync(_usuarioId, msg, _chatIdAtual > 0 ? _chatIdAtual : (int?)null);

                // Remove typingIndicator
                if (messagesList.Controls.Contains(typingIndicator))
                {
                    messagesList.Controls.Remove(typingIndicator);
                }
                typingIndicator.Visible = false;
                btnEnviar.Enabled = true;

                if (resp.Sucesso)
                {
                    _chatIdAtual = resp.ChatId;
                    _chatStatusAtual = 1;
                    _ultimaPergunta = msg;
                    _ultimaResposta = resp.Resposta;

                    AdicionarMensagem(resp.Resposta, "bot");
                    RenderFeedbackPorStatus(1);

                    await CarregarHistoricoAsync(true);
                }
                else
                {
                    AdicionarMensagem("Desculpe, ocorreu um erro ao processar sua pergunta.", "bot");
                }
            }
            catch (Exception ex)
            {
                // Remove typingIndicator
                if (messagesList.Controls.Contains(typingIndicator))
                {
                    messagesList.Controls.Remove(typingIndicator);
                }
                typingIndicator.Visible = false;
                btnEnviar.Enabled = true;
                AdicionarMensagem("Erro de conexão com o servidor.", "bot");
                Console.WriteLine(ex);
            }
            finally
            {
                ScrollToBottom();
            }
        }

        private async System.Threading.Tasks.Task AvaliarChatAsync(bool foiUtil)
        {
            if (_chatIdAtual <= 0) return;

            btnFeedbackUtil.Enabled = false;
            btnFeedbackNaoUtil.Enabled = false;

            var ok = await _apiClient.AvaliarRespostaAsync(_usuarioId, _ultimaPergunta, _ultimaResposta, foiUtil, _chatIdAtual);
            if (!ok)
            {
                Notificar("Falha ao registrar avaliação.");
                btnFeedbackUtil.Enabled = true;
                btnFeedbackNaoUtil.Enabled = true;
                return;
            }

            if (foiUtil)
            {
                _chatStatusAtual = 2;
                RenderFeedbackPorStatus(2);
                Notificar("Obrigado pelo feedback!");
            }
            else
            {
                _chatStatusAtual = 3;
                RenderFeedbackPorStatus(3);
                Notificar("Ticket criado! Um técnico irá atender.");
                IniciarPolling();
            }

            await CarregarHistoricoAsync(true);
        }

        // ===== Polling técnico =====

        private async System.Threading.Tasks.Task PollingTecnicoTickAsync()
        {
            try
            {
                if (_chatIdAtual <= 0) return;
                var vr = await _apiClient.VerificarRespostaAsync(_chatIdAtual);
                if (vr == null) return;

                if (vr.TemResposta && !string.IsNullOrWhiteSpace(vr.Solucao))
                {
                    ProcessarMensagensTecnico(vr.Solucao);
                    ScrollToBottom();

                    if (vr.StatusTicket == 2)
                    {
                        PararPolling();
                        _chatStatusAtual = 4;
                        RenderFeedbackPorStatus(4);
                        await CarregarHistoricoAsync(true);
                        Notificar("✅ Técnico finalizou este atendimento.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro polling: " + ex.Message);
            }
        }

        private void ProcessarMensagensTecnico(string solucao)
        {
            var blocos = solucao.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var bloco in blocos)
            {
                var m = bloco.Trim();
                if (string.IsNullOrEmpty(m)) continue;
                if (_mensagensTecnicoProcessadas.Contains(m)) continue;

                var usuarioRegex = new System.Text.RegularExpressions.Regex(@"^\[USUÁRIO\s*-\s*\d{2}/\d{2}/\d{4}\s+\d{2}:\d{2}\]\s*");
                var tecnicoRegex = new System.Text.RegularExpressions.Regex(@"^\[TÉCNICO\s*-\s*\d{2}/\d{2}/\d{4}\s+\d{2}:\d{2}\]\s*");

                var isUsuario = usuarioRegex.IsMatch(m);
                var isTecnico = tecnicoRegex.IsMatch(m);

                var limpo = usuarioRegex.Replace(m, "");
                limpo = tecnicoRegex.Replace(limpo, "");

                if (string.IsNullOrWhiteSpace(limpo)) continue;

                if (isTecnico)
                    AdicionarMensagem(limpo, "tech");
                else if (isUsuario)
                    AdicionarMensagem(limpo, "user");
                else
                    AdicionarMensagem(limpo, "tech");

                _mensagensTecnicoProcessadas.Add(m);
            }
        }

        // ===== Ações auxiliares =====

        private void NovoChat()
        {
            PararPolling();
            _chatIdAtual = 0;
            _chatStatusAtual = null;
            _mensagensTecnicoProcessadas.Clear();

            messagesList.Controls.Clear();
            messagesList.Visible = false;
            messagesList.SendToBack();

            welcomePanel.Visible = true;
            welcomePanel.BringToFront();

            lblChatTitle.Text = "DotIA - Assistente Inteligente";
            txtMensagem.Clear();
            blockedNotice.Visible = false;
            blockedNotice.Height = 0;
            feedbackArea.Visible = false;
            feedbackArea.Height = 0;
            RecalcularLayout();

            CarregarHistoricoAsync(true);
        }

        private void AbrirTicketDiretoDialog()
        {
            using var f = new Form
            {
                Text = "Abrir Ticket Direto",
                StartPosition = FormStartPosition.CenterParent,
                BackColor = DarkerBg,
                ForeColor = TextColor,
                Size = new Size(600, 500),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                Font = new Font("Segoe UI", 10f)
            };

            var lblT = new Label
            {
                Text = "Título do Problema:",
                AutoSize = true,
                Location = new Point(30, 30),
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold)
            };
            f.Controls.Add(lblT);

            // Wrapper para txtT com cantos arredondados
            var txtTWrapper = new Panel
            {
                Location = new Point(30, 60),
                Size = new Size(540, 50),
                BackColor = Color.Transparent
            };
            txtTWrapper.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = GetRoundedRect(txtTWrapper.ClientRectangle, 12))
                {
                    using (SolidBrush brush = new SolidBrush(CardBg))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                    using (Pen borderPen = new Pen(BorderColor, 2))
                    {
                        e.Graphics.DrawPath(borderPen, path);
                    }
                }
            };

            var txtT = new TextBox
            {
                Location = new Point(15, 12),
                Size = new Size(510, 26),
                BackColor = CardBg,
                ForeColor = TextColor,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 11f)
            };
            txtTWrapper.Controls.Add(txtT);
            f.Controls.Add(txtTWrapper);

            var lblD = new Label
            {
                Text = "Descrição Detalhada:",
                AutoSize = true,
                Location = new Point(30, 130),
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold)
            };
            f.Controls.Add(lblD);

            // Wrapper para txtD com cantos arredondados
            var txtDWrapper = new Panel
            {
                Location = new Point(30, 160),
                Size = new Size(540, 220),
                BackColor = Color.Transparent
            };
            txtDWrapper.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = GetRoundedRect(txtDWrapper.ClientRectangle, 12))
                {
                    using (SolidBrush brush = new SolidBrush(CardBg))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                    using (Pen borderPen = new Pen(BorderColor, 2))
                    {
                        e.Graphics.DrawPath(borderPen, path);
                    }
                }
            };

            var txtD = new TextBox
            {
                Location = new Point(15, 15),
                Size = new Size(510, 190),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = CardBg,
                ForeColor = TextColor,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 11f)
            };
            txtDWrapper.Controls.Add(txtD);
            f.Controls.Add(txtDWrapper);

            var btnCancel = new Button
            {
                Text = "Cancelar",
                Location = new Point(300, 410),
                Size = new Size(120, 45),
                BackColor = CardBg,
                ForeColor = TextColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = BorderColor;
            btnCancel.FlatAppearance.BorderSize = 2;
            btnCancel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = GetRoundedRect(btnCancel.ClientRectangle, 12))
                {
                    using (SolidBrush brush = new SolidBrush(CardBg))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                    using (Pen borderPen = new Pen(BorderColor, 2))
                    {
                        e.Graphics.DrawPath(borderPen, path);
                    }
                }
                TextRenderer.DrawText(e.Graphics, btnCancel.Text, btnCancel.Font,
                    btnCancel.ClientRectangle, btnCancel.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            btnCancel.Click += (s, e) => f.DialogResult = DialogResult.Cancel;
            f.Controls.Add(btnCancel);

            var btnOk = new RoundedButton
            {
                Text = "🎫  Abrir Ticket",
                Location = new Point(435, 410),
                Size = new Size(135, 45),
                BorderRadius = 12,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold)
            };

            btnOk.Click += async (s, e) =>
            {
                var titulo = txtT.Text.Trim();
                var descricao = txtD.Text.Trim();
                if (titulo.Length < 5) { MessageBox.Show("Informe um título (mín. 5 caracteres)."); return; }
                if (descricao.Length < 20) { MessageBox.Show("Descreva melhor o problema (mín. 20 caracteres)."); return; }

                btnOk.Enabled = false;
                var resp = await _apiClient.AbrirTicketDiretoAsync(_usuarioId, titulo, descricao);
                btnOk.Enabled = true;

                if (resp != null && resp.Sucesso)
                {
                    Notificar("Ticket criado com sucesso.");
                    f.DialogResult = DialogResult.OK;
                    await CarregarHistoricoAsync(true);

                    var chatCriado = _historicoCache.FirstOrDefault(c => c.Id == resp.ChatId);
                    if (chatCriado != null)
                        await CarregarChatAsync(chatCriado);
                }
                else
                {
                    MessageBox.Show(resp?.Mensagem ?? "Erro ao abrir ticket.");
                }
            };
            f.Controls.Add(btnOk);

            f.ShowDialog(this);
        }

        private void Notificar(string texto)
        {
            var t = new Label
            {
                Text = texto,
                AutoSize = true,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(160, 50, 50, 50),
                Padding = new Padding(15, 10, 15, 10),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Location = new Point(Width - 300, 20)
            };

            Controls.Add(t);
            t.BringToFront();

            var timer = new WinTimer { Interval = 3000 };
            timer.Tick += (s, e) => { if (Controls.Contains(t)) Controls.Remove(t); timer.Stop(); timer.Dispose(); };
            timer.Start();
        }

        private static string Prompt(string title, string defaultValue = "")
        {
            using var f = new Form
            {
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                Size = new Size(400, 150),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var tb = new TextBox { Left = 12, Top = 12, Width = 360, Text = defaultValue };
            var ok = new Button { Text = "OK", Left = 212, Width = 75, Top = 50, DialogResult = DialogResult.OK };
            var cancel = new Button { Text = "Cancelar", Left = 297, Width = 75, Top = 50, DialogResult = DialogResult.Cancel };

            f.Controls.Add(tb);
            f.Controls.Add(ok);
            f.Controls.Add(cancel);
            f.AcceptButton = ok;

            return f.ShowDialog() == DialogResult.OK ? tb.Text : defaultValue;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timerHistorico?.Stop();
                _timerHistorico?.Dispose();
                _timerPollingTecnico?.Stop();
                _timerPollingTecnico?.Dispose();
                _timerAnimacoes?.Stop();
                _timerAnimacoes?.Dispose();

                _logoImage?.Dispose();
                _botAvatarImage?.Dispose();
                _tecnicoAvatarImage?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}