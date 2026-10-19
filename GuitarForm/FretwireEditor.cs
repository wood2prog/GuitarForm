using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using GuitarForm.Model;
using GuitarForm.Model.Parameters;
using GuitarForm.Model.Parts;

namespace GuitarForm
{
    // Edits one fretwire: its manufacturer (optional), product code, the profile dimensions, slot depth and an optional
    // price. Each valid edit raises Changed with the fretwire changed; text that can't be read is held as an input
    // error (shown in Messages with the fretwire's own validation messages) and leaves the fretwire as it was.
    sealed class FretwireEditor : Panel
    {
        const string Section = "Fretwire";

        // One field: its parameter and how a slider steps (in mm for a length, else in the parameter's own unit).
        sealed record Field(string Label, Func<Fretwire, Parameter> Get, double Step = 0.01);

        static readonly Field[] Fields =
        {
            new("Manufacturer", f => f.Manufacturer),
            new("Product Code", f => f.ProductCode),
            new("Bead Height", f => f.BeadHeight),
            new("Bead Width", f => f.BeadWidth),
            new("Tang Height", f => f.TangHeight),
            new("Tang Thickness", f => f.TangThickness),
            new("Barb Thickness", f => f.BarbThickness),
            new("Slot Depth", f => f.SlotDepth, Step: 0.1),
            new("Price", f => f.Price),
        };

        sealed class Entry
        {
            public Field Field;
            public IFieldBox Box;
            public Label Unit;
        }

        readonly TabMessages _messages = new TabMessages();
        readonly Dictionary<IFieldBox, string> _errors = new Dictionary<IFieldBox, string>();
        readonly List<Entry> _entries = new List<Entry>();
        Fretwire _item = new Fretwire();
        DisplayUnits _units = DisplayUnits.Current;
        bool _showing;

        public FretwireEditor()
        {
            var rows = new List<TableRow>();
            foreach (var field in Fields)
            {
                var parameter = field.Get(new Fretwire());
                IFieldBox box;
                if (parameter.Definition.Kind == ParameterKind.Text)
                    box = new FieldTextBox { Width = UiMetrics.SliderMaxWidth, ToolTip = parameter.Label };
                else if (parameter.Definition.Kind == ParameterKind.Length)
                    box = FieldLayout.Box(true, parameter.Label, field.Step);
                else
                    box = FieldLayout.Box(false, parameter.Label);

                var entry = new Entry { Field = field, Box = box, Unit = FieldLayout.UnitLabel() };
                var definition = parameter.Definition;
                box.TextChanged += (_, _) => OnEdited(entry);
                _entries.Add(entry);
                rows.Add(FieldLayout.Row(field.Label, parameter.Label, (Control)box, entry.Unit, () => Limits(definition)));
            }

            var content = new StackLayout
            {
                Padding = new Padding(UiMetrics.PagePadding),
                Spacing = UiMetrics.GroupGap,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
            };
            content.Items.Add(FieldLayout.Group(Section, "Fretwire", rows));
            Content = new Scrollable { Content = content, Border = BorderType.None };
        }

        // The fretwire as last edited or shown.
        public Fretwire Current => _item;

        // Raised after each valid edit, with the new fretwire.
        public event Action<Fretwire> Changed;

        // The fretwire's input errors and the messages from validating it.
        public TabMessages Messages => _messages;

        public bool HasInputErrors => _errors.Count > 0;

        // Replaces the content with this fretwire, lengths in the given units, and clears any input errors.
        public void Show(Fretwire item, DisplayUnits units)
        {
            _item = item ?? new Fretwire();
            _units = units;
            _errors.Clear();
            FillBoxes();
            PushMessages();
        }

        // The document's units changed: shows the fretwire again in the new units, without changing it.
        public void Refresh(DisplayUnits units)
        {
            _units = units;
            _errors.Clear();
            FillBoxes();
            PushMessages();
        }

        // "Minimum: …  Maximum: …" in the units shown; null when the parameter has neither.
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

        // Fills every box from the fretwire, in the current units. Input errors are cleared by the caller.
        void FillBoxes()
        {
            bool showing = _showing;
            _showing = true;
            foreach (var entry in _entries)
            {
                var parameter = entry.Field.Get(_item);
                entry.Box.Text = parameter.Format(_units.LengthUnit, CultureInfo.CurrentCulture);
                entry.Unit.Text = parameter.Symbol(_units.LengthUnit);
                // Drag, step and arrow keys stop at the limits, in the units shown. An exclusive limit can't be
                // clamped to, so it is left to the typed-value check.
                if (entry.Box is NumberSlider slider)
                {
                    var definition = parameter.Definition;
                    double Shown(double v) => definition.Kind == ParameterKind.Length ? _units.FromMillimetres(v) : v;
                    slider.Step = Shown(entry.Field.Step);
                    slider.Minimum = definition.Minimum is double min && !definition.MinimumExclusive ? Shown(min) : null;
                    slider.Maximum = definition.Maximum is double max && !definition.MaximumExclusive ? Shown(max) : null;
                }
            }
            _showing = showing;
        }

        void OnEdited(Entry entry)
        {
            if (_showing)
                return;

            var current = entry.Field.Get(_item);
            if (!current.TryParse(entry.Box.Text ?? "", _units.LengthUnit, CultureInfo.CurrentCulture, out var parsed, out var message))
            {
                _errors[entry.Box] = message.Text;
                PushMessages();
                return;
            }
            _errors.Remove(entry.Box);

            if (parsed.Equals(current))
            {
                PushMessages();
                return;
            }
            _item = (Fretwire)_item.With(parsed);
            PushMessages();
            Changed?.Invoke(_item);
        }

        // The model's messages for the fretwire, after the input errors that stopped an edit.
        void PushMessages()
        {
            var messages = _errors.Values.Select(e => new GeometryMessage(MessageLevel.Error, e)).Concat(
                _item.Validate(_units.LengthUnit, CultureInfo.CurrentCulture).Select(m => new GeometryMessage(m.Level, m.Text)));
            _messages.Show(messages);
        }
    }
}
