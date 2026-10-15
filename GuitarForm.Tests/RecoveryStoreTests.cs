using System;
using System.IO;
using GuitarForm.Data;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    public class RecoveryStoreTests
    {
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
            _store.Write(3, "OM", "{\"a\":1}");
            var recovery = _store.TryRead(3);
            Assert.That(_store.Exists(3));
            Assert.That(recovery.Name, Is.EqualTo("OM"));
            Assert.That(recovery.Document, Is.EqualTo("{\"a\":1}"));
            Assert.That(recovery.SavedAtUtc, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromMinutes(1)));
        }

        [Test]
        public void OverwriteReplacesAndLeavesNoTempFile()
        {
            _store.Write(1, "A", "first");
            _store.Write(1, "B", "second");
            Assert.That(_store.TryRead(1).Document, Is.EqualTo("second"));
            Assert.That(Directory.GetFiles(_folder), Has.Length.EqualTo(1));
        }

        [Test]
        public void DeleteRemovesTheFileAndIgnoresAMissingOne()
        {
            _store.Write(1, "A", "x");
            _store.Delete(1);
            Assert.That(_store.Exists(1), Is.False);
            Assert.DoesNotThrow(() => _store.Delete(1));
        }

        [Test]
        public void MissingFileReadsAsNull() => Assert.That(_store.TryRead(9), Is.Null);

        [Test]
        public void CorruptFileReadsAsNull()
        {
            Directory.CreateDirectory(_folder);
            File.WriteAllText(Path.Combine(_folder, "4.json"), "{ not json");
            Assert.That(_store.TryRead(4), Is.Null);
        }

        [Test]
        public void DesignsAreKeptSeparately()
        {
            _store.Write(1, "A", "one");
            _store.Write(2, "B", "two");
            Assert.That(_store.TryRead(1).Document, Is.EqualTo("one"));
            Assert.That(_store.TryRead(2).Document, Is.EqualTo("two"));
        }
    }
}
