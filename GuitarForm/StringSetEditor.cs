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
    // Edits one string set: its manufacturer and name, and a collapsible group of fields for each string, with buttons
    // to add and remove strings. Each valid edit raises Changed with the set changed; text that can't be read is held as
    // an input error (shown in Messages with the set's own validation messages) and leaves the set as it was. Fields
    // that don't apply to a string (the core settings of a plain string) are disabled, and keep their values.
    sealed class StringSetEditor : Panel
    {
        const string Section = "Strings";
        const string LbPerIn = "lb/in";
        const string KgPerM = "kg/m";

        // One string field: its parameter, whether it applies to the string, and how a slider steps (in mm for a
        // length, else in the parameter's own unit). The unit weight is typed in lb/in or kg/m but stored in kg/m, so
        // it has its own reading and showing.
        sealed record StringField(string Label, Func<GuitarString, Parameter> Get, Func<GuitarString, bool> Applies = null,
            double Step = 0.1, bool IsUnitWeight = false);

        static readonly StringField[] StringFields =
        {
            new("Description", s => s.Description),
            new("Diameter", s => s.Diameter, Step: 0.01),
            new("Construction", s => s.Construction),
            new("Unit Weight", s => s.UnitWeight, IsUnitWeight: true),
            new("Hex Core", s => s.HexCore, s => s.UsesHexCore),
            new("Core Material", s => s.CoreMaterial, s => s.UsesCoreMaterial),
            new("Core Modulus", s => s.CoreModulus, s => s.UsesCoreProperties, Step: 0.1),
            new("Core Strength", s => s.CoreStrength, s => s.UsesCoreProperties, Step: 1),
            new("Used in Saddle Compensation", s => s.UsedInSaddleCompensation),
        };

        // A box with the thing it edits: a field of the set (Index -1) or of one string.
        sealed class Entry
        {
            public int Index;
            public StringField Field;
            public Func<StringSet, Parameter> GetSetValue;
            public IFieldBox Box;
            public Label Unit;
        }

        readonly TabMessages _messages = new TabMessages();
        readonly DropDown _weightUnit = new DropDown { Width = UiMetrics.ChoiceBoxWidth };
        readonly FieldTextBox _manufacturer = new FieldTextBox { Width = UiMetrics.SliderMaxWidth };
        readonly FieldTextBox _name = new FieldTextBox { Width = UiMetrics.SliderMaxWidth };
        readonly FieldTextBox _count = new FieldTextBox { Width = UiMetrics.CountBoxWidth, ReadOnly = true };
        readonly StackLayout _stringGroups = new StackLayout
        {
            Spacing = UiMetrics.GroupGap,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
        };
        readonly Button _add = new Button { Text = "Add String" };
        readonly Dictionary<IFieldBox, string> _errors = new Dictionary<IFieldBox, string>();
        readonly List<Entry> _entries = new List<Entry>();
        readonly List<Button> _removeButtons = new List<Button>();
        StringSet _set = new StringSet();
        DisplayUnits _units = DisplayUnits.Current;
        bool _showing;
        bool _weightInLbPerIn = true;

        // The set's own fields, in the order they're shown.
        Entry _manufacturerEntry, _nameEntry;

        public StringSetEditor()
        {
            _weightUnit.Items.Add(LbPerIn);
            _weightUnit.Items.Add(KgPerM);
            _weightUnit.SelectedKey = LbPerIn;
            _weightUnit.SelectedIndexChanged += (_, _) =>
            {
                if (_showing)
                    return;
                bool lb = _weightUnit.SelectedKey != KgPerM;
                if (lb == _weightInLbPerIn)
                    return;
                _weightInLbPerIn = lb;
                RefreshUnitWeights();
                UnitWeightUnitChanged?.Invoke();
            };
            _add.Click += (_, _) =>
            {
                _set = _set.WithAddedString();
                OnStructureChanged();
            };

            _manufacturerEntry = SetEntry(_manufacturer, "Manufacturer", s => s.Manufacturer);
            _nameEntry = SetEntry(_name, "Name", s => s.Name);

            var setGroup = FieldLayout.Group(Section, "Set", new[]
            {
                SetRow(_manufacturerEntry, _manufacturer),
                SetRow(_nameEntry, _name),
                FieldLayout.Row("String Count", StringSet.Definitions.StringCount.Label, _count, FieldLayout.UnitLabel()),
            });
            var weightRow = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Spacing = UiMetrics.Gap,
                VerticalContentAlignment = VerticalAlignment.Center,
            };
            weightRow.Items.Add(new Label { Text = "Unit weight shown in", VerticalAlignment = VerticalAlignment.Center });
            weightRow.Items.Add(_weightUnit);

            var content = new StackLayout
            {
                Padding = new Padding(UiMetrics.PagePadding),
                Spacing = UiMetrics.GroupGap,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
            };
            content.Items.Add(weightRow);
            content.Items.Add(setGroup);
            content.Items.Add(_stringGroups);
            content.Items.Add(_add);
            Content = new Scrollable { Content = content, Border = BorderType.None };
        }

        // The set as last edited or shown.
        public StringSet Current => _set;

        // Raised after each valid edit, with the new set.
        public event Action<StringSet> Changed;

        // Raised when the user picks the other unit for the unit weight.
        public event Action UnitWeightUnitChanged;

        // The set's input errors and the messages from validating it.
        public TabMessages Messages => _messages;

        public bool HasInputErrors => _errors.Count > 0;

        // Whether the unit weight is shown and typed in lb/in (the default) rather than kg/m. It is stored in kg/m
        // either way, so changing this changes only what is shown.
        public bool UnitWeightInLbPerIn
        {
            get => _weightInLbPerIn;
            set
            {
                if (value == _weightInLbPerIn)
                    return;
                _weightInLbPerIn = value;
                bool showing = _showing;
                _showing = true;
                _weightUnit.SelectedKey = value ? LbPerIn : KgPerM;
                _showing = showing;
                RefreshUnitWeights();
            }
        }

        // Replaces the content with this set, lengths in the given units, and clears any input errors.
        public void Show(StringSet set, DisplayUnits units)
        {
            _set = set ?? new StringSet();
            _units = units;
            _errors.Clear();
            Rebuild();
            PushMessages();
        }

        // The document's units changed: shows the set again in the new units, without changing it.
        public void Refresh(DisplayUnits units)
        {
            _units = units;
            _errors.Clear();
            FillBoxes();
            PushMessages();
        }

        // --- Building ---

        Entry SetEntry(IFieldBox box, string label, Func<StringSet, Parameter> get)
        {
            var entry = new Entry { Index = -1, Field = new StringField(label, null), GetSetValue = get, Box = box };
            box.TextChanged += (_, _) => OnEdited(entry);
            return entry;
        }

        static TableRow SetRow(Entry entry, Control box) =>
            FieldLayout.Row(entry.Field.Label, entry.GetSetValue(new StringSet()).Label, box, FieldLayout.UnitLabel());

        // Builds the string groups afresh for the current set. Whether each group is expanded is kept by FieldLayout.
        void Rebuild()
        {
            _entries.RemoveAll(e => e.Index >= 0);
            _removeButtons.Clear();
            _stringGroups.Items.Clear();

            int count = _set.Strings.Count;
            for (int i = 0; i < count; i++)
            {
                int index = i;
                var rows = new List<TableRow>();
                foreach (var field in StringFields)
                {
                    var parameter = field.Get(new GuitarString());
                    IFieldBox box;
                    if (parameter.OptionTexts.Count > 0)
                        box = new FieldDropDown(parameter.OptionTexts);
                    else if (parameter.Definition.Kind == ParameterKind.Text)
                        box = new FieldTextBox { Width = UiMetrics.SliderMaxWidth };
                    else if (field.IsUnitWeight)
                        box = new FieldTextBox { Width = UiMetrics.ChoiceBoxWidth };
                    else
                        box = FieldLayout.Box(true, parameter.Label, field.Step);

                    var entry = new Entry { Index = index, Field = field, Box = box, Unit = FieldLayout.UnitLabel() };
                    box.TextChanged += (_, _) => OnEdited(entry);
                    _entries.Add(entry);
                    rows.Add(FieldLayout.Row(field.Label, parameter.Label, (Control)box, entry.Unit));
                }

                var remove = new Button { Text = "Remove String" };
                remove.Click += (_, _) =>
                {
                    if (_set.Strings.Count <= 1)
                        return;
                    _set = _set.WithRemovedString(index);
                    OnStructureChanged();
                };
                _removeButtons.Add(remove);
                rows.Add(new TableRow(new Panel(), new TableCell(remove, true), new Panel()));

                string position = count > 1 && i == 0 ? " (treble)" : count > 1 && i == count - 1 ? " (bass)" : "";
                _stringGroups.Items.Add(FieldLayout.Group(Section, $"String {i + 1}{position}", rows));
            }
            FillBoxes();
        }

        // A string was added or removed: rebuild, then tell the owner.
        void OnStructureChanged()
        {
            foreach (var entry in _entries.Where(e => e.Index >= 0))
                _errors.Remove(entry.Box);
            Rebuild();
            PushMessages();
            Changed?.Invoke(_set);
        }

        // --- Showing ---

        // Fills every box from the set, in the current units. Input errors are cleared by the caller.
        void FillBoxes()
        {
            _showing = true;
            _manufacturer.Text = _set.Manufacturer.Format(_units.LengthUnit, CultureInfo.CurrentCulture);
            _name.Text = _set.Name.Format(_units.LengthUnit, CultureInfo.CurrentCulture);
            _count.Text = _set.Strings.Count.ToString(CultureInfo.CurrentCulture);

            foreach (var entry in _entries.Where(e => e.Index >= 0))
            {
                var parameter = entry.Field.Get(_set.Strings[entry.Index]);
                if (entry.Field.IsUnitWeight)
                {
                    entry.Box.Text = FormatUnitWeight(((NumberParameter)parameter).Value);
                    entry.Unit.Text = WeightUnitText;
                }
                else
                {
                    entry.Box.Text = parameter.Format(_units.LengthUnit, CultureInfo.CurrentCulture);
                    entry.Unit.Text = parameter.Symbol(_units.LengthUnit);
                }
                if (entry.Box is NumberSlider slider)
                {
                    bool length = parameter.Definition.Kind == ParameterKind.Length;
                    slider.Step = length ? _units.FromMillimetres(entry.Field.Step) : entry.Field.Step;
                }
            }
            for (int i = 0; i < _removeButtons.Count; i++)
                _removeButtons[i].Enabled = _set.Strings.Count > 1;
            _showing = false;
            UpdateEnabled();
        }

        // Shows the unit weights again in the chosen unit. The stored values are untouched; a weight with text that
        // couldn't be read loses that text, and its input error.
        void RefreshUnitWeights()
        {
            bool showing = _showing;
            _showing = true;
            foreach (var entry in _entries.Where(e => e.Index >= 0 && e.Field.IsUnitWeight))
            {
                _errors.Remove(entry.Box);
                entry.Box.Text = FormatUnitWeight(_set.Strings[entry.Index].UnitWeight.Value);
                entry.Unit.Text = WeightUnitText;
            }
            _showing = showing;
            PushMessages();
        }

        string WeightUnitText => _weightInLbPerIn ? LbPerIn : KgPerM;

        // Enough digits for the stored resolution (1e-7 kg/m, about 5.6e-9 lb/in).
        string FormatUnitWeight(double? kgPerM) => kgPerM is double kg
            ? (_weightInLbPerIn ? StringUnitWeight.ToLbPerIn(kg) : kg).ToString(
                _weightInLbPerIn ? "0.#########" : "0.#######", CultureInfo.CurrentCulture)
            : "";

        // Disables the fields that don't apply to their string, without touching the values. A disabled field can't be
        // fixed, so its held input error is dropped and its text goes back to the stored value.
        void UpdateEnabled()
        {
            foreach (var entry in _entries.Where(e => e.Index >= 0 && e.Index < _set.Strings.Count))
            {
                bool applies = entry.Field.Applies?.Invoke(_set.Strings[entry.Index]) ?? true;
                entry.Box.Enabled = applies;
                if (!applies && _errors.Remove(entry.Box))
                {
                    bool showing = _showing;
                    _showing = true;
                    var parameter = entry.Field.Get(_set.Strings[entry.Index]);
                    entry.Box.Text = entry.Field.IsUnitWeight
                        ? FormatUnitWeight(((NumberParameter)parameter).Value)
                        : parameter.Format(_units.LengthUnit, CultureInfo.CurrentCulture);
                    _showing = showing;
                }
            }
        }

        // --- Editing ---

        void OnEdited(Entry entry)
        {
            if (_showing)
                return;

            Parameter current = entry.Index < 0 ? entry.GetSetValue(_set) : entry.Field.Get(_set.Strings[entry.Index]);
            if (!TryRead(entry, current, out var parsed, out string error))
            {
                _errors[entry.Box] = error;
                PushMessages();
                return;
            }
            _errors.Remove(entry.Box);

            if (parsed.Equals(current))
            {
                PushMessages();
                return;
            }
            if (entry.Index < 0)
                _set = (StringSet)_set.With(parsed);
            else
            {
                var strings = _set.Strings.ToList();
                strings[entry.Index] = (GuitarString)strings[entry.Index].With(parsed);
                _set = _set.WithStrings(strings);
            }
            UpdateEnabled();
            PushMessages();
            Changed?.Invoke(_set);
        }

        bool TryRead(Entry entry, Parameter current, out Parameter parsed, out string error)
        {
            string text = entry.Box.Text ?? "";
            if (!entry.Field.IsUnitWeight)
            {
                bool ok = current.TryParse(text, _units.LengthUnit, CultureInfo.CurrentCulture, out parsed, out var message);
                error = ok ? null : message.Text;
                return ok;
            }

            // Typed in lb/in or kg/m, stored in kg/m.
            var weight = (NumberParameter)current;
            parsed = weight;
            error = null;
            if (string.IsNullOrWhiteSpace(text))
            {
                parsed = weight with { Value = null };
                return true;
            }
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out double value) || !double.IsFinite(value))
            {
                error = $"{weight.Label}: \"{text.Trim()}\" isn't a number.";
                return false;
            }
            parsed = weight with { Value = _weightInLbPerIn ? StringUnitWeight.ToKgPerM(value) : value };
            return true;
        }

        // The model's messages for the set, after the input errors that stopped an edit.
        void PushMessages()
        {
            var messages = _errors.Values.Select(e => new GeometryMessage(MessageLevel.Error, e)).Concat(
                _set.Validate(_units.LengthUnit, CultureInfo.CurrentCulture).Select(m => new GeometryMessage(m.Level, m.Text)));
            _messages.Show(messages);
        }
    }
}
