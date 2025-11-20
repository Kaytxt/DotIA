using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DotIA.Desktop.Controls
{
    public class RoundedComboBox : ComboBox
    {
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

        public RoundedComboBox()
        {
            DrawMode = DrawMode.OwnerDrawFixed;
            DropDownStyle = ComboBoxStyle.DropDownList;
            FlatStyle = FlatStyle.Flat;
            BackColor = bgColor;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 11f);
            ItemHeight = 30;

            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            isFocused = true;
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            isFocused = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

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

            // Texto selecionado
            if (SelectedIndex >= 0)
            {
                Rectangle textRect = new Rectangle(15, 0, Width - 50, Height);
                TextRenderer.DrawText(e.Graphics, GetItemText(SelectedItem), Font,
                    textRect, ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }
            else if (Items.Count > 0)
            {
                // Placeholder
                Rectangle textRect = new Rectangle(15, 0, Width - 50, Height);
                TextRenderer.DrawText(e.Graphics, "Selecione...", Font,
                    textRect, Color.FromArgb(156, 163, 175), TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }

            // Seta dropdown
            DrawDropDownArrow(e.Graphics);
        }

        private void DrawDropDownArrow(Graphics g)
        {
            int arrowSize = 8;
            int arrowX = Width - 30;
            int arrowY = (Height - arrowSize) / 2;

            Point[] arrowPoints = new Point[]
            {
                new Point(arrowX, arrowY),
                new Point(arrowX + arrowSize, arrowY),
                new Point(arrowX + arrowSize / 2, arrowY + arrowSize / 2)
            };

            using (SolidBrush arrowBrush = new SolidBrush(Color.FromArgb(156, 163, 175)))
            {
                g.FillPolygon(arrowBrush, arrowPoints);
            }
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            e.DrawBackground();

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color textColor = isSelected ? Color.White : Color.FromArgb(229, 231, 235);
            Color itemBgColor = isSelected ? ColorTranslator.FromHtml("#8d4bff") : bgColor;

            using (SolidBrush bgBrush = new SolidBrush(itemBgColor))
            {
                e.Graphics.FillRectangle(bgBrush, e.Bounds);
            }

            TextRenderer.DrawText(e.Graphics, GetItemText(Items[e.Index]), e.Font,
                e.Bounds, textColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

            e.DrawFocusRectangle();
        }

        private GraphicsPath GetRoundedRect(Rectangle rect, int radius)
        {
            rect.Inflate(-1, -1);
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
