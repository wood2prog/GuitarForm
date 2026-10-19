using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using GuitarForm.Data;
using GuitarForm.Model;
using GuitarForm.Model.Parameters;
using GuitarForm.Model.Parts;

namespace GuitarForm
{
    // The Strings tab: the library of string sets, which can be edited with no design open. A picker of the saved sets,
    // New, Save, Save as…, Revert and Delete, and the StringSetEditor showing the working copy. Edits change only the
    // working copy (_set); nothing reaches the library until Save. There are no recovery files for it.
    sealed class StringsTab : Panel
    {
        readonly StringSetEditor _editor = new StringSetEditor();
        readonly DropDown _picker = new DropDown();
        readonly Label _dirtyLabel = new Label { Text = "", VerticalAlignment = VerticalAlignment.Center };
        readonly Button _save = new Button { Text = "&Save" };
        readonly Button _saveAs = new Button { Text = "Save &as…" };
        readonly Button _revert = new Button { Text = "Re&vert" };
        readonly Button _delete = new Button { Text = "&Delete" };
        readonly Button _new = new Button { Text = "&New" };
        readonly Button _use = new Button { Text = "&Use for this guitar" };
        readonly Label _usedBy = new Label { Text = "", VerticalAlignment = VerticalAlignment.Center };
        const string Kind = "string set";
        readonly Func<string> _libraryPath;
        bool _designOpen;
        DisplayUnits _units = DisplayUnits.Current;
        // The working copy (its Id is null until it's saved) and the saved set it was loaded as, to tell if it changed.
        StringSet _set = BlankSet();
        StringSet _savedSet = BlankSet();
        bool _showing;

        public StringsTab(Func<string> libraryPath)
        {
            _libraryPath = libraryPath;
            _editor.Changed += set =>
            {
                _set = set with { Id = _set.Id };
                UpdateUi();
            };
            // The unit weight display is remembered in the settings.
            _editor.UnitWeightInLbPerIn = GuitarFormPlugin.Instance.GuitarFormSettings.StringUnitWeightInLbPerIn;
            _editor.UnitWeightUnitChanged += () =>
            {
                var plugin = GuitarFormPlugin.Instance;
                plugin.UpdateSettings(plugin.GuitarFormSettings with { StringUnitWeightInLbPerIn = _editor.UnitWeightInLbPerIn });
            };
            _picker.SelectedIndexChanged += (_, _) => OnPicked();
            _save.Click += (_, _) => Run(() => Save());
            _saveAs.Click += (_, _) => Run(SaveAs);
            _revert.Click += (_, _) => Run(Revert);
            _delete.Click += (_, _) => Run(Delete);
            _new.Click += (_, _) => Run(NewSet);
            _use.Click += (_, _) => Run(UseForGuitar);

            var buttons = new StackLayout { Orientation = Orientation.Horizontal, Spacing = UiMetrics.Gap };
            foreach (var button in new[] { _new, _save, _saveAs, _revert, _delete, _use })
                buttons.Items.Add(button);

            // The picker and buttons stay put above the scrolling editor.
            var pinned = new StackLayout
            {
                Padding = new Padding(UiMetrics.PagePadding, UiMetrics.PagePadding, UiMetrics.PagePadding, 0),
                Spacing = UiMetrics.GroupGap,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
            };
            pinned.Items.Add(new TableLayout
            {
                Spacing = new Size(UiMetrics.Gap, 0),
                Rows = { new TableRow(new Label { Text = "String set", VerticalAlignment = VerticalAlignment.Center }, new TableCell(_picker, true), _dirtyLabel) },
            });
            pinned.Items.Add(buttons);
            pinned.Items.Add(_usedBy);

            Content = new TableLayout
            {
                Rows = { pinned, new TableRow(_editor) { ScaleHeight = true } },
            };

            try
            {
                Reload();
            }
            catch (Exception e)
            {
                global::Rhino.RhinoApp.WriteLine($"GuitarForm: couldn't list the string sets: {e.Message}");
                ShowSet(BlankSet(), BlankSet());
            }
        }

        // The editor's messages, for the panel's Messages bar and the tab's title.
        public TabMessages Messages => _editor.Messages;

        // The working copy's saved set, as stored in the library; null when the working copy is a new, unsaved set.
        // (For "Use for this guitar".)
        public StringSet LoadSelectedSavedSet() =>
            _set.Id is long id ? WithLibrary(library => library.LoadLibraryItem<StringSet>(id)) : null;

        // The id of the saved set being shown, or null for a new one.
        public long? SelectedId => _set.Id;

