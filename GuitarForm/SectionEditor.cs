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
    // A required number can't be left blank; a whole number can't have a fraction; a number with a Minimum (in mm for
    // a length) can't be less than it, and a slider stops there. Step is how much a slider changes the value per
    // step (in mm for a length, converted to the document's units when shown).
    sealed record NumberField<T>(
        string Label,
        bool IsLength,
        string Description,
        Func<T, double?> Get,
        Func<T, double?, T> Set,
        bool Required = false,
        bool Whole = false,
        double Step = 0.1,
        double? Minimum = null);

    // Related fields shown together in a box on the section's tab.
    sealed record FieldGroup<T>(string Title, params NumberField<T>[] Fields);

    // Number boxes for one section's values, in groups, and the section's messages. Each edit that gives a valid value
    // raises Changed with the section record changed; a blank required box or text that isn't a number is reported
    // in the messages and leaves the record as it was.
    sealed class SectionEditor<T> where T : class
    {
        readonly NumberField<T>[] _fields;
        readonly IFieldBox[] _boxes;
        readonly Label[] _unitLabels;
        readonly string[] _inputErrors;
        readonly Dictionary<string, List<TableRow>> _rows = new Dictionary<string, List<TableRow>>();
        readonly TabMessages _messages = new TabMessages();
        IReadOnlyList<GeometryMessage> _geometryMessages = Array.Empty<GeometryMessage>();
        T _value;
        DisplayUnits _units = DisplayUnits.Current;
        bool _showing;

        public event Action<T> Changed;

        // The group titles, in order.
        public IReadOnlyList<string> GroupTitles { get; }

        // The section's input errors and the messages from validating or drawing it.
        public TabMessages Messages => _messages;

        // Decimal numbers get a NumberSlider, whole numbers a plain box.
        public SectionEditor(IEnumerable<FieldGroup<T>> groups)
        {
            var groupList = groups.ToList();
            GroupTitles = groupList.Select(g => g.Title).ToList();
            _fields = groupList.SelectMany(g => g.Fields).ToArray();
            _boxes = new IFieldBox[_fields.Length];
            _unitLabels = new Label[_fields.Length];
            _inputErrors = new string[_fields.Length];

            int i = 0;
            foreach (var group in groupList)
            {
                var rows = _rows[group.Title] = new List<TableRow>();
                foreach (var field in group.Fields)
                {
                    int index = i++;
                    _boxes[index] = FieldLayout.Box(field.IsLength && !field.Whole, field.Description, field.Step);
                    _boxes[index].PlaceholderText = field.Required ? "required" : "";
                    _boxes[index].TextChanged += (_, _) => OnTextChanged(index);
                    _unitLabels[index] = FieldLayout.UnitLabel();
                    rows.Add(FieldLayout.Row(field.Label, field.Description, (Control)_boxes[index], _unitLabels[index],
                        () => field.Minimum is double minimum
                            ? "Minimum: " + (field.IsLength ? _units.FormatLength(minimum) : Format(minimum)) : null));
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
                if (_boxes[i] is NumberSlider slider)
                {
                    // Steps are defined in mm for a length, so they follow the document's units.
                    slider.Step = field.IsLength ? units.FromMillimetres(field.Step) : field.Step;
                    if (field.Minimum is double minimum)
                        slider.Minimum = field.IsLength ? units.FromMillimetres(minimum) : minimum;
                }
                _inputErrors[i] = null;
            }
            _showing = false;
            ShowAllMessages();
        }

        // Whether any box holds text that couldn't be read (so the record is not what the boxes show).
        public bool HasInputErrors => _inputErrors.Any(e => e != null);

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
            else if (field.Minimum is double minimum && (field.IsLength ? _units.ToMillimetres(parsed) : parsed) < minimum)
                _inputErrors[index] = $"{field.Label} must be at least " +
                    (field.IsLength ? _units.FormatLength(minimum) : Format(minimum)) + ".";
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

        void ShowAllMessages() => _messages.Show(
            _inputErrors.Where(e => e != null).Select(e => new GeometryMessage(MessageLevel.Error, e)).Concat(_geometryMessages));

        static string Format(double value) => value.ToString("0.#####", CultureInfo.CurrentCulture);
    }

    // What the panel needs from a field's box, so a text box and a NumberSlider can be used alike.
    interface IFieldBox
    {
        string Text { get; set; }
        string PlaceholderText { get; set; }
        bool Enabled { get; set; }
        event EventHandler<EventArgs> TextChanged;
    }

    sealed class FieldTextBox : TextBox, IFieldBox
    {
    }

    // A box for a value picked from a list, e.g. a choice or yes/no. Its text is the picked option's.
    sealed class FieldDropDown : DropDown, IFieldBox
    {
        public FieldDropDown(IEnumerable<string> options)
        {
            Width = UiMetrics.ChoiceBoxWidth;
            foreach (string option in options)
                Items.Add(option);
            SelectedIndexChanged += (_, _) => TextChanged?.Invoke(this, EventArgs.Empty);
        }

        public string Text
        {
            get => SelectedKey ?? "";
            set => SelectedKey = value;
        }

        // Every option is a value, so there's nothing to show when none is picked.
        public string PlaceholderText { get; set; }

        public event EventHandler<EventArgs> TextChanged;
    }

    // A tab's errors, warnings and notes, which the panel shows beside the tabs while the tab is selected.
    sealed class TabMessages
    {
        public IReadOnlyList<GeometryMessage> Messages { get; private set; } = Array.Empty<GeometryMessage>();

        public event Action Changed;

        public void Show(IEnumerable<GeometryMessage> messages)
        {
            Messages = messages?.ToList() ?? new List<GeometryMessage>();
            Changed?.Invoke();
        }
    }

    // The pieces every tab's fields are laid out with, so the boxes line up from group to group and tab to tab.
    static class FieldLayout
    {
        // A box for a field's value: a NumberSlider for a decimal number when slider is true, else a text box.
        public static IFieldBox Box(bool slider, string toolTip = null, double step = 0.1) => slider
            ? new NumberSlider { ToolTip = toolTip, Step = step }
            : new FieldTextBox { Width = UiMetrics.CountBoxWidth, ToolTip = toolTip };

        // The unit after a box. Row gives its column at least UnitWidth, and more if the text needs it.
        public static Label UnitLabel() => new Label { VerticalAlignment = VerticalAlignment.Center };

        // A field's row: its label (a fixed column that wraps long text), its box (a slider fills the column; any other
        // box keeps its own width at the left of it) and the unit after it.
        public static TableRow Row(string label, string toolTip, Control box, Label unit, Func<string> limits = null)
        {
            var text = new Label
            {
                Text = label,
                ToolTip = toolTip,
                VerticalAlignment = VerticalAlignment.Center,
                Wrap = WrapMode.Word,
                Width = UiMetrics.LabelMinWidth,
            };
            // The same tooltip on the label and the box: the description, the unit and any limits.
            void RefreshToolTip()
            {
                var lines = new List<string>();
                if (!string.IsNullOrEmpty(toolTip))
                    lines.Add(toolTip);
                if (!string.IsNullOrEmpty(unit.Text))
                    lines.Add("Unit: " + unit.Text);
                string range = limits?.Invoke();
                if (!string.IsNullOrEmpty(range))
                    lines.Add(range);
                string tip = lines.Count == 0 ? null : string.Join("\n", lines);
                text.ToolTip = tip;
                if (box is NumberSlider slider)
                    slider.ToolTip = tip;
                else
                    box.ToolTip = tip;
            }
            unit.TextChanged += (_, _) => RefreshToolTip();
            RefreshToolTip();
            Control cell = box;
            if (box is not NumberSlider)
            {
                var holder = new StackLayout { Orientation = Orientation.Horizontal, VerticalContentAlignment = VerticalAlignment.Center };
                holder.Items.Add(box);
                cell = holder;
            }
            var unitCell = new Panel { Content = unit, MinimumSize = new Size(UiMetrics.UnitWidth, 0) };
            return new TableRow(text, new TableCell(cell, true), unitCell);
        }

        // Keeps a wrapping label as wide as the scrolling area it is in (less padding and a scroll bar), because a
        // wrapping label in a scroller can otherwise measure itself unbounded and never wrap.
        public static void WrapToViewport(Scrollable scroll, int margin, Action<int> setWidth)
        {
            void Update()
            {
                int width = scroll.ClientSize.Width - margin - UiMetrics.ScrollbarWidth;
                if (scroll.ClientSize.Width > 0 && width > 0)
                    setWidth(width);
            }
            scroll.SizeChanged += (_, _) => Update();
            Update();
        }

        // Whether each group is expanded, by section and title, kept for the session.
        static readonly Dictionary<string, bool> GroupState = new Dictionary<string, bool>();

        // True for a group made with Group that holds a slider.
        public static bool HasSlider(Control group) => SliderGroups.TryGetValue(group, out _);

        // Weak, so it doesn't keep groups alive.
        static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Control, object> SliderGroups = new();

        // A titled group of related fields, with a header that collapses and expands it. Groups start expanded.
        public static Control Group(string section, string title, IEnumerable<TableRow> rows)
        {
            var layout = new TableLayout { Spacing = new Size(UiMetrics.Gap, UiMetrics.RowGap) };
            foreach (var row in rows)
                layout.Rows.Add(row);
            var body = new Panel { Padding = new Padding(UiMetrics.GroupPadding), Content = layout };
            var header = new Button { Text = "", Font = SystemFonts.Bold() };
            string key = section + "|" + title;
            void Update()
            {
                bool open = !GroupState.TryGetValue(key, out bool expanded) || expanded;
                header.Text = (open ? "v  " : ">  ") + title;
                body.Visible = open;
            }
            header.Click += (_, _) =>
            {
                GroupState[key] = !(!GroupState.TryGetValue(key, out bool expanded) || expanded);
                Update();
            };
            Update();
            var group = new StackLayout { Spacing = 0, HorizontalContentAlignment = HorizontalAlignment.Stretch };
            group.Items.Add(header);
            group.Items.Add(body);
            if (layout.Rows.Any(r => r.Cells.Any(c => c.Control is NumberSlider)))
                SliderGroups.Add(group, new object());
            return group;
        }

        public static string Level(MessageLevel level) => level switch
        {
            MessageLevel.Error => "Error",
            MessageLevel.Warning => "Warning",
            _ => "Note",
        };
    }
    // The selected tab's messages below the tabs: a header with the counts (click it to expand or collapse) and, when
    // expanded, one line per message, coloured by severity and prefixed with its level. Hidden when there are none,
    // and opened when errors appear.
    sealed class MessagesBar : StackLayout
    {
        const int MaxListHeight = 110;
        const int LineHeight = 20;

        readonly Button _header = new Button();
        readonly Scrollable _scroll = new Scrollable { Border = BorderType.None };
        readonly StackLayout _list = new StackLayout { Spacing = 2, Padding = new Padding(UiMetrics.Gap, 2), HorizontalContentAlignment = HorizontalAlignment.Stretch };
        IReadOnlyList<GeometryMessage> _messages = Array.Empty<GeometryMessage>();
        int _lineWidth;
        bool _expanded;
        int _errors;

        public MessagesBar()
        {
            Spacing = 2;
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            _scroll.Content = _list;
            FieldLayout.WrapToViewport(_scroll, 2 * UiMetrics.Gap, width =>
            {
                _lineWidth = width;
                foreach (var label in _list.Items.Select(i => i.Control).OfType<Label>())
                    label.Width = width;
            });
            _header.Click += (_, _) =>
            {
                _expanded = !_expanded;
                Refresh();
            };
            Items.Add(_header);
            Items.Add(_scroll);
            Visible = false;
        }

        public void Show(IReadOnlyList<GeometryMessage> messages)
        {
            _messages = messages ?? Array.Empty<GeometryMessage>();
            int errors = _messages.Count(m => m.Level == MessageLevel.Error);
            if (errors > 0 && _errors == 0)
                _expanded = true;
            _errors = errors;
            Refresh();
        }

        void Refresh()
        {
            Visible = _messages.Count > 0;
            if (!Visible)
                return;

            int warnings = _messages.Count(m => m.Level == MessageLevel.Warning);
            int notes = _messages.Count - _errors - warnings;
            var counts = new List<string>();
            if (_errors > 0)
                counts.Add(_errors == 1 ? "1 error" : $"{_errors} errors");
            if (warnings > 0)
                counts.Add(warnings == 1 ? "1 warning" : $"{warnings} warnings");
            if (notes > 0)
                counts.Add(notes == 1 ? "1 note" : $"{notes} notes");
            _header.Text = $"{(_expanded ? "v" : ">")} Messages: {string.Join(", ", counts)}";

            _list.Items.Clear();
            foreach (var message in _messages)
            {
                _list.Items.Add(new Label
                {
                    Text = $"{FieldLayout.Level(message.Level)}: {message.Text}",
                    Wrap = WrapMode.Word,
                    Width = _lineWidth > 0 ? _lineWidth : -1,
                    TextColor = message.Level switch
                    {
                        MessageLevel.Error => UiMetrics.ErrorColor,
                        MessageLevel.Warning => UiMetrics.WarningColor,
                        _ => UiMetrics.NoteColor,
                    },
                });
            }
            _scroll.Visible = _expanded;
            _scroll.Height = Math.Min(UiMetrics.Scale(MaxListHeight), _messages.Count * UiMetrics.Scale(LineHeight) + UiMetrics.Scale(8));
        }
    }
}
