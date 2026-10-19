using System;
using System.Collections.Generic;
using System.Linq;
using GuitarForm.Model.Parameters;
using GuitarForm.Model.Parts;
using GeometryMessage = GuitarForm.Model.GeometryMessage;
using MessageLevel = GuitarForm.Model.MessageLevel;

namespace GuitarForm.Shapes
{
    // The top plate's bracing in plan view, worked out on the plate (tail end at the origin, body length up +Y, treble
    // +X) and drawn on its own copy of the plate outline and soundhole, seen from inside (see Placed).
    //
    // Every brace is a straight strip, its width across it. Where a brace runs toward the outline, its end follows the
    // outline, its edge offset inside it. Where it starts or stops at another brace, its end runs along that brace's
    // side.
    //
    // - The headblock and tailblock run from the neck and tail ends, their sides against the end following the
    //   outline.
    // - The upper transverse bar and the graft run across the guitar.
    // - The X brace's legs cross on the centerline, the X Brace Offset from Neck End from the neck end, each at the X
    //   Brace Angle to the centerline. The bar from the bass side lower leg to the treble side upper leg runs through
    //   the crossing; the other bar is cut there, its two legs ending against the first bar's sides. An upper leg that
    //   reaches the upper transverse bar before the outline ends against the bar.
    // - Finger braces run from the outer side of each lower leg toward the sides of the lower bout, and tone bars from
    //   the inner side of the treble side lower leg, inside the X, toward the tail. Each is placed by its spacing along
    //   the leg's centerline from the crossing, and at an angle of 0 runs square to the leg.
    // - The soundhole braces sit the Soundhole Brace Offset outside the soundhole: the bottom one straight across on
    //   the tail end side, and the bass and treble ones 120° around from it. Each runs both ways until it meets another
    //   brace, or the outline when it meets none.
    //
    // The bridge patch isn't drawn until the bridge's footprint is defined. Values the model reports as errors leave
    // their brace out without a message of their own.
    public sealed class BracingShape
    {
        // What a brace is, so the headblock and tailblock can be told apart from the rest.
        public enum BraceKind { Brace, Headblock, Tailblock }

        public sealed record Brace(string Name, Path Outline, BraceKind Kind = BraceKind.Brace);

        // The plate outline the bracing is drawn on; null when the outline isn't complete.
        public Path Plate { get; private set; }

        public Circle? Soundhole { get; private set; }

        public List<Brace> Braces { get; } = new List<Brace>();

        // The centerline.
        public List<LineSegment> ConstructionLines { get; } = new List<LineSegment>();

        public List<GeometryMessage> Messages { get; } = new List<GeometryMessage>();

        // True when there are no braces to draw.
        public bool IsEmpty => Braces.Count == 0;

        // A brace laid on the plate, for later braces to stop against: its centerline through Origin along Direction
        // (a unit vector), and its corners.
        sealed record Laid(Point2 Origin, Vector2 Direction, double Width, Point2[] Corners)
        {
            public Vector2 Normal => Direction.Left;

            // How far along Direction from Origin the brace reaches each way.
            public double From => Corners.Min(corner => (corner - Origin).Dot(Direction));
            public double To => Corners.Max(corner => (corner - Origin).Dot(Direction));
        }

        // Where a brace's end is: along a straight line (another brace's side, or a block's inner side), or along the
        // outline, moved inside it by an edge offset.
        abstract record Stop;
        sealed record LineStop(Point2 Point, Vector2 Direction) : Stop;
        sealed record OutlineStop(IReadOnlyList<Segment> Boundary) : Stop;

        // Where a side of a brace meets its stop: on the boundary's segment at the parameter, for an outline stop.
        readonly record struct Hit(Point2 Point, int Segment, double Parameter);

        const double Slack = ShapeSupport.BracingSlack;

        readonly LengthUnit _unit;
        readonly IFormatProvider _culture;
        readonly ShapeMessages _messages;
        readonly Dictionary<double, IReadOnlyList<Segment>> _boundaries = new Dictionary<double, IReadOnlyList<Segment>>();
        IReadOnlyList<Segment> _outline;
        bool _clockwise;

        BracingShape(LengthUnit unit, IFormatProvider culture)
        {
            _unit = unit;
            _culture = culture;
            _messages = new ShapeMessages(Messages, unit, culture);
        }

