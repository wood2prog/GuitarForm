using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using GuitarForm.Model.Parts;
using Microsoft.Data.Sqlite;

namespace GuitarForm.Data
{
    // Converts the designs of the old model (the designs, instruments, bodies and soundholes tables) into guitars of the
    // new model, inside the caller's transaction. The old tables, and the NewFields files beside the library, are left
    // as they are. Anything that can't be converted is left out and described in the notes; only a failing SQL command
    // throws (so the caller's transaction rolls back).
    //
    // The old tables are read raw through the frozen column to abbreviation maps below, not through the old records:
    // the maps must never change, because the old tables never do.
    internal static class LegacyDesignMigration
    {
        // instruments.* and bodies.* columns, by glossary abbreviation (mm; neck_join_fret is a count).
        static readonly (string Column, string Abbreviation)[] InstrumentColumns =
        {
            ("scale_length", "SL"), ("neck_join_fret", "NJF"),
        };

        static readonly (string Column, string Abbreviation)[] BodyColumns =
        {
            ("body_length", "BDL"), ("ub_width", "UBW"), ("ub_offset", "UBO"), ("ub_radius", "UBPR"),
            ("ub_secondary_offset", "UBSO"), ("waist_radius", "WSR"), ("waist_width", "WSW"), ("waist_offset", "WSO"),
            ("lb_width", "LBW"), ("lb_offset", "LBO"), ("lb_radius", "LBPR"), ("lb_secondary_offset", "LBSO"),
            ("heel_width", "HFW"), ("tail_depth", "BDDT"), ("neck_depth", "BDDN"),
        };

        // A soundhole's values live in the new model's body (its top plate).
        static readonly (string Column, string Abbreviation)[] SoundholeColumns =
        {
            ("diameter", "SHD"), ("horizontal_offset", "SHOC"), ("tail_offset", "SHOT"),
        };

        sealed record Design(long Id, string Name, string Modified);

        public static void Run(SqliteConnection connection, SqliteTransaction transaction, string libraryPath,
            List<string> notes)
        {
            var db = new Db(connection, transaction);
            var writer = new PartWriter(connection, transaction);
            string folder = Path.GetDirectoryName(Path.GetFullPath(libraryPath));
            string newFieldsFolder = Path.Combine(folder, "NewFields");

            var designs = new List<Design>();
            using (var query = db.Command("SELECT id, name, modified FROM designs ORDER BY id"))
            using (var reader = query.ExecuteReader())
                while (reader.Read())
                    designs.Add(new Design(reader.GetInt64(0), reader.GetString(1), reader.GetString(2)));

            foreach (var design in designs)
                ConvertDesign(db, writer, design, newFieldsFolder, notes);

            ConvertBodyPresets(db, writer, notes);
            NoteUnconvertedPresets(db, notes);

            if (Directory.Exists(newFieldsFolder))
            {
                List<string> others;
                try
                {
                    others = Directory.EnumerateFiles(folder)
                        .Where(f => new[] { ".db", ".sqlite", ".sqlite3" }.Contains(Path.GetExtension(f).ToLowerInvariant()))
                        .Where(f => !string.Equals(Path.GetFullPath(f), Path.GetFullPath(libraryPath), StringComparison.OrdinalIgnoreCase))
                        .Select(Path.GetFileName).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
                }
                catch (Exception e) when (e is not SqliteException)
                {
                    notes.Add($"The library's folder couldn't be scanned for other libraries sharing the NewFields folder ({e.Message.Trim()}).");
                    others = new List<string>();
                }
                if (others.Count > 0)
                    notes.Add($"The NewFields folder may be shared with other libraries in the same folder ({string.Join(", ", others)}). " +
                        "Its files were converted for this library anyway.");
            }
        }

