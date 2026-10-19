using System;
using System.Collections.Generic;
using System.Globalization;
using Eto.Drawing;
using Eto.Forms;
using GuitarForm.Model;
using GuitarForm.Model.Parameters;
using GuitarForm.Model.Parts;

namespace GuitarForm
{
    // One field of GuitarFields and the editor control it owns: a FieldDropDown for a choice or yes/no value, a
    // NumberSlider for a decimal number (length, angle, number) and a plain box for a count. The panel builds every
    // tab's rows from these.
    //
    // Lengths are shown and typed in the document's units and stored in mm. Show fills the box from a Guitar. When the
    // user changes the box, Changed is raised; the owner then calls TryApply with the current Guitar to read the text
    // into an edited Guitar. Text that can't be read leaves Error set (and no new Guitar) until the box is fixed or
    // shown again.
    sealed class FieldBinding
    {
        readonly Label _unit = FieldLayout.UnitLabel();
        readonly TableRow _row;
        DisplayUnits _units = DisplayUnits.Current;
        bool _showing;
        bool _visible = true;

        public FieldBinding(GuitarField field)
        {
            Field = field;
            var parameter = field.Get(new Guitar());
            Box = field.IsChoice
                ? new FieldDropDown(parameter.OptionTexts)
                : FieldLayout.Box(field.IsDecimal, field.Tooltip, field.Step);
            Box.PlaceholderText = field.PlaceholderText();
            Box.TextChanged += (_, _) =>
            {
                if (!_showing)
                    Changed?.Invoke(this);
            };
            _row = FieldLayout.Row(field.Label, field.Tooltip, (Control)Box, _unit, () => Limits(field.Definition));
        }

        public GuitarField Field { get; }

        public IFieldBox Box { get; }

        // The field's row for a group: label, box and unit.
        public TableRow Row => _row;

        // Why the box's text couldn't be read, or null.
        public string Error { get; private set; }

        // True while the field is shown and its text couldn't be read. A hidden field's text can't be fixed by the
        // user, so it doesn't block saving or building.
        public bool HasBlockingError => Error != null && _visible;

        // The user changed the box (not Show). Call TryApply to read it.
        public event Action<FieldBinding> Changed;

        // Fills the box from the guitar, with lengths in the given units; clears Error and updates the field's state.
        public void Show(Guitar guitar, DisplayUnits units)
        {
            _units = units;
            var parameter = Field.Get(guitar);
            var definition = parameter.Definition;
            bool showing = _showing;
            _showing = true;
            Box.Text = parameter.Format(units.LengthUnit, CultureInfo.CurrentCulture);
            _unit.Text = parameter.Symbol(units.LengthUnit);
            // Drag, step and arrow keys stop at the limits, in the units shown. An exclusive limit can't be clamped to
            // (the value itself is not allowed), so it is left to the model's check of the value.
            if (Box is NumberSlider slider)
            {
                double Shown(double v) => definition.Kind == ParameterKind.Length ? units.FromMillimetres(v) : v;
                // Steps are defined in mm for a length, so they follow the document's units.
                slider.Step = Shown(Field.Step);
                slider.Minimum = definition.Minimum is double min && !definition.MinimumExclusive ? Shown(min) : null;
                slider.Maximum = definition.Maximum is double max && !definition.MaximumExclusive ? Shown(max) : null;
            }
            _showing = showing;
            Error = null;
            UpdateState(guitar);
        }

        // Reads the box into the guitar. True when the text was understood: edited is the guitar with the new value
        // (the same object when the value is unchanged) and Error is null. False leaves Error set and edited as the
        // guitar given.
        public bool TryApply(Guitar guitar, out Guitar edited)
        {
            edited = guitar;
            if (!Field.Get(guitar).TryParse(Box.Text, _units.LengthUnit, CultureInfo.CurrentCulture, out var parsed,
                    out var error))
            {
                Error = error.Text;
                return false;
            }
            Error = null;
            if (!Equals(parsed, Field.Get(guitar)))
                edited = Field.Set(guitar, parsed);
            return true;
        }

        // Shows or hides the row, enables or disables the box and refreshes the placeholder from the guitar's values
        // (e.g. squareness or blend by profile mode, the course spacings by strings per course, the soundhole's
        // values by whether it has one). A disabled field stays shown and keeps its value.
        public void UpdateState(Guitar guitar)
        {
            bool enabled = Field.IsEnabled(guitar);
            Box.Enabled = enabled;
            // The caption and unit gray out with the box (a label doesn't always do it by itself).
            var textColor = enabled ? SystemColors.ControlText : UiMetrics.DisabledColor;
            if (_row.Cells.Count > 0 && _row.Cells[0].Control is Label caption)
                caption.TextColor = textColor;
            _unit.TextColor = textColor;
            Box.PlaceholderText = Field.PlaceholderText();
            _visible = Field.IsVisible(guitar);
            foreach (var cell in _row.Cells)
                if (cell.Control != null)
                    cell.Control.Visible = _visible;
        }

        // "Minimum: …  Maximum: …" in the units shown; null when there are neither.
        string Limits(ParameterDefinition definition)
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
    }

    // The bindings for a table of groups: one per field, in the table's order, and a row list per group for the tab.
    sealed class FieldBindings
    {
        readonly Dictionary<GuitarField, FieldBinding> _byField = new(ReferenceEqualityComparer.Instance);

        public FieldBindings(IEnumerable<GuitarFieldGroup> groups)
        {
            foreach (var group in groups)
                foreach (var field in group.Fields)
                    _byField[field] = new FieldBinding(field);
        }

        public IEnumerable<FieldBinding> All => _byField.Values;

        public FieldBinding this[GuitarField field] => _byField[field];

        // The rows of a group, in order.
        public IEnumerable<TableRow> Rows(GuitarFieldGroup group)
        {
            foreach (var field in group.Fields)
                yield return _byField[field].Row;
        }

        public void Show(Guitar guitar, DisplayUnits units)
        {
            foreach (var binding in All)
                binding.Show(guitar, units);
        }

        public void UpdateState(Guitar guitar)
        {
            foreach (var binding in All)
                binding.UpdateState(guitar);
        }

        public bool HasBlockingError
        {
            get
            {
                foreach (var binding in All)
                    if (binding.HasBlockingError)
                        return true;
                return false;
            }
        }
    }
}