        // Lengths in messages are shown in unit.
        public static BracingShape Solve(Guitar guitar, PlateShape plate, Circle? soundhole,
            LengthUnit unit = LengthUnit.Millimeters, IFormatProvider culture = null)
        {
            var shape = new BracingShape(unit, culture);
            // The braces run to the outline, so they wait for it.
            if (plate.ClosedOutline == null)
                return shape;
            shape.Plate = plate.ClosedOutline;
            shape.Soundhole = soundhole;
            try
            {
                shape.Build(guitar);
            }
            catch (NotSupportedException e)
            {
                shape.Braces.Clear();
                shape.ConstructionLines.Clear();
                shape._messages.Error(e.Message);
            }
            return shape;
        }

        // A copy seen from inside the guitar: mirrored across the centerline, which is moved to x = centerlineX. The
        // treble side is then on the left.
        public BracingShape Placed(double centerlineX)
        {
            var by = new Vector2(centerlineX, 0);
            var placed = new BracingShape(_unit, _culture)
            {
                Plate = Plate?.MirrorX().Moved(by),
                Soundhole = Soundhole?.MirrorX().Moved(by),
            };
            placed.Braces.AddRange(Braces.Select(brace => brace with { Outline = brace.Outline.MirrorX().Moved(by) }));
            placed.ConstructionLines.AddRange(ConstructionLines.Select(line => (LineSegment)line.MirrorX().Moved(by)));
            placed.Messages.AddRange(Messages);
            return placed;
        }

        void Build(Guitar guitar)
        {
            var body = guitar.Body;
            var bracing = body.TopPlate.Bracing;
            double length = body.Length.Value.Value;
            _outline = Plate.Segments;
            _clockwise = PolygonMath.SignedArea(_outline) < 0;
            ConstructionLines.Add(new LineSegment(Point2.Origin, new Point2(0, length)));
            var laid = new List<Laid>();

            if (Ready("the headblock", body.HeadblockWidth, body.HeadblockDepth) &&
                body.HeadblockDepth.Value < length)
            {
                var inner = new Point2(0, length - body.HeadblockDepth.Value.Value);
                Add(laid, Lay("the headblock", inner, Vector2.YAxis, body.HeadblockWidth.Value.Value,
                    new LineStop(inner, Vector2.XAxis), new OutlineStop(Boundary(0)), BraceKind.Headblock));
            }
            if (Ready("the tailblock", body.TailblockWidth, body.TailblockDepth) &&
                body.TailblockDepth.Value < length)
            {
                var inner = new Point2(0, body.TailblockDepth.Value.Value);
                Add(laid, Lay("the tailblock", inner, -Vector2.YAxis, body.TailblockWidth.Value.Value,
                    new LineStop(inner, Vector2.XAxis), new OutlineStop(Boundary(0)), BraceKind.Tailblock));
            }

            Laid upperBar = null;
            if (Ready("the upper transverse bar", bracing.UpperTransverseBarOffsetFromNeckEnd, bracing.UpperTransverseBarWidth))
                upperBar = Add(laid, Across("the upper transverse bar", length, bracing.UpperTransverseBarOffsetFromNeckEnd,
                    bracing.UpperTransverseBarWidth, bracing.UpperTransverseBarEdgeOffset));
            // The model reports what a graft needs, and warns when it's left out.
            if (body.IncludesGraft && bracing.GraftOffsetFromNeckEnd.HasValue && bracing.GraftWidth.HasValue)
                Add(laid, Across("the graft", length, bracing.GraftOffsetFromNeckEnd, bracing.GraftWidth,
                    bracing.GraftEdgeOffset));

            var x = AddXBrace(bracing, length, upperBar, laid);
            if (x != null)
            {
                AddFromLowerLeg(bracing, x, treble: true, laid);
                AddFromLowerLeg(bracing, x, treble: false, laid);
                AddToneBars(bracing, x, laid);
            }
            else if (bracing.BassFingerBraceCount.Value > 0 || bracing.TrebleFingerBraceCount.Value > 0 ||
                     bracing.ToneBarCount.Value > 0)
                _messages.Remark("The finger braces and tone bars are placed on the X brace: set it to draw them.");

            AddSoundholeBraces(bracing, laid);
        }

        // A bar straight across the guitar, its edge nearest the neck end the offset from it.
        Laid Across(string name, double length, NumberParameter offset, NumberParameter width, NumberParameter edgeOffset)
        {
            if (!(offset.Value < length && width.Value > 0))
                return null;
            double y = length - offset.Value.Value - width.Value.Value / 2;
            var boundary = new OutlineStop(Boundary(edgeOffset.Value ?? 0));
            return Lay(name, new Point2(0, y), Vector2.XAxis, width.Value.Value, boundary, boundary);
        }

