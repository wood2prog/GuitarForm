using System;
using System.Collections.Generic;
using System.Linq;
using GuitarForm.Model.Parameters;
using GuitarForm.Model.Parts;

namespace GuitarForm.Data
{
    // The working copy of one saved guitar, kept apart from the panel: Rhino-free and UI-free. It mirrors what
    // GuitarFormPanel keeps for a design: edits change only Working and mark it Dirty; nothing reaches the library until
    // Save (or SaveAs). The library is opened for each call, from the path, as the rest of the plug-in does.
    public sealed class DesignSession
    {
        readonly string _libraryPath;
        readonly RecoveryStore _recovery;
        long? _legacyId;
        bool _pendingLegacy;

        // recovery is where unsaved work goes (RecoveryStore.DefaultFolder when not given).
        public DesignSession(string libraryPath, RecoveryStore recovery = null)
        {
            _libraryPath = libraryPath ?? throw new ArgumentNullException(nameof(libraryPath));
            _recovery = recovery ?? new RecoveryStore(RecoveryStore.DefaultFolder);
        }

        public Guitar Working { get; private set; }
        public bool IsOpen => Working != null;
        public bool Dirty { get; private set; }
        public long GuitarId { get; private set; }
        public string Uid { get; private set; }
        public string SavedName { get; private set; }

        // True once the user has been asked, or needn't be asked, whether Save overwrites this guitar or makes a new one
        // (the panel sets it for a guitar it just created or copied, and after a Save).
        public bool SavePrompted { get; set; }

        // The tag that Built objects carry for this guitar.
        public string BuildKey => IsOpen ? "guitar-" + Uid : null;

        // Tags that earlier builds used: the id of the design the guitar was converted from.
        public IReadOnlyList<string> LegacyBuildKeys { get; private set; } = Array.Empty<string>();

        // Shown on the Frets and Strings tabs when the guitar's fretwire or string set isn't in the library (null if it is).
        // Null when there's nothing to say.
        public string FretwireNote { get; set; }
        public string StringSetNote { get; set; }

        // Loads a saved guitar as the working copy, not dirty. Problems lists stored values that couldn't be read.
        // Pending is the recovery offered for the guitar, if it has one: its own file, or else (for a guitar converted
        // from a design) the design's old numeric file. Offer it, then call AcceptRecovery or DeclineRecovery. A file
        // that is corrupt is deleted; one that can't be read just now (locked, no access) is left alone and not offered.
        // With discardPrevious, the guitar that was open loses its unsaved-work recovery files once this one has
        // loaded (the user chose to discard that work). Throws KeyNotFoundException if there's no such guitar; the
        // session is left as it was, and nothing is discarded.
        public (IReadOnlyList<string> Problems, Recovery Pending) Open(long id, bool discardPrevious = false)
        {
            var previous = discardPrevious ? Current() : default;
            var problems = new List<string>();
            Load(id, problems);
            SavePrompted = false;
            ClearFiles(previous);

            var status = _recovery.TryRead(_libraryPath, BuildKey, out Recovery pending);
            if (status == RecoveryReadStatus.Corrupt)
                _recovery.Delete(_libraryPath, BuildKey);
            if (status is RecoveryReadStatus.Missing or RecoveryReadStatus.Corrupt && _legacyId is long legacy)
            {
                status = _recovery.TryReadLegacy(_libraryPath, legacy, out pending);
                if (status == RecoveryReadStatus.Corrupt)
                    _recovery.DeleteLegacy(_libraryPath, legacy);
                else if (status == RecoveryReadStatus.Ok)
                    _pendingLegacy = true;
            }
            return (problems, pending);
        }

        (string Uid, long? LegacyId) Current() => IsOpen ? (Uid, _legacyId) : default;

        void ClearFiles((string Uid, long? LegacyId) who)
        {
            if (who.Uid != null)
                _recovery.Delete(_libraryPath, "guitar-" + who.Uid);
            if (who.LegacyId is long legacy)
                _recovery.DeleteLegacy(_libraryPath, legacy);
        }

