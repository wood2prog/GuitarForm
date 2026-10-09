using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using GuitarForm.Model;
using Microsoft.Data.Sqlite;

namespace GuitarForm.Data
{
    // A saved design in the list of designs.
    public sealed record DesignInfo(long Id, string Name, DateTime Modified);

    // The SQLite database that holds designs and presets. A design owns its own copy of each section; a preset is a
    // named section saved in the library, whose values are copied into a design when it's loaded.
    public sealed class Library : IDisposable
    {
        public string Path { get; }

        readonly SqliteConnection _connection;
        SqliteTransaction _transaction;

        Library(string path, SqliteConnection connection)
        {
            Path = path;
            _connection = connection;
        }

        public string SqliteVersion => _connection.ServerVersion;

        // Opens the library, creating it (and its folder) if the file doesn't exist, and upgrading its tables if it was
        // made by an older GuitarForm. Throws InvalidDataException if the file isn't a library this version can read.
        public static Library Open(string path)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path)));
            var connection = Connect(path, SqliteOpenMode.ReadWriteCreate);
            try
            {
                Schema.Prepare(connection, path);
                return new Library(path, connection);
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        // Pooling is off so the file is released as soon as the connection closes, for moving, backing up over or
        // restoring it.
        internal static SqliteConnection Connect(string path, SqliteOpenMode mode)
        {
            var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = mode,
                Pooling = false,
                ForeignKeys = true,
            }.ToString());
            connection.Open();
            return connection;
        }

        public void Dispose() => _connection.Dispose();

        // --- Designs ---

        public IReadOnlyList<DesignInfo> ListDesigns()
        {
            using var query = Command("SELECT id, name, modified FROM designs ORDER BY name COLLATE NOCASE");
            using var reader = query.ExecuteReader();
            var designs = new List<DesignInfo>();
            while (reader.Read())
                designs.Add(new DesignInfo(reader.GetInt64(0), reader.GetString(1), ParseTime(reader.GetString(2))));
            return designs;
        }

        public bool DesignExists(string name)
        {
            using var query = Command("SELECT count(*) FROM designs WHERE name = $name");
            query.Parameters.AddWithValue("$name", name);
            return (long)query.ExecuteScalar() > 0;
        }

        // Saves a new design and returns its id. Throws InvalidOperationException if the name is empty or taken
        // (names are compared ignoring case).
        public long CreateDesign(GuitarDesign design)
        {
            CheckDesign(design);
            return InTransaction(() =>
            {
                string now = Now();
                using var insert = Command(
                    "INSERT INTO designs (name, created, modified) VALUES ($name, $now, $now) RETURNING id");
                insert.Parameters.AddWithValue("$name", design.Name);
                insert.Parameters.AddWithValue("$now", now);
                long id = (long)insert.ExecuteScalar();
                InsertSections(id, design);
                return id;
            });
        }

        // Throws KeyNotFoundException if there's no design with the id.
        public GuitarDesign LoadDesign(long id)
        {
            using var query = Command("SELECT name FROM designs WHERE id = $id");
            query.Parameters.AddWithValue("$id", id);
            if (query.ExecuteScalar() is not string name)
                throw new KeyNotFoundException($"There's no design with id {id}.");

            return new GuitarDesign
            {
                Name = name,
                Instrument = ReadDesignSection(SectionTables.Instruments, id),
                Body = ReadDesignSection(SectionTables.Bodies, id),
                Soundhole = ReadDesignSection(SectionTables.Soundholes, id),
            };
        }

        // Overwrites a saved design, including its name.
        public void SaveDesign(long id, GuitarDesign design)
        {
            CheckDesign(design, id);
            InTransaction(() =>
            {
                using var update = Command("UPDATE designs SET name = $name, modified = $now WHERE id = $id");
                update.Parameters.AddWithValue("$name", design.Name);
                update.Parameters.AddWithValue("$now", Now());
                update.Parameters.AddWithValue("$id", id);
                if (update.ExecuteNonQuery() == 0)
                    throw new KeyNotFoundException($"There's no design with id {id}.");

                foreach (string table in new[] { "instruments", "bodies", "soundholes" })
                {
                    using var delete = Command($"DELETE FROM {table} WHERE design_id = $id");
                    delete.Parameters.AddWithValue("$id", id);
                    delete.ExecuteNonQuery();
                }
                InsertSections(id, design);
                return 0;
            });
        }

        // Saves a copy of a design under a name not already used ("OM copy", "OM copy 2", …) and returns its id.
        public long CopyDesign(long id)
        {
            var design = LoadDesign(id);
            return CreateDesign(design with { Name = UnusedDesignName(design.Name + " copy") });
        }

        public void DeleteDesign(long id)
        {
            using var delete = Command("DELETE FROM designs WHERE id = $id");
            delete.Parameters.AddWithValue("$id", id);
            delete.ExecuteNonQuery();
        }

        // The name, or the name followed by the lowest number from 2 up that makes it unused.
        public string UnusedDesignName(string name)
        {
            string candidate = name;
            for (int n = 2; DesignExists(candidate); n++)
                candidate = $"{name} {n}";
            return candidate;
        }

        void CheckDesign(GuitarDesign design, long? id = null)
        {
            if (string.IsNullOrWhiteSpace(design.Name))
                throw new InvalidOperationException("A design needs a name.");
            if (design.Instrument == null || design.Body == null)
                throw new InvalidOperationException("A design needs an instrument and a body.");

            using var query = Command("SELECT count(*) FROM designs WHERE name = $name AND id IS NOT $id");
            query.Parameters.AddWithValue("$name", design.Name);
            query.Parameters.AddWithValue("$id", (object)id ?? DBNull.Value);
            if ((long)query.ExecuteScalar() > 0)
                throw new InvalidOperationException($"There's already a design called \"{design.Name}\".");
        }

        void InsertSections(long id, GuitarDesign design)
        {
            SectionTables.Instruments.Insert(Command, id, null, design.Instrument);
            SectionTables.Bodies.Insert(Command, id, null, design.Body);
            if (design.Soundhole != null)
                SectionTables.Soundholes.Insert(Command, id, null, design.Soundhole);
        }

        T ReadDesignSection<T>(SectionTable<T> table, long id)
        {
            using var query = Command($"SELECT {table.SelectColumns} FROM {table.Table} WHERE design_id = $id");
            query.Parameters.AddWithValue("$id", id);
            return table.ReadSingle(query);
        }

        // --- Presets ---
        // T is a section record: Instrument, Body or Soundhole. Preset names are per section and compared ignoring case.

        public IReadOnlyList<string> ListPresets<T>()
        {
            var table = TableFor<T>();
            using var query = Command(
                $"SELECT preset_name FROM {table.Table} WHERE preset_name IS NOT NULL ORDER BY preset_name COLLATE NOCASE");
            using var reader = query.ExecuteReader();
            var names = new List<string>();
            while (reader.Read())
                names.Add(reader.GetString(0));
            return names;
        }

        public bool PresetExists<T>(string name)
        {
            var table = TableFor<T>();
            using var query = Command($"SELECT count(*) FROM {table.Table} WHERE preset_name = $name");
            query.Parameters.AddWithValue("$name", name);
            return (long)query.ExecuteScalar() > 0;
        }

        // Throws KeyNotFoundException if there's no preset with the name.
        public T LoadPreset<T>(string name)
        {
            var table = TableFor<T>();
            using var query = Command($"SELECT {table.SelectColumns} FROM {table.Table} WHERE preset_name = $name");
            query.Parameters.AddWithValue("$name", name);
            var preset = table.ReadSingle(query);
            return preset ?? throw new KeyNotFoundException($"There's no {typeof(T).Name} preset called \"{name}\".");
        }

        // Saves the section as a preset, replacing any preset with the same name.
        public void SavePreset<T>(string name, T section)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("A preset needs a name.");
            if (section == null)
                throw new ArgumentNullException(nameof(section));

            var table = TableFor<T>();
            InTransaction(() =>
            {
                DeletePresetRow(table, name);
                table.Insert(Command, null, name, section);
                return 0;
            });
        }

        public void DeletePreset<T>(string name) => DeletePresetRow(TableFor<T>(), name);

        void DeletePresetRow<T>(SectionTable<T> table, string name)
        {
            using var delete = Command($"DELETE FROM {table.Table} WHERE preset_name = $name");
            delete.Parameters.AddWithValue("$name", name);
            delete.ExecuteNonQuery();
        }

        static SectionTable<T> TableFor<T>()
        {
            object table =
                typeof(T) == typeof(Instrument) ? SectionTables.Instruments :
                typeof(T) == typeof(Body) ? SectionTables.Bodies :
                typeof(T) == typeof(Soundhole) ? SectionTables.Soundholes :
                null;
            return (SectionTable<T>)table ?? throw new ArgumentException($"{typeof(T).Name} isn't a design section.");
        }

        // --- Helpers ---

        SqliteCommand Command(string sql)
        {
            var command = _connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = _transaction;
            return command;
        }

        TResult InTransaction<TResult>(Func<TResult> action)
        {
            using var transaction = _connection.BeginTransaction();
            _transaction = transaction;
            try
            {
                var result = action();
                transaction.Commit();
                return result;
            }
            finally
            {
                _transaction = null;
            }
        }

        static string Now() => DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

        static DateTime ParseTime(string text) =>
            DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }
}
