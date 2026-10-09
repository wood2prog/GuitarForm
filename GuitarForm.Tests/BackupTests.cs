using System;
using System.IO;
using System.Linq;
using GuitarForm.Data;
using GuitarForm.Model;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    // Checks backing up, pruning and restoring the library, in a temporary folder for each test.
    public class BackupTests
    {
        static readonly GuitarDesign OM = new GuitarDesign
        {
            Name = "OM",
            Instrument = new Instrument { ScaleLength = 645, NeckJoinFret = 14 },
            Body = new Body { BodyLength = 495, LbWidth = 390 },
        };

        string _folder;
        string _library;
        string _backups;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "GuitarFormTests", Guid.NewGuid().ToString("N"));
            _library = Path.Combine(_folder, "Library", "GuitarForm.db");
            _backups = Path.Combine(_folder, "Backups");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_folder))
                Directory.Delete(_folder, true);
        }

        long CreateLibrary(GuitarDesign design)
        {
            using var library = Library.Open(_library);
            return library.CreateDesign(design);
        }

        [Test]
        public void BackUp_WritesACompleteCopy()
        {
            long id = CreateLibrary(OM);

            string backup = Backups.BackUp(_library, _backups, new DateTime(2026, 10, 8, 18, 30, 5));

            Assert.That(Path.GetFileName(backup), Is.EqualTo("GuitarForm 2026-10-08 183005.db"));
            Assert.That(Directory.GetFiles(_backups), Is.EqualTo(new[] { backup }));
            using var copy = Library.Open(backup);
            Assert.That(copy.LoadDesign(id), Is.EqualTo(OM));
        }

        [Test]
        public void List_IsNewestFirstAndIgnoresOtherFiles()
        {
            CreateLibrary(OM);
            Backups.BackUp(_library, _backups, new DateTime(2026, 10, 7, 9, 0, 0));
            Backups.BackUp(_library, _backups, new DateTime(2026, 10, 8, 9, 0, 0));
            File.WriteAllText(Path.Combine(_backups, "notes.txt"), "");
            File.WriteAllText(Path.Combine(_backups, "GuitarForm old.db"), "");

            var times = Backups.List(_backups).Select(b => b.Time);

            Assert.That(times, Is.EqualTo(new[] { new DateTime(2026, 10, 8, 9, 0, 0), new DateTime(2026, 10, 7, 9, 0, 0) }));
        }

        [Test]
        public void NeedsBackup_FollowsChangesToTheLibrary()
        {
            Assert.That(Backups.NeedsBackup(_library, _backups), Is.False, "no library");

            CreateLibrary(OM);
            Assert.That(Backups.NeedsBackup(_library, _backups), Is.True, "never backed up");

            Backups.BackUp(_library, _backups, DateTime.Now.AddSeconds(1));
            Assert.That(Backups.NeedsBackup(_library, _backups), Is.False, "just backed up");

            File.SetLastWriteTime(_library, DateTime.Now.AddMinutes(1));
            Assert.That(Backups.NeedsBackup(_library, _backups), Is.True, "changed since");
        }

        [Test]
        public void SelectToDelete_KeepsTheLastFewAndOnePerRecentDay()
        {
            var now = new DateTime(2026, 10, 8, 20, 0, 0);
            BackupFile At(int day, int hour) => new BackupFile($"{day} {hour}", new DateTime(2026, 10, day, hour, 0, 0));
            var backups = new[]
            {
                At(8, 18), At(8, 17), At(8, 16), // today: the newest three are kept by keepLast
                At(7, 12), At(7, 9),             // yesterday: the newest of the day is kept
                At(6, 12),                       // the day before: kept, the third of three days
                At(5, 12),                       // too old
            };

            var deleted = Backups.SelectToDelete(backups, keepLast: 3, keepDays: 3, now);

            Assert.That(deleted.Select(b => b.Path), Is.EquivalentTo(new[] { "7 9", "5 12" }));
        }

        [Test]
        public void Prune_DeletesTheOldBackups()
        {
            CreateLibrary(OM);
            for (int hour = 9; hour <= 12; hour++)
                Backups.BackUp(_library, _backups, new DateTime(2026, 10, 8, hour, 0, 0));

            var deleted = Backups.Prune(_backups, keepLast: 2, keepDays: 1, new DateTime(2026, 10, 8, 13, 0, 0));

            Assert.That(deleted, Has.Count.EqualTo(2));
            Assert.That(Backups.List(_backups).Select(b => b.Time.Hour), Is.EqualTo(new[] { 12, 11 }));
        }

        [Test]
        public void Restore_ReplacesTheLibraryAndBacksUpTheOldOne()
        {
            CreateLibrary(OM);
            string backup = Backups.BackUp(_library, _backups, new DateTime(2026, 10, 1, 9, 0, 0));
            using (var library = Library.Open(_library))
            {
                library.DeleteDesign(library.ListDesigns().Single().Id);
                library.CreateDesign(OM with { Name = "Dread" });
            }

            string safety = Backups.Restore(backup, _library, _backups);

            using (var restored = Library.Open(_library))
                Assert.That(restored.ListDesigns().Select(d => d.Name), Is.EqualTo(new[] { "OM" }));
            using (var old = Library.Open(safety))
                Assert.That(old.ListDesigns().Select(d => d.Name), Is.EqualTo(new[] { "Dread" }));
        }

        [Test]
        public void Restore_FromAFileThatIsntALibrary_LeavesTheLibraryAlone()
        {
            CreateLibrary(OM);
            Directory.CreateDirectory(_backups);
            string notALibrary = Path.Combine(_backups, "GuitarForm 2026-10-01 090000.db");
            File.WriteAllText(notALibrary, "not a database");

            Assert.That(() => Backups.Restore(notALibrary, _library, _backups), Throws.TypeOf<InvalidDataException>());
            using var library = Library.Open(_library);
            Assert.That(library.ListDesigns().Select(d => d.Name), Is.EqualTo(new[] { "OM" }));
        }
    }
}