        // Takes the recovered work back as the working copy, unsaved. Its library items are read from the library again
        // (a missing one leaves a note). Throws FormatException, after deleting the unreadable file, if it can't be read.
        public void AcceptRecovery(Recovery recovery)
        {
            Require();
            GuitarDocument.ReadResult read;
            try
            {
                read = GuitarDocument.Read(recovery.Document);
            }
            catch (FormatException)
            {
                DeclineRecovery();
                throw;
            }
            var guitar = read.Guitar;
            // The ids are the library's own (the recovery was written for this library).
            long? wireId = recovery.FretwireId ?? read.Fretwire?.Id, setId = recovery.StringSetId ?? read.StringSet?.Id;
            var (set, wire, setMissing, wireMissing) = ResolveByIds(setId, wireId);
            Working = guitar with { Neck = guitar.Neck with { StringSet = set, Fretwire = wire } };
            SetNotes(setMissing, wireMissing);
            Dirty = true;
            if (_pendingLegacy)
            {
                // The work goes under the new key first; the legacy file is deleted only once that is written (if the
                // write throws, the legacy file stays and is offered again).
                FlushRecovery();
                _pendingLegacy = false;
                if (_legacyId is long legacy)
                    _recovery.DeleteLegacy(_libraryPath, legacy);
            }
        }

        // Throws away the offered recovery.
        public void DeclineRecovery()
        {
            _pendingLegacy = false;
            ClearRecovery();
        }

        // Writes the unsaved working copy to its recovery file; nothing happens if it is clean or nothing is open.
        // Throws IOException (and the like) if the file can't be written: the caller reports it.
        public void FlushRecovery()
        {
            if (!IsOpen || !Dirty)
                return;
            _recovery.Write(_libraryPath, BuildKey, Working.Name.Value, GuitarDocument.ToJson(Working),
                Working.Neck.Fretwire?.Id, Working.Neck.StringSet?.Id);
        }

        // Deletes the open guitar's recovery files (its unsaved work was saved or thrown away).
        public void ClearRecovery()
        {
            ClearFiles(Current());
        }

        // The user chose to throw away the unsaved work: deletes the open guitar's recovery files and marks the working
        // copy clean without reloading it, so nothing more is written for it. For use just before switching guitars
        // (or closing), when the switch is certain; prefer the discardPrevious options, which delete only after a
        // switch has succeeded. Revert reloads the saved guitar instead.
        public void DiscardWorkingCopy()
        {
            Require();
            ClearRecovery();
            _pendingLegacy = false;
            Dirty = false;
        }

        // "<saved name> copy", made unique: the panel's answer to the first Save's "save as a new guitar".
        public string UnusedCopyName()
        {
            Require();
            using var library = Library.Open(_libraryPath);
            return library.UnusedGuitarName(SavedName + " copy");
        }

        // True if a guitar other than the open one has the name (ignoring case): for the rename and Save as checks.
        public bool NameTaken(string name)
        {
            using var library = Library.Open(_libraryPath);
            return library.GuitarExists(name, IsOpen ? GuitarId : null);
        }

        // Deletes the recovery files of guitars that no longer exist.
        public void DeleteStaleRecovery()
        {
            IReadOnlyList<GuitarInfo> guitars;
            using (var library = Library.Open(_libraryPath))
                guitars = library.ListGuitars();
            _recovery.DeleteStale(_libraryPath, guitars.Where(g => !string.IsNullOrEmpty(g.Uid)).Select(g => "guitar-" + g.Uid));
        }

        // The working copy as a guitar document (version 3), with its string set and fretwire embedded.
        public string Export()
        {
            Require();
            return GuitarDocument.ToJson(Working);
        }

        // Saves a guitar document (a version 1 design, with its "newFields", or a version 3 guitar) as a new guitar and
        // opens it, not dirty. Its name is the document's, else fallbackName, made unique. Its string set and fretwire
        // are matched to the library: an id counts only if the item with it has the document's name (the id belongs to
        // the library the file came from), otherwise the name is looked for. One not found is left out, with a note.
        // Returns the problems found reading the document. Throws FormatException if it can't be read; the session is
        // then unchanged.
        public IReadOnlyList<string> Import(string json, string fallbackName, bool discardPrevious = false)
        {
            var previous = discardPrevious ? Current() : default;
            var read = GuitarDocument.Read(json);
            var guitar = read.Guitar;
            bool setMissing, wireMissing;
            long id;
            using (var library = Library.Open(_libraryPath))
            {
                var set = ResolveReference<StringSet>(library, read.StringSet, out setMissing);
                var wire = ResolveReference<Fretwire>(library, read.Fretwire, out wireMissing);
                string name = library.UnusedGuitarName(
                    string.IsNullOrWhiteSpace(guitar.Name.Value) ? fallbackName : guitar.Name.Value);
                guitar = guitar with
                {
                    Name = new TextParameter(Guitar.Definitions.Name, name),
                    Neck = guitar.Neck with { StringSet = set, Fretwire = wire },
                };
                id = library.CreateGuitar(guitar);
            }
            Load(id, null);
            SavePrompted = true;
            ClearFiles(previous);
            SetNotes(setMissing, wireMissing);
            return read.Problems;
        }

