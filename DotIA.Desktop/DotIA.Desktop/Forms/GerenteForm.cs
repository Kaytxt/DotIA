// DotIA.Desktop/DotIA.Desktop/Forms/GerenteForm.cs
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using DotIA.Desktop.Services;
using Microsoft.VisualBasic;

namespace DotIA.Desktop.Forms
{
    public partial class GerenteForm : Form
    {
        private readonly ApiClient _apiClient;
        private readonly int _usuarioId;
        private readonly string _nomeUsuario;

        private TabControl tabControl;
        private TabPage tabDashboard;
        private TabPage tabTickets;
        private TabPage tabUsuarios;
        private TabPage tabRelatorios;

        private Label lblTotalUsuarios;
        private Label lblTicketsAbertos;
        private Label lblTicketsResolvidos;
        private Label lblTotalChats;
        private Label lblResolvidosHoje;
        private Label lblChatsResolvidos;

        private DataGridView dgvTickets;
        private DataGridView dgvUsuarios;
        private DataGridView dgvRelatorios;

        private System.Windows.Forms.Timer refreshTimer;
        private DashboardDTO dashboard;
        private List<UsuarioDTO> usuarios;
        private List<TicketGerenteDTO> tickets;

        public GerenteForm(int usuarioId, string nomeUsuario)
        {
            _usuarioId = usuarioId;
            _nomeUsuario = nomeUsuario;
            _apiClient = new ApiClient();
            InitializeComponent();
            IniciarAutoRefresh();
            CarregarDados();
        }

        private void InitializeComponent()
        {
            this.Text = "DotIA - Painel do Gerente";
            this.Size = new Size(1400, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(26, 19, 47);
            this.WindowState = FormWindowState.Maximized;

            // Header
            Panel headerPanel = new Panel
            {
                Height = 80,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(30, 20, 51),
            };

            Label lblTitulo = new Label
            {
                Text = "????? DotIA Manager",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = Color.FromArgb(139, 92, 246),
                Location = new Point(30, 20),
                AutoSize = true
            };

            Label lblSubtitulo = new Label
            {
                Text = "Painel de Gerenciamento",
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.LightGray,
                Location = new Point(30, 50),
                AutoSize = true
            };

            Label lblUsuario = new Label
            {
                Text = $"Olá, {_nomeUsuario}",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(this.Width - 200, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                AutoSize = true
            };

            headerPanel.Controls.Add(lblTitulo);
            headerPanel.Controls.Add(lblSubtitulo);
            headerPanel.Controls.Add(lblUsuario);

            // TabControl
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11),
                ItemSize = new Size(150, 40)
            };

            // ???????????????????????????????????????????????????????????
            // TAB DASHBOARD
            // ???????????????????????????????????????????????????????????
            tabDashboard = new TabPage("?? Dashboard");
            tabDashboard.BackColor = Color.FromArgb(30, 20, 51);

            FlowLayoutPanel statsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 200,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(20)
            };

            // Cards de estatísticas
            var statCards = new[]
            {
                CreateStatCard("??", "Total de Usuários", ref lblTotalUsuarios, Color.FromArgb(59, 130, 246)),
                CreateStatCard("??", "Tickets em Aberto", ref lblTicketsAbertos, Color.FromArgb(251, 191, 36)),
                CreateStatCard("?", "Tickets Resolvidos", ref lblTicketsResolvidos, Color.FromArgb(16, 185, 129)),
                CreateStatCard("??", "Total de Chats", ref lblTotalChats, Color.FromArgb(139, 92, 246)),
                CreateStatCard("??", "Resolvidos Hoje", ref lblResolvidosHoje, Color.FromArgb(236, 72, 153)),
                CreateStatCard("??", "Chats Concluídos", ref lblChatsResolvidos, Color.FromArgb(6, 182, 212))
            };

            foreach (var card in statCards)
            {
                statsPanel.Controls.Add(card);
            }

            // Lista de Top Usuários
            GroupBox grpTopUsuarios = new GroupBox
            {
                Text = "?? Top Usuários com Mais Tickets",
                Location = new Point(20, 220),
                Size = new Size(600, 300),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold)
            };

