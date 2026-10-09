using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using GuitarForm.Data;

namespace GuitarForm.Plugin
{
    // The Settings tab: where the library and its backups are kept, how often backups are made and how many are kept,
    // backing up and restoring by hand, and the drawing gap.
    sealed class SettingsPage : Panel
    {
        readonly TextBox _libraryFolder = new TextBox { ReadOnly = true };
        readonly TextBox _backupFolder = new TextBox { ReadOnly = true };
        readonly NumericStepper _interval = Stepper(1, 24 * 60, 0);
        readonly NumericStepper _keepLast = Stepper(1, 1000, 0);
        readonly NumericStepper _keepDays = Stepper(0, 3650, 0);
        readonly NumericStepper _drawingGap = Stepper(0, 10000, 2);
        readonly Label _drawingGapLabel = new Label { VerticalAlignment = VerticalAlignment.Center };
        readonly Label _status = new Label { Wrap = WrapMode.Word };

        // True while the controls are being filled from the settings, so their change events don't save them back.
        bool _showing;

        static GuitarFormPlugin Plugin => GuitarFormPlugin.Instance;

        // Raised when the library was replaced, moved or switched, so open designs should be reloaded.
        public event Action LibraryChanged;

        // Raised when a drawing setting changed, so the preview should be redrawn.
        public event Action DrawingChanged;

        public SettingsPage()
        {
            _interval.ValueChanged += (_, _) => SaveBackupNumbers();
            _keepLast.ValueChanged += (_, _) => SaveBackupNumbers();
            _keepDays.ValueChanged += (_, _) => SaveBackupNumbers();
            _drawingGap.ValueChanged += (_, _) => SaveDrawingGap();

            Content = new TableLayout
            {
                Padding = 10,
                Spacing = new Size(6, 6),
                Rows =
                {
                    new Label { Text = "Library folder (kept on this computer)" },
                    Row(_libraryFolder, ActionButton("Change…", ChangeLibraryFolder)),
                    new Label { Text = "Backup folder (e.g. in OneDrive)" },
                    Row(_backupFolder, ActionButton("Change…", ChangeBackupFolder)),
                    Row(new Label { Text = "Back up every (minutes)" }, _interval),
                    Row(new Label { Text = "Keep the last (backups)" }, _keepLast),
                    Row(new Label { Text = "Also keep one a day for (days)" }, _keepDays),
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 6,
                        Items =
                        {
                            ActionButton("Back up now", BackUpNow),
                            ActionButton("Restore…", Restore),
                            ActionButton("Open backup folder", OpenBackupFolder),
                        },
                    },
                    _status,
                    Row(_drawingGapLabel, _drawingGap),
                    new TableRow { ScaleHeight = true },
                },
            };
            ShowSettings();
        }

        static NumericStepper Stepper(int min, int max, int decimals) =>
            new NumericStepper { MinValue = min, MaxValue = max, DecimalPlaces = decimals, Increment = 1, Width = 80 };

        // A control that stretches, followed by one that keeps its size.
        static TableLayout Row(Control stretch, Control fixedSize) =>
            new TableLayout(new TableRow(new TableCell(stretch, true), fixedSize)) { Spacing = new Size(6, 0) };

