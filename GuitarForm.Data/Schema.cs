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

            // 1 -> 2: guitars and their parts, for the model in GuitarForm.Model.Parts.
            //
            // parts holds a row for each part saved on its own: a guitar; a neck, body or bracing, which belongs to a
            // guitar (or a body, for bracing) or is a named copy with no parent; or a fretwire or string set, which is
            // a library item that necks refer to. part_values holds each part's values by glossary abbreviation, with
            // item the place in a list (-1 when the value isn't in one).
            @"
            CREATE TABLE parts (
                id INTEGER PRIMARY KEY,
                kind TEXT NOT NULL CHECK (kind IN ('guitar', 'neck', 'body', 'bracing', 'fretwire', 'string_set')),
                parent_id INTEGER REFERENCES parts (id) ON DELETE CASCADE,
                name TEXT COLLATE NOCASE CHECK (name <> ''),
                modified TEXT NOT NULL,
                fretwire_id INTEGER REFERENCES parts (id),
                string_set_id INTEGER REFERENCES parts (id),
                CHECK (CASE WHEN kind IN ('guitar', 'fretwire', 'string_set')
                            THEN parent_id IS NULL AND name IS NOT NULL
                            ELSE (parent_id IS NULL) = (name IS NOT NULL) END)
            );

            CREATE UNIQUE INDEX parts_names ON parts (kind, name) WHERE parent_id IS NULL;
            CREATE INDEX parts_parents ON parts (parent_id);
            CREATE INDEX parts_fretwires ON parts (fretwire_id);
            CREATE INDEX parts_string_sets ON parts (string_set_id);

            CREATE TABLE part_values (
                part_id INTEGER NOT NULL REFERENCES parts (id) ON DELETE CASCADE,
                abbreviation TEXT NOT NULL,
                item INTEGER NOT NULL DEFAULT -1,
                value NOT NULL,
                PRIMARY KEY (part_id, abbreviation, item)
            ) WITHOUT ROWID;
            ",
        };

        public static int LatestVersion => Migrations.Length;

        // Marks a new, empty database as a GuitarForm library and brings the schema up to date. Throws
        // InvalidDataException if the file is another kind of database, or a library made by a newer GuitarForm.
        public static void Prepare(SqliteConnection connection, string path)
        {
            if ((long)Scalar(connection, "PRAGMA application_id") == 0 &&
                (long)Scalar(connection, "SELECT count(*) FROM sqlite_master") == 0)
                Scalar(connection, $"PRAGMA application_id = {ApplicationId}");

            int version = Check(connection, path);
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

        // Returns the library's schema version. Throws InvalidDataException if the file is another kind of database,
        // or a library made by a newer GuitarForm. Doesn't change the file.
        public static int Check(SqliteConnection connection, string path)
        {
            if ((long)Scalar(connection, "PRAGMA application_id") != ApplicationId)
                throw new InvalidDataException($"{path} is not a GuitarForm library.");

            int version = (int)(long)Scalar(connection, "PRAGMA user_version");
            if (version > LatestVersion)
                throw new InvalidDataException(
                    $"{path} was made by a newer GuitarForm (library version {version}; this version reads up to {LatestVersion}).");
            return version;
        }

        // The version an existing library is about to be upgraded from, or 0 if no upgrade is due (a new or empty file,
        // another kind of database, or a library already up to date or newer, which Prepare deals with).
        public static int UpgradeFrom(SqliteConnection connection)
        {
            if ((long)Scalar(connection, "PRAGMA application_id") != ApplicationId)
                return 0;
            int version = (int)(long)Scalar(connection, "PRAGMA user_version");
            return version > 0 && version < LatestVersion ? version : 0;
        }

        static object Scalar(SqliteConnection connection, string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            return command.ExecuteScalar();
        }
    }
}
