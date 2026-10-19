using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Forms;
using GuitarForm.Model;
using GuitarForm.Model.Parts;

namespace GuitarForm
{
    // The tabs' fields: one FieldBinding per field of GuitarFields, all editing the session's working Guitar.
    public partial class GuitarFormPanel
    {
        static readonly Guitar NoGuitar = new Guitar();

        // The soundhole yes/no is the check box above the Soundhole tab's groups, not a field row.
        const string SoundholeFlagGroup = "Soundhole";

        // The open guitar (an empty guitar with none open).
        Guitar CurrentGuitar => _session.Working ?? NoGuitar;

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

        static bool IsFlagGroup(GuitarFieldGroup group) =>
            group.Tab == GuitarFields.SoundholeTab && group.Title == SoundholeFlagGroup;

        // A tab's groups, from the field table.
        IEnumerable<Control> Groups(string tab) => GuitarFields.GroupsOf(tab).Where(g => !IsFlagGroup(g))
            .Select(group =>
            {
                var rows = _bindings.Rows(group).ToList();
                if (group.HasStartingShape)
                    rows.Add(StartingShapeRow(group.Title));
                if (group.HasMarkers)
                    rows.Add(FieldLayout.Row("Marker frets", _markers.ToolTip, _markers, FieldLayout.UnitLabel()));
                return FieldLayout.Group(tab, group.Title, rows);
            }).ToList();

        // A list of named profile shapes. Picking one writes its squareness, asymmetry and blend to the working copy's
        // station and the list goes back to blank; the choice itself isn't stored.
        TableRow StartingShapeRow(string title)
        {
            var list = new DropDown
            {
                Width = UiMetrics.ChoiceBoxWidth,
                ToolTip = "Fills this section's squareness, asymmetry and blend from a named shape. You can still change them after.",
            };
            foreach (var preset in NeckProfilePresets.All)
                list.Items.Add(preset.Name);
            list.SelectedIndex = -1;
            list.SelectedIndexChanged += (_, _) =>
            {
                int picked = list.SelectedIndex;
                if (picked < 0 || _showing)
                    return;
                list.SelectedIndex = -1;
                if (!_session.IsOpen)
                    return;
                ApplyToGuitar(g => StartingShape.With(g, title, NeckProfilePresets.All[picked]));
                ShowFields();
                Redraw();
            };
            return FieldLayout.Row("Starting shape", null, list, FieldLayout.UnitLabel());
        }

        // Fills every field's box (and the markers and the soundhole check box) from the working copy.
        void ShowFields()
        {
            bool showing = _showing;
            _showing = true;
            _bindings.Show(CurrentGuitar, _units);
            _hasSoundhole.Checked = CurrentGuitar.Body.TopPlate.Soundhole.Value;
            _markers.Text = DotMarkerText.Format(CurrentGuitar.Neck.Fretboard.Markers);
            _markersError = null;
            _showing = showing;
        }

        void OnFieldChanged(FieldBinding binding)
        {
            if (_showing || !_session.IsOpen)
                return;
            if (binding.TryApply(_session.Working, out var edited))
                ApplyToGuitar(_ => edited);
            _bindings.UpdateState(_session.Working);
            Redraw();
        }

        void OnMarkersChanged()
        {
            if (_showing || !_session.IsOpen)
                return;
            var markers = DotMarkerText.Parse(_markers.Text);
            _markersError = markers != null ? null
                : "Dot markers: write each marker's fret, with :2 for two dots, e.g. 3 5 7 9 12:2.";
            if (markers != null)
                ApplyToGuitar(g => g with { Neck = g.Neck with { Fretboard = g.Neck.Fretboard with { Markers = markers } } });
            Redraw();
        }

        // Only the Soundhole yes/no changes: the soundhole's values stay as they are (their boxes are grayed while it's
        // unticked), so ticking it again brings them back, and the shapes draw no soundhole while it's unticked.
        void OnHasSoundholeChanged()
        {
            if (_showing || !_session.IsOpen)
                return;
            bool has = _hasSoundhole.Checked == true;
            ApplyToGuitar(g => g with
            {
                Body = g.Body with
                {
                    TopPlate = g.Body.TopPlate with { Soundhole = g.Body.TopPlate.Soundhole with { Value = has } },
                },
            });
            _bindings.UpdateState(_session.Working);
            Redraw();
        }

        // Whether any box holds text that couldn't be read, or a required value is blank, so the working copy isn't
        // what the boxes show or isn't complete.
        bool HasInputErrors() => _bindings.HasBlockingError || _markersError != null || BlankRequired().Any();

        // The shown required fields with no value.
        IEnumerable<GuitarField> BlankRequired() => GuitarFields.All.Where(f =>
            _session.IsOpen && f.Definition.Required && f.IsVisible(CurrentGuitar) && !f.Get(CurrentGuitar).HasValue);

        // Stops an action (shown to the user by Run or ConfirmDiscard) while some text couldn't be read.
        void RequireNoInputErrors(string what)
        {
            if (HasInputErrors())
                throw new InvalidOperationException(
                    $"Some text couldn't be read or a required value is blank, so the design can't be {what} yet. Fix the fields marked in Messages first.");
        }

        // The errors on a tab from boxes whose text couldn't be read.
        IEnumerable<GeometryMessage> InputErrors(string tab)
        {
            var fields = GuitarFields.GroupsOf(tab).SelectMany(g => g.Fields).ToList();
            // Blank required values are reported once, by the model's Validate, so they aren't repeated here.
            var errors = fields.Select(f => _bindings[f]).Where(b => b.HasBlockingError).Select(b => b.Error);
            if (tab == GuitarFields.FretboardTab)
                errors = errors.Append(_markersError);
            return errors.Where(e => e != null).Select(e => new GeometryMessage(MessageLevel.Error, e));
        }
    }
}
