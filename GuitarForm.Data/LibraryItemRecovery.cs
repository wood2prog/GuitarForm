using System;
using System.Linq;
using GuitarForm.Model.Parts;

namespace GuitarForm.Data
{
    // The recovery keys other than guitars' ("guitar-<uid>"). DesignSession.DeleteStaleRecovery always keeps these.
    public static class RecoveryKeys
    {
        public const string StringSetWork = "stringset-work";
        public const string FretwireWork = "fretwire-work";
    }

    // A recovered string set or fretwire: the item (its Id is not set; use SavedId), the library id of the saved item
    // it was an edit of (null for a new, never saved item), the saved item's name (or the working name for a new
    // one) and when the recovery file was written.
    public sealed record RecoveredItem<T>(T Item, long? SavedId, string Name, DateTime SavedAtUtc);

    // Unsaved edits of a string set or fretwire from the Strings and Frets tabs, one recovery file for each kind per
    // library, so a crash doesn't lose them. The item is stored as a version 3 guitar document holding a neck with
    // just that item. It never touches the library.
    public sealed class LibraryItemRecovery
    {
        readonly RecoveryStore _store;
        readonly string _libraryPath;

        public LibraryItemRecovery(RecoveryStore store, string libraryPath)
        {
            _store = store;
            _libraryPath = libraryPath;
        }

        // savedId is the library id of the saved item being edited (null for a new item); name is that item's name,
        // or the working name for a new one.
        public void Write(StringSet set, long? savedId, string name) =>
            _store.Write(_libraryPath, RecoveryKeys.StringSetWork, name,
                GuitarDocument.ToJson(new Guitar { Neck = new Neck { StringSet = set } }), stringSetId: savedId);

        public void Write(Fretwire wire, long? savedId, string name) =>
            _store.Write(_libraryPath, RecoveryKeys.FretwireWork, name,
                GuitarDocument.ToJson(new Guitar { Neck = new Neck { Fretwire = wire } }), fretwireId: savedId);

        // Ok gives the item; Corrupt covers a bad file, another library's file or a document without the item.
        public RecoveryReadStatus TryReadStringSet(out RecoveredItem<StringSet> item)
        {
            item = null;
            var status = _store.TryRead(_libraryPath, RecoveryKeys.StringSetWork, out var recovery);
            if (status != RecoveryReadStatus.Ok)
                return status;
            var set = Parse(recovery.Document)?.Neck?.StringSet;
            if (set == null)
                return RecoveryReadStatus.Corrupt;
            item = new RecoveredItem<StringSet>(set, recovery.StringSetId, recovery.Name, recovery.SavedAtUtc);
            return RecoveryReadStatus.Ok;
        }

        public RecoveryReadStatus TryReadFretwire(out RecoveredItem<Fretwire> item)
        {
            item = null;
            var status = _store.TryRead(_libraryPath, RecoveryKeys.FretwireWork, out var recovery);
            if (status != RecoveryReadStatus.Ok)
                return status;
            var wire = Parse(recovery.Document)?.Neck?.Fretwire;
            if (wire == null)
                return RecoveryReadStatus.Corrupt;
            item = new RecoveredItem<Fretwire>(wire, recovery.FretwireId, recovery.Name, recovery.SavedAtUtc);
            return RecoveryReadStatus.Ok;
        }

        public void ClearStringSet() => _store.Delete(_libraryPath, RecoveryKeys.StringSetWork);

        public void ClearFretwire() => _store.Delete(_libraryPath, RecoveryKeys.FretwireWork);

        // True if the two have the same content, ignoring the library Id (a recovered item has none).
        public static bool SameContent(StringSet a, StringSet b) =>
            a with { Id = null, Strings = Array.Empty<GuitarString>() } == b with { Id = null, Strings = Array.Empty<GuitarString>() }
            && a.Strings.SequenceEqual(b.Strings);

        public static bool SameContent(Fretwire a, Fretwire b) => a with { Id = null } == b with { Id = null };

        static Guitar Parse(string document)
        {
            try
            {
                return GuitarDocument.FromJson(document);
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
