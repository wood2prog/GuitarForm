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
    // The dockable GuitarForm panel: the list of designs, a tab for each section of the open design, the Settings tab,
    // and Preview / Build. The first edit after a design is opened asks whether to overwrite it or save the change as a
    // copy; after that every edit is saved as it's made. The library is opened for each operation and closed again, so
    // the file is free for backing up, restoring and moving.
    [Guid("b27065af-ab08-4c16-aeef-415d7616b93c")]
    public class GuitarFormPanel : Panel, IPanel
    {
        readonly DropDown _designs = new DropDown();
        readonly SectionEditor<Instrument> _instrument = new SectionEditor<Instrument>(Sections.Instrument);
        readonly SectionEditor<Body> _body = new SectionEditor<Body>(Sections.Body);
        readonly SectionEditor<Soundhole> _soundhole = new SectionEditor<Soundhole>(Sections.Soundhole);
        readonly CheckBox _hasSoundhole = new CheckBox { Text = "This design has a soundhole" };
        readonly CheckBox _showPreview = new CheckBox { Text = "Preview", Checked = true };
        readonly SettingsPage _settings = new SettingsPage();
        readonly DesignPreview _preview = new DesignPreview();

        // Values the old sections don't have, drawn with GuitarForm.Shapes (see NewFields): on the Body tab, and on the
        // Bracing, Fretboard and Neck tabs, each with its own messages.
        static readonly string[] NewFieldTabs = NewFields.Groups.SelectMany(g => g.Fields.Select(_ => g.Tab)).ToArray();
        // Decimal numbers (lengths, angles, numbers) are sliders; counts, flags and choices are plain boxes.
        readonly IFieldBox[] _newFieldBoxes = NewFields.Fields.Select((field, i) => FieldLayout.Box(
            IsDecimal(field.Get(new Guitar()).Definition.Kind), step: field.Step)).ToArray();
        readonly Label[] _newFieldUnits = NewFields.Fields.Select(_ => FieldLayout.UnitLabel()).ToArray();
        readonly string[] _newFieldErrors = new string[NewFields.Fields.Length];
        readonly TextBox _markers = new TextBox { Width = 160, ToolTip = "Each marker's fret, with :2 for two dots, e.g. 3 5 7 9 12:2" };
        string _markersError;
        readonly Dictionary<string, TabMessages> _tabMessages = new[] { NewFields.BracingTab, NewFields.FretboardTab, NewFields.NeckTab }
            .ToDictionary(tab => tab, _ => new TabMessages());

        // The selected tab's messages, in a box to the right of the tabs; the divider between them can be dragged.
        readonly TabControl _tabs = new TabControl();
        readonly Dictionary<TabPage, TabMessages> _pageMessages = new Dictionary<TabPage, TabMessages>();
        readonly TextArea _messageBox = new TextArea { ReadOnly = true, Wrap = true };
        const string SplitterPositionKey = "PanelSplitterPosition";
        Guitar _newFields = new Guitar();
        Dictionary<string, List<GeometryMessage>> _newTabMessages = new Dictionary<string, List<GeometryMessage>>();

        // Controls that only work with a design open, and the preset lists to refresh when the library changes.
        readonly List<Control> _needDesign = new List<Control>();
        readonly List<Action> _refreshPresets = new List<Action>();

        long? _designId;
        GuitarDesign _design;
        DisplayUnits _units = DisplayUnits.Current;
        bool _editConfirmed;
        bool _showing;
        bool _panelVisible = true;

        // The soundhole a design had before its box was unticked, so ticking it again brings the values back.
        Soundhole _lastSoundhole;

        static GuitarFormPlugin Plugin => GuitarFormPlugin.Instance;

        public GuitarFormPanel()
        {
            _instrument.Changed += instrument => Apply(d => d with { Instrument = instrument });
            _body.Changed += body => Apply(d => d with { Body = body });
            _soundhole.Changed += soundhole => Apply(d => d with { Soundhole = soundhole });
            _hasSoundhole.CheckedChanged += (_, _) => OnHasSoundholeChanged();
            _designs.SelectedIndexChanged += (_, _) => OnDesignSelected();
            _showPreview.CheckedChanged += (_, _) => Redraw();
            _settings.DrawingChanged += Redraw;

            // Lengths are shown in the active document's units, which change with its properties or another document.
            RhinoDoc.DocumentPropertiesChanged += (_, _) => Application.Instance.AsyncInvoke(OnUnitsMaybeChanged);
            RhinoDoc.EndOpenDocument += (_, _) => Application.Instance.AsyncInvoke(OnUnitsMaybeChanged);
            RhinoDoc.NewDocument += (_, _) => Application.Instance.AsyncInvoke(OnUnitsMaybeChanged);
            _settings.LibraryChanged += () =>
            {
                CloseDesign();
                ShowDesignList(null);
                foreach (var refresh in _refreshPresets)
                    refresh();
            };

            for (int i = 0; i < NewFields.Fields.Length; i++)
            {
                int index = i;
                _newFieldBoxes[i].TextChanged += (_, _) => OnNewFieldChanged(index);
            }
            _markers.TextChanged += (_, _) => OnMarkersChanged();

            var tabs = _tabs;
            tabs.Pages.Add(SectionPage("Instrument",
                new Control[] { PresetRow<Instrument>(() => _design?.Instrument, preset => Apply(d => d with { Instrument = preset })) },
                Groups(null, _instrument.GroupTitles, _instrument.Rows),
                _instrument.Messages));
            tabs.Pages.Add(SectionPage("Body",
                new Control[] { PresetRow<Body>(() => _design?.Body, preset => Apply(d => d with { Body = preset })) },
                Groups(NewFields.BodyTab, _body.GroupTitles, _body.Rows),
                _body.Messages));
            tabs.Pages.Add(SectionPage("Soundhole",
                new Control[]
                {
                    _hasSoundhole,
                    PresetRow<Soundhole>(() => _design?.Soundhole, preset => Apply(d => d with { Soundhole = preset })),
                },
                Groups(null, _soundhole.GroupTitles, _soundhole.Rows),
                _soundhole.Messages));
            foreach (string tab in _tabMessages.Keys)
                tabs.Pages.Add(SectionPage(tab, Array.Empty<Control>(), Groups(tab), _tabMessages[tab]));
            tabs.Pages.Add(new TabPage { Text = "Settings", Content = new Scrollable { Content = _settings, Border = BorderType.None } });

            Content = new TableLayout
            {
                Padding = new Padding(6),
                Spacing = new Size(6, 6),
                Rows =
                {
                    // In its own layout, so the outer one has a single column and the tabs get its full width.
                    new TableLayout
                    {
                        Spacing = new Size(6, 0),
                        Rows = { new TableRow(new Label { Text = "Design", VerticalAlignment = VerticalAlignment.Center }, new TableCell(_designs, true)) },
                    },
                    Buttons(
                        ActionButton("New…", NewDesign),
                        NeedsDesign(ActionButton("Copy", CopyDesign)),
                        NeedsDesign(ActionButton("Rename…", RenameDesign)),
                        NeedsDesign(ActionButton("Delete", DeleteDesign))),
                    Buttons(
                        ActionButton("Import…", ImportDesign),
                        NeedsDesign(ActionButton("Export…", ExportDesign))),
                    new TableRow(TabsAndMessages()) { ScaleHeight = true },
                    new TableLayout
                    {
                        Spacing = new Size(6, 0),
                        Rows = { new TableRow(new TableCell(_showPreview, true), NeedsDesign(ActionButton("Build", Build))) },
                    },
                },
            };

            ShowDesignList(null);
            ShowDesign();
        }

        // --- Layout ---

        // A tab: the controls at its top (preset row, …) and its groups of fields. Its messages are shown beside the tabs.
        TabPage SectionPage(string title, Control[] top, IEnumerable<Control> groups, TabMessages messages)
        {
            var content = new StackLayout { Padding = 10, Spacing = 10, HorizontalContentAlignment = HorizontalAlignment.Stretch };
            foreach (var control in top.Concat(groups))
                content.Items.Add(control);
            NeedsDesign(content);
            var page = new TabPage { Text = title, Content = new Scrollable { Content = content, Border = BorderType.None } };
            _pageMessages[page] = messages;
            messages.Changed += () =>
            {
                if (_tabs.SelectedPage == page)
                    ShowSelectedMessages();
            };
            return page;
        }

        // The tabs on the left and the selected tab's messages on the right, with a divider that remembers where it
        // was dragged to.
        Control TabsAndMessages()
        {
            _tabs.SelectedIndexChanged += (_, _) => ShowSelectedMessages();
            var splitter = new Splitter
            {
                Orientation = Orientation.Horizontal,
                FixedPanel = SplitterFixedPanel.Panel1,
                Position = Plugin.Settings.GetInteger(SplitterPositionKey, 430),
                Panel1MinimumSize = 200,
                Panel2MinimumSize = 80,
                Panel1 = _tabs,
                Panel2 = new TableLayout
                {
                    Padding = new Padding(6, 0, 0, 0),
                    Spacing = new Size(4, 4),
                    Rows =
                    {
                        new Label { Text = "Messages" },
                        new TableRow(_messageBox) { ScaleHeight = true },
                    },
                },
            };
            splitter.PositionChanged += (_, _) => Plugin.Settings.SetInteger(SplitterPositionKey, splitter.Position);
            return splitter;
        }

        void ShowSelectedMessages() => FieldLayout.ShowMessages(_messageBox,
            _tabs.SelectedPage != null && _pageMessages.TryGetValue(_tabs.SelectedPage, out var messages)
                ? messages.Messages : Array.Empty<GeometryMessage>());

        // A tab's groups: the old section's groups, each with any new fields of the same title added after its own,
        // then the tab's other new-field groups.
        IEnumerable<Control> Groups(string tab, IReadOnlyList<string> oldTitles = null, Func<string, IEnumerable<TableRow>> oldRows = null)
        {
            oldTitles ??= Array.Empty<string>();
            var titles = oldTitles.Concat(NewFields.Groups.Where(g => g.Tab == tab).Select(g => g.Title)).Distinct();
            return titles.Select(title => FieldLayout.Group(title,
                (oldRows?.Invoke(title) ?? Enumerable.Empty<TableRow>()).Concat(NewFieldRows(tab, title)))).ToList();
        }

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
                        yield return FieldLayout.Row(field.Get(_newFields).Label, null, (Control)_newFieldBoxes[index], _newFieldUnits[index]);
                    index++;
                }
            }
            if (tab == NewFields.FretboardTab && title == NewFields.MarkersGroup)
                yield return FieldLayout.Row("Marker frets", _markers.ToolTip, _markers, FieldLayout.UnitLabel());
        }

        void OnMarkersChanged()
        {
            if (_showing || _designId == null)
                return;
            var changed = NewFields.WithMarkers(_newFields, _markers.Text);
            _markersError = changed == null ? "Dot markers: write each marker's fret, with :2 for two dots, e.g. 3 5 7 9 12:2." : null;
            if (changed != null)
            {
                _newFields = changed;
                Run(() => NewFields.Save(_designId.Value, _newFields));
            }
            Redraw();
        }

        void OnNewFieldChanged(int index)
        {
            if (_showing || _designId == null)
                return;
            var field = NewFields.Fields[index];
            if (field.Get(_newFields).TryParse(_newFieldBoxes[index].Text, _units.LengthUnit, CultureInfo.CurrentCulture,
                    out var parsed, out var error))
            {
                _newFieldErrors[index] = null;
                _newFields = field.Set(_newFields, parsed);
                Run(() => NewFields.Save(_designId.Value, _newFields));
            }
            else
                _newFieldErrors[index] = error.Text;
            Redraw();
        }

        static Control Buttons(params Control[] buttons)
        {
            var row = new StackLayout { Orientation = Orientation.Horizontal, Spacing = 6 };
            foreach (var button in buttons)
                row.Items.Add(button);
            return row;
        }

        Control NeedsDesign(Control control)
        {
            _needDesign.Add(control);
            return control;
        }

        // A button whose action's errors are shown in a message box.
        Button ActionButton(string text, Action action)
        {
            var button = new Button { Text = text };
            button.Click += (_, _) => Run(action);
            return button;
        }

        void Run(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                MessageBox.Show(this, e.Message, "GuitarForm", MessageBoxType.Error);
            }
        }

        // A list of the section's presets, with Load (copies the preset's values into the design), Save as… and Delete.
        Control PresetRow<T>(Func<T> current, Action<T> load) where T : class
        {
            var presets = new DropDown();
            void Refresh()
            {
                presets.Items.Clear();
                try
                {
                    foreach (string name in WithLibrary(library => library.ListPresets<T>()))
                        presets.Items.Add(name);
                }
                catch (Exception e)
                {
                    RhinoApp.WriteLine($"GuitarForm: couldn't list the presets: {e.Message}");
                }
            }
            _refreshPresets.Add(Refresh);
            Refresh();

            var loadButton = ActionButton("Load", () =>
            {
                if (presets.SelectedKey is not string name)
                    throw new InvalidOperationException("Choose a preset to load.");
                load(WithLibrary(library => library.LoadPreset<T>(name)));
                ShowDesign();
            });
            var saveButton = ActionButton("Save as…", () =>
            {
                var section = current() ?? throw new InvalidOperationException("There's nothing to save as a preset.");
                var values = FieldsDialog.Ask(this, "Save as preset", new[] { ("Preset name", presets.SelectedKey ?? "") },
                    v => v[0].Length == 0 ? "Enter a name." : null);
                if (values == null)
                    return;
                string name = values[0];
                if (WithLibrary(library => library.PresetExists<T>(name)) &&
                    MessageBox.Show(this, $"Replace the preset \"{name}\"?", "GuitarForm", MessageBoxButtons.OKCancel, MessageBoxType.Question) != DialogResult.Ok)
                    return;
                WithLibrary(library => library.SavePreset(name, section));
                Refresh();
                presets.SelectedKey = name;
            });
            var deleteButton = ActionButton("Delete", () =>
            {
                if (presets.SelectedKey is not string name)
                    throw new InvalidOperationException("Choose a preset to delete.");
                if (MessageBox.Show(this, $"Delete the preset \"{name}\"?", "GuitarForm", MessageBoxButtons.OKCancel, MessageBoxType.Question) != DialogResult.Ok)
                    return;
                WithLibrary(library => library.DeletePreset<T>(name));
                Refresh();
            });

            return new TableLayout
            {
                Spacing = new Size(6, 4),
                Rows =
                {
                    new TableRow(new Label { Text = "Preset", VerticalAlignment = VerticalAlignment.Center }, new TableCell(presets, true)),
                    new TableRow(null, Buttons(loadButton, saveButton, deleteButton)),
                },
            };
        }

        // --- The open design ---

        static TResult WithLibrary<TResult>(Func<Library, TResult> action)
        {
            using var library = Library.Open(Plugin.GuitarFormSettings.LibraryPath);
            return action(library);
        }

        static void WithLibrary(Action<Library> action) => WithLibrary(library =>
        {
            action(library);
            return 0;
        });

        void ShowDesignList(long? select)
        {
            _showing = true;
            try
            {
                _designs.Items.Clear();
                foreach (var design in WithLibrary(library => library.ListDesigns()))
                    _designs.Items.Add(new ListItem { Text = design.Name, Key = design.Id.ToString(CultureInfo.InvariantCulture) });
                _designs.SelectedKey = select?.ToString(CultureInfo.InvariantCulture);
            }
            catch (Exception e)
            {
                RhinoApp.WriteLine($"GuitarForm: couldn't list the designs: {e.Message}");
            }
            finally
            {
                _showing = false;
            }
        }

        void OnDesignSelected()
        {
            if (_showing || _designs.SelectedKey == null)
                return;
            long id = long.Parse(_designs.SelectedKey, CultureInfo.InvariantCulture);
            if (id != _designId)
                Run(() => Open(id, false));
        }

        // Opens a design. A design that was just made (new, copied or imported) needs no overwrite-or-copy prompt.
        void Open(long id, bool confirmed)
        {
            _design = WithLibrary(library => library.LoadDesign(id));
            _designId = id;
            _editConfirmed = confirmed;
            _lastSoundhole = null;
            _newFields = NewFields.Load(id);
            Array.Clear(_newFieldErrors);
            _markersError = null;
            ShowDesignList(id);
            ShowDesign();
        }

        void CloseDesign()
        {
            _design = null;
            _designId = null;
            ShowDesign();
        }

        void OnUnitsMaybeChanged()
        {
            if (DisplayUnits.Current == _units)
                return;
            ShowDesign();
            _settings.ShowSettings();
        }

        // Fills every tab from the open design, enables what needs one, and redraws.
        void ShowDesign()
        {
            _units = DisplayUnits.Current;
            _showing = true;
            _instrument.Show(_design?.Instrument, _units);
            _body.Show(_design?.Body, _units);
            _hasSoundhole.Checked = _design?.Soundhole != null;
            _soundhole.Show(_design?.Soundhole, _units);
            for (int i = 0; i < NewFields.Fields.Length; i++)
            {
                var parameter = NewFields.Fields[i].Get(_newFields);
                _newFieldBoxes[i].Text = parameter.Format(_units.LengthUnit, CultureInfo.CurrentCulture);
                _newFieldUnits[i].Text = parameter.Symbol(_units.LengthUnit);
            }
            _markers.Text = NewFields.FormatMarkers(_newFields);
            _showing = false;

            foreach (var control in _needDesign)
                control.Enabled = _design != null;
            _soundhole.Enabled = _design?.Soundhole != null;
            Redraw();
        }

        // Saves a change to the open design. On the first change after opening it, asks whether to overwrite the
        // design or save the change in a copy; cancelling undoes the change.
        void Apply(Func<GuitarDesign, GuitarDesign> change)
        {
            if (_design == null || _designId == null)
                return;
            var changed = change(_design);
            if (changed == _design)
                return;

            try
            {
                if (!_editConfirmed)
                {
                    var answer = MessageBox.Show(this,
                        $"Save this change to \"{_design.Name}\"?\n\n" +
                        $"Yes overwrites \"{_design.Name}\".\nNo saves the change in a copy and leaves \"{_design.Name}\" as it was.\nCancel undoes the change.",
                        "GuitarForm", MessageBoxButtons.YesNoCancel, MessageBoxType.Question);
                    if (answer == DialogResult.Cancel)
                    {
                        Application.Instance.AsyncInvoke(ShowDesign);
                        return;
                    }
                    if (answer == DialogResult.No)
                    {
                        long original = _designId.Value;
                        long copy = WithLibrary(library => library.CopyDesign(original));
                        NewFields.Save(copy, _newFields);
                        changed = changed with { Name = WithLibrary(library => library.LoadDesign(copy).Name) };
                        _designId = copy;
                        ShowDesignList(copy);
                    }
                    _editConfirmed = true;
                }

                long id = _designId.Value;
                WithLibrary(library => library.SaveDesign(id, changed));
                _design = changed;
            }
            catch (Exception e)
            {
                MessageBox.Show(this, e.Message, "GuitarForm", MessageBoxType.Error);
                Application.Instance.AsyncInvoke(ShowDesign);
                return;
            }
            Redraw();
        }

        void OnHasSoundholeChanged()
        {
            if (_showing || _design == null)
                return;
            if (_hasSoundhole.Checked == true)
                Apply(d => d with { Soundhole = _lastSoundhole ?? new Soundhole() });
            else
            {
                _lastSoundhole = _design.Soundhole;
                Apply(d => d with { Soundhole = null });
            }
            _showing = true;
            _soundhole.Show(_design.Soundhole, _units);
            _showing = false;
            _soundhole.Enabled = _design.Soundhole != null;
        }

        // --- Drawing ---

        DesignDrawing Draw()
        {
            var doc = RhinoDoc.ActiveDoc;
            double tolerance = doc == null ? 0.01 : DesignUnits.ToleranceInMillimetres(doc.ModelAbsoluteTolerance, doc.ModelUnitSystem);
            var unit = _units.LengthUnit;
            var culture = CultureInfo.CurrentCulture;
            var guitar = NewFields.CopyFields(_newFields, NewFields.FromDesign(_design));
            var shapes = GuitarDrawing.Draw(guitar, tolerance, Plugin.GuitarFormSettings.DrawingGap, unit, culture);

            // A part's messages include its subparts', so each tab's are what's left after its subparts' are taken out.
            static GeometryMessage FromModel(ModelMessage m) => new GeometryMessage(m.Level, m.Text);
            var topPlate = guitar.Body.TopPlate.Validate(unit, culture).Select(FromModel).ToList();
            var bracing = guitar.Body.TopPlate.Bracing.Validate(unit, culture).Select(FromModel).ToList();
            var fretboard = guitar.Neck.Fretboard.Validate(unit, culture).Select(FromModel).ToList();
            var soundhole = topPlate.Except(bracing).ToList();
            var body = InputErrors(NewFields.BodyTab)
                .Concat(guitar.Body.Validate(unit, culture).Select(FromModel).Except(topPlate))
                .Concat(shapes.BodyMessages).ToList();
            _newTabMessages = new Dictionary<string, List<GeometryMessage>>
            {
                [NewFields.BracingTab] = InputErrors(NewFields.BracingTab).Concat(bracing).Concat(shapes.BracingMessages).ToList(),
                [NewFields.FretboardTab] = InputErrors(NewFields.FretboardTab).Concat(fretboard).Concat(shapes.FretboardMessages).ToList(),
                [NewFields.NeckTab] = InputErrors(NewFields.NeckTab)
                    .Concat(guitar.Neck.Validate(unit, culture).Select(FromModel).Except(fretboard))
                    .Concat(shapes.NeckMessages).ToList(),
            };
            return DesignDrawing.FromShapes(shapes, _design.Instrument.Validate(), body, soundhole);
        }

        // The errors from new fields on the tab whose text couldn't be read.
        IEnumerable<GeometryMessage> InputErrors(string tab) =>
            _newFieldErrors.Where((_, i) => NewFieldTabs[i] == tab)
                .Append(tab == NewFields.FretboardTab ? _markersError : null)
                .Where(e => e != null)
                .Select(e => new GeometryMessage(MessageLevel.Error, e));

        // Shows each section's messages and updates the preview.
        void Redraw()
        {
            var doc = RhinoDoc.ActiveDoc;
            if (_design == null)
            {
                _instrument.ShowMessages(null);
                _body.ShowMessages(null);
                _soundhole.ShowMessages(null);
                foreach (var messages in _tabMessages.Values)
                    messages.Show(null);
                _preview.Hide(doc);
                return;
            }

            var drawing = Draw();
            _instrument.ShowMessages(drawing.InstrumentMessages);
            _body.ShowMessages(drawing.BodyMessages);
            _soundhole.ShowMessages(drawing.SoundholeMessages);
            foreach (var (tab, messages) in _tabMessages)
                messages.Show(_newTabMessages[tab]);
            if (_showPreview.Checked == true && _panelVisible && doc != null)
                _preview.Show(drawing, doc);
            else
                _preview.Hide(doc);
        }

        void Build()
        {
            var doc = RhinoDoc.ActiveDoc ?? throw new InvalidOperationException("There's no Rhino document to build into.");
            int added = DesignBuilder.Build(doc, _designId.Value, _design.Name, Draw());
            RhinoApp.WriteLine($"GuitarForm: built \"{_design.Name}\" ({added} objects).");
        }

        // --- Design list buttons ---

        void NewDesign()
        {
            double length = 0, scale = 0;
            int fret = 0;
            var units = DisplayUnits.Current;
            var values = FieldsDialog.Ask(this, "New design",
                new[] { ("Name", ""), ($"Body length ({units.Abbreviation})", ""), ($"Scale length ({units.Abbreviation})", ""), ("Neck join fret", "") },
                v =>
                {
                    if (v[0].Length == 0)
                        return "Enter a name.";
                    if (WithLibrary(library => library.DesignExists(v[0])))
                        return $"There's already a design called \"{v[0]}\".";
                    if (!double.TryParse(v[1], NumberStyles.Float, CultureInfo.CurrentCulture, out length) || !(length > 0))
                        return "Body length must be a number greater than zero.";
                    if (!double.TryParse(v[2], NumberStyles.Float, CultureInfo.CurrentCulture, out scale) || !(scale > 0))
                        return "Scale length must be a number greater than zero.";
                    if (!int.TryParse(v[3], NumberStyles.Integer, CultureInfo.CurrentCulture, out fret) || fret < 1)
                        return "Neck join fret must be a whole number, 1 or more.";
                    return null;
                });
            if (values == null)
                return;

            var design = new GuitarDesign
            {
                Name = values[0],
                Instrument = new Instrument { ScaleLength = units.ToMillimetres(scale), NeckJoinFret = fret },
                Body = new Body { BodyLength = units.ToMillimetres(length) },
            };
            Open(WithLibrary(library => library.CreateDesign(design)), true);
        }

        void CopyDesign()
        {
            long original = _designId.Value;
            long copy = WithLibrary(library => library.CopyDesign(original));
            NewFields.Save(copy, _newFields);
            Open(copy, true);
        }

        void RenameDesign()
        {
            string current = _design.Name;
            var values = FieldsDialog.Ask(this, "Rename design", new[] { ("Name", current) }, v =>
                v[0].Length == 0 ? "Enter a name." :
                !string.Equals(v[0], current, StringComparison.OrdinalIgnoreCase) && WithLibrary(library => library.DesignExists(v[0]))
                    ? $"There's already a design called \"{v[0]}\"." : null);
            if (values == null || values[0] == current)
                return;

            var renamed = _design with { Name = values[0] };
            long id = _designId.Value;
            WithLibrary(library => library.SaveDesign(id, renamed));
            _design = renamed;
            ShowDesignList(id);
        }

        void DeleteDesign()
        {
            if (MessageBox.Show(this, $"Delete the design \"{_design.Name}\"? This can't be undone, except by restoring a backup.",
                    "GuitarForm", MessageBoxButtons.OKCancel, MessageBoxType.Warning) != DialogResult.Ok)
                return;
            long id = _designId.Value;
            WithLibrary(library => library.DeleteDesign(id));
            CloseDesign();
            ShowDesignList(null);
        }

        void ExportDesign()
        {
            var dialog = new SaveFileDialog
            {
                Title = "Export design",
                FileName = _design.Name + ".json",
                Filters = { new FileFilter("Design document", ".json") },
            };
            if (dialog.ShowDialog(this) == DialogResult.Ok)
                File.WriteAllText(dialog.FileName, NewFields.AddToDocument(DesignDocument.ToJson(_design), _newFields));
        }

        void ImportDesign()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Import design",
                Filters = { new FileFilter("Design document", ".json") },
            };
            if (dialog.ShowDialog(this) != DialogResult.Ok)
                return;

            string json = File.ReadAllText(dialog.FileName);
            var design = DesignDocument.FromJson(json);
            var newFields = NewFields.ReadFromDocument(json);
            string name = string.IsNullOrWhiteSpace(design.Name) ? Path.GetFileNameWithoutExtension(dialog.FileName) : design.Name;
            long id = WithLibrary(library => library.CreateDesign(design with { Name = library.UnusedDesignName(name) }));
            if (newFields != null)
                NewFields.Save(id, newFields);
            Open(id, true);
        }

        // --- IPanel: the preview is only shown while the panel is ---

        public void PanelShown(uint documentSerialNumber, ShowPanelReason reason)
        {
            _panelVisible = true;
            Redraw();
        }

        public void PanelHidden(uint documentSerialNumber, ShowPanelReason reason)
        {
            _panelVisible = false;
            Redraw();
        }

        public void PanelClosing(uint documentSerialNumber, bool onCloseDocument)
        {
            _panelVisible = false;
            Redraw();
        }
    }
}
