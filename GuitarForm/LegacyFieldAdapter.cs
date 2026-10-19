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
    // TEMPORARY, deleted once the field editors edit the new model: the panel's editors (SectionEditor, Sections.cs,
    // NewFields) still work on the old section records and on the new-model values the old sections lack. The session
    // holds a Guitar; these members show it to the editors and write their edits back to it (see GuitarFieldMap).
    public partial class GuitarFormPanel
    {
        static readonly Guitar NoGuitar = new Guitar();

        // The open guitar as a design of the old model (name, instrument, body, soundhole), or null with none open.
        GuitarDesign Design => _session.IsOpen ? GuitarFieldMap.ToDesign(_session.Working) : null;

        bool HasSoundhole => _session.IsOpen && _session.Working.Body.TopPlate.Soundhole.Value;

        // The open guitar, for the new-model fields (an empty guitar with none open).
        Guitar Fields => _session.Working ?? NoGuitar;

        // Changes the working copy through the old model; nothing is written to the library until Save. True if it changed.
        bool Apply(Func<GuitarDesign, GuitarDesign> change)
        {
            bool changed = ApplyToGuitar(g => GuitarFieldMap.WithDesign(g, change(GuitarFieldMap.ToDesign(g))));
            if (changed)
                Redraw();
            return changed;
        }

        // Changes the working copy of the open design. True if it changed (the dirty marks are updated; the caller redraws).
        bool ApplyToGuitar(Func<Guitar, Guitar> change)
        {
            if (!_session.IsOpen)
                return false;
            var before = _session.Working;
            _session.Apply(change);
            if (ReferenceEquals(before, _session.Working))
                return false;
            MarkDirty();
            return true;
        }
    }
}
