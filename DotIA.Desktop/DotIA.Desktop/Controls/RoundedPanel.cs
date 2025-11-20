using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DotIA.Desktop.Controls
{
    public class RoundedPanel : Panel
    {
        private int borderRadius = 15;
        private Color borderColor = ColorTranslator.FromHtml("#3d2e6b");
        private Color bgColor = ColorTranslator.FromHtml("#2c204d");
        private int borderWidth = 1;
        private bool drawBorder = true;
        private bool drawShadow = false;

        public int BorderRadius
        {
            get => borderRadius;
            set { borderRadius = value; Invalidate(); }
        }

        public Color BorderColor
        {
            get => borderColor;
            set { borderColor = value; Invalidate(); }
        }

        public Color BackgroundColor
        {
            get => bgColor;
            set { bgColor = value; Invalidate(); }
        }

        public int BorderWidth
        {
            get => borderWidth;
            set { borderWidth = value; Invalidate(); }
        }

        public bool DrawBorder
        {
            get => drawBorder;
            set { drawBorder = value; Invalidate(); }
        }

        public bool DrawShadow
        {
            get => drawShadow;
            set { drawShadow = value; Invalidate(); }
        }

        public RoundedPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.Transparent;
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // Shadow
            if (drawShadow)
            {
                Rectangle shadowRect = new Rectangle(4, 4, Width - 4, Height - 4);
                using (GraphicsPath shadowPath = GetRoundedRect(shadowRect, borderRadius))
                {
                    using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
                    {
                        e.Graphics.FillPath(shadowBrush, shadowPath);
                    }
                }
            }

            // Background
            Rectangle mainRect = drawShadow ? new Rectangle(0, 0, Width - 4, Height - 4) : ClientRectangle;
            using (GraphicsPath path = GetRoundedRect(mainRect, borderRadius))
            {
                using (SolidBrush bgBrush = new SolidBrush(bgColor))
                {
                    e.Graphics.FillPath(bgBrush, path);
                }

                // Border
                if (drawBorder)
                {
                    using (Pen borderPen = new Pen(borderColor, borderWidth))
                    {
                        e.Graphics.DrawPath(borderPen, path);
                    }
                }
            }
        }

        private GraphicsPath GetRoundedRect(Rectangle rect, int radius)
        {
            if (drawBorder)
            {
                rect.Inflate(-borderWidth / 2, -borderWidth / 2);
            }

            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;

            if (diameter > rect.Width) diameter = rect.Width;
            if (diameter > rect.Height) diameter = rect.Height;

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
            Invalidate();
        }
    }
}
