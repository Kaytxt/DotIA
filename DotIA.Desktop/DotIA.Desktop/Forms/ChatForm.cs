using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DotIA.Desktop.Services;
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
        private int? _chatStatusAtual = null; // 1=aguardando avaliação, 2=avaliado útil, 3=pendente técnico, 4=resolver
        private readonly HashSet<string> _mensagensTecnicoProcessadas = new HashSet<string>();

        // Timers
        private readonly WinTimer _timerHistorico = new WinTimer();
        private readonly WinTimer _timerPollingTecnico = new WinTimer();

        // Paleta
        private readonly Color PrimaryPurple = ColorTranslator.FromHtml("#8d4bff");
        private readonly Color SecondaryPurple = ColorTranslator.FromHtml("#a855f7");
        private readonly Color DarkBg = ColorTranslator.FromHtml("#1a132f");
        private readonly Color DarkerBg = ColorTranslator.FromHtml("#1e1433");
        private readonly Color CardBg = ColorTranslator.FromHtml("#2c204d");
        private readonly Color BorderColor = ColorTranslator.FromHtml("#3d2e6b");
        private readonly Color TextColor = ColorTranslator.FromHtml("#e5e7eb");

        // LAYOUT ------------------------------------------
        private SplitContainer rootSplit; // sidebar | main

        // Sidebar
        private Panel sidebarHeader;
        private Panel sidebarButtons;
        private Button btnAtualizar;
        private Button btnNovoChat;
        private Button btnAbrirTicket;
        private Label lblLive;
        private ListBox lstHistorico; // lista dos chats
        private ContextMenuStrip menuHistorico; // editar / excluir
        private Panel sidebarUser;

        // Main chat area
        private Panel chatHeader;
        private Label lblChatTitle;
        private Button btnRefreshHeader;

        private Panel messagesContainer;   // scroll
        private FlowLayoutPanel messagesList; // onde entram as mensagens
        private Panel welcomePanel;

        private Panel feedbackArea; // 👍👎
        private Button btnFeedbackUtil;
        private Button btnFeedbackNaoUtil;

        private Panel typingIndicator;

        private Panel inputArea;
        private Panel inputWrapper;
        private TextBox txtMensagem;
        private Button btnEnviar;
        private Panel blockedNotice; // aviso técnico

        // Última pergunta/resposta (para avaliação)
        private string _ultimaPergunta = string.Empty;
        private string _ultimaResposta = string.Empty;

        public ChatForm(int usuarioId, string nomeUsuario)
        {
            _usuarioId = usuarioId;
            _nomeUsuario = nomeUsuario;
            _apiClient = new ApiClient();

            InitializeComponent();
            MontarLayout();
            ConfigurarTimers();

            CarregarHistoricoAsync();
        }

        private void InitializeComponent()
        {
            Text = "DotIA - Chat";
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = DarkBg;
            Size = new Size(1600, 900);
            FormBorderStyle = FormBorderStyle.Sizable;
            WindowState = FormWindowState.Maximized;
            MaximizeBox = true;
        }

        private void MontarLayout()
        {
            // SPLIT ROOT
            rootSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterWidth = 2,
                BackColor = BorderColor,
                IsSplitterFixed = false,
                FixedPanel = FixedPanel.Panel1
            };
            Controls.Add(rootSplit);

            rootSplit.Panel1MinSize = 280;
            rootSplit.Panel1.BackColor = DarkBg;
            rootSplit.Panel2.BackColor = DarkerBg;
            rootSplit.SplitterDistance = 320;

            // ===== Sidebar =====
            var sidebar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = DarkBg,
                ColumnCount = 1,
                RowCount = 5
            };
            sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));   // header
            sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));   // atualizar
            sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));   // novo chat
            sidebar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // histórico
            sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));   // user info
            rootSplit.Panel1.Controls.Add(sidebar);

            // Header (logo)
            sidebarHeader = new Panel { Dock = DockStyle.Fill, BackColor = DarkBg, Padding = new Padding(16, 16, 16, 8) };
            var logoIcon = new Panel
            {
                Size = new Size(40, 40),
                BackColor = PrimaryPurple,
                Margin = new Padding(0, 0, 10, 0)
            };
            var lblLogoText = new Label
            {
                Text = "DotIA",
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                AutoSize = true
            };
            var headerWrap = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            headerWrap.Controls.Add(logoIcon);
            headerWrap.Controls.Add(lblLogoText);
            sidebarHeader.Controls.Add(headerWrap);
            sidebar.Controls.Add(sidebarHeader, 0, 0);

            // Botão Atualizar
            btnAtualizar = BuildSidebarButton("Atualizar", "⟳");
            btnAtualizar.Click += async (s, e) =>
            {
                btnAtualizar.Enabled = false;
                await CarregarHistoricoAsync(true);
                btnAtualizar.Enabled = true;
                Notificar("✅ Atualizado!");
            };
            sidebar.Controls.Add(Wrap(btnAtualizar, 16, 8), 0, 1);

            // Botão Novo Chat
            btnNovoChat = BuildPrimarySidebarButton("Novo Chat", "＋");
            btnNovoChat.Click += (s, e) => NovoChat();
            sidebar.Controls.Add(Wrap(btnNovoChat, 16, 8), 0, 2);

            // Botão Abrir Ticket Direto
            btnAbrirTicket = BuildWarningSidebarButton("Abrir Ticket", "🎫");
            btnAbrirTicket.Click += (s, e) => AbrirTicketDiretoDialog();
            sidebar.Controls.Add(Wrap(btnAbrirTicket, 16, 8), 0, 2); // ocupa mesma row com margem extra
            sidebar.SetCellPosition(btnAbrirTicket.Parent, new TableLayoutPanelCellPosition(0, 2));

            // Histórico
            var historyPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };
            lstHistorico = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = CardBg,
                ForeColor = TextColor,
                IntegralHeight = false
            };
            lstHistorico.DrawMode = DrawMode.OwnerDrawFixed;
            lstHistorico.ItemHeight = 56;
            lstHistorico.DrawItem += LstHistorico_DrawItem;
            lstHistorico.DoubleClick += async (s, e) => await AbrirHistoricoSelecionadoAsync();

            // Context menu (editar / excluir)
            menuHistorico = new ContextMenuStrip();
            var miEditar = new ToolStripMenuItem("Editar título", null, async (s, e) => await EditarTituloSelecionadoAsync());
            var miExcluir = new ToolStripMenuItem("Excluir", null, async (s, e) => await ExcluirSelecionadoAsync());
            menuHistorico.Items.Add(miEditar);
            menuHistorico.Items.Add(miExcluir);
            lstHistorico.ContextMenuStrip = menuHistorico;

            historyPanel.Controls.Add(lstHistorico);
            sidebar.Controls.Add(historyPanel, 0, 3);

            // User info
            sidebarUser = new Panel { Dock = DockStyle.Fill, BackColor = CardBg, Padding = new Padding(16) };
            var lblUser = new Label
            {
                Text = $"{_nomeUsuario}\n● Online",
                ForeColor = TextColor,
                AutoSize = true
            };
            sidebarUser.Controls.Add(lblUser);
            sidebar.Controls.Add(sidebarUser, 0, 4);

            // ===== Chat Area =====
            var main = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = DarkerBg,
                ColumnCount = 1,
                RowCount = 3
            };
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 64)); // header
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // messages
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 110)); // input
            rootSplit.Panel2.Controls.Add(main);

            // Chat header
            chatHeader = new Panel { Dock = DockStyle.Fill, BackColor = DarkBg, Padding = new Padding(20, 12, 20, 12) };
            lblChatTitle = new Label
            {
                Text = "DotIA - Assistente Inteligente",
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                AutoSize = true,
                Dock = DockStyle.Left
            };
            btnRefreshHeader = new Button
            {
                Text = "⟳",
                ForeColor = TextColor,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Right,
                Width = 44
            };
            btnRefreshHeader.FlatAppearance.BorderSize = 0;
            btnRefreshHeader.Click += async (s, e) => await CarregarHistoricoAsync(true);
            chatHeader.Controls.Add(btnRefreshHeader);
            chatHeader.Controls.Add(lblChatTitle);
            main.Controls.Add(chatHeader, 0, 0);

            // Mensagens (scroll)
            messagesContainer = new Panel { Dock = DockStyle.Fill, BackColor = DarkerBg, AutoScroll = true, Padding = new Padding(20) };
            messagesList = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true
            };
            messagesContainer.Controls.Add(messagesList);

            // Welcome
            welcomePanel = new Panel { Dock = DockStyle.Fill };
            var welcomeIcon = new Label
            {
                Text = "🤖",
                Font = new Font("Segoe UI Emoji", 44, FontStyle.Regular),
                ForeColor = TextColor,
                AutoSize = true,
                Location = new Point(20, 20)
            };
            var welcomeText = new Label
            {
                Text = "Bem-vindo ao DotIA!\nFaça sua pergunta sobre TI e suporte técnico.",
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 12),
                AutoSize = true,
                Location = new Point(20, 90)
            };
            welcomePanel.Controls.Add(welcomeIcon);
            welcomePanel.Controls.Add(welcomeText);
            messagesContainer.Controls.Add(welcomePanel);

            // Feedback area
            feedbackArea = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                Padding = new Padding(10),
                Visible = false
            };
            var feedbackWrap = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            btnFeedbackUtil = BuildFeedbackButton("👍 Sim, foi útil");
            btnFeedbackNaoUtil = BuildFeedbackButton("👎 Não ajudou");
            btnFeedbackUtil.Click += async (s, e) => await AvaliarChatAsync(true);
            btnFeedbackNaoUtil.Click += async (s, e) => await AvaliarChatAsync(false);
            feedbackWrap.Controls.Add(btnFeedbackUtil);
            feedbackWrap.Controls.Add(btnFeedbackNaoUtil);
            feedbackArea.Controls.Add(feedbackWrap);
            messagesContainer.Controls.Add(feedbackArea);

            // Typing indicator
            typingIndicator = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Visible = false
            };
            var lblTyping = new Label
            {
                Text = "DotIA está digitando…",
                ForeColor = Color.Silver,
                AutoSize = true
            };
            typingIndicator.Controls.Add(lblTyping);
            messagesContainer.Controls.Add(typingIndicator);

            main.Controls.Add(messagesContainer, 0, 1);

            // Input
            inputArea = new Panel { Dock = DockStyle.Fill, BackColor = DarkBg, Padding = new Padding(20) };
            blockedNotice = new Panel { Dock = DockStyle.Top, Height = 46, Visible = false };
            inputWrapper = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = CardBg,
                Padding = new Padding(10),
                BorderStyle = BorderStyle.FixedSingle
            };
            txtMensagem = new TextBox
            {
                BorderStyle = BorderStyle.None,
                ForeColor = TextColor,
                BackColor = CardBg,
                Font = new Font("Segoe UI", 10),
                Multiline = true,
                Dock = DockStyle.Fill
            };
            txtMensagem.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !e.Shift)
                {
                    e.SuppressKeyPress = true;
                    _ = EnviarMensagemAsync();
                }
            };
            btnEnviar = new Button
            {
                Text = "Enviar ➜",
                ForeColor = Color.White,
                BackColor = PrimaryPurple,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Right,
                Width = 120
            };
            btnEnviar.FlatAppearance.BorderSize = 0;
            btnEnviar.Click += async (s, e) => await EnviarMensagemAsync();

            inputWrapper.Controls.Add(txtMensagem);
            inputWrapper.Controls.Add(btnEnviar);
            inputArea.Controls.Add(blockedNotice);
            inputArea.Controls.Add(inputWrapper);
            main.Controls.Add(inputArea, 0, 2);
        }

        // ======= Render helpers =======
        private Button BuildSidebarButton(string text, string icon)
        {
            return new Button
            {
                Text = $"{icon}  {text}",
                Height = 36,
                Dock = DockStyle.Top,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = PrimaryPurple
            };
        }
        private Button BuildPrimarySidebarButton(string text, string icon)
        {
            return new Button
            {
                Text = $"{icon}  {text}",
                Height = 40,
                Dock = DockStyle.Top,
                FlatStyle = FlatStyle.Flat,
                BackColor = PrimaryPurple,
                ForeColor = Color.White
            };
        }
        private Button BuildWarningSidebarButton(string text, string icon)
        {
            return new Button
            {
                Text = $"{icon}  {text}",
                Height = 40,
                Dock = DockStyle.Top,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Goldenrod,
                ForeColor = Color.White
            };
        }
        private Panel Wrap(Control c, int padH, int padV)
        {
            var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(padH, padV, padH, padV) };
            p.Controls.Add(c);
            return p;
        }
        private Button BuildFeedbackButton(string text)
        {
            return new Button
            {
                Text = text,
                Height = 44,
                Width = 200,
                FlatStyle = FlatStyle.Flat,
                BackColor = DarkerBg,
                ForeColor = TextColor
            };
        }

        private void Notificar(string texto)
        {
            // toast simples
            var t = new Label
            {
                Text = texto,
                AutoSize = true,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(160, 50, 50, 50),
                Padding = new Padding(10),
                Location = new Point(Width - 280, 20)
            };
            Controls.Add(t);
            var timer = new WinTimer { Interval = 3000 };
            timer.Tick += (s, e) => { Controls.Remove(t); timer.Stop(); };
            timer.Start();
        }

        // ======= Historico =======
        private List<ChatHistorico> _historicoCache = new List<ChatHistorico>();

        private async System.Threading.Tasks.Task CarregarHistoricoAsync(bool silencioso = false)
        {
            try
            {
                var hist = await _apiClient.ObterHistoricoAsync(_usuarioId);
                _historicoCache = hist ?? new List<ChatHistorico>();

                lstHistorico.Items.Clear();
                foreach (var h in _historicoCache)
                    lstHistorico.Items.Add(h);

                if (!silencioso) Notificar("Histórico carregado.");
            }
            catch (Exception ex)
            {
                if (!silencioso)
                    MessageBox.Show("Erro ao carregar histórico: " + ex.Message, "Erro",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async System.Threading.Tasks.Task AbrirHistoricoSelecionadoAsync()
        {
            if (lstHistorico.SelectedItem is ChatHistorico h)
            {
                await CarregarChatAsync(h);
            }
        }

        private async System.Threading.Tasks.Task EditarTituloSelecionadoAsync()
        {
            if (lstHistorico.SelectedItem is not ChatHistorico h) return;
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
            if (lstHistorico.SelectedItem is not ChatHistorico h) return;
            if (MessageBox.Show("Excluir esta conversa?", "Confirmação",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            var ok = await _apiClient.ExcluirChatAsync(h.Id);
            if (ok)
            {
                Notificar("Chat excluído.");
                if (h.Id == _chatIdAtual) NovoChat();
                await CarregarHistoricoAsync(true);
            }
            else Notificar("Falha ao excluir.");
        }

        // OwnerDraw do ListBox do histórico (título + badge status)
        private void LstHistorico_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            if (e.Index < 0 || e.Index >= lstHistorico.Items.Count) return;

            var h = (ChatHistorico)lstHistorico.Items[e.Index];
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var bounds = e.Bounds;
            using var bg = new SolidBrush(CardBg);
            using var hi = new SolidBrush(e.State.HasFlag(DrawItemState.Selected) ? BorderColor : CardBg);
            g.FillRectangle(hi, bounds);

            string title = string.IsNullOrWhiteSpace(h.Titulo) ? Abreviar(h.Pergunta, 40) : h.Titulo;

            using var f = new Font("Segoe UI", 10, FontStyle.Bold);
            using var f2 = new Font("Segoe UI", 9, FontStyle.Regular);
            using var white = new SolidBrush(TextColor);
            g.DrawString(title, f, white, bounds.Left + 10, bounds.Top + 8);

            // badge status
            var (badgeText, badgeColor) = Badge(h.Status);
            var sz = g.MeasureString(badgeText, f2);
            var badgeRect = new RectangleF(bounds.Right - sz.Width - 18, bounds.Top + 8, sz.Width + 8, sz.Height + 2);
            using var bbg = new SolidBrush(badgeColor);
            g.FillRectangle(bbg, badgeRect);
            g.DrawString(badgeText, f2, Brushes.White, badgeRect.Left + 4, badgeRect.Top + 1);

            // Pergunta curta
            var sub = Abreviar(h.Pergunta, 50);
            g.DrawString(sub, f2, new SolidBrush(Color.Silver), bounds.Left + 10, bounds.Top + 28);
        }

        private static (string, Color) Badge(int status)
        {
            // 1 aguardando avaliação, 2 concluído útil, 3 pendente técnico, 4 resolvido
            return status switch
            {
                2 => ("Concluído", Color.FromArgb(0, 128, 96)),
                3 => ("Pendente", Color.FromArgb(180, 120, 20)),
                4 => ("Resolvido", Color.FromArgb(120, 80, 200)),
                _ => ("Em andamento", Color.FromArgb(40, 90, 200)),
            };
        }

        private static string Abreviar(string s, int n)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= n ? s : s.Substring(0, n) + "...";
        }

        // ======= Chat =======
        private async System.Threading.Tasks.Task CarregarChatAsync(ChatHistorico h)
        {
            messagesList.SuspendLayout();
            messagesList.Controls.Clear();
            welcomePanel.Visible = false;
            _mensagensTecnicoProcessadas.Clear();

            _chatIdAtual = h.Id;
            _chatStatusAtual = h.Status;

            // mensagem do usuário + IA
            AdicionarMensagem(h.Pergunta, "user");
            AdicionarMensagem(h.Resposta, "bot");
            _ultimaPergunta = h.Pergunta;
            _ultimaResposta = h.Resposta;

            lblChatTitle.Text = string.IsNullOrWhiteSpace(h.Titulo) ? "Chat" : h.Titulo;

            // feedback
            RenderFeedbackPorStatus(h.Status);

            // técnico
            if (h.IdTicket.HasValue)
            {
                var vr = await _apiClient.VerificarRespostaAsync(h.Id);
                if (vr != null && vr.TemResposta && !string.IsNullOrWhiteSpace(vr.Solucao))
                {
                    ProcessarMensagensTecnico(vr.Solucao);
                    if (vr.StatusTicket == 1 && h.Status == 3) IniciarPolling();
                }
                else if (h.Status == 3) IniciarPolling();
            }
            else
            {
                PararPolling();
            }

            messagesList.ResumeLayout();
            ScrollToBottom();
        }

        private void RenderFeedbackPorStatus(int status)
        {
            feedbackArea.Visible = false;
            blockedNotice.Visible = false;

            if (status == 1)
            {
                feedbackArea.Visible = true;
            }
            else if (status == 2)
            {
                feedbackArea.Visible = true;
                feedbackArea.Enabled = false;
                btnFeedbackUtil.Text = "✅ Avaliado como útil";
                btnFeedbackNaoUtil.Visible = false;
            }
            else if (status == 3)
            {
                blockedNotice.Visible = true;
                blockedNotice.BackColor = Color.FromArgb(40, 59, 130, 246);
                blockedNotice.Controls.Clear();
                blockedNotice.Controls.Add(new Label
                {
                    Text = "🔒 Chat com técnico ativo: suas mensagens aqui vão para ele.",
                    ForeColor = Color.LightSkyBlue,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                });
            }
            else if (status == 4)
            {
                feedbackArea.Visible = true;
                feedbackArea.Enabled = false;
                btnFeedbackUtil.Text = "🟣 Ticket resolvido pelo técnico";
                btnFeedbackNaoUtil.Visible = false;
            }
        }

        private void AdicionarMensagem(string texto, string tipo)
        {
            var outer = new Panel { AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(0, 0, 0, 8) };
            var row = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = tipo == "user" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                WrapContents = false
            };

            var avatar = new Label
            {
                Text = tipo == "user" ? "👤" : (tipo == "tech" ? "🛠️" : "🤖"),
                AutoSize = true,
                Font = new Font("Segoe UI Emoji", 16)
            };

            var bubble = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(600, 0),
                Padding = new Padding(12),
                Text = texto,
                ForeColor = TextColor,
                BackColor = tipo == "user"
                    ? Mix(PrimaryPurple, SecondaryPurple)
                    : (tipo == "tech" ? Color.FromArgb(30, 120, 90) : CardBg)
            };

            row.Controls.Add(avatar);
            row.Controls.Add(bubble);
            outer.Controls.Add(row);
            messagesList.Controls.Add(outer);
        }

        private Color Mix(Color a, Color b)
        {
            return Color.FromArgb(255, (a.R + b.R) / 2, (a.G + b.G) / 2, (a.B + b.B) / 2);
        }

        private void ScrollToBottom()
        {
            messagesContainer.VerticalScroll.Value = messagesContainer.VerticalScroll.Maximum;
            messagesContainer.PerformLayout();
        }

        // ======= Envio =======
        private async System.Threading.Tasks.Task EnviarMensagemAsync()
        {
            var msg = txtMensagem.Text.Trim();
            if (string.IsNullOrEmpty(msg)) return;

            welcomePanel.Visible = false;
            AdicionarMensagem(msg, "user");
            txtMensagem.Clear();
            ScrollToBottom();

            btnEnviar.Enabled = false;
            typingIndicator.Visible = true;

            try
            {
                // se estiver pendente técnico (status=3), manda para técnico
                if (_chatStatusAtual == 3 && _chatIdAtual > 0)
                {
                    var ok = await _apiClient.EnviarMensagemParaTecnicoAsync(_chatIdAtual, msg);
                    typingIndicator.Visible = false;
                    btnEnviar.Enabled = true;

                    if (ok) Notificar("Mensagem enviada ao técnico.");
                    else Notificar("Falha ao enviar ao técnico.");
                    return;
                }

                // IA
                var resp = await _apiClient.EnviarPerguntaAsync(_usuarioId, msg);
                typingIndicator.Visible = false;
                btnEnviar.Enabled = true;

                if (resp.Sucesso)
                {
                    _chatIdAtual = resp.ChatId;
                    _chatStatusAtual = 1;
                    _ultimaPergunta = msg;
                    _ultimaResposta = resp.Resposta;
                    AdicionarMensagem(resp.Resposta, "bot");
                    feedbackArea.Enabled = true;
                    feedbackArea.Visible = true;
                    await CarregarHistoricoAsync(true);
                }
                else
                {
                    AdicionarMensagem("Desculpe, ocorreu um erro ao processar sua pergunta.", "bot");
                }
            }
            catch (Exception ex)
            {
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
                feedbackArea.Enabled = false;
                btnFeedbackNaoUtil.Visible = false;
                btnFeedbackUtil.Text = "✅ Avaliado como útil";
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

        // ======= Polling técnico =======
        private void ConfigurarTimers()
        {
            _timerHistorico.Interval = 5000;
            _timerHistorico.Tick += async (s, e) => await CarregarHistoricoAsync(true);
            _timerHistorico.Start();

            _timerPollingTecnico.Interval = 5000;
            _timerPollingTecnico.Tick += async (s, e) => await PollingTecnicoTickAsync();
        }

        private void IniciarPolling()
        {
            _timerPollingTecnico.Start();
            blockedNotice.Visible = true;
        }

        private void PararPolling()
        {
            _timerPollingTecnico.Stop();
        }

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

                    // statusTicket: 2 = resolvido
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

                // remove prefixos [USUÁRIO ...] / [TÉCNICO ...]
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

        // ======= Ações auxiliares =======
        private void NovoChat()
        {
            PararPolling();
            _chatIdAtual = 0;
            _chatStatusAtual = null;
            _mensagensTecnicoProcessadas.Clear();

            messagesList.Controls.Clear();
            welcomePanel.Visible = true;
            lblChatTitle.Text = "DotIA - Assistente Inteligente";
            txtMensagem.Clear();
            blockedNotice.Visible = false;
            feedbackArea.Visible = false;

            lstHistorico.ClearSelected();
        }

        private void AbrirTicketDiretoDialog()
        {
            using var f = new Form
            {
                Text = "Abrir Ticket Direto",
                StartPosition = FormStartPosition.CenterParent,
                BackColor = DarkerBg,
                ForeColor = TextColor,
                Size = new Size(500, 400),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var lblT = new Label { Text = "Título do Problema:", AutoSize = true, Location = new Point(16, 16) };
            var txtT = new TextBox { Location = new Point(16, 40), Width = 450 };
            var lblD = new Label { Text = "Descrição:", AutoSize = true, Location = new Point(16, 78) };
            var txtD = new TextBox { Location = new Point(16, 100), Size = new Size(450, 190), Multiline = true, ScrollBars = ScrollBars.Vertical };

            var btnOk = new Button { Text = "Abrir Ticket", BackColor = PrimaryPurple, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Location = new Point(280, 310), Width = 186 };
            var btnCancel = new Button { Text = "Cancelar", Location = new Point(180, 310), Width = 90 };
            btnCancel.Click += (s, e) => f.DialogResult = DialogResult.Cancel;

            btnOk.Click += async (s, e) =>
            {
                var titulo = txtT.Text.Trim();
                var descricao = txtD.Text.Trim();
                if (titulo.Length < 5) { MessageBox.Show("Informe um título (mín. 5)."); return; }
                if (descricao.Length < 20) { MessageBox.Show("Descreva melhor o problema (mín. 20)."); return; }

                btnOk.Enabled = false;
                var resp = await _apiClient.AbrirTicketDiretoAsync(_usuarioId, titulo, descricao);
                btnOk.Enabled = true;

                if (resp != null && resp.Sucesso)
                {
                    Notificar("Ticket criado com sucesso.");
                    f.DialogResult = DialogResult.OK;
                    await CarregarHistoricoAsync(true);

                    // abrir chat criado
                    var chatCriado = _historicoCache.FirstOrDefault(c => c.Id == resp.ChatId);
                    if (chatCriado != null) await CarregarChatAsync(chatCriado);
                }
                else
                {
                    MessageBox.Show(resp?.Mensagem ?? "Erro ao abrir ticket.");
                }
            };

            f.Controls.Add(lblT); f.Controls.Add(txtT);
            f.Controls.Add(lblD); f.Controls.Add(txtD);
            f.Controls.Add(btnCancel); f.Controls.Add(btnOk);

            f.ShowDialog(this);
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
            f.Controls.Add(tb); f.Controls.Add(ok); f.Controls.Add(cancel);
            return f.ShowDialog() == DialogResult.OK ? tb.Text : defaultValue;
        }
    }
}
