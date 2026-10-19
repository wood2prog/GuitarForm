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
        // Tells the Strings and Frets tabs whether a design is open and which item its guitar uses (and why it has none,
        // if it was saved with one that is gone: a null note means nothing to say).
        void UpdateStringsState()
        {
            _strings?.ShowAssigned(_session.IsOpen, CurrentGuitar.Neck.StringSet, _session.StringSetNote);
            _frets?.ShowAssigned(_session.IsOpen, CurrentGuitar.Neck.Fretwire, _session.FretwireNote);
        }

        // "Use for this guitar" on the Frets tab (null for none): the fretwire becomes the working guitar's, as an unsaved edit.
        void OnUseFretwire(Fretwire wire)
        {
            if (!_session.IsOpen)
                return;
            _session.UseFretwire(wire);
            MarkDirty();
            UpdateStringsState();
            Redraw();
        }

        // A fretwire was saved or deleted in the library: the guitar's copy is read again (not an edit of the design).
        void OnFretwireLibraryChanged() => OnStringLibraryChanged();

        // "Use for this guitar": the saved set becomes the working guitar's string set, as an unsaved edit.
        void OnUseStringSet(StringSet set)
        {
            if (!_session.IsOpen)
                return;
            _session.UseStringSet(set);
            MarkDirty();
            UpdateStringsState();
            Redraw();
        }

        // A string set was saved or deleted in the library: the guitar's copy is read again (not an edit of the design).
        void OnStringLibraryChanged()
        {
            if (!_session.IsOpen)
                return;
            // Both library items are read again (a deleted one leaves the guitar with none and a note).
            try
            {
                _session.ReloadLibraryItems();
            }
            catch (Exception e)
            {
                RhinoApp.WriteLine($"GuitarForm: couldn't read the string set or fretwire: {e.Message}");
                // The guitar keeps what it has; the tabs say the library couldn't be read.
                if (CurrentGuitar.Neck.StringSet?.Id != null)
                    _session.StringSetNote = "couldn't be read from the library just now";
                if (CurrentGuitar.Neck.Fretwire?.Id != null)
                    _session.FretwireNote = "couldn't be read from the library just now";
            }
            UpdateStringsState();
            Redraw();
        }
    }
}
