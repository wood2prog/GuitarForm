using System.Globalization;
using System.Linq;
using GuitarForm.Model.Parameters;
using GuitarForm.Model.Parts;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    // The advanced neck profile's parameters, defaults and presets.
    public class NeckProfilePresetTests
    {
        static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

        static string[] Messages(Neck neck) =>
            neck.Validate(LengthUnit.Millimeters, Culture).Select(m => m.Text).ToArray();

        [Test]
        public void Presets_AreTheNamedTable()
        {
            Assert.That(NeckProfilePresets.All.Select(p => (p.Name, p.Squareness, p.Asymmetry, p.Blend)),
                Is.EqualTo(new[]
                {
                    ("Modern C", 2.0, 0.0, 50.0), ("Soft V", 1.5, 0.0, 25.0), ("Hard V", 1.1, 0.0, 5.0),
                    ("D", 3.2, 0.0, 100.0), ("Asymmetric", 2.0, 15.0, 50.0),
                }));
        }

        [Test]
        public void Presets_AreWithinTheParameterRanges()
        {
            foreach (var p in NeckProfilePresets.All)
            {
                var neck = new Neck
                {
                    SquarenessAtFirstFret = new(Neck.Definitions.SquarenessAtFirstFret, p.Squareness),
                    AsymmetryAtFirstFret = new(Neck.Definitions.AsymmetryAtFirstFret, p.Asymmetry),
                    BlendAtFirstFret = new(Neck.Definitions.BlendAtFirstFret, p.Blend),
                };
                Assert.That(Messages(neck), Is.Empty, p.Name);
            }
        }

        [Test]
        public void Defaults_AreUsedWhenUnset_WithoutRemarks()
        {
            var neck = new Neck();

            Assert.That(neck.SquarenessOrDefault(neck.SquarenessAtHeelEnd), Is.EqualTo(Neck.DefaultSquareness));
            Assert.That(neck.AsymmetryOrDefault(neck.AsymmetryAtHeelEnd), Is.EqualTo(0));
            Assert.That(neck.BlendOrDefault(neck.BlendAtHeelEnd), Is.EqualTo(50));
            Assert.That(Messages(neck), Is.Empty);
            Assert.That(neck.ProfileAdvancedMode.Value, Is.False);
        }

        [Test]
        public void Ranges_AreChecked()
        {
            var d = Neck.Definitions.SquarenessAtSelectedFret;
            Assert.That(Messages(new Neck { SquarenessAtSelectedFret = new(d, 1) }), Is.Empty);
            Assert.That(Messages(new Neck { SquarenessAtSelectedFret = new(d, 10) }), Is.Empty);
            Assert.That(Messages(new Neck { SquarenessAtSelectedFret = new(d, 0.9) }), Has.Length.EqualTo(1));
            Assert.That(Messages(new Neck { SquarenessAtSelectedFret = new(d, 10.1) }), Has.Length.EqualTo(1));
            Assert.That(Messages(new Neck { AsymmetryAtHeelSection = new(Neck.Definitions.AsymmetryAtHeelSection, -50) }), Is.Empty);
            Assert.That(Messages(new Neck { AsymmetryAtHeelSection = new(Neck.Definitions.AsymmetryAtHeelSection, 51) }), Has.Length.EqualTo(1));
            Assert.That(Messages(new Neck { BlendAtHeelEnd = new(Neck.Definitions.BlendAtHeelEnd, 0) }), Is.Empty);
            Assert.That(Messages(new Neck { BlendAtHeelEnd = new(Neck.Definitions.BlendAtHeelEnd, 101) }), Has.Length.EqualTo(1));
            Assert.That(Messages(new Neck { BlendAtHeelEnd = new(Neck.Definitions.BlendAtHeelEnd, -1) }), Has.Length.EqualTo(1));
        }

        [Test]
        public void Formatting_UsesResolutionAndUnit()
        {
            var squareness = new Neck { SquarenessAtFirstFret = new(Neck.Definitions.SquarenessAtFirstFret, 3.2) }.SquarenessAtFirstFret;
            var asymmetry = new Neck { AsymmetryAtFirstFret = new(Neck.Definitions.AsymmetryAtFirstFret, -15) }.AsymmetryAtFirstFret;

            Assert.That(squareness.FormatWithUnit(LengthUnit.Millimeters, Culture).Trim(), Is.EqualTo("3.2"));
            Assert.That(asymmetry.FormatWithUnit(LengthUnit.Millimeters, Culture), Does.Contain("-15").And.Contain("%"));
        }
    }
}
