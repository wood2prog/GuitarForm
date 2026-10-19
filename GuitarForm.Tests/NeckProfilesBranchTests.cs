using System;
using System.Globalization;
using System.Linq;
using GuitarForm.Model.Parts;
using GuitarForm.Shapes;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    // Characterization tests: pin the neck profiles' guards, sampling and tolerance branches as they behave now.
    public class NeckProfilesBranchTests
    {
        const double Left = 1000;
        const double Gap = 50;

        static NeckProfilesShape Solve(Guitar guitar, double tolerance = 0.001, bool withSide = true) =>
            NeckProfilesShape.Solve(guitar,
                withSide ? FretboardSideShape.Solve(guitar, SideViewShape.Solve(guitar.Body, 0)) : null,
                Left, Gap, tolerance);

        // The point count and the sums of the coordinates, exactly.
        static string Fingerprint(NeckProfileShape profile)
        {
            if (profile == null)
                return "null";
            var points = profile.Outline.Segments.OfType<PolylineSegment>().SelectMany(s => s.Points).ToList();
            var b = profile.Outline.Bounds;
            return string.Create(CultureInfo.InvariantCulture,
                $"n={points.Count} x={points.Sum(p => p.X):R} y={points.Sum(p => p.Y):R} b={b.MinX:R},{b.MinY:R},{b.MaxX:R},{b.MaxY:R}");
        }

        static string Messages(NeckProfilesShape shape) =>
            string.Join("|", shape.Messages.Select(m => $"{m.Level}:{m.Text}"));

        static Guitar With(Func<Neck, Neck> change) => NeckProfilesShapeTests.TestGuitar(change);

        static Func<Neck, Neck> Adv(double blend) => n => n with
        {
            ProfileAdvancedMode = new(Neck.Definitions.ProfileAdvancedMode, true),
            BlendAtFirstFret = new(Neck.Definitions.BlendAtFirstFret, blend),
        };

        static Func<Neck, Neck> NoStraightSides(double? width = null) => n => n with
        {
            HeelOutlineStraightSides = new(Neck.Definitions.HeelOutlineStraightSides, false),
            HeelSectionWidth = width is double w ? new(Neck.Definitions.HeelSectionWidth, w) : n.HeelSectionWidth,
        };

        [Test]
        public void Baseline_AllFourProfiles()
        {
            var s = Solve(NeckProfilesShapeTests.TestGuitar());
            Assert.That(Fingerprint(s.FirstFret), Is.EqualTo("n=258 x=264565.03439839935 y=236376.20359109278 b=1002.9688014190327,911.0550483921297,1047.922938103444,932.0550483921297"));
            Assert.That(Fingerprint(s.SelectedFret), Is.EqualTo("n=258 x=264565.0343983996 y=178248.02929714453 b=999.9999999999999,684.8394115228269,1050.8917395224769,707.8394115228269"));
            Assert.That(Fingerprint(s.HeelSection), Is.EqualTo("n=514 x=580708.9715584478 y=317191.4298625078 b=1100.8917395224769,600,1158.6762431952209,655"));
            Assert.That(Fingerprint(s.HeelEnd), Is.EqualTo("n=514 x=636679.5890023429 y=314011.55097606883 b=1208.6762431952209,600,1268.6762431952209,640"));
            Assert.That(s.HeelSectionWidth.ToString(), Is.EqualTo("57.78450367274397"));
            Assert.That(Messages(s), Is.EqualTo(""));
        }

        [Test]
        public void Tolerance_FloorsAtTheProfileMinimum()
        {
            var g = NeckProfilesShapeTests.TestGuitar();
            Assert.That(Fingerprint(Solve(g, 1e-9).FirstFret), Is.EqualTo("n=1026 x=1052107.4623750325 y=939996.133025268 b=1002.9688014190327,911.0550483921297,1047.922938103444,932.0550483921297"));
            Assert.That(Fingerprint(Solve(g, 1e-9).FirstFret),
                Is.EqualTo(Fingerprint(Solve(g, ShapeSupport.ProfileCurveMinimumTolerance).FirstFret)));
        }

        [Test]
        public void Tolerance_CoarseAndFine()
        {
            var g = NeckProfilesShapeTests.TestGuitar();
            Assert.That(Fingerprint(Solve(g, 5).FirstFret), Is.EqualTo("n=18 x=18458.02565570229 y=16495.40118934088 b=1002.9688014190327,911.0550483921297,1047.922938103444,932.0550483921297"));
            Assert.That(Fingerprint(Solve(g, 0.05).FirstFret), Is.EqualTo("n=40 x=41017.83479044954 y=36692.37900753112 b=1002.9688014190327,911.0550483921297,1047.922938103444,932.0550483921297"));
            Assert.That(Fingerprint(Solve(g, 0.0001).HeelEnd), Is.EqualTo("n=1026 x=1270881.8255182991 y=626793.0559326786 b=1208.6762431952209,600,1268.6762431952209,640"));
        }

        [Test]
        public void Advanced_BlendLowerAndUpperHalves()
        {
            Assert.That(Fingerprint(Solve(With(Adv(25))).FirstFret), Is.EqualTo("n=274 x=280972.16831457935 y=251418.72060489067 b=1002.9688014190327,911.0550483921297,1047.922938103444,932.0550483921297"));
            Assert.That(Fingerprint(Solve(With(Adv(50))).FirstFret), Is.EqualTo("n=258 x=264565.03439839935 y=236376.20359109278 b=1002.9688014190327,911.0550483921297,1047.922938103444,932.0550483921297"));
            Assert.That(Fingerprint(Solve(With(Adv(75))).FirstFret), Is.EqualTo("n=260 x=266615.9261379219 y=238067.17641597937 b=1002.9688014190327,911.0550483921297,1047.922938103444,932.0550483921297"));
            Assert.That(Fingerprint(Solve(With(Adv(100))).FirstFret), Is.EqualTo("n=258 x=264565.0343983994 y=236058.9448257268 b=1002.9688014190327,911.0550483921297,1047.922938103444,932.0550483921297"));
            Assert.That(Fingerprint(Solve(With(Adv(0))).FirstFret), Is.EqualTo("n=18 x=18458.02565570229 y=16525.720016321804 b=1002.9688014190327,911.0550483921297,1047.922938103444,932.0550483921297"));
            Assert.That(Fingerprint(Solve(With(Adv(101))).FirstFret), Is.EqualTo("null"));
            Assert.That(Fingerprint(Solve(With(Adv(-1))).FirstFret), Is.EqualTo("null"));
        }

        [Test]
        public void InvalidSettings_DrawNothingAndSayNothing()
        {
            var squareZero = Solve(With(n => n with { SquarenessAtFirstFret = new(Neck.Definitions.SquarenessAtFirstFret, 0) }));
            var asymBig = Solve(With(n => n with { AsymmetryAtFirstFret = new(Neck.Definitions.AsymmetryAtFirstFret, 150) }));
            var asymEdge = Solve(With(n => n with { AsymmetryAtFirstFret = new(Neck.Definitions.AsymmetryAtFirstFret, 100) }));
            var asymNeg = Solve(With(n => n with { AsymmetryAtFirstFret = new(Neck.Definitions.AsymmetryAtFirstFret, -100) }));
            Assert.That(Fingerprint(squareZero.FirstFret), Is.EqualTo("null"));
            Assert.That(Messages(squareZero), Is.EqualTo(""));
            Assert.That(Fingerprint(asymBig.FirstFret), Is.EqualTo("null"));
            Assert.That(Messages(asymBig), Is.EqualTo(""));
            Assert.That(Fingerprint(asymEdge.FirstFret), Is.EqualTo("null"));
            Assert.That(Fingerprint(asymNeg.FirstFret), Is.EqualTo("null"));
        }

        [Test]
        public void FretPastTheLast_AndTheLastFret()
        {
            var past = Solve(With(n => n with { ThicknessFret = new(Neck.Definitions.ThicknessFret, 25) }));
            Assert.That(Fingerprint(past.SelectedFret), Is.EqualTo("null"));
            Assert.That(Messages(past), Is.EqualTo(""));
            var last = Solve(With(n => n with { ThicknessFret = new(Neck.Definitions.ThicknessFret, 20) }));
            Assert.That(Fingerprint(last.SelectedFret), Is.EqualTo("n=298 x=306291.15499515407 y=152545.00946805128 b=1000,504.53362428155526,1055.6453355379467,527.5336242815553"));
        }

        [Test]
        public void FretboardRadiusSmallerThanHalfTheWidth_IsError()
        {
            var g = NeckProfilesShapeTests.TestGuitar();
            g = g with { Neck = g.Neck with { Fretboard = g.Neck.Fretboard with { Radius = new(Fretboard.Definitions.Radius, 20) } } };
            var s = Solve(g);
            Assert.That(Fingerprint(s.FirstFret), Is.EqualTo("null"));
            Assert.That(Messages(s), Is.EqualTo("Error:At the first fret, the Fretboard Radius (FBR) must be at least half the fretboard's width there (22.48 mm).|Error:At the selected fret, the Fretboard Radius (FBR) must be at least half the fretboard's width there (25.45 mm)."));
        }

        [Test]
        public void ZeroFretboardThickness_DrawsNoNeckProfiles()
        {
            var g = NeckProfilesShapeTests.TestGuitar();
            g = g with { Neck = g.Neck with { Fretboard = g.Neck.Fretboard with { ThicknessAtEnd = new(Fretboard.Definitions.ThicknessAtEnd, 0) } } };
            var s = Solve(g);
            Assert.That(Fingerprint(s.FirstFret), Is.EqualTo("null"));
            Assert.That(Fingerprint(s.HeelEnd), Is.EqualTo("n=514 x=584821.2348877898 y=314011.55097606883 b=1107.784503672744,600,1167.784503672744,640"));
        }

        [Test]
        public void NoSideView_PlacesNeckProfilesAtTheirPlanPositions()
        {
            var s = Solve(NeckProfilesShapeTests.TestGuitar(), withSide: false);
            Assert.That(Fingerprint(s.FirstFret), Is.EqualTo("n=258 x=264565.03439839935 y=236357.89763641683 b=1002.9688014190327,910.9840950794328,1047.922938103444,931.9840950794328"));
            Assert.That(Fingerprint(s.SelectedFret), Is.EqualTo("n=258 x=264565.0343983996 y=178213.21128518938 b=999.9999999999999,684.7044579881181,1050.8917395224769,707.7044579881181"));
            Assert.That(Fingerprint(s.HeelSection), Is.EqualTo("n=514 x=580708.9715584478 y=317191.4298625078 b=1100.8917395224769,600,1158.6762431952209,655"));
            Assert.That(Fingerprint(s.HeelEnd), Is.EqualTo("n=514 x=636679.5890023429 y=314011.55097606883 b=1208.6762431952209,600,1268.6762431952209,640"));
        }

        [Test]
        public void HeelSides_ZeroLongOrAsLongAsTheHeel()
        {
            var zero = Solve(With(n => n with { SideLengthAtHeelEnd = new(Neck.Definitions.SideLengthAtHeelEnd, 0) }));
            var equal = Solve(With(n => n with { SideLengthAtHeelEnd = new(Neck.Definitions.SideLengthAtHeelEnd, 40) }));
            var longer = Solve(With(n => n with { SideLengthAtHeelSection = new(Neck.Definitions.SideLengthAtHeelSection, 60) }));
            var unset = Solve(With(n => n with { SideLengthAtHeelEnd = new(Neck.Definitions.SideLengthAtHeelEnd) }));
            Assert.That(Fingerprint(zero.HeelEnd), Is.EqualTo("n=514 x=636679.5890023429 y=315882.06796809204 b=1208.6762431952209,600,1268.6762431952209,640"));
            Assert.That(zero.HeelEnd.Outline.Segments.Count, Is.EqualTo(3));
            Assert.That(Fingerprint(equal.HeelEnd), Is.EqualTo("null"));
            Assert.That(Fingerprint(longer.HeelSection), Is.EqualTo("null"));
            Assert.That(Fingerprint(unset.HeelEnd), Is.EqualTo("n=514 x=636679.5890023429 y=315882.06796809204 b=1208.6762431952209,600,1268.6762431952209,640"));
            Assert.That(Messages(unset), Is.EqualTo(""));
        }

        [Test]
        public void HeelSection_WithoutStraightSides_UsesTheTypedWidth()
        {
            var s = Solve(With(NoStraightSides(58)));
            Assert.That(s.HeelSectionWidth.ToString(), Is.EqualTo("58"));
            Assert.That(Fingerprint(s.HeelSection), Is.EqualTo("n=514 x=580764.3541145533 y=317191.4298625078 b=1100.8917395224769,600,1158.8917395224769,655"));
            var missing = Solve(With(NoStraightSides()));
            Assert.That(Fingerprint(missing.HeelSection), Is.EqualTo("null"));
            Assert.That(Messages(missing), Is.EqualTo("Remark:Set the Heel Section Width (HLCW) to draw the heel profile at the heel section."));
            var nonPositive = Solve(With(NoStraightSides(0)));
            Assert.That(Fingerprint(nonPositive.HeelSection), Is.EqualTo("null"));
        }

        [Test]
        public void HeelCapOff_ChangesTheHeelSectionWidth()
        {
            var s = Solve(With(n => n with { HeelCap = new(Neck.Definitions.HeelCap, false) }));
            Assert.That(s.HeelSectionWidth.ToString(), Is.EqualTo("57.9023492220661"));
            Assert.That(Messages(s), Is.EqualTo(""));
        }

        [Test]
        public void HeelProfilesUseTheirOwnSettings_WhenNotTheFirstFrets()
        {
            var s = Solve(With(n => n with
            {
                UsesFirstFretAtHeelSection = new(Neck.Definitions.UsesFirstFretAtHeelSection, false),
                UsesFirstFretAtHeelEnd = new(Neck.Definitions.UsesFirstFretAtHeelEnd, false),
                SquarenessAtHeelSection = new(Neck.Definitions.SquarenessAtHeelSection, 3),
                AsymmetryAtHeelEnd = new(Neck.Definitions.AsymmetryAtHeelEnd, 15),
            }));
            Assert.That(Fingerprint(s.HeelSection), Is.EqualTo("n=478 x=540036.7478695298 y=292459.59120532573 b=1100.8917395224769,600,1158.6762431952209,655"));
            Assert.That(Fingerprint(s.HeelEnd), Is.EqualTo("n=468 x=579223.5215203975 y=285410.8274218175 b=1208.6762431952209,600,1268.6762431952209,640"));
        }
    }
}
