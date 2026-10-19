using GuitarForm.Model;
using GuitarForm.Model.Parts;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    public class GuitarFieldMapTests
    {
        static GuitarDesign FullDesign(bool soundhole = true) => new GuitarDesign
        {
            Name = "Full",
            Instrument = new Instrument { ScaleLength = 645.16, NeckJoinFret = 14 },
            Body = new Model.Body
            {
                BodyLength = 495, UbWidth = 290, UbOffset = 301, UbRadius = 160, UbSecondaryOffset = 12,
                WaistRadius = 80, WaistWidth = 240, WaistOffset = 210, LbWidth = 390, LbOffset = 120, LbRadius = 200,
                LbSecondaryOffset = 7, HeelWidth = 60, TailDepth = 105, NeckDepth = 90,
            },
            Soundhole = soundhole ? new Soundhole { Diameter = 100, HorizontalOffset = 3, TailOffset = 330 } : null,
        };

        // The documented, frozen mapping: a version 1 design document read as a guitar must give the same design back.
        [TestCase(true)]
        [TestCase(false)]
        public void ToDesign_ReadsEveryFieldOfTheFrozenVersion1Mapping(bool soundhole)
        {
            var design = FullDesign(soundhole);
            var guitar = GuitarDocument.Read(DesignDocument.ToJson(design)).Guitar;

            Assert.That(GuitarFieldMap.ToDesign(guitar), Is.EqualTo(design));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WithDesign_OnAnEmptyGuitar_RoundTripsEveryField(bool soundhole)
        {
            var design = FullDesign(soundhole);

            var guitar = GuitarFieldMap.WithDesign(new Guitar(), design);

            Assert.That(GuitarFieldMap.ToDesign(guitar), Is.EqualTo(design));
            var fromDocument = GuitarDocument.Read(DesignDocument.ToJson(design)).Guitar;
            Assert.That(guitar.Body.Length, Is.EqualTo(fromDocument.Body.Length));
            Assert.That(guitar.Body.TopPlate.SoundholeOffsetFromCenterline, Is.EqualTo(fromDocument.Body.TopPlate.SoundholeOffsetFromCenterline));
            Assert.That(guitar.Body.TopPlate.Soundhole, Is.EqualTo(fromDocument.Body.TopPlate.Soundhole));
        }

        [Test]
        public void WithDesign_OfTheGuitarsOwnView_IsTheSameObject()
        {
            var guitar = TestGuitars.OM();

            Assert.That(GuitarFieldMap.WithDesign(guitar, GuitarFieldMap.ToDesign(guitar)), Is.SameAs(guitar));
        }

        [Test]
        public void AnEdit_ChangesOnlyItsOwnValues_AndLeavesTheNewFieldsAlone()
        {
            var guitar = TestGuitars.OM();
            var design = GuitarFieldMap.ToDesign(guitar);

            var edited = GuitarFieldMap.WithDesign(guitar,
                design with { Body = design.Body with { LbWidth = 401 } });

            Assert.That(edited.Body.LowerBoutWidth.Value, Is.EqualTo(401));
            Assert.That(edited with { Body = edited.Body with { LowerBoutWidth = guitar.Body.LowerBoutWidth } },
                Is.EqualTo(guitar));
        }

        [Test]
        public void AnUnsetRequiredValue_StaysUnset_WhenAnotherValueIsEdited()
        {
            var guitar = new Guitar();
            var design = GuitarFieldMap.ToDesign(guitar);
            Assert.That(design.Instrument, Is.EqualTo(new Instrument()));

            var edited = GuitarFieldMap.WithDesign(guitar,
                design with { Instrument = design.Instrument with { ScaleLength = 650 } });

            Assert.That(edited.ScaleLength.Value, Is.EqualTo(650));
            Assert.That(edited.NeckJoinFret.Value, Is.Null);
        }

        [Test]
        public void RemovingTheSoundhole_ClearsItsValues_AndBringingItBackWritesThem()
        {
            var guitar = GuitarFieldMap.WithDesign(new Guitar(), FullDesign());
            var design = GuitarFieldMap.ToDesign(guitar);

            var without = GuitarFieldMap.WithDesign(guitar, design with { Soundhole = null });
            Assert.That(without.Body.TopPlate.Soundhole.Value, Is.False);
            Assert.That(without.Body.TopPlate.SoundholeDiameter.Value, Is.Null);
            Assert.That(without.Body.TopPlate.SoundholeOffsetFromTailEnd.Value, Is.Null);
            Assert.That(without.Body.TopPlate.SoundholeOffsetFromCenterline.Value, Is.Null);

            var back = GuitarFieldMap.WithDesign(without, design);
            Assert.That(back, Is.EqualTo(guitar));

            var blank = GuitarFieldMap.WithDesign(without, design with { Soundhole = new Soundhole() });
            Assert.That(blank.Body.TopPlate.Soundhole.Value, Is.True);
            Assert.That(blank.Body.TopPlate.SoundholeDiameter.Value, Is.EqualTo(0));
            Assert.That(blank.Body.TopPlate.SoundholeOffsetFromTailEnd.Value, Is.EqualTo(0));
        }

        [Test]
        public void TheName_IsPartOfTheView()
        {
            var guitar = TestGuitars.OM("Alpha");

            Assert.That(GuitarFieldMap.ToDesign(guitar).Name, Is.EqualTo("Alpha"));
            var renamed = GuitarFieldMap.WithDesign(guitar, GuitarFieldMap.ToDesign(guitar) with { Name = "Beta" });
            Assert.That(renamed.Name.Value, Is.EqualTo("Beta"));
        }
    }
}
