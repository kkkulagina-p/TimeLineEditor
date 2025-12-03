using System;
using System.Drawing;

namespace TimelineEditor.Models
{
    /// <summary>
    /// Одно событие на таймлайне.
    /// </summary>
    public class EventItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Название события.</summary>
        public string Title { get; set; }

        /// <summary>Дата/время начала.</summary>
        public DateTime Start { get; set; }

        /// <summary>Дата/время окончания (опционально, можно равна Start).</summary>
        public DateTime End { get; set; }

        /// <summary>Описание события.</summary>
        public string Description { get; set; }

        /// <summary>Категория / тег (для фильтрации).</summary>
        public string Category { get; set; }

        /// <summary>Цветовая метка события.</summary>
        public Color Color { get; set; } = Color.SteelBlue;

        /// <summary>Номер ряда (по вертикали), куда событие размещено.</summary>
        public int RowIndex { get; set; }

        public override string ToString()
        {
            return $"{Start:g} - {Title}";
        }
    }
}
