using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using TimelineEditor.Models;
using System.Drawing;

namespace TimelineEditor.Services
{
    public static class ProjectStorageService
    {
        // Небольшой конвертер для System.Drawing.Color
        private class ColorConverter : JsonConverter<Color>
        {
            public override Color Read(ref Utf8JsonReader reader, System.Type typeToConvert, JsonSerializerOptions options)
            {
                var argb = reader.GetInt32();
                return Color.FromArgb(argb);
            }

            public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value.ToArgb());
            }
        }

        private static JsonSerializerOptions CreateOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new ColorConverter() }
            };
        }

        public static void Save(TimelineProject project, string path)
        {
            var json = JsonSerializer.Serialize(project, CreateOptions());
            File.WriteAllText(path, json);
        }

        public static TimelineProject Load(string path)
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<TimelineProject>(json, CreateOptions())
                   ?? new TimelineProject();
        }
    }
}
