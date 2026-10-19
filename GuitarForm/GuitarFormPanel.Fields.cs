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
        static bool IsDecimal(ParameterKind kind) =>
            kind is ParameterKind.Length or ParameterKind.Angle or ParameterKind.Number;

        IEnumerable<TableRow> NewFieldRows(string tab, string title)
        {
            int index = 0;
            foreach (var group in NewFields.Groups)
            {
                foreach (var field in group.Fields)
                {
                    if (group.Tab == tab && group.Title == title)
                        {
                        var definition = field.Get(Fields).Definition;
                        yield return _newFieldRows[index] = FieldLayout.Row(field.Get(Fields).Label, null, (Control)_newFieldBoxes[index], _newFieldUnits[index],
                            () => NewFieldLimits(definition));
                    }
                    index++;
                }
            }
            if (NewFields.HasStartingShape(tab, title))
                yield return StartingShapeRow(title);
            if (tab == NewFields.FretboardTab && title == NewFields.MarkersGroup)
                yield return FieldLayout.Row("Marker frets", _markers.ToolTip, _markers, FieldLayout.UnitLabel());
        }

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
                ApplyToGuitar(g => NewFields.WithStartingShape(g, title, NeckProfilePresets.All[picked]));
                for (int i = 0; i < NewFields.Fields.Length; i++)
                    if (NewFieldTabs[i] == NewFields.NeckTab)
                        _newFieldErrors[i] = null;
                ShowNewFields();
                Redraw();
            };
            return FieldLayout.Row("Starting shape", null, list, FieldLayout.UnitLabel());
        }

        // Shows or hides each new field's row, and enables or disables its box, from the working copy's values (e.g.
        // squareness or blend by profile mode, the course spacings by strings per course).
        void UpdateNewFieldVisibility()
        {
            for (int i = 0; i < NewFields.Fields.Length; i++)
            {
                if (NewFields.Fields[i].Enabled is { } enabled)
                    _newFieldBoxes[i].Enabled = enabled(Fields);
                var visible = NewFields.Fields[i].Visible;
                if (visible == null || _newFieldRows[i] == null)
                    continue;
                bool show = visible(Fields);
                foreach (var cell in _newFieldRows[i].Cells)
                    if (cell.Control != null)
                        cell.Control.Visible = show;
            }
        }

        // Fills the new fields' boxes from the working copy.
        void ShowNewFields()
        {
            bool showing = _showing;
            _showing = true;
            for (int i = 0; i < NewFields.Fields.Length; i++)
            {
                var parameter = NewFields.Fields[i].Get(Fields);
                _newFieldBoxes[i].Text = parameter.Format(_units.LengthUnit, CultureInfo.CurrentCulture);
                _newFieldUnits[i].Text = parameter.Symbol(_units.LengthUnit);
                // Drag, step and arrow keys stop at the limits, in the units shown. An exclusive limit can't be
                // clamped to (the value itself is not allowed), so it is left to the typed-value check.
                if (_newFieldBoxes[i] is NumberSlider slider)
                {
                    var definition = parameter.Definition;
                    double Shown(double v) => definition.Kind == ParameterKind.Length ? _units.FromMillimetres(v) : v;
                    // Steps are defined in mm for a length, so they follow the document's units.
                    slider.Step = Shown(NewFields.Fields[i].Step);
                    slider.Minimum = definition.Minimum is double min && !definition.MinimumExclusive ? Shown(min) : null;
                    slider.Maximum = definition.Maximum is double max && !definition.MaximumExclusive ? Shown(max) : null;
                }
            }
            _showing = showing;
            UpdateNewFieldVisibility();
        }

        // "Minimum: …  Maximum: …" for a new field, in the units it's shown in; null when it has neither.
        string NewFieldLimits(ParameterDefinition definition)
        {
            string Show(double value) => definition.Kind == ParameterKind.Length
                ? _units.FormatLength(value) : value.ToString("0.#####", CultureInfo.CurrentCulture);
            var parts = new List<string>();
            if (definition.Minimum is double min)
                parts.Add("Minimum: " + (definition.MinimumExclusive ? "above " : "") + Show(min));
            if (definition.Maximum is double max)
                parts.Add("Maximum: " + (definition.MaximumExclusive ? "below " : "") + Show(max));
            return parts.Count == 0 ? null : string.Join(", ", parts);
        }

        void OnMarkersChanged()
        {
            if (_showing || !_session.IsOpen)
                return;
            string text = _markers.Text;
            bool understood = NewFields.WithMarkers(_session.Working, text) != null;
            _markersError = understood ? null : "Dot markers: write each marker's fret, with :2 for two dots, e.g. 3 5 7 9 12:2.";
            if (understood)
                ApplyToGuitar(g => NewFields.WithMarkers(g, text));
            Redraw();
        }

        void OnNewFieldChanged(int index)
        {
            if (_showing || !_session.IsOpen)
                return;
            var field = NewFields.Fields[index];
            if (field.Get(Fields).TryParse(_newFieldBoxes[index].Text, _units.LengthUnit, CultureInfo.CurrentCulture,
                    out var parsed, out var error))
            {
                _newFieldErrors[index] = null;
                ApplyToGuitar(g => field.Set(g, parsed));
                UpdateNewFieldVisibility();
            }
            else
                _newFieldErrors[index] = error.Text;
            Redraw();
        }

        // Whether any box holds text that couldn't be read, so the working copy isn't what the boxes show.
        bool HasInputErrors() =>
            _instrument.HasInputErrors || _body.HasInputErrors ||
            (HasSoundhole && _soundhole.HasInputErrors) || _markersError != null ||
            _newFieldErrors.Where((_, i) => NewFields.Fields[i].Visible?.Invoke(Fields) ?? true).Any(e => e != null);

        // Stops an action (shown to the user by Run or ConfirmDiscard) while some text couldn't be read.
        void RequireNoInputErrors(string what)
        {
            if (HasInputErrors())
                throw new InvalidOperationException(
                    $"Some text couldn't be read, so the design can't be {what} yet. Fix the fields marked in Messages first.");
        }

        void OnHasSoundholeChanged()
        {
            if (_showing || !_session.IsOpen)
                return;
            if (_hasSoundhole.Checked == true)
                Apply(d => d with { Soundhole = _lastSoundhole ?? new Soundhole() });
            else
            {
                _lastSoundhole = Design.Soundhole;
                Apply(d => d with { Soundhole = null });
            }
            var soundhole = Design.Soundhole;
            _showing = true;
            _soundhole.Show(soundhole, _units);
            _showing = false;
            _soundhole.Enabled = soundhole != null;
        }
    }
}