        // "Use for this guitar": the fretwire (null for none) becomes the working guitar's, as an unsaved edit.
        public void UseFretwire(Fretwire wire)
        {
            Apply(g => g with { Neck = g.Neck with { Fretwire = wire } });
            FretwireNote = null;
        }

        public void UseStringSet(StringSet set)
        {
            Apply(g => g with { Neck = g.Neck with { StringSet = set } });
            StringSetNote = null;
        }

        // Reads the working guitar's string set and fretwire from the library again, e.g. after one was edited. This
        // isn't an edit of the guitar. One that's gone leaves the guitar with none and a note.
        public void ReloadLibraryItems()
        {
            Require();
            var neck = Working.Neck;
            if (neck.StringSet?.Id == null && neck.Fretwire?.Id == null)
                return;
            var (set, wire, setMissing, wireMissing) = ResolveByIds(neck.StringSet?.Id, neck.Fretwire?.Id);
            Working = Working with { Neck = neck with { StringSet = set, Fretwire = wire } };
            SetNotes(setMissing, wireMissing);
        }

        void SetNotes(bool setMissing, bool wireMissing)
        {
            StringSetNote = setMissing ? "The string set this guitar used isn't in the library; choose another on the Strings tab." : null;
            FretwireNote = wireMissing ? "The fretwire this guitar used isn't in the library; choose another on the Frets tab." : null;
        }

        (StringSet Set, Fretwire Wire, bool SetMissing, bool WireMissing) ResolveByIds(long? setId, long? wireId)
        {
            using var library = Library.Open(_libraryPath);
            var set = ById<StringSet>(library, setId, out bool setMissing);
            var wire = ById<Fretwire>(library, wireId, out bool wireMissing);
            return (set, wire, setMissing, wireMissing);
        }

        static T ById<T>(Library library, long? id, out bool missing) where T : Part
        {
            missing = false;
            if (id is not long itemId)
                return null;
            try
            {
                return library.LoadLibraryItem<T>(itemId);
            }
            catch (Exception e) when (e is InvalidOperationException or KeyNotFoundException)
            {
                missing = true;
                return null;
            }
        }

        static string NameOf(Part item) => item switch { Fretwire f => f.DisplayName, StringSet s => s.DisplayName, _ => null };

        // By id, if the item with it has the reference's name; otherwise by name.
        static T ResolveReference<T>(Library library, GuitarDocument.ItemReference reference, out bool missing) where T : Part
        {
            missing = false;
            if (reference == null)
                return null;
            string name = reference.Name ?? "";
            if (reference.Id is long id)
            {
                var item = ById<T>(library, id, out _);
                if (item != null && string.Equals(NameOf(item), name, StringComparison.OrdinalIgnoreCase))
                    return item;
            }
            var match = library.ListLibraryItems<T>()
                .FirstOrDefault(i => name.Length > 0 && string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase));
            if (match == null)
            {
                missing = true;
                return null;
            }
            return library.LoadLibraryItem<T>(match.Id);
        }

        // Replaces the working copy with the edit's result. Dirty is set unless the result is the same object or equal
        // by record equality; parts holding arrays or lists compare those by reference, so an edit that rebuilds one
        // counts as a change even if the values are the same (a harmless extra "unsaved" mark, never a lost one).
        public void Apply(Func<Guitar, Guitar> edit)
        {
            Require();
            var result = edit(Working) ?? throw new InvalidOperationException("An edit can't produce no guitar.");
            if (ReferenceEquals(result, Working) || result.Equals(Working))
                return;
            Working = result;
            Dirty = true;
        }

