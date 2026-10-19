using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuitarForm.Data;
using GuitarForm.Model.Parts;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    // The conversion of old-model designs (designs, instruments, bodies, soundholes tables) into guitars. The step runs
    // inside a transaction the test opens; it isn't registered as a migration yet.
    public class LegacyDesignMigrationTests
    {
        const string T1 = "2024-01-02T03:04:05.0000000Z";
        const string T2 = "2024-02-02T03:04:05.0000000Z";
        const string T3 = "2024-03-02T03:04:05.0000000Z";
        const string T4 = "2024-04-02T03:04:05.0000000Z";

        string _folder, _path, _newFields;
        long _stringSetId, _fretwireId;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "GuitarFormTests", Guid.NewGuid().ToString("N"));
            _newFields = Path.Combine(_folder, "NewFields");
            Directory.CreateDirectory(_newFields);
            _path = Path.Combine(_folder, "GuitarForm.db");

            using (var library = Library.Open(_path))
            {
                _stringSetId = library.SaveLibraryItem(new StringSet
                {
                    Manufacturer = new(StringSet.Definitions.Manufacturer, "Savarez"),
                    Name = new(StringSet.Definitions.Name, "510"),
                });
                _fretwireId = library.SaveLibraryItem(new Fretwire
                {
                    ProductCode = new(Fretwire.Definitions.ProductCode, "47104"),
                });
            }

            // The library is at the latest version, which has no old tables: put them back for the step to read.
            Execute(LibraryUpgradeTests.V1Schema);
            Execute($@"
                INSERT INTO designs (id, name, created, modified) VALUES
                    (1, 'OM', '{T1}', '{T1}'),
                    (2, 'OM ', '{T2}', '{T2}'),
                    (3, 'Parlour', '{T3}', '{T3}'),
                    (4, 'Dread', '{T4}', '{T4}'),
                    (5, 'Broken', '{T4}', '{T4}');
                INSERT INTO instruments (design_id, scale_length, neck_join_fret) VALUES
                    (1, 645, 14), (2, 610, 12), (3, 600.5, 12), (4, 650, 14), (5, 640, 14);
                INSERT INTO instruments (preset_name, scale_length, neck_join_fret) VALUES ('Short', 580, 12), ('Long', 650, 14);
                INSERT INTO bodies (design_id, body_length, ub_width, ub_offset, ub_radius, ub_secondary_offset,
                        waist_radius, waist_width, waist_offset, lb_width, lb_offset, lb_radius, lb_secondary_offset,
                        heel_width, tail_depth, neck_depth) VALUES
                    (1, 495, 290, 11, 12, 13, 14, 240, 15, 390, 16, 17, 18, 60, 105, 90);
                INSERT INTO bodies (design_id, body_length, lb_width, tail_depth) VALUES
                    (2, 450, 330, 95), (3, 440, 320, 90), (4, 500, 400, 110), (5, 480, 380, 100);
                INSERT INTO bodies (preset_name, body_length, lb_width) VALUES ('Parlour body', 440, 320);
                INSERT INTO soundholes (design_id, diameter, horizontal_offset, tail_offset) VALUES
                    (1, 100, 2.5, 330), (3, 85, NULL, 300), (4, 100, NULL, 320), (5, 90, NULL, 310);
                INSERT INTO soundholes (preset_name, diameter, tail_offset) VALUES ('Round', 85, 300);");

            // Parlour: valid, with a valid string set and fretwire.
            File.WriteAllText(Path.Combine(_newFields, "3.json"),
                $"{{ \"NTW\": \"43.5\", \"FC\": \"19\", \"Markers\": \"3 5 12:2\", \"StringSetId\": \"{_stringSetId}\", " +
                $"\"FretwireId\": \"{_fretwireId}\", \"StringSetName\": \"Savarez 510\" }}");
            // Dread: points at a string set and fretwire that aren't in the library (one is a different kind of item).
            File.WriteAllText(Path.Combine(_newFields, "4.json"),
                $"{{ \"NTW\": \"44\", \"StringSetId\": \"9999\", \"FretwireId\": \"{_stringSetId}\", \"StringSetName\": \"Gone\" }}");
            // Broken: not JSON at all.
            File.WriteAllText(Path.Combine(_newFields, "5.json"), "{ this is not json");
        }

        [TearDown]
        public void TearDown()
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(_folder))
                Directory.Delete(_folder, true);
        }

        SqliteConnection Open()
        {
            var connection = new SqliteConnection($"Data Source={_path};Pooling=False");
            connection.Open();
            return connection;
        }

        void Execute(string sql)
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        long Scalar(string sql)
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            return Convert.ToInt64(command.ExecuteScalar());
        }

        List<string> RunMigration()
        {
            var notes = new List<string>();
            using var connection = Open();
            using var transaction = connection.BeginTransaction();
            LegacyDesignMigration.Run(connection, transaction, _path, notes);
            transaction.Commit();
            return notes;
        }

        static string DumpOldTables(string path)
        {
            using var connection = new SqliteConnection($"Data Source={path};Pooling=False");
            connection.Open();
            var text = new System.Text.StringBuilder();
            foreach (string table in new[] { "designs", "instruments", "bodies", "soundholes" })
            {
                using var command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM {table} ORDER BY id";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                    for (int i = 0; i < reader.FieldCount; i++)
                        text.Append(table).Append(i).Append('=').Append(reader.GetValue(i)).Append(';');
            }
            return text.ToString();
        }

        static GuitarInfo Info(Library library, string name) => library.ListGuitars().Single(g => g.Name == name);

        [Test]
        public void EveryDesign_BecomesAGuitar_WithItsValues()
        {
            RunMigration();

            using var library = Library.Open(_path);
            Assert.That(library.ListGuitars().Select(g => g.Name),
                Is.EquivalentTo(new[] { "OM", "OM 2", "Parlour", "Dread", "Broken" }));

            var om = library.LoadGuitar(Info(library, "OM").Id);
            Assert.That(om.ScaleLength.Value, Is.EqualTo(645));
            Assert.That(om.NeckJoinFret.Value, Is.EqualTo(14));
            var body = om.Body;
            Assert.That(body.Length.Value, Is.EqualTo(495));
            Assert.That(body.UpperBoutWidth.Value, Is.EqualTo(290));
            Assert.That(body.UpperBoutOffsetFromNeckEnd.Value, Is.EqualTo(11));
            Assert.That(body.UpperBoutPrimaryRadius.Value, Is.EqualTo(12));
            Assert.That(body.UpperBoutSecondaryOffset.Value, Is.EqualTo(13));
            Assert.That(body.WaistRadius.Value, Is.EqualTo(14));
            Assert.That(body.WaistWidth.Value, Is.EqualTo(240));
            Assert.That(body.WaistOffsetFromTailEnd.Value, Is.EqualTo(15));
            Assert.That(body.LowerBoutWidth.Value, Is.EqualTo(390));
            Assert.That(body.LowerBoutOffsetFromTailEnd.Value, Is.EqualTo(16));
            Assert.That(body.LowerBoutPrimaryRadius.Value, Is.EqualTo(17));
            Assert.That(body.LowerBoutSecondaryOffset.Value, Is.EqualTo(18));
            Assert.That(body.HeelFlatWidth.Value, Is.EqualTo(60));
            Assert.That(body.DepthAtTailEnd.Value, Is.EqualTo(105));
            Assert.That(body.DepthAtNeckEnd.Value, Is.EqualTo(90));
            Assert.That(body.TopPlate.Soundhole.Value, Is.True);
            Assert.That(body.TopPlate.SoundholeDiameter.Value, Is.EqualTo(100));
            Assert.That(body.TopPlate.SoundholeOffsetFromCenterline.Value, Is.EqualTo(2.5));
            Assert.That(body.TopPlate.SoundholeOffsetFromTailEnd.Value, Is.EqualTo(330));
            Assert.That(om.Neck.Fretboard.NutWidth.HasValue, Is.False);
        }

        [Test]
        public void ADesignWithoutSoundhole_HasNoSoundhole()
        {
            RunMigration();

            using var library = Library.Open(_path);
            var guitar = library.LoadGuitar(Info(library, "OM 2").Id);
            Assert.That(guitar.Body.TopPlate.Soundhole.Value, Is.False);
            Assert.That(guitar.Body.TopPlate.SoundholeDiameter.HasValue, Is.False);
            Assert.That(guitar.ScaleLength.Value, Is.EqualTo(610));
        }

        [Test]
        public void Names_AreTrimmedAndUnique_AndTheOldIdAndTimeAreKept()
        {
            var notes = RunMigration();

            using var library = Library.Open(_path);
            var first = Info(library, "OM");
            var second = Info(library, "OM 2");
            Assert.That(first.LegacyDesignId, Is.EqualTo(1));
            Assert.That(second.LegacyDesignId, Is.EqualTo(2));
            Assert.That(first.Modified, Is.EqualTo(DateTime.Parse(T1, null, System.Globalization.DateTimeStyles.RoundtripKind)));
            Assert.That(second.Modified, Is.EqualTo(DateTime.Parse(T2, null, System.Globalization.DateTimeStyles.RoundtripKind)));
            Assert.That(library.ListGuitars().Select(g => g.Uid).Distinct().Count(), Is.EqualTo(5));
            Assert.That(library.ListGuitars().All(g => !string.IsNullOrEmpty(g.Uid)));
            Assert.That(notes, Has.Some.Contain("\"OM 2\""));
            // The parts saved with a guitar keep the time too.
            Assert.That(Scalar($"SELECT count(*) FROM parts WHERE kind IN ('guitar', 'neck', 'body', 'bracing') " +
                $"AND modified NOT IN ('{T1}', '{T2}', '{T3}', '{T4}') AND id NOT IN " +
                "(SELECT id FROM parts WHERE name = 'Parlour body' OR parent_id IN (SELECT id FROM parts WHERE name = 'Parlour body'))"),
                Is.EqualTo(0));
        }

        [Test]
        public void NewFields_AreApplied_AndAValidLibraryLinkIsKept()
        {
            var notes = RunMigration();

            using var library = Library.Open(_path);
            var parlour = library.LoadGuitar(Info(library, "Parlour").Id);
            Assert.That(parlour.Neck.Fretboard.NutWidth.Value, Is.EqualTo(43.5));
            Assert.That(parlour.Neck.Fretboard.FretCount.Value, Is.EqualTo(19));
            Assert.That(parlour.Neck.Fretboard.Markers.Select(m => m.Fret.Value), Is.EqualTo(new int?[] { 3, 5, 12 }));
            Assert.That(parlour.Neck.StringSet?.Id, Is.EqualTo(_stringSetId));
            Assert.That(parlour.Neck.StringSet.DisplayName, Is.EqualTo("Savarez 510"));
            Assert.That(parlour.Neck.Fretwire?.Id, Is.EqualTo(_fretwireId));
            Assert.That(notes, Has.None.Contain("Design \"Parlour\""));
        }

        [Test]
        public void ALinkToAMissingOrWrongKindOfItem_IsDropped_WithANote()
        {
            var notes = RunMigration();

            using var library = Library.Open(_path);
            var dread = library.LoadGuitar(Info(library, "Dread").Id);
            Assert.That(dread.Neck.Fretboard.NutWidth.Value, Is.EqualTo(44));
            Assert.That(dread.Neck.StringSet, Is.Null);
            Assert.That(dread.Neck.Fretwire, Is.Null);
            Assert.That(notes, Has.Some.Contain("Dread").And.Contain("string set").And.Contain("9999"));
            Assert.That(notes, Has.Some.Contain("Dread").And.Contain("fretwire"));
        }

        [Test]
        public void ACorruptNewFieldsFile_AddsANote_AndTheDesignIsStillConverted()
        {
            var notes = RunMigration();

            using var library = Library.Open(_path);
            var broken = library.LoadGuitar(Info(library, "Broken").Id);
            Assert.That(broken.ScaleLength.Value, Is.EqualTo(640));
            Assert.That(broken.Neck.Fretboard.NutWidth.HasValue, Is.False);
            Assert.That(notes, Has.Some.Contain("Broken").And.Contain("5.json"));
        }

        [Test]
        public void ANewFieldsPathThatIsAFile_DoesNotStopTheConversion()
        {
            Directory.Delete(_newFields, true);
            File.WriteAllText(_newFields, "not a folder");

            var notes = RunMigration();

            using var library = Library.Open(_path);
            Assert.That(library.LoadGuitar(Info(library, "Broken").Id).ScaleLength.Value, Is.EqualTo(640));
            Assert.That(notes, Is.Not.Null);
        }

        [Test]
        public void ABadValueInNewFields_AddsANote_AndTheRestIsApplied()
        {
            File.WriteAllText(Path.Combine(_newFields, "1.json"), "{ \"NTW\": \"wide\", \"FC\": \"20\", \"NOPE\": \"1\" }");

            var notes = RunMigration();

            using var library = Library.Open(_path);
            var om = library.LoadGuitar(Info(library, "OM").Id);
            Assert.That(om.Neck.Fretboard.FretCount.Value, Is.EqualTo(20));
            Assert.That(om.Neck.Fretboard.NutWidth.HasValue, Is.False);
            Assert.That(notes, Has.Some.Contain("NTW"));
            Assert.That(notes, Has.Some.Contain("NOPE"));
        }

        [Test]
        public void BodyPresets_BecomeSavedBodies_AndOtherPresetsAreOnlyNoted()
        {
            var notes = RunMigration();

            using var library = Library.Open(_path);
            var bodies = library.ListAssemblies<Body>().Where(b => b.Name != null).ToList();
            Assert.That(bodies.Select(b => b.Name), Is.EqualTo(new[] { "Parlour body" }));
            var body = library.LoadAssembly<Body>(bodies[0].Id);
            Assert.That(body.Length.Value, Is.EqualTo(440));
            Assert.That(body.LowerBoutWidth.Value, Is.EqualTo(320));

            Assert.That(notes, Has.Some.Contain("2 instrument presets").And.Contain("Short").And.Contain("Long"));
            Assert.That(notes, Has.Some.Contain("1 soundhole preset").And.Contain("Round"));
            Assert.That(notes, Has.Some.Contain("Parlour body"));
            Assert.That(library.ListAssemblies<Neck>().Where(n => n.Name != null), Is.Empty);
        }

        [Test]
        public void OtherDatabaseFilesBesideNewFields_AddANote()
        {
            Assert.That(RunMigration(), Has.None.Contain("shared"));
            Execute("DELETE FROM parts WHERE kind = 'guitar' OR kind = 'body' AND parent_id IS NULL");

            File.WriteAllText(Path.Combine(_folder, "Other.db"), "");
            var notes = RunMigration();

            Assert.That(notes, Has.Some.Contain("shared").And.Contain("Other.db"));
        }

        [Test]
        public void OldTablesAndNewFieldsFiles_AreLeftUntouched()
        {
            string tablesBefore = DumpOldTables(_path);
            var filesBefore = Directory.GetFiles(_newFields).OrderBy(f => f).ToDictionary(f => f, File.ReadAllText);

            RunMigration();

            Assert.That(DumpOldTables(_path), Is.EqualTo(tablesBefore));
            var filesAfter = Directory.GetFiles(_newFields).OrderBy(f => f).ToDictionary(f => f, File.ReadAllText);
            Assert.That(filesAfter, Is.EqualTo(filesBefore));
        }

        [Test]
        public void ADesignWithNoSections_IsStillConverted()
        {
            Execute($"INSERT INTO designs (id, name, created, modified) VALUES (6, 'Empty', '{T1}', '{T1}')");

            RunMigration();

            using var library = Library.Open(_path);
            var empty = library.LoadGuitar(Info(library, "Empty").Id);
            Assert.That(empty.ScaleLength.HasValue, Is.False);
            Assert.That(empty.Body.TopPlate.Soundhole.Value, Is.False);
        }

        [Test]
        public void AFailingSql_RollsBackEverything()
        {
            using (var connection = Open())
            {
                using var transaction = connection.BeginTransaction();
                using (var drop = connection.CreateCommand())
                {
                    drop.Transaction = transaction;
                    drop.CommandText = "DROP TABLE part_values";
                    drop.ExecuteNonQuery();
                }
                Assert.Throws<SqliteException>(() =>
                    LegacyDesignMigration.Run(connection, transaction, _path, new List<string>()));
                transaction.Rollback();
            }

            Assert.That(Scalar("SELECT count(*) FROM sqlite_master WHERE name = 'part_values'"), Is.EqualTo(1));
            Assert.That(Scalar("SELECT count(*) FROM parts WHERE kind = 'guitar'"), Is.EqualTo(0));
            Assert.That(Scalar("SELECT count(*) FROM designs"), Is.EqualTo(5));
        }
    }
}
