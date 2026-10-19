using System.IO;
using System.Linq;
using GuitarForm.Data;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    public class LibraryFilesTests
    {
        string _root;
        string _from;
        string _to;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "GuitarFormLibraryFiles-" + Path.GetRandomFileName());
            _from = Path.Combine(_root, "a");
            _to = Path.Combine(_root, "b");
            Directory.CreateDirectory(_from);
            Directory.CreateDirectory(_to);
        }

        [TearDown]
        public void TearDown() => Directory.Delete(_root, true);

        string Make(string folder, string name, string text = null)
        {
            string path = Path.Combine(folder, name);
            File.WriteAllText(path, text ?? name);
            return path;
        }

        string[] Names(string folder) => Directory.GetFiles(folder).Select(Path.GetFileName).OrderBy(n => n).ToArray();

        [Test]
        public void SiblingsOf_FindsNumberedBackupsAndNotes_AndIgnoresTheRest()
        {
            string library = Make(_from, "GuitarForm.db");
            Make(_from, "GuitarForm.before-v1.bak");
            Make(_from, "GuitarForm.before-v2.1.bak");
            Make(_from, "GuitarForm.before-v2.2.bak");
            Make(_from, "GuitarForm.before-v3.notes.txt");
            Make(_from, "GuitarForm.before-v3.1.notes.txt");
            Make(_from, "Other.before-v1.bak");
            Make(_from, "GuitarForm2.before-v1.bak");
            Make(_from, "GuitarForm.before-v1.bak.tmp");
            Make(_from, "GuitarForm.before-v1.txt");
            Make(_from, "notes.txt");

            Assert.That(LibraryFiles.SiblingsOf(library).Select(Path.GetFileName), Is.EqualTo(new[]
            {
                "GuitarForm.before-v1.bak", "GuitarForm.before-v2.1.bak", "GuitarForm.before-v2.2.bak",
                "GuitarForm.before-v3.1.notes.txt", "GuitarForm.before-v3.notes.txt",
            }));
        }

        [Test]
        public void SiblingsOf_WithNone_OrNoFolder_IsEmpty()
        {
            Assert.That(LibraryFiles.SiblingsOf(Path.Combine(_from, "GuitarForm.db")), Is.Empty);
            Assert.That(LibraryFiles.SiblingsOf(Path.Combine(_root, "missing", "GuitarForm.db")), Is.Empty);
        }

        [Test]
        public void Move_TakesTheLibraryAndItsSiblings_AndLeavesOthersBehind()
        {
            string library = Make(_from, "GuitarForm.db", "library");
            Make(_from, "GuitarForm.before-v2.bak", "bak");
            Make(_from, "GuitarForm.before-v2.notes.txt", "notes");
            Make(_from, "Other.before-v1.bak");

            Assert.That(LibraryFiles.Move(library, Path.Combine(_to, "GuitarForm.db")), Is.True);

            Assert.That(Names(_to), Is.EqualTo(new[] { "GuitarForm.before-v2.bak", "GuitarForm.before-v2.notes.txt", "GuitarForm.db" }));
            Assert.That(File.ReadAllText(Path.Combine(_to, "GuitarForm.db")), Is.EqualTo("library"));
            Assert.That(File.ReadAllText(Path.Combine(_to, "GuitarForm.before-v2.bak")), Is.EqualTo("bak"));
            Assert.That(Names(_from), Is.EqualTo(new[] { "Other.before-v1.bak" }));
        }

        [Test]
        public void Move_WithACollision_ChangesNothing()
        {
            string library = Make(_from, "GuitarForm.db");
            Make(_from, "GuitarForm.before-v1.bak");
            Make(_from, "GuitarForm.before-v2.bak");
            Make(_to, "GuitarForm.before-v2.bak", "existing");

            var e = Assert.Throws<IOException>(() => LibraryFiles.Move(library, Path.Combine(_to, "GuitarForm.db")));

            Assert.That(e.Message, Does.Contain("GuitarForm.before-v2.bak").And.Contain("Nothing was moved"));
            Assert.That(Names(_from), Is.EqualTo(new[] { "GuitarForm.before-v1.bak", "GuitarForm.before-v2.bak", "GuitarForm.db" }));
            Assert.That(Names(_to), Is.EqualTo(new[] { "GuitarForm.before-v2.bak" }));
            Assert.That(File.ReadAllText(Path.Combine(_to, "GuitarForm.before-v2.bak")), Is.EqualTo("existing"));
        }

        [Test]
        public void Move_WhenTheLibraryNameIsTaken_ChangesNothing()
        {
            string library = Make(_from, "GuitarForm.db");
            Make(_to, "GuitarForm.db", "existing");

            Assert.Throws<IOException>(() => LibraryFiles.Move(library, Path.Combine(_to, "GuitarForm.db")));

            Assert.That(File.Exists(library));
            Assert.That(File.ReadAllText(Path.Combine(_to, "GuitarForm.db")), Is.EqualTo("existing"));
        }

        [Test]
        public void Move_WithNoLibrary_DoesNothing()
        {
            Make(_from, "GuitarForm.before-v1.bak");

            Assert.That(LibraryFiles.Move(Path.Combine(_from, "GuitarForm.db"), Path.Combine(_to, "GuitarForm.db")), Is.False);

            Assert.That(Names(_to), Is.Empty);
            Assert.That(Names(_from), Is.EqualTo(new[] { "GuitarForm.before-v1.bak" }));
        }

        [Test]
        public void Move_CreatesTheDestinationFolder()
        {
            string library = Make(_from, "GuitarForm.db");
            string destination = Path.Combine(_root, "new", "deeper", "GuitarForm.db");

            LibraryFiles.Move(library, destination);

            Assert.That(File.Exists(destination));
        }
    }
}
