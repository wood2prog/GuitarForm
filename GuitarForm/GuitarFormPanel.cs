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
        readonly CheckBox _hasSoundhole = new CheckBox { Text = "This design has a soundhole" };
        readonly CheckBox _showStrings = new CheckBox { Text = "Show &strings", ToolTip = "Draw the strings in the preview and in Build. A setting for all designs." };
        readonly CheckBox _showPreview = new CheckBox { Text = "&Preview", Checked = true };
        readonly SettingsPage _settings = new SettingsPage();
        readonly DesignPreview _preview = new DesignPreview();

        // One binding (box, unit, row) for each field of GuitarFields, and the dot markers' text box.
        readonly FieldBindings _bindings = new FieldBindings(GuitarFields.Groups);
        readonly TextBox _markers = new TextBox { Width = UiMetrics.ChoiceBoxWidth, ToolTip = "Each marker's fret, with :2 for two dots, e.g. 3 5 7 9 12:2" };
        string _markersError;
        // Each field tab's messages: its input errors, the model's checks and the shapes' messages.
        readonly Dictionary<string, TabMessages> _tabMessages = GuitarFields.Tabs.ToDictionary(tab => tab, _ => new TabMessages());

        // The tabs; the selected tab's messages are shown in the MessagesBar below them.
        readonly TabControl _tabs = new TabControl();
        readonly Dictionary<TabPage, TabMessages> _pageMessages = new Dictionary<TabPage, TabMessages>();
        readonly MessagesBar _messagesBar = new MessagesBar();
        FretsTab _frets;
        Dictionary<string, List<GeometryMessage>> _tabMessageLists = new Dictionary<string, List<GeometryMessage>>();

        // Controls that only work with a design open, and the preset lists to refresh when the library changes.
        readonly List<Control> _needDesign = new List<Control>();
        readonly List<Action> _refreshSavedCopies = new List<Action>();

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

        static GuitarFormPlugin Plugin => GuitarFormPlugin.Instance;

        public GuitarFormPanel()
        {
            Current = this;
            _session = new DesignSession(CurrentLibraryPath);
            _recoveryTimer.Elapsed += (_, _) => FlushRecovery();
            foreach (var binding in _bindings.All)
                binding.Changed += OnFieldChanged;
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
                foreach (var refresh in _refreshSavedCopies)
                    refresh();
            };

            _markers.TextChanged += (_, _) => OnMarkersChanged();

            var tabs = _tabs;
            foreach (string tab in GuitarFields.Tabs)
            {
                Control[] top = tab switch
                {
                    GuitarFields.BodyTab => new Control[] { SavedCopiesRow<Model.Parts.Body>("body") },
                    GuitarFields.NeckTab => new Control[] { SavedCopiesRow<Neck>("neck") },
                    GuitarFields.BracingTab => new Control[] { SavedCopiesRow<Bracing>("bracing") },
                    GuitarFields.SoundholeTab => new Control[] { _hasSoundhole },
                    GuitarFields.FretboardTab => new Control[] { _showStrings },
                    _ => Array.Empty<Control>(),
                };
                tabs.Pages.Add(SectionPage(tab, top, Groups(tab), _tabMessages[tab]));
            }
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
                _strings?.Dispose();
                _frets?.Dispose();
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
