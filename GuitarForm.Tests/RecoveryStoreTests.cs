using System;
using System.IO;
using GuitarForm.Data;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    public class RecoveryStoreTests
    {
        static readonly string Lib = Path.Combine(Path.GetTempPath(), "GuitarFormTests", "lib", "GuitarForm.db");
        static readonly string Other = Path.Combine(Path.GetTempPath(), "GuitarFormTests", "other", "GuitarForm.db");
        string _folder;
        RecoveryStore _store;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "GuitarFormTests", Guid.NewGuid().ToString("N"));
            _store = new RecoveryStore(_folder);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_folder))
                Directory.Delete(_folder, true);
        }

        [Test]
        public void RoundTrips()
        {
            _store.Write(Lib, 3, "OM", "{\"a\":1}");
            var recovery = _store.TryRead(Lib, 3);
            Assert.That(_store.Exists(Lib, 3));
            Assert.That(recovery.Name, Is.EqualTo("OM"));
            Assert.That(recovery.Document, Is.EqualTo("{\"a\":1}"));
            Assert.That(recovery.SavedAtUtc, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromMinutes(1)));
        }

        [Test]
        public void OverwriteReplacesAndLeavesNoTempFile()
        {
            _store.Write(Lib, 1, "A", "first");
            _store.Write(Lib, 1, "B", "second");
            Assert.That(_store.TryRead(Lib, 1).Document, Is.EqualTo("second"));
            Assert.That(Directory.GetFiles(_folder), Has.Length.EqualTo(1));
        }

        [Test]
        public void DeleteRemovesTheFileAndIgnoresAMissingOne()
        {
            _store.Write(Lib, 1, "A", "x");
            _store.Delete(Lib, 1);
            Assert.That(_store.Exists(Lib, 1), Is.False);
            Assert.DoesNotThrow(() => _store.Delete(Lib, 1));
        }

        [Test]
        public void MissingFileReadsAsNull() => Assert.That(_store.TryRead(Lib, 9), Is.Null);

        [Test]
        public void CorruptFileReadsAsNull()
        {
            _store.Write(Lib, 4, "x", "y");
            File.WriteAllText(Directory.GetFiles(_folder)[0], "{ not json");
            Assert.That(_store.TryRead(Lib, 4), Is.Null);
        }

        [Test]
        public void DesignsAreKeptSeparately()
        {
            _store.Write(Lib, 1, "A", "one");
            _store.Write(Lib, 2, "B", "two");
            Assert.That(_store.TryRead(Lib, 1).Document, Is.EqualTo("one"));
            Assert.That(_store.TryRead(Lib, 2).Document, Is.EqualTo("two"));
        }

        [Test]
        public void ADifferentLibraryDoesNotSeeTheFile()
        {
            _store.Write(Lib, 1, "A", "one");
            Assert.That(_store.TryRead(Other, 1), Is.Null);
            _store.Write(Other, 1, "B", "two");
            Assert.That(_store.TryRead(Lib, 1).Document, Is.EqualTo("one"));
            Assert.That(_store.TryRead(Other, 1).Document, Is.EqualTo("two"));
        }

        [Test]
        public void StaleFilesForMissingDesignsAreDeleted()
        {
            _store.Write(Lib, 1, "A", "one");
            _store.Write(Lib, 2, "B", "two");
            _store.Write(Other, 2, "C", "three");
            _store.DeleteStale(Lib, new long[] { 1 });
            Assert.That(_store.Exists(Lib, 1));
            Assert.That(_store.Exists(Lib, 2), Is.False);
            Assert.That(_store.Exists(Other, 2), Is.True);
        }
    }
}
