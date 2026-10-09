using System.IO;
using Microsoft.Data.Sqlite;

namespace GuitarForm.Data
{
    // The library's tables. The schema version is kept in PRAGMA user_version, and Migrations[i] upgrades a library
    // from version i to i + 1. Never edit a migration that has been released: add a new one instead.
    //
    // Each section has its own table. A section row belongs either to a design (design_id) or to the library as a named
    // preset (preset_name), never both. A design has one row in each required section's table, and at most one in
    // each optional section's table.
    static class Schema
    {
        // Stored in the file header to mark the file as a GuitarForm library ("GtFm").
        public const long ApplicationId = 0x4774466D;

        static readonly string[] Migrations =
        {
            // 0 -> 1: designs, instruments, bodies and soundholes.
            @"
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
            ",
        };

        public static int LatestVersion => Migrations.Length;

        // Marks a new, empty database as a GuitarForm library and brings the schema up to date. Throws
        // InvalidDataException if the file is another kind of database, or a library made by a newer GuitarForm.
        public static void Prepare(SqliteConnection connection, string path)
        {
            long applicationId = (long)Scalar(connection, "PRAGMA application_id");
            if (applicationId == 0 && (long)Scalar(connection, "SELECT count(*) FROM sqlite_master") == 0)
                Scalar(connection, $"PRAGMA application_id = {ApplicationId}");
            else if (applicationId != ApplicationId)
                throw new InvalidDataException($"{path} is not a GuitarForm library.");

            int version = (int)(long)Scalar(connection, "PRAGMA user_version");
            if (version > LatestVersion)
                throw new InvalidDataException(
                    $"{path} was made by a newer GuitarForm (library version {version}; this version reads up to {LatestVersion}).");

            for (; version < LatestVersion; version++)
            {
                using var transaction = connection.BeginTransaction();
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = Migrations[version] + $"PRAGMA user_version = {version + 1};";
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        static object Scalar(SqliteConnection connection, string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            return command.ExecuteScalar();
        }
    }
}