        static void ConvertDesign(Db db, PartWriter writer, Design design, string newFieldsFolder, List<string> notes)
        {
            string label = $"Design \"{design.Name.Trim()}\"";
            var problems = new List<string>();

            var guitarValues = ReadRow(db, "instruments", "design_id", design.Id, InstrumentColumns, out _);
            var bodyValues = ReadRow(db, "bodies", "design_id", design.Id, BodyColumns, out _);
            var soundholeValues = ReadRow(db, "soundholes", "design_id", design.Id, SoundholeColumns, out bool hasSoundhole);
            if (hasSoundhole)
                bodyValues.Add(new PartValue("SH", null, true));
            bodyValues.AddRange(soundholeValues);

            var guitar = (Guitar)new Guitar().WithStoredValues(guitarValues, problems);
            guitar = guitar with { Body = (Body)guitar.Body.WithStoredValues(bodyValues, problems) };

            guitar = ApplyNewFields(db, guitar, design, newFieldsFolder, problems);
            notes.AddRange(problems.Select(p => $"{label}: {p}"));

            string name = UnusedGuitarName(db, design.Name);
            if (!string.Equals(name, design.Name.Trim(), StringComparison.Ordinal))
                notes.Add($"{label} was converted to the guitar \"{name}\", because a guitar with that name already exists.");

            long id = writer.Insert(guitar, null, name, PartWriter.NewUid(), design.Id);

            // Keep the design's modified time on the guitar and the parts saved with it.
            using var update = db.Command(@"
                UPDATE parts SET modified = $modified
                WHERE id = $id OR parent_id = $id OR parent_id IN (SELECT id FROM parts WHERE parent_id = $id)");
            update.Parameters.AddWithValue("$modified", design.Modified);
            update.Parameters.AddWithValue("$id", id);
            update.ExecuteNonQuery();
        }

        // The values the old sections didn't have, kept per design in NewFields/<id>.json. A file or value that can't be
        // read is skipped with a note.
        static Guitar ApplyNewFields(Db db, Guitar guitar, Design design, string newFieldsFolder, List<string> problems)
        {
            string path = Path.Combine(newFieldsFolder, design.Id.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".json");
            if (!File.Exists(path))
                return guitar;

            Dictionary<string, string> texts;
            try
            {
                texts = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path));
                if (texts == null)
                    throw new JsonException("The file is empty.");
            }
            catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
            {
                problems.Add($"the new fields in {path} were skipped: the file can't be read ({e.Message.Trim()}).");
                return guitar;
            }

            LegacyNewFields.Result result;
            try
            {
                result = LegacyNewFields.Apply(guitar, texts);
            }
            catch (Exception e) when (e is not SqliteException)
            {
                problems.Add($"the new fields in {path} were skipped: they couldn't be applied ({e.Message.Trim()}).");
                return guitar;
            }
            problems.AddRange(result.Problems);
            guitar = result.Guitar;

            if (result.StringSetId is long setId)
            {
                if (db.KindOf(setId) == "string_set")
                    guitar = guitar with { Neck = guitar.Neck with { StringSet = new StringSet { Id = setId } } };
                else
                    problems.Add($"its string set (id {setId}{Named(result.StringSetName)}) isn't in the library, so the link was dropped.");
            }
            if (result.FretwireId is long wireId)
            {
                if (db.KindOf(wireId) == "fretwire")
                    guitar = guitar with { Neck = guitar.Neck with { Fretwire = new Fretwire { Id = wireId } } };
                else
                    problems.Add($"its fretwire (id {wireId}{Named(result.FretwireName)}) isn't in the library, so the link was dropped.");
            }
            return guitar;
        }

        static string Named(string name) => string.IsNullOrWhiteSpace(name) ? "" : $", \"{name}\"";

