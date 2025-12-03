using System;
using System.Collections.Generic;

namespace TimelineEditor.Models
{
    public class TimelineProject
    {
        public string Name { get; set; } = "Новый таймлайн";

        public List<EventItem> Events { get; set; } = new List<EventItem>();

        public TimeScale Scale { get; set; } = TimeScale.Days;

        public DateTime ViewStart { get; set; } = DateTime.Today.AddDays(-7);

        public DateTime ViewEnd { get; set; } = DateTime.Today.AddDays(30);

        public bool IsDarkTheme { get; set; }
    }
}
