using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Eto.Forms;
using GuitarForm.Data;

namespace GuitarForm
{
    // The pieces shared by the tabs that edit a library of items (string sets, fretwires): running an action with the
    // library open, reporting errors, the unsaved-changes prompts, the picker and the delete-with-replacement flow.
    // Each takes the item-kind word as shown to the user (e.g. "string set", "fretwire").
    static class LibraryItemUi
    {
        const string Title = "GuitarForm";

        // Opens the library at the path, runs the action and closes it.
        public static T WithLibrary<T>(Func<string> libraryPath, Func<Library, T> action)
        {
            using var library = Library.Open(libraryPath());
            return action(library);
        }

        // Runs the action, showing any exception's message. Returns whether it finished without one.
        public static bool Run(Control owner, Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(owner, e.Message, Title, MessageBoxType.Error);
                return false;
            }
        }

        // Fills the picker with the items and selects one (or none). The caller guards against the change event.
        public static void FillPicker(DropDown picker, IReadOnlyList<LibraryItemInfo> items, long? select)
        {
            picker.Items.Clear();
            foreach (var item in items)
                picker.Items.Add(new ListItem { Text = item.Name, Key = item.Id.ToString(CultureInfo.InvariantCulture) });
            picker.SelectedKey = select?.ToString(CultureInfo.InvariantCulture);
        }

        // The picked item's id, or null when none is picked.
        public static long? PickedId(DropDown picker) =>
            picker.SelectedKey == null ? null : long.Parse(picker.SelectedKey, CultureInfo.InvariantCulture);

        // The prompt for unsaved changes. Returns true when it's fine to go on: the user discarded the changes, or
        // saved them (save returns whether it did). Any exception is shown and counts as no.
        public static bool ConfirmDiscard(Control owner, string kind, string name, Func<bool> save)
        {
            try
            {
                var answer = MessageBox.Show(owner, name.Length == 0 ? $"Save changes to the new {kind}?" : $"Save changes to \"{name}\"?",
                    Title, MessageBoxButtons.YesNoCancel, MessageBoxType.Question);
                if (answer == DialogResult.Cancel)
                    return false;
                if (answer == DialogResult.No)
                    return true;
                return save();
            }
            catch (Exception e)
            {
                MessageBox.Show(owner, e.Message, Title, MessageBoxType.Error);
                return false;
            }
        }

        // Asks whether to discard the unsaved changes (for Revert). name is the saved item's name; isNew when unsaved.
        public static bool ConfirmRevert(Control owner, string kind, bool isNew, string name) =>
            MessageBox.Show(owner, $"Discard unsaved changes to {(isNew ? $"the new {kind}" : $"\"{name}\"")}?",
                Title, MessageBoxButtons.OKCancel, MessageBoxType.Warning) == DialogResult.Ok;

        // Asks whether to take back unsaved edits found in a recovery file. name is the saved item's name; isNew when
        // the edits were to a new item that was never saved.
        public static bool OfferRecovery(Control owner, string kind, string name, bool isNew) =>
            MessageBox.Show(owner, isNew ? $"Recover the unsaved new {kind}?" : $"Recover unsaved changes to the {kind} \"{name}\"?",
                Title, MessageBoxButtons.YesNo, MessageBoxType.Question) == DialogResult.Yes;

        // Asks whether to recover edits as a new item, when the item they were made to no longer exists.
        public static bool OfferRecoveryAsNew(Control owner, string kind) =>
            MessageBox.Show(owner, $"Recover as a new {kind}?", Title, MessageBoxButtons.YesNo, MessageBoxType.Question) == DialogResult.Yes;

        public static void Warn(Control owner, string text) =>
            MessageBox.Show(owner, text, Title, MessageBoxType.Warning);

        // Works out what a delete needs: lists the necks using the item and, when there are any, asks which other item
        // they should use. Returns false when the delete was cancelled or can't go ahead (after saying why); otherwise
        // the replacement is the chosen item's id, or null when nothing uses the item.
        public static bool ConfirmDelete<T>(Control owner, Func<string> libraryPath, string kind, string name, long id,
            bool unsaved, out long? replacement) where T : GuitarForm.Model.Parts.Part
        {
            replacement = null;
            var users = WithLibrary(libraryPath, library => library.NecksUsing(id));
            if (users.Count > 0)
            {
                var others = WithLibrary(libraryPath, library => library.ListLibraryItems<T>()).Where(s => s.Id != id).ToList();
                if (others.Count == 0)
                {
                    MessageBox.Show(owner, $"\"{name}\" is used by {users.Count} neck{(users.Count == 1 ? "" : "s")}, and there's no other {kind} to replace it with. Save another {kind} first.",
                        Title, MessageBoxType.Warning);
                    return false;
                }
                replacement = ReplacementDialog.Ask(owner, kind, name, users.Select(u => u.Name ?? u.GuitarName).ToList(), others, unsaved);
                return replacement != null;
            }
            return MessageBox.Show(owner, $"Delete the {kind} \"{name}\"? This can't be undone, except by restoring a backup.{(unsaved ? " Its unsaved changes are lost too." : "")}",
                Title, MessageBoxButtons.OKCancel, MessageBoxType.Warning) == DialogResult.Ok;
        }

        // Lists the necks that use an item and asks which other item they should use from now on.
        sealed class ReplacementDialog : Dialog<bool>
        {
            readonly DropDown _choices = new DropDown { Width = UiMetrics.SliderMaxWidth };
            readonly IReadOnlyList<LibraryItemInfo> _items;

            ReplacementDialog(string kind, string name, IReadOnlyList<string> users, IReadOnlyList<LibraryItemInfo> items, bool unsaved)
            {
                _items = items;
                Title = $"Delete {kind}";
                Padding = UiMetrics.PagePadding;
                Resizable = true;
                foreach (var item in items)
                    _choices.Items.Add(item.Name);
                _choices.SelectedIndex = 0;

                var ok = new Button { Text = "Delete" };
                ok.Click += (_, _) => Close(true);
                var cancel = new Button { Text = "Cancel" };
                cancel.Click += (_, _) => Close(false);
                DefaultButton = ok;
                AbortButton = cancel;

                var layout = new StackLayout { Spacing = UiMetrics.Gap, HorizontalContentAlignment = HorizontalAlignment.Stretch };
                layout.Items.Add(new Label
                {
                    Text = $"\"{name}\" is used by {users.Count} neck{(users.Count == 1 ? "" : "s")}:\n" + string.Join("\n", users) +
                        $"\n\nChoose the {kind} they will use instead." +
                        (unsaved ? "\n\nUnsaved changes to the working copy will be lost." : ""),
                    Wrap = WrapMode.Word,
                });
                layout.Items.Add(_choices);
                layout.Items.Add(new StackLayout { Orientation = Eto.Forms.Orientation.Horizontal, Spacing = UiMetrics.Gap, Items = { ok, cancel } });
                Content = layout;
            }

            public static long? Ask(Control parent, string kind, string name, IReadOnlyList<string> users, IReadOnlyList<LibraryItemInfo> items, bool unsaved)
            {
                var dialog = new ReplacementDialog(kind, name, users, items, unsaved);
                return dialog.ShowModal(parent) && dialog._choices.SelectedIndex >= 0
                    ? dialog._items[dialog._choices.SelectedIndex].Id : null;
            }
        }
    }
}
