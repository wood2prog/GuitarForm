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
    public partial class GuitarFormPanel
    {
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

        Control SaveButton()
        {
            _saveButton.Click += (_, _) => Run(() => SaveDesign());
            return _saveButton;
        }
    }
}
