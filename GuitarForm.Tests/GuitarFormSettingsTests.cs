using GuitarForm.Data;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    public class GuitarFormSettingsTests
    {
        [Test]
        public void Defaults_AreTheAgreedOnes()
        {
            var settings = new GuitarFormSettings();
            Assert.That(settings.LibraryPath, Does.EndWith(@"AppData\Local\GuitarForm\GuitarForm.db"));
            Assert.That(settings.BackupFolder, Does.EndWith(@"GuitarForm\Backups"));
            Assert.That(settings.BackupIntervalMinutes, Is.EqualTo(15));
            Assert.That(settings.KeepLastBackups, Is.EqualTo(20));
            Assert.That(settings.KeepDailyBackupsDays, Is.EqualTo(30));
            Assert.That(settings.DrawingGap, Is.EqualTo(50));
        }

        [Test]
        public void Corrected_ReplacesEmptyPathsAndOutOfRangeNumbers()
        {
            var corrected = new GuitarFormSettings
            {
                LibraryPath = "",
                BackupFolder = " ",
                BackupIntervalMinutes = 0,
                KeepLastBackups = -3,
                KeepDailyBackupsDays = -1,
                DrawingGap = -5,
            }.Corrected();

            Assert.That(corrected.LibraryPath, Is.EqualTo(GuitarFormSettings.DefaultLibraryPath));
            Assert.That(corrected.BackupFolder, Is.EqualTo(GuitarFormSettings.DefaultBackupFolder));
            Assert.That(corrected.BackupIntervalMinutes, Is.EqualTo(1));
            Assert.That(corrected.KeepLastBackups, Is.EqualTo(1));
            Assert.That(corrected.KeepDailyBackupsDays, Is.EqualTo(0));
            Assert.That(corrected.DrawingGap, Is.EqualTo(0));
        }
    }
}
