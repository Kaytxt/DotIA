using System;
using System.Collections.Generic;
using System.Drawing;
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

        // Cores
        private readonly Color PrimaryGreen = ColorTranslator.FromHtml("#10b981");
        private readonly Color SecondaryGreen = ColorTranslator.FromHtml("#059669");
        private readonly Color DarkBg = ColorTranslator.FromHtml("#1a132f");
        private readonly Color DarkerBg = ColorTranslator.FromHtml("#1e1433");
        private readonly Color CardBg = ColorTranslator.FromHtml("#2c204d");
        private readonly Color BorderColor = ColorTranslator.FromHtml("#3d2e6b");

        // Estado
        private TicketDTO _ticketSelecionado = null;
        private readonly HashSet<string> _mensagensProcessadas = new HashSet<string>();

        // Timers
        private readonly WinTimer _timerRefresh = new WinTimer();
        private readonly WinTimer _timerPolling = new WinTimer();

        // Layout
        private SplitContainer rootSplit;
        private Panel sidebar;
        private Panel statsCard;
        private Label lblTotalTickets;
        private Label lblResolvidosHoje;
        private Button btnRefresh;
        private Label lblLive;
        private ListBox lstTickets;
        private Panel workArea;
        private Panel workHeader;
        private Label lblTicketTitle;
        private Panel conversationPanel;
        private FlowLayoutPanel messagesList;
        private Panel solutionArea;
        private TextBox txtSolucao;
        private Button btnResponder;
        private Button btnResolver;
        private Label lblSuccess;

        public TecnicoForm(int usuarioId, string nomeUsuario)
        {
            _usuarioId = usuarioId;
            _nomeUsuario = nomeUsuario;
            _apiClient = new ApiClient();

            InitializeComponent();
            MontarLayout();
            ConfigurarTimers();
            CarregarTicketsAsync();
        }

        private void InitializeComponent()
        {
            Text = "DotIA - Painel do Técnico";
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

            rootSplit.Panel1MinSize = 320;
            rootSplit.Panel1.BackColor = DarkBg;
            rootSplit.Panel2.BackColor = DarkerBg;
            rootSplit.SplitterDistance = 350;

            // ===== Sidebar =====
            var sidebarTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = DarkBg,
                ColumnCount = 1,
                RowCount = 5
            };
            sidebarTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));   // header
            sidebarTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));   // refresh
            sidebarTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));  // stats
            sidebarTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // tickets
            sidebarTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));   // footer
            rootSplit.Panel1.Controls.Add(sidebarTable);

            // Header
            var header = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 16, 20, 8) };
            var logoIcon = new Panel
            {
                Size = new Size(45, 45),
                BackColor = PrimaryGreen,
                Margin = new Padding(0, 0, 10, 0)
            };
            var lblLogo = new Label
            {
                Text = "DotIA Tech",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                AutoSize = true
            };
            var headerWrap = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            headerWrap.Controls.Add(logoIcon);
            headerWrap.Controls.Add(lblLogo);
            header.Controls.Add(headerWrap);
            sidebarTable.Controls.Add(header, 0, 0);

            // Refresh
            btnRefresh = new Button
            {
                Text = "? Atualizar",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, PrimaryGreen),
                ForeColor = PrimaryGreen,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderColor = PrimaryGreen;
            btnRefresh.FlatAppearance.BorderSize = 2;
            btnRefresh.Click += async (s, e) => await CarregarTicketsAsync();
            sidebarTable.Controls.Add(Wrap(btnRefresh, 16, 8), 0, 1);

            // Stats
            statsCard = new Panel { Dock = DockStyle.Fill, BackColor = CardBg, Padding = new Padding(15) };
            var stats = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
            lblTotalTickets = new Label { Text = "0", Font = new Font("Segoe UI", 24, FontStyle.Bold), ForeColor = PrimaryGreen, AutoSize = true };
            lblResolvidosHoje = new Label { Text = "0", Font = new Font("Segoe UI", 24, FontStyle.Bold), ForeColor = Color.FromArgb(16, 185, 129), AutoSize = true };
            stats.Controls.Add(new Label { Text = "Tickets Pendentes", ForeColor = Color.Silver, AutoSize = true }, 0, 0);
            stats.Controls.Add(lblTotalTickets, 0, 1);
            stats.Controls.Add(new Label { Text = "Resolvidos Hoje", ForeColor = Color.Silver, AutoSize = true }, 1, 0);
            stats.Controls.Add(lblResolvidosHoje, 1, 1);
            statsCard.Controls.Add(stats);
            sidebarTable.Controls.Add(Wrap(statsCard, 16, 8), 0, 2);

            // Lista de Tickets
            var ticketsPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };
            lstTickets = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = CardBg,
                ForeColor = Color.White,
                IntegralHeight = false
            };
            lstTickets.DrawMode = DrawMode.OwnerDrawFixed;
            lstTickets.ItemHeight = 80;
            lstTickets.DrawItem += LstTickets_DrawItem;
            lstTickets.DoubleClick += async (s, e) => await AbrirTicketSelecionadoAsync();
            ticketsPanel.Controls.Add(lstTickets);
            sidebarTable.Controls.Add(ticketsPanel, 0, 3);

            // Footer
            var footer = new Panel { Dock = DockStyle.Fill, BackColor = CardBg, Padding = new Padding(16) };
            var btnSair = new Button
            {
                Text = "?? Sair",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSair.FlatAppearance.BorderColor = BorderColor;
            btnSair.Click += (s, e) => { this.Hide(); new LoginForm().Show(); };
            footer.Controls.Add(btnSair);
            sidebarTable.Controls.Add(footer, 0, 4);

            // ===== Work Area =====
            var workTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = DarkerBg,
                ColumnCount = 1,
                RowCount = 3
            };
            workTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));  // header
            workTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // messages
            workTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 180)); // solution
            rootSplit.Panel2.Controls.Add(workTable);

            // Work Header
            workHeader = new Panel { Dock = DockStyle.Fill, BackColor = DarkBg, Padding = new Padding(20), Visible = false };
            lblTicketTitle = new Label { Text = "Ticket #0", ForeColor = Color.White, Font = new Font("Segoe UI", 16, FontStyle.Bold), AutoSize = true };
            workHeader.Controls.Add(lblTicketTitle);
            workTable.Controls.Add(workHeader, 0, 0);

            // Messages
            conversationPanel = new Panel { Dock = DockStyle.Fill, BackColor = DarkerBg, AutoScroll = true, Padding = new Padding(20) };
            messagesList = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true
            };
            conversationPanel.Controls.Add(messagesList);

            var emptyState = new Panel { Dock = DockStyle.Fill };
            var emptyIcon = new Label { Text = "??", Font = new Font("Segoe UI Emoji", 80), ForeColor = Color.Silver, AutoSize = true, Location = new Point(20, 100) };
            var emptyText = new Label { Text = "Selecione um Ticket", ForeColor = Color.White, Font = new Font("Segoe UI", 20, FontStyle.Bold), AutoSize = true, Location = new Point(20, 220) };
            emptyState.Controls.Add(emptyIcon);
            emptyState.Controls.Add(emptyText);
            conversationPanel.Controls.Add(emptyState);
            workTable.Controls.Add(conversationPanel, 0, 1);

            // Solution Area
            solutionArea = new Panel { Dock = DockStyle.Fill, BackColor = DarkBg, Padding = new Padding(20), Visible = false };
            lblSuccess = new Label { Dock = DockStyle.Top, Height = 40, Visible = false, BackColor = Color.FromArgb(40, PrimaryGreen), ForeColor = PrimaryGreen, TextAlign = ContentAlignment.MiddleCenter };
            txtSolucao = new TextBox { Dock = DockStyle.Top, Height = 80, Multiline = true, BackColor = CardBg, ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };
            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft };
            btnResolver = new Button { Text = "? Resolver e Fechar", Width = 200, Height = 45, FlatStyle = FlatStyle.Flat, BackColor = PrimaryGreen, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };
            btnResponder = new Button { Text = "?? Enviar Mensagem", Width = 200, Height = 45, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(59, 130, 246), ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };
            btnResolver.FlatAppearance.BorderSize = 0;
            btnResponder.FlatAppearance.BorderSize = 0;
            btnResolver.Click += async (s, e) => await ResponderTicketAsync(true);
            btnResponder.Click += async (s, e) => await ResponderTicketAsync(false);
            btnPanel.Controls.Add(btnResolver);
            btnPanel.Controls.Add(btnResponder);
            solutionArea.Controls.Add(lblSuccess);
            solutionArea.Controls.Add(txtSolucao);
            solutionArea.Controls.Add(btnPanel);
            workTable.Controls.Add(solutionArea, 0, 2);
        }

        private Panel Wrap(Control c, int padH, int padV)
        {
            var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(padH, padV, padH, padV) };
            p.Controls.Add(c);
            return p;
        }

        private void ConfigurarTimers()
        {
            _timerRefresh.Interval = 30000; // 30 segundos
            _timerRefresh.Tick += async (s, e) => await CarregarTicketsAsync();
            _timerRefresh.Start();

            _timerPolling.Interval = 3000; // 3 segundos
            _timerPolling.Tick += async (s, e) => await PollingTicketAtualAsync();
        }

        private async System.Threading.Tasks.Task CarregarTicketsAsync()
        {
            try
            {
                var tickets = await _apiClient.ObterTicketsPendentesAsync();
                lstTickets.Items.Clear();
                foreach (var t in tickets)
                    lstTickets.Items.Add(t);

                lblTotalTickets.Text = tickets.Count.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar tickets: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LstTickets_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            if (e.Index < 0 || e.Index >= lstTickets.Items.Count) return;

            var ticket = (TicketDTO)lstTickets.Items[e.Index];
            var g = e.Graphics;
            var bounds = e.Bounds;

            using var bg = new SolidBrush(e.State.HasFlag(DrawItemState.Selected) ? BorderColor : CardBg);
            g.FillRectangle(bg, bounds);

            using var white = new SolidBrush(Color.White);
            using var gray = new SolidBrush(Color.Silver);
            using var f = new Font("Segoe UI", 10, FontStyle.Bold);
            using var f2 = new Font("Segoe UI", 9);

            g.DrawString($"Ticket #{ticket.Id}", f, white, bounds.Left + 10, bounds.Top + 10);
            g.DrawString(ticket.NomeSolicitante, f2, gray, bounds.Left + 10, bounds.Top + 32);

            var desc = ticket.DescricaoProblema.Length > 50 ? ticket.DescricaoProblema.Substring(0, 50) + "..." : ticket.DescricaoProblema;
            g.DrawString(desc, f2, gray, bounds.Left + 10, bounds.Top + 52);
        }

        private async System.Threading.Tasks.Task AbrirTicketSelecionadoAsync()
        {
            if (lstTickets.SelectedItem is TicketDTO ticket)
            {
                await CarregarTicketAsync(ticket);
            }
        }

        private async System.Threading.Tasks.Task CarregarTicketAsync(TicketDTO ticket)
        {
            _ticketSelecionado = ticket;
            _mensagensProcessadas.Clear();

            messagesList.Controls.Clear();
            workHeader.Visible = true;
            lblTicketTitle.Text = $"Ticket #{ticket.Id} - {ticket.NomeSolicitante}";

            AdicionarMensagem("user", ticket.PerguntaOriginal, ticket.NomeSolicitante);
            AdicionarMensagem("bot", ticket.RespostaIA, "DotIA");

            if (!string.IsNullOrEmpty(ticket.Solucao))
            {
                ProcessarMensagensTecnico(ticket.Solucao);
            }

            solutionArea.Visible = true;
            txtSolucao.Clear();
            lblSuccess.Visible = false;

            _timerPolling.Start();
        }

        private void AdicionarMensagem(string tipo, string texto, string autor)
        {
            var msgPanel = new Panel { AutoSize = true, MaximumSize = new Size(800, 0), Padding = new Padding(10), Margin = new Padding(0, 0, 0, 10) };

            Color bgColor = tipo == "user" ? Color.FromArgb(141, 75, 255) : tipo == "bot" ? Color.FromArgb(59, 130, 246) : PrimaryGreen;
            msgPanel.BackColor = bgColor;

            var lblAutor = new Label { Text = autor, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true, Dock = DockStyle.Top };
            var lblTexto = new Label { Text = texto, ForeColor = Color.White, AutoSize = true, MaximumSize = new Size(780, 0), Dock = DockStyle.Top };

            msgPanel.Controls.Add(lblTexto);
            msgPanel.Controls.Add(lblAutor);
            messagesList.Controls.Add(msgPanel);
            messagesList.ScrollControlIntoView(msgPanel);
        }

        private void ProcessarMensagensTecnico(string solucao)
        {
            var blocos = solucao.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var bloco in blocos)
            {
                var m = bloco.Trim();
                if (string.IsNullOrEmpty(m) || _mensagensProcessadas.Contains(m)) continue;

                var usuarioRegex = new System.Text.RegularExpressions.Regex(@"^\[USUÁRIO\s*-\s*\d{2}/\d{2}/\d{4}\s+\d{2}:\d{2}\]\s*");
                var tecnicoRegex = new System.Text.RegularExpressions.Regex(@"^\[TÉCNICO\s*-\s*\d{2}/\d{2}/\d{4}\s+\d{2}:\d{2}\]\s*");

                var isUsuario = usuarioRegex.IsMatch(m);
                var isTecnico = tecnicoRegex.IsMatch(m);

                var limpo = usuarioRegex.Replace(m, "");
                limpo = tecnicoRegex.Replace(limpo, "");

                if (!string.IsNullOrWhiteSpace(limpo))
                {
                    if (isTecnico)
                        AdicionarMensagem("tech", limpo, "Você (Técnico)");
                    else if (isUsuario)
                        AdicionarMensagem("user", limpo, _ticketSelecionado.NomeSolicitante);

                    _mensagensProcessadas.Add(m);
                }
            }
        }

        private async System.Threading.Tasks.Task ResponderTicketAsync(bool resolver)
        {
            if (_ticketSelecionado == null) return;

            var solucao = txtSolucao.Text.Trim();
            if (string.IsNullOrEmpty(solucao) && !resolver)
            {
                MessageBox.Show("Digite uma mensagem para o cliente.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (resolver && MessageBox.Show("Tem certeza que deseja fechar este ticket?", "Confirmação", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            btnResponder.Enabled = false;
            btnResolver.Enabled = false;

            try
            {
                var ok = await _apiClient.ResolverTicketAsync(_ticketSelecionado.Id, solucao, resolver);

                if (ok)
                {
                    if (!string.IsNullOrEmpty(solucao))
                        AdicionarMensagem("tech", solucao, "Você (Técnico)");

                    lblSuccess.Text = resolver ? "? Ticket resolvido com sucesso!" : "? Mensagem enviada!";
                    lblSuccess.Visible = true;
                    txtSolucao.Clear();

                    if (resolver)
                    {
                        await CarregarTicketsAsync();
                        _timerPolling.Stop();
                        workHeader.Visible = false;
                        solutionArea.Visible = false;
                        messagesList.Controls.Clear();
                    }
                }
                else
                {
                    MessageBox.Show("Erro ao processar resposta.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnResponder.Enabled = true;
                btnResolver.Enabled = true;
            }
        }

        private async System.Threading.Tasks.Task PollingTicketAtualAsync()
        {
            if (_ticketSelecionado == null) return;

            try
            {
                var ticketAtual = await _apiClient.ObterTicketAsync(_ticketSelecionado.Id);
                if (ticketAtual != null && ticketAtual.Ticket != null && !string.IsNullOrEmpty(ticketAtual.Ticket.Solucao))
                {
                    ProcessarMensagensTecnico(ticketAtual.Ticket.Solucao);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro no polling: " + ex.Message);
            }
        }
    }
}