        // The X brace, as its two bars and the crossing, or null when it isn't drawn.
        sealed record XBrace(Point2 Crossing, double Angle, double Width, Laid Through, Laid TrebleLowerLeg);

        XBrace AddXBrace(Bracing bracing, double length, Laid upperBar, List<Laid> laid)
        {
            if (!Ready("the X brace", bracing.XBraceOffsetFromNeckEnd, bracing.XBraceAngle, bracing.XBraceWidth) ||
                !(bracing.XBraceOffsetFromNeckEnd.Value < length && bracing.XBraceAngle.Value is > 0 and < 90 &&
                  bracing.XBraceWidth.Value > 0))
                return null;
            var crossing = new Point2(0, length - bracing.XBraceOffsetFromNeckEnd.Value.Value);
            double angle = bracing.XBraceAngle.Value.Value * Math.PI / 180, width = bracing.XBraceWidth.Value.Value;
            double edgeOffset = bracing.XBraceEdgeOffset.Value ?? 0;
            var upperBars = upperBar == null ? Array.Empty<Laid>() : new[] { upperBar };

            // From the bass side lower leg through the crossing to the treble side upper leg.
            var trebleUp = new Vector2(Math.Sin(angle), Math.Cos(angle));
            var through = Lay("the X brace", crossing, trebleUp, width,
                Nearest(crossing, -trebleUp, edgeOffset), Nearest(crossing, trebleUp, edgeOffset, upperBars));
            if (through == null)
                return null;
            Add(laid, through);

            // The cut bar's legs, each from the through bar's side.
            var bassUp = new Vector2(-Math.Sin(angle), Math.Cos(angle));
            var side = through.Normal * (width / 2);
            Add(laid, Lay("the X brace's bass side upper leg", crossing, bassUp, width,
                new LineStop(crossing + side, trebleUp), Nearest(crossing, bassUp, edgeOffset, upperBars)));
            var trebleLowerLeg = Add(laid, Lay("the X brace's treble side lower leg", crossing, -bassUp, width,
                new LineStop(crossing - side, trebleUp), Nearest(crossing, -bassUp, edgeOffset)));
            return new XBrace(crossing, angle, width, through, trebleLowerLeg);
        }

        // The finger braces on one side, from the outer side of that side's lower leg.
        void AddFromLowerLeg(Bracing bracing, XBrace x, bool treble, List<Laid> laid)
        {
            var count = treble ? bracing.TrebleFingerBraceCount : bracing.BassFingerBraceCount;
            var angle = treble ? bracing.TrebleFingerBraceAngle : bracing.BassFingerBraceAngle;
            var spacing = treble ? bracing.TrebleFingerBraceSpacing : bracing.BassFingerBraceSpacing;
            // The model reports what's missing.
            if (!(count.Value > 0 && angle.Value is > -90 and < 90 && spacing.Value > 0 && bracing.FingerBraceWidth.Value > 0))
                return;
            string sideName = treble ? "treble" : "bass";
            var leg = LowerLeg(x, treble, out double legLength);
            if (leg == null)
                return;
            var outward = Outward(x.Angle, treble);
            var direction = TowardNeck(outward, angle.Value.Value);
            var legSide = new LineStop(x.Crossing + outward * (x.Width / 2), leg.Value);
            var boundary = new OutlineStop(Boundary(bracing.FingerBraceEdgeOffset.Value ?? 0));
            for (int k = 1; k <= count.Value; k++)
            {
                double along = k * spacing.Value.Value;
                string name = $"{Capitalized(sideName)} finger brace {k}";
                if (along >= legLength)
                {
                    _messages.Error($"{name} is past the end of the X brace's {sideName} side lower leg: {count.Label} times " +
                          $"{spacing.Label} must be less than the leg is long ({_messages.Format(legLength)}).");
                    return;
                }
                Add(laid, Lay(name, x.Crossing + leg.Value * along, direction, bracing.FingerBraceWidth.Value.Value,
                    legSide, boundary));
            }
        }

