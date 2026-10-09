using System.Drawing;
using System.Reflection;
using GuitarForm.Data;
using Rhino.PlugIns;
using Rhino.UI;

namespace GuitarForm.Plugin
{
    public class GuitarFormPlugin : PlugIn
    {
        public GuitarFormPlugin()
        {
            Instance = this;
        }

        public static GuitarFormPlugin Instance { get; private set; }

        public LibrarySettings LibrarySettings { get; private set; } = new LibrarySettings();

        internal BackupScheduler BackupScheduler { get; private set; }

        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            LibrarySettings = ReadLibrarySettings();
            BackupScheduler = new BackupScheduler(() => LibrarySettings);
            Panels.RegisterPanel(this, typeof(GuitarFormPanel), "GuitarForm", LoadIcon());
            return LoadReturnCode.Success;
        }

        protected override void OnShutdown()
        {
            BackupScheduler?.Dispose();
            base.OnShutdown();
        }

        // Saves the settings straight away and restarts the backup timer with them.
        public void UpdateLibrarySettings(LibrarySettings settings)
        {
            LibrarySettings = settings.Corrected();
            Settings.SetString(nameof(LibrarySettings.LibraryPath), LibrarySettings.LibraryPath);
            Settings.SetString(nameof(LibrarySettings.BackupFolder), LibrarySettings.BackupFolder);
            Settings.SetInteger(nameof(LibrarySettings.BackupIntervalMinutes), LibrarySettings.BackupIntervalMinutes);
            Settings.SetInteger(nameof(LibrarySettings.KeepLastBackups), LibrarySettings.KeepLastBackups);
            Settings.SetInteger(nameof(LibrarySettings.KeepDailyBackupsDays), LibrarySettings.KeepDailyBackupsDays);
            SaveSettings();
            BackupScheduler?.Restart();
        }

        LibrarySettings ReadLibrarySettings()
        {
            var defaults = new LibrarySettings();
            return new LibrarySettings
            {
                LibraryPath = Settings.GetString(nameof(defaults.LibraryPath), defaults.LibraryPath),
                BackupFolder = Settings.GetString(nameof(defaults.BackupFolder), defaults.BackupFolder),
                BackupIntervalMinutes = Settings.GetInteger(nameof(defaults.BackupIntervalMinutes), defaults.BackupIntervalMinutes),
                KeepLastBackups = Settings.GetInteger(nameof(defaults.KeepLastBackups), defaults.KeepLastBackups),
                KeepDailyBackupsDays = Settings.GetInteger(nameof(defaults.KeepDailyBackupsDays), defaults.KeepDailyBackupsDays),
            }.Corrected();
        }

        static System.Drawing.Icon LoadIcon()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GuitarForm.Plugin.GuitarForm.png");
            using var bitmap = new Bitmap(stream);
            return System.Drawing.Icon.FromHandle(bitmap.GetHicon());
        }
    }
}
