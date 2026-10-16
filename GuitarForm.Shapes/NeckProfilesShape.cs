using System;
using System.Collections.Generic;
using System.Linq;
using GuitarForm.Model.Parameters;
using GuitarForm.Model.Parts;
using GeometryMessage = GuitarForm.Model.GeometryMessage;
using MessageLevel = GuitarForm.Model.MessageLevel;

namespace GuitarForm.Shapes
{
    // One cross-section through the neck or the heel, drawn fretboard (or heel/body joint) side up with the treble
    // side to the right.
    public sealed class NeckProfileShape
    {
        // Where it's taken, as messages name it, e.g. "the first fret".
        public string Place { get; }

        // Clockwise from the top's bass corner: the fretboard's playing surface (or the heel/body joint), the treble
        // side, the bottom arc and the bass side.
        public Path Outline { get; }

        // The fretboard's underside, where it's glued to the neck, from the bass side to the treble side. Null for the
        // heel.
        public LineSegment FretboardUnderside { get; }

        internal NeckProfileShape(string place, Path outline, LineSegment fretboardUnderside)
        {
            Place = place;
            Outline = outline;
            FretboardUnderside = fretboardUnderside;
        }

        internal NeckProfileShape Moved(Vector2 by) =>
            new NeckProfileShape(Place, Outline.Moved(by), (LineSegment)FretboardUnderside?.Moved(by));
    }

    // The neck's cross-sections, beside the side view: at the first fret and the Neck Thickness Fret, and through the
    // heel at the heel section and the heel end.
    //
    // A neck profile's top is the fretboard's playing surface, an arc of the Fretboard Radius across the fretboard's
    // width at that fret, and its fretboard is as thick at the centerline as the taper from the Fretboard Thickness at
    // Nut to the Fretboard Thickness at End makes it there. A heel profile's top is the heel/body joint, a straight
    // line as wide as the heel, with the heel's straight sides running down from it by the Profile Side Length.
    //
    // Below that, the profile runs from the shoulder start (the fretboard edge's top or underside, per Profile Includes
    // Fretboard, or the end of the heel's straight side) down to the bottom, the neck thickness below the crown (the
    // heel thickness below the joint). Each half is a superellipse quadrant, vertical at the shoulder and level at the
    // apex: with half-width a, height b and squareness n, x = a cos^(2/n) t and y = b (1 - sin^(2/n) t), so n = 1 is a
    // V, 2 a round C and larger values squarer. Asymmetry moves the apex off the centerline by a percent of the
    // half-width, toward the treble side when positive. In advanced mode the squareness is ignored and the blend mixes
    // the reference shapes n = 1 (0%), n = 2 (50%) and n = 3.2 (100%) point for point.
    //
    // The neck profiles share a centerline, the drawing gap right of the side view (and the fretboard's side profile),
    // each centered on the height of its fret on the playing surface. The heel section and then the heel end follow,
    // each the gap further right, level with the heel: the joint at the top and the front of the heel on the body's
    // neck end.
    public sealed class NeckProfilesShape
    {
        public NeckProfileShape FirstFret { get; private set; }
        public NeckProfileShape SelectedFret { get; private set; }
        public NeckProfileShape HeelSection { get; private set; }
        public NeckProfileShape HeelEnd { get; private set; }

        // The heel's width at the heel section, typed in or, with Heel Outline Straight Sides, calculated: on the line
        // from the heel end's width to the fretboard's width at the body's neck end, at the fretboard's underside.
        // Null when it can't be worked out.
        public double? HeelSectionWidth { get; private set; }

        // The profiles that are drawn, in order from the nut.
        public IEnumerable<NeckProfileShape> All =>
            new[] { FirstFret, SelectedFret, HeelSection, HeelEnd }.Where(profile => profile != null);

        public List<GeometryMessage> Messages { get; } = new List<GeometryMessage>();

        // A profile's settings, after Profile Uses First Fret is applied.
        sealed record Settings(double Squareness, double Asymmetry, double Blend, bool Advanced,
            bool IncludesFretboard = false);

        readonly Guitar _guitar;
        readonly Neck _neck;
        readonly Fretboard _fretboard;
        readonly double _tolerance;
        readonly LengthUnit _unit;
        readonly IFormatProvider _culture;

