using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DotIA.Desktop.Controls
{
    public class RoundedButton : Button
    {
        private int borderRadius = 20;
        private Color gradientStart = ColorTranslator.FromHtml("#8d4bff");
        private Color gradientEnd = ColorTranslator.FromHtml("#a855f7");
        private Color hoverGradientStart = ColorTranslator.FromHtml("#a855f7");
        private Color hoverGradientEnd = ColorTranslator.FromHtml("#c084fc");
        private bool isHovered = false;

        public int BorderRadius
        {
            get => borderRadius;
            set { borderRadius = value; Invalidate(); }
        }

        public Color GradientStart
        {
            get => gradientStart;
            set { gradientStart = value; Invalidate(); }
        }

        public Color GradientEnd
        {
            get => gradientEnd;
            set { gradientEnd = value; Invalidate(); }
        }

        public RoundedButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            FlatAppearance.MouseOverBackColor = Color.Transparent;
            FlatAppearance.MouseDownBackColor = Color.Transparent;
            BackColor = Color.Transparent;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            isHovered = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            using (GraphicsPath path = GetRoundedRect(ClientRectangle, borderRadius))
            {
                // Gradiente
                Color start = isHovered ? hoverGradientStart : gradientStart;
                Color end = isHovered ? hoverGradientEnd : gradientEnd;

                using (LinearGradientBrush brush = new LinearGradientBrush(
                    ClientRectangle, start, end, 45f))
                {
                    e.Graphics.FillPath(brush, path);
                }

                // Glow effect
                if (isHovered)
                {
                    using (Pen glowPen = new Pen(Color.FromArgb(80, gradientStart), 2))
                    {
                        e.Graphics.DrawPath(glowPen, path);
                    }
                }
            }

            // Texto
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle,
                ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private GraphicsPath GetRoundedRect(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}
