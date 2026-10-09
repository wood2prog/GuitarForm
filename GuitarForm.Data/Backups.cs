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
            Copy(libraryPath, temporary);
            File.Move(temporary, path, true);
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

            string safety = File.Exists(libraryPath) ? BackUp(libraryPath, backupFolder) : null;
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(libraryPath)));
            Copy(backupPath, libraryPath);
            return safety;
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