        NeckProfilesShape(Guitar guitar, double tolerance, LengthUnit unit, IFormatProvider culture)
        {
            _guitar = guitar;
            _neck = guitar.Neck;
            _fretboard = _neck.Fretboard;
            _tolerance = tolerance;
            _unit = unit;
            _culture = culture;
        }

        // The profiles placed beside the side view, starting at left. fretboardSide places the neck profiles at their
        // frets; when it isn't drawn, they're placed at their frets' plan positions. Lengths in messages are shown in
        // unit.
        public static NeckProfilesShape Solve(Guitar guitar, FretboardSideShape fretboardSide, double left, double gap,
            double tolerance, LengthUnit unit = LengthUnit.Millimeters, IFormatProvider culture = null)
        {
            var shape = new NeckProfilesShape(guitar, tolerance, unit, culture);
            var neck = guitar.Neck;
            var first = shape.SettingsFor(neck.SquarenessAtFirstFret, neck.AsymmetryAtFirstFret, neck.BlendAtFirstFret,
                neck.IncludesFretboardAtFirstFret.Value);

            var firstFret = shape.NeckProfile("the first fret", 1, neck.ThicknessAtFirstFret, first);
            NeckProfileShape selectedFret = null;
            if (neck.ThicknessFret.Value is int fret)
                selectedFret = shape.NeckProfile("the selected fret", fret, neck.ThicknessAtSelectedFret,
                    neck.UsesFirstFretAtSelectedFret.Value
                        ? first
                        : shape.SettingsFor(neck.SquarenessAtSelectedFret, neck.AsymmetryAtSelectedFret,
                            neck.BlendAtSelectedFret, neck.IncludesFretboardAtSelectedFret.Value));
            else
                shape.RemarkMissing("the neck profile at the selected fret", neck.ThicknessFret);

            var heelSection = shape.HeelProfile("the heel section", neck.HeelSectionThickness,
                shape.HeelSectionWidthNeeds(), shape.FindHeelSectionWidth, neck.SideLengthAtHeelSection,
                neck.UsesFirstFretAtHeelSection.Value
                    ? first
                    : shape.SettingsFor(neck.SquarenessAtHeelSection, neck.AsymmetryAtHeelSection,
                        neck.BlendAtHeelSection, false));
            var heelEnd = shape.HeelProfile("the heel end", neck.HeelEndThickness, new[] { neck.HeelEndWidth },
                () => neck.HeelEndWidth.Value, neck.SideLengthAtHeelEnd, neck.UsesFirstFretAtHeelEnd.Value
                    ? first
                    : shape.SettingsFor(neck.SquarenessAtHeelEnd, neck.AsymmetryAtHeelEnd, neck.BlendAtHeelEnd, false));

            // The neck profiles share a centerline, placed so the widest reaches left.
            var neckProfiles = new[] { (firstFret, 1), (selectedFret, neck.ThicknessFret.Value ?? 0) }
                .Where(p => p.Item1 != null).ToList();
            if (neckProfiles.Count > 0)
            {
                double center = left - neckProfiles.Min(p => p.Item1.Outline.Bounds.MinX);
                foreach (var (profile, at) in neckProfiles)
                {
                    var bounds = profile.Outline.Bounds;
                    double y = shape.FretHeight(at, fretboardSide) - (bounds.MinY + bounds.MaxY) / 2;
                    var moved = profile.Moved(new Vector2(center, y));
                    if (profile == firstFret)
                        shape.FirstFret = moved;
                    else
                        shape.SelectedFret = moved;
                }
                left = center + neckProfiles.Max(p => p.Item1.Outline.Bounds.MaxX) + gap;
            }
            // A heel profile's joint is at y = 0 and its front at its lowest point.
            double neckEnd = guitar.Body.Length.Value ?? 0;
            if (heelSection != null)
            {
                var bounds = heelSection.Outline.Bounds;
                shape.HeelSection = heelSection.Moved(new Vector2(left - bounds.MinX, neckEnd - bounds.MinY));
                left += bounds.MaxX - bounds.MinX + gap;
            }
            if (heelEnd != null)
            {
                var bounds = heelEnd.Outline.Bounds;
                shape.HeelEnd = heelEnd.Moved(new Vector2(left - bounds.MinX, neckEnd - bounds.MinY));
            }
            return shape;
        }

