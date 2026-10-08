using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace TrayAlarm
{
    public static class IconHelper
    {
        public static Icon GetAppIcon()
        {
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (File.Exists(iconPath))
                {
                    return new Icon(iconPath);
                }
            }
            catch { }

            return CreateAlarmClockIcon();
        }

        public static Icon CreateAlarmClockIcon()
        {
            using (var bmp = new Bitmap(32, 32))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(Color.Transparent);

                    // Clock outer ring
                    using (var bodyBrush = new SolidBrush(Color.FromArgb(220, 38, 38)))
                    {
                        g.FillEllipse(bodyBrush, 4, 7, 24, 24);
                    }
                    using (var bodyPen = new Pen(Color.FromArgb(185, 28, 28), 2))
                    {
                        g.DrawEllipse(bodyPen, 4, 7, 24, 24);
                    }

                    // Alarm bells on top
                    using (var bellBrush = new SolidBrush(Color.FromArgb(245, 158, 11)))
                    {
                        g.FillEllipse(bellBrush, 3, 3, 8, 8);
                        g.FillEllipse(bellBrush, 21, 3, 8, 8);
                    }

                    // Inner face
                    using (var faceBrush = new SolidBrush(Color.White))
                    {
                        g.FillEllipse(faceBrush, 7, 10, 18, 18);
                    }

                    // Clock hands
                    using (var handPen = new Pen(Color.FromArgb(30, 41, 59), 2))
                    {
                        handPen.StartCap = LineCap.Round;
                        handPen.EndCap = LineCap.Round;
                        g.DrawLine(handPen, 16, 19, 16, 13);
                        g.DrawLine(handPen, 16, 19, 21, 19);
                    }

                    // Center pin
                    using (var pinBrush = new SolidBrush(Color.FromArgb(220, 38, 38)))
                    {
                        g.FillEllipse(pinBrush, 14.5f, 17.5f, 3, 3);
                    }
                }

                IntPtr hIcon = bmp.GetHicon();
                return Icon.FromHandle(hIcon);
            }
        }
    }
}