        // The tone bars, from the inner side of the treble side lower leg. A tone bar can't cross the bass side lower leg.
        void AddToneBars(Bracing bracing, XBrace x, List<Laid> laid)
        {
            if (!(bracing.ToneBarCount.Value > 0 && bracing.ToneBarAngle.Value is > -90 and < 90 &&
                  bracing.ToneBarSpacing.Value > 0 && bracing.ToneBarWidth.Value > 0))
                return;
            var leg = LowerLeg(x, treble: true, out double legLength);
            if (leg == null)
                return;
            var inward = -Outward(x.Angle, treble: true);
            var direction = TowardNeck(inward, bracing.ToneBarAngle.Value.Value);
            var legSide = new LineStop(x.Crossing + inward * (x.Width / 2), leg.Value);
            double edgeOffset = bracing.ToneBarEdgeOffset.Value ?? 0;
            var boundary = new OutlineStop(Boundary(edgeOffset));
            double width = bracing.ToneBarWidth.Value.Value;
            for (int k = 1; k <= bracing.ToneBarCount.Value; k++)
            {
                double along = k * bracing.ToneBarSpacing.Value.Value;
                if (along >= legLength)
                {
                    _messages.Error($"Tone bar {k} is past the end of the X brace's treble side lower leg: " +
                          $"{bracing.ToneBarCount.Label} times {bracing.ToneBarSpacing.Label} must be less than the leg " +
                          $"is long ({_messages.Format(legLength)}).");
                    return;
                }
                var origin = x.Crossing + leg.Value * along;
                if (CrossesBassLowerLeg(origin, direction, width, x, edgeOffset))
                {
                    _messages.Error($"Tone bar {k} runs into the X brace's bass side lower leg before it reaches the outline: " +
                          $"turn it toward the tail with a lower {bracing.ToneBarAngle.Label}, or move it along the " +
                          $"leg with the {bracing.ToneBarSpacing.Label}.");
                    continue;
                }
                Add(laid, Lay($"Tone bar {k}", origin, direction, width, legSide, boundary));
            }
        }

        // The direction along a lower leg from the crossing, and how long its centerline is; null when that leg wasn't
        // drawn.
        static Vector2? LowerLeg(XBrace x, bool treble, out double length)
        {
            var direction = new Vector2(treble ? Math.Sin(x.Angle) : -Math.Sin(x.Angle), -Math.Cos(x.Angle));
            var brace = treble ? x.TrebleLowerLeg : x.Through;
            length = brace == null ? 0 : treble ? brace.To : -brace.From;
            return brace == null ? null : direction;
        }

        // Square to a lower leg, away from the inside of the X: toward the side of the lower bout and the neck end.
        static Vector2 Outward(double angle, bool treble) =>
            new Vector2(treble ? Math.Cos(angle) : -Math.Cos(angle), Math.Sin(angle));

        // A direction turned toward the neck end (+Y) by an angle in degrees; a negative angle turns it toward the tail.
        static Vector2 TowardNeck(Vector2 direction, double degrees) =>
            direction.Rotated((direction.X >= 0 ? 1 : -1) * degrees * Math.PI / 180);

        bool CrossesBassLowerLeg(Point2 origin, Vector2 direction, double width, XBrace x, double edgeOffset)
        {
            var through = x.Through;
            var normal = direction.Left;
            foreach (int side in new[] { 1, -1 })
            {
                var start = origin + normal * (side * width / 2);
                var reach = HitOutline(Boundary(edgeOffset), start, direction);
                double toOutline = reach == null ? double.PositiveInfinity : (reach.Value.Point - start).Dot(direction);
                // The leg's side facing the tone bar.
                var legSide = through.Origin + through.Normal * (Math.Sign((start - through.Origin).Dot(through.Normal)) *
                                                                 through.Width / 2);
                if (ShapeSupport.Intersect(start, direction, legSide, through.Direction) is not Point2 meets)
                    continue;
                double along = (meets - through.Origin).Dot(through.Direction);
                double distance = (meets - start).Dot(direction);
                if (distance > Slack && distance < toOutline && along >= through.From && along <= 0)
                    return true;
            }
            return false;
        }

