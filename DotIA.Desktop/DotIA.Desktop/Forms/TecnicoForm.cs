using System;
using System.Drawing;
using System.Windows.Forms;
using DotIA.Desktop.Services;

namespace DotIA.Desktop.Forms
{
    public partial class TecnicoForm : Form
    {
        private readonly ApiClient _apiClient;
        private ListBox listTickets;
        private TextBox txtPergunta;
        private TextBox txtRespostaIA;
        private TextBox txtSolucao;
        private Button btnResolver;
        private TicketDTO ticketSelecionado;

        public TecnicoForm()
        {
            InitializeComponent();
            _apiClient = new ApiClient();
            CarregarTickets();
        }

        private void InitializeComponent()
        {
            this.Text = "DotIA - Painel do Técnico";
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(26, 19, 47);

            // Lista de tickets
            Label lblTickets = new Label
            {
                Text = "Tickets Pendentes:",
                Location = new Point(20, 20),
                Size = new Size(200, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            listTickets = new ListBox
            {
                Location = new Point(20, 45),
                Size = new Size(250, 450),
                Font = new Font("Segoe UI", 9)
            };
            listTickets.SelectedIndexChanged += ListTickets_SelectedIndexChanged;

            // Detalhes do ticket
            Label lblPergunta = new Label
            {
                Text = "Pergunta do Usuário:",
                Location = new Point(290, 20),
                Size = new Size(200, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            txtPergunta = new TextBox
            {
                Location = new Point(290, 45),
                Size = new Size(570, 100),
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.FromArgb(44, 32, 77),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10)
            };

            Label lblIA = new Label
            {
                Text = "Resposta da IA:",
                Location = new Point(290, 155),
                Size = new Size(200, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            txtRespostaIA = new TextBox
            {
                Location = new Point(290, 180),
                Size = new Size(570, 100),
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.FromArgb(44, 32, 77),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10)
            };

            Label lblSolucao = new Label
            {
                Text = "Sua Solução:",
                Location = new Point(290, 290),
                Size = new Size(200, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            txtSolucao = new TextBox
            {
                Location = new Point(290, 315),
                Size = new Size(570, 150),
                Multiline = true,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10)
            };

            btnResolver = new Button
            {
                Text = "Resolver Ticket",
                Location = new Point(290, 480),
                Size = new Size(200, 40),
                BackColor = Color.FromArgb(141, 75, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnResolver.Click += BtnResolver_Click;

            this.Controls.Add(lblTickets);
            this.Controls.Add(listTickets);
            this.Controls.Add(lblPergunta);
            this.Controls.Add(txtPergunta);
            this.Controls.Add(lblIA);
            this.Controls.Add(txtRespostaIA);
            this.Controls.Add(lblSolucao);
            this.Controls.Add(txtSolucao);
            this.Controls.Add(btnResolver);
        }

        private async void CarregarTickets()
        {
            listTickets.Items.Clear();
            var tickets = await _apiClient.ObterTicketsPendentesAsync();

            foreach (var ticket in tickets)
            {
                listTickets.Items.Add($"#{ticket.Id} - {ticket.NomeSolicitante}");
                listTickets.Tag = tickets; // Armazena lista completa
            }
        }

        private void ListTickets_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listTickets.SelectedIndex < 0) return;

            var tickets = (System.Collections.Generic.List<TicketDTO>)listTickets.Tag;
            ticketSelecionado = tickets[listTickets.SelectedIndex];

            txtPergunta.Text = ticketSelecionado.DescricaoProblema;
            txtRespostaIA.Text = ticketSelecionado.RespostaIA;
            txtSolucao.Clear();
        }

        private async void BtnResolver_Click(object sender, EventArgs e)
        {
            if (ticketSelecionado == null || string.IsNullOrEmpty(txtSolucao.Text))
            {
                MessageBox.Show("Selecione um ticket e escreva uma solução.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnResolver.Enabled = false;
            var sucesso = await _apiClient.ResolverTicketAsync(ticketSelecionado.Id, txtSolucao.Text);
            btnResolver.Enabled = true;

            if (sucesso)
            {
                MessageBox.Show("Ticket resolvido com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                CarregarTickets();
                txtPergunta.Clear();
                txtRespostaIA.Clear();
                txtSolucao.Clear();
            }
            else
            {
                MessageBox.Show("Erro ao resolver ticket.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}