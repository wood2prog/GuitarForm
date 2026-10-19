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
    public sealed record Recovery(string Name, DateTime SavedAtUtc, string Document, string LibraryPath = null,
        long? FretwireId = null, long? StringSetId = null);

    // How a recovery file read went: Unavailable is a transient failure (locked, no access), so the file may be fine;
    // Corrupt is a file that can't be used (not JSON, no document, or for another library).
    public enum RecoveryReadStatus { Missing, Ok, Corrupt, Unavailable }

    // One recovery file per design in a library, holding unsaved work so a crash doesn't lose it. Files are named by
    // the library's path and the design's id, so two libraries' designs never share one. It never touches the library.
    public sealed class RecoveryStore
    {
        readonly string _folder;

        public RecoveryStore(string folder) => _folder = folder;

        public static string DefaultFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GuitarForm", "recovery");

        static string Normalize(string libraryPath) => Path.GetFullPath(libraryPath).ToLowerInvariant();

        static string Prefix(string libraryPath) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Normalize(libraryPath))), 0, 6).ToLowerInvariant();

        string PathFor(string libraryPath, long id) =>
            Path.Combine(_folder, Prefix(libraryPath) + "-" + id.ToString(CultureInfo.InvariantCulture) + ".json");

        // String keys (such as "guitar-<uid>") name the file directly, so they're limited to letters, digits, '-' and '_'.
        // An all-digit key is refused: it would be indistinguishable from a legacy numeric design id.
        static void CheckKey(string key)
        {
            if (string.IsNullOrEmpty(key) || IsAllDigits(key))
                throw new ArgumentException("Recovery key must be non-empty and not all digits.", nameof(key));
            foreach (char c in key)
                if (!(c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-' or '_'))
                    throw new ArgumentException("Recovery key may contain only letters, digits, '-' and '_'.", nameof(key));
        }

        static bool IsAllDigits(string text)
        {
            foreach (char c in text)
                if (c is < '0' or > '9')
                    return false;
            return text.Length > 0;
        }

        string PathFor(string libraryPath, string key)
        {
            CheckKey(key);
            return Path.Combine(_folder, Prefix(libraryPath) + "-" + key + ".json");
        }

        public bool Exists(string libraryPath, string key) => File.Exists(PathFor(libraryPath, key));

        public void Write(string libraryPath, string key, string name, string document, long? fretwireId = null, long? stringSetId = null) =>
            WriteTo(PathFor(libraryPath, key), libraryPath, name, document, fretwireId, stringSetId);

        public Recovery TryRead(string libraryPath, string key) => ReadFrom(PathFor(libraryPath, key), libraryPath);

        // Like TryRead, but says why there's nothing: only Corrupt means the file is no use and may be deleted.
        public RecoveryReadStatus TryRead(string libraryPath, string key, out Recovery recovery) =>
            ReadStatus(PathFor(libraryPath, key), libraryPath, out recovery);

        public void Delete(string libraryPath, string key) => DeleteFile(PathFor(libraryPath, key));

        // The old numeric-key files, until they're converted.
        public Recovery TryReadLegacy(string libraryPath, long designId) => TryRead(libraryPath, designId);

        public RecoveryReadStatus TryReadLegacy(string libraryPath, long designId, out Recovery recovery) =>
            ReadStatus(PathFor(libraryPath, designId), libraryPath, out recovery);

        public void DeleteLegacy(string libraryPath, long designId) => Delete(libraryPath, designId);

        // Deletes this library's string-keyed recovery files whose key isn't given. Legacy numeric files are left alone.
        public void DeleteStale(string libraryPath, IEnumerable<string> keys)
        {
            try
            {
                if (!Directory.Exists(_folder))
                    return;
                var keep = new HashSet<string>();
                foreach (string key in keys)
                    keep.Add(Path.GetFileName(PathFor(libraryPath, key)));
                string prefix = Prefix(libraryPath) + "-";
                foreach (string file in Directory.GetFiles(_folder, prefix + "*.json"))
                {
                    string name = Path.GetFileName(file);
                    string key = name.Substring(prefix.Length, name.Length - prefix.Length - ".json".Length);
                    if (!IsAllDigits(key) && !keep.Contains(name))
                        File.Delete(file);
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
            }
        }

        public bool Exists(string libraryPath, long id) => File.Exists(PathFor(libraryPath, id));

        // Written to a temporary file and moved over the old one, so an interrupted write leaves the last good file.
        public void Write(string libraryPath, long id, string name, string document) =>
            WriteTo(PathFor(libraryPath, id), libraryPath, name, document, null, null);

        void WriteTo(string path, string libraryPath, string name, string document, long? fretwireId, long? stringSetId)
        {
            Directory.CreateDirectory(_folder);
            string temp = path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(
                new Recovery(name, DateTime.UtcNow, document, Normalize(libraryPath), fretwireId, stringSetId)));
            File.Move(temp, path, true);
        }

        // Null when there's no file, it can't be read, or it was written for a different library.
        public Recovery TryRead(string libraryPath, long id) => ReadFrom(PathFor(libraryPath, id), libraryPath);

        Recovery ReadFrom(string path, string libraryPath)
        {
            ReadStatus(path, libraryPath, out var recovery);
            return recovery;
        }

        RecoveryReadStatus ReadStatus(string path, string libraryPath, out Recovery recovery)
        {
            recovery = null;
            try
            {
                if (!File.Exists(path))
                    return RecoveryReadStatus.Missing;
                var read = JsonSerializer.Deserialize<Recovery>(File.ReadAllText(path));
                if (read?.Document == null || read.LibraryPath != Normalize(libraryPath))
                    return RecoveryReadStatus.Corrupt;
                recovery = read;
                return RecoveryReadStatus.Ok;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                return RecoveryReadStatus.Unavailable;
            }
            catch (Exception e) when (e is JsonException or NotSupportedException or ArgumentException)
            {
                return RecoveryReadStatus.Corrupt;
            }
        }

        public void Delete(string libraryPath, long id) => DeleteFile(PathFor(libraryPath, id));

        static void DeleteFile(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
            }
        }

        // Deletes this library's legacy (all-digit key) recovery files for designs that no longer exist. String-keyed
        // files are left alone.
        public void DeleteStale(string libraryPath, IEnumerable<long> existingIds)
        {
            try
            {
                if (!Directory.Exists(_folder))
                    return;
                var keep = new HashSet<string>();
                foreach (long id in existingIds)
                    keep.Add(Path.GetFileName(PathFor(libraryPath, id)));
                string prefix = Prefix(libraryPath) + "-";
                foreach (string file in Directory.GetFiles(_folder, prefix + "*.json"))
                {
                    string name = Path.GetFileName(file);
                    string key = name.Substring(prefix.Length, name.Length - prefix.Length - ".json".Length);
                    if (IsAllDigits(key) && !keep.Contains(name))
                        File.Delete(file);
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
            }
        }
    }
}
