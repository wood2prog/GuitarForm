using System;
using System.Collections.Generic;
using System.Linq;
using GuitarForm.Model.Parameters;
using GuitarForm.Model.Parts;
using GeometryMessage = GuitarForm.Model.GeometryMessage;
using MessageLevel = GuitarForm.Model.MessageLevel;

namespace GuitarForm.Shapes
{
    // The fretboard seen from above, in place over the plate. The neck join fret is at the body's neck end, the nut is
    // that fret's distance beyond it (+Y), and the saddle is the scale length from the nut (without compensation). The
    // fretboard is centered on the centerline: Nut Width wide at the nut, and as wide as the String Spacing at Bridge
    // plus both setbacks at the saddle, with straight edges between. Its body end is square to the centerline, Last
    // Fret Overhang past the last fret. Frets are at equal-tempered positions; the strings run from the nut to the
    // saddle.
    public sealed class FretboardShape
    {
        // The fretboard's outline, clockwise from the bass end of the nut. Null until the values it needs are set.
        public Path Outline { get; private set; }

        // A line across the fretboard at each fret's position, from the first fret on.
        public List<LineSegment> FretLines { get; } = new List<LineSegment>();

        // Each fret slot, the chosen fretwire's Tang Thickness wide and centered on its fret line.
        public List<Path> Slots { get; } = new List<Path>();

        // The face dots, halfway between each marker's fret and the fret before it.
        public List<Circle> Dots { get; } = new List<Circle>();

        // Each string's centerline from the nut to the saddle, treble first. Only the outer strings without a string set.
        public List<LineSegment> Strings { get; } = new List<LineSegment>();

        public List<GeometryMessage> Messages { get; } = new List<GeometryMessage>();

        public double NutY { get; private set; }
        public double SaddleY { get; private set; }

        readonly Guitar _guitar;
        readonly Fretboard _fretboard;
        double _nutHalf, _saddleHalf;

        FretboardShape(Guitar guitar)
        {
            _guitar = guitar;
            _fretboard = guitar.Neck.Fretboard;
        }

        // The distance from the centerline to the fretboard's treble edge at the body's neck end. Null when a value it
        // needs isn't set.
        public static double? TrebleEdgeAtNeckEnd(Guitar guitar)
        {
            var shape = new FretboardShape(guitar);
            return shape.FindTaper() && guitar.Body.Length.Value is double length ? shape.HalfWidthAt(length) : null;
        }

        // Half the fretboard's width at the given fret. Null when a value it needs isn't set.
        public static double? HalfWidthAtFret(Guitar guitar, int fret)
        {
            var shape = new FretboardShape(guitar);
            return shape.FindTaper() ? shape.HalfWidthAt(shape.FretY(fret)) : null;
        }

        // The values the fretboard's width depends on.
        internal static Parameter[] TaperParameters(Guitar guitar)
        {
            var fretboard = guitar.Neck.Fretboard;
            return new Parameter[]
            {
                guitar.Body.Length, guitar.ScaleLength, guitar.NeckJoinFret, fretboard.NutWidth,
                fretboard.StringSpacingAtBridge, fretboard.BassStringSetback, fretboard.TrebleStringSetback,
            };
        }

        public static FretboardShape Solve(Guitar guitar)
        {
            var shape = new FretboardShape(guitar);
            shape.Build();
            return shape;
        }

        void Build()
        {
            if (!FindTaper())
            {
                RemarkMissing("the fretboard", TaperParameters(_guitar));
                return;
            }
            AddStrings();

            if (!(_fretboard.FretCount.Value is int fretCount && fretCount >= 1))
            {
                RemarkMissing("the frets", _fretboard.FretCount);
                return;
            }
            double? tang = _guitar.Neck.Fretwire?.TangThickness.Value;
            for (int fret = 1; fret <= fretCount; fret++)
            {
                double y = FretY(fret);
                FretLines.Add(Across(y));
                if (tang > 0)
                    Slots.Add(Rectangle(y + tang.Value / 2, y - tang.Value / 2));
            }
            AddDots(fretCount);

            if (!(_fretboard.LastFretOverhang.Value is double overhang && overhang >= 0))
            {
                RemarkMissing("the fretboard's outline", _fretboard.LastFretOverhang);
                return;
            }
            Outline = Rectangle(NutY, FretY(fretCount) - overhang);
        }

        // Where the nut and saddle are, and the fretboard's half-widths there. False when a value it needs isn't set.
        bool FindTaper()
        {
            if (!(_guitar.Body.Length.Value is double length && _guitar.ScaleLength.Value is double scale && scale > 0 &&
                  _guitar.NeckJoinDistance is double join && _fretboard.NutWidth.Value is double nutWidth &&
                  _fretboard.StringSpacingAtBridge.Value is double spacing &&
                  _fretboard.BassStringSetback.Value is double bass && _fretboard.TrebleStringSetback.Value is double treble))
                return false;
            NutY = length + join;
            SaddleY = NutY - scale;
            _nutHalf = nutWidth / 2;
            _saddleHalf = (spacing + bass + treble) / 2;
            return true;
        }

        double HalfWidthAt(double y) => _nutHalf + (_saddleHalf - _nutHalf) * (NutY - y) / (NutY - SaddleY);

        double FretY(int fret) => NutY - _guitar.FretDistance(fret).Value;

        LineSegment Across(double y) => new LineSegment(new Point2(-HalfWidthAt(y), y), new Point2(HalfWidthAt(y), y));