        // The soundhole braces, each running both ways to the nearest brace. They're laid once against the braces
        // already there, then again against each other as well.
        void AddSoundholeBraces(Bracing bracing, List<Laid> laid)
        {
            if (!(bracing.SoundholeBraces.Value && Soundhole is Circle hole && bracing.SoundholeBraceOffset.HasValue &&
                  bracing.SoundholeBraceWidth.Value > 0))
                return;
            double width = bracing.SoundholeBraceWidth.Value.Value;
            double fromCenter = hole.Radius + bracing.SoundholeBraceOffset.Value.Value + width / 2;
            var places = new (string Name, double Degrees)[]
            {
                ("The bottom soundhole brace", -90), ("The treble soundhole brace", 30), ("The bass soundhole brace", 150),
            };
            var strips = places.Select(place =>
            {
                var radial = Vector2.AtAngle(place.Degrees * Math.PI / 180);
                return (place.Name, Origin: hole.Center + radial * fromCenter, Direction: radial.Left);
            }).ToArray();

            var first = strips.Select(strip => LayBetween(strip.Name, strip.Origin, strip.Direction, width, laid, quiet: true))
                .ToArray();
            for (int i = 0; i < strips.Length; i++)
            {
                var others = laid.Concat(first.Where((brace, j) => j != i && brace != null)).ToList();
                var (name, origin, direction) = strips[i];
                Add(laid, LayBetween(name, origin, direction, width, others, quiet: false));
            }
        }

        Laid LayBetween(string name, Point2 origin, Vector2 direction, double width, IReadOnlyList<Laid> obstacles, bool quiet)
        {
            var start = Nearest(origin, -direction, 0, obstacles);
            var end = Nearest(origin, direction, 0, obstacles);
            return Lay(name, origin, direction, width, start, end, quiet ? null : Braces);
        }

        static Laid Add(List<Laid> laid, Laid brace)
        {
            if (brace != null)
                laid.Add(brace);
            return brace;
        }

        // Where a brace from origin going in direction stops: the nearest brace it meets, against the side facing it, or
        // else the outline, the edge offset inside it.
        Stop Nearest(Point2 origin, Vector2 direction, double edgeOffset, IEnumerable<Laid> obstacles = null)
        {
            var boundary = Boundary(edgeOffset);
            var reach = HitOutline(boundary, origin, direction);
            double best = reach == null ? double.PositiveInfinity : (reach.Value.Point - origin).Dot(direction);
            Stop stop = reach == null ? null : new OutlineStop(boundary);
            foreach (var obstacle in obstacles ?? Enumerable.Empty<Laid>())
            {
                if (ShapeSupport.Intersect(origin, direction, obstacle.Origin, obstacle.Direction) is not Point2 meets)
                    continue;
                double along = (meets - obstacle.Origin).Dot(obstacle.Direction);
                if (along < obstacle.From || along > obstacle.To)
                    continue;
                double facing = Math.Sign((origin - obstacle.Origin).Dot(obstacle.Normal));
                var side = obstacle.Origin + obstacle.Normal * (facing * obstacle.Width / 2);
                if (ShapeSupport.Intersect(origin, direction, side, obstacle.Direction) is not Point2 atSide)
                    continue;
                double distance = (atSide - origin).Dot(direction);
                if (distance > Slack && distance < best)
                {
                    best = distance;
                    stop = new LineStop(side, obstacle.Direction);
                }
            }
            return stop;
        }

        // A brace with its centerline through origin along direction, from its start stop (behind origin) to its end
        // stop (ahead of it). Its outline is added to Braces; null, with an error, when it doesn't fit.
        Laid Lay(string name, Point2 origin, Vector2 direction, double width, Stop start, Stop end,
            BraceKind kind = BraceKind.Brace) =>
            Lay(name, origin, direction, width, start, end, Braces, kind);

        Laid Lay(string name, Point2 origin, Vector2 direction, double width, Stop start, Stop end, List<Brace> into,
            BraceKind kind = BraceKind.Brace)
        {
            direction = direction.Unit;
            var normal = direction.Left;
            var starts = new Hit[2];
            var ends = new Hit[2];
            bool fits = start != null && end != null;
            for (int i = 0; fits && i < 2; i++)
            {
                var sideOrigin = origin + normal * ((i == 0 ? 1 : -1) * width / 2);
                var from = Inside(sideOrigin) ? HitStop(start, sideOrigin, -direction) : null;
                var to = from == null ? null : HitStop(end, sideOrigin, direction);
                fits = to != null && (to.Value.Point - from.Value.Point).Dot(direction) > Slack;
                if (fits)
                    (starts[i], ends[i]) = (from.Value, to.Value);
            }
            if (!fits)
            {
                if (into != null)
                    _messages.Error($"{Capitalized(name)} doesn't fit inside the outline.");
                return null;
            }

            if (into != null)
            {
                // Along the left side, across the end, back along the right side and across the start.
                var segments = new List<Segment> { new LineSegment(starts[0].Point, ends[0].Point) };
                segments.AddRange(Across(end, ends[0], ends[1]));
                segments.Add(new LineSegment(ends[1].Point, starts[1].Point));
                segments.AddRange(Across(start, starts[1], starts[0]));
                into.Add(new Brace(Capitalized(name), new Path(segments, isClosed: true), kind));
            }
            return new Laid(origin, direction, width, new[] { starts[0].Point, ends[0].Point, ends[1].Point, starts[1].Point });
        }

