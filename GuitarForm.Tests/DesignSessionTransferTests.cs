using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using GuitarForm.Data;
using GuitarForm.Model;
using GuitarForm.Model.Parameters;
using GuitarForm.Model.Parts;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    // The design session's import, export, recovery and library-item handling.
    public class DesignSessionTransferTests
    {
        string _folder, _path;
        RecoveryStore _recovery;
        DesignSession _session;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "GuitarFormTests", Guid.NewGuid().ToString("N"));
            _path = Path.Combine(_folder, "GuitarForm.db");
            using (Library.Open(_path)) { }
            _recovery = new RecoveryStore(Path.Combine(_folder, "recovery"));
            _session = new DesignSession(_path, _recovery);
        }

        [TearDown]
        public void TearDown()
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(_folder))
                Directory.Delete(_folder, true);
        }

        long Create(string name, long? legacyId = null)
        {
            var om = TestGuitars.OM(name);
            long id;
            using (var library = Library.Open(_path))
                id = library.CreateGuitar(om with { Neck = om.Neck with { Fretwire = null, StringSet = null } });
            if (legacyId != null)
            {
                using var connection = new SqliteConnection("Data Source=" + _path + ";Pooling=False");
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = $"UPDATE parts SET legacy_design_id = {legacyId} WHERE id = {id}";
                command.ExecuteNonQuery();
            }
            return id;
        }

        (long SetId, long WireId) SaveItems()
        {
            using var library = Library.Open(_path);
            return (library.SaveLibraryItem(TestGuitars.StringSet), library.SaveLibraryItem(TestGuitars.Fretwire));
        }

        static string V1(string name, JsonObject newFields = null)
        {
            var design = new GuitarDesign
            {
                Name = name,
                Instrument = new Instrument { ScaleLength = 650, NeckJoinFret = 14 },
                Body = new GuitarForm.Model.Body { BodyLength = 480 },
            };
            var document = JsonNode.Parse(DesignDocument.ToJson(design)).AsObject();
            if (newFields != null)
                document["newFields"] = JsonNode.Parse(newFields.ToJsonString());
            return document.ToJsonString();
        }

        static Guitar Scale(Guitar g, double mm) => g with { ScaleLength = new NumberParameter(Guitar.Definitions.ScaleLength, mm) };

        [Test]
        public void Import_V1WithMissingStringSet_NotesItAndLeavesNone()
        {
            var problems = _session.Import(V1("Old", new JsonObject
            {
                ["StringSetId"] = "7",
                ["StringSetName"] = "Savarez 510",
            }), "file");
            Assert.That(problems, Is.Empty);
            Assert.That(_session.IsOpen && !_session.Dirty && _session.SavePrompted);
            Assert.That(_session.SavedName, Is.EqualTo("Old"));
            Assert.That(_session.Working.ScaleLength.Value, Is.EqualTo(650));
            Assert.That(_session.Working.Neck.StringSet, Is.Null);
            Assert.That(_session.StringSetNote, Does.Contain("no longer in the library"));
            Assert.That(_session.FretwireNote, Is.Null);
        }

        [Test]
        public void Import_V1_ResolvesByIdWithMatchingNameThenByName_AndConvertsNewFields()
        {
            var (setId, _) = SaveItems();
            string setName = TestGuitars.StringSet.DisplayName;
            var fields = new JsonObject
            {
                ["Markers"] = "5 12:2",
                ["StringSetId"] = setId.ToString(),
                ["StringSetName"] = setName,
                ["FretwireId"] = "999",
                ["FretwireName"] = TestGuitars.Fretwire.DisplayName.ToLowerInvariant(),
            };
            _session.Import(V1("Old", fields), "file");
            Assert.That(_session.Working.Neck.Fretboard.Markers, Has.Count.EqualTo(2));
            Assert.That(_session.Working.Neck.StringSet.Id, Is.EqualTo(setId));
            Assert.That(_session.Working.Neck.Fretwire, Is.Not.Null); // The id is wrong, the name matches.
            Assert.That(_session.StringSetNote, Is.Null);
            Assert.That(_session.FretwireNote, Is.Null);

            // An id whose item has another name doesn't count.
            fields["StringSetName"] = "Someone Else";
            _session.Import(V1("Other", fields), "file");
            Assert.That(_session.Working.Neck.StringSet, Is.Null);
            Assert.That(_session.StringSetNote, Is.Not.Null);
        }

        [Test]
        public void Import_NameIsMadeUnique_AndFallbackUsedWhenUnnamed()
        {
            Create("Old");
            _session.Import(V1("Old"), "file");
            Assert.That(_session.SavedName, Is.EqualTo("Old 2"));
            var unnamed = JsonNode.Parse(V1("x")).AsObject();
            unnamed["design"].AsObject().Remove("name");
            _session.Import(unnamed.ToJsonString(), "from file");
            Assert.That(_session.SavedName, Is.EqualTo("from file"));
        }

        [Test]
        public void Import_NotADocument_ThrowsAndLeavesSessionAlone()
        {
            long id = Create("A");
            _session.Open(id);
            Assert.Throws<FormatException>(() => _session.Import("{}", "x"));
            Assert.That(_session.GuitarId, Is.EqualTo(id));
        }

        [Test]
        public void ExportThenImport_V3_RoundTrips()
        {
            SaveItems();
            long id = Create("A");
            _session.Open(id);
            using (var library = Library.Open(_path))
            {
                var set = library.LoadLibraryItem<StringSet>(library.ListLibraryItems<StringSet>().Single().Id);
                var wire = library.LoadLibraryItem<Fretwire>(library.ListLibraryItems<Fretwire>().Single().Id);
                _session.UseStringSet(set);
                _session.UseFretwire(wire);
            }
            string json = _session.Export();
            var exported = _session.Working;

            _session.Import(json, "file");
            Assert.That(_session.SavedName, Is.EqualTo("A 2"));
            Assert.That(_session.StringSetNote, Is.Null);
            Assert.That(_session.FretwireNote, Is.Null);
            TestGuitars.AssertSameValues(_session.Working with { Name = exported.Name }, exported);
            Assert.That(_session.Working.Neck.StringSet.Id, Is.Not.Null);
        }

        [Test]
        public void Recovery_FlushOnlyWhenDirty_OfferedAndAccepted()
        {
            long id = Create("A");
            _session.Open(id);
            _session.FlushRecovery();
            Assert.That(_recovery.Exists(_path, _session.BuildKey), Is.False);

            _session.Apply(g => Scale(g, 650));
            _session.FlushRecovery();
            Assert.That(_recovery.Exists(_path, _session.BuildKey));

            var second = new DesignSession(_path, _recovery);
            var (_, pending) = second.Open(id);
            Assert.That(pending, Is.Not.Null);
            Assert.That(second.Working.ScaleLength.Value, Is.EqualTo(645));
            second.AcceptRecovery(pending);
            Assert.That(second.Dirty);
            Assert.That(second.Working.ScaleLength.Value, Is.EqualTo(650));
        }

        [Test]
        public void Recovery_DeclineDeletesIt_AndSaveRevertDeleteClearIt()
        {
            long id = Create("A");
            _session.Open(id);
            _session.Apply(g => Scale(g, 650));
            _session.FlushRecovery();
            _session.DeclineRecovery();
            Assert.That(_recovery.Exists(_path, _session.BuildKey), Is.False);

            foreach (var end in new Action[] { _session.Save, _session.Revert, _session.Delete })
            {
                _session.Open(Create("B" + Guid.NewGuid().ToString("N")));
                _session.Apply(g => Scale(g, 650));
                string key = _session.BuildKey;
                _session.FlushRecovery();
                Assert.That(_recovery.Exists(_path, key));
                end();
                Assert.That(_recovery.Exists(_path, key), Is.False);
            }
        }

        [Test]
        public void Recovery_KeepsItsLibraryItems()
        {
            var (setId, wireId) = SaveItems();
            long id = Create("A");
            _session.Open(id);
            using (var library = Library.Open(_path))
            {
                _session.UseStringSet(library.LoadLibraryItem<StringSet>(setId));
                _session.UseFretwire(library.LoadLibraryItem<Fretwire>(wireId));
            }
            _session.FlushRecovery();
            var second = new DesignSession(_path, _recovery);
            var pending = second.Open(id).Pending;
            second.AcceptRecovery(pending);
            Assert.That(second.Working.Neck.StringSet.Id, Is.EqualTo(setId));
            Assert.That(second.Working.Neck.Fretwire.Id, Is.EqualTo(wireId));
        }

        [Test]
        public void LegacyRecovery_IsFoundThroughTheLegacyDesignId()
        {
            long id = Create("A", 42);
            _recovery.Write(_path, 42L, "Old", V1("A", new JsonObject { ["Markers"] = "5" }));
            var (_, pending) = _session.Open(id);
            Assert.That(pending, Is.Not.Null);
            _session.AcceptRecovery(pending);
            Assert.That(_session.Dirty);
            Assert.That(_session.Working.ScaleLength.Value, Is.EqualTo(650));
            Assert.That(_session.Working.Neck.Fretboard.Markers, Has.Count.EqualTo(1));
            Assert.That(_recovery.Exists(_path, 42L), Is.False);
            Assert.That(_recovery.Exists(_path, _session.BuildKey)); // The work lives on under the new key.
        }

        [Test]
        public void LegacyRecovery_AcceptKeepsTheLegacyFileIfTheNewOneCantBeWritten()
        {
            long id = Create("A", 42);
            _recovery.Write(_path, 42L, "Old", V1("A"));
            string legacyFile = Directory.GetFiles(Path.Combine(_folder, "recovery")).Single();
            string prefix = Path.GetFileName(legacyFile).Split('-')[0];
            var (_, pending) = _session.Open(id);
            // A directory where the new file belongs makes the write fail.
            Directory.CreateDirectory(Path.Combine(_folder, "recovery", prefix + "-" + _session.BuildKey + ".json"));
            Assert.That(() => _session.AcceptRecovery(pending), Throws.InstanceOf<Exception>());
            Assert.That(_recovery.Exists(_path, 42L));
        }

        [Test]
        public void Open_CorruptRecoveryIsDeleted_ButAnUnreadableOneIsLeftAlone()
        {
            long id = Create("A");
            _session.Open(id);
            string key = _session.BuildKey;
            _recovery.Write(_path, key, "x", "{}");
            string file = Directory.GetFiles(Path.Combine(_folder, "recovery")).Single();

            using (new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                Assert.That(_session.Open(id).Pending, Is.Null);
            }
            Assert.That(File.Exists(file), "a locked file is not deleted");
            Assert.That(_session.Open(id).Pending, Is.Not.Null, "and is offered once it can be read");

            File.WriteAllText(file, "not json");
            Assert.That(_session.Open(id).Pending, Is.Null);
            Assert.That(File.Exists(file), Is.False);
        }

        [Test]
        public void SaveAs_AndRevert_ClearTheRecoveryFile()
        {
            long id = Create("A");
            _session.Open(id);
            string oldKey = _session.BuildKey;
            _session.Apply(g => g with { ScaleLength = new NumberParameter(Guitar.Definitions.ScaleLength, 650) });
            _session.FlushRecovery();
            _session.Revert();
            Assert.That(_recovery.Exists(_path, oldKey), Is.False);

            _session.Apply(g => g with { ScaleLength = new NumberParameter(Guitar.Definitions.ScaleLength, 650) });
            _session.FlushRecovery();
            _session.SaveAs("B");
            Assert.That(_recovery.Exists(_path, oldKey), Is.False);
        }

        [Test]
        public void Save_TrimsTheWorkingName_SoItMatchesTheRow()
        {
            _session.Open(Create("A"));
            _session.Apply(g => g with { Name = new TextParameter(Guitar.Definitions.Name, "  B  ") });
            _session.Save();
            Assert.That(_session.Working.Name.Value, Is.EqualTo("B"));
            Assert.That(_session.Dirty, Is.False);
        }

        [Test]
        public void DiscardWorkingCopy_DeletesTheRecoveryAndMarksClean()
        {
            _session.Open(Create("A"));
            _session.Apply(g => g with { ScaleLength = new NumberParameter(Guitar.Definitions.ScaleLength, 650) });
            _session.FlushRecovery();
            _session.DiscardWorkingCopy();
            Assert.That(_session.Dirty, Is.False);
            Assert.That(_recovery.Exists(_path, _session.BuildKey), Is.False);
        }

        [Test]
        public void Switching_WithDiscardPrevious_DeletesThePreviousRecoveryOnlyAfterTheSwitch()
        {
            long a = Create("A"), b = Create("B");
            _session.Open(a);
            string keyA = _session.BuildKey;
            _session.Apply(g => g with { ScaleLength = new NumberParameter(Guitar.Definitions.ScaleLength, 650) });
            _session.FlushRecovery();

            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => _session.Open(9999, true));
            Assert.That(_recovery.Exists(_path, keyA), "a failed switch discards nothing");

            _session.Open(b);
            Assert.That(_recovery.Exists(_path, keyA), "without the option it stays");
            _session.Open(a);
            _session.Apply(g => g with { ScaleLength = new NumberParameter(Guitar.Definitions.ScaleLength, 651) });
            _session.FlushRecovery();
            _session.Open(b, true);
            Assert.That(_recovery.Exists(_path, keyA), Is.False);

            _session.Open(a);
            _session.Apply(g => g with { ScaleLength = new NumberParameter(Guitar.Definitions.ScaleLength, 652) });
            _session.FlushRecovery();
            _session.CreateNew("C", null, null, null, true);
            Assert.That(_recovery.Exists(_path, keyA), Is.False);
        }

        [Test]
        public void UnusedCopyName_AndNameTaken()
        {
            long a = Create("A");
            Create("A copy");
            _session.Open(a);
            Assert.That(_session.UnusedCopyName(), Is.EqualTo("A copy 2"));
            Assert.That(_session.NameTaken("a"), Is.False, "its own name");
            Assert.That(_session.NameTaken(" A COPY "), Is.True);
            Assert.That(_session.NameTaken("Free"), Is.False);
        }

        [Test]
        public void LegacyRecovery_DeclineDeletesTheFile()
        {
            long id = Create("A", 42);
            _recovery.Write(_path, 42L, "Old", V1("A"));
            _session.Open(id);
            _session.DeclineRecovery();
            Assert.That(_recovery.Exists(_path, 42L), Is.False);
            Assert.That(_session.Open(id).Pending, Is.Null);
        }

        [Test]
        public void DeleteStaleRecovery_KeepsOnlyGuitarsThatExist()
        {
            long id = Create("A");
            _session.Open(id);
            _session.Apply(g => Scale(g, 650));
            _session.FlushRecovery();
            _recovery.Write(_path, "guitar-gone", "Gone", V1("G"));
            _session.DeleteStaleRecovery();
            Assert.That(_recovery.Exists(_path, _session.BuildKey));
            Assert.That(_recovery.Exists(_path, "guitar-gone"), Is.False);
        }

        [Test]
        public void ReloadLibraryItems_ReadsEditsAndNotesDeletedItems()
        {
            var (setId, wireId) = SaveItems();
            _session.Open(Create("A"));
            using (var library = Library.Open(_path))
            {
                _session.UseStringSet(library.LoadLibraryItem<StringSet>(setId));
                _session.UseFretwire(library.LoadLibraryItem<Fretwire>(wireId));
            }
            Assert.That(_session.Dirty);

            using (var library = Library.Open(_path))
            {
                var wire = library.LoadLibraryItem<Fretwire>(wireId);
                library.SaveLibraryItem(wire with { BeadHeight = new(Fretwire.Definitions.BeadHeight, 1.5) });
                library.DeleteLibraryItem(setId);
            }
            _session.ReloadLibraryItems();
            Assert.That(_session.Working.Neck.Fretwire.BeadHeight.Value, Is.EqualTo(1.5));
            Assert.That(_session.Working.Neck.StringSet, Is.Null);
            Assert.That(_session.StringSetNote, Does.Contain("no longer in the library"));
            Assert.That(_session.FretwireNote, Is.Null);
        }

        [Test]
        public void UseFretwire_IsAnUnsavedEdit_AndClearsTheNote()
        {
            var (_, wireId) = SaveItems();
            _session.Open(Create("A"));
            _session.FretwireNote = "x";
            using (var library = Library.Open(_path))
                _session.UseFretwire(library.LoadLibraryItem<Fretwire>(wireId));
            Assert.That(_session.Dirty);
            Assert.That(_session.FretwireNote, Is.Null);
            _session.UseFretwire(null);
            Assert.That(_session.Working.Neck.Fretwire, Is.Null);
        }
    }
}
