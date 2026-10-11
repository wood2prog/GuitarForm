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
    // Below that, each side is a shoulder line running down and in at the Profile Shoulder Angle (from a flat line
    // across), from the fretboard edge's top or underside (Profile Includes Fretboard) or from the end of the heel's
    // straight side. The bottom arc's lowest point is the neck thickness below the crown (the heel thickness below the
    // joint), on the Profile Center Offset (the heel has none). A fillet of the Profile Midshoulder Radius blends each
    // shoulder line into the bottom arc where the line leaves the arc's circle; with a center offset each side gets its
    // own fillet of the same radius.
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
        sealed record Settings(NumberParameter BottomRadius, NumberParameter ShoulderAngle,
            NumberParameter MidshoulderRadius, NumberParameter CenterOffset = null, bool IncludesFretboard = false);

        enum Failure { None, Misses, PastStart, PastBottom, NoFit }

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
            var first = new Settings(neck.BottomRadiusAtFirstFret, neck.ShoulderAngleAtFirstFret,
                neck.MidshoulderRadiusAtFirstFret, neck.CenterOffsetAtFirstFret, neck.IncludesFretboardAtFirstFret.Value);

            var firstFret = shape.NeckProfile("the first fret", 1, neck.ThicknessAtFirstFret, first);
            NeckProfileShape selectedFret = null;
            if (neck.ThicknessFret.Value is int fret)
                selectedFret = shape.NeckProfile("the selected fret", fret, neck.ThicknessAtSelectedFret,
                    neck.UsesFirstFretAtSelectedFret.Value
                        ? first
                        : new Settings(neck.BottomRadiusAtSelectedFret, neck.ShoulderAngleAtSelectedFret,
                            neck.MidshoulderRadiusAtSelectedFret, neck.CenterOffsetAtSelectedFret,
                            neck.IncludesFretboardAtSelectedFret.Value));
            else
                shape.RemarkMissing("the neck profile at the selected fret", neck.ThicknessFret);

            var heelSection = shape.HeelProfile("the heel section", neck.HeelSectionThickness,
                shape.HeelSectionWidthNeeds(), shape.FindHeelSectionWidth, neck.SideLengthAtHeelSection, neck.UsesFirstFretAtHeelSection.Value
                    ? first
                    : new Settings(neck.BottomRadiusAtHeelSection, neck.ShoulderAngleAtHeelSection,
                        neck.MidshoulderRadiusAtHeelSection));
            var heelEnd = shape.HeelProfile("the heel end", neck.HeelEndThickness, new[] { neck.HeelEndWidth },
                () => neck.HeelEndWidth.Value, neck.SideLengthAtHeelEnd, neck.UsesFirstFretAtHeelEnd.Value
                    ? first
                    : new Settings(neck.BottomRadiusAtHeelEnd, neck.ShoulderAngleAtHeelEnd, neck.MidshoulderRadiusAtHeelEnd));

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

        // The neck profile at the given fret, with the crown of the playing surface at the origin. Null when it can't
        // be drawn.
        NeckProfileShape NeckProfile(string place, int fret, NumberParameter thickness, Settings settings)
        {
            var needed = FretboardShape.TaperParameters(_guitar).Concat(new Parameter[]
            {
                _fretboard.Radius, _fretboard.ThicknessAtNut, _fretboard.ThicknessAtEnd, _fretboard.FretCount,
                _fretboard.LastFretOverhang, thickness, settings.BottomRadius, settings.ShoulderAngle,
                settings.MidshoulderRadius,
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
            var trebleUnder = new Point2(half, -boardThickness);
            var bassUnder = new Point2(-half, -boardThickness);
            var start = settings.IncludesFretboard ? trebleTop : trebleUnder;
            var bottom = Bottom(settings, depth, settings.CenterOffset.Value ?? 0);

            var sides = Sides(start, start.MirrorX(), settings, bottom, out var failure);
            if (sides == null)
            {
                ReportFailure(place, failure, settings, "the fretboard edge", "the bottom of the neck");
                return null;
            }
            var segments = new List<Segment>();
            segments.AddIfDrawn(ArcSegment.Around(surface, bassTop, trebleTop, clockwise: true, _tolerance));
            if (!settings.IncludesFretboard)
                segments.Add(new LineSegment(trebleTop, trebleUnder));
            segments.AddRange(sides);
            if (!settings.IncludesFretboard)
                segments.Add(new LineSegment(bassUnder, bassTop));

            // The underside runs between where it meets the sides.
            var crossings = sides.SelectMany(side => side.CrossingsAtY(-boardThickness)).Select(c => c.Point.X).ToList();
            var underside = settings.IncludesFretboard
                ? new LineSegment(new Point2(crossings.Min(), -boardThickness), new Point2(crossings.Max(), -boardThickness))
                : new LineSegment(bassUnder, trebleUnder);
            return new NeckProfileShape(place, new Path(segments, isClosed: true), underside);
        }

        // The heel profile with the heel/body joint along y = 0, as wide as findWidth says once the values it needs are
        // set. Null when it can't be drawn.
        NeckProfileShape HeelProfile(string place, NumberParameter thickness, IEnumerable<Parameter> widthNeeds,
            Func<double?> findWidth, NumberParameter sideLength, Settings settings)
        {
            var needed = new Parameter[] { thickness, settings.BottomRadius, settings.ShoulderAngle, settings.MidshoulderRadius };
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
            var sides = Sides(start, start.MirrorX(), settings, Bottom(settings, thickness.Value.Value, 0), out var failure);
            if (sides == null)
            {
                ReportFailure(place, failure, settings,
                    side > 0 ? "the heel's straight sides" : "the heel/body joint", "the front of the heel");
                return null;
            }
            var segments = new List<Segment> { new LineSegment(bassJoint, trebleJoint) };
            if (side > 0)
                segments.Add(new LineSegment(trebleJoint, start));
            segments.AddRange(sides);
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

        static bool Valid(Settings settings) =>
            settings.BottomRadius.Value is double bottom && bottom > 0 &&
            settings.MidshoulderRadius.Value is double midshoulder && midshoulder > 0 && midshoulder < bottom &&
            settings.ShoulderAngle.Value is > 0 and <= 90;

        // The bottom arc's circle, with its lowest point depth below y = 0, on the offset.
        static Circle Bottom(Settings settings, double depth, double offset)
        {
            double radius = settings.BottomRadius.Value.Value;
            return new Circle(new Point2(offset, radius - depth), radius);
        }

        // Clockwise from the treble shoulder's start to the bass shoulder's start: the treble shoulder line and fillet,
        // the bottom arc and the bass fillet and shoulder line. Null when they can't be drawn, with the reason.
        List<Segment> Sides(Point2 trebleStart, Point2 bassStart, Settings settings, Circle bottom, out Failure failure)
        {
            var treble = Blend(trebleStart, 1, settings, bottom, out failure);
            if (treble == null)
                return null;
            var bass = Blend(bassStart, -1, settings, bottom, out failure);
            if (bass == null)
                return null;

            var segments = new List<Segment>();
            segments.AddIfDrawn(Line(trebleStart, treble.Value.OnLine));
            segments.AddIfDrawn(ArcSegment.Around(treble.Value.Fillet, treble.Value.OnLine, treble.Value.OnBottom, true, _tolerance));
            segments.AddIfDrawn(ArcSegment.Around(bottom, treble.Value.OnBottom, bass.Value.OnBottom, true, _tolerance));
            segments.AddIfDrawn(ArcSegment.Around(bass.Value.Fillet, bass.Value.OnBottom, bass.Value.OnLine, true, _tolerance));
            segments.AddIfDrawn(Line(bass.Value.OnLine, bassStart));
            return segments;
        }

        // One side's fillet between its shoulder line and the bottom arc (side is 1 for treble, -1 for bass): where
        // it's tangent to each, or null with the reason it doesn't fit.
        (Circle Fillet, Point2 OnLine, Point2 OnBottom)? Blend(Point2 start, int side, Settings settings, Circle bottom,
            out Failure failure)
        {
            double angle = settings.ShoulderAngle.Value.Value * Math.PI / 180;
            double radius = settings.MidshoulderRadius.Value.Value;
            // Down the shoulder line, and square to it into the neck.
            var down = new Vector2(-side * Math.Cos(angle), -Math.Sin(angle));
            var inward = side > 0 ? down.Right : down.Left;

            if (Math.Abs((bottom.Center - start).Dot(inward)) >= bottom.Radius)
            {
                failure = Failure.Misses;
                return null;
            }
            // The fillet's center is the radius in from the line and the radius in from the bottom circle. Of the two
            // places, the lower is where the line leaves the circle.
            var q = start + inward * radius - bottom.Center;
            double b = q.Dot(down);
            double discriminant = b * b - (q.Dot(q) - Math.Pow(bottom.Radius - radius, 2));
            if (discriminant < 0)
            {
                failure = Failure.NoFit;
                return null;
            }
            double along = -b + Math.Sqrt(discriminant);
            if (along < -_tolerance)
            {
                failure = Failure.PastStart;
                return null;
            }
            var onLine = start + down * Math.Max(along, 0);
            var center = onLine + inward * radius;
            var onBottom = bottom.Center + (center - bottom.Center).Unit * bottom.Radius;
            if (side * (onBottom.X - bottom.Center.X) < -_tolerance)
            {
                failure = Failure.PastBottom;
                return null;
            }
            failure = Failure.None;
            return (new Circle(center, radius), onLine, onBottom);
        }

        void ReportFailure(string place, Failure failure, Settings settings, string start, string bottom)
        {
            var midshoulder = settings.MidshoulderRadius.Label;
            Error(failure switch
            {
                Failure.Misses => $"At {place}, the shoulder line doesn't meet the arc of the {settings.BottomRadius.Label}.",
                Failure.PastStart => $"At {place}, the {midshoulder} is too large: it runs past {start}.",
                Failure.PastBottom => $"At {place}, the {midshoulder} is too large: it runs past {bottom}.",
                _ => $"At {place}, the {midshoulder} is too large to fit between the shoulder line and the bottom arc.",
            });
        }

        LineSegment Line(Point2 from, Point2 to) => from.DistanceTo(to) > _tolerance ? new LineSegment(from, to) : null;

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
