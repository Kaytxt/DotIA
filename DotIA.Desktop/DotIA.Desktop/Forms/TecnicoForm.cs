// DotIA.Desktop/DotIA.Desktop/Forms/TecnicoForm.cs
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using DotIA.Desktop.Services;

namespace DotIA.Desktop.Forms
{
    public partial class TecnicoForm : Form
    {
        private readonly ApiClient _apiClient;
        private readonly int _usuarioId;
        private readonly string _nomeUsuario;
        private FlowLayoutPanel panelTickets;
        private Panel panelDetalhes;
        private RichTextBox rtbConversa;
        private TextBox txtSolucao;
        private Button btnResponder;
        private Button btnResolver;
        private Label lblTicketInfo;
        private Label lblTotalTickets;
        private Label lblResolvidosHoje;
        private TicketDTO ticketSelecionado;
        private System.Windows.Forms.Timer refreshTimer;
        private List<TicketDTO> tickets;

        public TecnicoForm(int usuarioId, string nomeUsuario)
        {
            _usuarioId = usuarioId;
            _nomeUsuario = nomeUsuario;
            _apiClient = new ApiClient();
            InitializeComponent();
            IniciarAutoRefresh();
            CarregarTickets();
        }

        private void InitializeComponent()
        {
            this.Text = "DotIA - Painel do Técnico";
            this.Size = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(26, 19, 47);
            this.WindowState = FormWindowState.Maximized;

            // Container principal
            SplitContainer splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 350,
                BackColor = Color.FromArgb(44, 32, 77)
            };

            // ???????????????????????????????????????????????????????????
            // PAINEL ESQUERDO - Lista de Tickets
            // ???????????????????????????????????????????????????????????

            Panel leftPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(26, 19, 47)
            };

            // Header
            Panel headerPanel = new Panel
            {
                Height = 80,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(30, 20, 51),
                Padding = new Padding(15)
            };

