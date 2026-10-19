using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuitarForm.Data;
using GuitarForm.Model.Parameters;
using GuitarForm.Model.Parts;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    // The design session against a new library in a temporary folder for each test.
    public class DesignSessionTests
    {
        string _folder, _path;
        DesignSession _session;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "GuitarFormTests", Guid.NewGuid().ToString("N"));
            _path = Path.Combine(_folder, "GuitarForm.db");
            using (Library.Open(_path)) { }
            _session = new DesignSession(_path);
        }

        [TearDown]
        public void TearDown()
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(_folder))
                Directory.Delete(_folder, true);
        }

        long Create(string name)
        {
            var om = TestGuitars.OM(name);
            using var library = Library.Open(_path);
            return library.CreateGuitar(om with { Neck = om.Neck with { Fretwire = null, StringSet = null } });
        }

        long Count(string table)
        {
            using var connection = new SqliteConnection("Data Source=" + _path + ";Pooling=False");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM {table}";
            return (long)command.ExecuteScalar();
        }

        string Fingerprint(long id)
        {
            GuitarInfo info;
            using (var library = Library.Open(_path))
                info = library.ListGuitars().Single(g => g.Id == id);
            return $"{Count("parts")}/{Count("part_values")}/{info.Modified:O}/{info.Name}";
        }

        static Guitar Rename(Guitar g, string name) => g with { Name = new TextParameter(Guitar.Definitions.Name, name) };
        static Guitar Scale(Guitar g, double mm) => g with { ScaleLength = new NumberParameter(Guitar.Definitions.ScaleLength, mm) };

        [Test]
        public void Open_LoadsCleanWithKeys()
        {
            long id = Create("A");
            var (problems, pending) = _session.Open(id);
            Assert.That(problems, Is.Empty);
            Assert.That(pending, Is.Null);
            Assert.That(_session.IsOpen && !_session.Dirty && !_session.SavePrompted);
            Assert.That(_session.GuitarId, Is.EqualTo(id));
            Assert.That(_session.SavedName, Is.EqualTo("A"));
            Assert.That(_session.Uid, Is.Not.Empty);
            Assert.That(_session.BuildKey, Is.EqualTo("guitar-" + _session.Uid));
            Assert.That(_session.LegacyBuildKeys, Is.Empty);
            Assert.That(_session.Working.ScaleLength.Value, Is.EqualTo(645));
        }

        [Test]
        public void Open_MissingGuitar_ThrowsAndLeavesSessionAlone()
        {
            long id = Create("A");
            _session.Open(id);
            Assert.Throws<KeyNotFoundException>(() => _session.Open(9999));
            Assert.That(_session.GuitarId, Is.EqualTo(id));
        }

        [Test]
        public void LegacyBuildKeys_ComeFromTheLegacyDesignId()
        {
            long id = Create("A");
            using (var connection = new SqliteConnection("Data Source=" + _path + ";Pooling=False"))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = $"UPDATE parts SET legacy_design_id = 42 WHERE id = {id}";
                command.ExecuteNonQuery();
            }
            _session.Open(id);
            Assert.That(_session.LegacyBuildKeys, Is.EqualTo(new[] { "42" }));
        }

        [Test]
        public void Apply_MarksDirty_AndLeavesTheLibraryAlone()
        {
            long id = Create("A");
            _session.Open(id);
            string before = Fingerprint(id);
            _session.Apply(g => Scale(g, 650));
            _session.Apply(g => Rename(g, "Renamed"));
            Assert.That(_session.Dirty);
            Assert.That(_session.Working.ScaleLength.Value, Is.EqualTo(650));
            Assert.That(Fingerprint(id), Is.EqualTo(before));
            Assert.That(_session.SavedName, Is.EqualTo("A"));
        }

        [Test]
        public void Apply_WithNoChange_StaysClean()
        {
            _session.Open(Create("A"));
            _session.Apply(g => g);
            _session.Apply(g => Scale(g, 645));
            Assert.That(_session.Dirty, Is.False);
        }

        [Test]
        public void Apply_WithNothingOpen_Throws() =>
            Assert.Throws<InvalidOperationException>(() => _session.Apply(g => g));

        [Test]
        public void Save_WritesKeepsUidAndClearsDirty()
        {
            long id = Create("A");
            _session.Open(id);
            string uid = _session.Uid;
            _session.Apply(g => Scale(Rename(g, "B"), 640));
            _session.Save();
            Assert.That(_session.Dirty, Is.False);
            Assert.That(_session.SavePrompted);
            Assert.That(_session.Uid, Is.EqualTo(uid));
            Assert.That(_session.SavedName, Is.EqualTo("B"));
            using var library = Library.Open(_path);
            Assert.That(library.LoadGuitar(id).ScaleLength.Value, Is.EqualTo(640));
            Assert.That(library.ListGuitars().Single().Uid, Is.EqualTo(uid));
        }

        [Test]
        public void Save_NameTaken_ThrowsAndStaysDirty()
        {
            Create("A");
            long b = Create("B");
            _session.Open(b);
            _session.Apply(g => Rename(g, "a"));
            Assert.Throws<InvalidOperationException>(() => _session.Save());
            Assert.That(_session.Dirty);
        }

        [Test]
        public void SaveAs_MakesANewGuitarWithANewUid_AndLeavesTheOriginal()
        {
            long id = Create("A");
            _session.Open(id);
            string uid = _session.Uid;
            _session.Apply(g => Scale(g, 650));
            _session.SaveAs("Second");
            Assert.That(_session.GuitarId, Is.Not.EqualTo(id));
            Assert.That(_session.Uid, Is.Not.EqualTo(uid));
            Assert.That(_session.SavedName, Is.EqualTo("Second"));
            Assert.That(_session.Dirty, Is.False);
            Assert.That(_session.SavePrompted);
            Assert.That(_session.Working.ScaleLength.Value, Is.EqualTo(650));
            using var library = Library.Open(_path);
            Assert.That(library.LoadGuitar(id).ScaleLength.Value, Is.EqualTo(645));
            Assert.That(library.ListGuitars(), Has.Count.EqualTo(2));
        }

        [Test]
        public void SaveAs_NameTaken_Throws_AndKeepsTheSession()
        {
            long id = Create("A");
            Create("B");
            _session.Open(id);
            _session.Apply(g => Scale(g, 650));
            Assert.Throws<InvalidOperationException>(() => _session.SaveAs("b"));
            Assert.Throws<InvalidOperationException>(() => _session.SaveAs(""));
            Assert.That(_session.GuitarId, Is.EqualTo(id));
            Assert.That(_session.Dirty);
        }

        [Test]
        public void Revert_DiscardsEdits()
        {
            _session.Open(Create("A"));
            _session.Apply(g => Scale(g, 650));
            _session.Revert();
            Assert.That(_session.Dirty, Is.False);
            Assert.That(_session.Working.ScaleLength.Value, Is.EqualTo(645));
        }

        [Test]
        public void Close_ClearsTheSession()
        {
            _session.Open(Create("A"));
            _session.Close();
            Assert.That(_session.IsOpen, Is.False);
            Assert.That(_session.BuildKey, Is.Null);
        }

        [Test]
        public void CreateNew_SavesAndOpens()
        {
            _session.CreateNew("New", 480, 640, 14);
            Assert.That(_session.IsOpen && !_session.Dirty && _session.SavePrompted);
            Assert.That(_session.SavedName, Is.EqualTo("New"));
            Assert.That(_session.Working.Body.Length.Value, Is.EqualTo(480));
            Assert.That(_session.Working.ScaleLength.Value, Is.EqualTo(640));
            Assert.That(_session.Working.NeckJoinFret.Value, Is.EqualTo(14));
            Assert.Throws<InvalidOperationException>(() => _session.CreateNew("new", null, null, null));
            Assert.Throws<InvalidOperationException>(() => _session.CreateNew(" ", null, null, null));
        }

        [Test]
        public void Copy_CopiesTheSavedGuitar_NotTheEdits()
        {
            long id = Create("A");
            _session.Open(id);
            string uid = _session.Uid;
            _session.Apply(g => Scale(g, 650));
            _session.Copy();
            Assert.That(_session.GuitarId, Is.Not.EqualTo(id));
            Assert.That(_session.Uid, Is.Not.EqualTo(uid));
            Assert.That(_session.SavedName, Is.EqualTo("A copy"));
            Assert.That(_session.Working.ScaleLength.Value, Is.EqualTo(645));
            Assert.That(_session.Dirty, Is.False);
        }

        [Test]
        public void Delete_RemovesTheGuitarAndClosesTheSession()
        {
            long id = Create("A");
            Create("B");
            _session.Open(id);
            _session.Apply(g => Scale(g, 650));
            _session.Delete();
            Assert.That(_session.IsOpen, Is.False);
            Assert.That(_session.Dirty, Is.False);
            using var library = Library.Open(_path);
            Assert.That(library.ListGuitars().Select(g => g.Name), Is.EqualTo(new[] { "B" }));
        }

        // --- Named copies of the neck, body and bracing ---

        [Test]
        public void ListNamed_ListsOnlyNamedCopies()
        {
            Create("A");
            Assert.That(_session.ListNamed<Body>(), Is.Empty);
            _session.Open(Create("B"));
            _session.SaveNamed<Body>("Jumbo");
            Assert.That(_session.ListNamed<Body>().Select(b => b.Name), Is.EqualTo(new[] { "Jumbo" }));
            Assert.That(_session.ListNamed<Neck>(), Is.Empty);
        }

        [Test]
        public void CheckNamedName_RejectsEmptyAndTakenIgnoringCase()
        {
            _session.Open(Create("A"));
            _session.SaveNamed<Neck>("Slim C");
            Assert.That(_session.CheckNamedName<Neck>("  "), Is.Not.Null);
            Assert.That(_session.CheckNamedName<Neck>(null), Is.Not.Null);
            Assert.That(_session.CheckNamedName<Neck>(" slim c "), Is.Not.Null);
            Assert.That(_session.CheckNamedName<Neck>("Fat C"), Is.Null);
            Assert.That(_session.CheckNamedName<Body>("Slim C"), Is.Null);
            Assert.Throws<InvalidOperationException>(() => _session.SaveNamed<Neck>("SLIM C"));
            Assert.Throws<InvalidOperationException>(() => _session.SaveNamed<Neck>(""));
        }

        [Test]
        public void SaveNamed_ThenLoadIntoAnotherGuitar_CopiesTheValuesAsAnEdit()
        {
            _session.Open(Create("A"));
            _session.Apply(g => g with { Body = g.Body with { Length = new NumberParameter(Body.Definitions.Length, 480) } });
            long id = _session.SaveNamed<Body>("Long");

            _session.Open(Create("B"), discardPrevious: true);
            Assert.That(_session.Working.Body.Length.Value, Is.Not.EqualTo(480));
            var problems = _session.LoadNamed<Body>(id);
            Assert.That(problems, Is.Empty);
            Assert.That(_session.Working.Body.Length.Value, Is.EqualTo(480));
            Assert.That(_session.Dirty, Is.True);
            Assert.That(_session.Working.Name.Value, Is.EqualTo("B"));
        }

        [Test]
        public void LoadNamed_Bracing_ReplacesOnlyTheTopPlatesBracing()
        {
            _session.Open(Create("A"));
            var bracing = new Bracing { ToneBarCount = new(Bracing.Definitions.ToneBarCount, 3) };
            _session.Apply(g => g with { Body = g.Body with { TopPlate = g.Body.TopPlate with { Bracing = bracing } } });
            long id = _session.SaveNamed<Bracing>("Three bars");

            _session.Open(Create("B"), discardPrevious: true);
            var body = _session.Working.Body;
            _session.LoadNamed<Bracing>(id);
            Assert.That(_session.Working.Body.TopPlate.Bracing.ToneBarCount.Value, Is.EqualTo(3));
            Assert.That(_session.Working.Body with { TopPlate = body.TopPlate with { Bracing = body.TopPlate.Bracing } },
                Is.EqualTo(body));
            Assert.That(_session.Working.Body.Length.Value, Is.EqualTo(body.Length.Value));
        }

        [Test]
        public void LoadNamed_Neck_ReplacesTheNeckAndClearsItsNotes()
        {
            _session.Open(Create("A"));
            _session.Apply(g => g with { Neck = g.Neck with { Fretboard = g.Neck.Fretboard with { NutWidth = new(Fretboard.Definitions.NutWidth, 44) } } });
            long id = _session.SaveNamed<Neck>("Wide");
            _session.Open(Create("B"), discardPrevious: true);
            _session.FretwireNote = "gone";
            _session.LoadNamed<Neck>(id);
            Assert.That(_session.Working.Neck.Fretboard.NutWidth.Value, Is.EqualTo(44));
            Assert.That(_session.FretwireNote, Is.Null);
        }

        [Test]
        public void LoadNamed_Neck_RestoresTheFretwireAndStringSetReferences()
        {
            long wireId, setId, guitarId;
            using (var library = Library.Open(_path))
            {
                wireId = library.SaveLibraryItem(TestGuitars.Fretwire);
                setId = library.SaveLibraryItem(TestGuitars.StringSet);
                var om = TestGuitars.OM("A");
                guitarId = library.CreateGuitar(om with
                {
                    Neck = om.Neck with
                    {
                        Fretwire = TestGuitars.Fretwire with { Id = wireId },
                        StringSet = TestGuitars.StringSet with { Id = setId },
                    },
                });
            }
            _session.Open(guitarId);
            long copy = _session.SaveNamed<Neck>("With wire");

            _session.Open(Create("B"), discardPrevious: true);
            Assert.That(_session.Working.Neck.Fretwire, Is.Null);
            _session.LoadNamed<Neck>(copy);

            Assert.That(_session.Working.Neck.Fretwire.Id, Is.EqualTo(wireId));
            Assert.That(_session.Working.Neck.StringSet.Id, Is.EqualTo(setId));
        }

        [Test]
        public void LoadNamed_MissingCopy_ThrowsAndChangesNothing()
        {
            _session.Open(Create("A"));
            Assert.Throws<KeyNotFoundException>(() => _session.LoadNamed<Body>(99999));
            Assert.That(_session.Dirty, Is.False);
        }

        [Test]
        public void DeleteNamed_RemovesTheCopy_ButNotAGuitarsOwnPart()
        {
            _session.Open(Create("A"));
            long id = _session.SaveNamed<Body>("Jumbo");
            _session.DeleteNamed(id);
            Assert.That(_session.ListNamed<Body>(), Is.Empty);
            long own;
            using (var library = Library.Open(_path))
                own = library.ListAssemblies<Body>().Single().Id;
            Assert.Throws<InvalidOperationException>(() => _session.DeleteNamed(own));
        }
    }
}
