using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using GuitarForm.Data;

namespace GuitarForm
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
        readonly Label _drawingGapLabel = FieldLayout.UnitLabel();
        readonly Label _status = new Label { Wrap = WrapMode.Word };

        // True while the controls are being filled from the settings, so their change events don't save them back.
        bool _showing;

        static GuitarFormPlugin Plugin => GuitarFormPlugin.Instance;

        // Raised when the library was replaced, moved or switched, so open designs should be reloaded.
        public event Action LibraryChanged;

        // Asked just before the library is replaced, moved or switched; false cancels (the panel offers to save its edits).
        public Func<bool> ConfirmLibraryChange { get; set; }
        bool ConfirmChange() => ConfirmLibraryChange?.Invoke() ?? true;

        // Raised when a drawing setting changed, so the preview should be redrawn.
        public event Action DrawingChanged;

        public SettingsPage()
        {
            _interval.ValueChanged += (_, _) => SaveBackupNumbers();
            _keepLast.ValueChanged += (_, _) => SaveBackupNumbers();
            _keepDays.ValueChanged += (_, _) => SaveBackupNumbers();
            _drawingGap.ValueChanged += (_, _) => SaveDrawingGap();

            var backupButtons = new StackLayout { Spacing = UiMetrics.Gap, HorizontalContentAlignment = HorizontalAlignment.Stretch };
            backupButtons.Items.Add(ActionButton("Back up now", BackUpNow));
            backupButtons.Items.Add(ActionButton("Restore…", Restore));
            backupButtons.Items.Add(ActionButton("Open backup folder", OpenBackupFolder));

            // The same label / box / unit rows as the other tabs, in groups that collapse like theirs.
            var content = new StackLayout { Padding = UiMetrics.PagePadding, Spacing = UiMetrics.GroupGap, HorizontalContentAlignment = HorizontalAlignment.Stretch };
            content.Items.Add(FieldLayout.Group("Settings", "Library", new[]
            {
                FolderRow("Library folder (kept on this computer)", _libraryFolder, ActionButton("Change…", ChangeLibraryFolder)),
            }));
            content.Items.Add(FieldLayout.Group("Settings", "Backups", new[]
            {
                FolderRow("Backup folder (e.g. in OneDrive)", _backupFolder, ActionButton("Change…", ChangeBackupFolder)),
                FieldLayout.Row("Back up every", null, _interval, UnitLabel("minutes")),
                FieldLayout.Row("Keep the last", null, _keepLast, UnitLabel("backups")),
                FieldLayout.Row("Also keep one a day for", null, _keepDays, UnitLabel("days")),
                new TableRow(null, new TableCell(backupButtons, true)),
            }));
            content.Items.Add(FieldLayout.Group("Settings", "Drawing", new[]
            {
                FieldLayout.Row("Gap between plate and side view", null, _drawingGap, _drawingGapLabel),
            }));
            content.Items.Add(_status);
            Content = content;
            ShowSettings();
        }

        static Label UnitLabel(string text) => new Label { Text = text, VerticalAlignment = VerticalAlignment.Center };

        static TableRow FolderRow(string label, TextBox box, Button change) => new TableRow(
            new Label { Text = label, Wrap = WrapMode.Word, Width = UiMetrics.LabelMinWidth, VerticalAlignment = VerticalAlignment.Center },
            new TableCell(box, true),
            change);

        static NumericStepper Stepper(int min, int max, int decimals) =>
            new NumericStepper { MinValue = min, MaxValue = max, DecimalPlaces = decimals, Increment = 1, Width = UiMetrics.BoxWidth };

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
            _drawingGapLabel.Text = units.Abbreviation;
            _drawingGap.Value = units.FromMillimetres(settings.DrawingGap);
            _showing = false;
            (_status.Text, bool failed) = Status(settings);
            _status.TextColor = failed ? UiMetrics.ErrorColor : UiMetrics.NoteColor;
        }

        static (string Text, bool Failed) Status(GuitarFormSettings settings)
        {
            string library;
            bool failed = false;
            try
            {
                using var opened = Library.Open(settings.LibraryPath);
                int count = opened.ListGuitars().Count;
                library = $"Library: {count} design{(count == 1 ? "" : "s")}.";
            }
            catch (Exception e)
            {
                library = $"Couldn't open the library: {e.Message}";
                failed = true;
            }

            var newest = Backups.List(settings.BackupFolder).FirstOrDefault();
            string backup = newest == null ? "Not backed up yet." : $"Last backup: {newest.Time:g}.";
            return (library + "\n" + backup, failed);
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
                if (!ConfirmChange())
                    return;
            }
            else if (File.Exists(settings.LibraryPath))
            {
                var answer = MessageBox.Show(this,
                    $"Move the current library to {dialog.Directory}?\n\nYes moves it. No starts a new, empty library there and leaves the current one where it is.",
                    "GuitarForm", MessageBoxButtons.YesNoCancel, MessageBoxType.Question);
                if (answer == DialogResult.Cancel || !ConfirmChange())
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
            if (!ConfirmChange())
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