        // Writes the working copy over the saved guitar. Throws InvalidOperationException if its name is empty or taken.
        public void Save()
        {
            Require();
            var guitar = Working;
            using (var library = Library.Open(_libraryPath))
                library.SaveGuitar(GuitarId, guitar);
            SavedName = guitar.Name.Value?.Trim();
            Working = guitar with { Name = new TextParameter(Guitar.Definitions.Name, SavedName) };
            Dirty = false;
            ClearRecovery();
            SavePrompted = true;
        }

        // Saves the working copy as a new guitar with the name and switches to it; the original stays as it was saved.
        // Throws InvalidOperationException if the name is empty or taken (the session is then unchanged). The new row is
        // created before the switch; if loading it then fails, the row stays in the library but the session stays on the
        // old guitar, still dirty, with its recovery files. They are deleted only after the switch has worked.
        public void SaveAs(string name)
        {
            Require();
            var guitar = Working with { Name = new TextParameter(Guitar.Definitions.Name, name?.Trim()) };
            long id;
            using (var library = Library.Open(_libraryPath))
                id = library.CreateGuitar(guitar);
            var previous = Current();
            Load(id, null);
            SavePrompted = true;
            ClearFiles(previous); // The unsaved work now belongs to the new guitar.
        }

        // Discards the edits by reloading the saved guitar.
        public void Revert()
        {
            Require();
            Load(GuitarId, null); // If this throws, the recovery files are kept.
            ClearRecovery();
        }

        public void Close()
        {
            Working = null;
            Dirty = false;
            GuitarId = 0;
            Uid = null;
            SavedName = null;
            SavePrompted = false;
            LegacyBuildKeys = Array.Empty<string>();
            _legacyId = null;
            _pendingLegacy = false;
            FretwireNote = StringSetNote = null;
        }

        // Saves a new guitar with the name and opens it. Values that aren't given stay unset. Throws
        // InvalidOperationException if the name is empty or taken.
        public void CreateNew(string name, double? bodyMm, double? scaleMm, int? neckJoinFret, bool discardPrevious = false)
        {
            var previous = discardPrevious ? Current() : default;
            var guitar = new Guitar
            {
                Name = new TextParameter(Guitar.Definitions.Name, name?.Trim()),
                ScaleLength = new NumberParameter(Guitar.Definitions.ScaleLength, scaleMm),
                NeckJoinFret = new CountParameter(Guitar.Definitions.NeckJoinFret, neckJoinFret),
                Body = new Body { Length = new NumberParameter(Body.Definitions.Length, bodyMm) },
            };
            long id;
            using (var library = Library.Open(_libraryPath))
                id = library.CreateGuitar(guitar);
            Load(id, null);
            SavePrompted = true;
            ClearFiles(previous);
        }

        // Copies the saved guitar (not the working copy) and opens the copy, as the panel does after its discard question.
        public void Copy(bool discardPrevious = false)
        {
            Require();
            var previous = discardPrevious ? Current() : default;
            long copy;
            using (var library = Library.Open(_libraryPath))
                copy = library.CopyGuitar(GuitarId);
            Load(copy, null);
            SavePrompted = true;
            ClearFiles(previous);
        }

        // Deletes the saved guitar, with any unsaved changes, and closes the session.
        public void Delete()
        {
            Require();
            using (var library = Library.Open(_libraryPath))
                library.DeleteGuitar(GuitarId);
            ClearRecovery();
            Close();
        }

        void Require()
        {
            if (!IsOpen)
                throw new InvalidOperationException("No guitar is open.");
        }

        void Load(long id, ICollection<string> problems)
        {
            Guitar guitar;
            GuitarInfo info;
            using (var library = Library.Open(_libraryPath))
            {
                guitar = library.LoadGuitar(id, problems);
                info = library.ListGuitars().FirstOrDefault(g => g.Id == id)
                    ?? throw new KeyNotFoundException($"There's no guitar with id {id}.");
            }
            Working = guitar;
            GuitarId = id;
            Uid = info.Uid;
            SavedName = info.Name;
            LegacyBuildKeys = info.LegacyDesignId is long legacy
                ? new[] { legacy.ToString(System.Globalization.CultureInfo.InvariantCulture) }
                : Array.Empty<string>();
            _legacyId = info.LegacyDesignId;
            _pendingLegacy = false;
            FretwireNote = StringSetNote = null;
            Dirty = false;
        }
    }
}
