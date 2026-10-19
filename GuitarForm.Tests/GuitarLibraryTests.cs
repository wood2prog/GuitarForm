using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuitarForm.Data;
using GuitarForm.Model.Parts;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    // Saving and loading guitars, their necks, bodies and bracing, and the library's fretwire and string sets,
    // against a new library in a temporary folder for each test.
    public class GuitarLibraryTests
    {
        string _folder;
        Library _library;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "GuitarFormTests", Guid.NewGuid().ToString("N"));
            _library = Library.Open(Path.Combine(_folder, "GuitarForm.db"));
        }

        [TearDown]
        public void TearDown()
        {
            _library.Dispose();
            if (Directory.Exists(_folder))
                Directory.Delete(_folder, true);
        }

        // The test OM, with its fretwire and string set saved in the library.
        Guitar SavedItemsOM(string name = "OM")
        {
            var fretwire = TestGuitars.Fretwire with { Id = _library.SaveLibraryItem(TestGuitars.Fretwire) };
            var strings = TestGuitars.StringSet with { Id = _library.SaveLibraryItem(TestGuitars.StringSet) };
            var guitar = TestGuitars.OM(name);
            return guitar with { Neck = guitar.Neck with { Fretwire = fretwire, StringSet = strings } };
        }

        static Neck NeckWith(double nutWidth) => new Neck
        {
            Fretboard = new Fretboard { NutWidth = new(Fretboard.Definitions.NutWidth, nutWidth) },
        };

        // --- Guitars ---

        [Test]
        public void Guitar_RoundTrip()
        {
            var guitar = SavedItemsOM();
            long id = _library.CreateGuitar(guitar);

            TestGuitars.AssertSameValues(_library.LoadGuitar(id), guitar);
        }

        [Test]
        public void Neck_LegacyProfileRows_AreIgnoredAndNotSavedAgain()
        {
            var guitar = SavedItemsOM();
            long id = _library.CreateGuitar(guitar);
            string path = Path.Combine(_folder, "GuitarForm.db");
            long neckId;
            using (var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=" + path + ";Pooling=False"))
            {
                connection.Open();
                using (var find = connection.CreateCommand())
                {
                    find.CommandText = "SELECT id FROM parts WHERE kind = 'neck' AND parent_id = $id";
                    find.Parameters.AddWithValue("$id", id);
                    neckId = (long)find.ExecuteScalar();
                }
                foreach (var abbreviation in new[] { "PBRF", "PSAF", "PMRF", "PCOS", "PBRE", "PSAH" })
                {
                    using var insert = connection.CreateCommand();
                    insert.CommandText = "INSERT INTO part_values (part_id, abbreviation, item, value) VALUES ($id, $a, -1, 30)";
                    insert.Parameters.AddWithValue("$id", neckId);
                    insert.Parameters.AddWithValue("$a", abbreviation);
                    insert.ExecuteNonQuery();
                }
            }

            var loaded = _library.LoadGuitar(id);
            TestGuitars.AssertSameValues(loaded, guitar);

            _library.SaveGuitar(id, loaded);
            using var check = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=" + path + ";Pooling=False");
            check.Open();
            using var count = check.CreateCommand();
            count.CommandText = "SELECT count(*) FROM part_values WHERE abbreviation LIKE 'PBR%' OR abbreviation LIKE 'PSA%' " +
                "OR abbreviation LIKE 'PMR%' OR abbreviation LIKE 'PCO%'";
            Assert.That((long)count.ExecuteScalar(), Is.Zero);
        }

        [Test]
        public void Guitar_ListedByName()
        {
            _library.CreateGuitar(TestGuitars.OM("Parlor"));
            _library.CreateGuitar(TestGuitars.OM("dreadnought"));

            Assert.That(_library.ListGuitars().Select(g => g.Name), Is.EqualTo(new[] { "dreadnought", "Parlor" }));
            Assert.That(_library.GuitarExists("PARLOR"), Is.True);
        }

        [Test]
        public void Guitar_NeedsName()
        {
            var unnamed = TestGuitars.OM() with { Name = new(Guitar.Definitions.Name, " ") };

            var error = Assert.Throws<InvalidOperationException>(() => _library.CreateGuitar(unnamed));
            Assert.That(error.Message, Is.EqualTo("A guitar needs an Instrument Name (IN) to be saved."));
        }

        [Test]
        public void Guitar_NameTakenIgnoringCase_Throws()
        {
            _library.CreateGuitar(TestGuitars.OM("OM"));

            Assert.Throws<InvalidOperationException>(() => _library.CreateGuitar(TestGuitars.OM("om")));
        }

        [Test]
        public void SaveGuitar_ReplacesItsParts()
        {
            long id = _library.CreateGuitar(TestGuitars.OM());
            var changed = TestGuitars.OM("OM 2") with { Neck = NeckWith(43) };

            _library.SaveGuitar(id, changed);

            TestGuitars.AssertSameValues(_library.LoadGuitar(id), changed);
            Assert.That(_library.ListAssemblies<Neck>(), Has.Count.EqualTo(1));
            Assert.That(_library.ListGuitars().Single().Name, Is.EqualTo("OM 2"));
        }

        [Test]
        public void SaveGuitar_KeepingItsOwnName_IsAllowed()
        {
            long id = _library.CreateGuitar(TestGuitars.OM());

            Assert.DoesNotThrow(() => _library.SaveGuitar(id, TestGuitars.OM()));
        }

        [Test]
        public void DeleteGuitar_DeletesItsNeckBodyAndBracing()
        {
            long id = _library.CreateGuitar(TestGuitars.OM());

            _library.DeleteGuitar(id);

            Assert.That(_library.ListGuitars(), Is.Empty);
            Assert.That(_library.ListAssemblies<Neck>(), Is.Empty);
            Assert.That(_library.ListAssemblies<Body>(), Is.Empty);
            Assert.That(_library.ListAssemblies<Bracing>(), Is.Empty);
        }

        [Test]
        public void LoadGuitar_UnknownId_Throws()
        {
            Assert.Throws<KeyNotFoundException>(() => _library.LoadGuitar(42));
        }

        // --- Necks, bodies and bracing ---

        [Test]
        public void Assemblies_ListNamedCopiesThenGuitars()
        {
            _library.CreateGuitar(TestGuitars.OM("OM"));
            _library.SaveAssembly("Slim C", NeckWith(43));

            var necks = _library.ListAssemblies<Neck>();

            Assert.That(necks.Select(n => (n.Name, n.GuitarName)),
                Is.EqualTo(new (string, string)[] { ("Slim C", null), (null, "OM") }));
        }

        [Test]
        public void Bracing_ListedWithItsGuitarOrNamedBody()
        {
            var guitar = TestGuitars.OM();
            _library.CreateGuitar(guitar);
            _library.SaveAssembly("Jumbo", guitar.Body);

            var bracing = _library.ListAssemblies<Bracing>();

            Assert.That(bracing.Select(b => (b.GuitarName, b.BodyName)),
                Is.EquivalentTo(new (string, string)[] { ("OM", null), (null, "Jumbo") }));
        }

        [Test]
        public void LoadBody_BringsItsBracing()
        {
            var guitar = TestGuitars.OM();
            _library.CreateGuitar(guitar);
            long bodyId = _library.ListAssemblies<Body>().Single().Id;

            var body = _library.LoadAssembly<Body>(bodyId);

            TestGuitars.AssertSameValues(body, guitar.Body);
            TestGuitars.AssertSameValues(body.TopPlate.Bracing, guitar.Body.TopPlate.Bracing);
        }

        [Test]
        public void ReplacingANeck_CopiesItAndLeavesTheSource()
        {
            long source = _library.CreateGuitar(TestGuitars.OM("Source") with { Neck = NeckWith(43) });
            long target = _library.CreateGuitar(TestGuitars.OM("Target"));
            long sourceNeck = _library.ListAssemblies<Neck>().Single(n => n.GuitarName == "Source").Id;

            var replaced = _library.LoadGuitar(target) with { Neck = _library.LoadAssembly<Neck>(sourceNeck) };
            _library.SaveGuitar(target, replaced with { Neck = replaced.Neck with { Fretboard = NeckWith(46).Fretboard } });

            Assert.That(_library.LoadGuitar(source).Neck.Fretboard.NutWidth.Value, Is.EqualTo(43));
            Assert.That(_library.LoadGuitar(target).Neck.Fretboard.NutWidth.Value, Is.EqualTo(46));
        }

        [Test]
        public void ReplacingBracingOnly_KeepsTheBody()
        {
            var guitar = TestGuitars.OM();
            long id = _library.CreateGuitar(guitar);
            long bracingId = _library.SaveAssembly("Light", new Bracing { ToneBarCount = new(Bracing.Definitions.ToneBarCount, 3) });

            var loaded = _library.LoadGuitar(id);
            var bracing = _library.LoadAssembly<Bracing>(bracingId);
            _library.SaveGuitar(id, loaded with
            {
                Body = loaded.Body with { TopPlate = loaded.Body.TopPlate with { Bracing = bracing } },
            });

            var saved = _library.LoadGuitar(id);
            TestGuitars.AssertSameValues(saved.Body, guitar.Body);
            Assert.That(saved.Body.TopPlate.Bracing.ToneBarCount.Value, Is.EqualTo(3));
        }

        [Test]
        public void SaveAssembly_ReplacesSameNameIgnoringCase()
        {
            _library.SaveAssembly("Slim C", NeckWith(43));
            long id = _library.SaveAssembly("slim c", NeckWith(44));

            Assert.That(_library.ListAssemblies<Neck>().Select(n => n.Id), Is.EqualTo(new[] { id }));
            Assert.That(_library.LoadAssembly<Neck>(id).Fretboard.NutWidth.Value, Is.EqualTo(44));
        }

        [Test]
        public void SaveAssembly_SameNameDifferentKind_Allowed()
        {
            _library.SaveAssembly("Standard", new Neck());
            _library.SaveAssembly("Standard", new Body());

            Assert.That(_library.ListAssemblies<Neck>(), Has.Count.EqualTo(1));
            Assert.That(_library.ListAssemblies<Body>(), Has.Count.EqualTo(1));
        }

        [Test]
        public void SaveAssembly_NotAnAssembly_Throws()
        {
            Assert.Throws<ArgumentException>(() => _library.SaveAssembly("Nut", new Nut()));
        }

        [Test]
        public void LoadAssembly_WrongKind_Throws()
        {
            long id = _library.SaveAssembly("Slim C", new Neck());

            Assert.Throws<KeyNotFoundException>(() => _library.LoadAssembly<Body>(id));
        }

        [Test]
        public void DeleteAssembly_OnlyNamedCopies()
        {
            long named = _library.SaveAssembly("Slim C", new Neck());
            _library.CreateGuitar(TestGuitars.OM());
            long guitarNeck = _library.ListAssemblies<Neck>().Single(n => n.GuitarName == "OM").Id;

            _library.DeleteAssembly(named);

            Assert.Throws<InvalidOperationException>(() => _library.DeleteAssembly(guitarNeck));
            Assert.That(_library.ListAssemblies<Neck>().Select(n => n.Id), Is.EqualTo(new[] { guitarNeck }));
        }

        // --- Fretwire and string sets ---

        [Test]
        public void LibraryItems_RoundTripAndNamedByManufacturerAndProduct()
        {
            long fretwire = _library.SaveLibraryItem(TestGuitars.Fretwire);
            long strings = _library.SaveLibraryItem(TestGuitars.StringSet);

            TestGuitars.AssertSameValues(_library.LoadLibraryItem<Fretwire>(fretwire), TestGuitars.Fretwire);
            TestGuitars.AssertSameValues(_library.LoadLibraryItem<StringSet>(strings), TestGuitars.StringSet);
            Assert.That(_library.LoadLibraryItem<Fretwire>(fretwire).Id, Is.EqualTo(fretwire));
            Assert.That(_library.ListLibraryItems<Fretwire>().Single().Name, Is.EqualTo("Jescar 47104"));
            Assert.That(_library.ListLibraryItems<StringSet>().Single().Name, Is.EqualTo("D'Addario EJ16"));
        }

        [Test]
        public void LibraryItem_NeedsProduct_AndStringSetsAlsoManufacturer()
        {
            var unnamed = TestGuitars.Fretwire with { ProductCode = new(Fretwire.Definitions.ProductCode) };
            var error = Assert.Throws<InvalidOperationException>(() => _library.SaveLibraryItem(unnamed));
            Assert.That(error.Message, Is.EqualTo(
                "A fretwire needs a Fretwire Product Code (FWPC) to be saved in the library."));

            var noManufacturer = TestGuitars.StringSet with { Manufacturer = new(StringSet.Definitions.Manufacturer) };
            error = Assert.Throws<InvalidOperationException>(() => _library.SaveLibraryItem(noManufacturer));
            Assert.That(error.Message, Is.EqualTo(
                "A string set needs a String Set Manufacturer (SSM) and String Set Name (SSN) to be saved in the library."));
        }

        [Test]
        public void Fretwire_WithoutManufacturer_IsNamedByProductCode()
        {
            var bare = TestGuitars.Fretwire with { Manufacturer = new(Fretwire.Definitions.Manufacturer, "  ") };
            long id = _library.SaveLibraryItem(bare);

            Assert.That(_library.ListLibraryItems<Fretwire>().Single().Name, Is.EqualTo("47104"));
            Assert.That(() => _library.SaveLibraryItem(bare), Throws.InvalidOperationException);
            Assert.That(_library.SaveLibraryItem(TestGuitars.Fretwire), Is.Not.EqualTo(id));
            Assert.That(_library.ListLibraryItems<Fretwire>().Select(i => i.Name), Is.EquivalentTo(new[] { "47104", "Jescar 47104" }));
        }

        [Test]
        public void SlotDepthPriceAndOverage_SaveAndReload()
        {
            var wire = TestGuitars.Fretwire with
            {
                SlotDepth = new(Fretwire.Definitions.SlotDepth, 1.7),
                Price = new(Fretwire.Definitions.Price, 12.5),
            };
            var guitar = TestGuitars.OM("Slots");
            guitar = guitar with
            {
                Neck = guitar.Neck with
                {
                    Fretwire = wire,
                    Fretboard = guitar.Neck.Fretboard with { SlotOverage = new(Fretboard.Definitions.SlotOverage, 0.4) },
                },
            };

            long wireId = _library.SaveLibraryItem(wire);
            var reloadedWire = _library.LoadLibraryItem<Fretwire>(wireId);
            Assert.That(reloadedWire.SlotDepth.Value, Is.EqualTo(1.7));
            Assert.That(reloadedWire.Price.Value, Is.EqualTo(12.5));

            long id = _library.CreateGuitar(guitar with { Neck = guitar.Neck with { Fretwire = reloadedWire } });
            var loaded = _library.LoadGuitar(id);
            Assert.That(loaded.Neck.Fretboard.SlotOverage.Value, Is.EqualTo(0.4));
            Assert.That(loaded.Neck.Fretwire.SlotDepth.Value, Is.EqualTo(1.7));
            Assert.That(loaded.Neck.Fretwire.Price.Value, Is.EqualTo(12.5));
        }

        [Test]
        public void LibraryItem_SameNameTwice_Throws()
        {
            _library.SaveLibraryItem(TestGuitars.Fretwire);

            Assert.Throws<InvalidOperationException>(() => _library.SaveLibraryItem(TestGuitars.Fretwire));
        }

        [Test]
        public void LibraryItem_SavedAgainById_IsUpdated()
        {
            long id = _library.SaveLibraryItem(TestGuitars.Fretwire);
            var changed = TestGuitars.Fretwire with { Id = id, BeadHeight = new(Fretwire.Definitions.BeadHeight, 1.4) };

            Assert.That(_library.SaveLibraryItem(changed), Is.EqualTo(id));
            Assert.That(_library.LoadLibraryItem<Fretwire>(id).BeadHeight.Value, Is.EqualTo(1.4));
            Assert.That(_library.ListLibraryItems<Fretwire>(), Has.Count.EqualTo(1));
        }

        [Test]
        public void Neck_WithUnsavedFretwire_Throws()
        {
            var guitar = TestGuitars.OM();
            guitar = guitar with { Neck = guitar.Neck with { Fretwire = TestGuitars.Fretwire } };

            var error = Assert.Throws<InvalidOperationException>(() => _library.CreateGuitar(guitar));
            Assert.That(error.Message, Is.EqualTo("The neck's fretwire isn't in the library. Save it to the library first."));
            Assert.That(_library.ListGuitars(), Is.Empty);
        }

        [Test]
        public void DeleteLibraryItem_Unused_Deletes()
        {
            long id = _library.SaveLibraryItem(TestGuitars.Fretwire);

            _library.DeleteLibraryItem(id);

            Assert.That(_library.ListLibraryItems<Fretwire>(), Is.Empty);
        }

        [Test]
        public void DeleteLibraryItem_InUse_NeedsReplacement()
        {
            var guitar = SavedItemsOM();
            _library.CreateGuitar(guitar);
            long id = guitar.Neck.Fretwire.Id.Value;

            var error = Assert.Throws<InvalidOperationException>(() => _library.DeleteLibraryItem(id));
            Assert.That(error.Message, Is.EqualTo("It's used by 1 neck: OM. Choose a replacement."));
            Assert.That(_library.NecksUsing(id).Select(n => n.GuitarName), Is.EqualTo(new[] { "OM" }));
        }

        [Test]
        public void DeleteLibraryItem_InUse_SwitchesNecksToReplacement()
        {
            var guitar = SavedItemsOM();
            long guitarId = _library.CreateGuitar(guitar);
            _library.SaveAssembly("Spare", guitar.Neck);
            long old = guitar.Neck.Fretwire.Id.Value;
            long replacement = _library.SaveLibraryItem(TestGuitars.Fretwire with
            {
                ProductCode = new(Fretwire.Definitions.ProductCode, "57110"),
            });

            _library.DeleteLibraryItem(old, replacement);

            Assert.That(_library.LoadGuitar(guitarId).Neck.Fretwire.Id, Is.EqualTo(replacement));
            Assert.That(_library.NecksUsing(replacement), Has.Count.EqualTo(2));
            Assert.That(_library.ListLibraryItems<Fretwire>().Select(f => f.Id), Is.EqualTo(new[] { replacement }));
        }

        [Test]
        public void DeleteLibraryItem_ReplacementOfAnotherKind_Throws()
        {
            var guitar = SavedItemsOM();
            _library.CreateGuitar(guitar);

            Assert.Throws<InvalidOperationException>(() =>
                _library.DeleteLibraryItem(guitar.Neck.Fretwire.Id.Value, guitar.Neck.StringSet.Id.Value));
        }
    }
}
