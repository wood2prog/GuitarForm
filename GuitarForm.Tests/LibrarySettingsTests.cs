using GuitarForm.Data;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    public class LibrarySettingsTests
    {
        [Test]
        public void Defaults_AreTheAgreedOnes()
        {
            var settings = new LibrarySettings();
            Assert.That(settings.LibraryPath, Does.EndWith(@"AppData\Local\GuitarForm\GuitarForm.db"));
            Assert.That(settings.BackupFolder, Does.EndWith(@"GuitarForm\Backups"));
            Assert.That(settings.BackupIntervalMinutes, Is.EqualTo(15));
            Assert.That(settings.KeepLastBackups, Is.EqualTo(20));
            Assert.That(settings.KeepDailyBackupsDays, Is.EqualTo(30));
        }

        [Test]
        public void Corrected_ReplacesEmptyPathsAndOutOfRangeNumbers()
        {
            var corrected = new LibrarySettings
            {
                LibraryPath = "",
                BackupFolder = " ",
                BackupIntervalMinutes = 0,
                KeepLastBackups = -3,
                KeepDailyBackupsDays = -1,
            }.Corrected();

            Assert.That(corrected.LibraryPath, Is.EqualTo(LibrarySettings.DefaultLibraryPath));
            Assert.That(corrected.BackupFolder, Is.EqualTo(LibrarySettings.DefaultBackupFolder));
            Assert.That(corrected.BackupIntervalMinutes, Is.EqualTo(1));
            Assert.That(corrected.KeepLastBackups, Is.EqualTo(1));
            Assert.That(corrected.KeepDailyBackupsDays, Is.EqualTo(0));
        }
    }
}
