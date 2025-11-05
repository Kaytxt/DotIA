using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DotIA_Login
{
    public partial class LoginForm : Form
    {
        TextBox txtEmail;
        TextBox txtSenha;
        Button btnEntrar;
        Button btnCriarConta;
        CheckBox chkLembrar;

        public LoginForm()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
        }

        private void InitializeComponent()
        {
            this.Text = "DotIA - Login";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(920, 540);
            this.FormBorderStyle = FormBorderStyle.None;

            // Fundo com gradient
            this.Paint += (s, e) =>
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    this.ClientRectangle,
                    ColorTranslator.FromHtml("#0a051f"),
                    ColorTranslator.FromHtml("#1a1035"),
                    LinearGradientMode.Vertical))
                {
                    e.Graphics.FillRectangle(brush, this.ClientRectangle);
                }
            };

            // Card central
            Panel card = new Panel
            {
                Size = new Size(350, 480),
                Location = new Point((this.Width - 350) / 2, (this.Height - 480) / 2),
                BackColor = ColorTranslator.FromHtml("#1a0f33")
            };
            RoundControl(card, 22);
            this.Controls.Add(card);

            // Logo
            PictureBox logo = new PictureBox
            {
                Size = new Size(110, 110),
                Location = new Point((card.Width - 110) / 2, 25),
                Image = Properties.Resources.dotia_logo, // você coloca sua logo aqui
                SizeMode = PictureBoxSizeMode.Zoom
            };
            card.Controls.Add(logo);

            // Título
            Label lblTitulo = BuildLabel("DotIA", new Point((card.Width - 60) / 2, 145), 18, FontStyle.Bold);
            card.Controls.Add(lblTitulo);

            Label lblSub = BuildLabel("Sistema de Suporte Inteligente", new Point((card.Width - 200) / 2, 172), 9, FontStyle.Regular);
            card.Controls.Add(lblSub);

            // Email
            Label lblEmail = BuildLabel("Email", new Point(30, 210), 9, FontStyle.Regular);
            card.Controls.Add(lblEmail);

            txtEmail = BuildTextBox(new Point(30, 232));
            card.Controls.Add(txtEmail);

            // Senha
            Label lblSenha = BuildLabel("Senha", new Point(30, 290), 9, FontStyle.Regular);
            card.Controls.Add(lblSenha);

            txtSenha = BuildTextBox(new Point(30, 312));
            txtSenha.PasswordChar = '●';
            card.Controls.Add(txtSenha);

            // Checkbox lembrar
            chkLembrar = new CheckBox
            {
                Text = "Lembrar-me",
                Location = new Point(30, 350),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            card.Controls.Add(chkLembrar);

            // Botão Entrar
            btnEntrar = BuildButton("ENTRAR →", new Point(30, 380));
            card.Controls.Add(btnEntrar);

            // Ou criar conta
            Label ou = BuildLabel("ou", new Point((card.Width - 20) / 2, 420), 9, FontStyle.Regular);
            card.Controls.Add(ou);

            btnCriarConta = BuildButton("CRIAR NOVA CONTA", new Point(30, 445));
            btnCriarConta.BackColor = ColorTranslator.FromHtml("#24164a");
            card.Controls.Add(btnCriarConta);
        }

        // Estilização TextBox
        private TextBox BuildTextBox(Point pos)
        {
            TextBox tb = new TextBox
            {
                Location = pos,
                Width = 290,
                BorderStyle = BorderStyle.None,
                BackColor = ColorTranslator.FromHtml("#0a051f"),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Padding = new Padding(10, 8, 10, 8)
            };
            RoundControl(tb, 12);
            return tb;
        }

        // Estilização Label
        private Label BuildLabel(string text, Point pos, int size = 10, FontStyle style = FontStyle.Regular)
        {
            return new Label
            {
                Text = text,
                Location = pos,
                ForeColor = Color.White,
                AutoSize = true,
                Font = new Font("Segoe UI", size, style)
            };
        }

        // Estilização Botão
        private Button BuildButton(string text, Point pos)
        {
            Button btn = new Button
            {
                Text = text,
                Location = pos,
                Width = 290,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = ColorTranslator.FromHtml("#8e44ff");
            RoundControl(btn, 10);
            return btn;
        }

        // Borda arredondada
        private void RoundControl(Control control, int radius)
        {
            Rectangle r = new Rectangle(0, 0, control.Width, control.Height);
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            control.Region = new Region(path);
        }
    }
}
