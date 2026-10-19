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
            _store.Write(Lib, "guitar-3", "OM", "{\"a\":1}");
            var recovery = _store.TryRead(Lib, "guitar-3");
            Assert.That(_store.Exists(Lib, "guitar-3"));
            Assert.That(recovery.Name, Is.EqualTo("OM"));
            Assert.That(recovery.Document, Is.EqualTo("{\"a\":1}"));
            Assert.That(recovery.SavedAtUtc, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromMinutes(1)));
        }

        [Test]
        public void OverwriteReplacesAndLeavesNoTempFile()
        {
            _store.Write(Lib, "guitar-1", "A", "first");
            _store.Write(Lib, "guitar-1", "B", "second");
            Assert.That(_store.TryRead(Lib, "guitar-1").Document, Is.EqualTo("second"));
            Assert.That(Directory.GetFiles(_folder), Has.Length.EqualTo(1));
        }

        [Test]
        public void DeleteRemovesTheFileAndIgnoresAMissingOne()
        {
            _store.Write(Lib, "guitar-1", "A", "x");
            _store.Delete(Lib, "guitar-1");
            Assert.That(_store.Exists(Lib, "guitar-1"), Is.False);
            Assert.DoesNotThrow(() => _store.Delete(Lib, "guitar-1"));
        }

        [Test]
        public void MissingFileReadsAsNull() => Assert.That(_store.TryRead(Lib, "guitar-9"), Is.Null);

        [Test]
        public void CorruptFileReadsAsNull()
        {
            _store.Write(Lib, "guitar-4", "x", "y");
            File.WriteAllText(Directory.GetFiles(_folder)[0], "{ not json");
            Assert.That(_store.TryRead(Lib, "guitar-4"), Is.Null);
        }

        [Test]
        public void DesignsAreKeptSeparately()
        {
            _store.Write(Lib, "guitar-1", "A", "one");
            _store.Write(Lib, "guitar-2", "B", "two");
            Assert.That(_store.TryRead(Lib, "guitar-1").Document, Is.EqualTo("one"));
            Assert.That(_store.TryRead(Lib, "guitar-2").Document, Is.EqualTo("two"));
        }

        [Test]
        public void ADifferentLibraryDoesNotSeeTheFile()
        {
            _store.Write(Lib, "guitar-1", "A", "one");
            Assert.That(_store.TryRead(Other, "guitar-1"), Is.Null);
            _store.Write(Other, "guitar-1", "B", "two");
            Assert.That(_store.TryRead(Lib, "guitar-1").Document, Is.EqualTo("one"));
            Assert.That(_store.TryRead(Other, "guitar-1").Document, Is.EqualTo("two"));
        }

        [Test]
        public void StringKeyRoundTripsWithLibraryIds()
        {
            _store.Write(Lib, "guitar-abc_1", "OM", "{\"a\":1}", 7, 9);
            var r = _store.TryRead(Lib, "guitar-abc_1");
            Assert.That(_store.Exists(Lib, "guitar-abc_1"));
            Assert.That(r.Document, Is.EqualTo("{\"a\":1}"));
            Assert.That(r.FretwireId, Is.EqualTo(7));
            Assert.That(r.StringSetId, Is.EqualTo(9));
            Assert.That(_store.TryRead(Other, "guitar-abc_1"), Is.Null);
            _store.Delete(Lib, "guitar-abc_1");
            Assert.That(_store.Exists(Lib, "guitar-abc_1"), Is.False);
        }

        [Test]
        public void IdsDefaultToNull()
        {
            _store.Write(Lib, "guitar-x", "A", "d");
            var r = _store.TryRead(Lib, "guitar-x");
            Assert.That(r.FretwireId, Is.Null);
            Assert.That(r.StringSetId, Is.Null);
        }

        [TestCase("")]
        [TestCase("a/b")]
        [TestCase("..\\x")]
        [TestCase("a b")]
        [TestCase("a.b")]
        [TestCase("123")]
        public void UnsafeKeyIsRejected(string key)
        {
            Assert.Throws<ArgumentException>(() => _store.Write(Lib, key, "A", "d"));
            Assert.Throws<ArgumentException>(() => _store.TryRead(Lib, key));
            Assert.Throws<ArgumentException>(() => _store.Delete(Lib, key));
        }

        [Test]
        public void OldFormatFileWithoutIdsStillReads()
        {
            _store.Write(Lib, "guitar-old", "A", "d");
            string file = Directory.GetFiles(_folder)[0];
            var doc = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(file)).AsObject();
            doc.Remove("FretwireId");
            doc.Remove("StringSetId");
            File.WriteAllText(file, doc.ToJsonString());
            var r = _store.TryRead(Lib, "guitar-old");
            Assert.That(r.Document, Is.EqualTo("d"));
            Assert.That(r.FretwireId, Is.Null);
        }

        [Test]
        public void LegacyReadAndDelete()
        {
            _store.WriteLegacy(Lib, 5, "A", "legacy");
            Assert.That(_store.TryReadLegacy(Lib, 5).Document, Is.EqualTo("legacy"));
            _store.DeleteLegacy(Lib, 5);
            Assert.That(_store.TryReadLegacy(Lib, 5), Is.Null);
        }

        [Test]
        public void StringDeleteStaleLeavesKeptKeysOtherLibrariesAndLegacyFiles()
        {
            _store.Write(Lib, "guitar-a", "A", "1");
            _store.Write(Lib, "guitar-b", "B", "2");
            _store.Write(Other, "guitar-b", "C", "3");
            _store.WriteLegacy(Lib, 12, "D", "legacy");
            _store.DeleteStale(Lib, new[] { "guitar-a" });
            Assert.That(_store.Exists(Lib, "guitar-a"));
            Assert.That(_store.Exists(Lib, "guitar-b"), Is.False);
            Assert.That(_store.Exists(Other, "guitar-b"));
            Assert.That(_store.LegacyExists(Lib, 12));
        }

        [Test]
        public void TryReadStatus_DistinguishesMissingOkCorruptAndUnavailable()
        {
            Assert.That(_store.TryRead(Lib, "guitar-s", out _), Is.EqualTo(RecoveryReadStatus.Missing));
            _store.Write(Lib, "guitar-s", "S", "doc");
            Assert.That(_store.TryRead(Lib, "guitar-s", out var r), Is.EqualTo(RecoveryReadStatus.Ok));
            Assert.That(r.Document, Is.EqualTo("doc"));
            string file = Directory.GetFiles(_folder)[0];
            using (new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
                Assert.That(_store.TryRead(Lib, "guitar-s", out _), Is.EqualTo(RecoveryReadStatus.Unavailable));
            File.WriteAllText(file, "garbage");
            Assert.That(_store.TryRead(Lib, "guitar-s", out _), Is.EqualTo(RecoveryReadStatus.Corrupt));
        }
    }
}
