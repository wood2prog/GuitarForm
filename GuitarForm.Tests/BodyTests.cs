using System.Globalization;
using System.Linq;
using GuitarForm.Model.Parameters;
using GuitarForm.Model.Parts;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    // The checks between the body's values, using the outline from the docs/Body.md Quick Test.
    public class BodyTests
    {
        static readonly Body QuickTest = new Body
        {
            Length = new(Body.Definitions.Length, 600),
            UpperBoutWidth = new(Body.Definitions.UpperBoutWidth, 280),
            UpperBoutOffsetFromNeckEnd = new(Body.Definitions.UpperBoutOffsetFromNeckEnd, 5),
            UpperBoutPrimaryRadius = new(Body.Definitions.UpperBoutPrimaryRadius, 100),
            UpperBoutSecondaryOffset = new(Body.Definitions.UpperBoutSecondaryOffset, 30),
            WaistWidth = new(Body.Definitions.WaistWidth, 240),
            WaistOffsetFromTailEnd = new(Body.Definitions.WaistOffsetFromTailEnd, 300),
            WaistRadius = new(Body.Definitions.WaistRadius, 60),
            LowerBoutWidth = new(Body.Definitions.LowerBoutWidth, 380),
            LowerBoutOffsetFromTailEnd = new(Body.Definitions.LowerBoutOffsetFromTailEnd, 10),
            LowerBoutPrimaryRadius = new(Body.Definitions.LowerBoutPrimaryRadius, 120),
            LowerBoutSecondaryOffset = new(Body.Definitions.LowerBoutSecondaryOffset, 40),
            HeelFlatWidth = new(Body.Definitions.HeelFlatWidth, 56),
        };

        static string[] Messages(Part part) =>
            part.Validate(LengthUnit.Millimeters, CultureInfo.InvariantCulture).Select(m => m.Text).ToArray();

        [Test]
        public void QuickTest_IsValid()
        {
            Assert.That(Messages(QuickTest), Is.Empty);
        }

        [Test]
        public void WidestPoints()
        {
            Assert.That(QuickTest.LowerBoutWidestOffset, Is.EqualTo(130));
            Assert.That(QuickTest.UpperBoutWidestOffset, Is.EqualTo(495));
        }

        [Test]
        public void WaistWiderThanUpperBout_IsError()
        {
            var body = QuickTest with { WaistWidth = new(Body.Definitions.WaistWidth, 300) };

            Assert.That(Messages(body), Does.Contain("Waist Width (WSW) can't be more than Upper Bout Width (UBW)."));
        }

        [Test]
        public void WaistAsWideAsUpperBout_IsNotThatError()
        {
            var body = QuickTest with { WaistWidth = new(Body.Definitions.WaistWidth, 280) };

            Assert.That(Messages(body), Has.None.Contains("can't be more than"));
        }

        [Test]
        public void WaistBelowLowerBoutWidest_IsError()
        {
            var body = QuickTest with { WaistOffsetFromTailEnd = new(Body.Definitions.WaistOffsetFromTailEnd, 120) };

            Assert.That(Messages(body), Does.Contain(
                "The lower bout's widest point (130 mm from the tail end) must be closer to the tail end than the waist " +
                "(Waist Offset from Tail End (WSO))."));
        }

        [Test]
        public void WaistAboveUpperBoutWidest_IsError()
        {
            var body = QuickTest with { WaistOffsetFromTailEnd = new(Body.Definitions.WaistOffsetFromTailEnd, 500) };

            Assert.That(Messages(body), Does.Contain(
                "The upper bout's widest point (495 mm from the tail end) must be farther from the tail end than the " +
                "waist (Waist Offset from Tail End (WSO))."));
        }

        [Test]
        public void HeelFlatPastUpperBoutCenters_IsError()
        {
            // The upper bout centers are at x = ±40, so the heel flat can be 80 wide.
            Assert.That(QuickTest.MaximumHeelFlatWidth, Is.EqualTo(80));
            Assert.That(Messages(QuickTest with { HeelFlatWidth = new(Body.Definitions.HeelFlatWidth, 80) }), Is.Empty);
            Assert.That(Messages(QuickTest with { HeelFlatWidth = new(Body.Definitions.HeelFlatWidth, 90) }),
                Is.EqualTo(new[]
                {
                    "Heel Flat Width (HFW) can't reach past the centers of the Upper Bout Primary Radius (UBPR): it " +
                    "can be at most 80 mm.",
                }));
        }

        [Test]
        public void UpperBoutRadiusPastCenterline_NoHeelFlatFits()
        {
            var body = QuickTest with
            {
                UpperBoutPrimaryRadius = new(Body.Definitions.UpperBoutPrimaryRadius, 150),
                UpperBoutOffsetFromNeckEnd = new(Body.Definitions.UpperBoutOffsetFromNeckEnd, 0),
                UpperBoutSecondaryOffset = new(Body.Definitions.UpperBoutSecondaryOffset, 0),
            };

            Assert.That(Messages(body), Does.Contain(
                "Upper Bout Primary Radius (UBPR) is more than half the Upper Bout Width (UBW), so its center is past " +
                "the centerline and no heel flat fits."));
        }

        [Test]
        public void SecondaryRadiusOverlappingWaist_IsError()
        {
            // docs/Body.md: the most LbSO can be is about 61.4.
            var body = QuickTest with { LowerBoutSecondaryOffset = new(Body.Definitions.LowerBoutSecondaryOffset, 70) };

            Assert.That((QuickTest with { LowerBoutSecondaryOffset = new(Body.Definitions.LowerBoutSecondaryOffset, 61.4) })
                .LowerBoutWaistOverlap, Is.LessThan(0));
            Assert.That(body.LowerBoutWaistOverlap, Is.EqualTo(2.412).Within(0.001));
            Assert.That(Messages(body), Is.EqualTo(new[]
            {
                "The lower bout's secondary radius overlaps the Waist Radius (WSR) by 2.41 mm.",
            }));
        }

        [Test]
        public void SecondaryOffsetNotSet_ChecksPrimary()
        {
            var body = QuickTest with { UpperBoutSecondaryOffset = new(Body.Definitions.UpperBoutSecondaryOffset) };

            Assert.That(body.UpperBoutWaistOverlap, Is.Not.Null);
        }

        [Test]
        public void Cutaway_RequiresItsValues()
        {
            var florentine = QuickTest with { CutawayType = new(Body.Definitions.CutawayType, CutawayType.Florentine) };
            var venetian = QuickTest with { CutawayType = new(Body.Definitions.CutawayType, CutawayType.Venetian) };

            Assert.That(Messages(florentine), Is.EqualTo(new[]
            {
                "Cutaway Offset from Neck (CTO) is required when Cutaway Type (CTT) is Florentine.",
                "Cutaway Width (CTW) is required when Cutaway Type (CTT) is Florentine.",
                "Cutaway Depth (CTD) is required when Cutaway Type (CTT) is Florentine.",
            }));
            Assert.That(Messages(venetian), Has.Length.EqualTo(5));
            Assert.That(Messages(venetian), Does.Contain("Cutaway Point Radius (CTPR) is required when Cutaway Type (CTT) is Venetian."));
        }

        [Test]
        public void BlocksFillingBody_IsError()
        {
            var body = QuickTest with
            {
                HeadblockDepth = new(Body.Definitions.HeadblockDepth, 300),
                TailblockDepth = new(Body.Definitions.TailblockDepth, 300),
            };

            Assert.That(Messages(body), Is.EqualTo(new[]
            {
                "Headblock Depth (HBD) and Tailblock Depth (TBD) together must be less than Body Length (BDL).",
            }));
        }

        static Body WithSoundhole(double? diameter, double offset) => QuickTest with
        {
            TopPlate = new TopPlate
            {
                Soundhole = new(TopPlate.Definitions.Soundhole, true),
                SoundholeDiameter = new(TopPlate.Definitions.SoundholeDiameter, diameter),
                SoundholeOffsetFromTailEnd = new(TopPlate.Definitions.SoundholeOffsetFromTailEnd, offset),
            },
        };

        [Test]
        public void SoundholeWithinBodyLessDiameter()
        {
            Assert.That(Messages(WithSoundhole(100, 499)), Is.Empty);
            Assert.That(Messages(WithSoundhole(100, 500)), Is.EqualTo(new[]
            {
                "Soundhole Offset from Tail End (SHOT) must be less than Body Length (BDL) minus Soundhole Diameter " +
                "(SHD) (500 mm).",
            }));
        }

        [Test]
        public void SoundholeWithoutDiameter_MustBeOnBody()
        {
            Assert.That(Messages(WithSoundhole(null, 600)), Does.Contain(
                "Soundhole Offset from Tail End (SHOT) must be less than Body Length (BDL)."));
        }

        [Test]
        public void Soundhole_RequiresDiameterAndOffset()
        {
            var plate = new TopPlate { Soundhole = new(TopPlate.Definitions.Soundhole, true) };

            Assert.That(Messages(plate), Is.EqualTo(new[]
            {
                "Soundhole Diameter (SHD) is required when Soundhole (SH) is Yes.",
                "Soundhole Offset from Tail End (SHOT) is required when Soundhole (SH) is Yes.",
            }));
            Assert.That(Messages(new TopPlate()), Is.Empty);
        }

        [Test]
        public void Arching_RequiresItsValues()
        {
            var spherical = new BackPlate { ArchingType = new(BackPlate.Definitions.ArchingType, ArchingType.Spherical) };
            var custom = new TopPlate { ArchingType = new(TopPlate.Definitions.ArchingType, ArchingType.Custom) };

            Assert.That(Messages(spherical), Is.EqualTo(new[]
            {
                "Back Spherical Radius (BKSR) is required when Back Arching Type (BKAT) is Spherical.",
            }));
            Assert.That(Messages(custom), Is.EqualTo(new[]
            {
                "Top Arching Height at Bridge (TPAB) is required when Top Arching Type (TPAT) is Custom.",
                "Top Arching Offset from Edge (TPAO) is required when Top Arching Type (TPAT) is Custom.",
            }));
        }
    }
}
