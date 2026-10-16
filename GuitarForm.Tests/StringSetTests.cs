using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GuitarForm.Model.Parameters;
using GuitarForm.Model.Parts;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    // String sets and strings: unit weight conversion, adding and removing strings, and which settings apply.
    public class StringSetTests
    {
        static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

        static GuitarString Complete(StringConstruction construction, StringCoreMaterial core = StringCoreMaterial.NotSpecified) =>
            new GuitarString
            {
                Diameter = new(GuitarString.Definitions.Diameter, 0.305),
                Construction = new(GuitarString.Definitions.Construction, construction),
                UnitWeight = new(GuitarString.Definitions.UnitWeight, 0.000584),
                CoreMaterial = new(GuitarString.Definitions.CoreMaterial, core),
                CoreModulus = new(GuitarString.Definitions.CoreModulus, 5),
                CoreStrength = new(GuitarString.Definitions.CoreStrength, 100),
            };

        static StringSet ThreeStrings() => new StringSet
        {
            Strings = new[] { Complete(StringConstruction.Plain), Complete(StringConstruction.Plain), Complete(StringConstruction.Plain) },
            StringCount = new(StringSet.Definitions.StringCount, 3),
        };

        static string[] Messages(StringSet set) =>
            set.Validate(LengthUnit.Millimeters, Culture).Select(m => m.Text).ToArray();

        [Test]
        public void UnitWeight_RoundTripsThroughLbPerIn()
        {
            foreach (double kgPerM in new[] { 0.0, 0.000584, 0.00983, 0.0357, 1.25 })
            {
                double back = StringUnitWeight.ToKgPerM(StringUnitWeight.ToLbPerIn(kgPerM));
                Assert.That(back, Is.EqualTo(kgPerM).Within(1e-9));
            }
        }

        [Test]
        public void UnitWeight_ConvertsTwoTenThousandthsPoundPerInch()
        {
            // 0.0002 lb/in = 0.0002 * 0.45359237 kg / 0.0254 m, computed here from the definitions rather than the constant.
            double expected = 0.0002 * 0.45359237 / 0.0254;
            Assert.That(StringUnitWeight.ToKgPerM(0.0002), Is.EqualTo(expected).Within(1e-9));
            Assert.That(StringUnitWeight.ToLbPerIn(expected), Is.EqualTo(0.0002).Within(1e-9));
        }

        [Test]
        public void AddString_AppendsPlainStringAndKeepsCountInStep()
        {
            var added = ThreeStrings().WithAddedString();

            Assert.That(added.Strings, Has.Count.EqualTo(4));
            Assert.That(added.Strings[3].Construction.Value, Is.EqualTo(StringConstruction.Plain));
            Assert.That(added.StringCount.Value, Is.EqualTo(4));
            Assert.That(Messages(added).Where(m => m.StartsWith("String Count")), Is.Empty);
        }

        [Test]
        public void RemoveString_RemovesAtIndexAndKeepsCountInStep()
        {
            var set = ThreeStrings();
            var removed = set.WithRemovedString(0);

            Assert.That(removed.Strings, Has.Count.EqualTo(2));
            Assert.That(removed.Strings, Is.EqualTo(set.Strings.Skip(1).ToList()));
            Assert.That(removed.StringCount.Value, Is.EqualTo(2));
            Assert.That(Messages(removed).Where(m => m.StartsWith("String Count")), Is.Empty);
        }

        [Test]
        public void WithStrings_SetsCountToListLength()
        {
            var set = ThreeStrings().WithStrings(new List<GuitarString> { Complete(StringConstruction.Plain) });

            Assert.That(set.Strings, Has.Count.EqualTo(1));
            Assert.That(set.StringCount.Value, Is.EqualTo(1));
        }

        [Test]
        public void RemoveString_OutOfRange_Throws()
        {
            var set = ThreeStrings();

            Assert.Throws<System.ArgumentOutOfRangeException>(() => set.WithRemovedString(-1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => set.WithRemovedString(3));
        }

        [Test]
        public void PlainString_UsesNoCoreSettings()
        {
            var plain = Complete(StringConstruction.Plain);

            Assert.That(plain.IsWound, Is.False);
            Assert.That(plain.UsesCoreMaterial, Is.False);
            Assert.That(plain.UsesHexCore, Is.False);
            Assert.That(plain.UsesCoreProperties, Is.False);
        }

        [Test]
        public void WoundStringWithoutCoreMaterial_UsesCoreSettingsAndProperties()
        {
            var wound = Complete(StringConstruction.Wound, StringCoreMaterial.NotSpecified);

            Assert.That(wound.IsWound, Is.True);
            Assert.That(wound.UsesCoreMaterial, Is.True);
            Assert.That(wound.UsesHexCore, Is.True);
            Assert.That(wound.UsesCoreProperties, Is.True);
        }

        [Test]
        public void WoundStringWithSteelCore_UsesCoreMaterialButNotCoreProperties()
        {
            var wound = Complete(StringConstruction.Wound, StringCoreMaterial.Steel);

            Assert.That(wound.IsWound, Is.True);
            Assert.That(wound.UsesCoreMaterial, Is.True);
            Assert.That(wound.UsesHexCore, Is.True);
            Assert.That(wound.UsesCoreProperties, Is.False);
        }
    }
}