        // The fretboard between two places along it, edge to edge, clockwise from the bass edge at the top.
        Path Rectangle(double top, double bottom)
        {
            var corners = new[]
            {
                new Point2(-HalfWidthAt(top), top), new Point2(HalfWidthAt(top), top),
                new Point2(HalfWidthAt(bottom), bottom), new Point2(-HalfWidthAt(bottom), bottom),
            };
            return new Path(corners.Select((corner, i) => (Segment)new LineSegment(corner, corners[(i + 1) % 4])), isClosed: true);
        }

        // A single dot on the centerline; a pair, each Double Dot Edge Distance in from its edge.
        void AddDots(int fretCount)
        {
            var markers = _fretboard.Markers.Where(m => m.Fret.Value is int fret && fret <= fretCount).ToList();
            if (markers.Count == 0)
                return;
            if (!(_fretboard.FaceDotDiameter.Value is double diameter && diameter > 0))
            {
                RemarkMissing("the face dots", _fretboard.FaceDotDiameter);
                return;
            }
            double radius = diameter / 2;
            foreach (var marker in markers)
            {
                int fret = marker.Fret.Value.Value;
                double y = ((fret == 1 ? NutY : FretY(fret - 1)) + FretY(fret)) / 2;
                if (marker.DotCount.Value != 2)
                {
                    Dots.Add(new Circle(new Point2(0, y), radius));
                    continue;
                }
                if (!(_fretboard.DoubleDotEdgeDistance.Value is double edge))
                {
                    RemarkMissing("double face dots", _fretboard.DoubleDotEdgeDistance);
                    continue;
                }
                double x = HalfWidthAt(y) - edge - radius;
                Dots.Add(new Circle(new Point2(x, y), radius));
                Dots.Add(new Circle(new Point2(-x, y), radius));
            }
        }

        // The outer strings are the setbacks in from the fretboard's edges at the nut and half the String Spacing at
        // Bridge from the centerline at the saddle. With a string set, the strings between are spaced so the gaps between
        // neighboring strings' edges are equal, at the nut and at the saddle. For courses of two, each course's strings
        // are the Course String Spacing apart, center to center, and the equal gaps are between courses.
        void AddStrings()
        {
            double nutTreble = _nutHalf - _fretboard.TrebleStringSetback.Value.Value;
            double nutBass = -_nutHalf + _fretboard.BassStringSetback.Value.Value;
            double saddleHalf = _fretboard.StringSpacingAtBridge.Value.Value / 2;

            var nut = StringPositions(nutTreble, nutBass, _fretboard.CourseStringSpacingAtNut.Value);
            var saddle = StringPositions(saddleHalf, -saddleHalf, _fretboard.CourseStringSpacingAtBridge.Value);
            if (nut == null || saddle == null)
            {
                nut = new[] { nutTreble, nutBass };
                saddle = new[] { saddleHalf, -saddleHalf };
            }
            for (int i = 0; i < nut.Length; i++)
                Strings.Add(new LineSegment(new Point2(nut[i], NutY), new Point2(saddle[i], SaddleY)));
        }

        // The strings' centers across the fretboard, treble (+X) first, or null when they can't be worked out: no
        // string set, a string without a diameter, or courses that don't divide the set.
        double[] StringPositions(double treble, double bass, double? courseSpacing)
        {
            var strings = _guitar.Neck.StringSet?.Strings;
            int perCourse = _fretboard.StringsPerCourse.Value ?? 1;
            if (strings == null || strings.Count < 2 || strings.Any(s => !(s.Diameter.Value > 0)) ||
                strings.Count % perCourse != 0 || perCourse == 2 && !(courseSpacing > 0))
                return null;

            var diameters = strings.Select(s => s.Diameter.Value.Value).ToArray();
            // A step from one string to the next is either within a course (the course spacing) or between courses (an
            // equal gap plus the two half-diameters).
            bool WithinCourse(int i) => perCourse == 2 && i % 2 == 0;
            double fixedSpan = 0;
            int gaps = 0;
            for (int i = 0; i < diameters.Length - 1; i++)
            {
                if (WithinCourse(i))
                    fixedSpan += courseSpacing.Value;
                else
                {
                    fixedSpan += (diameters[i] + diameters[i + 1]) / 2;
                    gaps++;
                }
            }
            double gap = gaps == 0 ? 0 : (treble - bass - fixedSpan) / gaps;
            if (gap < 0)
            {
                Messages.Add(new GeometryMessage(MessageLevel.Warning,
                    "The strings overlap: there isn't room for them between the setbacks or across the String Spacing at Bridge."));
                return null;
            }

            var positions = new double[diameters.Length];
            positions[0] = treble;
            for (int i = 1; i < diameters.Length; i++)
                positions[i] = positions[i - 1] -
                    (WithinCourse(i - 1) ? courseSpacing.Value : (diameters[i - 1] + diameters[i]) / 2 + gap);
            return positions;
        }

        void RemarkMissing(string part, params Parameter[] parameters)
        {
            var missing = parameters.Where(p => !p.HasValue).Select(p => p.Label).ToList();
            if (missing.Count > 0)
                Messages.Add(new GeometryMessage(MessageLevel.Remark, $"Set the {Join(missing)} to draw {part}."));
        }

        internal static string Join(IReadOnlyList<string> items) =>
            items.Count <= 1 ? string.Join("", items) : string.Join(", ", items.Take(items.Count - 1)) + " and " + items[^1];
    }
}
