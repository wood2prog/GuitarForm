using System;
using System.Globalization;
using System.IO;
using GuitarForm.Data;
using GuitarForm.Model.Parts;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    public class LibraryItemRecoveryTests
    {
        static readonly string Lib = Path.Combine(Path.GetTempPath(), "GuitarFormTests", "lib", "GuitarForm.db");
        static readonly string Other = Path.Combine(Path.GetTempPath(), "GuitarFormTests", "other", "GuitarForm.db");
        string _folder;
        RecoveryStore _store;
        LibraryItemRecovery _recovery;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "GuitarFormTests", Guid.NewGuid().ToString("N"));
            _store = new RecoveryStore(_folder);
            _recovery = new LibraryItemRecovery(_store, Lib);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_folder))
                Directory.Delete(_folder, true);
        }

        [Test]
        public void StringSet_RoundTripsWithIdAndName()
        {
            _recovery.Write(TestGuitars.StringSet, 7, "D'Addario EJ16");
            Assert.That(_recovery.TryReadStringSet(out var item), Is.EqualTo(RecoveryReadStatus.Ok));
            Assert.That(item.SavedId, Is.EqualTo(7));
            Assert.That(item.Name, Is.EqualTo("D'Addario EJ16"));
            Assert.That(item.SavedAtUtc, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromMinutes(1)));
            Assert.That(item.Item.Strings.Count, Is.EqualTo(2));
            Assert.That(LibraryItemRecovery.SameContent(item.Item, TestGuitars.StringSet));
            Assert.That(LibraryItemRecovery.SameContent(item.Item.WithAddedString(), TestGuitars.StringSet), Is.False);
        }

        [Test]
        public void Fretwire_RoundTripsAsNewItem()
        {
            _recovery.Write(TestGuitars.Fretwire, null, "New wire");
            Assert.That(_recovery.TryReadFretwire(out var item), Is.EqualTo(RecoveryReadStatus.Ok));
            Assert.That(item.SavedId, Is.Null);
            Assert.That(item.Name, Is.EqualTo("New wire"));
            Assert.That(LibraryItemRecovery.SameContent(item.Item, TestGuitars.Fretwire));
            Assert.That(LibraryItemRecovery.SameContent(item.Item,
                TestGuitars.Fretwire with { BeadHeight = new(Fretwire.Definitions.BeadHeight, 1.5) }), Is.False);
        }

        [Test]
        public void SameContent_IgnoresId()
        {
            Assert.That(LibraryItemRecovery.SameContent(TestGuitars.Fretwire with { Id = 3 }, TestGuitars.Fretwire));
            Assert.That(LibraryItemRecovery.SameContent(TestGuitars.StringSet with { Id = 3 }, TestGuitars.StringSet));
        }

        [Test]
        public void Missing_ThenClear()
        {
            Assert.That(_recovery.TryReadStringSet(out var set), Is.EqualTo(RecoveryReadStatus.Missing));
            Assert.That(set, Is.Null);
            Assert.That(_recovery.TryReadFretwire(out _), Is.EqualTo(RecoveryReadStatus.Missing));
            _recovery.Write(TestGuitars.StringSet, null, "x");
            _recovery.Write(TestGuitars.Fretwire, null, "y");
            _recovery.ClearStringSet();
            Assert.That(_recovery.TryReadStringSet(out _), Is.EqualTo(RecoveryReadStatus.Missing));
            Assert.That(_recovery.TryReadFretwire(out _), Is.EqualTo(RecoveryReadStatus.Ok));
            _recovery.ClearFretwire();
            Assert.That(_recovery.TryReadFretwire(out _), Is.EqualTo(RecoveryReadStatus.Missing));
        }

        [Test]
        public void NotAGuitarDocument_IsCorrupt()
        {
            _store.Write(Lib, RecoveryKeys.StringSetWork, "x", "not json");
            Assert.That(_recovery.TryReadStringSet(out var item), Is.EqualTo(RecoveryReadStatus.Corrupt));
            Assert.That(item, Is.Null);
        }

        [Test]
        public void WrongKindDocument_IsCorrupt()
        {
            _recovery.Write(TestGuitars.Fretwire, null, "wire");
            File.Move(Path.Combine(_folder, Directory.GetFiles(_folder, "*-fretwire-work.json")[0]),
                Path.Combine(_folder, Path.GetFileName(Directory.GetFiles(_folder, "*-fretwire-work.json")[0])
                    .Replace("fretwire-work", "stringset-work")));
            Assert.That(_recovery.TryReadStringSet(out _), Is.EqualTo(RecoveryReadStatus.Corrupt));
        }

        [Test]
        public void OtherLibrary_IsNotRead()
        {
            _recovery.Write(TestGuitars.StringSet, 1, "x");
            var other = new LibraryItemRecovery(_store, Other);
            Assert.That(other.TryReadStringSet(out _), Is.EqualTo(RecoveryReadStatus.Missing));
        }

        [Test]
        public void RoundTrip_UnderGerman_IsSame()
        {
            var saved = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");
                _recovery.Write(TestGuitars.StringSet, 2, "x");
                _recovery.Write(TestGuitars.Fretwire, 3, "y");
                Assert.That(_recovery.TryReadStringSet(out var set), Is.EqualTo(RecoveryReadStatus.Ok));
                Assert.That(_recovery.TryReadFretwire(out var wire), Is.EqualTo(RecoveryReadStatus.Ok));
                Assert.That(LibraryItemRecovery.SameContent(set.Item, TestGuitars.StringSet));
                Assert.That(LibraryItemRecovery.SameContent(wire.Item, TestGuitars.Fretwire));
            }
            finally
            {
                CultureInfo.CurrentCulture = saved;
            }
        }
    }
}
