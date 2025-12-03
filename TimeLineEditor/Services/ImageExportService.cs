using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using TimelineEditor.Models;

namespace TimelineEditor.Services
{
    public static class ImageExportService
    {
        /// <summary>
        /// Экспорт таймлайна в изображение (PNG/JPEG).
        /// </summary>
        public static void ExportToImage(
            TimelineProject project,
            string path,
            int width,
            int height,
            ImageFormat format)
        {
            if (project == null)
                throw new ArgumentNullException("project");
            if (format == null)
                format = ImageFormat.Png;

            using (Bitmap bmp = new Bitmap(width, height))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(project.IsDarkTheme ? Color.FromArgb(30, 30, 30) : Color.White);

                    // Раскладываем события по рядам
                    TimelineLayoutService.ArrangeEvents(project.Events);

                    DrawAxis(g, project, width, height);
                    DrawEvents(g, project, width, height);
                }

                bmp.Save(path, format);
            }
        }

        private static void DrawAxis(Graphics g, TimelineProject project, int width, int height)
        {
            int axisY = height - 30;

            using (Pen axisPen = new Pen(project.IsDarkTheme ? Color.White : Color.Black, 2))
            {
                g.DrawLine(axisPen, 20, axisY, width - 20, axisY);

                long totalTicks = (project.ViewEnd - project.ViewStart).Ticks;
                if (totalTicks <= 0) return;

                using (Brush textBrush = new SolidBrush(project.IsDarkTheme ? Color.White : Color.Black))
                {
                    Font font = SystemFonts.DefaultFont;
                    int tickCount = 10;

                    for (int i = 0; i <= tickCount; i++)
                    {
                        float t = (float)i / tickCount;
                        float x = 20 + t * (width - 40);

                        g.DrawLine(axisPen, x, axisY - 5, x, axisY + 5);

                        DateTime date = project.ViewStart + TimeSpan.FromTicks((long)(totalTicks * t));

                        string label;
                        switch (project.Scale)
                        {
                            case TimeScale.Years:
                                label = date.ToString("yyyy");
                                break;
                            case TimeScale.Months:
                                label = date.ToString("MM.yyyy");
                                break;
                            case TimeScale.Weeks:
                                label = date.ToString("dd.MM");
                                break;
                            default:
                                label = date.ToString("dd.MM HH:mm");
                                break;
                        }

                        SizeF size = g.MeasureString(label, font);
                        g.DrawString(label, font, textBrush,
                            x - size.Width / 2, axisY + 8);
                    }
                }
            }
        }

        private static void DrawEvents(Graphics g, TimelineProject project, int width, int height)
        {
            if (project == null || !project.Events.Any()) return;

            DateTime min = project.ViewStart;
            DateTime max = project.ViewEnd;
            long totalTicks = (max - min).Ticks;
            if (totalTicks <= 0) totalTicks = TimeSpan.FromDays(1).Ticks;

            int rowHeight = 30;
            int topOffset = 20;

            foreach (EventItem ev in project.Events)
            {
                double startRatio = (double)(ev.Start - min).Ticks / totalTicks;
                double endRatio = (double)(ev.End - min).Ticks / totalTicks;

                float x = 20 + (float)startRatio * (width - 40);
                float x2 = 20 + (float)endRatio * (width - 40);
                if (x2 < x + 10) x2 = x + 10;

                float y = topOffset + ev.RowIndex * (rowHeight + 8);
                float w = x2 - x;
                float h = rowHeight;

                RectangleF rect = new RectangleF(x, y, w, h);

                using (Brush brush = new SolidBrush(ev.Color))
                using (Pen pen = new Pen(Color.Black))
                using (Brush textBrush = new SolidBrush(Color.Black))
                {
                    g.FillRectangle(brush, rect);
                    g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);

                    string text = ev.Title;
                    SizeF size = g.MeasureString(text, SystemFonts.DefaultFont);
                    g.DrawString(text, SystemFonts.DefaultFont, textBrush,
                        rect.X + 4, rect.Y + (h - size.Height) / 2);
                }
            }
        }
    }
}