        // The fret's height on the playing surface in the side view, or its plan position when that isn't drawn.
        double FretHeight(int fret, FretboardSideShape fretboardSide)
        {
            double fromNut = _guitar.FretDistance(fret).Value;
            return fretboardSide?.Outline != null
                ? fretboardSide.PlayingSurfaceAt(fromNut).Y
                : _guitar.Body.Length.Value.Value + _guitar.NeckJoinDistance.Value - fromNut;
        }

        Settings SettingsFor(NumberParameter squareness, NumberParameter asymmetry, NumberParameter blend,
            bool includesFretboard) =>
            new Settings(_neck.SquarenessOrDefault(squareness), _neck.AsymmetryOrDefault(asymmetry),
                _neck.BlendOrDefault(blend), _neck.ProfileAdvancedMode.Value, includesFretboard);

        // The neck profile at the given fret, with the crown of the playing surface at the origin. Null when it can't
        // be drawn.
        NeckProfileShape NeckProfile(string place, int fret, NumberParameter thickness, Settings settings)
        {
            var needed = FretboardShape.TaperParameters(_guitar).Concat(new Parameter[]
            {
                _fretboard.Radius, _fretboard.ThicknessAtNut, _fretboard.ThicknessAtEnd, _fretboard.FretCount,
                _fretboard.LastFretOverhang, thickness,
            }).ToArray();
            if (RemarkMissing($"the neck profile at {place}", needed))
                return null;
            // The model reports values out of range, and a fret past the last.
            if (!(_fretboard.FretCount.Value is int fretCount && fret <= fretCount && _guitar.ScaleLength.Value > 0 &&
                  _fretboard.ThicknessAtNut.Value > 0 && _fretboard.ThicknessAtEnd.Value > 0 && Valid(settings)) ||
                FretboardShape.HalfWidthAtFret(_guitar, fret) is not double half || !(half > 0))
                return null;

            double radius = _fretboard.Radius.Value.Value;
            if (radius < half)
            {
                Error($"At {place}, the {_fretboard.Radius.Label} must be at least half the fretboard's width there " +
                      $"({Format(half)}).");
                return null;
            }
            double fromNut = _guitar.FretDistance(fret).Value;
            double length = _guitar.FretDistance(fretCount).Value + _fretboard.LastFretOverhang.Value.Value;
            double nutThickness = _fretboard.ThicknessAtNut.Value.Value;
            double boardThickness = nutThickness + (_fretboard.ThicknessAtEnd.Value.Value - nutThickness) * fromNut / length;
            double depth = thickness.Value.Value;
            if (depth <= boardThickness)
            {
                Error($"At {place}, the {thickness.Label} must be more than the fretboard's thickness there " +
                      $"({Format(boardThickness)}).");
                return null;
            }

            var surface = new Circle(new Point2(0, -radius), radius);
            double edgeTop = -(radius - Math.Sqrt(radius * radius - half * half));
            var trebleTop = new Point2(half, edgeTop);
            var bassTop = new Point2(-half, edgeTop);
            if (settings.IncludesFretboard && edgeTop <= -depth)
            {
                Error($"At {place}, the {thickness.Label} must be more than the drop of the fretboard's surface at its " +
                      $"edge ({Format(-edgeTop)}), because the profile includes the fretboard.");
                return null;
            }
            var trebleUnder = new Point2(half, -boardThickness);
            var bassUnder = new Point2(-half, -boardThickness);

            var sides = Sides(half, settings.IncludesFretboard ? edgeTop : -boardThickness, -depth, settings);
            var segments = new List<Segment>();
            segments.AddIfDrawn(ArcSegment.Around(surface, bassTop, trebleTop, clockwise: true, _tolerance));
            if (!settings.IncludesFretboard)
                segments.Add(new LineSegment(trebleTop, trebleUnder));
            segments.AddRange(sides);
            if (!settings.IncludesFretboard)
                segments.Add(new LineSegment(bassUnder, bassTop));

            // The underside runs between where it meets the sides.
            var crossings = sides.SelectMany(side => side.CrossingsAtY(-boardThickness)).Select(c => c.Point.X).ToList();
            var underside = settings.IncludesFretboard && crossings.Count > 0
                ? new LineSegment(new Point2(crossings.Min(), -boardThickness), new Point2(crossings.Max(), -boardThickness))
                : new LineSegment(bassUnder, trebleUnder);
            return new NeckProfileShape(place, new Path(segments, isClosed: true), underside);
        }

