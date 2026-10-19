using System.Drawing;
using System.Reflection;
using GuitarForm.Data;
using Rhino.PlugIns;
using Rhino.UI;

namespace GuitarForm
{
    public class GuitarFormPlugin : PlugIn
    {
        public GuitarFormPlugin()
        {
            Instance = this;
        }

        public static GuitarFormPlugin Instance { get; private set; }

        public GuitarFormSettings GuitarFormSettings { get; private set; } = new GuitarFormSettings();

        internal BackupScheduler BackupScheduler { get; private set; }

        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            GuitarFormSettings = ReadGuitarFormSettings();
            BackupScheduler = new BackupScheduler(() => GuitarFormSettings);
            Panels.RegisterPanel(this, typeof(GuitarFormPanel), "GuitarForm", LoadIcon());
            return LoadReturnCode.Success;
        }

        protected override void OnShutdown()
        {
            try
            {
                GuitarFormPanel.Current?.FlushRecovery();
            }
            finally
            {
                BackupScheduler?.Dispose();
                base.OnShutdown();
            }
        }

        // Saves the settings straight away and restarts the backup timer with them.
        public void UpdateSettings(GuitarFormSettings settings)
        {
            GuitarFormSettings = settings.Corrected();
            Settings.SetString(nameof(GuitarFormSettings.LibraryPath), GuitarFormSettings.LibraryPath);
            Settings.SetString(nameof(GuitarFormSettings.BackupFolder), GuitarFormSettings.BackupFolder);
            Settings.SetInteger(nameof(GuitarFormSettings.BackupIntervalMinutes), GuitarFormSettings.BackupIntervalMinutes);
            Settings.SetInteger(nameof(GuitarFormSettings.KeepLastBackups), GuitarFormSettings.KeepLastBackups);
            Settings.SetInteger(nameof(GuitarFormSettings.KeepDailyBackupsDays), GuitarFormSettings.KeepDailyBackupsDays);
            Settings.SetDouble(nameof(GuitarFormSettings.DrawingGap), GuitarFormSettings.DrawingGap);
            Settings.SetBool(nameof(GuitarFormSettings.StringUnitWeightInLbPerIn), GuitarFormSettings.StringUnitWeightInLbPerIn);
            Settings.SetBool(nameof(GuitarFormSettings.ShowStrings), GuitarFormSettings.ShowStrings);
            SaveSettings();
            BackupScheduler?.Restart();
        }

        // Saves just the Show strings setting; the backup timer isn't touched.
        public void SetShowStrings(bool show)
        {
            GuitarFormSettings = GuitarFormSettings with { ShowStrings = show };
            Settings.SetBool(nameof(GuitarFormSettings.ShowStrings), show);
            SaveSettings();
        }

        GuitarFormSettings ReadGuitarFormSettings()
        {
            var defaults = new GuitarFormSettings();
            return new GuitarFormSettings
            {
                LibraryPath = Settings.GetString(nameof(defaults.LibraryPath), defaults.LibraryPath),
                BackupFolder = Settings.GetString(nameof(defaults.BackupFolder), defaults.BackupFolder),
                BackupIntervalMinutes = Settings.GetInteger(nameof(defaults.BackupIntervalMinutes), defaults.BackupIntervalMinutes),
                KeepLastBackups = Settings.GetInteger(nameof(defaults.KeepLastBackups), defaults.KeepLastBackups),
                KeepDailyBackupsDays = Settings.GetInteger(nameof(defaults.KeepDailyBackupsDays), defaults.KeepDailyBackupsDays),
                DrawingGap = Settings.GetDouble(nameof(defaults.DrawingGap), defaults.DrawingGap),
                StringUnitWeightInLbPerIn = Settings.GetBool(nameof(defaults.StringUnitWeightInLbPerIn), defaults.StringUnitWeightInLbPerIn),
                ShowStrings = Settings.GetBool(nameof(defaults.ShowStrings), defaults.ShowStrings),
            }.Corrected();
        }

        static System.Drawing.Icon LoadIcon()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GuitarForm.GuitarForm.png");
            using var bitmap = new Bitmap(stream);
            return System.Drawing.Icon.FromHandle(bitmap.GetHicon());
        }
    }
}
