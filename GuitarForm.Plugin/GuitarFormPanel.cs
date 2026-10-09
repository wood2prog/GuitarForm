using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Eto.Drawing;
using Eto.Forms;
using GuitarForm.Data;

namespace GuitarForm.Plugin
{
    // The dockable GuitarForm panel. For now it has only the Settings tab: where the library and its backups are kept,
    // how often backups are made and how many are kept, and backing up and restoring by hand.
    [Guid("b27065af-ab08-4c16-aeef-415d7616b93c")]
    public class GuitarFormPanel : Panel
    {
        readonly TextBox _libraryFolder = new TextBox { ReadOnly = true };
        readonly TextBox _backupFolder = new TextBox { ReadOnly = true };
        readonly NumericStepper _interval = Stepper(1, 24 * 60);
        readonly NumericStepper _keepLast = Stepper(1, 1000);
        readonly NumericStepper _keepDays = Stepper(0, 3650);
        readonly Label _status = new Label { Wrap = WrapMode.Word };

        // True while the controls are being filled from the settings, so their change events don't save them back.
        bool _showing;

        static GuitarFormPlugin Plugin => GuitarFormPlugin.Instance;

        public GuitarFormPanel()
        {
            _interval.ValueChanged += (_, _) => SaveNumbers();
            _keepLast.ValueChanged += (_, _) => SaveNumbers();
            _keepDays.ValueChanged += (_, _) => SaveNumbers();

            var settings = new TableLayout
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
                    new TableRow { ScaleHeight = true },
                },
            };

            Content = new TabControl
            {
                Pages = { new TabPage { Text = "Settings", Content = new Scrollable { Content = settings, Border = BorderType.None } } },
            };
            ShowSettings();
        }

        static NumericStepper Stepper(int min, int max) =>
            new NumericStepper { MinValue = min, MaxValue = max, DecimalPlaces = 0, Increment = 1, Width = 80 };

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

        void ShowSettings()
        {
            var settings = Plugin.LibrarySettings;
            _showing = true;
            _libraryFolder.Text = Path.GetDirectoryName(settings.LibraryPath);
            _backupFolder.Text = settings.BackupFolder;
            _interval.Value = settings.BackupIntervalMinutes;
            _keepLast.Value = settings.KeepLastBackups;
            _keepDays.Value = settings.KeepDailyBackupsDays;
            _showing = false;
            _status.Text = Status(settings);
        }

        static string Status(LibrarySettings settings)
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

        void SaveNumbers()
        {
            if (_showing)
                return;
            Plugin.UpdateLibrarySettings(Plugin.LibrarySettings with
            {
                BackupIntervalMinutes = (int)_interval.Value,
                KeepLastBackups = (int)_keepLast.Value,
                KeepDailyBackupsDays = (int)_keepDays.Value,
            });
            _status.Text = Status(Plugin.LibrarySettings);
        }

        // A library already in the chosen folder is opened; otherwise the current library is moved there, or a new,
        // empty one is started there.
        void ChangeLibraryFolder()
        {
            var settings = Plugin.LibrarySettings;
            var dialog = new SelectFolderDialog { Title = "Library folder", Directory = Path.GetDirectoryName(settings.LibraryPath) };
            if (dialog.ShowDialog(this) != DialogResult.Ok)
                return;

            string path = Path.GetFullPath(Path.Combine(dialog.Directory, LibrarySettings.LibraryFileName));
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

            Plugin.UpdateLibrarySettings(settings with { LibraryPath = path });
        }

        void ChangeBackupFolder()
        {
            var settings = Plugin.LibrarySettings;
            var dialog = new SelectFolderDialog { Title = "Backup folder", Directory = settings.BackupFolder };
            if (dialog.ShowDialog(this) == DialogResult.Ok)
                Plugin.UpdateLibrarySettings(settings with { BackupFolder = dialog.Directory });
        }

        void BackUpNow()
        {
            string path = Plugin.BackupScheduler.BackUpNow();
            MessageBox.Show(this, $"Backed up to {path}", "GuitarForm");
        }

        void Restore()
        {
            var settings = Plugin.LibrarySettings;
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
            MessageBox.Show(this,
                safety == null ? "Restored." : $"Restored. The library it replaced was backed up to {safety}",
                "GuitarForm");
        }

        void OpenBackupFolder()
        {
            string folder = Plugin.LibrarySettings.BackupFolder;
            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
        }
    }
}