        public bool IsDirty => !SameSet(_set, _savedSet);

        // The editor, so the owner can read or set its unit weight display.
        public StringSetEditor Editor => _editor;

        // Raised when the user asks to use the shown (saved, unchanged) set for the open guitar, with the set as stored.
        public event Action<StringSet> UseRequested;

        // Raised after a set was saved or deleted, so the owner can refresh a copy of it that it holds.
        public event Action LibraryItemChanged;

        // The owner tells the tab whether a design is open and what its guitar uses: a set (null for none) and, when it
        // has none (or could not be read), an optional note on why (e.g. the saved set no longer exists).
        public void ShowAssigned(bool designOpen, StringSet assigned, string note = null)
        {
            _designOpen = designOpen;
            _usedBy.Text = !designOpen ? ""
                : "Used by this guitar: " + (assigned == null ? "none" : EmptyAs(assigned.DisplayName, "(unnamed set)")) +
                  (note != null ? $" ({note})" : "");
            UpdateUi();
        }

        static string EmptyAs(string text, string fallback) => text.Length == 0 ? fallback : text;

        void UseForGuitar()
        {
            if (!_designOpen || _set.Id == null || IsDirty)
                return;
            var saved = LoadSelectedSavedSet();
            if (saved != null)
                UseRequested?.Invoke(saved);
        }

        // The document's units changed.
        public void RefreshUnits()
        {
            _units = DisplayUnits.Current;
            _editor.Refresh(_units);
        }

        // The library changed (another file, a restore): shows its first set. Unsaved work is dropped, so the caller
        // asks ConfirmDiscard first.
        public void Reload()
        {
            var sets = WithLibrary(library => library.ListLibraryItems<StringSet>());
            if (sets.Count == 0)
            {
                FillPicker(sets, null);
                ShowSet(BlankSet(), BlankSet());
                return;
            }
            Open(sets[0].Id, sets);
        }

        // True when it's fine to replace the working copy: no unsaved changes, or the user saved or discarded them.
        public bool ConfirmDiscard()
        {
            if (!IsDirty)
                return true;
            return LibraryItemUi.ConfirmDiscard(this, Kind, DisplayName(_set), Save);
        }

        // Drops unsaved changes by showing the saved set again (no question: the caller has asked).
        public void DiscardChanges()
        {
            if (!IsDirty)
                return;
            try
            {
                if (_set.Id is long id)
                    Open(id);
                else
                    ShowSet(BlankSet(), BlankSet());
            }
            catch (Exception e)
            {
                global::Rhino.RhinoApp.WriteLine($"GuitarForm: couldn't reload the string set: {e.Message}");
            }
        }

        // --- Working copy ---

        static StringSet BlankSet() => new StringSet().WithAddedString();

        static string DisplayName(StringSet set) =>
            $"{set.Manufacturer.Value} {set.Name.Value}".Trim();

        // Record equality compares the string list by reference, so the strings are compared one by one.
        static bool SameSet(StringSet a, StringSet b) =>
            a with { Strings = Array.Empty<GuitarString>() } == b with { Strings = Array.Empty<GuitarString>() }
            && a.Strings.SequenceEqual(b.Strings);

        void ShowSet(StringSet set, StringSet saved)
        {
            _set = set;
            _savedSet = saved;
            _editor.Show(set, _units);
            UpdateUi();
        }

        void UpdateUi()
        {
            bool dirty = IsDirty;
            _dirtyLabel.Text = dirty ? "Unsaved changes" : "";
            _save.Enabled = dirty;
            _revert.Enabled = dirty;
            _delete.Enabled = _set.Id != null;
            _use.Enabled = _designOpen && _set.Id != null && !dirty;
            _use.ToolTip = !_designOpen ? "Open a design to use this set for its guitar."
                : _set.Id == null ? "Save the new string set first."
                : dirty ? "Save the changes to this string set first."
                : "Use this string set for the open guitar.";
        }

        void FillPicker(IReadOnlyList<LibraryItemInfo> sets, long? select)
        {
            _showing = true;
            try
            {
                LibraryItemUi.FillPicker(_picker, sets, select);
            }
            finally
            {
                _showing = false;
            }
        }

        // Loads a saved set from the library as the working copy and refreshes the picker.
        void Open(long id, IReadOnlyList<LibraryItemInfo> sets = null)
        {
            var (loaded, list) = WithLibrary(library => (library.LoadLibraryItem<StringSet>(id), sets ?? library.ListLibraryItems<StringSet>()));
            FillPicker(list, id);
            ShowSet(loaded, loaded);
        }

