using System.Globalization;
using System.Linq;
using GuitarForm.Model;
using GuitarForm.Model.Parameters;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    // Checks that formatting, parsing and JSON don't depend on the machine's culture, using German (decimal comma).
    // Behavior pinned here is the current behavior; where it looks wrong, a comment says so.
    public class CultureTests
    {
        static readonly CultureInfo German = new CultureInfo("de-DE");
        static readonly CultureInfo English = new CultureInfo("en-US");

        static readonly ParameterDefinition ScaleLength = ParameterDefinition.Size("Scale Length", "SL", required: true);
        static readonly ParameterDefinition UnitWeight = ParameterDefinition.Number("Unit Weight", "UW", "lb/in", 1e-8);
        static readonly ParameterDefinition StringsPerCourse = ParameterDefinition.Count("Strings per Course", "SPC", 1, 2);
        static readonly ParameterDefinition CoreMaterial = ParameterDefinition.Choice("Core Material", "CM");
        static readonly ParameterDefinition Designer = ParameterDefinition.Text("Designer", "DSG", required: true);

        enum Material { NotSpecified, SolidNylon, Steel }

        CultureInfo _culture;
        CultureInfo _uiCulture;

        [SetUp]
        public void SetUp()
        {
            _culture = CultureInfo.CurrentCulture;
            _uiCulture = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentCulture = German;
            CultureInfo.CurrentUICulture = German;
        }

        [TearDown]
        public void TearDown()
        {
            CultureInfo.CurrentCulture = _culture;
            CultureInfo.CurrentUICulture = _uiCulture;
        }

        static void UseEnglish()
        {
            CultureInfo.CurrentCulture = English;
            CultureInfo.CurrentUICulture = English;
        }

        // --- Format ---

        [Test]
        public void Format_UsesTheCultureGiven()
        {
            var length = new NumberParameter(ScaleLength, 650.5);

            Assert.That(length.Format(LengthUnit.Millimeters, German), Is.EqualTo("650,5"));
            Assert.That(length.Format(LengthUnit.Millimeters, English), Is.EqualTo("650.5"));
        }

        [Test]
        public void Format_WithNoCulture_UsesTheCurrentCulture()
        {
            Assert.That(new NumberParameter(ScaleLength, 650.5).Format(LengthUnit.Millimeters), Is.EqualTo("650,5"));
        }

        [Test]
        public void Format_InInches_UsesTheDecimalComma()
        {
            Assert.That(new NumberParameter(ScaleLength, 25.4 * 25.5).FormatWithUnit(LengthUnit.Inches, German),
                Is.EqualTo("25,5 in"));
        }

        [Test]
        public void Format_CountChoiceAndText_DontChange()
        {
            var choice = new ChoiceParameter<Material>(CoreMaterial, Material.SolidNylon);

            Assert.That(new CountParameter(StringsPerCourse, 2).Format(LengthUnit.Millimeters, German), Is.EqualTo("2"));
            Assert.That(choice.Format(LengthUnit.Millimeters, German), Is.EqualTo(choice.Format(LengthUnit.Millimeters, English)));
            Assert.That(new TextParameter(Designer, "Jesse").Format(LengthUnit.Millimeters, German), Is.EqualTo("Jesse"));
        }

        // --- Parse ---

        [Test]
        public void Parse_Number_AcceptsTheCommaInGerman()
        {
            Assert.That(new NumberParameter(ScaleLength).TryParse("1,5", LengthUnit.Millimeters, German, out var result, out _), Is.True);
            Assert.That(((NumberParameter)result).Value, Is.EqualTo(1.5));
        }

        [Test]
        public void Parse_Number_RejectsThePointInGerman()
        {
            // NumberStyles.Float has no thousands separator, so "1.5" is refused rather than read as 15.
            Assert.That(new NumberParameter(ScaleLength).TryParse("1.5", LengthUnit.Millimeters, German, out _, out var error), Is.False);
            Assert.That(error.Text, Does.Contain("isn't a number"));
        }

        [Test]
        public void Parse_Number_AcceptsThePointInEnglish_AndRejectsTheComma()
        {
            Assert.That(new NumberParameter(ScaleLength).TryParse("1.5", LengthUnit.Millimeters, English, out var result, out _), Is.True);
            Assert.That(((NumberParameter)result).Value, Is.EqualTo(1.5));
            // Neither culture reads both forms: a user pasting "1.5" in a German session gets an error.
            Assert.That(new NumberParameter(ScaleLength).TryParse("1,5", LengthUnit.Millimeters, English, out _, out _), Is.False);
        }

        [Test]
        public void Parse_Number_WithNoCulture_UsesTheCurrentCulture()
        {
            // A caller that passes null gets the machine's culture, not a fixed one.
            Assert.That(new NumberParameter(ScaleLength).TryParse("1,5", LengthUnit.Millimeters, null, out var result, out _), Is.True);
            Assert.That(((NumberParameter)result).Value, Is.EqualTo(1.5));
        }

        [Test]
        public void Parse_Number_InInches_ConvertsToMillimeters()
        {
            new NumberParameter(ScaleLength).TryParse("25,5", LengthUnit.Inches, German, out var result, out _);

            Assert.That(((NumberParameter)result).Value, Is.EqualTo(25.4 * 25.5).Within(1e-9));
        }

        [Test]
        public void Parse_Count_RejectsFractionsInBothForms()
        {
            Assert.That(new CountParameter(StringsPerCourse).TryParse("1,5", LengthUnit.Millimeters, German, out _, out _), Is.False);
            Assert.That(new CountParameter(StringsPerCourse).TryParse("1.5", LengthUnit.Millimeters, German, out _, out _), Is.False);
            Assert.That(new CountParameter(StringsPerCourse).TryParse("2", LengthUnit.Millimeters, German, out var result, out _), Is.True);
            Assert.That(((CountParameter)result).Value, Is.EqualTo(2));
        }

        [Test]
        public void Parse_ChoiceAndText_DontDependOnCulture()
        {
            Assert.That(new ChoiceParameter<Material>(CoreMaterial).TryParse("Steel", LengthUnit.Millimeters, German, out var choice, out _), Is.True);
            Assert.That(((ChoiceParameter<Material>)choice).Value, Is.EqualTo(Material.Steel));
            Assert.That(new TextParameter(Designer).TryParse("Jesse 1,5", LengthUnit.Millimeters, German, out var text, out _), Is.True);
            Assert.That(((TextParameter)text).Value, Is.EqualTo("Jesse 1,5"));
        }

        [Test]
        public void StoredValues_AreNumbersNotCultureText()
        {
            var stored = new NumberParameter(UnitWeight, 0.5).StoredValue;

            Assert.That(stored, Is.TypeOf<double>());
            Assert.That(stored, Is.EqualTo(0.5));
            Assert.That(new NumberParameter(UnitWeight).WithStoredValue(0.5), Is.EqualTo(new NumberParameter(UnitWeight, 0.5)));
        }

        // --- Validation messages ---

        [Test]
        public void Validate_Messages_UseTheCultureGiven()
        {
            var german = new NumberParameter(UnitWeight, 0.5).Validate(LengthUnit.Millimeters, German).Select(m => m.Text);
            var english = new NumberParameter(UnitWeight, 0.5).Validate(LengthUnit.Millimeters, English).Select(m => m.Text);

            Assert.That(german.Select(t => t.Replace(',', '.')), Is.EqualTo(english));
        }

        // --- JSON ---

        [Test]
        public void GuitarDocument_UnderGerman_EqualsEnglish()
        {
            var guitar = TestGuitars.OM();

            string german = GuitarForm.Model.Parts.GuitarDocument.ToJson(guitar);
            UseEnglish();
            string english = GuitarForm.Model.Parts.GuitarDocument.ToJson(guitar);

            Assert.That(german, Is.EqualTo(english));
            Assert.That(german, Does.Contain("1.5"));
            Assert.That(german, Does.Not.Match(@"\d,\d"));
        }

        [Test]
        public void GuitarDocument_RoundTrip_UnderGerman()
        {
            var guitar = TestGuitars.OM();

            TestGuitars.AssertSameValues(GuitarForm.Model.Parts.GuitarDocument.FromJson(GuitarForm.Model.Parts.GuitarDocument.ToJson(guitar)), guitar);
        }

        [Test]
        public void Version1Document_UnderGerman_ReadsTheSameAsUnderEnglish()
        {
            string json = V1Documents.Document("OM", V1Documents.Instrument(645.5, 14),
                V1Documents.Obj(("bodyLength", 495.25), ("ubWidth", 290.5)),
                V1Documents.Obj(("diameter", 100.5), ("tailOffset", 330))).ToJsonString();
            Assert.That(json, Does.Contain("645.5"));

            var german = GuitarForm.Model.Parts.GuitarDocument.FromJson(json);
            UseEnglish();
            var english = GuitarForm.Model.Parts.GuitarDocument.FromJson(json);

            TestGuitars.AssertSameValues(german, english);
            Assert.That(german.ScaleLength.Value, Is.EqualTo(645.5));
            Assert.That(german.Body.Length.Value, Is.EqualTo(495.25));
            Assert.That(german.Body.TopPlate.SoundholeDiameter.Value, Is.EqualTo(100.5));
        }
    }
}
