using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DotIA.Desktop.Controls
{
    public class RoundedTextBox : Panel
    {
        private TextBox innerTextBox;
        private int borderRadius = 12;
        private Color borderColor = ColorTranslator.FromHtml("#3d2e6b");
        private Color focusBorderColor = ColorTranslator.FromHtml("#8d4bff");
        private Color bgColor = ColorTranslator.FromHtml("#2c204d");
        private bool isFocused = false;

        public int BorderRadius
        {
            get => borderRadius;
            set { borderRadius = value; Invalidate(); }
        }

        public Color BorderColorNormal
        {
            get => borderColor;
            set { borderColor = value; Invalidate(); }
        }

        public Color FocusBorderColor
        {
            get => focusBorderColor;
            set { focusBorderColor = value; Invalidate(); }
        }

        public Color BackgroundColor
        {
            get => bgColor;
            set { bgColor = value; innerTextBox.BackColor = value; Invalidate(); }
        }

        public override string Text
        {
            get => innerTextBox.Text;
            set => innerTextBox.Text = value ?? string.Empty;
        }

        public bool UseSystemPasswordChar
        {
            get => innerTextBox.UseSystemPasswordChar;
            set => innerTextBox.UseSystemPasswordChar = value;
        }

        public char PasswordChar
        {
            get => innerTextBox.PasswordChar;
            set => innerTextBox.PasswordChar = value;
        }

        public int MaxLength
        {
            get => innerTextBox.MaxLength;
            set => innerTextBox.MaxLength = value;
        }

        public bool Multiline
        {
            get => innerTextBox.Multiline;
            set
            {
                innerTextBox.Multiline = value;
                if (value)
                {
                    innerTextBox.ScrollBars = ScrollBars.Vertical;
                }
            }
        }

        public string PlaceholderText { get; set; } = "";

        public RoundedTextBox()
        {
            innerTextBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = bgColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11f),
                Location = new Point(15, 0),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            // Centraliza verticalmente
            innerTextBox.Location = new Point(15, (Height - innerTextBox.Height) / 2);

            innerTextBox.GotFocus += (s, e) => { isFocused = true; Invalidate(); };
            innerTextBox.LostFocus += (s, e) => { isFocused = false; Invalidate(); };
            innerTextBox.TextChanged += (s, e) => OnTextChanged(e);

            Controls.Add(innerTextBox);

            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            BackColor = Color.Transparent;
            Padding = new Padding(15, 10, 15, 10);
            MinimumSize = new Size(100, 45);
            Size = new Size(300, 45);

            // Recalcula posição ao redimensionar
            Resize += (s, e) =>
            {
                if (!innerTextBox.Multiline)
                {
                    innerTextBox.Location = new Point(15, (Height - innerTextBox.Height) / 2);
                }
                else
                {
                    innerTextBox.Location = new Point(15, 10);
                    innerTextBox.Size = new Size(Width - 30, Height - 20);
                }
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = GetRoundedRect(ClientRectangle, borderRadius))
            {
                // Fundo
                using (SolidBrush bgBrush = new SolidBrush(bgColor))
                {
                    e.Graphics.FillPath(bgBrush, path);
                }

                // Borda
                Color currentBorderColor = isFocused ? focusBorderColor : borderColor;
                int borderWidth = isFocused ? 2 : 1;

                using (Pen borderPen = new Pen(currentBorderColor, borderWidth))
                {
                    e.Graphics.DrawPath(borderPen, path);
                }

                // Glow effect quando focado
                if (isFocused)
                {
                    using (Pen glowPen = new Pen(Color.FromArgb(50, focusBorderColor), 3))
                    {
                        Rectangle glowRect = ClientRectangle;
                        glowRect.Inflate(-1, -1);
                        using (GraphicsPath glowPath = GetRoundedRect(glowRect, borderRadius))
                        {
                            e.Graphics.DrawPath(glowPen, glowPath);
                        }
                    }
                }
            }

            // Placeholder
            if (string.IsNullOrEmpty(innerTextBox.Text) && !isFocused && !string.IsNullOrEmpty(PlaceholderText))
            {
                TextRenderer.DrawText(e.Graphics, PlaceholderText,
                    new Font("Segoe UI", 11f), new Point(20, (Height - 20) / 2),
                    Color.FromArgb(156, 163, 175));
            }
        }

        private GraphicsPath GetRoundedRect(Rectangle rect, int radius)
        {
            rect.Inflate(-1, -1); // Ajuste para a borda
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            if (!innerTextBox.Multiline)
            {
                innerTextBox.Width = Width - 30;
            }
        }
    }
}
