using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace IMEKeyBinder
{
    internal static class IconHelper
    {
        public static Icon CreateAppIcon(bool isEnabled = true)
        {
            // 32x32 dynamic icon
            using var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                // Background rounded rect
                Color bgStart = isEnabled ? Color.FromArgb(41, 128, 185) : Color.FromArgb(127, 140, 141);
                Color bgEnd = isEnabled ? Color.FromArgb(109, 213, 250) : Color.FromArgb(189, 195, 199);

                using (var brush = new LinearGradientBrush(new Rectangle(0, 0, 32, 32), bgStart, bgEnd, 45f))
                {
                    using var path = CreateRoundedRectanglePath(new Rectangle(1, 1, 30, 30), 6);
                    g.FillPath(brush, path);
                }

                // Border
                Color borderColor = isEnabled ? Color.FromArgb(255, 255, 255, 200) : Color.FromArgb(200, 200, 200, 150);
                using (var pen = new Pen(borderColor, 1.5f))
                {
                    using var path = CreateRoundedRectanglePath(new Rectangle(1, 1, 30, 30), 6);
                    g.DrawPath(pen, path);
                }

                // Text "IME" or "あ/A"
                using var font = new Font("Segoe UI", 9f, FontStyle.Bold);
                using var textBrush = new SolidBrush(Color.White);

                var stringFormat = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                g.DrawString("IME", font, textBrush, new RectangleF(0, 1, 32, 32), stringFormat);
            }

            IntPtr hIcon = bmp.GetHicon();
            Icon icon = Icon.FromHandle(hIcon);
            return icon;
        }

        public static Icon CreateStatusIcon(bool isImeOn)
        {
            using var bmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                Color bg = isImeOn ? Color.FromArgb(46, 204, 113) : Color.FromArgb(52, 152, 219);
                using (var brush = new SolidBrush(bg))
                {
                    using var path = CreateRoundedRectanglePath(new Rectangle(0, 0, 16, 16), 3);
                    g.FillPath(brush, path);
                }

                using var font = new Font("MS Gothic", 8f, FontStyle.Bold);
                using var textBrush = new SolidBrush(Color.White);
                var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                string label = isImeOn ? "あ" : "A";
                g.DrawString(label, font, textBrush, new RectangleF(-1, 0, 18, 16), sf);
            }

            IntPtr hIcon = bmp.GetHicon();
            return Icon.FromHandle(hIcon);
        }

        private static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            var arc = new Rectangle(rect.Location, new Size(diameter, diameter));

            // top left
            path.AddArc(arc, 180, 90);
            // top right
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            // bottom right
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            // bottom left
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}
