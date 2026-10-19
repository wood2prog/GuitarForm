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
    // and Preview / Build. Edits change a working copy of the open design in memory; nothing reaches the library until
    // Save (the first Save of a design asks whether to overwrite it or save a new design), Save as… or Revert. Unsaved
    // work is autosaved to a recovery file (RecoveryStore) and offered back when the design is next opened, and
    // switching designs, New, Import, Copy or changing the library asks to save first. The library is opened for each
    // operation and closed again, so the file is free for backing up, restoring and moving.
    [Guid("b27065af-ab08-4c16-aeef-415d7616b93c")]
    public class GuitarFormPanel : Panel, IPanel
    {
        readonly DropDown _designs = new DropDown();
        readonly SectionEditor<Instrument> _instrument = new SectionEditor<Instrument>(Sections.Instrument);
        readonly SectionEditor<Body> _body = new SectionEditor<Body>(Sections.Body);
        readonly SectionEditor<Soundhole> _soundhole = new SectionEditor<Soundhole>(Sections.Soundhole);
        readonly CheckBox _hasSoundhole = new CheckBox { Text = "This design has a soundhole" };
        readonly CheckBox _showStrings = new CheckBox { Text = "Show &strings", ToolTip = "Draw the strings in the preview and in Build. A setting for all designs." };
        readonly CheckBox _showPreview = new CheckBox { Text = "&Preview", Checked = true };
        readonly SettingsPage _settings = new SettingsPage();
        readonly DesignPreview _preview = new DesignPreview();

        // Values the old sections don't have, drawn with GuitarForm.Shapes (see NewFields): on the Body tab, and on the
        // Bracing, Fretboard and Neck tabs, each with its own messages.
        static readonly string[] NewFieldTabs = NewFields.Groups.SelectMany(g => g.Fields.Select(_ => g.Tab)).ToArray();
        // Choices and yes/no values are drop-downs, decimal numbers (lengths, angles, numbers) sliders, and counts
        // plain boxes.
        readonly IFieldBox[] _newFieldBoxes = NewFields.Fields.Select(field =>
        {
            var parameter = field.Get(new Guitar());
            IFieldBox box = parameter.OptionTexts.Count > 0
                ? new FieldDropDown(parameter.OptionTexts)
                : FieldLayout.Box(IsDecimal(parameter.Definition.Kind), step: field.Step);
            if (field.Placeholder != null)
                box.PlaceholderText = field.Placeholder;
            return box;
        }).ToArray();
        // Each new field's row, so fields that depend on other values can be hidden (see NewFields.Field.Visible).
        readonly TableRow[] _newFieldRows = new TableRow[NewFields.Fields.Length];
        readonly Label[] _newFieldUnits = NewFields.Fields.Select(_ => FieldLayout.UnitLabel()).ToArray();
        readonly string[] _newFieldErrors = new string[NewFields.Fields.Length];
        readonly TextBox _markers = new TextBox { Width = UiMetrics.ChoiceBoxWidth, ToolTip = "Each marker's fret, with :2 for two dots, e.g. 3 5 7 9 12:2" };
        string _markersError;
        readonly Dictionary<string, TabMessages> _tabMessages = new[] { NewFields.BracingTab, NewFields.FretboardTab, NewFields.NeckTab }
            .ToDictionary(tab => tab, _ => new TabMessages());

        // The tabs; the selected tab's messages are shown in the MessagesBar below them.
        readonly TabControl _tabs = new TabControl();
        readonly Dictionary<TabPage, TabMessages> _pageMessages = new Dictionary<TabPage, TabMessages>();
        readonly MessagesBar _messagesBar = new MessagesBar();
        Guitar _newFields = new Guitar();
        string _fretwireNote; // Why the guitar has no fretwire although it was saved with one.
        FretsTab _frets;
        string _stringSetNote; // Why the guitar has no string set although it was saved with one.
        Dictionary<string, List<GeometryMessage>> _newTabMessages = new Dictionary<string, List<GeometryMessage>>();

        // Controls that only work with a design open, and the preset lists to refresh when the library changes.
        readonly List<Control> _needDesign = new List<Control>();
        readonly List<Action> _refreshPresets = new List<Action>();

        // The Strings tab: the library of string sets, with its own working copy.
        StringsTab _strings;

        long? _designId;
        GuitarDesign _design;
        DisplayUnits _units = DisplayUnits.Current;
        // Edits change only the working copy (_design, _newFields); Save writes it to the library.
        bool _dirty;
        string _savedName;
        // Whether the first Save of this design has been asked about (overwrite or save as new).
        bool _savePrompted;
        readonly Label _dirtyLabel = new Label { Text = "", VerticalAlignment = VerticalAlignment.Center };
        readonly Button _saveButton = new Button { Text = "&Save" };
        // Unsaved work is written to a recovery file shortly after the last edit.
        readonly RecoveryStore _recovery = new RecoveryStore(RecoveryStore.DefaultFolder);
        readonly UITimer _recoveryTimer = new UITimer { Interval = 2 };
        internal static GuitarFormPanel Current { get; private set; }
        // The library the open design came from, which its recovery file is keyed by.
        string _recoveryLibrary;
        // The user chose "Don't save"; the working copy is dropped when a design is actually opened in its place.
        bool _discardChosen;

        static string CurrentLibraryPath => Plugin.GuitarFormSettings.LibraryPath;
        bool _showing;
        bool _panelVisible = true;

        // The soundhole a design had before its box was unticked, so ticking it again brings the values back.
        Soundhole _lastSoundhole;

        static GuitarFormPlugin Plugin => GuitarFormPlugin.Instance;

        public GuitarFormPanel()
        {
            Current = this;
            _recoveryTimer.Elapsed += (_, _) => FlushRecovery();
            _instrument.Changed += instrument => Apply(d => d with { Instrument = instrument });
            _body.Changed += body => Apply(d => d with { Body = body });
            _soundhole.Changed += soundhole => Apply(d => d with { Soundhole = soundhole });
            _hasSoundhole.CheckedChanged += (_, _) => OnHasSoundholeChanged();
            _designs.SelectedIndexChanged += (_, _) => OnDesignSelected();
            _showPreview.CheckedChanged += (_, _) => Redraw();
            _showStrings.Checked = GuitarFormPlugin.Instance.GuitarFormSettings.ShowStrings;
            _showStrings.CheckedChanged += (_, _) =>
            {
                GuitarFormPlugin.Instance.SetShowStrings(_showStrings.Checked == true);
                Redraw();
            };
            _settings.DrawingChanged += Redraw;

            // Lengths are shown in the active document's units, which change with its properties or another document.
            RhinoDoc.DocumentPropertiesChanged += OnDocumentUnitsEvent;
            RhinoDoc.EndOpenDocument += OnDocumentUnitsEvent;
            RhinoDoc.NewDocument += OnDocumentUnitsEvent;
            _settings.ConfirmLibraryChange = ConfirmLibraryChange;
            _settings.LibraryChanged += () =>
            {
                CloseDesign();
                ShowDesignList(null);
                try
                {
                    _strings?.Reload();
                }
                catch (Exception e)
                {
                    RhinoApp.WriteLine($"GuitarForm: couldn't list the string sets: {e.Message}");
                }
                try
                {
                    _frets?.Reload();
                }
                catch (Exception e)
                {
                    RhinoApp.WriteLine($"GuitarForm: couldn't list the fretwires: {e.Message}");
                }
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
                Groups(null, _instrument.GroupTitles, _instrument.Rows, "Instrument"),
                _instrument.Messages));
            tabs.Pages.Add(SectionPage("Body",
                new Control[] { PresetRow<Body>(() => _design?.Body, preset => Apply(d => d with { Body = preset })) },
                Groups(NewFields.BodyTab, _body.GroupTitles, _body.Rows, "Body"),
                _body.Messages));
            tabs.Pages.Add(SectionPage("Soundhole",
                new Control[]
                {
                    _hasSoundhole,
                    PresetRow<Soundhole>(() => _design?.Soundhole, preset => Apply(d => d with { Soundhole = preset })),
                },
                Groups(null, _soundhole.GroupTitles, _soundhole.Rows, "Soundhole"),
                _soundhole.Messages));
            foreach (string tab in _tabMessages.Keys)
                tabs.Pages.Add(SectionPage(tab, tab == NewFields.FretboardTab ? new Control[] { _showStrings } : Array.Empty<Control>(), Groups(tab), _tabMessages[tab]));
            // The string set library works with no design open.
            _strings = new StringsTab(() => CurrentLibraryPath);
            _strings.UseRequested += OnUseStringSet;
            _strings.LibraryItemChanged += OnStringLibraryChanged;
            tabs.Pages.Add(MessagesPage("Strings", _strings, _strings.Messages));
            _frets = new FretsTab(() => CurrentLibraryPath);
            _frets.UseRequested += OnUseFretwire;
            _frets.LibraryItemChanged += OnFretwireLibraryChanged;
            tabs.Pages.Add(MessagesPage("Frets", _frets, _frets.Messages));
            tabs.Pages.Add(new TabPage { Text = "Settings", Content = new Scrollable { Content = _settings, Border = BorderType.None } });

            Content = new TableLayout
            {
                Padding = new Padding(UiMetrics.Gap),
                Spacing = new Size(UiMetrics.Gap, UiMetrics.Gap),
                Rows =
                {
                    // In its own layout, so the outer one has a single column and the tabs get its full width.
                    new TableLayout
                    {
                        Spacing = new Size(UiMetrics.Gap, 0),
                        Rows = { new TableRow(new Label { Text = "Design", VerticalAlignment = VerticalAlignment.Center }, new TableCell(_designs, true), _dirtyLabel) },
                    },
                    Buttons(
                        SaveButton(),
                        NeedsDesign(ActionButton("Save &as…", () => SaveAs(null))),
                        NeedsDesign(ActionButton("Re&vert", RevertDesign))),
                    Buttons(
                        ActionButton("&New…", NewDesign),
                        NeedsDesign(ActionButton("&Copy", CopyDesign)),
                        NeedsDesign(ActionButton("R&ename…", RenameDesign)),
                        NeedsDesign(ActionButton("&Delete", DeleteDesign)),
                        MoreButton()),
                    new TableRow(TabsAndMessages()) { ScaleHeight = true },
                    _messagesBar,
                    new TableLayout
                    {
                        Spacing = new Size(UiMetrics.Gap, 0),
                        Rows = { new TableRow(new TableCell(_showPreview, true), NeedsDesign(BuildButton())) },
                    },
                },
            };

            ShowDesignList(null);
            ShowDesign();
        }

        bool _disposed;

        // The static RhinoDoc events outlive the panel, so Dispose unsubscribes this one handler from all three.
        void OnDocumentUnitsEvent(object sender, EventArgs e)
        {
            if (!_disposed)
                Application.Instance.AsyncInvoke(() =>
                {
                    if (!_disposed)
                        OnUnitsMaybeChanged();
                });
        }

        // --- Layout ---

        readonly ButtonMenuItem _exportItem = new ButtonMenuItem { Text = "Export…" };

        // "More ▾": Import… and Export… in a menu, so the design buttons fit on one row.
        Button MoreButton()
        {
            var menu = new ContextMenu();
            var import = new ButtonMenuItem { Text = "Import…" };
            import.Click += (_, _) => Run(ImportDesign);
            _exportItem.Click += (_, _) => Run(ExportDesign);
            menu.Items.Add(import);
            menu.Items.Add(_exportItem);
            var button = new Button { Text = "Mo&re ▾", ToolTip = "Import a design, or export the open one" };
            button.Click += (_, _) => menu.Show(button);
            return button;
        }

        // Build is the main action, so its text is bold.
        Button BuildButton()
        {
            var button = ActionButton("&Build", Build);
            button.Font = SystemFonts.Bold();
            button.ToolTip = "Build the design into the Rhino document";
            return button;
        }

        // A tab: the controls at its top (preset row, …) and its groups of fields. Its messages are shown below the tabs.
        TabPage SectionPage(string title, Control[] top, IEnumerable<Control> groups, TabMessages messages)
        {
            var content = new StackLayout { Padding = UiMetrics.PagePadding, Spacing = UiMetrics.GroupGap, HorizontalContentAlignment = HorizontalAlignment.Stretch };
            var groupList = groups.ToList();
            Label hint = null;
            if (groupList.Any(FieldLayout.HasSlider))
            {
                hint = new Label
                {
                    Text = "Click a value to type · Arrow keys step · Shift = fine · Ctrl = coarse",
                    Wrap = WrapMode.Word,
                    TextColor = UiMetrics.MutedTickColor,
                };
                content.Items.Add(hint);
            }
            foreach (var control in groupList)
                content.Items.Add(control);
            NeedsDesign(content);

            // The controls at the top (the preset row) stay put above the scrolling fields.
            var pinned = new StackLayout
            {
                Padding = new Padding(UiMetrics.PagePadding, UiMetrics.PagePadding, UiMetrics.PagePadding, 0),
                Spacing = UiMetrics.GroupGap,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
            };
            foreach (var control in top)
                pinned.Items.Add(control);
            NeedsDesign(pinned);
            var layout = new TableLayout();
            if (top.Length > 0)
                layout.Rows.Add(pinned);
            var scroll = new Scrollable { Content = content, Border = BorderType.None };
            if (hint != null)
                FieldLayout.WrapToViewport(scroll, 2 * UiMetrics.PagePadding, width => hint.Width = width);
            layout.Rows.Add(new TableRow(scroll) { ScaleHeight = true });
            return MessagesPage(title, layout, messages);
        }

        // A tab whose messages are shown below the tabs, and whose title carries their error count.
        TabPage MessagesPage(string title, Control content, TabMessages messages)
        {
            var page = new TabPage { Text = title, Content = content };
            _pageMessages[page] = messages;
            page.Text = TabTitle(title, messages);
            messages.Changed += () =>
            {
                page.Text = TabTitle(title, messages);
                if (_tabs.SelectedPage == page)
                    ShowSelectedMessages();
            };
            return page;
        }

        // The tabs, whose selected tab's messages are shown in the bar below them.
        Control TabsAndMessages()
        {
            _tabs.SelectedIndexChanged += (_, _) => ShowSelectedMessages();
            return _tabs;
        }

        void ShowSelectedMessages() => _messagesBar.Show(
            _tabs.SelectedPage != null && _pageMessages.TryGetValue(_tabs.SelectedPage, out var messages)
                ? messages.Messages : Array.Empty<GeometryMessage>());

        // A tab's title carries the number of errors in its section, e.g. "Body (2)".
        static string TabTitle(string title, TabMessages messages)
        {
            int errors = messages.Messages.Count(m => m.Level == MessageLevel.Error);
            return errors == 0 ? title : $"{title} ({errors})";
        }

        // A tab's groups: the old section's groups, each with any new fields of the same title added after its own,
        // then the tab's other new-field groups.
        IEnumerable<Control> Groups(string tab, IReadOnlyList<string> oldTitles = null, Func<string, IEnumerable<TableRow>> oldRows = null, string section = null)
        {
            section ??= tab;
            oldTitles ??= Array.Empty<string>();
            var titles = oldTitles.Concat(NewFields.Groups.Where(g => g.Tab == tab).Select(g => g.Title)).Distinct();
            return titles.Select(title => FieldLayout.Group(section, title,
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
                        {
                        var definition = field.Get(_newFields).Definition;
                        yield return _newFieldRows[index] = FieldLayout.Row(field.Get(_newFields).Label, null, (Control)_newFieldBoxes[index], _newFieldUnits[index],
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
                if (_designId == null)
                    return;
                _newFields = NewFields.WithStartingShape(_newFields, title, NeckProfilePresets.All[picked]);
                for (int i = 0; i < NewFields.Fields.Length; i++)
                    if (NewFieldTabs[i] == NewFields.NeckTab)
                        _newFieldErrors[i] = null;
                MarkDirty();
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
                    _newFieldBoxes[i].Enabled = enabled(_newFields);
                var visible = NewFields.Fields[i].Visible;
                if (visible == null || _newFieldRows[i] == null)
                    continue;
                bool show = visible(_newFields);
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
                var parameter = NewFields.Fields[i].Get(_newFields);
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
            if (_showing || _designId == null)
                return;
            var changed = NewFields.WithMarkers(_newFields, _markers.Text);
            _markersError = changed == null ? "Dot markers: write each marker's fret, with :2 for two dots, e.g. 3 5 7 9 12:2." : null;
            if (changed != null)
            {
                _newFields = changed;
                MarkDirty();
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
                UpdateNewFieldVisibility();
                MarkDirty();
            }
            else
                _newFieldErrors[index] = error.Text;
            Redraw();
        }

        static Control Buttons(params Control[] buttons)
        {
            var row = new StackLayout { Orientation = Orientation.Horizontal, Spacing = UiMetrics.Gap };
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
                var designs = WithLibrary(library => library.ListDesigns());
                foreach (var design in designs)
                    _designs.Items.Add(new ListItem { Text = design.Name, Key = design.Id.ToString(CultureInfo.InvariantCulture) });
                _recovery.DeleteStale(CurrentLibraryPath, designs.Select(d => d.Id));
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
            if (id == _designId)
                return;
            if (!ConfirmDiscard())
            {
                // Cancelled: put the previous design back in the drop-down.
                _showing = true;
                _designs.SelectedKey = _designId?.ToString(CultureInfo.InvariantCulture);
                _showing = false;
                return;
            }
            try
            {
                Open(id, false);
            }
            catch (Exception e)
            {
                MessageBox.Show(this, e.Message, "GuitarForm", MessageBoxType.Error);
                // Nothing was replaced: put the current design back in the drop-down.
                _discardChosen = false;
                _showing = true;
                _designs.SelectedKey = _designId?.ToString(CultureInfo.InvariantCulture);
                _showing = false;
            }
        }

        // Opens a design from the library as the working copy, dropping any unsaved changes the user chose to discard.
        // An existing design (confirmed false) offers its recovery file if there is one; a design that was just made
        // (new, copied, imported or saved as) needs no first-save prompt.
        void Open(long id, bool confirmed)
        {
            // Everything is loaded first, so a failure leaves the open design as it was.
            var loaded = WithLibrary(library => library.LoadDesign(id));
            var newFields = ResolveLibraryItems(NewFields.Load(id));
            DiscardWorkingCopy();
            _design = loaded;
            _designId = id;
            _savedName = _design.Name;
            _recoveryLibrary = CurrentLibraryPath;
            _dirty = false;
            _savePrompted = confirmed;
            _lastSoundhole = null;
            _newFields = newFields;
            Array.Clear(_newFieldErrors);
            _markersError = null;
            if (!confirmed)
                OfferRecovery(id);
            ShowDesignList(id);
            ShowDesign();
        }

        // If the design has a recovery file, asks whether to take it back as the working copy.
        void OfferRecovery(long id)
        {
            var recovery = _recovery.TryRead(_recoveryLibrary, id);
            if (recovery == null)
            {
                _recovery.Delete(_recoveryLibrary, id); // An unreadable file is no use.
                return;
            }
            if (MessageBox.Show(this, $"Recover unsaved changes to \"{_design.Name}\"?", "GuitarForm",
                    MessageBoxButtons.YesNo, MessageBoxType.Question) == DialogResult.Yes)
            {
                try
                {
                    _design = DesignDocument.FromJson(recovery.Document);
                    _newFields = ResolveLibraryItems(NewFields.ReadFromDocument(recovery.Document) ?? new Guitar());
                    MarkDirty();
                    return;
                }
                catch (Exception e)
                {
                    MessageBox.Show(this, $"The recovery file couldn't be read: {e.Message}", "GuitarForm", MessageBoxType.Error);
                }
            }
            _recovery.Delete(_recoveryLibrary, id);
        }

        // The guitar with its string set (known only by id after loading) read from the library. Sets the note shown on
        // the Strings tab when the saved set no longer exists. With expectedName, a set of another name counts as missing.
        // The same for the fretwire. With expected names (an import), an item of another name counts as missing.
        Guitar ResolveLibraryItems(Guitar guitar, string expectedStringSetName = null, string expectedFretwireName = null)
        {
            _stringSetNote = null;
            _fretwireNote = null;
            if (guitar.Neck.StringSet == null && guitar.Neck.Fretwire == null)
                return guitar;
            bool setMissing = false, wireMissing = false;
            Guitar resolved;
            try
            {
                resolved = WithLibrary(library => NewFields.ResolveLibraryItems(
                    guitar, library, out setMissing, out wireMissing, expectedStringSetName, expectedFretwireName));
            }
            catch (Exception e)
            {
                RhinoApp.WriteLine($"GuitarForm: couldn't read the string set or fretwire: {e.Message}");
                setMissing = guitar.Neck.StringSet != null;
                wireMissing = guitar.Neck.Fretwire != null;
                resolved = guitar with { Neck = guitar.Neck with { StringSet = null, Fretwire = null } };
            }
            if (setMissing)
                _stringSetNote = "the string set this guitar used is no longer in the library";
            if (wireMissing)
                _fretwireNote = "The fretwire this guitar used isn't in the library; choose another on the Frets tab.";
            return resolved;
        }

        // Tells the Strings and Frets tabs whether a design is open and which item its guitar uses.
        void UpdateStringsState()
        {
            _strings?.ShowAssigned(_design != null, _newFields.Neck.StringSet, _stringSetNote);
            _frets?.ShowAssigned(_design != null, _newFields.Neck.Fretwire, _fretwireNote);
        }

        // "Use for this guitar" on the Frets tab (null for none): the fretwire becomes the working guitar's, as an unsaved edit.
        void OnUseFretwire(Fretwire wire)
        {
            if (_design == null || _designId == null)
                return;
            _newFields = _newFields with { Neck = _newFields.Neck with { Fretwire = wire } };
            _fretwireNote = null;
            MarkDirty();
            UpdateStringsState();
            Redraw();
        }

        // A fretwire was saved or deleted in the library: the guitar's copy is read again (not an edit of the design).
        void OnFretwireLibraryChanged() => OnStringLibraryChanged();

        // "Use for this guitar": the saved set becomes the working guitar's string set, as an unsaved edit.
        void OnUseStringSet(StringSet set)
        {
            if (_design == null || _designId == null)
                return;
            _newFields = _newFields with { Neck = _newFields.Neck with { StringSet = set } };
            _stringSetNote = null;
            MarkDirty();
            UpdateStringsState();
            Redraw();
        }

        // A string set was saved or deleted in the library: the guitar's copy is read again (not an edit of the design).
        void OnStringLibraryChanged()
        {
            if (_design == null || (_newFields.Neck.StringSet?.Id == null && _newFields.Neck.Fretwire?.Id == null))
                return;
            // Both library items are read again (a deleted one leaves the guitar with none and a note).
            _newFields = ResolveLibraryItems(_newFields);
            UpdateStringsState();
            Redraw();
        }

        void CloseDesign()
        {
            DiscardWorkingCopy();
            _design = null;
            _designId = null;
            _dirty = false;
            ShowDesign();
        }

        Control SaveButton()
        {
            _saveButton.Click += (_, _) => Run(() => SaveDesign());
            return _saveButton;
        }

        void UpdateDirtyUi()
        {
            _dirtyLabel.Text = _dirty ? "Unsaved changes" : "";
            _saveButton.Enabled = _dirty && _design != null;
        }

        // Records an edit of the working copy; nothing is written until Save.
        void MarkDirty()
        {
            _dirty = true;
            _discardChosen = false;
            UpdateDirtyUi();
            _recoveryTimer.Stop();
            _recoveryTimer.Start();
        }

        // Writes the unsaved working copy to its recovery file now (also on hiding, closing and shutting down).
        internal void FlushRecovery()
        {
            try
            {
                _recoveryTimer.Stop();
                if (!_dirty || _design == null || _designId == null || _recoveryLibrary == null)
                    return;
                _recovery.Write(_recoveryLibrary, _designId.Value, _design.Name,
                    NewFields.AddToDocument(DesignDocument.ToJson(_design), _newFields));
            }
            catch (Exception e)
            {
                RhinoApp.WriteLine($"GuitarForm: couldn't write the recovery file: {e.Message}");
            }
        }

        // Removes the open design's recovery file: its unsaved work was saved or thrown away.
        void ClearRecovery()
        {
            try
            {
                _recoveryTimer.Stop();
                if (_designId != null && _recoveryLibrary != null)
                    _recovery.Delete(_recoveryLibrary, _designId.Value);
            }
            catch (Exception e)
            {
                RhinoApp.WriteLine($"GuitarForm: couldn't delete the recovery file: {e.Message}");
            }
        }

        // Drops the working copy's unsaved state once the user's "Don't save" is carried out.
        void DiscardWorkingCopy()
        {
            if (!_discardChosen)
                return;
            _discardChosen = false;
            ClearRecovery();
            _dirty = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _disposed = true;
                RhinoDoc.DocumentPropertiesChanged -= OnDocumentUnitsEvent;
                RhinoDoc.EndOpenDocument -= OnDocumentUnitsEvent;
                RhinoDoc.NewDocument -= OnDocumentUnitsEvent;
                try
                {
                    _preview.Hide(RhinoDoc.ActiveDoc);
                }
                catch (Exception e)
                {
                    RhinoApp.WriteLine($"GuitarForm: couldn't hide the preview: {e.Message}");
                }
                _recoveryTimer.Stop();
                _recoveryTimer.Dispose();
                if (Current == this)
                    Current = null;
            }
            base.Dispose(disposing);
        }

        // Writes the working copy to the library (and its new fields beside it). The first Save of a design asks
        // whether to overwrite it or save a new design instead.
        // overwriteConfirmed: the user already said Yes to saving over this design, so the first-save question is skipped.
        void SaveDesign(bool overwriteConfirmed = false)
        {
            if (_design == null || _designId == null)
                return;
            RequireNoInputErrors("saved");
            if (!_savePrompted && !overwriteConfirmed)
            {
                var answer = MessageBox.Show(this,
                    $"Save your changes to \"{_savedName}\"?\n\nYes overwrites \"{_savedName}\".\nNo saves them as a new design and leaves \"{_savedName}\" as it was.\nCancel doesn't save.",
                    "GuitarForm", MessageBoxButtons.YesNoCancel, MessageBoxType.Question);
                if (answer == DialogResult.Cancel)
                    return;
                if (answer == DialogResult.No)
                {
                    SaveAs(WithLibrary(library => library.UnusedDesignName(_design.Name + " copy")));
                    return;
                }
            }

            string name = _design.Name;
            if (name.Length == 0)
                throw new InvalidOperationException("Enter a name for the design.");
            if (!string.Equals(name, _savedName, StringComparison.OrdinalIgnoreCase) && WithLibrary(library => library.DesignExists(name)))
                throw new InvalidOperationException($"There's already a design called \"{name}\".");

            long id = _designId.Value;
            var design = _design;
            WithLibrary(library => library.SaveDesign(id, design));
            NewFields.Save(id, _newFields);
            _savePrompted = true;
            _savedName = name;
            _dirty = false;
            ClearRecovery();
            ShowDesignList(id);
            UpdateDirtyUi();
        }

        // Saves the working copy as a new design and switches to it; the original is left as it was. With no name
        // given, asks for one.
        void SaveAs(string name)
        {
            if (_design == null)
                return;
            RequireNoInputErrors("saved");
            if (name == null)
            {
                string initial = WithLibrary(library => library.UnusedDesignName(_design.Name + " copy"));
                var values = FieldsDialog.Ask(this, "Save as", new[] { ("Name", initial) }, v =>
                    v[0].Length == 0 ? "Enter a name." :
                    WithLibrary(library => library.DesignExists(v[0])) ? $"There's already a design called \"{v[0]}\"." : null);
                if (values == null)
                    return;
                name = values[0];
            }

            var design = _design with { Name = name };
            long id = WithLibrary(library => library.CreateDesign(design));
            try
            {
                NewFields.Save(id, _newFields);
            }
            catch
            {
                // No orphan: the design that was just created is removed again.
                try
                {
                    WithLibrary(library => library.DeleteDesign(id));
                }
                catch (Exception)
                {
                }
                throw;
            }
            ClearRecovery();
            Open(id, true);
        }

        // True when it's fine to replace the working copy: there are no unsaved changes, or the user saved them or chose
        // to discard them. False when they cancelled, or the save didn't happen.
        // Only the design's own unsaved changes count: edits to a string set or fretwire in the library tabs are
        // independent of the design and stay in memory across design switches.
        bool ConfirmDiscard() => ConfirmDiscardDesign();

        // For a change of library (Settings: switch or restore), which replaces everything: the design's unsaved
        // changes are asked about, then those in the Strings and Frets tabs. Tabs whose changes were discarded are
        // put back to their saved state so the question doesn't come back.
        bool ConfirmLibraryChange()
        {
            if (!ConfirmDiscardDesign() || !(_strings?.ConfirmDiscard() ?? true) || !(_frets?.ConfirmDiscard() ?? true))
                return false;
            _strings?.DiscardChanges();
            _frets?.DiscardChanges();
            return true;
        }

        bool ConfirmDiscardDesign()
        {
            if (!_dirty || _design == null)
                return true;
            _discardChosen = false;
            // A design's first Save would ask overwrite-or-new next, so that choice is folded into this one question.
            string question = _savePrompted
                ? $"Save changes to \"{_design.Name}\"?"
                : $"Save changes to \"{_savedName}\"?\n\nYes overwrites \"{_savedName}\" (use Save as… to keep it).\nNo discards the changes.";
            var answer = MessageBox.Show(this, question, "GuitarForm", MessageBoxButtons.YesNoCancel, MessageBoxType.Question);
            if (answer == DialogResult.Cancel)
                return false;
            if (answer == DialogResult.No)
            {
                // Only the choice is recorded; the changes are dropped when the replacement really happens.
                _discardChosen = true;
                return true;
            }
            try
            {
                SaveDesign(true);
            }
            catch (Exception e)
            {
                MessageBox.Show(this, e.Message, "GuitarForm", MessageBoxType.Error);
                return false;
            }
            return !_dirty;
        }

        void RevertDesign()
        {
            if (_designId == null)
                return;
            if (_dirty && MessageBox.Show(this, $"Discard unsaved changes to \"{_savedName}\"?", "GuitarForm",
                    MessageBoxButtons.OKCancel, MessageBoxType.Warning) != DialogResult.Ok)
                return;
            ClearRecovery();
            Open(_designId.Value, _savePrompted);
        }

        void OnUnitsMaybeChanged()
        {
            if (DisplayUnits.Current == _units)
                return;
            ShowDesign();
            _strings?.RefreshUnits();
            _frets?.RefreshUnits();
            _settings.ShowSettings();
        }

        // Fills every tab from the open design, enables what needs one, and redraws.
        void ShowDesign()
        {
            _units = DisplayUnits.Current;
            Array.Clear(_newFieldErrors);
            _markersError = null;
            _showing = true;
            _instrument.Show(_design?.Instrument, _units);
            _body.Show(_design?.Body, _units);
            _hasSoundhole.Checked = _design?.Soundhole != null;
            _soundhole.Show(_design?.Soundhole, _units);
            ShowNewFields();
            _markers.Text = NewFields.FormatMarkers(_newFields);
            _showing = false;

            foreach (var control in _needDesign)
                control.Enabled = _design != null;
            _soundhole.Enabled = _design?.Soundhole != null;
            _exportItem.Enabled = _design != null;
            UpdateDirtyUi();
            UpdateStringsState();
            Redraw();
        }

        // Whether any box holds text that couldn't be read, so the working copy isn't what the boxes show.
        bool HasInputErrors() =>
            _instrument.HasInputErrors || _body.HasInputErrors ||
            (_design?.Soundhole != null && _soundhole.HasInputErrors) || _markersError != null ||
            _newFieldErrors.Where((_, i) => NewFields.Fields[i].Visible?.Invoke(_newFields) ?? true).Any(e => e != null);

        // Stops an action (shown to the user by Run or ConfirmDiscard) while some text couldn't be read.
        void RequireNoInputErrors(string what)
        {
            if (HasInputErrors())
                throw new InvalidOperationException(
                    $"Some text couldn't be read, so the design can't be {what} yet. Fix the fields marked in Messages first.");
        }

        // Changes the working copy of the open design; nothing is written to the library until Save.
        void Apply(Func<GuitarDesign, GuitarDesign> change)
        {
            if (_design == null || _designId == null)
                return;
            var changed = change(_design);
            if (changed == _design)
                return;

            _design = changed;
            MarkDirty();
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
            _newFieldErrors.Where((_, i) => NewFieldTabs[i] == tab &&
                    (NewFields.Fields[i].Visible?.Invoke(_newFields) ?? true))
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
                _preview.Show(drawing, doc, _showStrings.Checked == true);
            else
                _preview.Hide(doc);
        }

        void Build()
        {
            var doc = RhinoDoc.ActiveDoc ?? throw new InvalidOperationException("There's no Rhino document to build into.");
            RequireNoInputErrors("built");
            var drawing = Draw();
            // A part with errors is drawn blank in the preview, so building would leave it out.
            int errors = drawing.InstrumentMessages.Concat(drawing.BodyMessages).Concat(drawing.SoundholeMessages)
                .Concat(_newTabMessages.Values.SelectMany(m => m))
                .Where(m => m.Level == MessageLevel.Error).Distinct().Count();
            if (errors > 0 && MessageBox.Show(this,
                    $"The design has {errors} {(errors == 1 ? "error" : "errors")} (see Messages), so the parts with errors will be missing from the build.\n\nBuild anyway?",
                    "GuitarForm", MessageBoxButtons.YesNo, MessageBoxType.Warning) != DialogResult.Yes)
                return;
            int added = DesignBuilder.Build(doc, _designId.Value.ToString(CultureInfo.InvariantCulture), new List<string>(), _design.Name, drawing, _showStrings.Checked == true);
            RhinoApp.WriteLine($"GuitarForm: built \"{_design.Name}\"{(_dirty ? " (unsaved)" : "")} ({added} objects).");
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
            if (values == null || !ConfirmDiscard())
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
            if (!ConfirmDiscard())
                return;
            // The saved design is copied, with its saved new fields (the working copy was just saved or discarded).
            long original = _designId.Value;
            long copy = WithLibrary(library => library.CopyDesign(original));
            NewFields.Save(copy, NewFields.Load(original));
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

            // Applied to the library on Save.
            Apply(d => d with { Name = values[0] });
        }

        void DeleteDesign()
        {
            string unsaved = _dirty ? " Its unsaved changes are lost too." : "";
            if (MessageBox.Show(this, $"Delete the design \"{_design.Name}\"? This can't be undone, except by restoring a backup.{unsaved}",
                    "GuitarForm", MessageBoxButtons.OKCancel, MessageBoxType.Warning) != DialogResult.Ok)
                return;
            long id = _designId.Value;
            WithLibrary(library => library.DeleteDesign(id));
            try
            {
                NewFields.Delete(id);
            }
            catch (Exception e)
            {
                RhinoApp.WriteLine($"GuitarForm: couldn't delete the design's new-field values: {e.Message}");
            }
            _dirty = false;
            ClearRecovery();
            CloseDesign();
            ShowDesignList(null);
        }

        void ExportDesign()
        {
            RequireNoInputErrors("exported");
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
            if (!ConfirmDiscard())
                return;
            // The file's string set id belongs to the library it was exported from, so it counts only if the name matches.
            if (newFields != null)
                newFields = ResolveLibraryItems(newFields, NewFields.DocumentStringSetName(json) ?? "",
                    NewFields.DocumentFretwireName(json) ?? "");
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
            FlushRecovery();
            Redraw();
        }

        public void PanelClosing(uint documentSerialNumber, bool onCloseDocument)
        {
            _panelVisible = false;
            FlushRecovery();
            Redraw();
        }
    }
}
