using System;
using System.Collections.Generic;
using System.Linq;
using TimelineEditor.Models;

namespace TimelineEditor.Services
{
    /// <summary>
    /// Расчёт рядов для событий, чтобы визуально не пересекались.
    /// </summary>
    public static class TimelineLayoutService
    {
        /// <summary>
        /// Назначает каждому событию RowIndex так, чтобы пересекающиеся события
        /// попадали в разные ряды.
        /// </summary>
        public static void ArrangeEvents(IList<EventItem> events)
        {
            // Сортируем по времени начала
            var ordered = events.OrderBy(e => e.Start).ToList();

            // Список "рядов", в каждом храним последнее событие в этом ряду
            var rows = new List<EventItem>();

            foreach (var ev in ordered)
            {
                // Ищем первый ряд, где это событие не пересекается с последним
                int rowIndex = -1;

                for (int i = 0; i < rows.Count; i++)
                {
                    var last = rows[i];

                    // Пересечение интервалов: если конец последнего <= начало нового,
                    // то можем положить в этот ряд.
                    if (last.End <= ev.Start)
                    {
                        rowIndex = i;
                        rows[i] = ev; // обновляем "последнее" событие в ряду
                        break;
                    }
                }

                // Если ряд не нашли — создаём новый
                if (rowIndex == -1)
                {
                    rows.Add(ev);
                    rowIndex = rows.Count - 1;
                }

                ev.RowIndex = rowIndex;
            }
        }
    }
}
