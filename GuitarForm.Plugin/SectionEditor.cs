using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using GuitarForm.Model;

namespace GuitarForm.Plugin
{
    // One number in a section: how to read it from the section record and how to make a record with it changed.
    // A required number can't be left blank; a whole number can't have a fraction.
    sealed record NumberField<T>(
        string Label,
        string Unit,
        string Description,
        Func<T, double?> Get,
        Func<T, double?, T> Set,
        bool Required = false,
        bool Whole = false);

    // Number boxes for one section's values, and the section's messages below them. Each edit that gives a valid value
    // raises Changed with the section record changed; a blank required box or text that isn't a number is reported
    // under the boxes and leaves the record as it was.
    sealed class SectionEditor<T> where T : class
    {
        readonly NumberField<T>[] _fields;
        readonly TextBox[] _boxes;
        readonly string[] _inputErrors;
        readonly Label _messages = new Label { Wrap = WrapMode.Word };
        IReadOnlyList<GeometryMessage> _geometryMessages = Array.Empty<GeometryMessage>();
        T _value;
        bool _showing;

        public event Action<T> Changed;

        public Control Control { get; }

        public SectionEditor(IEnumerable<NumberField<T>> fields)
        {
            _fields = fields.ToArray();
            _boxes = new TextBox[_fields.Length];
            _inputErrors = new string[_fields.Length];

            var layout = new TableLayout { Spacing = new Size(6, 4) };
            for (int i = 0; i < _fields.Length; i++)
            {
                int index = i;
                var field = _fields[i];
                _boxes[i] = new TextBox
                {
                    Width = 90,
                    ToolTip = field.Description,
                    PlaceholderText = field.Required ? "required" : "",
                };
                _boxes[i].TextChanged += (_, _) => OnTextChanged(index);
                layout.Rows.Add(new TableRow(
                    new TableCell(new Label { Text = field.Label, ToolTip = field.Description, VerticalAlignment = VerticalAlignment.Center }, true),
                    _boxes[i],
                    new Label { Text = field.Unit, VerticalAlignment = VerticalAlignment.Center }));
            }

            Control = new StackLayout
            {
                Spacing = 8,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Items = { layout, _messages },
            };
        }

        public bool Enabled
        {
            get => _boxes.FirstOrDefault()?.Enabled ?? true;
            set
            {
                foreach (var box in _boxes)
                    box.Enabled = value;
            }
        }

        // Fills the boxes from the record (null leaves them all blank) and clears any input errors.
        public void Show(T value)
        {
            _value = value;
            _showing = true;
            for (int i = 0; i < _fields.Length; i++)
            {
                _boxes[i].Text = value == null ? "" : Format(_fields[i].Get(value));
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
                number = parsed;

            ShowAllMessages();
            if (_inputErrors[index] != null)
                return;

            var changed = field.Set(_value, number);
            if (Equals(changed, _value))
                return;
            _value = changed;
            Changed?.Invoke(changed);
        }

        void ShowAllMessages()
        {
            var lines = _inputErrors.Where(e => e != null).Select(e => "Error: " + e)
                .Concat(_geometryMessages.Select(m => $"{Level(m.Level)}: {m.Text}"))
                .ToList();
            _messages.Text = string.Join("\n", lines);
            bool anyError = _inputErrors.Any(e => e != null) || _geometryMessages.Any(m => m.Level == MessageLevel.Error);
            _messages.TextColor = anyError ? Colors.Red : SystemColors.ControlText;
        }

        static string Level(MessageLevel level) => level switch
        {
            MessageLevel.Error => "Error",
            MessageLevel.Warning => "Warning",
            _ => "Note",
        };

        static string Format(double? value) =>
            value?.ToString("0.#####", CultureInfo.CurrentCulture) ?? "";
    }
}
