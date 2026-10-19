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
using GuitarDrawing = GuitarForm.Shapes.GuitarDrawing;
using IPanel = Rhino.UI.IPanel;
using ShowPanelReason = Rhino.UI.ShowPanelReason;

namespace GuitarForm
{
    public partial class GuitarFormPanel
    {
        // --- The open design ---

        static TResult WithLibrary<TResult>(Func<Library, TResult> action)
        {
            using var library = Library.Open(Plugin.GuitarFormSettings.LibraryPath);
            return action(library);
        }

        static void WithLibrary(Action<Library> action) => WithLibrary(library =>
        {
            action(library);
            return 0;
        });

        void ShowDesignList(long? select)
        {
            _showing = true;
            try
            {
                _designs.Items.Clear();
                var guitars = WithLibrary(library => library.ListGuitars());
                foreach (var guitar in guitars)
                    _designs.Items.Add(new ListItem { Text = guitar.Name, Key = guitar.Id.ToString(CultureInfo.InvariantCulture) });
                _session.DeleteStaleRecovery();
                _designs.SelectedKey = select?.ToString(CultureInfo.InvariantCulture);
                ShowUpgradeNotes();
            }
            catch (Exception e)
            {
                RhinoApp.WriteLine($"GuitarForm: couldn't list the designs: {e.Message}");
            }
            finally
            {
                _showing = false;
            }
        }

        // What a library upgrade had to say (see Library.UpgradeNotes), once: in Rhino's command history, and in a
        // message box when the panel has settled. The same notes are in a text file beside the library's safety copy.
        void ShowUpgradeNotes()
        {
            var notes = Library.TakeUpgradeNotes();
            if (notes.Count == 0)
                return;
            RhinoApp.WriteLine("GuitarForm: the library was upgraded. Notes:");
            foreach (string note in notes)
                RhinoApp.WriteLine("  " + note);
            Application.Instance.AsyncInvoke(() =>
            {
                if (!_disposed)
                    MessageBox.Show(this,
                        "The library was upgraded, and its designs were converted. " +
                        "These notes are also in a .notes.txt file beside the library's .bak safety copy.\n\n" +
                        string.Join("\n", notes.Take(12).Select(n => "- " + n)) +
                        (notes.Count > 12 ? $"\n... and {notes.Count - 12} more." : ""),
                        "GuitarForm", MessageBoxButtons.OK, MessageBoxType.Information);
            });
        }

        // The drop-down shows the open design (or none).
        void SelectOpenDesign()
        {
            _showing = true;
            _designs.SelectedKey = _session.IsOpen ? _session.GuitarId.ToString(CultureInfo.InvariantCulture) : null;
            _showing = false;
        }

        void OnDesignSelected()
        {
            if (_showing || _designs.SelectedKey == null)
                return;
            long id = long.Parse(_designs.SelectedKey, CultureInfo.InvariantCulture);
            if (_session.IsOpen && id == _session.GuitarId)
                return;
            if (!ConfirmDiscard())
            {
                // Cancelled: put the previous design back in the drop-down.
                SelectOpenDesign();
                return;
            }
            try
            {
                Open(id);
            }
            catch (Exception e)
            {
                MessageBox.Show(this, e.Message, "GuitarForm", MessageBoxType.Error);
                // Nothing was replaced: put the current design back in the drop-down.
                _discardChosen = false;
                SelectOpenDesign();
            }
        }

        // Opens a saved design as the working copy, dropping any unsaved changes the user chose to discard. Offers its
        // recovery file if there is one.
        void Open(long id)
        {
            // The session loads first, so a failure leaves the open design as it was.
            var (problems, pending) = _session.Open(id, _discardChosen);
            _discardChosen = false;
            if (pending != null)
                OfferRecovery(pending);
            AfterOpened();
            if (problems.Count > 0)
                ShowOpenProblems(problems);
        }

        // Some stored values couldn't be read; the design opens without them.
        void ShowOpenProblems(IReadOnlyList<string> problems)
        {
            foreach (string problem in problems)
                RhinoApp.WriteLine($"GuitarForm: {problem}");
            MessageBox.Show(this,
                "Some values stored with this design couldn't be read, so they are left blank:\n\n" +
                string.Join("\n", problems.Take(12).Select(p => "- " + p)) +
                (problems.Count > 12 ? $"\n... and {problems.Count - 12} more." : ""),
                "GuitarForm", MessageBoxButtons.OK, MessageBoxType.Warning);
        }

        // The session has switched to another guitar (or reloaded this one): shows it.
        void AfterOpened()
        {
            _discardChosen = false;
            ShowDesignList(_session.IsOpen ? _session.GuitarId : null);
            ShowDesign();
        }

        // Empties the panel, e.g. when the library changes. The session is made again, for the library's path.
        void CloseDesign()
        {
            _recoveryTimer.Stop();
            if (_discardChosen && _session.IsOpen)
            {
                try
                {
                    _session.DiscardWorkingCopy();
                }
                catch (Exception e)
                {
                    RhinoApp.WriteLine($"GuitarForm: couldn't delete the recovery file: {e.Message}");
                }
            }
            _discardChosen = false;
            _session.Close();
            _session = new DesignSession(CurrentLibraryPath);
            ShowDesign();
        }

        // Fills every tab from the open design, enables what needs one, and redraws.
        void ShowDesign()
        {
            _units = DisplayUnits.Current;
            _showing = true;
            ShowFields();
            _showing = false;

            foreach (var control in _needDesign)
                control.Enabled = _session.IsOpen;
            _exportItem.Enabled = _session.IsOpen;
            UpdateDirtyUi();
            UpdateStringsState();
            Redraw();
        }
    }
}
