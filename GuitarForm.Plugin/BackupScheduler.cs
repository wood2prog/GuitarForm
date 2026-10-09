using System;
using System.IO;
using GuitarForm.Data;
using Rhino;

namespace GuitarForm.Plugin
{
    // Backs up the library every few minutes while it has changed since the last backup, and when Rhino closes, then
    // deletes old backups. Everything that writes the library file as a whole (backing up, restoring, moving) runs
    // under one lock, so they never overlap.
    sealed class BackupScheduler : IDisposable
    {
        readonly object _lock = new object();
        readonly System.Timers.Timer _timer = new System.Timers.Timer { AutoReset = true };
        readonly Func<LibrarySettings> _settings;

        public BackupScheduler(Func<LibrarySettings> settings)
        {
            _settings = settings;
            _timer.Elapsed += (_, _) => BackUpIfNeededQuietly();
            RhinoApp.Closing += OnRhinoClosing;
            Restart();
        }

        // Starts the timer again with the current interval.
        public void Restart()
        {
            _timer.Stop();
            _timer.Interval = TimeSpan.FromMinutes(_settings().BackupIntervalMinutes).TotalMilliseconds;
            _timer.Start();
        }

        // Backs up and prunes, whether or not the library has changed. Returns the backup's path.
        public string BackUpNow()
        {
            lock (_lock)
            {
                var settings = _settings();
                string path = Backups.BackUp(settings.LibraryPath, settings.BackupFolder);
                Backups.Prune(settings.BackupFolder, settings.KeepLastBackups, settings.KeepDailyBackupsDays, DateTime.Now);
                return path;
            }
        }

        // Replaces the library with a backup; see Backups.Restore. Returns the backup of the replaced library.
        public string Restore(string backupPath)
        {
            lock (_lock)
            {
                var settings = _settings();
                return Backups.Restore(backupPath, settings.LibraryPath, settings.BackupFolder);
            }
        }

        // Moves the library file, if there is one, to a new path.
        public void MoveLibrary(string from, string to)
        {
            lock (_lock)
            {
                if (!File.Exists(from))
                    return;
                Directory.CreateDirectory(Path.GetDirectoryName(to));
                File.Move(from, to);
            }
        }

        public void Dispose()
        {
            RhinoApp.Closing -= OnRhinoClosing;
            _timer.Dispose();
        }

        void OnRhinoClosing(object sender, EventArgs e) => BackUpIfNeededQuietly();

        // Reports problems on the command line rather than throwing, since nothing is waiting for the result.
        void BackUpIfNeededQuietly()
        {
            try
            {
                lock (_lock)
                {
                    var settings = _settings();
                    if (Backups.NeedsBackup(settings.LibraryPath, settings.BackupFolder))
                        BackUpNow();
                }
            }
            catch (Exception e)
            {
                RhinoApp.InvokeOnUiThread(new Action(() => RhinoApp.WriteLine($"GuitarForm: the library backup failed: {e.Message}")));
            }
        }
    }
}