        // The heel profile with the heel/body joint along y = 0, as wide as findWidth says once the values it needs are
        // set. Null when it can't be drawn.
        NeckProfileShape HeelProfile(string place, NumberParameter thickness, IEnumerable<Parameter> widthNeeds,
            Func<double?> findWidth, NumberParameter sideLength, Settings settings)
        {
            var needed = new Parameter[] { thickness };
            if (RemarkMissing($"the heel profile at {place}", needed.Concat(widthNeeds).ToArray()) ||
                findWidth() is not double width)
                return null;
            // The model reports values out of range, and straight sides as long as the heel is thick.
            double side = sideLength.Value ?? 0;
            if (!(width > 0 && thickness.Value > 0 && side < thickness.Value && Valid(settings)))
                return null;

            double half = width / 2;
            var trebleJoint = new Point2(half, 0);
            var bassJoint = new Point2(-half, 0);
            var start = new Point2(half, -side);
            var segments = new List<Segment> { new LineSegment(bassJoint, trebleJoint) };
            if (side > 0)
                segments.Add(new LineSegment(trebleJoint, start));
            segments.AddRange(Sides(half, -side, -thickness.Value.Value, settings));
            if (side > 0)
                segments.Add(new LineSegment(start.MirrorX(), bassJoint));
            return new NeckProfileShape(place, new Path(segments, isClosed: true), null);
        }

        // The values the heel section's width needs: the Heel Section Width, or with Heel Outline Straight Sides, what
        // the straight sides are worked out from.
        IEnumerable<Parameter> HeelSectionWidthNeeds()
        {
            if (!_neck.HeelOutlineStraightSides.Value)
                return new[] { _neck.HeelSectionWidth };
            var needed = FretboardShape.TaperParameters(_guitar).Concat(new Parameter[]
            {
                _guitar.Body.DepthAtNeckEnd, _neck.Overstand, _neck.HeelEndOffsetFromBack,
                _neck.HeelSectionOffsetFromHeelEnd, _neck.HeelEndWidth,
            });
            return _neck.HeelCap.Value ? needed.Append(_neck.HeelCapThickness) : needed;
        }

        // The Heel Section Width, or with Heel Outline Straight Sides, the width the straight sides give it. Null when
        // it can't be worked out.
        double? FindHeelSectionWidth()
        {
            if (!_neck.HeelOutlineStraightSides.Value)
                return HeelSectionWidth = _neck.HeelSectionWidth.Value;
            if (FretboardShape.TrebleEdgeAtNeckEnd(_guitar) is not double edge)
                return null;

            var body = _guitar.Body;

            // From the back, along the body's neck end.
            double heelEnd = _neck.HeelEndOffsetFromBack.Value.Value +
                (_neck.HeelCap.Value ? _neck.HeelCapThickness.Value.Value : 0);
            double underside = body.DepthAtNeckEnd.Value.Value + _neck.Overstand.Value.Value;
            double span = underside - heelEnd;
            double offset = _neck.HeelSectionOffsetFromHeelEnd.Value.Value;
            if (offset >= span)
            {
                Error($"The heel section must be between the heel end and the fretboard: the " +
                      $"{_neck.HeelSectionOffsetFromHeelEnd.Label} must be less than {Format(span)}.");
                return null;
            }
            double endWidth = _neck.HeelEndWidth.Value.Value;
            return HeelSectionWidth = endWidth + (2 * edge - endWidth) * offset / span;
        }

        // The model reports values out of range; these would only make the maths meaningless.
        static bool Valid(Settings settings) =>
            settings.Asymmetry is >= Neck.MinimumAsymmetry and <= Neck.MaximumAsymmetry &&
            (settings.Advanced ? settings.Blend is >= 0 and <= 100 : settings.Squareness > 0);

        // The reference squareness values the blend mixes between, at 0%, 50% and 100%.
        static readonly double[] BlendReferences = { 1, 2, 3.2 };

        // A point of the unit quadrant at angle t (0 at the shoulder, pi/2 at the apex): across from the apex as a
        // fraction of the half's width, and up from the bottom as a fraction of the height.
        static (double X, double Y) Quadrant(Settings settings, double t)
        {
            double cos = Math.Max(Math.Cos(t), 0), sin = Math.Max(Math.Sin(t), 0);
            if (!settings.Advanced)
                return Superellipse(settings.Squareness, cos, sin);
            int lower = settings.Blend < 50 ? 0 : 1;
            double weight = (settings.Blend - 50 * lower) / 50;
            var a = Superellipse(BlendReferences[lower], cos, sin);
            var b = Superellipse(BlendReferences[lower + 1], cos, sin);
            return ((1 - weight) * a.X + weight * b.X, (1 - weight) * a.Y + weight * b.Y);
        }