            ListBox lstTopUsuarios = new ListBox
            {
                Name = "lstTopUsuarios",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(44, 32, 77),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11),
                BorderStyle = BorderStyle.None
            };

            grpTopUsuarios.Controls.Add(lstTopUsuarios);

            tabDashboard.Controls.Add(grpTopUsuarios);
            tabDashboard.Controls.Add(statsPanel);

            // ???????????????????????????????????????????????????????????
            // TAB TICKETS
            // ???????????????????????????????????????????????????????????
            tabTickets = new TabPage("?? Tickets");
            tabTickets.BackColor = Color.FromArgb(30, 20, 51);

            dgvTickets = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(30, 20, 51),
                GridColor = Color.FromArgb(44, 32, 77),
                BorderStyle = BorderStyle.None,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(44, 32, 77),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    SelectionBackColor = Color.FromArgb(44, 32, 77)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(30, 20, 51),
                    ForeColor = Color.White,
                    SelectionBackColor = Color.FromArgb(139, 92, 246),
                    SelectionForeColor = Color.White
                },
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            tabTickets.Controls.Add(dgvTickets);

            // ???????????????????????????????????????????????????????????
            // TAB USUÁRIOS
            // ???????????????????????????????????????????????????????????
            tabUsuarios = new TabPage("?? Usuários");
            tabUsuarios.BackColor = Color.FromArgb(30, 20, 51);

            Panel toolbarUsuarios = new Panel
            {
                Height = 60,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(26, 19, 47),
                Padding = new Padding(10)
            };

            Button btnEditarUsuario = new Button
            {
                Text = "?? Editar",
                Location = new Point(10, 10),
                Size = new Size(100, 40),
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnEditarUsuario.Click += BtnEditarUsuario_Click;

            Button btnAlterarSenha = new Button
            {
                Text = "?? Senha",
                Location = new Point(120, 10),
                Size = new Size(100, 40),
                BackColor = Color.FromArgb(251, 191, 36),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAlterarSenha.Click += BtnAlterarSenha_Click;

            Button btnAlterarCargo = new Button
            {
                Text = "?? Cargo",
                Location = new Point(230, 10),
                Size = new Size(100, 40),
                BackColor = Color.FromArgb(139, 92, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAlterarCargo.Click += BtnAlterarCargo_Click;

            Button btnExcluirUsuario = new Button
            {
                Text = "??? Excluir",
                Location = new Point(340, 10),
                Size = new Size(100, 40),
                BackColor = Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnExcluirUsuario.Click += BtnExcluirUsuario_Click;

            toolbarUsuarios.Controls.Add(btnEditarUsuario);
            toolbarUsuarios.Controls.Add(btnAlterarSenha);
            toolbarUsuarios.Controls.Add(btnAlterarCargo);
            toolbarUsuarios.Controls.Add(btnExcluirUsuario);

            dgvUsuarios = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(30, 20, 51),
                GridColor = Color.FromArgb(44, 32, 77),
                BorderStyle = BorderStyle.None,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(44, 32, 77),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    SelectionBackColor = Color.FromArgb(44, 32, 77)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(30, 20, 51),
                    ForeColor = Color.White,
                    SelectionBackColor = Color.FromArgb(139, 92, 246),
                    SelectionForeColor = Color.White
                },
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            tabUsuarios.Controls.Add(dgvUsuarios);
            tabUsuarios.Controls.Add(toolbarUsuarios);

            // ???????????????????????????????????????????????????????????
            // TAB RELATÓRIOS
            // ???????????????????????????????????????????????????????????
            tabRelatorios = new TabPage("?? Relatórios");
            tabRelatorios.BackColor = Color.FromArgb(30, 20, 51);

            dgvRelatorios = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(30, 20, 51),
                GridColor = Color.FromArgb(44, 32, 77),
                BorderStyle = BorderStyle.None,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(44, 32, 77),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    SelectionBackColor = Color.FromArgb(44, 32, 77)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(30, 20, 51),
                    ForeColor = Color.White,
                    SelectionBackColor = Color.FromArgb(139, 92, 246),
                    SelectionForeColor = Color.White
                },
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            tabRelatorios.Controls.Add(dgvRelatorios);

            // Adicionar tabs
            tabControl.TabPages.Add(tabDashboard);
            tabControl.TabPages.Add(tabTickets);
            tabControl.TabPages.Add(tabUsuarios);
            tabControl.TabPages.Add(tabRelatorios);

            this.Controls.Add(tabControl);
            this.Controls.Add(headerPanel);

            // Botão Sair
            Button btnSair = new Button
            {
                Text = "Sair",
                Size = new Size(100, 30),
                Location = new Point(this.Width - 120, 35),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSair.Click += (s, e) => Application.Restart();
            headerPanel.Controls.Add(btnSair);
        }

        private Panel CreateStatCard(string icon, string label, ref Label valueLabel, Color color)
        {
            Panel card = new Panel
            {
                Size = new Size(200, 140),
                BackColor = Color.FromArgb(44, 32, 77),
                Margin = new Padding(10),
                Padding = new Padding(20)
            };

            Label lblIcon = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI", 32),
                Location = new Point(20, 10),
                Size = new Size(60, 60)
            };

            valueLabel = new Label
            {
                Text = "0",
                Font = new Font("Segoe UI", 28, FontStyle.Bold),
                ForeColor = color,
                Location = new Point(90, 20),
                AutoSize = true
            };

            Label lblLabel = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.LightGray,
                Location = new Point(20, 80),
                AutoSize = true
            };

            card.Controls.Add(lblIcon);
            card.Controls.Add(valueLabel);
            card.Controls.Add(lblLabel);

            return card;
        }

        private void IniciarAutoRefresh()
        {
            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 30000; // 30 segundos
            refreshTimer.Tick += async (s, e) => await CarregarDados();
            refreshTimer.Start();
        }

        private async Task CarregarDados()
        {
            await CarregarDashboard();
            await CarregarTickets();
            await CarregarUsuarios();
            await CarregarRelatorios();
        }

        private async Task CarregarDashboard()
        {
            try
            {
                dashboard = await _apiClient.ObterDashboardAsync();

                lblTotalUsuarios.Text = dashboard.TotalUsuarios.ToString();
                lblTicketsAbertos.Text = dashboard.TicketsAbertos.ToString();
                lblTicketsResolvidos.Text = dashboard.TicketsResolvidos.ToString();
                lblTotalChats.Text = dashboard.TotalChats.ToString();
                lblResolvidosHoje.Text = dashboard.TicketsResolvidosHoje.ToString();
                lblChatsResolvidos.Text = dashboard.ChatsResolvidos.ToString();

                // Atualizar top usuários
                var lstTopUsuarios = tabDashboard.Controls.Find("lstTopUsuarios", true).FirstOrDefault() as ListBox;
                if (lstTopUsuarios != null && dashboard.TopUsuarios != null)
                {
                    lstTopUsuarios.Items.Clear();
                    int posicao = 1;
                    foreach (var usuario in dashboard.TopUsuarios)
                    {
                        lstTopUsuarios.Items.Add($"{posicao}º - {usuario.Nome} - {usuario.TotalTickets} tickets");
                        posicao++;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar dashboard: {ex.Message}",
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task CarregarTickets()
        {
            try
            {
                tickets = await _apiClient.ObterTodosTicketsAsync();

                dgvTickets.DataSource = null;
                dgvTickets.DataSource = tickets.Select(t => new
                {
                    ID = t.Id,
                    Solicitante = t.NomeSolicitante,
                    Departamento = t.Departamento,
                    Descrição = t.DescricaoProblema,
                    Status = t.Status,
                    Abertura = t.DataAbertura.ToString("dd/MM/yyyy HH:mm")
                }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar tickets: {ex.Message}",
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task CarregarUsuarios()
        {
            try
            {
                usuarios = await _apiClient.ObterUsuariosAsync();

                dgvUsuarios.DataSource = null;
                dgvUsuarios.DataSource = usuarios.Select(u => new
                {
                    ID = u.Id,
                    Nome = u.Nome,
                    Email = u.Email,
                    Departamento = u.Departamento,
                    Tickets = u.TotalTickets,
                    Abertos = u.TicketsAbertos,
                    Chats = u.TotalChats
                }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar usuários: {ex.Message}",
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task CarregarRelatorios()
        {
            try
            {
                var relatorio = await _apiClient.ObterRelatorioDepartamentosAsync();

                dgvRelatorios.DataSource = null;
                dgvRelatorios.DataSource = relatorio.Select(r => new
                {
                    Departamento = r.Departamento,
                    Usuários = r.TotalUsuarios,
                    Total_Tickets = r.TotalTickets,
                    Abertos = r.TicketsAbertos,
                    Resolvidos = r.TicketsResolvidos
                }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar relatórios: {ex.Message}",
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnEditarUsuario_Click(object sender, EventArgs e)
        {
            if (dgvUsuarios.SelectedRows.Count == 0)
            {
                MessageBox.Show("Selecione um usuário para editar.",
                    "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int userId = (int)dgvUsuarios.SelectedRows[0].Cells[0].Value;
            var usuario = usuarios.FirstOrDefault(u => u.Id == userId);

            if (usuario == null) return;

            var form = new EditarUsuarioForm(usuario, _apiClient);
            if (form.ShowDialog() == DialogResult.OK)
            {
                await CarregarUsuarios();
            }
        }

        private async void BtnAlterarSenha_Click(object sender, EventArgs e)
        {
            if (dgvUsuarios.SelectedRows.Count == 0)
            {
                MessageBox.Show("Selecione um usuário para alterar a senha.",
                    "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int userId = (int)dgvUsuarios.SelectedRows[0].Cells[0].Value;
            string userName = dgvUsuarios.SelectedRows[0].Cells[1].Value.ToString();

            string novaSenha = Microsoft.VisualBasic.Interaction.InputBox(
                $"Digite a nova senha para {userName}:",
                "Alterar Senha",
                "");

            if (string.IsNullOrEmpty(novaSenha) || novaSenha.Length < 6)
            {
                MessageBox.Show("A senha deve ter no mínimo 6 caracteres.",
                    "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var sucesso = await _apiClient.AlterarSenhaUsuarioAsync(userId, novaSenha);
                if (sucesso)
                {
                    MessageBox.Show("Senha alterada com sucesso!",
                        "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao alterar senha: {ex.Message}",
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnAlterarCargo_Click(object sender, EventArgs e)
        {
            if (dgvUsuarios.SelectedRows.Count == 0)
            {
                MessageBox.Show("Selecione um usuário para alterar o cargo.",
                    "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int userId = (int)dgvUsuarios.SelectedRows[0].Cells[0].Value;
            string userName = dgvUsuarios.SelectedRows[0].Cells[1].Value.ToString();

            var form = new AlterarCargoForm(userId, userName, _apiClient);
            if (form.ShowDialog() == DialogResult.OK)
            {
                await CarregarUsuarios();
            }
        }

        private async void BtnExcluirUsuario_Click(object sender, EventArgs e)
        {
            if (dgvUsuarios.SelectedRows.Count == 0)
            {
                MessageBox.Show("Selecione um usuário para excluir.",
                    "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int userId = (int)dgvUsuarios.SelectedRows[0].Cells[0].Value;
            string userName = dgvUsuarios.SelectedRows[0].Cells[1].Value.ToString();

            if (MessageBox.Show($"Tem certeza que deseja excluir o usuário '{userName}'?\n\n" +
                               "ISTO IRÁ DELETAR:\n" +
                               "• Todos os chats\n" +
                               "• Todos os tickets\n" +
                               "• Todo o histórico\n\n" +
                               "Esta ação não pode ser desfeita!",
                               "Confirmar Exclusão",
                               MessageBoxButtons.YesNo,
                               MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                var sucesso = await _apiClient.ExcluirUsuarioAsync(userId);
                if (sucesso)
                {
                    MessageBox.Show("Usuário excluído com sucesso!",
                        "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await CarregarUsuarios();
                    await CarregarDashboard();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao excluir usuário: {ex.Message}",
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            refreshTimer?.Stop();
            refreshTimer?.Dispose();
            base.OnFormClosed(e);
        }
    }

    // Form auxiliar para editar usuário
    public class EditarUsuarioForm : Form
    {
        private readonly UsuarioDTO _usuario;
        private readonly ApiClient _apiClient;
        private TextBox txtNome;
        private TextBox txtEmail;
        private ComboBox cboDepartamento;

        public EditarUsuarioForm(UsuarioDTO usuario, ApiClient apiClient)
        {
            _usuario = usuario;
            _apiClient = apiClient;
            InitializeComponent();
        }

        private async void InitializeComponent()
        {
            this.Text = "Editar Usuário";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(30, 20, 51);

            Label lblNome = new Label
            {
                Text = "Nome:",
                ForeColor = Color.White,
                Location = new Point(20, 20),
                AutoSize = true
            };

            txtNome = new TextBox
            {
                Text = _usuario.Nome,
                Location = new Point(20, 45),
                Size = new Size(340, 25),
                BackColor = Color.FromArgb(44, 32, 77),
                ForeColor = Color.White
            };

            Label lblEmail = new Label
            {
                Text = "Email:",
                ForeColor = Color.White,
                Location = new Point(20, 80),
                AutoSize = true
            };

            txtEmail = new TextBox
            {
                Text = _usuario.Email,
                Location = new Point(20, 105),
                Size = new Size(340, 25),
                BackColor = Color.FromArgb(44, 32, 77),
                ForeColor = Color.White
            };

            Label lblDepartamento = new Label
            {
                Text = "Departamento:",
                ForeColor = Color.White,
                Location = new Point(20, 140),
                AutoSize = true
            };

            cboDepartamento = new ComboBox
            {
                Location = new Point(20, 165),
                Size = new Size(340, 25),
                BackColor = Color.FromArgb(44, 32, 77),
                ForeColor = Color.White,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Carregar departamentos
            var departamentos = await _apiClient.ObterDepartamentosAsync();
            foreach (var dept in departamentos)
            {
                cboDepartamento.Items.Add(dept.Nome);
                if (dept.Id == _usuario.IdDepartamento)
                    cboDepartamento.SelectedIndex = cboDepartamento.Items.Count - 1;
            }

            Button btnSalvar = new Button
            {
                Text = "Salvar",
                Location = new Point(100, 210),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnSalvar.Click += async (s, e) => await SalvarUsuario();

            Button btnCancelar = new Button
            {
                Text = "Cancelar",
                Location = new Point(200, 210),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };

            this.Controls.Add(lblNome);
            this.Controls.Add(txtNome);
            this.Controls.Add(lblEmail);
            this.Controls.Add(txtEmail);
            this.Controls.Add(lblDepartamento);
            this.Controls.Add(cboDepartamento);
            this.Controls.Add(btnSalvar);
            this.Controls.Add(btnCancelar);
        }

        private async Task SalvarUsuario()
        {
            try
            {
                var departamentos = await _apiClient.ObterDepartamentosAsync();
                var deptSelecionado = departamentos[cboDepartamento.SelectedIndex];

                var sucesso = await _apiClient.AtualizarUsuarioAsync(
                    _usuario.Id,
                    txtNome.Text,
                    txtEmail.Text,
                    deptSelecionado.Id
                );

                if (sucesso)
                {
                    MessageBox.Show("Usuário atualizado com sucesso!",
                        "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar: {ex.Message}",
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // Form auxiliar para alterar cargo
    public class AlterarCargoForm : Form
    {
        private readonly int _userId;
        private readonly string _userName;
        private readonly ApiClient _apiClient;
        private ComboBox cboCargo;

        public AlterarCargoForm(int userId, string userName, ApiClient apiClient)
        {
            _userId = userId;
            _userName = userName;
            _apiClient = apiClient;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Alterar Cargo";
            this.Size = new Size(400, 250);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(30, 20, 51);

            Label lblInfo = new Label
            {
                Text = $"Alterando cargo de: {_userName}",
                ForeColor = Color.White,
                Location = new Point(20, 20),
                AutoSize = true
            };

            Label lblCargo = new Label
            {
                Text = "Novo Cargo:",
                ForeColor = Color.White,
                Location = new Point(20, 60),
                AutoSize = true
            };

            cboCargo = new ComboBox
            {
                Location = new Point(20, 85),
                Size = new Size(340, 25),
                BackColor = Color.FromArgb(44, 32, 77),
                ForeColor = Color.White,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboCargo.Items.Add("Solicitante (Usuário Normal)");
            cboCargo.Items.Add("Técnico");
            cboCargo.Items.Add("Gerente (Administrador)");
            cboCargo.SelectedIndex = 0;

            Label lblAviso = new Label
            {
                Text = "?? Ao promover para Técnico/Gerente, o usuário terá acesso ao painel administrativo.",
                ForeColor = Color.FromArgb(251, 191, 36),
                Location = new Point(20, 120),
                Size = new Size(340, 40)
            };

            Button btnSalvar = new Button
            {
                Text = "Alterar",
                Location = new Point(100, 180),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(139, 92, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSalvar.Click += async (s, e) => await AlterarCargo();

            Button btnCancelar = new Button
            {
                Text = "Cancelar",
                Location = new Point(200, 180),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };

            this.Controls.Add(lblInfo);
            this.Controls.Add(lblCargo);
            this.Controls.Add(cboCargo);
            this.Controls.Add(lblAviso);
            this.Controls.Add(btnSalvar);
            this.Controls.Add(btnCancelar);
        }

        private async Task AlterarCargo()
        {
            string cargo = cboCargo.SelectedIndex switch
            {
                0 => "Solicitante",
                1 => "Tecnico",
                2 => "Gerente",
                _ => "Solicitante"
            };

            try
            {
                var sucesso = await _apiClient.AlterarCargoUsuarioAsync(_userId, cargo);
                if (sucesso)
                {
                    MessageBox.Show("Cargo alterado com sucesso!",
                        "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao alterar cargo: {ex.Message}",
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}