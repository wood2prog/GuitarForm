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

        // The row at the top of the Body, Neck and Bracing tabs: the library's saved copies of that part (a drop-down),
        // with Load (copies the saved values into the open design, as an unsaved edit), Save as… (saves the open
        // design's part under a new name) and Delete. T is Neck, Body or Bracing; what is the name shown to the user.
        Control SavedCopiesRow<T>(string what) where T : Part
        {
            var copies = new DropDown();
            void Refresh()
            {
                string key = copies.SelectedKey;
                copies.Items.Clear();
                try
                {
                    foreach (var copy in _session.ListNamed<T>())
                        copies.Items.Add(new ListItem { Text = copy.Name, Key = copy.Id.ToString(CultureInfo.InvariantCulture) });
                    copies.SelectedKey = key;
                }
                catch (Exception e)
                {
                    RhinoApp.WriteLine($"GuitarForm: couldn't list the saved {what} copies: {e.Message}");
                }
            }
            _refreshSavedCopies.Add(Refresh);
            Refresh();

            long SelectedId(string action) =>
                long.TryParse(copies.SelectedKey, NumberStyles.None, CultureInfo.InvariantCulture, out long id)
                    ? id : throw new InvalidOperationException($"Choose a saved {what} copy to {action}.");

            var loadButton = ActionButton("Load", () =>
            {
                long id = SelectedId("load");
                var problems = _session.LoadNamed<T>(id);
                MarkDirty();
                ShowDesign();
                if (problems.Count > 0)
                {
                    foreach (string problem in problems)
                        RhinoApp.WriteLine($"GuitarForm: {problem}");
                    string list = string.Join("\n", problems.Take(12).Select(p => "- " + p)) +
                        (problems.Count > 12 ? $"\n... and {problems.Count - 12} more." : "");
                    MessageBox.Show(this,
                        "Some values stored with this saved copy couldn't be read, so they are left blank:\n\n" + list,
                        "GuitarForm", MessageBoxButtons.OK, MessageBoxType.Warning);
                }
            });
            var saveButton = ActionButton("Save as…", () =>
            {
                RequireNoInputErrors("saved");
                var values = FieldsDialog.Ask(this, $"Save {what} copy", new[] { ("Name", "") },
                    v => _session.CheckNamedName<T>(v[0]));
                if (values == null)
                    return;
                long id = _session.SaveNamed<T>(values[0]);
                Refresh();
                copies.SelectedKey = id.ToString(CultureInfo.InvariantCulture);
            });
            var deleteButton = ActionButton("Delete", () =>
            {
                long id = SelectedId("delete");
                if (MessageBox.Show(this, $"Delete the saved {what} copy \"{copies.SelectedValue}\"?", "GuitarForm",
                        MessageBoxButtons.OKCancel, MessageBoxType.Question) != DialogResult.Ok)
                    return;
                _session.DeleteNamed(id);
                Refresh();
            });
            NeedsDesign(loadButton);
            NeedsDesign(saveButton);

            return new TableLayout
            {
                Spacing = new Size(6, 4),
                Rows =
                {
                    new TableRow(new Label { Text = $"Saved {what} copies", VerticalAlignment = VerticalAlignment.Center }, new TableCell(copies, true)),
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