        static void ConvertBodyPresets(Db db, PartWriter writer, List<string> notes)
        {
            var presets = new List<(string Name, List<PartValue> Values)>();
            using (var query = db.Command("SELECT id, preset_name FROM bodies WHERE preset_name IS NOT NULL ORDER BY id"))
            using (var reader = query.ExecuteReader())
            {
                var rows = new List<(long Id, string Name)>();
                while (reader.Read())
                    rows.Add((reader.GetInt64(0), reader.GetString(1)));
                reader.Close();
                foreach (var (id, name) in rows)
                    presets.Add((name, ReadRow(db, "bodies", "id", id, BodyColumns, out _)));
            }

            foreach (var (presetName, values) in presets)
            {
                var problems = new List<string>();
                var body = (Body)new Body().WithStoredValues(values, problems);
                string name = UnusedName(db, "body", presetName);
                writer.Insert(body, null, name);
                notes.AddRange(problems.Select(p => $"Body preset \"{presetName}\": {p}"));
                if (name != presetName.Trim())
                    notes.Add($"Body preset \"{presetName}\" was saved as the body \"{name}\", because a body with that name already exists.");
            }
            if (presets.Count > 0)
                notes.Add($"{presets.Count} body preset{(presets.Count == 1 ? "" : "s")} became saved bod{(presets.Count == 1 ? "y" : "ies")}: " +
                    string.Join(", ", presets.Select(p => $"\"{p.Name}\"")) + ".");
        }

        static void NoteUnconvertedPresets(Db db, List<string> notes)
        {
            foreach (var (table, label) in new[] { ("instruments", "instrument"), ("soundholes", "soundhole") })
            {
                var names = new List<string>();
                using var query = db.Command($"SELECT preset_name FROM {table} WHERE preset_name IS NOT NULL ORDER BY preset_name COLLATE NOCASE");
                using var reader = query.ExecuteReader();
                while (reader.Read())
                    names.Add(reader.GetString(0));
                if (names.Count > 0)
                    notes.Add($"{names.Count} {label} preset{(names.Count == 1 ? " was" : "s were")} not converted (the new model has no {label} presets): " +
                        string.Join(", ", names.Select(n => $"\"{n}\"")) + ".");
            }
        }

        // The row's non-null columns as part values, in the order of the map. hasRow says whether the row exists.
        static List<PartValue> ReadRow(Db db, string table, string keyColumn, long key,
            (string Column, string Abbreviation)[] map, out bool hasRow)
        {
            var values = new List<PartValue>();
            using var query = db.Command(
                $"SELECT {string.Join(", ", map.Select(m => m.Column))} FROM {table} WHERE {keyColumn} = $key");
            query.Parameters.AddWithValue("$key", key);
            using var reader = query.ExecuteReader();
            hasRow = reader.Read();
            if (hasRow)
                for (int i = 0; i < map.Length; i++)
                    if (!reader.IsDBNull(i))
                        values.Add(new PartValue(map[i].Abbreviation, null, reader.GetValue(i)));
            return values;
        }

        static string UnusedGuitarName(Db db, string name)
        {
            name = name?.Trim() ?? "";
            if (name.Length == 0)
                name = "Guitar";
            return UnusedName(db, "guitar", name);
        }

        // The same rule as Library.UnusedGuitarName: the name, or "name 2", "name 3" and so on.
        static string UnusedName(Db db, string kind, string name)
        {
            name = name.Trim();
            string candidate = name;
            for (int n = 2; db.TopLevelExists(kind, candidate); n++)
                candidate = $"{name} {n}";
            return candidate;
        }

        sealed class Db
        {
            readonly SqliteConnection _connection;
            readonly SqliteTransaction _transaction;

            public Db(SqliteConnection connection, SqliteTransaction transaction)
            {
                _connection = connection;
                _transaction = transaction;
            }

            public SqliteCommand Command(string sql)
            {
                var command = _connection.CreateCommand();
                command.CommandText = sql;
                command.Transaction = _transaction;
                return command;
            }

            public string KindOf(long id)
            {
                using var query = Command("SELECT kind FROM parts WHERE id = $id");
                query.Parameters.AddWithValue("$id", id);
                return query.ExecuteScalar() as string;
            }

            public bool TopLevelExists(string kind, string name)
            {
                using var query = Command("SELECT 1 FROM parts WHERE kind = $kind AND parent_id IS NULL AND name = $name");
                query.Parameters.AddWithValue("$kind", kind);
                query.Parameters.AddWithValue("$name", name);
                return query.ExecuteScalar() != null;
            }
        }
    }
}
