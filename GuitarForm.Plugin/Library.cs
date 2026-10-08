using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace GuitarForm.Plugin
{
    // The SQLite database that holds designs and presets.
    static class Library
    {
        // Stored in the file header to mark the file as a GuitarForm library ("GtFm").
        public const long ApplicationId = 0x4774466D;

        public static string DefaultPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GuitarForm", "GuitarForm.db");

        // Opens the library, creating it if the file doesn't exist. Throws if the file is a database that isn't a
        // GuitarForm library.
        public static SqliteConnection Open(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadWriteCreate,
            }.ToString());
            try
            {
                connection.Open();
                long applicationId = (long)Scalar(connection, "PRAGMA application_id");
                if (applicationId == 0 && (long)Scalar(connection, "SELECT count(*) FROM sqlite_master") == 0)
                    Scalar(connection, $"PRAGMA application_id = {ApplicationId}");
                else if (applicationId != ApplicationId)
                    throw new InvalidDataException($"{path} is not a GuitarForm library.");
                return connection;
            }
            catch
            {
                connection.Dispose();
                throw;
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
