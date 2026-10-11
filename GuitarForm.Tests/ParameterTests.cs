using System.Globalization;
using System.Linq;
using GuitarForm.Model.Parameters;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    // Formatting, parsing and checking parameter values.
    public class ParameterTests
    {
        static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

        static readonly ParameterDefinition ScaleLength = ParameterDefinition.Size("Scale Length", "SL", required: true);
        static readonly ParameterDefinition Overhang = ParameterDefinition.Distance("Last Fret Overhang", "LFO");
        static readonly ParameterDefinition CenterOffset = ParameterDefinition.Offset("Center Offset", "CO");
        static readonly ParameterDefinition XBraceAngle = ParameterDefinition.Angle("X Brace Angle", "XBA", 0, 90, exclusive: true);
        static readonly ParameterDefinition UnitWeight = ParameterDefinition.Number("Unit Weight", "UW", "lb/in", 1e-8);
        static readonly ParameterDefinition StringsPerCourse = ParameterDefinition.Count("Strings per Course", "SPC", 1, 2);
        static readonly ParameterDefinition HasSoundhole = ParameterDefinition.Flag("Soundhole", "SH");
        static readonly ParameterDefinition CoreMaterial = ParameterDefinition.Choice("Core Material", "CM");
        static readonly ParameterDefinition Designer = ParameterDefinition.Text("Designer", "DSG", required: true);

        enum Material { NotSpecified, SolidNylon, Steel }

        static Parameter Parse(Parameter parameter, string text, LengthUnit unit = LengthUnit.Millimeters)
        {
            Assert.That(parameter.TryParse(text, unit, Culture, out Parameter result, out var error), Is.True,
                error.Text);
            return result;
        }

        static string[] Messages(Parameter parameter, LengthUnit unit = LengthUnit.Millimeters) =>
            parameter.Validate(unit, Culture).Select(m => m.Text).ToArray();

        [Test]
        public void Length_RoundsToResolution()
        {
            Assert.That(new NumberParameter(ScaleLength, 650.126).Format(LengthUnit.Millimeters, Culture),
                Is.EqualTo("650.13"));
        }

        [Test]
        public void Length_DropsTrailingZeros()
        {
            Assert.That(new NumberParameter(ScaleLength, 650).Format(LengthUnit.Millimeters, Culture),
                Is.EqualTo("650"));
        }

        [Test]
        public void Length_ShownInDisplayUnit()
        {
            Assert.That(new NumberParameter(ScaleLength, 635).Format(LengthUnit.Inches, Culture), Is.EqualTo("25"));
        }

        [Test]
        public void Length_ResolutionScalesWithUnit()
        {
            // 0.01 mm is about 0.0004 in, so inches show four decimals.
            Assert.That(new NumberParameter(ScaleLength, 1).Format(LengthUnit.Inches, Culture), Is.EqualTo("0.0394"));
        }

        [Test]
        public void Length_RoundedNegativeZeroShowsAsZero()
        {
            Assert.That(new NumberParameter(CenterOffset, -0.001).Format(LengthUnit.Millimeters, Culture),
                Is.EqualTo("0"));
        }

        [Test]
        public void Length_WithUnit()
        {
            Assert.That(new NumberParameter(ScaleLength, 635).FormatWithUnit(LengthUnit.Inches, Culture),
                Is.EqualTo("25 in"));
        }

        [Test]
        public void Length_NotSet_FormatsEmpty()
        {
            Assert.That(new NumberParameter(ScaleLength).FormatWithUnit(LengthUnit.Millimeters, Culture), Is.Empty);
        }

        [Test]
        public void Length_ParsedFromDisplayUnit_StoredInMillimeters()
        {
            var parsed = (NumberParameter)Parse(new NumberParameter(ScaleLength), "25.5", LengthUnit.Inches);

            Assert.That(parsed.Value, Is.EqualTo(647.7).Within(1e-9));
        }

        [Test]
        public void EmptyText_ClearsValue()
        {
            var parsed = (NumberParameter)Parse(new NumberParameter(ScaleLength, 650), "  ");

            Assert.That(parsed.HasValue, Is.False);
        }

        [Test]
        public void BadNumber_FailsWithMessage()
        {
            bool parsed = new NumberParameter(ScaleLength, 650).TryParse("abc", LengthUnit.Millimeters, Culture,
                out _, out var error);

            Assert.That(parsed, Is.False);
            Assert.That(error.Text, Is.EqualTo("Scale Length (SL): \"abc\" isn't a number."));
        }

        [Test]
        public void Required_NotSet_IsError()
        {
            Assert.That(Messages(new NumberParameter(ScaleLength)), Is.EqualTo(new[] { "Scale Length (SL) is required." }));
        }

        [Test]
        public void Optional_NotSet_IsValid()
        {
            Assert.That(Messages(new NumberParameter(Overhang)), Is.Empty);
        }

        [TestCase(0)]
        [TestCase(-10)]
        public void Size_NotPositive_IsError(double value)
        {
            Assert.That(Messages(new NumberParameter(ScaleLength, value)),
                Is.EqualTo(new[] { "Scale Length (SL) must be greater than 0 mm." }));
        }

        [Test]
        public void Distance_Zero_IsValid()
        {
            Assert.That(Messages(new NumberParameter(Overhang, 0)), Is.Empty);
        }

        [Test]
        public void Distance_Negative_IsError()
        {
            Assert.That(Messages(new NumberParameter(Overhang, -1)),
                Is.EqualTo(new[] { "Last Fret Overhang (LFO) must be at least 0 mm." }));
        }

        [Test]
        public void Offset_Negative_IsValid()
        {
            Assert.That(Messages(new NumberParameter(CenterOffset, -3)), Is.Empty);
        }

        [Test]
        public void Angle_NotConvertedAndShownInDegrees()
        {
            var angle = (NumberParameter)Parse(new NumberParameter(XBraceAngle), "97.5", LengthUnit.Inches);

            Assert.That(angle.Value, Is.EqualTo(97.5));
            Assert.That(angle.FormatWithUnit(LengthUnit.Inches, Culture), Is.EqualTo("97.5°"));
            Assert.That(Messages(angle), Is.EqualTo(new[] { "X Brace Angle (XBA) must be less than 90°." }));
        }

        [Test]
        public void Number_KeepsItsOwnUnit()
        {
            var weight = new NumberParameter(UnitWeight, 0.00002215);

            Assert.That(weight.FormatWithUnit(LengthUnit.Inches, Culture), Is.EqualTo("0.00002215 lb/in"));
        }

        [Test]
        public void Count_RejectsFractions()
        {
            bool parsed = new CountParameter(StringsPerCourse).TryParse("1.5", LengthUnit.Millimeters, Culture,
                out _, out var error);

            Assert.That(parsed, Is.False);
            Assert.That(error.Text, Is.EqualTo("Strings per Course (SPC): \"1.5\" isn't a whole number."));
        }

        [TestCase(0, "Strings per Course (SPC) must be at least 1.")]
        [TestCase(3, "Strings per Course (SPC) must be at most 2.")]
        public void Count_OutOfRange_IsError(int value, string message)
        {
            Assert.That(Messages(new CountParameter(StringsPerCourse, value)), Is.EqualTo(new[] { message }));
        }

        [TestCase("yes", true)]
        [TestCase("No", false)]
        [TestCase("true", true)]
        public void Flag_Parses(string text, bool value)
        {
            Assert.That(((FlagParameter)Parse(new FlagParameter(HasSoundhole), text)).Value, Is.EqualTo(value));
        }

        [Test]
        public void Choice_ShownInSentenceCase()
        {
            Assert.That(new ChoiceParameter<Material>(CoreMaterial, Material.NotSpecified).Format(LengthUnit.Millimeters),
                Is.EqualTo("Not specified"));
        }

        [TestCase("solid nylon")]
        [TestCase("SolidNylon")]
        public void Choice_ParsesTextOrName(string text)
        {
            var parsed = (ChoiceParameter<Material>)Parse(new ChoiceParameter<Material>(CoreMaterial), text);

            Assert.That(parsed.Value, Is.EqualTo(Material.SolidNylon));
        }

        [Test]
        public void OptionTexts_ArePickableAndParse()
        {
            var choice = new ChoiceParameter<Material>(CoreMaterial);
            Assert.That(choice.OptionTexts, Is.EqualTo(new[] { "Not specified", "Solid nylon", "Steel" }));
            Assert.That(((ChoiceParameter<Material>)Parse(choice, choice.OptionTexts[1])).Value, Is.EqualTo(Material.SolidNylon));

            var flag = new FlagParameter(HasSoundhole);
            Assert.That(flag.OptionTexts, Is.EqualTo(new[] { "Yes", "No" }));
            Assert.That(((FlagParameter)Parse(flag, flag.OptionTexts[0])).Value, Is.True);

            Assert.That(new NumberParameter(ScaleLength, 650).OptionTexts, Is.Empty);
        }

        [Test]
        public void Choice_UnknownOption_ListsOptions()
        {
            new ChoiceParameter<Material>(CoreMaterial).TryParse("gut", LengthUnit.Millimeters, Culture, out _,
                out var error);

            Assert.That(error.Text,
                Is.EqualTo("Core Material (CM): \"gut\" isn't one of: Not specified, Solid nylon, Steel."));
        }

        [Test]
        public void Text_TrimmedAndBlankIsNotSet()
        {
            Assert.That(((TextParameter)Parse(new TextParameter(Designer), "  Jesse ")).Value, Is.EqualTo("Jesse"));
            Assert.That(Messages(Parse(new TextParameter(Designer), " ")),
                Is.EqualTo(new[] { "Designer (DSG) is required." }));
        }
    }
}
