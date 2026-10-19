using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Eto.Drawing;
using Eto.Forms;
using GuitarForm.Data;
using GuitarForm.Geometry;
using GuitarForm.Model;
using GuitarForm.Model.Parameters;
using GuitarForm.Model.Parts;
using Rhino;
using Body = GuitarForm.Model.Body;
using GuitarDrawing = GuitarForm.Shapes.GuitarDrawing;
using IPanel = Rhino.UI.IPanel;
using ShowPanelReason = Rhino.UI.ShowPanelReason;

namespace GuitarForm
{
    public partial class GuitarFormPanel
    {
        void Run(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                MessageBox.Show(this, e.Message, "GuitarForm", MessageBoxType.Error);
            }
        }

        string WorkingName => _session.Working?.Name.Value?.Trim() ?? "";

        // True if a design has the name (ignoring case).
        static bool NameExists(string name) => WithLibrary(library => library.GuitarExists(name, null));

        // Writes the working copy to the library. The first Save of a design asks whether to overwrite it or save a new
        // design instead.
        // overwriteConfirmed: the user already said Yes to saving over this design, so the first-save question is skipped.
        void SaveDesign(bool overwriteConfirmed = false)
        {
            if (!_session.IsOpen)
                return;
            RequireNoInputErrors("saved");
            if (!_session.SavePrompted && !overwriteConfirmed)
            {
                string saved = _session.SavedName;
                var answer = MessageBox.Show(this,
                    $"Save your changes to \"{saved}\"?\n\nYes overwrites \"{saved}\".\nNo saves them as a new design and leaves \"{saved}\" as it was.\nCancel doesn't save.",
                    "GuitarForm", MessageBoxButtons.YesNoCancel, MessageBoxType.Question);
                if (answer == DialogResult.Cancel)
                    return;
                if (answer == DialogResult.No)
                {
                    SaveAs(_session.UnusedCopyName());
                    return;
                }
            }

            string name = WorkingName;
            if (name.Length == 0)
                throw new InvalidOperationException("Enter a name for the design.");
            if (_session.NameTaken(name))
                throw new InvalidOperationException($"There's already a design called \"{name}\".");

            _session.Save();
            _recoveryTimer.Stop();
            ShowDesignList(_session.GuitarId);
            UpdateDirtyUi();
        }

        // Saves the working copy as a new design and switches to it; the original is left as it was. With no name
        // given, asks for one.
        void SaveAs(string name)
        {
            if (!_session.IsOpen)
                return;
            RequireNoInputErrors("saved");
            if (name == null)
            {
                string initial = _session.UnusedCopyName();
                var values = FieldsDialog.Ask(this, "Save as", new[] { ("Name", initial) }, v =>
                    v[0].Length == 0 ? "Enter a name." :
                    NameExists(v[0]) ? $"There's already a design called \"{v[0]}\"." : null);
                if (values == null)
                    return;
                name = values[0];
            }

            _session.SaveAs(name);
            AfterOpened();
        }

        // True when it's fine to replace the working copy: there are no unsaved changes, or the user saved them or chose
        // to discard them. False when they cancelled, or the save didn't happen.
        // Only the design's own unsaved changes count: edits to a string set or fretwire in the library tabs are
        // independent of the design and stay in memory across design switches.
        bool ConfirmDiscard() => ConfirmDiscardDesign();

        // For a change of library (Settings: switch or restore), which replaces everything: the design's unsaved
        // changes are asked about, then those in the Strings and Frets tabs. Tabs whose changes were discarded are
        // put back to their saved state so the question doesn't come back.
        bool ConfirmLibraryChange()
        {
            if (!ConfirmDiscardDesign() || !(_strings?.ConfirmDiscard() ?? true) || !(_frets?.ConfirmDiscard() ?? true))
                return false;
            _strings?.DiscardChanges();
            _frets?.DiscardChanges();
            return true;
        }

        bool ConfirmDiscardDesign()
        {
            if (!_session.Dirty || !_session.IsOpen)
                return true;
            _discardChosen = false;
            // A design's first Save would ask overwrite-or-new next, so that choice is folded into this one question.
            string saved = _session.SavedName;
            string question = _session.SavePrompted
                ? $"Save changes to \"{WorkingName}\"?"
                : $"Save changes to \"{saved}\"?\n\nYes overwrites \"{saved}\" (use Save as… to keep it).\nNo discards the changes.";
            var answer = MessageBox.Show(this, question, "GuitarForm", MessageBoxButtons.YesNoCancel, MessageBoxType.Question);
            if (answer == DialogResult.Cancel)
                return false;
            if (answer == DialogResult.No)
            {
                // Only the choice is recorded; the changes are dropped when the replacement really happens (the
                // session's discardPrevious).
                _discardChosen = true;
                return true;
            }
            try
            {
                SaveDesign(true);
            }
            catch (Exception e)
            {
                MessageBox.Show(this, e.Message, "GuitarForm", MessageBoxType.Error);
                return false;
            }
            return !_session.Dirty;
        }

