using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuitarForm.Data;
using GuitarForm.Model.Parts;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    // Saving, loading, renaming, listing and deleting string sets in the library, against a new library in a temporary
    // folder for each test.
    public class StringSetLibraryTests
    {
        string _folder;
        string _path;
        Library _library;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "GuitarFormTests", Guid.NewGuid().ToString("N"));
            _path = Path.Combine(_folder, "GuitarForm.db");
            _library = Library.Open(_path);
        }

        [TearDown]
        public void TearDown()
        {
            _library.Dispose();
            if (Directory.Exists(_folder))
                Directory.Delete(_folder, true);
        }

        // A string with a diameter and unit weight, the other values unset.
        static GuitarString PlainString(string description, double diameter, double unitWeight) => new GuitarString
        {
            Description = new(GuitarString.Definitions.Description, description),
            Diameter = new(GuitarString.Definitions.Diameter, diameter),
            UnitWeight = new(GuitarString.Definitions.UnitWeight, unitWeight),
        };

        // A set of the given strings, from the treble to the bass.
        static StringSet SetOf(string manufacturer, string name, params GuitarString[] strings) => new StringSet
        {
            Manufacturer = new(StringSet.Definitions.Manufacturer, manufacturer),
            Name = new(StringSet.Definitions.Name, name),
            StringCount = new(StringSet.Definitions.StringCount, strings.Length),
            Strings = strings,
        };

        // A three-string set with every kind of string: plain, wound with no core material, and wound steel with a hex core.
        static StringSet ThreeStringSet(string manufacturer = "D'Addario", string name = "EJ16") => SetOf(manufacturer, name,
            PlainString("E1", 0.0105, 0.000278) with
            {
                UsedInSaddleCompensation = new(GuitarString.Definitions.UsedInSaddleCompensation, true),
            },
            PlainString("B2", 0.0135, 0.000491) with
            {
                Construction = new(GuitarString.Definitions.Construction, StringConstruction.Plain),
            },
            PlainString("E3", 0.0465, 0.00337) with
            {
                Construction = new(GuitarString.Definitions.Construction, StringConstruction.Wound),
                CoreMaterial = new(GuitarString.Definitions.CoreMaterial, StringCoreMaterial.NotSpecified),
                CoreModulus = new(GuitarString.Definitions.CoreModulus, 200.0),
                CoreStrength = new(GuitarString.Definitions.CoreStrength, 1500.0),
            });

        // A guitar whose neck uses the saved string set.
        static Guitar GuitarUsing(long setId, StringSet set, string name = "OM")
        {
            var guitar = TestGuitars.OM(name);
            return guitar with { Neck = guitar.Neck with { StringSet = set with { Id = setId } } };
        }

        // The number of stored values of the string set's strings from the given position on.
        long ValuesFromString(long setId, int position)
        {
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=" + _path + ";Pooling=False");
            connection.Open();
            using var count = connection.CreateCommand();
            count.CommandText = "SELECT count(*) FROM part_values WHERE part_id = $id AND item >= $position";
            count.Parameters.AddWithValue("$id", setId);
            count.Parameters.AddWithValue("$position", position);
            return (long)count.ExecuteScalar();
        }

        [Test]
        public void StringSet_RoundTrip_WithEveryKindOfString()
        {
            var set = ThreeStringSet();

            long id = _library.SaveLibraryItem(set);
            var loaded = _library.LoadLibraryItem<StringSet>(id);

            Assert.That(loaded.Id, Is.EqualTo(id));
            TestGuitars.AssertSameValues(loaded, set);
            Assert.That(loaded.Strings, Has.Count.EqualTo(3));
            for (int i = 0; i < set.Strings.Count; i++)
                TestGuitars.AssertSameValues(loaded.Strings[i], set.Strings[i]);
            Assert.That(loaded.Strings.Select(s => s.Description.Value), Is.EqualTo(new[] { "E1", "B2", "E3" }));
            Assert.That(loaded.Strings[2].CoreModulus.Value, Is.EqualTo(200.0));
            Assert.That(loaded.Strings[2].CoreStrength.Value, Is.EqualTo(1500.0));
        }

        [Test]
        public void StringSet_RoundTrip_WoundSteelHexCore()
        {
            var set = SetOf("D'Addario", "Steel hex", PlainString("E6", 1.2, 0.0093) with
            {
                Construction = new(GuitarString.Definitions.Construction, StringConstruction.Wound),
                CoreMaterial = new(GuitarString.Definitions.CoreMaterial, StringCoreMaterial.Steel),
                HexCore = new(GuitarString.Definitions.HexCore, true),
            });

            long id = _library.SaveLibraryItem(set);
            var loaded = _library.LoadLibraryItem<StringSet>(id);

            Assert.That(loaded.Strings.Single().HexCore.Value, Is.True);
            Assert.That(loaded.Strings.Single().CoreMaterial.Value, Is.EqualTo(StringCoreMaterial.Steel));
            Assert.That(loaded.Strings.Single().CoreModulus.HasValue, Is.False);
            TestGuitars.AssertSameValues(loaded, set);
        }

        [Test]
        public void StringSet_UpdatedToFewerStrings_LeavesNoOldRows()
        {
            long id = _library.SaveLibraryItem(ThreeStringSet());
            var twoStrings = SetOf("D'Addario", "EJ16", PlainString("E1", 0.0105, 0.000278), PlainString("B2", 0.0135, 0.000491))
                with { Id = id };

            Assert.That(_library.SaveLibraryItem(twoStrings), Is.EqualTo(id));

            var loaded = _library.LoadLibraryItem<StringSet>(id);
            Assert.That(loaded.Strings, Has.Count.EqualTo(2));
            Assert.That(loaded.StringCount.Value, Is.EqualTo(2));
            Assert.That(ValuesFromString(id, 2), Is.Zero);
            TestGuitars.AssertSameValues(loaded, twoStrings);
        }

        [Test]
        public void StringSet_UpdatedToMoreStrings_KeepsTheOrder()
        {
            long id = _library.SaveLibraryItem(SetOf("D'Addario", "EJ16", PlainString("E1", 0.0105, 0.000278)));
            var threeStrings = ThreeStringSet() with { Id = id };

            _library.SaveLibraryItem(threeStrings);

            var loaded = _library.LoadLibraryItem<StringSet>(id);
            Assert.That(loaded.Strings, Has.Count.EqualTo(3));
            Assert.That(loaded.Strings.Select(s => s.Description.Value), Is.EqualTo(new[] { "E1", "B2", "E3" }));
            TestGuitars.AssertSameValues(loaded, threeStrings);
        }

        [Test]
        public void StringSet_Renamed_KeepsItsId()
        {
            long id = _library.SaveLibraryItem(ThreeStringSet());
            var renamed = ThreeStringSet(manufacturer: "Ernie Ball", name: "2221") with { Id = id };

            Assert.That(_library.SaveLibraryItem(renamed), Is.EqualTo(id));

            var items = _library.ListLibraryItems<StringSet>();
            Assert.That(items.Select(s => (s.Id, s.Name)), Is.EqualTo(new[] { (id, "Ernie Ball 2221") }));
            TestGuitars.AssertSameValues(_library.LoadLibraryItem<StringSet>(id), renamed);
        }

        [Test]
        public void StringSet_RenamedToAnotherSetsName_Throws()
        {
            _library.SaveLibraryItem(ThreeStringSet(name: "EJ16"));
            long other = _library.SaveLibraryItem(ThreeStringSet(name: "EJ17"));

            var error = Assert.Throws<InvalidOperationException>(() =>
                _library.SaveLibraryItem(ThreeStringSet(name: "EJ16") with { Id = other }));
            Assert.That(error.Message, Is.EqualTo("There's already a string set called \"D'Addario EJ16\"."));
        }

        [Test]
        public void StringSet_SameNameTwice_Throws()
        {
            _library.SaveLibraryItem(ThreeStringSet());

            var error = Assert.Throws<InvalidOperationException>(() => _library.SaveLibraryItem(ThreeStringSet()));
            Assert.That(error.Message, Is.EqualTo("There's already a string set called \"D'Addario EJ16\"."));
            Assert.That(_library.ListLibraryItems<StringSet>(), Has.Count.EqualTo(1));
        }

        [Test]
        public void StringSet_SameNameDifferingOnlyInCase_Throws()
        {
            _library.SaveLibraryItem(ThreeStringSet(manufacturer: "D'Addario", name: "EJ16"));

            Assert.Throws<InvalidOperationException>(() =>
                _library.SaveLibraryItem(ThreeStringSet(manufacturer: "d'addario", name: "ej16")));
            Assert.That(_library.ListLibraryItems<StringSet>(), Has.Count.EqualTo(1));
        }

        [Test]
        public void StringSet_ListedByName()
        {
            _library.SaveLibraryItem(ThreeStringSet(manufacturer: "Martin", name: "SP"));
            _library.SaveLibraryItem(ThreeStringSet(manufacturer: "ernie ball", name: "2221"));
            _library.SaveLibraryItem(ThreeStringSet(manufacturer: "D'Addario", name: "EJ16"));

            Assert.That(_library.ListLibraryItems<StringSet>().Select(s => s.Name),
                Is.EqualTo(new[] { "D'Addario EJ16", "ernie ball 2221", "Martin SP" }));
        }

        [Test]
        public void StringSet_InUse_ListsItsNecks()
        {
            long setId = _library.SaveLibraryItem(ThreeStringSet());
            _library.CreateGuitar(GuitarUsing(setId, ThreeStringSet()));

            Assert.That(_library.NecksUsing(setId).Select(n => n.GuitarName), Is.EqualTo(new[] { "OM" }));
        }

        [Test]
        public void DeleteLibraryItem_StringSetInUse_NeedsReplacement()
        {
            long setId = _library.SaveLibraryItem(ThreeStringSet());
            _library.CreateGuitar(GuitarUsing(setId, ThreeStringSet()));

            var error = Assert.Throws<InvalidOperationException>(() => _library.DeleteLibraryItem(setId));
            Assert.That(error.Message, Is.EqualTo("It's used by 1 neck: OM. Choose a replacement."));
            Assert.That(_library.ListLibraryItems<StringSet>().Select(s => s.Id), Is.EqualTo(new[] { setId }));
        }

        [Test]
        public void DeleteLibraryItem_StringSetInUseBySeveralNecks_CountsThem()
        {
            long setId = _library.SaveLibraryItem(ThreeStringSet());
            _library.CreateGuitar(GuitarUsing(setId, ThreeStringSet()));
            _library.SaveAssembly("Spare", GuitarUsing(setId, ThreeStringSet()).Neck);

            var error = Assert.Throws<InvalidOperationException>(() => _library.DeleteLibraryItem(setId));
            Assert.That(error.Message, Is.EqualTo("It's used by 2 necks: Spare, OM. Choose a replacement."));
        }

        [Test]
        public void DeleteLibraryItem_StringSetInUse_SwitchesNecksToReplacement()
        {
            long setId = _library.SaveLibraryItem(ThreeStringSet());
            long guitarId = _library.CreateGuitar(GuitarUsing(setId, ThreeStringSet()));
            _library.SaveAssembly("Spare", GuitarUsing(setId, ThreeStringSet()).Neck);
            long replacement = _library.SaveLibraryItem(ThreeStringSet(name: "EJ17"));

            _library.DeleteLibraryItem(setId, replacement);

            Assert.That(_library.LoadGuitar(guitarId).Neck.StringSet.Id, Is.EqualTo(replacement));
            Assert.That(_library.NecksUsing(replacement), Has.Count.EqualTo(2));
            Assert.That(_library.ListLibraryItems<StringSet>().Select(s => s.Id), Is.EqualTo(new[] { replacement }));
        }

        [Test]
        public void DeleteLibraryItem_StringSetUnused_Deletes()
        {
            long id = _library.SaveLibraryItem(ThreeStringSet());

            _library.DeleteLibraryItem(id);

            Assert.That(_library.ListLibraryItems<StringSet>(), Is.Empty);
        }

        [Test]
        public void BlankStrings_RoundTripThroughTheLibrary()
        {
            var set = SetOf("D'Addario", "Blank", new GuitarString(), PlainString("B", 0.0135, 0.000491), new GuitarString());

            long id = _library.SaveLibraryItem(set);
            var read = _library.LoadLibraryItem<StringSet>(id);

            Assert.That(read.Strings, Has.Count.EqualTo(3));
            Assert.That(read.Strings[1].Description.Value, Is.EqualTo("B"));
            Assert.That(read.Strings[0].Description.Value, Is.Null);
            Assert.That(_library.ListLibraryItems<StringSet>().Single().Name, Is.EqualTo(set.DisplayName));
        }
    }
}
