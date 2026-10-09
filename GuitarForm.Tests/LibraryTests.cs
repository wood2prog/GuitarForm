using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuitarForm.Data;
using GuitarForm.Model;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    // Checks saving and loading designs and presets, against a new library in a temporary folder for each test.
    public class LibraryTests
    {
        static readonly GuitarDesign OM = new GuitarDesign
        {
            Name = "OM",
            Instrument = new Instrument { ScaleLength = 645, NeckJoinFret = 14 },
            Body = new Body { BodyLength = 495, UbWidth = 290, LbWidth = 390, TailDepth = 105, NeckDepth = 90 },
            Soundhole = new Soundhole { Diameter = 100, TailOffset = 330 },
        };

        string _folder;
        string _path;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "GuitarFormTests", Guid.NewGuid().ToString("N"));
            _path = Path.Combine(_folder, "GuitarForm.db");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_folder))
                Directory.Delete(_folder, true);
        }

        long Pragma(string name)
        {
            using var connection = new SqliteConnection($"Data Source={_path};Pooling=False");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA {name}";
            return (long)command.ExecuteScalar();
        }

        void Execute(string sql)
        {
            using var connection = new SqliteConnection($"Data Source={_path};Pooling=False");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        // --- Opening ---

        [Test]
        public void Open_CreatesALibraryAtTheLatestVersion()
        {
            using (Library.Open(_path)) { }

            Assert.That(File.Exists(_path));
            Assert.That(Pragma("application_id"), Is.EqualTo(0x4774466D));
            Assert.That(Pragma("user_version"), Is.EqualTo(1));
        }

        [Test]
        public void Open_RejectsAnotherKindOfDatabase()
        {
            Directory.CreateDirectory(_folder);
            Execute("CREATE TABLE something (x)");

            Assert.That(() => Library.Open(_path), Throws.TypeOf<InvalidDataException>());
        }

        [Test]
        public void Open_RejectsALibraryFromANewerVersion()
        {
            using (Library.Open(_path)) { }
            Execute("PRAGMA user_version = 99");

            Assert.That(() => Library.Open(_path), Throws.TypeOf<InvalidDataException>());
        }

        [Test]
        public void Open_Again_KeepsTheDesigns()
        {
            long id;
            using (var library = Library.Open(_path))
                id = library.CreateDesign(OM);

            using var reopened = Library.Open(_path);
            Assert.That(reopened.LoadDesign(id), Is.EqualTo(OM));
        }

        // --- Designs ---

        [Test]
        public void CreateDesign_ThenLoad_GivesTheSameDesign()
        {
            using var library = Library.Open(_path);
            long id = library.CreateDesign(OM);
            Assert.That(library.LoadDesign(id), Is.EqualTo(OM));
        }

        [Test]
        public void CreateDesign_WithoutSoundhole_LoadsWithoutSoundhole()
        {
            using var library = Library.Open(_path);
            var design = OM with { Soundhole = null };
            long id = library.CreateDesign(design);
            Assert.That(library.LoadDesign(id), Is.EqualTo(design));
        }

        [Test]
        public void CreateDesign_WithATakenName_Throws()
        {
            using var library = Library.Open(_path);
            library.CreateDesign(OM);
            Assert.That(() => library.CreateDesign(OM with { Name = "om" }), Throws.InvalidOperationException);
        }

        [Test]
        public void CreateDesign_WithoutAName_Throws()
        {
            using var library = Library.Open(_path);
            Assert.That(() => library.CreateDesign(OM with { Name = " " }), Throws.InvalidOperationException);
        }

        [Test]
        public void SaveDesign_OverwritesIt()
        {
            using var library = Library.Open(_path);
            long id = library.CreateDesign(OM);
            var changed = OM with
            {
                Name = "OM 2",
                Body = OM.Body with { LbWidth = 395, HeelWidth = 56 },
                Soundhole = null,
            };

            library.SaveDesign(id, changed);

            Assert.That(library.LoadDesign(id), Is.EqualTo(changed));
            Assert.That(library.ListDesigns().Select(d => d.Name), Is.EqualTo(new[] { "OM 2" }));
        }

        [Test]
        public void SaveDesign_UpdatesModified()
        {
            using var library = Library.Open(_path);
            long id = library.CreateDesign(OM);
            var created = library.ListDesigns().Single().Modified;

            library.SaveDesign(id, OM with { Name = "OM 2" });

            Assert.That(library.ListDesigns().Single().Modified, Is.GreaterThanOrEqualTo(created));
        }

        [Test]
        public void SaveDesign_ToAnotherDesignsName_Throws()
        {
            using var library = Library.Open(_path);
            library.CreateDesign(OM);
            long id = library.CreateDesign(OM with { Name = "Dread" });
            Assert.That(() => library.SaveDesign(id, OM), Throws.InvalidOperationException);
        }

        [Test]
        public void LoadDesign_Missing_Throws()
        {
            using var library = Library.Open(_path);
            Assert.That(() => library.LoadDesign(42), Throws.TypeOf<KeyNotFoundException>());
        }

        [Test]
        public void ListDesigns_IsSortedByName()
        {
            using var library = Library.Open(_path);
            library.CreateDesign(OM with { Name = "parlour" });
            library.CreateDesign(OM with { Name = "Dread" });
            library.CreateDesign(OM);

            Assert.That(library.ListDesigns().Select(d => d.Name), Is.EqualTo(new[] { "Dread", "OM", "parlour" }));
        }

        [Test]
        public void CopyDesign_GetsAnUnusedNameAndTheSameValues()
        {
            using var library = Library.Open(_path);
            long id = library.CreateDesign(OM);

            long copy = library.CopyDesign(id);
            long secondCopy = library.CopyDesign(id);

            Assert.That(library.LoadDesign(copy), Is.EqualTo(OM with { Name = "OM copy" }));
            Assert.That(library.LoadDesign(secondCopy).Name, Is.EqualTo("OM copy 2"));
        }

        [Test]
        public void CopyDesign_IsIndependentOfTheOriginal()
        {
            using var library = Library.Open(_path);
            long id = library.CreateDesign(OM);
            long copy = library.CopyDesign(id);

            library.SaveDesign(copy, OM with { Name = "OM copy", Body = OM.Body with { BodyLength = 500 } });

            Assert.That(library.LoadDesign(id), Is.EqualTo(OM));
        }

        [Test]
        public void DeleteDesign_RemovesItAndItsSections()
        {
            using (var library = Library.Open(_path))
            {
                long id = library.CreateDesign(OM);
                library.DeleteDesign(id);
                Assert.That(library.ListDesigns(), Is.Empty);
            }

            using var connection = new SqliteConnection($"Data Source={_path};Pooling=False");
            connection.Open();
            foreach (string table in new[] { "instruments", "bodies", "soundholes" })
            {
                using var count = connection.CreateCommand();
                count.CommandText = $"SELECT count(*) FROM {table}";
                Assert.That((long)count.ExecuteScalar(), Is.Zero, table);
            }
        }

        // --- Presets ---

        [Test]
        public void SavePreset_ThenLoad_GivesTheSameSection()
        {
            using var library = Library.Open(_path);
            library.SavePreset("OM body", OM.Body);
            library.SavePreset("Short scale", OM.Instrument);
            library.SavePreset("100 mm", OM.Soundhole);

            Assert.That(library.LoadPreset<Body>("OM body"), Is.EqualTo(OM.Body));
            Assert.That(library.LoadPreset<Instrument>("Short scale"), Is.EqualTo(OM.Instrument));
            Assert.That(library.LoadPreset<Soundhole>("100 mm"), Is.EqualTo(OM.Soundhole));
        }

        [Test]
        public void ListPresets_ListsOnlyThatSectionsPresetsSortedByName()
        {
            using var library = Library.Open(_path);
            library.CreateDesign(OM);
            library.SavePreset("parlour", OM.Body);
            library.SavePreset("Dread", OM.Body);
            library.SavePreset("Standard", OM.Instrument);

            Assert.That(library.ListPresets<Body>(), Is.EqualTo(new[] { "Dread", "parlour" }));
            Assert.That(library.ListPresets<Instrument>(), Is.EqualTo(new[] { "Standard" }));
            Assert.That(library.ListPresets<Soundhole>(), Is.Empty);
        }

        [Test]
        public void SavePreset_WithAnExistingName_ReplacesIt()
        {
            using var library = Library.Open(_path);
            library.SavePreset("OM body", OM.Body);
            var changed = OM.Body with { LbWidth = 400 };

            library.SavePreset("om BODY", changed);

            Assert.That(library.ListPresets<Body>(), Is.EqualTo(new[] { "om BODY" }));
            Assert.That(library.LoadPreset<Body>("OM body"), Is.EqualTo(changed));
        }

        [Test]
        public void Preset_CopiedIntoADesign_IsIndependentOfIt()
        {
            using var library = Library.Open(_path);
            library.SavePreset("OM body", OM.Body);
            long id = library.CreateDesign(OM with { Body = library.LoadPreset<Body>("OM body") });

            library.SavePreset("OM body", OM.Body with { LbWidth = 400 });

            Assert.That(library.LoadDesign(id).Body, Is.EqualTo(OM.Body));
        }

        [Test]
        public void DeletePreset_RemovesIt()
        {
            using var library = Library.Open(_path);
            library.SavePreset("OM body", OM.Body);
            library.DeletePreset<Body>("OM body");

            Assert.That(library.PresetExists<Body>("OM body"), Is.False);
            Assert.That(() => library.LoadPreset<Body>("OM body"), Throws.TypeOf<KeyNotFoundException>());
        }

        [Test]
        public void Presets_OfAnotherType_Throw()
        {
            using var library = Library.Open(_path);
            Assert.That(() => library.ListPresets<GuitarDesign>(), Throws.ArgumentException);
        }
    }
}
