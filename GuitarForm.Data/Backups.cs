using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace GuitarForm.Data
{
    // A backup file, and the local time it was made (from its name).
    public sealed record BackupFile(string Path, DateTime Time);

    // Backups of the library, written to a folder (usually one OneDrive syncs) as "GuitarForm 2026-10-08 183005.db".
    // Each is written with SQLite's backup API to a temporary name and then renamed, so the folder only ever holds
    // complete, consistent files.
    public static class Backups
    {
        const string Prefix = "GuitarForm ";
        const string TimeFormat = "yyyy-MM-dd HHmmss";

        public static string BackUp(string libraryPath, string backupFolder) =>
            BackUp(libraryPath, backupFolder, DateTime.Now);

        // Returns the backup's path. `now` is the local time that names the file.
        public static string BackUp(string libraryPath, string backupFolder, DateTime now)
        {
            Directory.CreateDirectory(backupFolder);
            string path = System.IO.Path.Combine(backupFolder, Prefix + now.ToString(TimeFormat, CultureInfo.InvariantCulture) + ".db");
            string temporary = path + ".tmp";
            try
            {
                Copy(libraryPath, temporary);
                File.Move(temporary, path, true);
            }
            finally
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); } catch { /* best effort: must not replace the real failure */ }
            }
            return path;
        }

        // The backups in the folder, newest first. Other files are ignored.
        public static IReadOnlyList<BackupFile> List(string backupFolder)
        {
            if (!Directory.Exists(backupFolder))
                return Array.Empty<BackupFile>();

            var backups = new List<BackupFile>();
            foreach (string path in Directory.EnumerateFiles(backupFolder, Prefix + "*.db"))
            {
                string time = System.IO.Path.GetFileNameWithoutExtension(path).Substring(Prefix.Length);
                if (DateTime.TryParseExact(time, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                    backups.Add(new BackupFile(path, parsed));
            }
            return backups.OrderByDescending(b => b.Time).ToList();
        }

        // True when the library has changed since the newest backup, or has never been backed up.
        public static bool NeedsBackup(string libraryPath, string backupFolder)
        {
            if (!File.Exists(libraryPath))
                return false;
            var newest = List(backupFolder).FirstOrDefault();
            return newest == null || File.GetLastWriteTime(libraryPath) > newest.Time;
        }

        // Deletes old backups: keeps the newest `keepLast`, plus the newest one from each of the last `keepDays` days
        // (today counts as one). Returns the deleted files.
        public static IReadOnlyList<BackupFile> Prune(string backupFolder, int keepLast, int keepDays, DateTime now)
        {
            var delete = SelectToDelete(List(backupFolder), keepLast, keepDays, now);
            foreach (var backup in delete)
                File.Delete(backup.Path);
            return delete;
        }

        public static IReadOnlyList<BackupFile> SelectToDelete(
            IEnumerable<BackupFile> backups, int keepLast, int keepDays, DateTime now)
        {
            var newestFirst = backups.OrderByDescending(b => b.Time).ToList();
            var keep = new HashSet<BackupFile>(newestFirst.Take(Math.Max(keepLast, 0)));
            var firstDay = now.Date.AddDays(1 - keepDays);
            foreach (var day in newestFirst.Where(b => b.Time.Date >= firstDay).GroupBy(b => b.Time.Date))
                keep.Add(day.First());
            return newestFirst.Where(b => !keep.Contains(b)).ToList();
        }

        // Replaces the library with a backup. The current library is backed up first, and that backup's path is returned
        // (null if there was no library). Throws InvalidDataException, leaving the library alone, if the backup isn't a
        // library this version can read. Close the library before restoring.
        public static string Restore(string backupPath, string libraryPath, string backupFolder)
        {
            try
            {
                using var backup = Library.Connect(backupPath, SqliteOpenMode.ReadOnly);
                Schema.Check(backup, backupPath);
            }
            catch (SqliteException e)
            {
                throw new InvalidDataException($"{backupPath} is not a GuitarForm library: {e.Message}", e);
            }

            // Never overwrites an earlier backup made in the same second: the safety copy moves on to the next free second.
            string safety = null;
            if (File.Exists(libraryPath))
            {
                var time = DateTime.Now;
                while (File.Exists(System.IO.Path.Combine(backupFolder,
                           Prefix + time.ToString(TimeFormat, CultureInfo.InvariantCulture) + ".db")))
                    time = time.AddSeconds(1);
                safety = BackUp(libraryPath, backupFolder, time);
            }
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(libraryPath)));
            Copy(backupPath, libraryPath);
            return safety;
        }

        // Before a schema upgrade: copies the library beside itself as "<name>.before-v{version}.bak" (".1.bak", ".2.bak"
        // and so on if that name is taken, so an earlier safety copy is never overwritten). Written to a temporary name
        // and then renamed. Throws IOException, naming the file and the cause, if the copy can't be made.
        internal static string SafetyCopy(string libraryPath, int fromVersion)
        {
            string folder = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(libraryPath));
            string stem = System.IO.Path.GetFileNameWithoutExtension(libraryPath) + ".before-v" + fromVersion.ToString(CultureInfo.InvariantCulture);
            string path = System.IO.Path.Combine(folder, stem + ".bak");
            for (int n = 1; File.Exists(path) || Directory.Exists(path); n++)
                path = System.IO.Path.Combine(folder, stem + "." + n.ToString(CultureInfo.InvariantCulture) + ".bak");
            string temporary = path + ".tmp";
            try
            {
                Copy(libraryPath, temporary);
                File.Move(temporary, path, false);
                return path;
            }
            catch (Exception e)
            {
                throw new IOException(
                    $"Could not back up {libraryPath} to {path} before upgrading it, so it was not opened: {e.Message} Check that the library's folder is writable and has free space, or move the library to a folder you can write to.", e);
            }
            finally
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); } catch { /* best effort: must not replace the real failure */ }
            }
        }

        // Writes the upgrade's notes beside the safety copy, as "<its name without .bak>.notes.txt" (never over an
        // existing file). Best effort: the upgrade has already happened, so a failure here is ignored. Returns the
        // path, or null if nothing was written.
        internal static string WriteNotes(string safetyCopyPath, IEnumerable<string> notes)
        {
            try
            {
                string stem = safetyCopyPath.EndsWith(".bak", StringComparison.OrdinalIgnoreCase)
                    ? safetyCopyPath.Substring(0, safetyCopyPath.Length - 4) : safetyCopyPath;
                string path = stem + ".notes.txt";
                for (int n = 1; File.Exists(path) || Directory.Exists(path); n++)
                    path = stem + "." + n.ToString(CultureInfo.InvariantCulture) + ".notes.txt";
                var lines = new List<string>
                {
                    "GuitarForm upgraded this library. These are the things it had to tell you:",
                    "",
                };
                lines.AddRange(notes.Select(n => "- " + n));
                File.WriteAllLines(path, lines);
                return path;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }

        // Copies a database page by page, replacing the destination's contents.
        static void Copy(string sourcePath, string destinationPath)
        {
            using var source = Library.Connect(sourcePath, SqliteOpenMode.ReadOnly);
            using var destination = Library.Connect(destinationPath, SqliteOpenMode.ReadWriteCreate);
            source.BackupDatabase(destination);
        }
    }
}
