using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TimelineEditor.Models;
using TimelineEditor.Services;

namespace TimelineEditor.Controls
{
    /// <summary>
    /// Кастомный контрол, который рисует шкалу времени и события.
    /// Поддерживает прокрутку и масштабирование.
    /// </summary>
    public class TimelineControl : UserControl
    {
        private TimelineProject _project = new TimelineProject();
        private float _scrollOffset = 0; // пиксели сдвига по горизонтали

        /// <summary>Выбранное событие (при клике мышью).</summary>
        public EventItem SelectedEvent { get; set; }

        public event EventHandler SelectedEventChanged;

        public TimelineProject Project
        {
            get { return _project; }
            set
            {
                _project = value;
                Invalidate();
            }
        }

        private bool _isDragging;
        private Point _lastMouse;

        public TimelineControl()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            MouseWheel += TimelineControl_MouseWheel;
            MouseDown += TimelineControl_MouseDown;
            MouseMove += TimelineControl_MouseMove;
        }

        private void TimelineControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                _isDragging = true;
                _lastMouse = e.Location;
            }
            else if (e.Button == MouseButtons.Left)
            {
                HitTestEvent(e.Location);
            }
        }

        private void TimelineControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                int dx = e.X - _lastMouse.X;
                _scrollOffset += dx;
                _lastMouse = e.Location;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isDragging = false;
        }

        /// <summary>
        /// Масштабирование колесом мыши (увеличение/уменьшение периода просмотра).
        /// </summary>
        private void TimelineControl_MouseWheel(object sender, MouseEventArgs e)
        {
            if (Project == null) return;

            TimeSpan span = Project.ViewEnd - Project.ViewStart;
            double factor = e.Delta > 0 ? 0.8 : 1.25;

            TimeSpan newSpan = TimeSpan.FromTicks((long)(span.Ticks * factor));
            if (newSpan.TotalDays < 1) newSpan = TimeSpan.FromDays(1);

            DateTime center = Project.ViewStart + TimeSpan.FromTicks(span.Ticks / 2);
            Project.ViewStart = center - TimeSpan.FromTicks(newSpan.Ticks / 2);
            Project.ViewEnd = center + TimeSpan.FromTicks(newSpan.Ticks / 2);

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (Project == null) return;

            Graphics g = e.Graphics;
            g.Clear(Project.IsDarkTheme ? Color.FromArgb(30, 30, 30) : Color.White);

            TimelineLayoutService.ArrangeEvents(Project.Events);

            DrawAxis(g);
            DrawEvents(g);
        }

        private void DrawAxis(Graphics g)
        {
            int axisY = Height - 30;

            using (Pen axisPen = new Pen(Project.IsDarkTheme ? Color.White : Color.Black, 2))
            {
                g.DrawLine(axisPen, 20, axisY, Width - 20, axisY);

                long totalTicks = (Project.ViewEnd - Project.ViewStart).Ticks;
                if (totalTicks <= 0) return;

                using (Brush textBrush = new SolidBrush(Project.IsDarkTheme ? Color.White : Color.Black))
                {
                    Font font = Font;
                    int tickCount = 10;

                    for (int i = 0; i <= tickCount; i++)
                    {
                        float t = (float)i / tickCount;
                        float x = 20 + _scrollOffset + t * (Width - 40);

                        g.DrawLine(axisPen, x, axisY - 5, x, axisY + 5);

                        DateTime date = Project.ViewStart + TimeSpan.FromTicks((long)(totalTicks * t));

                        string label;
                        switch (Project.Scale)
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
                        g.DrawString(label, font, textBrush, x - size.Width / 2, axisY + 8);
                    }
                }
            }
        }

        private void DrawEvents(Graphics g)
        {
            if (Project == null || !Project.Events.Any()) return;

            DateTime min = Project.ViewStart;
            DateTime max = Project.ViewEnd;
            long totalTicks = (max - min).Ticks;
            if (totalTicks <= 0) totalTicks = TimeSpan.FromDays(1).Ticks;

            int rowHeight = 30;
            int topOffset = 20;

            foreach (EventItem ev in Project.Events)
            {
                double startRatio = (double)(ev.Start - min).Ticks / totalTicks;
                double endRatio = (double)(ev.End - min).Ticks / totalTicks;

                float x = 20 + _scrollOffset + (float)startRatio * (Width - 40);
                float x2 = 20 + _scrollOffset + (float)endRatio * (Width - 40);
                if (x2 < x + 10) x2 = x + 10;

                float y = topOffset + ev.RowIndex * (rowHeight + 8);
                float w = x2 - x;
                float h = rowHeight;

                RectangleF rect = new RectangleF(x, y, w, h);

                Color back = ev == SelectedEvent ? ControlPaint.Light(ev.Color) : ev.Color;

                using (Brush brush = new SolidBrush(back))
                using (Pen pen = new Pen(Color.Black))
                using (Brush textBrush = new SolidBrush(Color.Black))
                {
                    g.FillRectangle(brush, rect);
                    g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);

                    string text = ev.Title;
                    SizeF size = g.MeasureString(text, Font);
                    g.DrawString(text, Font, textBrush, rect.X + 4, rect.Y + (h - size.Height) / 2);
                }
            }
        }

        /// <summary>
        /// Определяем, по какому событию кликнули.
        /// </summary>
        private void HitTestEvent(Point location)
        {
            if (Project == null || !Project.Events.Any()) return;

            DateTime min = Project.ViewStart;
            DateTime max = Project.ViewEnd;
            long totalTicks = (max - min).Ticks;
            if (totalTicks <= 0) totalTicks = TimeSpan.FromDays(1).Ticks;

            int rowHeight = 30;
            int topOffset = 20;

            foreach (EventItem ev in Project.Events)
            {
                double startRatio = (double)(ev.Start - min).Ticks / totalTicks;
                double endRatio = (double)(ev.End - min).Ticks / totalTicks;

                float x = 20 + _scrollOffset + (float)startRatio * (Width - 40);
                float x2 = 20 + _scrollOffset + (float)endRatio * (Width - 40);
                if (x2 < x + 10) x2 = x + 10;

                float y = topOffset + ev.RowIndex * (rowHeight + 8);
                float w = x2 - x;
                float h = rowHeight;

                RectangleF rect = new RectangleF(x, y, w, h);

                if (rect.Contains(location))
                {
                    SelectedEvent = ev;
                    OnSelectedEventChanged();
                    Invalidate();
                    return;
                }
            }

            SelectedEvent = null;
            OnSelectedEventChanged();
            Invalidate();
        }

        private void OnSelectedEventChanged()
        {
            EventHandler handler = SelectedEventChanged;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }
    }
}
