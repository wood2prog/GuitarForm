using System;
using System.IO;
using System.Linq;
using GuitarForm.Data;
using GuitarForm.Model;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    // Opens libraries made by older versions of the code and checks that they are upgraded without losing data.
    public class LibraryUpgradeTests
    {
        const long ApplicationId = Schema.ApplicationId;
        const int LatestVersion = 4; // pinned: bump by hand with each migration

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

        string BackupPath(string name) => Path.Combine(_folder, name);

        static string Hash(string file) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(file)));

        string[] SafetyFiles() => Array.ConvertAll(Directory.GetFiles(_folder, "*.bak*"), Path.GetFileName);

        static long ScalarAt(string file, string sql)
        {
            using var connection = new SqliteConnection($"Data Source={file};Mode=ReadOnly;Pooling=False");
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

            using (Library.Open(_path)) { }

            // T8: the version 1 file was copied beside the library before the migration ran.
            string backup = BackupPath("GuitarForm.before-v1.bak");
            Assert.That(File.Exists(backup));
            Assert.That(ScalarAt(backup, "PRAGMA user_version"), Is.EqualTo(1));
            Assert.That(ScalarAt(backup, "SELECT count(*) FROM designs"), Is.EqualTo(2));
            Assert.That(ScalarAt(backup, "SELECT count(*) FROM instruments"), Is.EqualTo(3));
            Assert.That(ScalarAt(backup, "SELECT count(*) FROM sqlite_master WHERE name = 'parts'"), Is.EqualTo(0));

            Assert.That(Scalar("PRAGMA user_version"), Is.EqualTo(LatestVersion));
            Assert.That(Scalar("SELECT count(*) FROM sqlite_master WHERE name IN ('parts', 'part_values')"),
                Is.EqualTo(2));
        }

        [Test]
        public void Version2Library_GetsGuitarUids_AndKeepsItsData()
        {
            long a, b;
            using (var library = Library.Open(_path))
            {
                a = library.CreateGuitar(TestGuitars.OM("Alpha"));
                b = library.CreateGuitar(TestGuitars.OM("Beta"));
            }
            // Take it back to version 2: no uid or legacy_design_id columns, name kept as an IN value as before.
            Execute("DROP INDEX parts_uids; ALTER TABLE parts DROP COLUMN uid; ALTER TABLE parts DROP COLUMN legacy_design_id;" +
                "INSERT OR IGNORE INTO part_values (part_id, abbreviation, item, value) " +
                $"SELECT id, 'IN', -1, name FROM parts WHERE kind = 'guitar'; PRAGMA user_version = 2;");

            using (var library = Library.Open(_path))
            {
                Assert.That(library.ListGuitars().Count, Is.EqualTo(2));
                TestGuitars.AssertSameValues(library.LoadGuitar(a), TestGuitars.OM("Alpha"));
                Assert.That(library.LoadGuitar(b).Name.Value, Is.EqualTo("Beta"));
            }

            Assert.That(Scalar("PRAGMA user_version"), Is.EqualTo(LatestVersion));
            Assert.That(Scalar("SELECT count(*) FROM parts WHERE kind = 'guitar' AND uid IS NOT NULL AND uid <> ''"), Is.EqualTo(2));
            Assert.That(Scalar("SELECT count(DISTINCT uid) FROM parts"), Is.EqualTo(2));
            Assert.That(Scalar("SELECT count(*) FROM parts WHERE kind <> 'guitar' AND uid IS NOT NULL"), Is.EqualTo(0));
            Assert.That(Scalar("SELECT count(*) FROM parts WHERE legacy_design_id IS NOT NULL"), Is.EqualTo(0));
            Assert.That(Scalar("SELECT count(*) FROM part_values v JOIN parts p ON p.id = v.part_id " +
                "WHERE p.kind = 'guitar' AND v.abbreviation = 'IN'"), Is.EqualTo(0));

            string backup = BackupPath("GuitarForm.before-v2.bak");
            Assert.That(File.Exists(backup));
            Assert.That(ScalarAt(backup, "PRAGMA user_version"), Is.EqualTo(2));
            Assert.That(ScalarAt(backup, "SELECT count(*) FROM parts WHERE kind = 'guitar'"), Is.EqualTo(2));
        }

        // A version 3 library that still holds designs of the old model.
        void MakeVersion3LibraryWithDesigns()
        {
            using (Library.Open(_path)) { }
            Execute(V1Data + "PRAGMA user_version = 3;");
        }

        [Test]
        public void Version3Library_ConvertsItsDesignsToGuitars_AndKeepsTheOldTables()
        {
            MakeVersion3LibraryWithDesigns();

            using (var library = Library.Open(_path))
            {
                var guitars = library.ListGuitars();
                Assert.That(guitars.Select(g => g.Name), Is.EqualTo(new[] { "OM", "Parlour" }));
                Assert.That(guitars.Select(g => g.LegacyDesignId), Is.EqualTo(new long?[] { 1, 2 }));
                var om = library.LoadGuitar(guitars[0].Id);
                Assert.That(om.ScaleLength.Value, Is.EqualTo(645));
                Assert.That(om.NeckJoinFret.Value, Is.EqualTo(14));
                Assert.That(om.Body.Length.Value, Is.EqualTo(495));
                Assert.That(Scalar("SELECT count(*) FROM designs"), Is.EqualTo(2));
            }

            Assert.That(Scalar("PRAGMA user_version"), Is.EqualTo(LatestVersion));
            Assert.That(Scalar("SELECT count(*) FROM instruments"), Is.EqualTo(3));
            Assert.That(Scalar("SELECT count(*) FROM soundholes"), Is.EqualTo(2));

            string backup = BackupPath("GuitarForm.before-v3.bak");
            Assert.That(File.Exists(backup));
            Assert.That(ScalarAt(backup, "PRAGMA user_version"), Is.EqualTo(3));
            Assert.That(ScalarAt(backup, "SELECT count(*) FROM parts WHERE kind = 'guitar'"), Is.EqualTo(0));
        }

        [Test]
        public void Version3Upgrade_KeepsItsNotes_OnTheLibrary_InAFile_AndOnce()
        {
            MakeVersion3LibraryWithDesigns();
            Library.TakeUpgradeNotes();

            using (var library = Library.Open(_path))
            {
                Assert.That(library.UpgradeNotes, Is.Not.Empty);
                Assert.That(string.Join(" ", library.UpgradeNotes), Does.Contain("instrument preset"));
            }
            string notesFile = BackupPath("GuitarForm.before-v3.notes.txt");
            Assert.That(File.Exists(notesFile));
            Assert.That(File.ReadAllText(notesFile), Does.Contain("\"Short\""));

            var pending = Library.TakeUpgradeNotes();
            Assert.That(pending, Is.Not.Empty);
            Assert.That(Library.TakeUpgradeNotes(), Is.Empty);
            using (var again = Library.Open(_path))
                Assert.That(again.UpgradeNotes, Is.Empty);
        }

        [Test]
        public void NotesFile_IsNeverOverwritten()
        {
            File.WriteAllText(BackupPath("GuitarForm.before-v3.notes.txt"), "earlier");
            MakeVersion3LibraryWithDesigns();

            using (Library.Open(_path)) { }

            Assert.That(File.ReadAllText(BackupPath("GuitarForm.before-v3.notes.txt")), Is.EqualTo("earlier"));
            Assert.That(File.Exists(BackupPath("GuitarForm.before-v3.notes.txt")));
            Assert.That(Directory.GetFiles(_folder, "*.notes.txt").Length, Is.EqualTo(2));
        }

        [Test]
        public void Version1Library_IsConvertedToGuitarsToo()
        {
            MakeVersion1Library();

            using var library = Library.Open(_path);

            Assert.That(library.ListGuitars().Select(g => g.Name), Is.EqualTo(new[] { "OM", "Parlour" }));
        }

        [Test]
        public void Version1Library_KeepsItsOldTables()
        {
            MakeVersion1Library();

            using (Library.Open(_path)) { }

            Assert.That(Scalar("SELECT count(*) FROM designs"), Is.EqualTo(2));
            Assert.That(Scalar("SELECT scale_length FROM instruments WHERE design_id = 1"), Is.EqualTo(645));
            Assert.That(Scalar("SELECT body_length FROM bodies WHERE design_id = 2"), Is.EqualTo(450));
            Assert.That(Scalar("SELECT count(*) FROM soundholes WHERE design_id = 2"), Is.Zero);
            Assert.That(Scalar("SELECT count(*) FROM instruments WHERE preset_name = 'Short'"), Is.EqualTo(1));
            Assert.That(Scalar("SELECT count(*) FROM soundholes WHERE preset_name = 'Round'"), Is.EqualTo(1));
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
            Assert.That(Scalar("SELECT count(*) FROM designs"), Is.EqualTo(2));
        }

        [Test]
        public void NewLibrary_IsAtTheLatestVersion()
        {
            using (Library.Open(_path)) { }

            Assert.That(Scalar("PRAGMA application_id"), Is.EqualTo(ApplicationId));
            Assert.That(Scalar("PRAGMA user_version"), Is.EqualTo(LatestVersion));
        }

        [Test]
        public void NewLibrary_MakesNoSafetyCopy_AndNeitherDoesReopeningAnUpgradedOne()
        {
            using (Library.Open(_path)) { }
            using (Library.Open(_path)) { }
            Assert.That(SafetyFiles(), Is.Empty);

            File.Delete(_path);
            MakeVersion1Library();
            using (Library.Open(_path)) { }
            using (Library.Open(_path)) { }
            Assert.That(SafetyFiles(), Is.EqualTo(new[] { "GuitarForm.before-v1.bak" }));
        }

        [Test]
        public void ExistingSafetyCopy_IsNeverOverwritten()
        {
            File.WriteAllText(BackupPath("GuitarForm.before-v1.bak"), "earlier");
            MakeVersion1Library();

            using (Library.Open(_path)) { }

            Assert.That(File.ReadAllText(BackupPath("GuitarForm.before-v1.bak")), Is.EqualTo("earlier"));
            Assert.That(ScalarAt(BackupPath("GuitarForm.before-v1.1.bak"), "PRAGMA user_version"), Is.EqualTo(1));
            Assert.That(Scalar("PRAGMA user_version"), Is.EqualTo(LatestVersion));
        }

        [Test]
        public void FailedSafetyCopy_BlocksTheOpen_AndLeavesTheLibraryUnmigrated()
        {
            MakeVersion1Library();
            Directory.CreateDirectory(BackupPath("GuitarForm.before-v1.bak.tmp"));
            string before = Hash(_path);

            var error = Assert.Throws<IOException>(() => Library.Open(_path));

            Assert.That(error.Message, Does.Contain("GuitarForm.before-v1.bak"));
            Assert.That(Scalar("PRAGMA user_version"), Is.EqualTo(1));
            Assert.That(Scalar("SELECT count(*) FROM designs"), Is.EqualTo(2));
            Assert.That(Scalar("SELECT count(*) FROM sqlite_master WHERE name = 'parts'"), Is.EqualTo(0));
            Assert.That(File.Exists(BackupPath("GuitarForm.before-v1.bak")), Is.False);
            Assert.That(Hash(_path), Is.EqualTo(before));
            Assert.That(error.Message, Does.Contain("folder is writable"));
        }

        [Test]
        public void LibraryNewerThanTheCode_IsRefused_AndLeftAlone()
        {
            using (Library.Open(_path)) { }
            Execute($"PRAGMA user_version = {LatestVersion + 1}");

            var error = Assert.Throws<InvalidDataException>(() => Library.Open(_path));

            Assert.That(error.Message, Does.Contain("made by a newer GuitarForm"));
            Assert.That(Scalar("PRAGMA user_version"), Is.EqualTo(LatestVersion + 1));
            Assert.That(SafetyFiles(), Is.Empty);
        }
    }
}
