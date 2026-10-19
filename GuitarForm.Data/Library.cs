using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;

namespace GuitarForm.Data
{
    // The SQLite database that holds guitars, their parts, named copies of parts and the fretwire and string sets (see
    // Library.Parts.cs). The tables of the first versions (designs and section presets) exist only until the upgrade
    // that converts them (LegacyDesignMigration, version 4) is followed by version 5, which drops them.
    public sealed partial class Library : IDisposable
    {
        public string Path { get; }

        readonly SqliteConnection _connection;
        SqliteTransaction _transaction;

        static readonly object PendingLock = new();
        static readonly List<string> Pending = new();

        Library(string path, SqliteConnection connection, IReadOnlyList<string> upgradeNotes)
        {
            Path = path;
            _connection = connection;
            UpgradeNotes = upgradeNotes;
        }

        // What the upgrade that this Open ran had to say (designs that were renamed, values that couldn't be read,
        // presets that weren't converted). Empty when no upgrade ran.
        public IReadOnlyList<string> UpgradeNotes { get; }

        // The notes of upgrades since the last call, once: for the panel to show when it first opens the library.
        // (The same notes are in the text file beside the safety copy.)
        public static IReadOnlyList<string> TakeUpgradeNotes()
        {
            lock (PendingLock)
            {
                var notes = Pending.ToArray();
                Pending.Clear();
                return notes;
            }
        }

        public string SqliteVersion => _connection.ServerVersion;

        // Opens the library, creating it (and its folder) if the file doesn't exist, and upgrading its tables if it was
        // made by an older GuitarForm. Throws InvalidDataException if the file isn't a library this version can read.
        public static Library Open(string path)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path)));
            bool existed = File.Exists(path);
            var connection = Connect(path, SqliteOpenMode.ReadWriteCreate);
            try
            {
                // A real upgrade is about to run: keep a copy of the file as it is, and don't migrate without one.
                int from = existed ? Schema.UpgradeFrom(connection) : 0;
                string safetyCopy = from > 0 ? Backups.SafetyCopy(path, from) : null;
                var notes = new List<string>();
                Schema.Prepare(connection, path, notes);
                if (safetyCopy == null)
                    notes.Clear(); // A new library has nothing to report.
                else if (notes.Count > 0)
                {
                    Backups.WriteNotes(safetyCopy, notes);
                    lock (PendingLock)
                        Pending.AddRange(notes);
                }
                return new Library(path, connection, notes.AsReadOnly());
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
            using (var timeout = connection.CreateCommand())
            {
                timeout.CommandText = "PRAGMA busy_timeout = 5000";
                timeout.ExecuteNonQuery();
            }
            return connection;
        }

        public void Dispose() => _connection.Dispose();

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
            catch (SqliteException e) when (e.SqliteErrorCode == 19 && e.SqliteExtendedErrorCode is 2067 or 1555 && IsNameConflict(e))
            {
                // A name check that passed lost a race, or was skipped: give the same kind of message as the checks.
                throw new InvalidOperationException("There's already an item with that name.", e);
            }
            finally
            {
                _transaction = null;
            }
        }

        // Whether a unique violation concerns a name (designs.name, preset_name, the parts' kind and name index).
        static bool IsNameConflict(SqliteException e) =>
            e.Message.Contains("name", StringComparison.OrdinalIgnoreCase);

        internal static string Now() => DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

        static DateTime ParseTime(string text) =>
            DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }
}
