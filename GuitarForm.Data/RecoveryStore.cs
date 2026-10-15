using System;
using System.IO;
using System.Text.Json;

namespace GuitarForm.Data
{
    // What was recovered: the design's name, when it was written, and the working copy as a design document
    // (the DesignDocument JSON, with the plug-in's new fields in its "newFields" object).
    public sealed record Recovery(string Name, DateTime SavedAtUtc, string Document);

    // One recovery file per design, holding unsaved work so a crash doesn't lose it. It never touches the library.
    public sealed class RecoveryStore
    {
        readonly string _folder;

        public RecoveryStore(string folder) => _folder = folder;

        public static string DefaultFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GuitarForm", "recovery");

        string PathFor(long id) => Path.Combine(_folder, id.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".json");

        public bool Exists(long id) => File.Exists(PathFor(id));

        // Written to a temporary file and moved over the old one, so an interrupted write leaves the last good file.
        public void Write(long id, string name, string document)
        {
            Directory.CreateDirectory(_folder);
            string path = PathFor(id);
            string temp = path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(new Recovery(name, DateTime.UtcNow, document)));
            File.Move(temp, path, true);
        }

        // Null when there's no file or it can't be read.
        public Recovery TryRead(long id)
        {
            try
            {
                string path = PathFor(id);
                if (!File.Exists(path))
                    return null;
                var recovery = JsonSerializer.Deserialize<Recovery>(File.ReadAllText(path));
                return recovery?.Document == null ? null : recovery;
            }
            catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
            {
                return null;
            }
        }

        public void Delete(long id)
        {
            try
            {
                File.Delete(PathFor(id));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
            }
        }
    }
}
