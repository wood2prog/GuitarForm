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
        // Asks whether to take the design's recovery file back as the working copy. Called right after Open.
        void OfferRecovery(Recovery pending)
        {
            if (MessageBox.Show(this, $"Recover unsaved changes to \"{_session.SavedName}\"?", "GuitarForm",
                    MessageBoxButtons.YesNo, MessageBoxType.Question) == DialogResult.Yes)
            {
                try
                {
                    _session.AcceptRecovery(pending);
                    MarkDirty();
                    return;
                }
                catch (Exception e)
                {
                    string what = e is FormatException
                        ? $"The recovery file couldn't be read: {e.Message}"
                        : $"The recovered changes couldn't be written to a new recovery file: {e.Message} The changes are kept as your unsaved working copy; save the design to keep them.";
                    MessageBox.Show(this, what, "GuitarForm", MessageBoxType.Error);
                    // An unreadable file is deleted by the session; recovered work (if it was taken back) stays as the dirty working copy.
                    if (_session.Dirty)
                        MarkDirty();
                    return;
                }
            }
            try
            {
                _session.DeclineRecovery();
            }
            catch (Exception e)
            {
                RhinoApp.WriteLine($"GuitarForm: couldn't delete the recovery file: {e.Message}");
            }
        }

        void UpdateDirtyUi()
        {
            _dirtyLabel.Text = _session.Dirty ? "Unsaved changes" : "";
            _saveButton.Enabled = _session.Dirty && _session.IsOpen;
        }

        // Records an edit of the working copy (the session has marked it dirty); nothing is written until Save.
        void MarkDirty()
        {
            _discardChosen = false;
            UpdateDirtyUi();
            _recoveryTimer.Stop();
            _recoveryTimer.Start();
        }

        // Writes the unsaved working copy to its recovery file now (also on hiding, closing and shutting down).
        internal void FlushRecovery()
        {
            _strings?.FlushRecovery();
            _frets?.FlushRecovery();
            try
            {
                _recoveryTimer.Stop();
                _session.FlushRecovery();
            }
            catch (Exception e)
            {
                RhinoApp.WriteLine($"GuitarForm: couldn't write the recovery file: {e.Message}");
            }
        }
    }
}
