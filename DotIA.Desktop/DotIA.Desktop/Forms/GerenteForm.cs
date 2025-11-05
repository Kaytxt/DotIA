using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DotIA.Desktop.Services;

namespace DotIA.Desktop.Forms
{
    public partial class GerenteForm : Form
    {
        private readonly ApiClient _apiClient;
        private readonly int _usuarioId;
        private readonly string _nomeUsuario;

        // Cores
        private readonly Color PrimaryBlue = ColorTranslator.FromHtml("#3b82f6");
        private readonly Color SecondaryBlue = ColorTranslator.FromHtml("#2563eb");
        private readonly Color PrimaryPurple = ColorTranslator.FromHtml("#8b5cf6");
        private readonly Color PrimaryGreen = ColorTranslator.FromHtml("#10b981");
        private readonly Color DarkBg = ColorTranslator.FromHtml("#1a132f");
        private readonly Color DarkerBg = ColorTranslator.FromHtml("#1e1433");
        private readonly Color PanelBg = ColorTranslator.FromHtml("#221a3d");
        private readonly Color PanelBg2 = ColorTranslator.FromHtml("#20173a");
        private readonly Color PanelBorder = ColorTranslator.FromHtml("#3d2e6b");

        // Layout
        private Panel headerPanel;
        private Panel contentPanel;
        private TabControl tabControl;
        private TabPage tabDashboard;
        private TabPage tabTickets;
        private TabPage tabUsuarios;
        private TabPage tabRelatorios;

        // Dashboard
        private FlowLayoutPanel statsPanel;
        private Label lblTotalUsuarios;
        private Label lblTicketsAbertos;
        private Label lblTicketsResolvidos;
        private Label lblTotalChats;
        private Label lblResolvidosHoje;
        private Label lblChatsResolvidos;

        // Tickets
        private DataGridView dgvTickets;

        // Usuários
        private DataGridView dgvUsuarios;
        private Button btnNovoUsuario;
        private Button btnEditarUsuario;
        private Button btnExcluirUsuario;
        private Button btnAlterarSenha;
        private Button btnAlterarCargo;

        // Relatórios
        private DataGridView dgvRelatorio;
        private ListBox lstTopUsuarios;

        public GerenteForm(int usuarioId, string nomeUsuario)
        {
            _usuarioId = usuarioId;
            _nomeUsuario = nomeUsuario;
            _apiClient = new ApiClient();

            InitializeComponent();
            MontarLayout();
            CarregarDadosIniciais();
        }

        private void InitializeComponent()
        {
            Text = "DotIA - Painel do Gerente";
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = DarkBg;
            Size = new Size(1600, 900);
            FormBorderStyle = FormBorderStyle.Sizable;
            WindowState = FormWindowState.Maximized;
        }

        private void MontarLayout()
        {
            // HEADER
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = DarkerBg,
                Padding = new Padding(40, 20, 40, 20)
            };

            var logoIcon = new Panel
            {
                Size = new Size(55, 55),
                BackColor = PrimaryBlue,
                Location = new Point(40, 12)
            };

            var logoText = new Label
            {
                Text = "DotIA Manager",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(105, 15)
            };

            var lblSubtitle = new Label
            {
                Text = "Painel de Gerenciamento",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(183, 188, 230),
                AutoSize = true,
                Location = new Point(105, 45)
            };

            var lblUser = new Label
            {
                Text = $"Olá, {_nomeUsuario}",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(headerPanel.Width - 250, 20)
            };
            lblUser.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            var btnSair = new Button
            {
                Text = "?? Sair",
                Size = new Size(100, 36),
                Location = new Point(headerPanel.Width - 130, 22),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSair.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSair.FlatAppearance.BorderColor = PanelBorder;
            btnSair.Click += (s, e) => { this.Hide(); new LoginForm().Show(); };

            headerPanel.Controls.Add(logoIcon);
            headerPanel.Controls.Add(logoText);
            headerPanel.Controls.Add(lblSubtitle);
            headerPanel.Controls.Add(lblUser);
            headerPanel.Controls.Add(btnSair);
            Controls.Add(headerPanel);

            // CONTENT
            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = DarkBg,
                Padding = new Padding(40, 30, 40, 30)
            };
            Controls.Add(contentPanel);

