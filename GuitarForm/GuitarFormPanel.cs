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
    public partial class GuitarFormPanel : Panel, IPanel
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
        FretsTab _frets;
        Dictionary<string, List<GeometryMessage>> _newTabMessages = new Dictionary<string, List<GeometryMessage>>();

        // Controls that only work with a design open, and the preset lists to refresh when the library changes.
        readonly List<Control> _needDesign = new List<Control>();
        readonly List<Action> _refreshPresets = new List<Action>();

        // The Strings tab: the library of string sets, with its own working copy.
        StringsTab _strings;

        // The open design's working copy and every operation on it (see DesignSession). Edits change only its working
        // copy; Save writes it to the library. Replaced when the library changes, because it is made for one file.
        DesignSession _session;
        DisplayUnits _units = DisplayUnits.Current;
        readonly Label _dirtyLabel = new Label { Text = "", VerticalAlignment = VerticalAlignment.Center };
        readonly Button _saveButton = new Button { Text = "&Save" };
        // Unsaved work is written to a recovery file (by the session) shortly after the last edit.
        readonly UITimer _recoveryTimer = new UITimer { Interval = 2 };
        internal static GuitarFormPanel Current { get; private set; }
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
            _session = new DesignSession(CurrentLibraryPath);
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
                new Control[] { PresetRow<Instrument>(() => Design?.Instrument, preset => Apply(d => d with { Instrument = preset })) },
                Groups(null, _instrument.GroupTitles, _instrument.Rows, "Instrument"),
                _instrument.Messages));
            tabs.Pages.Add(SectionPage("Body",
                new Control[] { PresetRow<Body>(() => Design?.Body, preset => Apply(d => d with { Body = preset })) },
                Groups(NewFields.BodyTab, _body.GroupTitles, _body.Rows, "Body"),
                _body.Messages));
            tabs.Pages.Add(SectionPage("Soundhole",
                new Control[]
                {
                    _hasSoundhole,
                    PresetRow<Soundhole>(() => Design?.Soundhole, preset => Apply(d => d with { Soundhole = preset })),
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