            Label lblTitulo = new Label
            {
                Text = "??? DotIA Tech",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 185, 129),
                AutoSize = true,
                Location = new Point(15, 20)
            };
            headerPanel.Controls.Add(lblTitulo);

            // Stats Panel
            Panel statsPanel = new Panel
            {
                Height = 100,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(44, 32, 77),
                Padding = new Padding(15)
            };

            lblTotalTickets = new Label
            {
                Text = "Tickets Pendentes: 0",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(251, 191, 36),
                Location = new Point(15, 20),
                AutoSize = true
            };

            lblResolvidosHoje = new Label
            {
                Text = "Resolvidos Hoje: 0",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 185, 129),
                Location = new Point(15, 50),
                AutoSize = true
            };

            statsPanel.Controls.Add(lblTotalTickets);
            statsPanel.Controls.Add(lblResolvidosHoje);

            // Botão Atualizar
            Button btnRefresh = new Button
            {
                Text = "?? Atualizar",
                Location = new Point(15, 10),
                Size = new Size(320, 35),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRefresh.Click += async (s, e) => await CarregarTickets();

            // Container de Tickets
            panelTickets = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(26, 19, 47),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(10)
            };

            leftPanel.Controls.Add(panelTickets);
            leftPanel.Controls.Add(btnRefresh);
            leftPanel.Controls.Add(statsPanel);
            leftPanel.Controls.Add(headerPanel);

            // ???????????????????????????????????????????????????????????
            // PAINEL DIREITO - Detalhes e Conversa
            // ???????????????????????????????????????????????????????????

            panelDetalhes = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 20, 51),
                Visible = false
            };

            // Header do Ticket
            Panel ticketHeader = new Panel
            {
                Height = 60,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(26, 19, 47),
                BorderStyle = BorderStyle.FixedSingle
            };

            lblTicketInfo = new Label
            {
                Text = "Selecione um ticket",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                AutoSize = true
            };
            ticketHeader.Controls.Add(lblTicketInfo);

            // Área de Conversa
            rtbConversa = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 20, 51),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11),
                ReadOnly = true,
                BorderStyle = BorderStyle.None
            };

            // Área de Solução
            Panel solutionPanel = new Panel
            {
                Height = 180,
                Dock = DockStyle.Bottom,
                BackColor = Color.FromArgb(26, 19, 47),
                Padding = new Padding(20)
            };

            Label lblSolucao = new Label
            {
                Text = "Digite sua resposta:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 185, 129),
                Location = new Point(20, 10),
                AutoSize = true
            };

            txtSolucao = new TextBox
            {
                Location = new Point(20, 35),
                Size = new Size(760, 60),
                Multiline = true,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(44, 32, 77),
                ForeColor = Color.White,
                ScrollBars = ScrollBars.Vertical
            };

            btnResponder = new Button
            {
                Text = "?? Enviar Mensagem",
                Location = new Point(20, 105),
                Size = new Size(370, 40),
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnResponder.Click += async (s, e) => await ResponderTicket(false);

            btnResolver = new Button
            {
                Text = "? Resolver e Fechar",
                Location = new Point(410, 105),
                Size = new Size(370, 40),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnResolver.Click += async (s, e) => await ResponderTicket(true);

            solutionPanel.Controls.Add(lblSolucao);
            solutionPanel.Controls.Add(txtSolucao);
            solutionPanel.Controls.Add(btnResponder);
            solutionPanel.Controls.Add(btnResolver);

            panelDetalhes.Controls.Add(rtbConversa);
            panelDetalhes.Controls.Add(solutionPanel);
            panelDetalhes.Controls.Add(ticketHeader);

            // Estado Vazio
            Panel emptyState = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 20, 51)
            };

            Label lblEmpty = new Label
            {
                Text = "??\n\nSelecione um ticket da lista\npara visualizar os detalhes",
                Font = new Font("Segoe UI", 16),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            emptyState.Controls.Add(lblEmpty);

            splitContainer.Panel1.Controls.Add(leftPanel);
            splitContainer.Panel2.Controls.Add(panelDetalhes);
            splitContainer.Panel2.Controls.Add(emptyState);

            this.Controls.Add(splitContainer);

            // Botão Sair no rodapé
            Button btnSair = new Button
            {
                Text = "Sair",
                Size = new Size(100, 30),
                Location = new Point(this.Width - 120, this.Height - 50),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                BackColor = Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSair.Click += (s, e) => {
                Application.Restart();
            };
            this.Controls.Add(btnSair);
            btnSair.BringToFront();
        }

        private void IniciarAutoRefresh()
        {
            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 5000; // 5 segundos
            refreshTimer.Tick += async (s, e) => await CarregarTickets();
            refreshTimer.Start();
        }

        private async Task CarregarTickets()
        {
            try
            {
                tickets = await _apiClient.ObterTicketsPendentesAsync();

                lblTotalTickets.Text = $"Tickets Pendentes: {tickets.Count}";

                panelTickets.Controls.Clear();

                if (tickets.Count == 0)
                {
                    Label lblNoTickets = new Label
                    {
                        Text = "??\n\nNenhum ticket pendente!",
                        Font = new Font("Segoe UI", 12),
                        ForeColor = Color.Gray,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Size = new Size(330, 100)
                    };
                    panelTickets.Controls.Add(lblNoTickets);
                    return;
                }

                foreach (var ticket in tickets)
                {
                    Panel ticketCard = CreateTicketCard(ticket);
                    panelTickets.Controls.Add(ticketCard);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar tickets: {ex.Message}",
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Panel CreateTicketCard(TicketDTO ticket)
        {
            Panel card = new Panel
            {
                Size = new Size(330, 120),
                BackColor = Color.FromArgb(44, 32, 77),
                Margin = new Padding(5),
                Padding = new Padding(10),
                Cursor = Cursors.Hand
            };

            Label lblTicketId = new Label
            {
                Text = $"Ticket #{ticket.Id}",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 185, 129),
                Location = new Point(10, 10),
                AutoSize = true
            };

            Label lblSolicitante = new Label
            {
                Text = $"?? {ticket.NomeSolicitante}",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.White,
                Location = new Point(10, 35),
                AutoSize = true
            };

            Label lblDescricao = new Label
            {
                Text = ticket.DescricaoProblema.Length > 50
                    ? ticket.DescricaoProblema.Substring(0, 50) + "..."
                    : ticket.DescricaoProblema,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.LightGray,
                Location = new Point(10, 60),
                Size = new Size(310, 30)
            };

            Label lblData = new Label
            {
                Text = $"?? {ticket.DataAbertura:dd/MM/yyyy}",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray,
                Location = new Point(10, 90),
                AutoSize = true
            };

            card.Controls.Add(lblTicketId);
            card.Controls.Add(lblSolicitante);
            card.Controls.Add(lblDescricao);
            card.Controls.Add(lblData);

            card.Click += async (s, e) => await SelecionarTicket(ticket);
            foreach (Control c in card.Controls)
                c.Click += async (s, e) => await SelecionarTicket(ticket);

            card.MouseEnter += (s, e) => card.BackColor = Color.FromArgb(61, 43, 109);
            card.MouseLeave += (s, e) => card.BackColor = Color.FromArgb(44, 32, 77);

            return card;
        }

        private async Task SelecionarTicket(TicketDTO ticket)
        {
            ticketSelecionado = ticket;
            panelDetalhes.Visible = true;

            lblTicketInfo.Text = $"Ticket #{ticket.Id} - {ticket.NomeSolicitante}";

            rtbConversa.Clear();

            // Adicionar pergunta do usuário
            rtbConversa.SelectionFont = new Font("Segoe UI", 11, FontStyle.Bold);
            rtbConversa.SelectionColor = Color.FromArgb(141, 75, 255);
            rtbConversa.AppendText($"?? {ticket.NomeSolicitante}:\n");
            rtbConversa.SelectionFont = new Font("Segoe UI", 10, FontStyle.Regular);
            rtbConversa.SelectionColor = Color.White;
            rtbConversa.AppendText($"{ticket.PerguntaOriginal}\n\n");

            // Adicionar resposta da IA
            rtbConversa.SelectionFont = new Font("Segoe UI", 11, FontStyle.Bold);
            rtbConversa.SelectionColor = Color.FromArgb(59, 130, 246);
            rtbConversa.AppendText("?? DotIA:\n");
            rtbConversa.SelectionFont = new Font("Segoe UI", 10, FontStyle.Regular);
            rtbConversa.SelectionColor = Color.White;
            rtbConversa.AppendText($"{ticket.RespostaIA}\n\n");

            // Se já houver solução do técnico
            if (!string.IsNullOrEmpty(ticket.Solucao))
            {
                var mensagens = ticket.Solucao.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var msg in mensagens)
                {
                    if (msg.Contains("[USUÁRIO"))
                    {
                        rtbConversa.SelectionFont = new Font("Segoe UI", 11, FontStyle.Bold);
                        rtbConversa.SelectionColor = Color.FromArgb(141, 75, 255);
                        rtbConversa.AppendText("?? Usuário:\n");
                        var conteudo = System.Text.RegularExpressions.Regex.Replace(msg, @"\[USUÁRIO.*?\]\s*", "");
                        rtbConversa.SelectionFont = new Font("Segoe UI", 10, FontStyle.Regular);
                        rtbConversa.SelectionColor = Color.White;
                        rtbConversa.AppendText($"{conteudo}\n\n");
                    }
                    else if (msg.Contains("[TÉCNICO"))
                    {
                        rtbConversa.SelectionFont = new Font("Segoe UI", 11, FontStyle.Bold);
                        rtbConversa.SelectionColor = Color.FromArgb(16, 185, 129);
                        rtbConversa.AppendText("??? Você (Técnico):\n");
                        var conteudo = System.Text.RegularExpressions.Regex.Replace(msg, @"\[TÉCNICO.*?\]\s*", "");
                        rtbConversa.SelectionFont = new Font("Segoe UI", 10, FontStyle.Regular);
                        rtbConversa.SelectionColor = Color.White;
                        rtbConversa.AppendText($"{conteudo}\n\n");
                    }
                }
            }

            txtSolucao.Clear();
            txtSolucao.Focus();
        }

        private async Task ResponderTicket(bool marcarComoResolvido)
        {
            if (ticketSelecionado == null)
            {
                MessageBox.Show("Selecione um ticket primeiro.", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string solucao = txtSolucao.Text.Trim();

            if (marcarComoResolvido)
            {
                if (MessageBox.Show("Tem certeza que deseja fechar este ticket?",
                    "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
            }
            else if (string.IsNullOrEmpty(solucao))
            {
                MessageBox.Show("Por favor, escreva uma mensagem.", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnResponder.Enabled = false;
            btnResolver.Enabled = false;
            this.Cursor = Cursors.WaitCursor;

            try
            {
                var sucesso = await _apiClient.ResolverTicketAsync(
                    ticketSelecionado.Id,
                    solucao,
                    marcarComoResolvido
                );

                if (sucesso)
                {
                    if (marcarComoResolvido)
                    {
                        MessageBox.Show("Ticket resolvido com sucesso!", "Sucesso",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        panelDetalhes.Visible = false;
                        ticketSelecionado = null;
                    }
                    else
                    {
                        MessageBox.Show("Mensagem enviada com sucesso!", "Sucesso",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Adicionar mensagem na conversa
                        rtbConversa.SelectionFont = new Font("Segoe UI", 11, FontStyle.Bold);
                        rtbConversa.SelectionColor = Color.FromArgb(16, 185, 129);
                        rtbConversa.AppendText("??? Você (Técnico):\n");
                        rtbConversa.SelectionFont = new Font("Segoe UI", 10, FontStyle.Regular);
                        rtbConversa.SelectionColor = Color.White;
                        rtbConversa.AppendText($"{solucao}\n\n");
                    }

                    txtSolucao.Clear();
                    await CarregarTickets();
                }
                else
                {
                    MessageBox.Show("Erro ao processar ticket.", "Erro",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro: {ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnResponder.Enabled = true;
                btnResolver.Enabled = true;
                this.Cursor = Cursors.Default;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            refreshTimer?.Stop();
            refreshTimer?.Dispose();
            base.OnFormClosed(e);
        }
    }
}