            // TAB CONTROL
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                DrawMode = TabDrawMode.OwnerDrawFixed,
                ItemSize = new Size(200, 50),
                SizeMode = TabSizeMode.Fixed
            };
            tabControl.DrawItem += TabControl_DrawItem;
            contentPanel.Controls.Add(tabControl);

            // TABS
            tabDashboard = new TabPage("?? Dashboard");
            tabTickets = new TabPage("?? Tickets");
            tabUsuarios = new TabPage("?? Usuários");
            tabRelatorios = new TabPage("?? Relatórios");

            tabControl.TabPages.Add(tabDashboard);
            tabControl.TabPages.Add(tabTickets);
            tabControl.TabPages.Add(tabUsuarios);
            tabControl.TabPages.Add(tabRelatorios);

            ConfigurarTabDashboard();
            ConfigurarTabTickets();
            ConfigurarTabUsuarios();
            ConfigurarTabRelatorios();
        }

        private void TabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            var g = e.Graphics;
            var tab = tabControl.TabPages[e.Index];
            var bounds = tabControl.GetTabRect(e.Index);

            var bgColor = e.Index == tabControl.SelectedIndex ? PrimaryBlue : DarkerBg;
            using var bg = new SolidBrush(bgColor);
            g.FillRectangle(bg, bounds);

            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var f = new Font("Segoe UI", 11, FontStyle.Bold);
            g.DrawString(tab.Text, f, Brushes.White, bounds, sf);
        }

        private void ConfigurarTabDashboard()
        {
            tabDashboard.BackColor = DarkBg;
            tabDashboard.Padding = new Padding(20);

            // Stats Grid
            statsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 150,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };

            statsPanel.Controls.Add(CriarStatCard("??", "Total de Usuários", out lblTotalUsuarios));
            statsPanel.Controls.Add(CriarStatCard("??", "Tickets em Aberto", out lblTicketsAbertos));
            statsPanel.Controls.Add(CriarStatCard("?", "Tickets Resolvidos", out lblTicketsResolvidos));
            statsPanel.Controls.Add(CriarStatCard("??", "Total de Chats", out lblTotalChats));
            statsPanel.Controls.Add(CriarStatCard("??", "Resolvidos Hoje", out lblResolvidosHoje));
            statsPanel.Controls.Add(CriarStatCard("??", "Chats Concluídos", out lblChatsResolvidos));

            tabDashboard.Controls.Add(statsPanel);
        }

        private Panel CriarStatCard(string icon, string label, out Label valueLabel)
        {
            var card = new Panel
            {
                Size = new Size(280, 120),
                BackColor = PanelBg,
                Margin = new Padding(10),
                Padding = new Padding(20)
            };

            var lblIcon = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI Emoji", 32),
                AutoSize = true,
                Location = new Point(20, 15)
            };

            valueLabel = new Label
            {
                Text = "0",
                Font = new Font("Segoe UI", 32, FontStyle.Bold),
                ForeColor = PrimaryBlue,
                AutoSize = true,
                Location = new Point(80, 20)
            };

            var lblLabel = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.Silver,
                AutoSize = true,
                Location = new Point(20, 85)
            };

            card.Controls.Add(lblIcon);
            card.Controls.Add(valueLabel);
            card.Controls.Add(lblLabel);

            return card;
        }

        private void ConfigurarTabTickets()
        {
            tabTickets.BackColor = DarkBg;
            tabTickets.Padding = new Padding(20);

            var lblTitle = new Label
            {
                Text = "Gerenciamento de Tickets",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 20)
            };
            tabTickets.Controls.Add(lblTitle);

            dgvTickets = new DataGridView
            {
                Location = new Point(20, 70),
                Size = new Size(tabTickets.Width - 40, tabTickets.Height - 100),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackgroundColor = PanelBg,
                ForeColor = Color.White,
                GridColor = PanelBorder,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };

            dgvTickets.ColumnHeadersDefaultCellStyle.BackColor = PanelBg2;
            dgvTickets.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvTickets.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dgvTickets.RowsDefaultCellStyle.BackColor = PanelBg;
            dgvTickets.RowsDefaultCellStyle.ForeColor = Color.White;
            dgvTickets.AlternatingRowsDefaultCellStyle.BackColor = DarkerBg;

            tabTickets.Controls.Add(dgvTickets);
        }

        private void ConfigurarTabUsuarios()
        {
            tabUsuarios.BackColor = DarkBg;
            tabUsuarios.Padding = new Padding(20);

            var lblTitle = new Label
            {
                Text = "Gerenciamento de Usuários",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 20)
            };
            tabUsuarios.Controls.Add(lblTitle);

            // Botões de ação
            var btnPanel = new FlowLayoutPanel
            {
                Location = new Point(20, 60),
                Height = 50,
                Width = tabUsuarios.Width - 40,
                FlowDirection = FlowDirection.LeftToRight
            };

            btnEditarUsuario = CriarBotaoAcao("? Editar");
            btnAlterarCargo = CriarBotaoAcao("?? Alterar Cargo");
            btnAlterarSenha = CriarBotaoAcao("?? Alterar Senha");
            btnExcluirUsuario = CriarBotaoAcao("?? Excluir");

            btnEditarUsuario.Click += async (s, e) => await EditarUsuarioAsync();
            btnAlterarCargo.Click += async (s, e) => await AlterarCargoAsync();
            btnAlterarSenha.Click += async (s, e) => await AlterarSenhaAsync();
            btnExcluirUsuario.Click += async (s, e) => await ExcluirUsuarioAsync();

            btnPanel.Controls.Add(btnEditarUsuario);
            btnPanel.Controls.Add(btnAlterarCargo);
            btnPanel.Controls.Add(btnAlterarSenha);
            btnPanel.Controls.Add(btnExcluirUsuario);
            tabUsuarios.Controls.Add(btnPanel);

            dgvUsuarios = new DataGridView
            {
                Location = new Point(20, 120),
                Size = new Size(tabUsuarios.Width - 40, tabUsuarios.Height - 150),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackgroundColor = PanelBg,
                ForeColor = Color.White,
                GridColor = PanelBorder,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };

            dgvUsuarios.ColumnHeadersDefaultCellStyle.BackColor = PanelBg2;
            dgvUsuarios.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvUsuarios.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dgvUsuarios.RowsDefaultCellStyle.BackColor = PanelBg;
            dgvUsuarios.RowsDefaultCellStyle.ForeColor = Color.White;
            dgvUsuarios.AlternatingRowsDefaultCellStyle.BackColor = DarkerBg;

            tabUsuarios.Controls.Add(dgvUsuarios);
        }

        private Button CriarBotaoAcao(string texto)
        {
            var btn = new Button
            {
                Text = texto,
                Width = 150,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                BackColor = PrimaryBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void ConfigurarTabRelatorios()
        {
            tabRelatorios.BackColor = DarkBg;
            tabRelatorios.Padding = new Padding(20);

            var lblTitle = new Label
            {
                Text = "Relatórios e Estatísticas",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 20)
            };
            tabRelatorios.Controls.Add(lblTitle);

            // Relatório por Departamento
            var lblDept = new Label
            {
                Text = "Por Departamento",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 70)
            };
            tabRelatorios.Controls.Add(lblDept);

            dgvRelatorio = new DataGridView
            {
                Location = new Point(20, 100),
                Size = new Size(700, 400),
                BackgroundColor = PanelBg,
                ForeColor = Color.White,
                GridColor = PanelBorder,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            dgvRelatorio.ColumnHeadersDefaultCellStyle.BackColor = PanelBg2;
            dgvRelatorio.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvRelatorio.RowsDefaultCellStyle.BackColor = PanelBg;
            dgvRelatorio.RowsDefaultCellStyle.ForeColor = Color.White;

            tabRelatorios.Controls.Add(dgvRelatorio);

            // Top Usuários
            var lblTop = new Label
            {
                Text = "Top Usuários",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(750, 70)
            };
            tabRelatorios.Controls.Add(lblTop);

            lstTopUsuarios = new ListBox
            {
                Location = new Point(750, 100),
                Size = new Size(400, 400),
                BackColor = PanelBg,
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10)
            };
            tabRelatorios.Controls.Add(lstTopUsuarios);
        }

        private async void CarregarDadosIniciais()
        {
            await CarregarDashboardAsync();
            await CarregarTicketsAsync();
            await CarregarUsuariosAsync();
            await CarregarRelatoriosAsync();
        }

        // Continuação da classe GerenteForm - Métodos de dados e ações

        private async System.Threading.Tasks.Task CarregarDashboardAsync()
        {
            try
            {
                var dashboard = await _apiClient.ObterDashboardAsync();

                lblTotalUsuarios.Text = dashboard.TotalUsuarios.ToString();
                lblTicketsAbertos.Text = dashboard.TicketsAbertos.ToString();
                lblTicketsResolvidos.Text = dashboard.TicketsResolvidos.ToString();
                lblTotalChats.Text = dashboard.TotalChats.ToString();
                lblResolvidosHoje.Text = dashboard.TicketsResolvidosHoje.ToString();
                lblChatsResolvidos.Text = dashboard.ChatsResolvidos.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar dashboard: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                        Descrição = t.DescricaoProblema.Length > 50
                            ? t.DescricaoProblema.Substring(0, 50) + "..."
                            : t.DescricaoProblema,
                        Status = t.Status,
                        DataAbertura = t.DataAbertura.ToString("dd/MM/yyyy HH:mm")
                    }).ToList();

                    dgvTickets.DataSource = dataSource;

                    // Ajusta largura das colunas
                    dgvTickets.Columns["ID"].Width = 60;
                    dgvTickets.Columns["Solicitante"].Width = 150;
                    dgvTickets.Columns["Email"].Width = 200;
                    dgvTickets.Columns["Departamento"].Width = 120;
                    dgvTickets.Columns["Status"].Width = 100;
                    dgvTickets.Columns["DataAbertura"].Width = 140;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar tickets: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

                    // Ajusta largura das colunas
                    dgvUsuarios.Columns["ID"].Width = 60;
                    dgvUsuarios.Columns["Nome"].Width = 200;
                    dgvUsuarios.Columns["Email"].Width = 250;
                    dgvUsuarios.Columns["Departamento"].Width = 150;
                    dgvUsuarios.Columns["TotalTickets"].Width = 120;
                    dgvUsuarios.Columns["TicketsAbertos"].Width = 120;
                    dgvUsuarios.Columns["TotalChats"].Width = 100;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar usuários: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async System.Threading.Tasks.Task CarregarRelatoriosAsync()
        {
            try
            {
                // Relatório por Departamento
                var relatorio = await _apiClient.ObterRelatorioDepartamentosAsync();

                dgvRelatorio.DataSource = null;
                dgvRelatorio.Columns.Clear();

                if (relatorio != null && relatorio.Count > 0)
                {
                    var dataSource = relatorio.Select(r => new
                    {
                        Departamento = r.Departamento,
                        Usuários = r.TotalUsuarios,
                        Tickets = r.TotalTickets,
                        Abertos = r.TicketsAbertos,
                        Resolvidos = r.TicketsResolvidos
                    }).ToList();

                    dgvRelatorio.DataSource = dataSource;
                }

                // Top Usuários
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
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar relatórios: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async System.Threading.Tasks.Task EditarUsuarioAsync()
        {
            if (dgvUsuarios.SelectedRows.Count == 0)
            {
                MessageBox.Show("Selecione um usuário para editar.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var usuarioId = Convert.ToInt32(dgvUsuarios.SelectedRows[0].Cells["ID"].Value);

            try
            {
                var usuario = await _apiClient.ObterUsuarioAsync(usuarioId);

                if (usuario == null)
                {
                    MessageBox.Show("Erro ao carregar dados do usuário.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                using var form = new Form
                {
                    Text = "Editar Usuário",
                    StartPosition = FormStartPosition.CenterParent,
                    BackColor = DarkerBg,
                    ForeColor = Color.White,
                    Size = new Size(500, 350),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false
                };

                var lblNome = new Label { Text = "Nome:", Location = new Point(20, 20), ForeColor = Color.White, AutoSize = true };
                var txtNome = new TextBox { Location = new Point(20, 45), Width = 440, Text = usuario.Nome, BackColor = PanelBg, ForeColor = Color.White };

                var lblEmail = new Label { Text = "Email:", Location = new Point(20, 85), ForeColor = Color.White, AutoSize = true };
                var txtEmail = new TextBox { Location = new Point(20, 110), Width = 440, Text = usuario.Email, BackColor = PanelBg, ForeColor = Color.White };

                var lblDept = new Label { Text = "Departamento:", Location = new Point(20, 150), ForeColor = Color.White, AutoSize = true };
                var cboDept = new ComboBox { Location = new Point(20, 175), Width = 440, BackColor = PanelBg, ForeColor = Color.White, DropDownStyle = ComboBoxStyle.DropDownList };

                // Carrega departamentos
                var departamentos = await _apiClient.ObterDepartamentosAsync();
                foreach (var dept in departamentos)
                {
                    cboDept.Items.Add(dept);
                    cboDept.DisplayMember = "Nome";
                    cboDept.ValueMember = "Id";
                }

                var deptSelecionado = departamentos.FirstOrDefault(d => d.Id == usuario.IdDepartamento);
                if (deptSelecionado != null)
                {
                    cboDept.SelectedItem = deptSelecionado;
                }

                var btnSalvar = new Button
                {
                    Text = "Salvar",
                    Location = new Point(280, 230),
                    Width = 90,
                    Height = 40,
                    BackColor = PrimaryGreen,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                btnSalvar.FlatAppearance.BorderSize = 0;

                var btnCancelar = new Button
                {
                    Text = "Cancelar",
                    Location = new Point(380, 230),
                    Width = 80,
                    Height = 40,
                    BackColor = PanelBg,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                btnCancelar.FlatAppearance.BorderSize = 0;
                btnCancelar.Click += (s, e) => form.DialogResult = DialogResult.Cancel;

                btnSalvar.Click += async (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(txtNome.Text) || string.IsNullOrWhiteSpace(txtEmail.Text) || cboDept.SelectedItem == null)
                    {
                        MessageBox.Show("Preencha todos os campos.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var deptSel = (DepartamentoDTO)cboDept.SelectedItem;
                    var sucesso = await _apiClient.AtualizarUsuarioAsync(usuarioId, txtNome.Text, txtEmail.Text, deptSel.Id);

                    if (sucesso)
                    {
                        MessageBox.Show("Usuário atualizado com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        form.DialogResult = DialogResult.OK;
                        await CarregarUsuariosAsync();
                        await CarregarDashboardAsync();
                    }
                    else
                    {
                        MessageBox.Show("Erro ao atualizar usuário.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                form.Controls.Add(lblNome);
                form.Controls.Add(txtNome);
                form.Controls.Add(lblEmail);
                form.Controls.Add(txtEmail);
                form.Controls.Add(lblDept);
                form.Controls.Add(cboDept);
                form.Controls.Add(btnSalvar);
                form.Controls.Add(btnCancelar);

                form.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async System.Threading.Tasks.Task AlterarSenhaAsync()
        {
            if (dgvUsuarios.SelectedRows.Count == 0)
            {
                MessageBox.Show("Selecione um usuário.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var usuarioId = Convert.ToInt32(dgvUsuarios.SelectedRows[0].Cells["ID"].Value);
            var nomeUsuario = dgvUsuarios.SelectedRows[0].Cells["Nome"].Value.ToString();

            using var form = new Form
            {
                Text = $"Alterar Senha - {nomeUsuario}",
                StartPosition = FormStartPosition.CenterParent,
                BackColor = DarkerBg,
                ForeColor = Color.White,
                Size = new Size(450, 280),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var lblSenha = new Label { Text = "Nova Senha:", Location = new Point(20, 20), ForeColor = Color.White, AutoSize = true };
            var txtSenha = new TextBox { Location = new Point(20, 45), Width = 390, PasswordChar = '?', BackColor = PanelBg, ForeColor = Color.White };

            var lblConfirm = new Label { Text = "Confirmar Senha:", Location = new Point(20, 85), ForeColor = Color.White, AutoSize = true };
            var txtConfirm = new TextBox { Location = new Point(20, 110), Width = 390, PasswordChar = '?', BackColor = PanelBg, ForeColor = Color.White };

            var btnSalvar = new Button
            {
                Text = "Alterar",
                Location = new Point(230, 170),
                Width = 90,
                Height = 40,
                BackColor = PrimaryGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSalvar.FlatAppearance.BorderSize = 0;

            var btnCancelar = new Button
            {
                Text = "Cancelar",
                Location = new Point(330, 170),
                Width = 80,
                Height = 40,
                BackColor = PanelBg,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += (s, e) => form.DialogResult = DialogResult.Cancel;

            btnSalvar.Click += async (s, e) =>
            {
                if (txtSenha.Text.Length < 6)
                {
                    MessageBox.Show("A senha deve ter no mínimo 6 caracteres.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (txtSenha.Text != txtConfirm.Text)
                {
                    MessageBox.Show("As senhas não coincidem.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var sucesso = await _apiClient.AlterarSenhaUsuarioAsync(usuarioId, txtSenha.Text);

                if (sucesso)
                {
                    MessageBox.Show("Senha alterada com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    form.DialogResult = DialogResult.OK;
                }
                else
                {
                    MessageBox.Show("Erro ao alterar senha.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            form.Controls.Add(lblSenha);
            form.Controls.Add(txtSenha);
            form.Controls.Add(lblConfirm);
            form.Controls.Add(txtConfirm);
            form.Controls.Add(btnSalvar);
            form.Controls.Add(btnCancelar);

            form.ShowDialog(this);
        }

        private async System.Threading.Tasks.Task AlterarCargoAsync()
        {
            if (dgvUsuarios.SelectedRows.Count == 0)
            {
                MessageBox.Show("Selecione um usuário.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var usuarioId = Convert.ToInt32(dgvUsuarios.SelectedRows[0].Cells["ID"].Value);
            var nomeUsuario = dgvUsuarios.SelectedRows[0].Cells["Nome"].Value.ToString();

            using var form = new Form
            {
                Text = $"Alterar Cargo - {nomeUsuario}",
                StartPosition = FormStartPosition.CenterParent,
                BackColor = DarkerBg,
                ForeColor = Color.White,
                Size = new Size(450, 280),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var lblCargo = new Label { Text = "Novo Cargo:", Location = new Point(20, 20), ForeColor = Color.White, AutoSize = true };
            var cboCargo = new ComboBox
            {
                Location = new Point(20, 45),
                Width = 390,
                BackColor = PanelBg,
                ForeColor = Color.White,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboCargo.Items.Add("Solicitante");
            cboCargo.Items.Add("Tecnico");
            cboCargo.Items.Add("Gerente");
            cboCargo.SelectedIndex = 0;

            var lblAviso = new Label
            {
                Text = "? Atenção: Ao promover para Técnico/Gerente,\no usuário terá acesso ao painel administrativo.",
                Location = new Point(20, 90),
                ForeColor = Color.FromArgb(251, 191, 36),
                AutoSize = true
            };

            var btnSalvar = new Button
            {
                Text = "Alterar",
                Location = new Point(230, 160),
                Width = 90,
                Height = 40,
                BackColor = PrimaryGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSalvar.FlatAppearance.BorderSize = 0;

            var btnCancelar = new Button
            {
                Text = "Cancelar",
                Location = new Point(330, 160),
                Width = 80,
                Height = 40,
                BackColor = PanelBg,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += (s, e) => form.DialogResult = DialogResult.Cancel;

            btnSalvar.Click += async (s, e) =>
            {
                var cargo = cboCargo.SelectedItem.ToString();

                if (MessageBox.Show($"Tem certeza que deseja alterar o cargo para {cargo}?", "Confirmação", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                var sucesso = await _apiClient.AlterarCargoUsuarioAsync(usuarioId, cargo);

                if (sucesso)
                {
                    MessageBox.Show($"Cargo alterado para {cargo} com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    form.DialogResult = DialogResult.OK;
                    await CarregarUsuariosAsync();
                    await CarregarDashboardAsync();
                }
                else
                {
                    MessageBox.Show("Erro ao alterar cargo.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            form.Controls.Add(lblCargo);
            form.Controls.Add(cboCargo);
            form.Controls.Add(lblAviso);
            form.Controls.Add(btnSalvar);
            form.Controls.Add(btnCancelar);

            form.ShowDialog(this);
        }

        private async System.Threading.Tasks.Task ExcluirUsuarioAsync()
        {
            if (dgvUsuarios.SelectedRows.Count == 0)
            {
                MessageBox.Show("Selecione um usuário para excluir.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var usuarioId = Convert.ToInt32(dgvUsuarios.SelectedRows[0].Cells["ID"].Value);
            var nomeUsuario = dgvUsuarios.SelectedRows[0].Cells["Nome"].Value.ToString();

            if (MessageBox.Show(
                $"Tem certeza que deseja excluir o usuário \"{nomeUsuario}\"?\n\n" +
                "ISTO IRÁ DELETAR:\n" +
                "? Todos os chats\n" +
                "? Todos os tickets\n" +
                "? Todo o histórico\n\n" +
                "Esta ação não pode ser desfeita!",
                "Confirmação",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                var sucesso = await _apiClient.ExcluirUsuarioAsync(usuarioId);

                if (sucesso)
                {
                    MessageBox.Show("Usuário excluído com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await CarregarUsuariosAsync();
                    await CarregarDashboardAsync();
                }
                else
                {
                    MessageBox.Show("Erro ao excluir usuário.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}