        // A brace's end from one side to the other: straight along a line, or along the outline the shorter way.
        static IEnumerable<Segment> Across(Stop stop, Hit from, Hit to)
        {
            if (stop is LineStop)
                return new[] { new LineSegment(from.Point, to.Point) };
            var boundary = ((OutlineStop)stop).Boundary;
            var forward = Along(boundary, from, to);
            var backward = Along(boundary, to, from);
            return forward.Sum(segment => segment.Length) <= backward.Sum(segment => segment.Length)
                ? forward
                : Enumerable.Reverse(backward).Select(segment => segment.Reversed()).ToList();
        }

        // The boundary from one hit to another, in its own direction, with any gap between its pieces bridged.
        static List<Segment> Along(IReadOnlyList<Segment> boundary, Hit from, Hit to)
        {
            var pieces = new List<Segment>();
            if (from.Segment == to.Segment && from.Parameter <= to.Parameter)
                pieces.Add(boundary[from.Segment].Between(from.Parameter, to.Parameter));
            else
            {
                pieces.Add(boundary[from.Segment].Between(from.Parameter, 1));
                for (int i = (from.Segment + 1) % boundary.Count; i != to.Segment; i = (i + 1) % boundary.Count)
                    pieces.Add(boundary[i]);
                pieces.Add(boundary[to.Segment].Between(0, to.Parameter));
            }

            var joined = new List<Segment>();
            var at = from.Point;
            foreach (var piece in pieces.Where(piece => piece.Length > Slack))
            {
                if (at.DistanceTo(piece.Start) > Slack)
                    joined.Add(new LineSegment(at, piece.Start));
                joined.Add(piece);
                at = piece.End;
            }
            if (at.DistanceTo(to.Point) > Slack)
                joined.Add(new LineSegment(at, to.Point));
            return joined;
        }

        Hit? HitStop(Stop stop, Point2 origin, Vector2 direction) => stop switch
        {
            LineStop line => ShapeSupport.Intersect(origin, direction, line.Point, line.Direction) is Point2 point
                ? new Hit(point, -1, 0)
                : null,
            OutlineStop outline => HitOutline(outline.Boundary, origin, direction),
            _ => null,
        };

        // The first place the boundary is crossed going from origin in direction.
        static Hit? HitOutline(IReadOnlyList<Segment> boundary, Point2 origin, Vector2 direction)
        {
            Hit? first = null;
            double best = double.PositiveInfinity;
            for (int i = 0; i < boundary.Count; i++)
                foreach (var crossing in boundary[i].CrossingsWithLine(origin, direction))
                {
                    double distance = (crossing.Point - origin).Dot(direction.Unit);
                    if (distance > Slack && distance < best)
                    {
                        best = distance;
                        first = new Hit(crossing.Point, i, crossing.Parameter);
                    }
                }
            return first;
        }

        // The outline moved inside itself by the offset: lines moved square to themselves, arcs' radii changed. An arc
        // the offset would shrink to nothing is left out.
        IReadOnlyList<Segment> Boundary(double offset)
        {
            if (_boundaries.TryGetValue(offset, out var cached))
                return cached;
            var boundary = PolygonMath.OffsetInward(_outline, _clockwise, offset);
            return _boundaries[offset] = boundary;
        }

        bool Inside(Point2 point) => PolygonMath.Contains(_outline, point);

        // True when every value is set. When some are, a remark says which are missing.
        bool Ready(string what, params Parameter[] values)
        {
            var missing = values.Where(p => !p.HasValue).Select(p => p.Label).ToList();
            if (missing.Count > 0 && missing.Count < values.Length)
                _messages.Remark($"Set the {ShapeSupport.Join(missing)} to draw {what}.");
            return missing.Count == 0;
        }

        static string Capitalized(string text) => char.ToUpperInvariant(text[0]) + text.Substring(1);
    }
}
