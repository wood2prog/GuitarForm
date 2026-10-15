using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GuitarForm.Data
{
    // What was recovered: the library and design it belongs to, when it was written, and the working copy as a design
    // document (the DesignDocument JSON, with the plug-in's new fields in its "newFields" object).
    public sealed record Recovery(string Name, DateTime SavedAtUtc, string Document, string LibraryPath = null);

    // One recovery file per design in a library, holding unsaved work so a crash doesn't lose it. Files are named by
    // the library's path and the design's id, so two libraries' designs never share one. It never touches the library.
    public sealed class RecoveryStore
    {
        readonly string _folder;

        public RecoveryStore(string folder) => _folder = folder;

        public static string DefaultFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GuitarForm", "recovery");

        static string Normalise(string libraryPath) => Path.GetFullPath(libraryPath).ToLowerInvariant();

        static string Prefix(string libraryPath) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Normalise(libraryPath))), 0, 6).ToLowerInvariant();

        string PathFor(string libraryPath, long id) =>
            Path.Combine(_folder, Prefix(libraryPath) + "-" + id.ToString(CultureInfo.InvariantCulture) + ".json");

        public bool Exists(string libraryPath, long id) => File.Exists(PathFor(libraryPath, id));

        // Written to a temporary file and moved over the old one, so an interrupted write leaves the last good file.
        public void Write(string libraryPath, long id, string name, string document)
        {
            Directory.CreateDirectory(_folder);
            string path = PathFor(libraryPath, id);
            string temp = path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(new Recovery(name, DateTime.UtcNow, document, Normalise(libraryPath))));
            File.Move(temp, path, true);
        }

        // Null when there's no file, it can't be read, or it was written for a different library.
        public Recovery TryRead(string libraryPath, long id)
        {
            try
            {
                string path = PathFor(libraryPath, id);
                if (!File.Exists(path))
                    return null;
                var recovery = JsonSerializer.Deserialize<Recovery>(File.ReadAllText(path));
                if (recovery?.Document == null || recovery.LibraryPath != Normalise(libraryPath))
                    return null;
                return recovery;
            }
            catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
            {
                return null;
            }
        }

        public void Delete(string libraryPath, long id)
        {
            try
            {
                File.Delete(PathFor(libraryPath, id));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
            }
        }

        // Deletes this library's recovery files for designs that no longer exist.
        public void DeleteStale(string libraryPath, IEnumerable<long> existingIds)
        {
            try
            {
                if (!Directory.Exists(_folder))
                    return;
                var keep = new HashSet<string>();
                foreach (long id in existingIds)
                    keep.Add(Path.GetFileName(PathFor(libraryPath, id)));
                foreach (string file in Directory.GetFiles(_folder, Prefix(libraryPath) + "-*.json"))
                    if (!keep.Contains(Path.GetFileName(file)))
                        File.Delete(file);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
            }
        }
    }
}
