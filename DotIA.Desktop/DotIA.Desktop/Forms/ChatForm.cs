using System;
using System.Drawing;
using System.Windows.Forms;
using DotIA.Desktop.Services;

namespace DotIA.Desktop.Forms
{
    public partial class ChatForm : Form
    {
        private readonly ApiClient _apiClient;
        private readonly int _usuarioId;
        private readonly string _nomeUsuario;
        private TextBox txtPergunta;
        private Button btnEnviar;
        private FlowLayoutPanel panelMensagens;
        private string ultimaPergunta = "";
        private string ultimaResposta = "";

        public ChatForm(int usuarioId, string nomeUsuario)
        {
            InitializeComponent();
            _apiClient = new ApiClient();
            _usuarioId = usuarioId;
            _nomeUsuario = nomeUsuario;
            CarregarHistorico();
        }

        private void InitializeComponent()
        {
            this.Text = $"DotIA - Chat";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(26, 19, 47);

            // Painel de mensagens
            panelMensagens = new FlowLayoutPanel
            {
                Location = new Point(10, 10),
                Size = new Size(760, 480),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.FromArgb(30, 20, 51)
            };

            // TextBox para pergunta
            txtPergunta = new TextBox
            {
                Location = new Point(10, 500),
                Size = new Size(650, 50),
                Multiline = true,
                Font = new Font("Segoe UI", 10)
            };

            // Botão enviar
            btnEnviar = new Button
            {
                Text = "Enviar",
                Location = new Point(670, 500),
                Size = new Size(100, 50),
                BackColor = Color.FromArgb(141, 75, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnEnviar.Click += BtnEnviar_Click;

            this.Controls.Add(panelMensagens);
            this.Controls.Add(txtPergunta);
            this.Controls.Add(btnEnviar);
        }

        private async void BtnEnviar_Click(object sender, EventArgs e)
        {
            string pergunta = txtPergunta.Text.Trim();
            if (string.IsNullOrEmpty(pergunta)) return;

            ultimaPergunta = pergunta;
            AdicionarMensagem(pergunta, true); // true = usuário
            txtPergunta.Clear();

            btnEnviar.Enabled = false;
            btnEnviar.Text = "...";

            var resposta = await _apiClient.EnviarPerguntaAsync(_usuarioId, pergunta);

            btnEnviar.Enabled = true;
            btnEnviar.Text = "Enviar";

            if (resposta.Sucesso)
            {
                ultimaResposta = resposta.Resposta;
                AdicionarMensagem(resposta.Resposta, false); // false = IA
                AdicionarBotoesFeedback();
            }
            else
            {
                MessageBox.Show($"Erro: {resposta.Resposta}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AdicionarMensagem(string texto, bool isUsuario)
        {
            Panel mensagemPanel = new Panel
            {
                Width = 700,
                AutoSize = true,
                Padding = new Padding(10),
                Margin = new Padding(5)
            };

            Label lblMensagem = new Label
            {
                Text = texto,
                AutoSize = true,
                MaximumSize = new Size(600, 0),
                BackColor = isUsuario ? Color.FromArgb(162, 55, 240) : Color.FromArgb(70, 60, 100),
                ForeColor = Color.White,
                Padding = new Padding(15),
                Font = new Font("Segoe UI", 10)
            };

            mensagemPanel.Controls.Add(lblMensagem);
            panelMensagens.Controls.Add(mensagemPanel);
            panelMensagens.ScrollControlIntoView(mensagemPanel);
        }

        private void AdicionarBotoesFeedback()
        {
            Panel feedbackPanel = new Panel
            {
                Width = 700,
                Height = 50,
                Margin = new Padding(5)
            };

            Button btnUtil = new Button
            {
                Text = "👍 Foi útil",
                Location = new Point(10, 10),
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnUtil.Click += async (s, e) =>
            {
                btnUtil.Enabled = false;
                await _apiClient.AvaliarRespostaAsync(_usuarioId, ultimaPergunta, ultimaResposta, true);
                MessageBox.Show("Obrigado pelo feedback!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            Button btnNaoUtil = new Button
            {
                Text = "👎 Não foi útil",
                Location = new Point(140, 10),
                Size = new Size(130, 30),
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnNaoUtil.Click += async (s, e) =>
            {
                btnNaoUtil.Enabled = false;
                await _apiClient.AvaliarRespostaAsync(_usuarioId, ultimaPergunta, ultimaResposta, false);
                MessageBox.Show("Sua solicitação foi encaminhada para um técnico.", "Ticket Criado", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            feedbackPanel.Controls.Add(btnUtil);
            feedbackPanel.Controls.Add(btnNaoUtil);
            panelMensagens.Controls.Add(feedbackPanel);
        }

        private async void CarregarHistorico()
        {
            var historico = await _apiClient.ObterHistoricoAsync(_usuarioId);
            foreach (var chat in historico)
            {
                AdicionarMensagem(chat.Pergunta, true);
                AdicionarMensagem(chat.Resposta, false);
            }
        }
    }
}