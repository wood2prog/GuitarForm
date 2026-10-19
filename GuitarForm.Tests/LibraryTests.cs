using System;
using System.IO;
using GuitarForm.Data;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    // Checks saving and loading the library file: opening, upgrading and rejecting other files, against a new library in a temporary folder for each test.
    public class LibraryTests
    {
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
            Assert.That(Pragma("application_id"), Is.EqualTo(Schema.ApplicationId));
            Assert.That(Pragma("user_version"), Is.EqualTo(Schema.LatestVersion));
        }

        [Test]
        public void Open_RejectsAnotherKindOfDatabase()
        {
            Directory.CreateDirectory(_folder);
            Execute("CREATE TABLE something (x)");

            Assert.That(() => Library.Open(_path), Throws.TypeOf<InvalidDataException>());
            Assert.That(Directory.GetFiles(_folder, "*.bak*"), Is.Empty);
            Assert.That(Directory.GetFiles(_folder, "*.tmp"), Is.Empty);
        }

        [Test]
        public void Open_RejectsALibraryFromANewerVersion()
        {
            using (Library.Open(_path)) { }
            Execute("PRAGMA user_version = 99");

            Assert.That(() => Library.Open(_path), Throws.TypeOf<InvalidDataException>());
        }

        [Test]
        public void Open_Again_KeepsTheGuitars()
        {
            var om = TestGuitars.OM();
            long id;
            using (var library = Library.Open(_path))
                id = library.CreateGuitar(om);

            using var reopened = Library.Open(_path);
            TestGuitars.AssertSameValues(reopened.LoadGuitar(id), om);
        }
    }
}
