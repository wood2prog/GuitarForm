using System;
using System.IO;
using GuitarForm.Data;
using GuitarForm.Model;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    // Opens libraries made by older versions of the code and checks that they are upgraded without losing data.
    public class LibraryUpgradeTests
    {
        // Schema is internal to GuitarForm.Data, so its values are repeated here.
        const long ApplicationId = 0x4774466D;
        const int LatestVersion = 2;

        // The version 1 schema (migration 0 in Schema.cs), frozen here as it was released.
        const string V1Schema = @"
            CREATE TABLE designs (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL UNIQUE COLLATE NOCASE CHECK (name <> ''),
                created TEXT NOT NULL,
                modified TEXT NOT NULL
            );

            CREATE TABLE instruments (
                id INTEGER PRIMARY KEY,
                design_id INTEGER UNIQUE REFERENCES designs (id) ON DELETE CASCADE,
                preset_name TEXT UNIQUE COLLATE NOCASE CHECK (preset_name <> ''),
                scale_length REAL NOT NULL,
                neck_join_fret INTEGER NOT NULL,
                CHECK ((design_id IS NULL) <> (preset_name IS NULL))
            );

            CREATE TABLE bodies (
                id INTEGER PRIMARY KEY,
                design_id INTEGER UNIQUE REFERENCES designs (id) ON DELETE CASCADE,
                preset_name TEXT UNIQUE COLLATE NOCASE CHECK (preset_name <> ''),
                body_length REAL NOT NULL,
                ub_width REAL,
                ub_offset REAL,
                ub_radius REAL,
                ub_secondary_offset REAL,
                waist_radius REAL,
                waist_width REAL,
                waist_offset REAL,
                lb_width REAL,
                lb_offset REAL,
                lb_radius REAL,
                lb_secondary_offset REAL,
                heel_width REAL,
                tail_depth REAL,
                neck_depth REAL,
                CHECK ((design_id IS NULL) <> (preset_name IS NULL))
            );

            CREATE TABLE soundholes (
                id INTEGER PRIMARY KEY,
                design_id INTEGER UNIQUE REFERENCES designs (id) ON DELETE CASCADE,
                preset_name TEXT UNIQUE COLLATE NOCASE CHECK (preset_name <> ''),
                diameter REAL NOT NULL,
                horizontal_offset REAL,
                tail_offset REAL NOT NULL,
                CHECK ((design_id IS NULL) <> (preset_name IS NULL))
            );
            ";

        const string V1Data = @"
            INSERT INTO designs (id, name, created, modified) VALUES
                (1, 'OM', '2024-01-02T03:04:05.0000000Z', '2024-01-02T03:04:05.0000000Z'),
                (2, 'Parlour', '2024-02-02T03:04:05.0000000Z', '2024-02-02T03:04:05.0000000Z');
            INSERT INTO instruments (design_id, scale_length, neck_join_fret) VALUES (1, 645, 14), (2, 610, 12);
            INSERT INTO instruments (preset_name, scale_length, neck_join_fret) VALUES ('Short', 580, 12);
            INSERT INTO bodies (design_id, body_length, ub_width, lb_width, tail_depth, neck_depth) VALUES
                (1, 495, 290, 390, 105, 90);
            INSERT INTO bodies (design_id, body_length, lb_width, tail_depth) VALUES (2, 450, 330, 95);
            INSERT INTO soundholes (design_id, diameter, tail_offset) VALUES (1, 100, 330);
            INSERT INTO soundholes (preset_name, diameter, horizontal_offset, tail_offset) VALUES ('Round', 85, 0, 300);
            ";

        string _folder;
        string _path;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "GuitarFormTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            _path = Path.Combine(_folder, "GuitarForm.db");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_folder))
                Directory.Delete(_folder, true);
        }

        void Execute(string sql)
        {
            using var connection = new SqliteConnection($"Data Source={_path};Pooling=False");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        long Scalar(string sql)
        {
            using var connection = new SqliteConnection($"Data Source={_path};Pooling=False");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            return Convert.ToInt64(command.ExecuteScalar());
        }

        void MakeVersion1Library()
        {
            Execute($"PRAGMA application_id = {ApplicationId};" + V1Schema + V1Data + "PRAGMA user_version = 1;");
            Assert.That(Scalar("PRAGMA user_version"), Is.EqualTo(1));
        }

        [Test]
        public void Version1Library_IsUpgradedToTheLatestVersion()
        {
            MakeVersion1Library();

            // T8: assert here that a backup of the version 1 file was made before the migration ran.

            using (Library.Open(_path)) { }

            Assert.That(Scalar("PRAGMA user_version"), Is.EqualTo(LatestVersion));
            Assert.That(Scalar("SELECT count(*) FROM sqlite_master WHERE name IN ('parts', 'part_values')"),
                Is.EqualTo(2));
        }

        [Test]
        public void Version1Library_KeepsItsDesigns()
        {
            MakeVersion1Library();

            using var library = Library.Open(_path);

            Assert.That(library.ListDesigns().Count, Is.EqualTo(2));
            var om = library.LoadDesign(1);
            Assert.That(om.Name, Is.EqualTo("OM"));
            Assert.That(om.Instrument, Is.EqualTo(new Instrument { ScaleLength = 645, NeckJoinFret = 14 }));
            Assert.That(om.Body, Is.EqualTo(new Body
                { BodyLength = 495, UbWidth = 290, LbWidth = 390, TailDepth = 105, NeckDepth = 90 }));
            Assert.That(om.Soundhole, Is.EqualTo(new Soundhole { Diameter = 100, TailOffset = 330 }));

            var parlour = library.LoadDesign(2);
            Assert.That(parlour.Name, Is.EqualTo("Parlour"));
            Assert.That(parlour.Instrument.ScaleLength, Is.EqualTo(610));
            Assert.That(parlour.Body, Is.EqualTo(new Body { BodyLength = 450, LbWidth = 330, TailDepth = 95 }));
            Assert.That(parlour.Soundhole, Is.Null);
        }

        [Test]
        public void Version1Library_KeepsItsPresets()
        {
            MakeVersion1Library();

            using var library = Library.Open(_path);

            Assert.That(library.ListPresets<Instrument>(), Is.EqualTo(new[] { "Short" }));
            Assert.That(library.LoadPreset<Instrument>("Short"),
                Is.EqualTo(new Instrument { ScaleLength = 580, NeckJoinFret = 12 }));
            Assert.That(library.LoadPreset<Soundhole>("Round"),
                Is.EqualTo(new Soundhole { Diameter = 85, HorizontalOffset = 0, TailOffset = 300 }));
        }

        [Test]
        public void Version1Library_CanSaveANewGuitar()
        {
            MakeVersion1Library();

            long id;
            using (var library = Library.Open(_path))
                id = library.CreateGuitar(TestGuitars.OM("Upgraded"));

            using var reopened = Library.Open(_path);
            TestGuitars.AssertSameValues(reopened.LoadGuitar(id), TestGuitars.OM("Upgraded"));
            Assert.That(reopened.ListDesigns().Count, Is.EqualTo(2));
        }

        [Test]
        public void NewLibrary_IsAtTheLatestVersion()
        {
            using (Library.Open(_path)) { }

            Assert.That(Scalar("PRAGMA application_id"), Is.EqualTo(ApplicationId));
            Assert.That(Scalar("PRAGMA user_version"), Is.EqualTo(LatestVersion));
        }

        [Test]
        public void LibraryNewerThanTheCode_IsRefused_AndLeftAlone()
        {
            using (Library.Open(_path)) { }
            Execute($"PRAGMA user_version = {LatestVersion + 1}");

            var error = Assert.Throws<InvalidDataException>(() => Library.Open(_path));

            Assert.That(error.Message, Does.Contain("made by a newer GuitarForm"));
            Assert.That(Scalar("PRAGMA user_version"), Is.EqualTo(LatestVersion + 1));
        }
    }
}