        void RevertDesign()
        {
            if (!_session.IsOpen)
                return;
            if (_session.Dirty && MessageBox.Show(this, $"Discard unsaved changes to \"{_session.SavedName}\"?", "GuitarForm",
                    MessageBoxButtons.OKCancel, MessageBoxType.Warning) != DialogResult.Ok)
                return;
            _session.Revert();
            AfterOpened();
        }

        void OnUnitsMaybeChanged()
        {
            if (DisplayUnits.Current == _units)
                return;
            ShowDesign();
            _strings?.RefreshUnits();
            _frets?.RefreshUnits();
            _settings.ShowSettings();
        }

        // --- Design list buttons ---

        void NewDesign()
        {
            double length = 0, scale = 0;
            int fret = 0;
            var units = DisplayUnits.Current;
            var values = FieldsDialog.Ask(this, "New design",
                new[] { ("Name", ""), ($"Body length ({units.Abbreviation})", ""), ($"Scale length ({units.Abbreviation})", ""), ("Neck join fret", "") },
                v =>
                {
                    if (v[0].Length == 0)
                        return "Enter a name.";
                    if (NameExists(v[0]))
                        return $"There's already a design called \"{v[0]}\".";
                    if (!double.TryParse(v[1], NumberStyles.Float, CultureInfo.CurrentCulture, out length) || !(length > 0))
                        return "Body length must be a number greater than zero.";
                    if (!double.TryParse(v[2], NumberStyles.Float, CultureInfo.CurrentCulture, out scale) || !(scale > 0))
                        return "Scale length must be a number greater than zero.";
                    if (!int.TryParse(v[3], NumberStyles.Integer, CultureInfo.CurrentCulture, out fret) || fret < 1)
                        return "Neck join fret must be a whole number, 1 or more.";
                    return null;
                });
            if (values == null || !ConfirmDiscard())
                return;

            _session.CreateNew(values[0], units.ToMillimetres(length), units.ToMillimetres(scale), fret, _discardChosen);
            AfterOpened();
        }

        void CopyDesign()
        {
            if (!ConfirmDiscard())
                return;
            // The saved design is copied (the working copy was just saved or discarded).
            _session.Copy(_discardChosen);
            AfterOpened();
        }

        void RenameDesign()
        {
            string current = WorkingName;
            var values = FieldsDialog.Ask(this, "Rename design", new[] { ("Name", current) }, v =>
                v[0].Length == 0 ? "Enter a name." :
                _session.NameTaken(v[0]) ? $"There's already a design called \"{v[0]}\"." : null);
            if (values == null || values[0] == current)
                return;

            // Applied to the library on Save.
            Apply(d => d with { Name = values[0] });
        }

        void DeleteDesign()
        {
            string unsaved = _session.Dirty ? " Its unsaved changes are lost too." : "";
            if (MessageBox.Show(this, $"Delete the design \"{WorkingName}\"? This can't be undone, except by restoring a backup.{unsaved}",
                    "GuitarForm", MessageBoxButtons.OKCancel, MessageBoxType.Warning) != DialogResult.Ok)
                return;
            _session.Delete();
            _discardChosen = false;
            _recoveryTimer.Stop();
            ShowDesign();
            ShowDesignList(null);
        }

        void ExportDesign()
        {
            RequireNoInputErrors("exported");
            var dialog = new SaveFileDialog
            {
                Title = "Export design",
                FileName = WorkingName + ".json",
                Filters = { new FileFilter("Design document", ".json") },
            };
            if (dialog.ShowDialog(this) == DialogResult.Ok)
                File.WriteAllText(dialog.FileName, _session.Export());
        }

        void ImportDesign()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Import design",
                Filters = { new FileFilter("Design document", ".json") },
            };
            if (dialog.ShowDialog(this) != DialogResult.Ok)
                return;

            string json = File.ReadAllText(dialog.FileName);
            GuitarDocument.Read(json); // Throws FormatException, before anything is asked, if the file isn't a design.
            if (!ConfirmDiscard())
                return;
            var problems = _session.Import(json, Path.GetFileNameWithoutExtension(dialog.FileName), _discardChosen);
            AfterOpened();
            if (problems.Count > 0)
                ShowOpenProblems(problems);
        }
    }
}