        static (double X, double Y) Superellipse(double n, double cos, double sin) =>
            (Math.Pow(cos, 2 / n), 1 - Math.Pow(sin, 2 / n));

        // The two halves of the profile below the shoulder start, clockwise from the treble shoulder start through the
        // apex to the bass shoulder start. Half is the half-width, shoulder the height of the shoulder start and bottom
        // the lowest point.
        List<Segment> Sides(double half, double shoulder, double bottom, Settings settings)
        {
            double apex = half * settings.Asymmetry / 100;
            double height = shoulder - bottom;
            var apexPoint = new Point2(apex, bottom);
            var treble = Sample(settings, half - apex, 1, apex, bottom, height, new Point2(half, shoulder), apexPoint);
            var bass = Sample(settings, half + apex, -1, apex, bottom, height, new Point2(-half, shoulder), apexPoint);
            bass.Reverse();
            return new List<Segment> { new PolylineSegment(treble), new PolylineSegment(bass) };
        }

        // Points along one half from its shoulder start to the apex (direction is 1 for treble, -1 for bass, and
        // width the half's width from the apex). The angle is split in eight and each piece subdivided until its chord
        // is within half the tolerance of the curve, down to a depth that bounds the count.
        List<Point2> Sample(Settings settings, double width, int direction, double apexX, double bottom, double height,
            Point2 first, Point2 last)
        {
            Point2 At(double t)
            {
                var (x, y) = Quadrant(settings, t);
                return new Point2(apexX + direction * width * x, bottom + height * y);
            }

            const int Initial = 8, MaxDepth = 6;
            double allowed = Math.Max(_tolerance, 1e-6) / 2;
            var points = new List<Point2> { first };

            void Refine(double t0, Point2 p0, double t1, Point2 p1, int depth)
            {
                double mid = (t0 + t1) / 2;
                var pm = At(mid);
                if (depth < MaxDepth &&
                    (DistanceToChord(pm, p0, p1) > allowed ||
                     DistanceToChord(At((t0 + mid) / 2), p0, p1) > allowed ||
                     DistanceToChord(At((mid + t1) / 2), p0, p1) > allowed))
                {
                    Refine(t0, p0, mid, pm, depth + 1);
                    Refine(mid, pm, t1, p1, depth + 1);
                }
                else
                    points.Add(p1);
            }

            var previous = first;
            for (int i = 1; i <= Initial; i++)
            {
                var next = i == Initial ? last : At(Math.PI / 2 * i / Initial);
                Refine(Math.PI / 2 * (i - 1) / Initial, previous, Math.PI / 2 * i / Initial, next, 0);
                previous = next;
            }
            return points;
        }

        static double DistanceToChord(Point2 p, Point2 a, Point2 b)
        {
            var ab = b - a;
            double lengthSquared = ab.Dot(ab);
            double along = lengthSquared == 0 ? 0 : Math.Clamp((p - a).Dot(ab) / lengthSquared, 0, 1);
            return p.DistanceTo(a + ab * along);
        }

        // Adds a remark naming the parameters that aren't set. True when any isn't.
        bool RemarkMissing(string part, params Parameter[] parameters)
        {
            var missing = parameters.Where(p => !p.HasValue).Select(p => p.Label).Distinct().ToList();
            if (missing.Count > 0)
                Messages.Add(new GeometryMessage(MessageLevel.Remark, $"Set the {FretboardShape.Join(missing)} to draw {part}."));
            return missing.Count > 0;
        }

        void Error(string text) => Messages.Add(new GeometryMessage(MessageLevel.Error, text));

        string Format(double millimeters) =>
            new NumberParameter(Body.Definitions.Length, millimeters).FormatWithUnit(_unit, _culture);
    }

    static class SegmentListExtensions
    {
        // Adds the segment unless it's null: too short to draw.
        public static void AddIfDrawn(this List<Segment> segments, Segment segment)
        {
            if (segment != null)
                segments.Add(segment);
        }
    }
}
