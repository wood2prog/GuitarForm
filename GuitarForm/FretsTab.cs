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
    // The Frets tab: the library of fretwires, which can be edited with no design open. A picker of the saved
    // fretwires, New, Save, Save as…, Revert and Delete, and the FretwireEditor showing the working copy. Edits change
    // only the working copy (_item); nothing reaches the library until Save. There are no recovery files for it.
    sealed class FretsTab : Panel
    {
        const string Kind = "fretwire";

        readonly FretwireEditor _editor = new FretwireEditor();
        readonly DropDown _picker = new DropDown();
        readonly Label _dirtyLabel = new Label { Text = "", VerticalAlignment = VerticalAlignment.Center };
        readonly Button _save = new Button { Text = "&Save" };
        readonly Button _saveAs = new Button { Text = "Save &as…" };
        readonly Button _revert = new Button { Text = "Re&vert" };
        readonly Button _delete = new Button { Text = "&Delete" };
        readonly Button _new = new Button { Text = "&New" };
        readonly Button _use = new Button { Text = "&Use for this guitar" };
        readonly Button _useNone = new Button { Text = "Use &no fretwire" };
        readonly Label _usedBy = new Label { Text = "", VerticalAlignment = VerticalAlignment.Center };
        readonly Func<string> _libraryPath;
        bool _designOpen;
        DisplayUnits _units = DisplayUnits.Current;
        // The working copy (its Id is null until it's saved) and the saved fretwire it was loaded as, to tell if it changed.
        Fretwire _item = new Fretwire();
        Fretwire _savedItem = new Fretwire();
        bool _showing;

        public FretsTab(Func<string> libraryPath)
        {
            _libraryPath = libraryPath;
            _editor.Changed += item =>
            {
                _item = item;
                UpdateUi();
            };
            _picker.SelectedIndexChanged += (_, _) => OnPicked();
            _save.Click += (_, _) => Run(() => Save());
            _saveAs.Click += (_, _) => Run(SaveAs);
            _revert.Click += (_, _) => Run(Revert);
            _delete.Click += (_, _) => Run(Delete);
            _new.Click += (_, _) => Run(NewItem);
            _use.Click += (_, _) => Run(UseForGuitar);
            _useNone.Click += (_, _) => Run(() =>
            {
                if (_designOpen)
                    UseRequested?.Invoke(null);
            });

            var buttons = new StackLayout { Orientation = Orientation.Horizontal, Spacing = UiMetrics.Gap };
            foreach (var button in new[] { _new, _save, _saveAs, _revert, _delete, _use, _useNone })
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
                Rows = { new TableRow(new Label { Text = "Fretwire", VerticalAlignment = VerticalAlignment.Center }, new TableCell(_picker, true), _dirtyLabel) },
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
                global::Rhino.RhinoApp.WriteLine($"GuitarForm: couldn't list the fretwires: {e.Message}");
                ShowItem(new Fretwire(), new Fretwire());
            }
        }

        // The editor's messages, for the panel's Messages bar and the tab's title.
        public TabMessages Messages => _editor.Messages;

        // The working copy's saved fretwire, as stored in the library; null when the working copy is a new, unsaved one.
        // (For "Use for this guitar".)
        public Fretwire LoadSelectedSavedItem() =>
            _item.Id is long id ? WithLibrary(library => library.LoadLibraryItem<Fretwire>(id)) : null;

        // The id of the saved fretwire being shown, or null for a new one.
        public long? SelectedId => _item.Id;

        public bool IsDirty => !_item.Equals(_savedItem);

        // Raised when the user asks to use the shown (saved, unchanged) fretwire for the open guitar, with it as
        // stored, or to use none (null).
        public event Action<Fretwire> UseRequested;

        // Raised after a fretwire was saved or deleted, so the owner can refresh a copy of it that it holds.
        public event Action LibraryItemChanged;

        // The owner tells the tab whether a design is open and what its guitar uses: a fretwire (null for none) and,
        // when it has none, an optional note on why (e.g. the saved fretwire no longer exists).
        public void ShowAssigned(bool designOpen, Fretwire assigned, string note = null)
        {
            _designOpen = designOpen;
            _usedBy.Text = !designOpen ? ""
                : "Used by this guitar: " + (assigned == null ? "none" : EmptyAs(DisplayName(assigned), "(unnamed fretwire)")) +
                  (assigned == null && note != null ? $" ({note})" : "");
            UpdateUi();
        }

        static string EmptyAs(string text, string fallback) => text.Length == 0 ? fallback : text;

        void UseForGuitar()
        {
            if (!_designOpen || _item.Id == null || IsDirty)
                return;
            var saved = LoadSelectedSavedItem();
            if (saved != null)
                UseRequested?.Invoke(saved);
        }

        // The document's units changed.
        public void RefreshUnits()
        {
            _units = DisplayUnits.Current;
            _editor.Refresh(_units);
        }

        // The library changed (another file, a restore): shows its first fretwire. Unsaved work is dropped, so the
        // caller asks ConfirmDiscard first.
        public void Reload()
        {
            var items = WithLibrary(library => library.ListLibraryItems<Fretwire>());
            if (items.Count == 0)
            {
                FillPicker(items, null);
                ShowItem(new Fretwire(), new Fretwire());
                return;
            }
            Open(items[0].Id, items);
        }

        // True when it's fine to replace the working copy: no unsaved changes, or the user saved or discarded them.
        public bool ConfirmDiscard() =>
            !IsDirty || LibraryItemUi.ConfirmDiscard(this, Kind, DisplayName(_item), Save);

        // Drops unsaved changes by showing the saved fretwire again (no question: the caller has asked).
        public void DiscardChanges()
        {
            if (!IsDirty)
                return;
            try
            {
                if (_item.Id is long id)
                    Open(id);
                else
                    ShowItem(new Fretwire(), new Fretwire());
            }
            catch (Exception e)
            {
                global::Rhino.RhinoApp.WriteLine($"GuitarForm: couldn't reload the fretwire: {e.Message}");
            }
        }

        // --- Working copy ---

        // "Manufacturer Product", or just the product code when there's no manufacturer (as the library names it).
        static string DisplayName(Fretwire item) =>
            $"{item.Manufacturer.Value} {item.ProductCode.Value}".Trim();

        void ShowItem(Fretwire item, Fretwire saved)
        {
            _item = item;
            _savedItem = saved;
            _editor.Show(item, _units);
            UpdateUi();
        }

        void UpdateUi()
        {
            bool dirty = IsDirty;
            _dirtyLabel.Text = dirty ? "Unsaved changes" : "";
            _save.Enabled = dirty;
            _revert.Enabled = dirty;
            _delete.Enabled = _item.Id != null;
            _use.Enabled = _designOpen && _item.Id != null && !dirty;
            _use.ToolTip = !_designOpen ? "Open a design to use this fretwire for its guitar."
                : _item.Id == null ? "Save the new fretwire first."
                : dirty ? "Save the changes to this fretwire first."
                : "Use this fretwire for the open guitar.";
            _useNone.Enabled = _designOpen;
            _useNone.ToolTip = _designOpen ? "Draw the open guitar's frets with no fretwire." : "Open a design first.";
        }

        void FillPicker(IReadOnlyList<LibraryItemInfo> items, long? select)
        {
            _showing = true;
            try
            {
                LibraryItemUi.FillPicker(_picker, items, select);
            }
            finally
            {
                _showing = false;
            }
        }

        // Loads a saved fretwire from the library as the working copy and refreshes the picker.
        void Open(long id, IReadOnlyList<LibraryItemInfo> items = null)
        {
            var (loaded, list) = WithLibrary(library => (library.LoadLibraryItem<Fretwire>(id), items ?? library.ListLibraryItems<Fretwire>()));
            FillPicker(list, id);
            ShowItem(loaded, loaded);
        }

        void OnPicked()
        {
            if (_showing || LibraryItemUi.PickedId(_picker) is not long id)
                return;
            if (id == _item.Id)
                return;
            if (!ConfirmDiscard())
            {
                // Cancelled (or the save failed): put the previous fretwire back in the picker.
                _showing = true;
                _picker.SelectedKey = _item.Id?.ToString(CultureInfo.InvariantCulture);
                _showing = false;
                return;
            }
            if (!Run(() => Open(id)))
                ReselectCurrent();
        }

        void ReselectCurrent()
        {
            _showing = true;
            _picker.SelectedKey = _item.Id?.ToString(CultureInfo.InvariantCulture);
            _showing = false;
        }

        // --- Buttons ---

        void NewItem()
        {
            if (!ConfirmDiscard())
                return;
            FillPicker(WithLibrary(library => library.ListLibraryItems<Fretwire>()), null);
            ShowItem(new Fretwire(), new Fretwire());
        }

        // Why the working copy can't be saved, or null if it can.
        string SaveBlocker(Fretwire item)
        {
            if (_editor.HasInputErrors)
                return "Some text couldn't be read. Fix the fields marked in Messages first.";
            if (string.IsNullOrWhiteSpace(item.ProductCode.Value))
                return $"Enter a {item.ProductCode.Label} first: it names the fretwire in the library.";
            var errors = item.Validate(_units.LengthUnit, CultureInfo.CurrentCulture)
                .Where(m => m.Level == MessageLevel.Error).Select(m => m.Text).ToList();
            return errors.Count == 0 ? null : "The fretwire can't be saved yet:\n\n" + string.Join("\n", errors);
        }

        // Writes the working copy to the library. Returns whether it was saved (false after showing why not).
        bool Save()
        {
            if (!IsDirty && _item.Id != null)
                return true;
            return SaveItem(_item);
        }

        bool SaveItem(Fretwire item)
        {
            string blocker = SaveBlocker(item);
            if (blocker != null)
            {
                LibraryItemUi.Warn(this, blocker);
                return false;
            }
            try
            {
                long id = WithLibrary(library => library.SaveLibraryItem(item));
                Open(id);
                LibraryItemChanged?.Invoke();
                return true;
            }
            catch (InvalidOperationException e)
            {
                LibraryItemUi.Warn(this, e.Message);
                return false;
            }
        }

        void SaveAs()
        {
            // Checked before asking, so a copy of unreadable text isn't offered.
            if (_editor.HasInputErrors)
            {
                LibraryItemUi.Warn(this, SaveBlocker(_item));
                return;
            }
            var values = FieldsDialog.Ask(this, "Save as",
                new[] { ("Manufacturer (optional)", _item.Manufacturer.Value ?? ""), ("Product code", _item.ProductCode.Value ?? "") },
                v =>
                {
                    if (v[1].Length == 0)
                        return "Enter a product code.";
                    string name = $"{v[0]} {v[1]}".Trim();
                    return WithLibrary(library => library.ListLibraryItems<Fretwire>())
                        .Any(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase))
                        ? $"There's already a fretwire called \"{name}\"." : null;
                });
            if (values == null)
                return;
            var copy = _item with
            {
                Id = null,
                Manufacturer = _item.Manufacturer with { Value = values[0].Length == 0 ? null : values[0] },
                ProductCode = _item.ProductCode with { Value = values[1] },
            };
            SaveItem(copy);
        }

        void Revert()
        {
            if (!IsDirty)
                return;
            if (!LibraryItemUi.ConfirmRevert(this, Kind, _item.Id == null, DisplayName(_savedItem)))
                return;
            if (_item.Id is long id)
                Open(id);
            else
                ShowItem(new Fretwire(), new Fretwire());
        }

        void Delete()
        {
            if (_item.Id is not long id)
                return;
            if (!LibraryItemUi.ConfirmDelete<Fretwire>(this, _libraryPath, Kind, DisplayName(_savedItem), id, IsDirty, out long? replacement))
                return;

            WithLibrary(library =>
            {
                library.DeleteLibraryItem(id, replacement);
                return 0;
            });
            var items = WithLibrary(library => library.ListLibraryItems<Fretwire>());
            if (items.Count == 0)
            {
                FillPicker(items, null);
                ShowItem(new Fretwire(), new Fretwire());
            }
            else
                Open(items[0].Id, items);
            LibraryItemChanged?.Invoke();
        }

        // --- Helpers ---

        T WithLibrary<T>(Func<Library, T> action) => LibraryItemUi.WithLibrary(_libraryPath, action);

        bool Run(Action action) => LibraryItemUi.Run(this, action);
    }
}
