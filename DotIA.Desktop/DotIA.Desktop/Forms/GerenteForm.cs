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
    public partial class GerenteForm : Form
    {
        private readonly ApiClient _apiClient;
        private readonly int _usuarioId;
        private readonly string _nomeUsuario;

        // imagens
        private Image? _logoImage;
        private Image? _iconPessoa;
        private Image? _iconTicket;
        private Image? _iconCheck;
        private Image? _iconBalao;
        private Image? _iconGrafico;

        // cores do tema
        private readonly Color PrimaryBlue = ColorTranslator.FromHtml("#3b82f6");
        private readonly Color SecondaryBlue = ColorTranslator.FromHtml("#2563eb");
        private readonly Color PrimaryPurple = ColorTranslator.FromHtml("#8b5cf6");
        private readonly Color SecondaryPurple = ColorTranslator.FromHtml("#a855f7");
        private readonly Color PrimaryGreen = ColorTranslator.FromHtml("#10b981");
        private readonly Color DarkBg = ColorTranslator.FromHtml("#1a132f");
        private readonly Color DarkerBg = ColorTranslator.FromHtml("#1e1433");
        private readonly Color PanelBg = ColorTranslator.FromHtml("#221a3d");
        private readonly Color PanelBg2 = ColorTranslator.FromHtml("#2d244e");
        private readonly Color PanelBorder = ColorTranslator.FromHtml("#3d2e6b");
        private readonly Color TextColor = ColorTranslator.FromHtml("#e5e7eb");
        private readonly Color GridSelectionColor = ColorTranslator.FromHtml("#5b21b6");

        // layout principal
        private Panel headerPanel = null!;
        private Panel contentPanel = null!;
        private FlowLayoutPanel statsPanel = null!;
        private Panel tabsPanel = null!;
        private Panel tabContent = null!;

        // controles do header
        private Label lblUser = null!;
        private Button btnSair = null!;

        // cards de estatistica
        private Label lblTotalUsuarios = null!;
        private Label lblTicketsAbertos = null!;
        private Label lblTicketsResolvidos = null!;
        private Label lblTotalChats = null!;
        private Label lblResolvidosHoje = null!;
        private Label lblChatsResolvidos = null!;

        // abas
        private Button btnTabTickets = null!;
        private Button btnTabUsuarios = null!;
        private Button btnTabRelatorios = null!;
        private int _abaAtiva = 0;

        // paineis de conteudo
        private Panel panelTickets = null!;
        private Panel panelUsuarios = null!;
        private Panel panelRelatorios = null!;

        // grids
        private DataGridView dgvTickets = null!;
        private DataGridView dgvUsuarios = null!;
        private DataGridView dgvRelatorioDept = null!;
        private ListBox lstTopUsuarios = null!;

        // timer pra atualizar
        private WinTimer? _timerRefresh;

        public GerenteForm(int usuarioId, string nomeUsuario)
        {
            _usuarioId = usuarioId;
            _nomeUsuario = nomeUsuario;
            _apiClient = new ApiClient();

            CarregarImagens();
            InitializeComponent();
            MontarLayout();
            ConfigurarTimers();
            CarregarDadosIniciais();
        }

        private void CarregarImagens()
        {
            string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");

            string logoPath = Path.Combine(basePath, "dotia-logo.png");
            string pessoaPath = Path.Combine(basePath, "icon-pessoa.png");
            string ticketPath = Path.Combine(basePath, "icon-ticket.png");
            string checkPath = Path.Combine(basePath, "icon-check.png");
            string balaoPath = Path.Combine(basePath, "icon-balao.png");
            string graficoPath = Path.Combine(basePath, "icon-grafico.png");

            if (File.Exists(logoPath)) _logoImage = Image.FromFile(logoPath);
            if (File.Exists(pessoaPath)) _iconPessoa = Image.FromFile(pessoaPath);
            if (File.Exists(ticketPath)) _iconTicket = Image.FromFile(ticketPath);
            if (File.Exists(checkPath)) _iconCheck = Image.FromFile(checkPath);
            if (File.Exists(balaoPath)) _iconBalao = Image.FromFile(balaoPath);
            if (File.Exists(graficoPath)) _iconGrafico = Image.FromFile(graficoPath);
        }

        private void InitializeComponent()
        {
            Text = "DotIA - Painel do Gerente";
            WindowState = FormWindowState.Maximized;
            BackColor = DarkBg;
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(1400, 800);
            Font = new Font("Segoe UI", 10f);
        }

        private void MontarLayout()
        {
            SuspendLayout();

            // header
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = DarkerBg,
                Padding = new Padding(40, 20, 40, 20)
            };
            headerPanel.Paint += HeaderPanel_Paint;

            // logo
            var logoIcon = new PictureBox { Size = new Size(55, 55), Location = new Point(40, 12), SizeMode = PictureBoxSizeMode.Zoom, Image = _logoImage, BackColor = Color.Transparent };
            logoIcon.Paint += LogoIcon_Paint;

            // panel do texto do logo
            var logoTextPanel = new Panel
            {
                Size = new Size(250, 35),
                Location = new Point(105, 15),
                BackColor = Color.Transparent
            };
            logoTextPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                using (LinearGradientBrush brush = new LinearGradientBrush(
                    logoTextPanel.ClientRectangle, PrimaryBlue, PrimaryPurple, 135f))
                {
                    using (StringFormat sf = new StringFormat
                    {
                        Alignment = StringAlignment.Near,
                        LineAlignment = StringAlignment.Center
                    })
                    {
                        e.Graphics.DrawString("DotIA Manager", new Font("Segoe UI", 20, FontStyle.Bold),
                            brush, logoTextPanel.ClientRectangle, sf);
                    }
                }
            };

            var lblSubtitle = new Label
            {
                Text = "Painel de Gerenciamento",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(183, 188, 230),
                AutoSize = true,
                Location = new Point(105, 45),
                BackColor = Color.Transparent
            };

            // label do usuario
            lblUser = new Label
            {
                Text = $"Olá, {_nomeUsuario}",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                BackColor = Color.Transparent
            };

            btnSair = new Button
            {
                Text = "🚪 Sair",
                Size = new Size(100, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSair.FlatAppearance.BorderColor = PanelBorder;
            btnSair.FlatAppearance.BorderSize = 2;
            btnSair.Paint += BtnSair_Paint;
            btnSair.Click += (s, e) =>
            {
                this.Hide();
                var loginForm = new LoginForm();
                loginForm.FormClosed += (sender, args) => this.Close();
                loginForm.Show();
            };

            // reposiciona quando redimensiona
            headerPanel.Resize += (s, e) => AtualizarPosicaoHeader();

            headerPanel.Controls.Add(logoIcon);
            headerPanel.Controls.Add(logoTextPanel);
            headerPanel.Controls.Add(lblSubtitle);
            headerPanel.Controls.Add(lblUser);
            headerPanel.Controls.Add(btnSair);
            Controls.Add(headerPanel);

            // painel de conteudo - COM SCROLLBAR
            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = DarkBg,
                Padding = new Padding(40, 110, 40, 30),
                AutoScroll = true,
                AutoScrollMinSize = new Size(0, 1000) // Garante que a scrollbar apareça
            };
            Controls.Add(contentPanel);

            // painel de estatisticas
            statsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(0, 140),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Margin = new Padding(0, 0, 0, 20)
            };

            statsPanel.Controls.Add(CriarStatCard("Total de Usuários", _iconPessoa, out lblTotalUsuarios));
            statsPanel.Controls.Add(CriarStatCard("Tickets em Aberto", _iconTicket, out lblTicketsAbertos));
            statsPanel.Controls.Add(CriarStatCard("Tickets Resolvidos", _iconCheck, out lblTicketsResolvidos));
            statsPanel.Controls.Add(CriarStatCard("Total de Chats", _iconBalao, out lblTotalChats));
            statsPanel.Controls.Add(CriarStatCard("Resolvidos Hoje", _iconGrafico, out lblResolvidosHoje));
            statsPanel.Controls.Add(CriarStatCard("Chats Concluídos", _iconBalao, out lblChatsResolvidos));

            contentPanel.Controls.Add(statsPanel);

            // painel das abas
            tabsPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 10, 0, 0),
                Margin = new Padding(0, 0, 0, 10)
            };
            tabsPanel.Paint += TabsPanel_Paint;

            btnTabTickets = CriarBotaoTab("🎫 Tickets", 0);
            btnTabUsuarios = CriarBotaoTab("👥 Usuários", 1);
            btnTabRelatorios = CriarBotaoTab("📊 Relatórios", 2);

            btnTabTickets.Click += (s, e) => MudarAba(0);
            btnTabUsuarios.Click += (s, e) => MudarAba(1);
            btnTabRelatorios.Click += (s, e) => MudarAba(2);

            tabsPanel.Controls.Add(btnTabTickets);
            tabsPanel.Controls.Add(btnTabUsuarios);
            tabsPanel.Controls.Add(btnTabRelatorios);
            contentPanel.Controls.Add(tabsPanel);

            // conteudo das abas
            tabContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };
            contentPanel.Controls.Add(tabContent);

            CriarPanelTickets();
            CriarPanelUsuarios();
            CriarPanelRelatorios();

            MudarAba(0);

            // ordem dos controles
            contentPanel.Controls.SetChildIndex(tabContent, 0);
            contentPanel.Controls.SetChildIndex(tabsPanel, 1);
            contentPanel.Controls.SetChildIndex(statsPanel, 2);

            headerPanel.BringToFront();

            AtualizarPosicaoHeader();

            ResumeLayout();
        }

        // atualiza posicao do header
        private void AtualizarPosicaoHeader()
        {
            int marginRight = 40;
            int spacing = 20;

            // botao sair na direita
            btnSair.Location = new Point(headerPanel.Width - btnSair.Width - marginRight, 22);

            // nome do usuario do lado do botao
            lblUser.Location = new Point(btnSair.Left - lblUser.Width - spacing, 28);
        }

        private Button CriarBotaoTab(string texto, int index)
        {
            var btn = new Button
            {
                Text = "",
                Location = new Point(index * 200, 10),
                Size = new Size(180, 45),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Tag = new { Index = index, Texto = texto }
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btn.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btn.Paint += BtnTab_Paint;
            btn.MouseEnter += (s, e) => btn.Invalidate();
            btn.MouseLeave += (s, e) => btn.Invalidate();
            return btn;
        }

        private Panel CriarStatCard(string labelText, Image? icon, out Label valueLabel)
        {
            var card = new Panel
            {
                Size = new Size(280, 130),
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 20, 10)
            };
            card.Paint += StatCard_Paint;

            var iconBox = new PictureBox
            {
                Size = new Size(48, 48),
                Location = new Point(20, 35),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = icon,
                BackColor = Color.Transparent
            };

            var valuePanel = new Panel
            {
                Size = new Size(180, 45),
                Location = new Point(85, 35),
                BackColor = Color.Transparent,
                Tag = "0"
            };
            valuePanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                using (LinearGradientBrush brush = new LinearGradientBrush(valuePanel.ClientRectangle, PrimaryBlue, PrimaryPurple, 135f))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
                {
                    e.Graphics.DrawString(valuePanel.Tag?.ToString() ?? "0", new Font("Segoe UI", 26, FontStyle.Bold), brush, new Rectangle(0, 0, valuePanel.Width, valuePanel.Height), sf);
                }
            };

            valueLabel = new Label { Visible = false, Tag = valuePanel };
            var lblLabel = new Label { Text = labelText, Font = new Font("Segoe UI", 9.5f), ForeColor = Color.FromArgb(156, 163, 175), AutoSize = true, Location = new Point(20, 95), BackColor = Color.Transparent };

            card.Controls.Add(iconBox);
            card.Controls.Add(valuePanel);
            card.Controls.Add(lblLabel);
            return card;
        }

        private void CriarPanelTickets()
        {
            panelTickets = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Visible = false };

            var tituloPanel = CriarTituloPanel("🎫 Gerenciamento de Tickets");
            var containerGrid = CriarContainerGrid();

            dgvTickets = CriarDataGridView();
            containerGrid.Controls.Add(dgvTickets);

            panelTickets.Controls.Add(tituloPanel);
            panelTickets.Controls.Add(containerGrid);
            tabContent.Controls.Add(panelTickets);
        }

        private void CriarPanelUsuarios()
        {
            panelUsuarios = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Visible = false };

            var tituloPanel = CriarTituloPanel("👥 Gerenciamento de Usuários");
            var containerGrid = CriarContainerGrid();

            dgvUsuarios = CriarDataGridView();
            dgvUsuarios.CellContentClick += DgvUsuarios_CellContentClick;
            containerGrid.Controls.Add(dgvUsuarios);

            panelUsuarios.Controls.Add(tituloPanel);
            panelUsuarios.Controls.Add(containerGrid);
            tabContent.Controls.Add(panelUsuarios);
        }

        private void CriarPanelRelatorios()
        {
            panelRelatorios = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Visible = false, AutoScroll = true };

            var tituloPanel = CriarTituloPanel("📊 Relatórios e Estatísticas");

            var lblDept = new Label { Text = "Por Departamento", Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.White, AutoSize = true, Location = new Point(0, 50), BackColor = Color.Transparent };

            var containerDept = new Panel { Location = new Point(0, 85), Size = new Size(700, 400), BackColor = Color.Transparent };
            containerDept.Paint += ContainerGrid_Paint;
            dgvRelatorioDept = CriarDataGridView();
            containerDept.Controls.Add(dgvRelatorioDept);

            var lblTop = new Label { Text = "🏆 Top Usuários", Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.White, AutoSize = true, Location = new Point(750, 50), BackColor = Color.Transparent };

            var containerTop = new Panel { Location = new Point(750, 85), Size = new Size(400, 400), BackColor = Color.Transparent };
            containerTop.Paint += ContainerGrid_Paint;

            // MELHORIA VISUAL: ListBox customizada (OwnerDraw)
            lstTopUsuarios = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = PanelBg,
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10),
                DrawMode = DrawMode.OwnerDrawFixed, // Permite desenho manual
                ItemHeight = 45, // Altura maior para parecer card
                Margin = new Padding(2)
            };
            lstTopUsuarios.DrawItem += LstTopUsuarios_DrawItem;
            containerTop.Controls.Add(lstTopUsuarios);

            panelRelatorios.Controls.Add(tituloPanel);
            panelRelatorios.Controls.Add(lblDept);
            panelRelatorios.Controls.Add(containerDept);
            panelRelatorios.Controls.Add(lblTop);
            panelRelatorios.Controls.Add(containerTop);
            tabContent.Controls.Add(panelRelatorios);
        }

        // cria titulo das abas
        private Panel CriarTituloPanel(string texto)
        {
            var pnl = new Panel { Size = new Size(500, 35), Location = new Point(0, 0), BackColor = Color.Transparent };
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                using (LinearGradientBrush brush = new LinearGradientBrush(pnl.ClientRectangle, PrimaryBlue, PrimaryPurple, 135f))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
                {
                    e.Graphics.DrawString(texto, new Font("Segoe UI", 18, FontStyle.Bold), brush, pnl.ClientRectangle, sf);
                }
            };
            return pnl;
        }

        // container da grid
        private Panel CriarContainerGrid()
        {
            var pnl = new Panel
            {
                Location = new Point(0, 50),
                Size = new Size(tabContent.Width - 20, tabContent.Height - 70),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.Transparent
            };
            pnl.Paint += ContainerGrid_Paint;
            return pnl;
        }

        // config da datagridview
        private DataGridView CriarDataGridView()
        {
            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = PanelBg,
                ForeColor = TextColor,
                GridColor = PanelBorder,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 50,
                RowTemplate = { Height = 50 }
            };

            // estilo do header
            dgv.ColumnHeadersDefaultCellStyle.BackColor = PanelBg2;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(15, 0, 0, 0);
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            // estilo das celulas
            dgv.DefaultCellStyle.BackColor = PanelBg;
            dgv.DefaultCellStyle.ForeColor = TextColor;
            dgv.DefaultCellStyle.SelectionBackColor = GridSelectionColor;
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            dgv.DefaultCellStyle.Padding = new Padding(15, 0, 0, 0);
            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 10);
            dgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            // linhas alternadas
            dgv.AlternatingRowsDefaultCellStyle.BackColor = DarkerBg;

            return dgv;
        }

        // desenho customizado da listbox
        private void LstTopUsuarios_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            if (sender is not ListBox lb) return;

            e.DrawBackground();

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color bgColor = isSelected ? GridSelectionColor : (e.Index % 2 == 0 ? PanelBg : DarkerBg);

            // Fundo
            using (SolidBrush bgBrush = new SolidBrush(bgColor))
            {
                e.Graphics.FillRectangle(bgBrush, e.Bounds);
            }

            // Texto
            string text = lb.Items[e.Index].ToString() ?? "";
            using (Font font = new Font("Segoe UI", 10.5f))
            {
                Rectangle textRect = new Rectangle(e.Bounds.X + 15, e.Bounds.Y, e.Bounds.Width - 15, e.Bounds.Height);
                TextRenderer.DrawText(e.Graphics, text, font, textRect, Color.White, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }

            // Linha separadora sutil
            using (Pen pen = new Pen(PanelBorder, 1))
            {
                e.Graphics.DrawLine(pen, e.Bounds.X, e.Bounds.Bottom - 1, e.Bounds.Width, e.Bounds.Bottom - 1);
            }

            e.DrawFocusRectangle();
        }

        // paint handlers

        private void HeaderPanel_Paint(object? sender, PaintEventArgs e)
        {
            using (Pen pen = new Pen(PanelBorder, 2))
            {
                e.Graphics.DrawLine(pen, 0, headerPanel.Height - 2, headerPanel.Width, headerPanel.Height - 2);
            }
        }

        private void LogoIcon_Paint(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath shadowPath = new GraphicsPath())
            {
                shadowPath.AddEllipse(new Rectangle(-5, -5, 65, 65));
                using (PathGradientBrush shadowBrush = new PathGradientBrush(shadowPath))
                {
                    shadowBrush.CenterColor = Color.FromArgb(100, PrimaryBlue);
                    shadowBrush.SurroundColors = new[] { Color.Transparent };
                    e.Graphics.FillPath(shadowBrush, shadowPath);
                }
            }
        }

        private void BtnSair_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is not Button btn) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = GetRoundedRect(btn.ClientRectangle, 12))
            {
                using (Pen borderPen = new Pen(PanelBorder, 2))
                {
                    e.Graphics.DrawPath(borderPen, path);
                }
            }
            TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, btn.ClientRectangle, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private void StatCard_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel card) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = GetRoundedRect(card.ClientRectangle, 22))
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(card.ClientRectangle, PanelBg, DarkerBg, 135f))
                {
                    e.Graphics.FillPath(brush, path);
                }
                using (Pen borderPen = new Pen(PanelBorder, 2))
                {
                    e.Graphics.DrawPath(borderPen, path);
                }
            }
        }

        private void TabsPanel_Paint(object? sender, PaintEventArgs e)
        {
            using (Pen pen = new Pen(PanelBorder, 3))
            {
                e.Graphics.DrawLine(pen, 0, tabsPanel.Height - 3, tabsPanel.Width, tabsPanel.Height - 3);
            }
        }

        private void BtnTab_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is not Button btn) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            dynamic tagData = btn.Tag;
            int index = tagData.Index;
            string texto = tagData.Texto;
            bool isActive = index == _abaAtiva;
            bool isHover = btn.ClientRectangle.Contains(btn.PointToClient(Cursor.Position));

            if (isActive)
            {
                using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(25, PrimaryBlue))) { e.Graphics.FillRectangle(bgBrush, btn.ClientRectangle); }
                using (Pen borderPen = new Pen(PrimaryBlue, 4)) { e.Graphics.DrawLine(borderPen, 0, btn.Height - 4, btn.Width, btn.Height - 4); }
            }
            else if (isHover)
            {
                using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(18, PrimaryPurple))) { e.Graphics.FillRectangle(bgBrush, btn.ClientRectangle); }
            }

            Color textColor = isActive ? Color.White : Color.FromArgb(191, 191, 232);
            Rectangle textRect = new Rectangle(15, 0, btn.Width - 15, btn.Height);
            TextRenderer.DrawText(e.Graphics, texto, btn.Font, textRect, textColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }

        private void ContainerGrid_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel panel) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = GetRoundedRect(panel.ClientRectangle, 22))
            {
                using (SolidBrush bgBrush = new SolidBrush(PanelBg)) { e.Graphics.FillPath(bgBrush, path); }
                using (Pen borderPen = new Pen(PanelBorder, 2)) { e.Graphics.DrawPath(borderPen, path); }
            }
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

        // navegacao entre abas

        private void MudarAba(int index)
        {
            _abaAtiva = index;
            panelTickets.Visible = index == 0;
            panelUsuarios.Visible = index == 1;
            panelRelatorios.Visible = index == 2;
            btnTabTickets.Invalidate();
            btnTabUsuarios.Invalidate();
            btnTabRelatorios.Invalidate();

            if (index == 0) CarregarTicketsAsync();
            else if (index == 1) CarregarUsuariosAsync();
            else if (index == 2) CarregarRelatoriosAsync();
        }

        // dados

        private void ConfigurarTimers()
        {
            _timerRefresh = new WinTimer { Interval = 30000 };
            _timerRefresh.Tick += async (s, e) => await CarregarDashboardAsync();
            _timerRefresh.Start();
        }

        private async void CarregarDadosIniciais()
        {
            await CarregarDashboardAsync();
            await CarregarTicketsAsync();
        }

        private async System.Threading.Tasks.Task CarregarDashboardAsync()
        {
            try
            {
                var dashboard = await _apiClient.ObterDashboardAsync();
                AtualizarValorCard(lblTotalUsuarios, dashboard.TotalUsuarios.ToString());
                AtualizarValorCard(lblTicketsAbertos, dashboard.TicketsAbertos.ToString());
                AtualizarValorCard(lblTicketsResolvidos, dashboard.TicketsResolvidos.ToString());
                AtualizarValorCard(lblTotalChats, dashboard.TotalChats.ToString());
                AtualizarValorCard(lblResolvidosHoje, dashboard.TicketsResolvidosHoje.ToString());
                AtualizarValorCard(lblChatsResolvidos, dashboard.ChatsResolvidos.ToString());
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Erro ao carregar dashboard: {ex.Message}"); }
        }

        private void AtualizarValorCard(Label label, string valor)
        {
            if (label.Tag is Panel panel) { panel.Tag = valor; panel.Invalidate(); }
        }

        private async System.Threading.Tasks.Task CarregarTicketsAsync()
        {
            try
            {
                var tickets = await _apiClient.ObterTodosTicketsAsync();
                dgvTickets.DataSource = null;
                dgvTickets.Columns.Clear();

                if (tickets != null && tickets.Count > 0)
                {
                    var dataSource = tickets.Select(t => new
                    {
                        ID = t.Id,
                        Solicitante = t.NomeSolicitante,
                        Email = t.EmailSolicitante,
                        Departamento = t.Departamento,
                        Descrição = t.DescricaoProblema.Length > 50 ? t.DescricaoProblema.Substring(0, 50) + "..." : t.DescricaoProblema,
                        Status = t.Status,
                        DataAbertura = t.DataAbertura.ToString("dd/MM/yyyy HH:mm")
                    }).ToList();
                    dgvTickets.DataSource = dataSource;
                }
            }
            catch (Exception ex) { MessageBox.Show($"Erro ao carregar tickets: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private async System.Threading.Tasks.Task CarregarUsuariosAsync()
        {
            try
            {
                var usuarios = await _apiClient.ObterUsuariosAsync();
                dgvUsuarios.DataSource = null;
                dgvUsuarios.Columns.Clear();

                if (usuarios != null && usuarios.Count > 0)
                {
                    var dataSource = usuarios.Select(u => new
                    {
                        ID = u.Id,
                        Nome = u.Nome,
                        Email = u.Email,
                        Departamento = u.Departamento,
                        TotalTickets = u.TotalTickets,
                        TicketsAbertos = u.TicketsAbertos,
                        TotalChats = u.TotalChats
                    }).ToList();
                    dgvUsuarios.DataSource = dataSource;
                    dgvUsuarios.Columns.Add(new DataGridViewButtonColumn { Name = "Ações", Text = "⚙ Ações", UseColumnTextForButtonValue = true, Width = 100 });
                }
            }
            catch (Exception ex) { MessageBox.Show($"Erro ao carregar usuários: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private async System.Threading.Tasks.Task CarregarRelatoriosAsync()
        {
            try
            {
                var relatorio = await _apiClient.ObterRelatorioDepartamentosAsync();
                dgvRelatorioDept.DataSource = null;
                dgvRelatorioDept.Columns.Clear();
                if (relatorio != null && relatorio.Count > 0)
                {
                    var dataSource = relatorio.Select(r => new { Departamento = r.Departamento, Usuários = r.TotalUsuarios, Tickets = r.TotalTickets, Abertos = r.TicketsAbertos, Resolvidos = r.TicketsResolvidos }).ToList();
                    dgvRelatorioDept.DataSource = dataSource;
                }

                var dashboard = await _apiClient.ObterDashboardAsync();
                lstTopUsuarios.Items.Clear();
                if (dashboard.TopUsuarios != null && dashboard.TopUsuarios.Count > 0)
                {
                    int posicao = 1;
                    foreach (var usuario in dashboard.TopUsuarios)
                    {
                        lstTopUsuarios.Items.Add($"{posicao}. {usuario.Nome} - {usuario.TotalTickets} tickets");
                        posicao++;
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show($"Erro ao carregar relatórios: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void DgvUsuarios_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (dgvUsuarios.Columns[e.ColumnIndex].Name == "Ações")
            {
                var usuarioId = Convert.ToInt32(dgvUsuarios.Rows[e.RowIndex].Cells["ID"].Value);
                var nomeUsuario = dgvUsuarios.Rows[e.RowIndex].Cells["Nome"].Value.ToString();
                MostrarMenuAcoesUsuario(usuarioId, nomeUsuario);
            }
        }

        private void MostrarMenuAcoesUsuario(int usuarioId, string? nomeUsuario)
        {
            var menu = new ContextMenuStrip { BackColor = PanelBg, ForeColor = Color.White, Font = new Font("Segoe UI", 10) };
            menu.Items.Add("✏ Editar", null, async (s, e) => await EditarUsuarioAsync(usuarioId));
            menu.Items.Add("🛡 Alterar Cargo", null, async (s, e) => await AlterarCargoAsync(usuarioId, nomeUsuario ?? ""));
            menu.Items.Add("🔑 Alterar Senha", null, async (s, e) => await AlterarSenhaAsync(usuarioId, nomeUsuario ?? ""));
            menu.Items.Add("🗑 Excluir", null, async (s, e) => await ExcluirUsuarioAsync(usuarioId, nomeUsuario ?? ""));
            menu.Show(Cursor.Position);
        }

        private async System.Threading.Tasks.Task EditarUsuarioAsync(int usuarioId)
        {
            try
            {
                var usuario = await _apiClient.ObterUsuarioAsync(usuarioId);
                if (usuario == null) return;
                var departamentos = await _apiClient.ObterDepartamentosAsync();

                using (var formEditar = new Form { Text = "Editar Usuário", Size = new Size(500, 350), StartPosition = FormStartPosition.CenterParent, BackColor = DarkerBg, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false })
                {
                    int yPos = 20;
                    var lblNome = new Label { Text = "Nome:", Location = new Point(20, yPos), AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", 10f, FontStyle.Bold) }; formEditar.Controls.Add(lblNome); yPos += 30;
                    var txtNome = new TextBox { Text = usuario.Nome, Location = new Point(20, yPos), Size = new Size(440, 30), BackColor = PanelBg, ForeColor = TextColor, Font = new Font("Segoe UI", 10f) }; formEditar.Controls.Add(txtNome); yPos += 50;
                    var lblEmail = new Label { Text = "Email:", Location = new Point(20, yPos), AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", 10f, FontStyle.Bold) }; formEditar.Controls.Add(lblEmail); yPos += 30;
                    var txtEmail = new TextBox { Text = usuario.Email, Location = new Point(20, yPos), Size = new Size(440, 30), BackColor = PanelBg, ForeColor = TextColor, Font = new Font("Segoe UI", 10f) }; formEditar.Controls.Add(txtEmail); yPos += 50;
                    var lblDept = new Label { Text = "Departamento:", Location = new Point(20, yPos), AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", 10f, FontStyle.Bold) }; formEditar.Controls.Add(lblDept); yPos += 30;
                    var cmbDepartamento = new ComboBox { Location = new Point(20, yPos), Size = new Size(440, 30), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = PanelBg, ForeColor = TextColor, Font = new Font("Segoe UI", 10f) };
                    foreach (var dept in departamentos) cmbDepartamento.Items.Add(dept);
                    var deptAtual = departamentos.FirstOrDefault(d => d.Id == usuario.IdDepartamento);
                    if (deptAtual != null) cmbDepartamento.SelectedItem = deptAtual;
                    cmbDepartamento.DisplayMember = "Nome"; formEditar.Controls.Add(cmbDepartamento); yPos += 50;

                    var btnCancelar = new Button { Text = "Cancelar", Location = new Point(260, yPos), Size = new Size(100, 35), BackColor = Color.Gray, ForeColor = Color.White, Font = new Font("Segoe UI", 10f, FontStyle.Bold), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel }; formEditar.Controls.Add(btnCancelar);
                    var btnSalvar = new Button { Text = "Salvar", Location = new Point(370, yPos), Size = new Size(100, 35), BackColor = PrimaryGreen, ForeColor = Color.White, Font = new Font("Segoe UI", 10f, FontStyle.Bold), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.OK }; formEditar.Controls.Add(btnSalvar);
                    formEditar.AcceptButton = btnSalvar; formEditar.CancelButton = btnCancelar;

                    if (formEditar.ShowDialog(this) == DialogResult.OK)
                    {
                        var nome = txtNome.Text.Trim(); var email = txtEmail.Text.Trim(); var deptSelecionado = (DepartamentoDTO)cmbDepartamento.SelectedItem;
                        if (string.IsNullOrEmpty(nome) || string.IsNullOrEmpty(email)) { MessageBox.Show("Preencha tudo."); return; }
                        var sucesso = await _apiClient.AtualizarUsuarioAsync(usuarioId, nome, email, deptSelecionado.Id);
                        if (sucesso) { MessageBox.Show("Sucesso!"); await CarregarUsuariosAsync(); }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Erro: " + ex.Message); }
        }

        private async System.Threading.Tasks.Task AlterarCargoAsync(int usuarioId, string nomeUsuario)
        {
            try
            {
                using (var form = new Form { Text = $"Cargo: {nomeUsuario}", Size = new Size(400, 250), BackColor = DarkerBg, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false })
                {
                    int yPos = 20;
                    var lbl = new Label { Text = "Novo Cargo:", Location = new Point(20, yPos), AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", 11f, FontStyle.Bold) }; form.Controls.Add(lbl); yPos += 40;
                    var rb1 = new RadioButton { Text = "Solicitante", Location = new Point(30, yPos), AutoSize = true, ForeColor = TextColor, Font = new Font("Segoe UI", 10f), Checked = true }; form.Controls.Add(rb1); yPos += 30;
                    var rb2 = new RadioButton { Text = "Técnico", Location = new Point(30, yPos), AutoSize = true, ForeColor = TextColor, Font = new Font("Segoe UI", 10f) }; form.Controls.Add(rb2); yPos += 30;
                    var rb3 = new RadioButton { Text = "Gerente", Location = new Point(30, yPos), AutoSize = true, ForeColor = TextColor, Font = new Font("Segoe UI", 10f) }; form.Controls.Add(rb3); yPos += 50;

                    var btnOk = new Button { Text = "Alterar", Location = new Point(260, yPos), Size = new Size(100, 35), BackColor = PrimaryPurple, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.OK }; form.Controls.Add(btnOk);
                    form.AcceptButton = btnOk;

                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        string cargo = rb1.Checked ? "Solicitante" : rb2.Checked ? "Tecnico" : "Gerente";
                        if (MessageBox.Show($"Confirmar alteração para {cargo}?", "Confirmar", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            await _apiClient.AlterarCargoUsuarioAsync(usuarioId, cargo);
                            await CarregarUsuariosAsync();
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Erro: " + ex.Message); }
        }

        private async System.Threading.Tasks.Task AlterarSenhaAsync(int usuarioId, string nomeUsuario)
        {
            try
            {
                using (var form = new Form { Text = $"Senha: {nomeUsuario}", Size = new Size(400, 200), BackColor = DarkerBg, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false })
                {
                    var lbl = new Label { Text = "Nova Senha:", Location = new Point(20, 20), AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", 10f, FontStyle.Bold) }; form.Controls.Add(lbl);
                    var txt = new TextBox { Location = new Point(20, 50), Size = new Size(340, 30), BackColor = PanelBg, ForeColor = TextColor, UseSystemPasswordChar = true, Font = new Font("Segoe UI", 11f) }; form.Controls.Add(txt);
                    var btn = new Button { Text = "Salvar", Location = new Point(260, 100), Size = new Size(100, 35), BackColor = ColorTranslator.FromHtml("#f59e0b"), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.OK }; form.Controls.Add(btn);
                    form.AcceptButton = btn;

                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        if (txt.Text.Length < 6) { MessageBox.Show("Mínimo 6 caracteres."); return; }
                        await _apiClient.AlterarSenhaUsuarioAsync(usuarioId, txt.Text);
                        MessageBox.Show("Senha alterada!");
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Erro: " + ex.Message); }
        }

        private async System.Threading.Tasks.Task ExcluirUsuarioAsync(int usuarioId, string nomeUsuario)
        {
            if (MessageBox.Show($"Excluir usuário {nomeUsuario} permanentemente?", "Excluir", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    await _apiClient.ExcluirUsuarioAsync(usuarioId);
                    await CarregarUsuariosAsync();
                }
                catch (Exception ex) { MessageBox.Show("Erro: " + ex.Message); }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timerRefresh?.Stop(); _timerRefresh?.Dispose();
                _logoImage?.Dispose(); _iconPessoa?.Dispose(); _iconTicket?.Dispose(); _iconCheck?.Dispose(); _iconBalao?.Dispose(); _iconGrafico?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}