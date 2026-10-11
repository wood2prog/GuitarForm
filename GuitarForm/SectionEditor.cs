using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using GuitarForm.Model;

namespace GuitarForm
{
    // One number in a section: how to read it from the section record and how to make a record with it changed.
    // A length is stored in mm and shown in the document's units; other numbers (counts) are shown as they are.
    // A required number can't be left blank; a whole number can't have a fraction.
    sealed record NumberField<T>(
        string Label,
        bool IsLength,
        string Description,
        Func<T, double?> Get,
        Func<T, double?, T> Set,
        bool Required = false,
        bool Whole = false);

    // Related fields shown together in a box on the section's tab.
    sealed record FieldGroup<T>(string Title, params NumberField<T>[] Fields);

    // Number boxes for one section's values, in groups, and the section's messages. Each edit that gives a valid value
    // raises Changed with the section record changed; a blank required box or text that isn't a number is reported
    // in the messages and leaves the record as it was.
    sealed class SectionEditor<T> where T : class
    {
        readonly NumberField<T>[] _fields;
        readonly TextBox[] _boxes;
        readonly Label[] _unitLabels;
        readonly string[] _inputErrors;
        readonly Dictionary<string, List<TableRow>> _rows = new Dictionary<string, List<TableRow>>();
        readonly Label _messages = FieldLayout.MessageLabel();
        IReadOnlyList<GeometryMessage> _geometryMessages = Array.Empty<GeometryMessage>();
        T _value;
        DisplayUnits _units = DisplayUnits.Current;
        bool _showing;

        public event Action<T> Changed;

        // The group titles, in order.
        public IReadOnlyList<string> GroupTitles { get; }

        // The section's input errors and the messages from validating or drawing it.
        public Control Messages => _messages;

        public SectionEditor(IEnumerable<FieldGroup<T>> groups)
        {
            var groupList = groups.ToList();
            GroupTitles = groupList.Select(g => g.Title).ToList();
            _fields = groupList.SelectMany(g => g.Fields).ToArray();
            _boxes = new TextBox[_fields.Length];
            _unitLabels = new Label[_fields.Length];
            _inputErrors = new string[_fields.Length];

            int i = 0;
            foreach (var group in groupList)
            {
                var rows = _rows[group.Title] = new List<TableRow>();
                foreach (var field in group.Fields)
                {
                    int index = i++;
                    _boxes[index] = FieldLayout.Box(field.Description);
                    _boxes[index].PlaceholderText = field.Required ? "required" : "";
                    _boxes[index].TextChanged += (_, _) => OnTextChanged(index);
                    _unitLabels[index] = FieldLayout.UnitLabel();
                    rows.Add(FieldLayout.Row(field.Label, field.Description, _boxes[index], _unitLabels[index]));
                }
            }
        }

        // The rows of the group with this title; none if the section has no such group.
        public IEnumerable<TableRow> Rows(string title) =>
            _rows.TryGetValue(title, out var rows) ? rows : Enumerable.Empty<TableRow>();

        public bool Enabled
        {
            get => _boxes.FirstOrDefault()?.Enabled ?? true;
            set
            {
                foreach (var box in _boxes)
                    box.Enabled = value;
            }
        }

        // Fills the boxes from the record (null leaves them all blank), with lengths in the given units, and clears any
        // input errors.
        public void Show(T value, DisplayUnits units)
        {
            _value = value;
            _units = units;
            _showing = true;
            for (int i = 0; i < _fields.Length; i++)
            {
                var field = _fields[i];
                double? number = value == null ? null : field.Get(value);
                _boxes[i].Text = number == null ? "" : field.IsLength ? units.FormatNumber(number.Value) : Format(number.Value);
                _unitLabels[i].Text = field.IsLength ? units.Abbreviation : "";
                _inputErrors[i] = null;
            }
            _showing = false;
            ShowAllMessages();
        }

        // The messages from validating or drawing the section.
        public void ShowMessages(IReadOnlyList<GeometryMessage> messages)
        {
            _geometryMessages = messages ?? Array.Empty<GeometryMessage>();
            ShowAllMessages();
        }

        void OnTextChanged(int index)
        {
            if (_showing || _value == null)
                return;

            var field = _fields[index];
            string text = _boxes[index].Text.Trim();
            _inputErrors[index] = null;

            double? number = null;
            if (text.Length == 0)
            {
                if (field.Required)
                    _inputErrors[index] = $"{field.Label} is required.";
            }
            else if (!double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out double parsed) ||
                     !double.IsFinite(parsed))
                _inputErrors[index] = $"{field.Label} isn't a number.";
            else if (field.Whole && parsed != Math.Round(parsed))
                _inputErrors[index] = $"{field.Label} must be a whole number.";
            else
                number = field.IsLength ? _units.ToMillimetres(parsed) : parsed;

            ShowAllMessages();
            if (_inputErrors[index] != null)
                return;

            var changed = field.Set(_value, number);
            if (Equals(changed, _value))
                return;
            _value = changed;
            Changed?.Invoke(changed);
        }

        void ShowAllMessages() => FieldLayout.ShowMessages(_messages,
            _inputErrors.Where(e => e != null).Select(e => new GeometryMessage(MessageLevel.Error, e)).Concat(_geometryMessages));

        static string Format(double value) => value.ToString("0.#####", CultureInfo.CurrentCulture);
    }

    // The pieces every tab's fields are laid out with, so the boxes line up from group to group and tab to tab.
    static class FieldLayout
    {
        public static TextBox Box(string toolTip = null) => new TextBox { Width = 90, ToolTip = toolTip };

        public static Label UnitLabel() => new Label { Width = 24, VerticalAlignment = VerticalAlignment.Center };

        // A field's label, its box and the unit after it.
        public static TableRow Row(string label, string toolTip, Control box, Label unit) => new TableRow(
            new TableCell(new Label { Text = label, ToolTip = toolTip, VerticalAlignment = VerticalAlignment.Center }, true),
            box,
            unit);

        // A titled box holding related fields.
        public static GroupBox Group(string title, IEnumerable<TableRow> rows)
        {
            var layout = new TableLayout { Spacing = new Size(6, 4) };
            foreach (var row in rows)
                layout.Rows.Add(row);
            return new GroupBox { Text = title, Padding = new Padding(6), Content = layout };
        }

        public static Label MessageLabel() => new Label { Wrap = WrapMode.Word };

        // Messages one per line, red when any of them is an error.
        public static void ShowMessages(Label label, IEnumerable<GeometryMessage> messages)
        {
            var list = messages.ToList();
            label.Text = string.Join("\n", list.Select(m => $"{Level(m.Level)}: {m.Text}"));
            label.TextColor = list.Any(m => m.Level == MessageLevel.Error) ? Colors.Red : SystemColors.ControlText;
        }

        static string Level(MessageLevel level) => level switch
        {
            MessageLevel.Error => "Error",
            MessageLevel.Warning => "Warning",
            _ => "Note",
        };
    }
}