        // A button whose action's errors are shown in a message box. The settings and status are shown again after.
        Button ActionButton(string text, Action action)
        {
            var button = new Button { Text = text };
            button.Click += (_, _) =>
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    MessageBox.Show(this, e.Message, "GuitarForm", MessageBoxType.Error);
                }
                ShowSettings();
            };
            return button;
        }

        public void ShowSettings()
        {
            var settings = Plugin.GuitarFormSettings;
            _showing = true;
            _libraryFolder.Text = Path.GetDirectoryName(settings.LibraryPath);
            _backupFolder.Text = settings.BackupFolder;
            _interval.Value = settings.BackupIntervalMinutes;
            _keepLast.Value = settings.KeepLastBackups;
            _keepDays.Value = settings.KeepDailyBackupsDays;
            var units = DisplayUnits.Current;
            _drawingGapLabel.Text = $"Gap between plate and side view ({units.Abbreviation})";
            _drawingGap.Value = units.FromMillimetres(settings.DrawingGap);
            _showing = false;
            _status.Text = Status(settings);
        }

        static string Status(GuitarFormSettings settings)
        {
            string library;
            try
            {
                using var opened = Library.Open(settings.LibraryPath);
                int count = opened.ListDesigns().Count;
                library = $"Library: {count} design{(count == 1 ? "" : "s")}.";
            }
            catch (Exception e)
            {
                library = $"Couldn't open the library: {e.Message}";
            }

            var newest = Backups.List(settings.BackupFolder).FirstOrDefault();
            string backup = newest == null ? "Not backed up yet." : $"Last backup: {newest.Time:g}.";
            return library + "\n" + backup;
        }

        void SaveBackupNumbers()
        {
            if (_showing)
                return;
            Plugin.UpdateSettings(Plugin.GuitarFormSettings with
            {
                BackupIntervalMinutes = (int)_interval.Value,
                KeepLastBackups = (int)_keepLast.Value,
                KeepDailyBackupsDays = (int)_keepDays.Value,
            });
        }

        // The gap is stored in mm and shown in the document's units.
        void SaveDrawingGap()
        {
            if (_showing)
                return;
            Plugin.UpdateSettings(Plugin.GuitarFormSettings with
            {
                DrawingGap = DisplayUnits.Current.ToMillimetres(_drawingGap.Value),
            });
            DrawingChanged?.Invoke();
        }

        // A library already in the chosen folder is opened; otherwise the current library is moved there, or a new,
        // empty one is started there.
        void ChangeLibraryFolder()
        {
            var settings = Plugin.GuitarFormSettings;
            var dialog = new SelectFolderDialog { Title = "Library folder", Directory = Path.GetDirectoryName(settings.LibraryPath) };
            if (dialog.ShowDialog(this) != DialogResult.Ok)
                return;

            string path = Path.GetFullPath(Path.Combine(dialog.Directory, GuitarFormSettings.LibraryFileName));
            if (string.Equals(path, Path.GetFullPath(settings.LibraryPath), StringComparison.OrdinalIgnoreCase))
                return;

            if (File.Exists(path))
            {
                if (MessageBox.Show(this, $"There's already a library in {dialog.Directory}. Use it?\n\nThe current library stays where it is.",
                        "GuitarForm", MessageBoxButtons.OKCancel, MessageBoxType.Question) != DialogResult.Ok)
                    return;
                using (Library.Open(path)) { } // Refuses a file that isn't a library before switching to it.
            }
            else if (File.Exists(settings.LibraryPath))
            {
                var answer = MessageBox.Show(this,
                    $"Move the current library to {dialog.Directory}?\n\nYes moves it. No starts a new, empty library there and leaves the current one where it is.",
                    "GuitarForm", MessageBoxButtons.YesNoCancel, MessageBoxType.Question);
                if (answer == DialogResult.Cancel)
                    return;
                if (answer == DialogResult.Yes)
                    Plugin.BackupScheduler.MoveLibrary(settings.LibraryPath, path);
            }

            Plugin.UpdateSettings(settings with { LibraryPath = path });
            LibraryChanged?.Invoke();
        }

        void ChangeBackupFolder()
        {
            var settings = Plugin.GuitarFormSettings;
            var dialog = new SelectFolderDialog { Title = "Backup folder", Directory = settings.BackupFolder };
            if (dialog.ShowDialog(this) == DialogResult.Ok)
                Plugin.UpdateSettings(settings with { BackupFolder = dialog.Directory });
        }

        void BackUpNow()
        {
            string path = Plugin.BackupScheduler.BackUpNow();
            MessageBox.Show(this, $"Backed up to {path}", "GuitarForm");
        }

        void Restore()
        {
            var settings = Plugin.GuitarFormSettings;
            Directory.CreateDirectory(settings.BackupFolder);
            var dialog = new OpenFileDialog
            {
                Title = "Restore from backup",
                Directory = new Uri(settings.BackupFolder),
                Filters = { new FileFilter("GuitarForm library", ".db") },
            };
            if (dialog.ShowDialog(this) != DialogResult.Ok)
                return;

            if (MessageBox.Show(this,
                    $"Replace the library with {Path.GetFileName(dialog.FileName)}?\n\nThe current library is backed up first.",
                    "GuitarForm", MessageBoxButtons.OKCancel, MessageBoxType.Warning) != DialogResult.Ok)
                return;

            string safety = Plugin.BackupScheduler.Restore(dialog.FileName);
            LibraryChanged?.Invoke();
            MessageBox.Show(this,
                safety == null ? "Restored." : $"Restored. The library it replaced was backed up to {safety}",
                "GuitarForm");
        }

        void OpenBackupFolder()
        {
            string folder = Plugin.GuitarFormSettings.BackupFolder;
            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
        }
    }
}
