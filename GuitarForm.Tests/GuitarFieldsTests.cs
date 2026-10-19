using System.Globalization;
using System.Linq;
using GuitarForm.Model;
using GuitarForm.Model.Parameters;
using GuitarForm.Model.Parts;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    // The single field table (GuitarFields) must offer every field the tabs showed through Sections.cs and
    // NewFields.cs, in the same tab, group and order. The expected layout below is that old layout, written out.
    public class GuitarFieldsTests
    {
        // tab | group | abbreviations of the old sections' fields (the new-field groups give their field counts).
        static readonly string[] OldFieldLayout =
        {
            "Instrument|Scale|SL,NJF",
            "Body|Outline|BDL,HFW",
            "Body|Upper bout|UBW,UBO,UBPR,UBSO",
            "Body|Waist|WSW,WSR,WSO",
            "Body|Lower bout|LBW,LBO,LBPR,LBSO",
            "Body|Side view|BDDT,BDDN,BDPS",
            "Soundhole|Soundhole|SH",
            "Soundhole|Size and position|SHD,SHOC,SHOT",
        };

        // tab | group | field count, from NewFields.Groups.
        static readonly string[] OldNewFieldCounts =
        {
            "Body|Cutaway|6", "Body|Blocks|4",
            "Bracing|X brace|6", "Bracing|Graft|5", "Bracing|Upper transverse bar|4", "Bracing|Finger braces|9",
            "Bracing|Tone bars|6", "Bracing|Soundhole braces|4", "Bracing|Bridge patch|3",
            "Fretboard|Width and string spacing|4", "Fretboard|Courses|3", "Fretboard|Frets|2",
            "Fretboard|Fret slots|1", "Fretboard|Thickness and radius|3", "Fretboard|Dot markers|5",
            "Fretboard|Setup (information only)|4",
            "Neck|Profile mode|1", "Neck|Angle|2", "Neck|Nut|2", "Neck|Thickness|3", "Neck|Profile at first fret|4",
            "Neck|Profile at selected fret|5", "Neck|Heel shaft|2", "Neck|Heel end|8", "Neck|Heel cap|2",
            "Neck|Heel section|9",
        };

        [Test]
        public void KeepsTheOldSectionsLayout()
        {
            foreach (string line in OldFieldLayout)
            {
                var parts = line.Split('|');
                var group = GuitarFields.Groups.Single(g => g.Tab == parts[0] && g.Title == parts[1]);
                Assert.That(string.Join(",", group.Fields.Take(parts[2].Split(',').Length - (parts[1] == "Side view" ? 1 : 0))
                    .Select(f => f.Abbreviation)), Is.EqualTo(parts[2].Replace(",BDPS", "")), line);
            }
            // The Side view group adds the profile skew after the old two depths.
            var side = GuitarFields.Groups.Single(g => g.Tab == "Body" && g.Title == "Side view");
            Assert.That(side.Fields.Select(f => f.Abbreviation), Is.EqualTo(new[] { "BDDT", "BDDN", "BDPS" }));
        }

        [Test]
        public void KeepsTheOldNewFieldGroups()
        {
            foreach (string line in OldNewFieldCounts)
            {
                var parts = line.Split('|');
                var group = GuitarFields.Groups.Single(g => g.Tab == parts[0] && g.Title == parts[1]);
                Assert.That(group.Fields, Has.Length.EqualTo(int.Parse(parts[2])), line);
            }
        }

        [Test]
        public void ShowsTheOldGroupsInTheOldOrder()
        {
            Assert.That(GuitarFields.Tabs, Is.EqualTo(new[] { "Instrument", "Body", "Soundhole", "Bracing", "Fretboard", "Neck" }));
            Assert.That(GuitarFields.GroupsOf("Body").Select(g => g.Title), Is.EqualTo(new[]
                { "Outline", "Upper bout", "Waist", "Lower bout", "Side view", "Cutaway", "Blocks" }));
            Assert.That(GuitarFields.GroupsOf("Neck").Select(g => g.Title), Is.EqualTo(new[]
            {
                "Profile mode", "Angle", "Nut", "Thickness", "Profile at first fret", "Profile at selected fret",
                "Heel shaft", "Heel end", "Heel cap", "Heel section",
            }));
            // 21 on the Instrument, Body and Soundhole tabs (2 + 15 + 3 + 1 for the yes/no) and 91 on the other tabs.
            Assert.That(GuitarFields.All, Has.Count.EqualTo(GuitarFields.Groups.Sum(g => g.Fields.Length)));
        }

        [Test]
        public void EachFieldIsUniqueAndLabelledFromTheGlossary()
        {
            Assert.That(GuitarFields.All.Select(f => f.Abbreviation), Is.Unique);
            foreach (var field in GuitarFields.All)
                Assert.That(field.Label, Is.EqualTo($"{field.Definition.Name} ({field.Definition.Abbreviation})"));
            Assert.That(GuitarFields.All.First().Label, Is.EqualTo("Scale Length (SL)"));
        }

        [Test]
        public void EveryFieldReadsAndWritesItsOwnParameter()
        {
            var guitar = new Guitar();
            foreach (var field in GuitarFields.All)
            {
                var parameter = field.Get(guitar);
                // Writing the value back changes nothing, and the field writes only its own parameter.
                Assert.That(field.Set(guitar, parameter), Is.EqualTo(guitar), field.Abbreviation);
                // The parameter is found again in the edited guitar.
                if (field.IsChoice || !parameter.TryParse("1", LengthUnit.Millimeters, CultureInfo.InvariantCulture, out var one, out _))
                    continue;
                var edited = field.Set(guitar, one);
                Assert.That(field.Get(edited), Is.EqualTo(one), field.Abbreviation);
                Assert.That(GuitarFields.All.Where(f => f != field && !ReferenceEquals(f, field))
                    .Count(f => !Equals(f.Get(edited), f.Get(guitar))), Is.EqualTo(0), field.Abbreviation);
            }
        }

        [Test]
        public void ChoicesAndYesNoValuesArePickedAndNumbersAreTyped()
        {
            Assert.That(GuitarFields.All.Single(f => f.Abbreviation == "SH").IsChoice, Is.True);
            Assert.That(GuitarFields.All.Single(f => f.Abbreviation == "CTT").IsChoice, Is.True);
            Assert.That(GuitarFields.All.Single(f => f.Abbreviation == "SL").IsDecimal, Is.True);
            var njf = GuitarFields.All.Single(f => f.Abbreviation == "NJF");
            Assert.That(njf.IsDecimal, Is.False);
            Assert.That(njf.IsChoice, Is.False);
        }

        [Test]
        public void SoundholeFieldsAreEnabledOnlyWithASoundhole()
        {
            var without = new Guitar();
            var with = without with
            {
                Body = without.Body with { TopPlate = without.Body.TopPlate with { Soundhole = new(TopPlate.Definitions.Soundhole, true) } },
            };
            foreach (var abbreviation in new[] { "SHD", "SHOC", "SHOT" })
            {
                var field = GuitarFields.All.Single(f => f.Abbreviation == abbreviation);
                Assert.That(field.IsEnabled(without), Is.False, abbreviation);
                Assert.That(field.IsEnabled(with), Is.True, abbreviation);
            }
            Assert.That(GuitarFields.All.Single(f => f.Abbreviation == "SH").IsEnabled(without), Is.True);
        }

        [Test]
        public void CourseSpacingsNeedTwoStringCourses()
        {
            var one = new Guitar();
            var two = one with
            {
                Neck = one.Neck with { Fretboard = one.Neck.Fretboard with { StringsPerCourse = one.Neck.Fretboard.StringsPerCourse with { Value = 2 } } },
            };
            var spacing = GuitarFields.GroupsOf("Fretboard").Single(g => g.Title == "Courses").Fields.Skip(1).ToArray();
            Assert.That(spacing, Has.Length.EqualTo(2));
            foreach (var field in spacing)
            {
                Assert.That(field.IsEnabled(one with { Neck = one.Neck with { Fretboard = one.Neck.Fretboard with { StringsPerCourse = one.Neck.Fretboard.StringsPerCourse with { Value = 1 } } } }), Is.False);
                Assert.That(field.IsEnabled(two), Is.True);
            }
        }

        [Test]
        public void ProfileModeChoosesSquarenessOrBlend()
        {
            var simple = new Guitar();
            var advanced = simple with { Neck = simple.Neck with { ProfileAdvancedMode = simple.Neck.ProfileAdvancedMode with { Value = true } } };
            var group = GuitarFields.GroupsOf("Neck").Single(g => g.Title == "Profile at first fret");
            var squareness = group.Fields.Single(f => f.Abbreviation == simple.Neck.SquarenessAtFirstFret.Definition.Abbreviation);
            var blend = group.Fields.Single(f => f.Abbreviation == simple.Neck.BlendAtFirstFret.Definition.Abbreviation);
            var asymmetry = group.Fields.Single(f => f.Abbreviation == simple.Neck.AsymmetryAtFirstFret.Definition.Abbreviation);
            Assert.That(squareness.IsVisible(simple), Is.True);
            Assert.That(squareness.IsVisible(advanced), Is.False);
            Assert.That(blend.IsVisible(simple), Is.False);
            Assert.That(blend.IsVisible(advanced), Is.True);
            Assert.That(asymmetry.IsVisible(simple) && asymmetry.IsVisible(advanced), Is.True);
            Assert.That(squareness.PlaceholderText(CultureInfo.InvariantCulture), Is.EqualTo("2"));
            Assert.That(blend.PlaceholderText(CultureInfo.InvariantCulture), Is.EqualTo("50"));
            Assert.That(blend.Step, Is.EqualTo(1));
            Assert.That(asymmetry.Step, Is.EqualTo(1));
            Assert.That(squareness.Step, Is.EqualTo(0.1));
        }

        [Test]
        public void RequiredFieldsShowRequiredAndOldDescriptionsAreTooltips()
        {
            var length = GuitarFields.All.Single(f => f.Abbreviation == "BDL");
            Assert.That(length.PlaceholderText(), Is.EqualTo("required"));
            Assert.That(length.Tooltip, Is.EqualTo("Overall length of the guitar body"));
            Assert.That(GuitarFields.All.Single(f => f.Abbreviation == "UBW").PlaceholderText(), Is.EqualTo(""));
        }

        [Test]
        public void OnlyTheNeckProfileStationsOfferAStartingShape()
        {
            Assert.That(GuitarFields.Groups.Where(g => g.HasStartingShape).Select(g => g.Title), Is.EqualTo(new[]
                { "Profile at first fret", "Profile at selected fret", "Heel end", "Heel section" }));
            Assert.That(GuitarFields.Groups.Where(g => g.HasMarkers).Select(g => g.Title), Is.EqualTo(new[] { "Dot markers" }));
        }
    }
}
