using System;
using System.IO;

namespace GuitarForm.Data
{
    // Where the library and its backups are kept, and how often backups are made. The main copy is kept locally (not
    // synced) and the backups in Documents, which OneDrive usually syncs, so a sync never sees a half-written file.
    public sealed record LibrarySettings
    {
        public const string LibraryFileName = "GuitarForm.db";

        public static string DefaultLibraryPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GuitarForm", LibraryFileName);

        public static string DefaultBackupFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GuitarForm", "Backups");

        public string LibraryPath { get; init; } = DefaultLibraryPath;
        public string BackupFolder { get; init; } = DefaultBackupFolder;
        public int BackupIntervalMinutes { get; init; } = 15;
        public int KeepLastBackups { get; init; } = 20;
        public int KeepDailyBackupsDays { get; init; } = 30;

        // The settings with empty paths replaced by the defaults and numbers brought into range.
        public LibrarySettings Corrected() => this with
        {
            LibraryPath = string.IsNullOrWhiteSpace(LibraryPath) ? DefaultLibraryPath : LibraryPath,
            BackupFolder = string.IsNullOrWhiteSpace(BackupFolder) ? DefaultBackupFolder : BackupFolder,
            BackupIntervalMinutes = Math.Clamp(BackupIntervalMinutes, 1, 24 * 60),
            KeepLastBackups = Math.Max(KeepLastBackups, 1),
            KeepDailyBackupsDays = Math.Max(KeepDailyBackupsDays, 0),
        };
    }
}