        void OnPicked()
        {
            if (_showing || _picker.SelectedKey == null)
                return;
            long id = long.Parse(_picker.SelectedKey, CultureInfo.InvariantCulture);
            if (id == _set.Id)
                return;
            if (!ConfirmDiscard())
            {
                // Cancelled (or the save failed): put the previous set back in the picker.
                _showing = true;
                _picker.SelectedKey = _set.Id?.ToString(CultureInfo.InvariantCulture);
                _showing = false;
                return;
            }
            if (!Run(() => Open(id)))
            {
                _showing = true;
                _picker.SelectedKey = _set.Id?.ToString(CultureInfo.InvariantCulture);
                _showing = false;
            }
        }

        // --- Buttons ---

        void NewSet()
        {
            if (!ConfirmDiscard())
                return;
            FillPicker(WithLibrary(library => library.ListLibraryItems<StringSet>()), null);
            ShowSet(BlankSet(), BlankSet());
        }

        // Why the working copy can't be saved, or null if it can.
        string SaveBlocker()
        {
            if (_editor.HasInputErrors)
                return "Some text couldn't be read. Fix the fields marked in Messages first.";
            var errors = _set.Validate(_units.LengthUnit, CultureInfo.CurrentCulture)
                .Where(m => m.Level == MessageLevel.Error).Select(m => m.Text).ToList();
            return errors.Count == 0 ? null : "The string set can't be saved yet:\n\n" + string.Join("\n", errors);
        }

        // Writes the working copy to the library. Returns whether it was saved (false after showing why not).
        bool Save()
        {
            if (!IsDirty && _set.Id != null)
                return true;
            return SaveSet(_set);
        }

        bool SaveSet(StringSet set)
        {
            string blocker = SaveBlocker();
            if (blocker != null)
            {
                MessageBox.Show(this, blocker, "GuitarForm", MessageBoxType.Warning);
                return false;
            }
            try
            {
                long id = WithLibrary(library => library.SaveLibraryItem(set));
                Open(id);
                LibraryItemChanged?.Invoke();
                return true;
            }
            catch (InvalidOperationException e)
            {
                MessageBox.Show(this, e.Message, "GuitarForm", MessageBoxType.Warning);
                return false;
            }
        }

        void SaveAs()
        {
            string blocker = SaveBlocker();
            if (blocker != null)
            {
                MessageBox.Show(this, blocker, "GuitarForm", MessageBoxType.Warning);
                return;
            }
            var values = FieldsDialog.Ask(this, "Save as",
                new[] { ("Manufacturer", _set.Manufacturer.Value ?? ""), ("Name", _set.Name.Value ?? "") },
                v =>
                {
                    if (v[0].Length == 0 || v[1].Length == 0)
                        return "Enter a manufacturer and a name.";
                    string name = $"{v[0]} {v[1]}";
                    return WithLibrary(library => library.ListLibraryItems<StringSet>())
                        .Any(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase))
                        ? $"There's already a string set called \"{name}\"." : null;
                });
            if (values == null)
                return;
            var copy = _set with
            {
                Id = null,
                Manufacturer = _set.Manufacturer with { Value = values[0] },
                Name = _set.Name with { Value = values[1] },
            };
            SaveSet(copy);
        }

        void Revert()
        {
            if (!IsDirty)
                return;
            if (!LibraryItemUi.ConfirmRevert(this, Kind, _set.Id == null, DisplayName(_savedSet)))
                return;
            if (_set.Id is long id)
                Open(id);
            else
                ShowSet(BlankSet(), BlankSet());
        }

        void Delete()
        {
            if (_set.Id is not long id)
                return;
            string name = DisplayName(_savedSet);
            if (!LibraryItemUi.ConfirmDelete<StringSet>(this, _libraryPath, Kind, name, id, IsDirty, out long? replacement))
                return;

            WithLibrary(library =>
            {
                library.DeleteLibraryItem(id, replacement);
                return 0;
            });
            var sets = WithLibrary(library => library.ListLibraryItems<StringSet>());
            if (sets.Count == 0)
            {
                FillPicker(sets, null);
                ShowSet(BlankSet(), BlankSet());
            }
            else
                Open(sets[0].Id, sets);
            LibraryItemChanged?.Invoke();
        }

        // --- Helpers ---

        T WithLibrary<T>(Func<Library, T> action) => LibraryItemUi.WithLibrary(_libraryPath, action);

        bool Run(Action action) => LibraryItemUi.Run(this, action);
    }
}
