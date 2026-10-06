using System;
using System.Collections.Generic;
using Rhino.Geometry;

namespace GuitarForm.Geometry
{
    // Plate inputs. Everything except the body length is optional (null when not connected).
    public sealed record PlateDimensions
    {
        public double BodyLength { get; init; }
        public double? UbWidth { get; init; }
        public double? UbOffset { get; init; }
        public double? UbRadius { get; init; }
        public double? UbSecondaryOffset { get; init; }
        public double? WaistRadius { get; init; }
        public double? WaistWidth { get; init; }
        public double? WaistOffset { get; init; }
        public double? LbWidth { get; init; }
        public double? LbOffset { get; init; }
        public double? LbRadius { get; init; }
        public double? LbSecondaryOffset { get; init; }
        public double? HeelWidth { get; init; }
    }

    // Guitar body plate outline, drawn vertically from the tail end at the origin up +Y. Each region of the body (upper
    // bout, waist, lower bout and tail, heel) adds its construction geometry and the parts of the outline it owns; the
    // circle segments that join them are added last, once every point where the outline meets a circle is known.
    public sealed class PlateGeometry
    {
        // Length of the vertical marks at each end of the heel width, as a fraction of the body length, so they're visible
        // at any scale and in any units. They're centred on the top of the body.
        public const double HeelMarkFraction = 0.01;

        // Output geometry, in output order. Mirrored pairs are right side (+X) then left side (-X).
        public List<Line> ConstructionLines { get; } = new List<Line>();
        public List<Circle> OutlineRadii { get; } = new List<Circle>();
        public List<Arc> OutlineArcs { get; } = new List<Arc>();
        public List<Line> OutlineLines { get; } = new List<Line>();

        // The outline arcs and lines joined into one closed curve, or null until the inputs describe a complete outline.
        public Curve Outline { get; private set; }

        public List<GeometryMessage> Messages { get; } = new List<GeometryMessage>();

        // True when an error stopped the solution. The geometry is then incomplete and shouldn't be output.
        public bool Failed { get; private set; }

        readonly PlateDimensions _d;
        readonly double _tolerance;

        // Right-side circles, where the outline joins the upper primary (from the heel end) and leaves the lower primary
        // (to the tail), and the right-side waist tangent lines.
        Circle? _ubPrimary, _ubSecondary, _waist, _lbPrimary, _lbSecondary;
        Point3d? _ubOutlineStart, _lbOutlineEnd;
        Line? _ubWaistTangent, _lbWaistTangent;
        Line? _tailLine;
        Arc? _tailArc;

        PlateGeometry(PlateDimensions dimensions, double tolerance)
        {
            _d = dimensions;
            _tolerance = tolerance;
        }

        public static PlateGeometry Solve(PlateDimensions dimensions, double tolerance)
        {
            var plate = new PlateGeometry(dimensions, tolerance);
            plate.Failed = !plate.Build();
            return plate;
        }

        bool Build()
        {
            if (!RequirePositive(_d.BodyLength, "Body Length")) return false;

            // Body centerline from the origin along +Y.
            ConstructionLines.Add(new Line(Point3d.Origin, new Point3d(0, _d.BodyLength, 0)));

            if (!AddUpperBout()) return false;
            if (!AddWaist()) return false;
            if (!AddLowerBout()) return false;
            if (!FindWaistTangents()) return false;
            if (!AddHeel()) return false;

            AddWaistTangentLines();
            if (_tailLine.HasValue) OutlineLines.Add(_tailLine.Value);
            AddCircleSegments();
            if (_tailArc.HasValue) OutlineArcs.Add(_tailArc.Value);

            JoinOutline();
            return true;
        }

        // Joins the outline arcs and lines. They only form one closed curve once every outline input is connected; until
        // then the pieces join into several open curves and there's no Outline.
        void JoinOutline()
        {
            var pieces = new List<Curve>();
            foreach (var arc in OutlineArcs) pieces.Add(new ArcCurve(arc));
            foreach (var line in OutlineLines) pieces.Add(new LineCurve(line));
            if (pieces.Count == 0) return;

            var joined = Curve.JoinCurves(pieces, _tolerance);
            if (joined.Length == 1 && joined[0].IsClosed)
                Outline = joined[0];
        }

        // Upper bout: horizontal line centred on the Y axis. The primary radius centre sits one radius below the top of
        // the body; Upper Bout Offset moves the line down from there.
        bool AddUpperBout()
        {
            if (!(_d.UbWidth.HasValue && _d.UbOffset.HasValue && _d.UbRadius.HasValue))
            {
                if (_d.UbWidth.HasValue || _d.UbOffset.HasValue || _d.UbRadius.HasValue)
                    Remark("Upper bout width, offset and primary radius are all needed to draw the upper bout line.");
                return true;
            }

            double width = _d.UbWidth.Value, offset = _d.UbOffset.Value, radius = _d.UbRadius.Value;
            if (!RequirePositive(width, "Upper bout width")) return false;
            if (!RequirePositive(radius, "Upper bout primary radius")) return false;

            double length = _d.BodyLength;
            double y = length - radius - offset;
            if (y < 0 || y > length)
                Warning("Upper bout line is outside the body length.");

            double half = width / 2;
            ConstructionLines.Add(HorizontalLine(half, y));

            // Radius centre is one radius inside the width, so the circle touches the edge of the width.
            if (radius > half)
                Warning("Upper bout primary radius is larger than half the upper bout width.");
            _ubPrimary = AddMirroredCircles(half - radius, y, radius);

            return AddSecondaryCircles(_d.UbSecondaryOffset, "Upper bout", _ubPrimary.Value, out _ubSecondary);
        }

        // Waist: horizontal center line centred on the Y axis, Waist Offset up from the tail end.
        bool AddWaist()
        {
            if (!(_d.WaistWidth.HasValue && _d.WaistOffset.HasValue))
            {
                if (_d.WaistWidth.HasValue || _d.WaistOffset.HasValue)
                    Remark("Waist width and waist offset are both needed to draw the waist center line.");
                return true;
            }

            double width = _d.WaistWidth.Value, offset = _d.WaistOffset.Value;
            if (!RequirePositive(width, "Waist width")) return false;
            if (offset < 0 || offset > _d.BodyLength)
                Warning("Waist center line is outside the body length.");

            double half = width / 2;
            ConstructionLines.Add(HorizontalLine(half, offset));

            if (!_d.WaistRadius.HasValue) return true;
            double radius = _d.WaistRadius.Value;
            if (!RequirePositive(radius, "Waist radius")) return false;

            // Radius centre is one radius outside the width, so the circle touches the waist from outside the body.
            _waist = AddMirroredCircles(half + radius, offset, radius);
            return true;
        }

        // Lower bout: horizontal center line centred on the Y axis. The primary radius centre sits one radius above the
        // tail end; Lower Bout Offset moves the line up from there.
        bool AddLowerBout()
        {
            if (!(_d.LbWidth.HasValue && _d.LbOffset.HasValue && _d.LbRadius.HasValue))
            {
                if (_d.LbWidth.HasValue || _d.LbOffset.HasValue || _d.LbRadius.HasValue)
                    Remark("Lower bout width, offset and primary radius are all needed to draw the lower bout center line.");
                return true;
            }

            double width = _d.LbWidth.Value, offset = _d.LbOffset.Value, radius = _d.LbRadius.Value;
            if (!RequirePositive(width, "Lower bout width")) return false;
            if (!RequirePositive(radius, "Lower bout primary radius")) return false;

            double y = radius + offset;
            if (y < 0 || y > _d.BodyLength)
                Warning("Lower bout center line is outside the body length.");

            double half = width / 2;
            ConstructionLines.Add(HorizontalLine(half, y));

            // Radius centre is one radius inside the width, so the circle touches the edge of the width.
            if (radius > half)
                Warning("Lower bout primary radius is larger than half the lower bout width.");
            _lbPrimary = AddMirroredCircles(half - radius, y, radius);

            if (!AddSecondaryCircles(_d.LbSecondaryOffset, "Lower bout", _lbPrimary.Value, out _lbSecondary))
                return false;
            return AddTail(_lbPrimary.Value, offset);
        }

        // Tail end: when the lower bout primary circles sit on the end of the body it is a straight line between their
        // bottoms. When they are offset upward it is one arc through the origin, tangent to both circles.
        bool AddTail(Circle primary, double offset)
        {
            double centreX = primary.Center.X;
            if (offset < -_tolerance)
                return Fail("The lower bout circles reach below the end of the body. Lower bout offset can't be negative.");

            if (offset <= _tolerance)
            {
                _lbOutlineEnd = new Point3d(centreX, 0, 0);
                if (centreX > _tolerance)
                    _tailLine = new Line(new Point3d(-centreX, 0, 0), new Point3d(centreX, 0, 0));
                return true;
            }

            // The arc passes through the origin heading sideways, so its centre is on the Y axis above it. It wraps each
            // primary circle and touches it from outside: |arcCentre - circleCentre| = arcRadius - radius.
            // The tangent point sits below the lower bout line only while centreX > offset.
            if (centreX < offset)
                return Fail(
                    $"The tail arc meets the lower bout radius above its centre, so it would run wider than the lower bout width. " +
                    $"The lower bout radius centre's distance from the centerline ({centreX:0.###}) must be at least the lower bout offset ({offset:0.###}). " +
                    "Reduce the lower bout offset or the lower bout primary radius, or widen the lower bout.");

            double centreY = primary.Center.Y;
            double arcRadius = (centreX * centreX + centreY * centreY - primary.Radius * primary.Radius) / (2 * offset);

            var arcCentre = new Point3d(0, arcRadius, 0);
            var toCircle = primary.Center - arcCentre;
            toCircle.Unitize();
            var tangentPoint = primary.Center + toCircle * primary.Radius;

            _tailArc = new Arc(MirrorX(tangentPoint), Point3d.Origin, tangentPoint);
            _lbOutlineEnd = tangentPoint;
            return true;
        }

        // Waist tangent lines: final outline from each bout secondary circle to the waist circle. Found here so the bout
        // and waist outline segments can end on them; they're added to the outline after the heel flat.
        bool FindWaistTangents()
        {
            if (!_waist.HasValue) return true;

            if (_ubSecondary.HasValue &&
                !CheckSecondaryClearsWaist(_ubSecondary.Value, "Upper bout", "upper bout secondary offset"))
                return false;
            if (_lbSecondary.HasValue &&
                !CheckSecondaryClearsWaist(_lbSecondary.Value, "Lower bout", "lower bout secondary offset"))
                return false;

            if (_ubSecondary.HasValue)
                _ubWaistTangent = WaistTangentLine(_ubSecondary.Value, _waist.Value, _tolerance);
            if (_lbSecondary.HasValue)
                _lbWaistTangent = WaistTangentLine(_lbSecondary.Value, _waist.Value, _tolerance);
            return true;
        }

        // Right then left for each. A tangent that has shrunk to a point (circles touching) is not drawn as a line.
        void AddWaistTangentLines()
        {
            foreach (var tangent in new[] { _ubWaistTangent, _lbWaistTangent })
            {
                if (!tangent.HasValue || tangent.Value.Length <= _tolerance) continue;
                OutlineLines.Add(tangent.Value);
                OutlineLines.Add(MirrorX(tangent.Value));
            }
        }

        // Heel flat: horizontal line at the top of the body, Heel Width long. When the upper bout circle tops are level
        // with the top of the body, it is lengthened to the upper bout radius centres. When they sit below the top, a
        // tangent arc runs from each end of the flat (G1 with the flat) to the upper bout circle.
        // Short vertical marks show the heel width ends.
        bool AddHeel()
        {
            if (!_d.HeelWidth.HasValue) return true;
            double heelWidth = _d.HeelWidth.Value;
            if (!RequirePositive(heelWidth, "Heel width")) return false;

            double length = _d.BodyLength;
            double heelHalf = heelWidth / 2;
            double flatHalf = heelHalf;
            if (_ubPrimary.HasValue)
            {
                var ub = _ubPrimary.Value;
                double ubOffset = _d.UbOffset.Value;

                double centreSpacing = 2 * ub.Center.X;
                if (centreSpacing < heelWidth)
                    return Fail(
                        $"The upper bout radius centres are {centreSpacing:0.###} apart, which is less than the heel width ({heelWidth:0.###}). " +
                        "Reduce the upper bout primary radius, widen the upper bout, or reduce the heel width.");

                if (ubOffset < -_tolerance)
                    return Fail("The upper bout circles reach above the top of the body. Upper bout offset can't be negative when a heel width is set.");

                if (ubOffset <= _tolerance)
                {
                    // Circle tops are level with the top of the body: the flat runs out to them.
                    flatHalf = ub.Center.X;
                    _ubOutlineStart = new Point3d(ub.Center.X, length, 0);
                }
                else
                {
                    // Shoulder arc: starts at the flat end heading outward, so its centre is directly below that point.
                    // It wraps the upper bout circle and touches it from outside: |arcCentre - circleCentre| = arcRadius - radius.
                    double dx = ub.Center.X - heelHalf;
                    // The tangent point sits above the upper bout radius centre only while dx > ubOffset.
                    if (dx < ubOffset)
                        return Fail(
                            $"The shoulder arc meets the upper bout radius below its centre, so it would run wider than the upper bout width. " +
                            $"The gap from the heel flat end to the upper bout radius centre ({dx:0.###}) must be at least the upper bout offset ({ubOffset:0.###}). " +
                            "Reduce the upper bout offset or the heel width, or widen the upper bout.");

                    double t = length - ub.Center.Y; // = radius + ubOffset
                    double arcRadius = (dx * dx + t * t - ub.Radius * ub.Radius) / (2 * (t - ub.Radius));

                    var arcCentre = new Point3d(heelHalf, length - arcRadius, 0);
                    var toCircle = ub.Center - arcCentre;
                    toCircle.Unitize();
                    var tangentPoint = ub.Center + toCircle * ub.Radius;

                    OutlineArcs.Add(new Arc(new Point3d(heelHalf, length, 0), Vector3d.XAxis, tangentPoint));
                    OutlineArcs.Add(new Arc(new Point3d(-heelHalf, length, 0), -Vector3d.XAxis, MirrorX(tangentPoint)));
                    _ubOutlineStart = tangentPoint;
                }
            }

            OutlineLines.Add(HorizontalLine(flatHalf, length));

            double markHalf = HeelMarkFraction * length / 2;
            ConstructionLines.Add(new Line(new Point3d(heelHalf, length - markHalf, 0), new Point3d(heelHalf, length + markHalf, 0)));
            ConstructionLines.Add(new Line(new Point3d(-heelHalf, length - markHalf, 0), new Point3d(-heelHalf, length + markHalf, 0)));
            return true;
        }

        // Outline segments of the bout and waist circles, top to bottom, right then left for each. Each segment runs
        // between two points where the outline meets that circle, and is skipped if either point isn't known.
        void AddCircleSegments()
        {
            if (_ubPrimary.HasValue)
            {
                var ubWidest = _ubPrimary.Value.Center + Vector3d.XAxis * _ubPrimary.Value.Radius;
                if (_ubOutlineStart.HasValue)
                    AddMirroredSegment(_ubPrimary.Value, _ubOutlineStart.Value, ubWidest, clockwise: true);
                if (_ubSecondary.HasValue && _ubWaistTangent.HasValue)
                    AddMirroredSegment(_ubSecondary.Value, ubWidest, _ubWaistTangent.Value.From, clockwise: true);
            }
            if (_waist.HasValue && _ubWaistTangent.HasValue && _lbWaistTangent.HasValue)
                AddMirroredSegment(_waist.Value, _ubWaistTangent.Value.To, _lbWaistTangent.Value.To, clockwise: false);
            if (_lbPrimary.HasValue)
            {
                var lbWidest = _lbPrimary.Value.Center + Vector3d.XAxis * _lbPrimary.Value.Radius;
                if (_lbSecondary.HasValue && _lbWaistTangent.HasValue)
                    AddMirroredSegment(_lbSecondary.Value, _lbWaistTangent.Value.From, lbWidest, clockwise: true);
                if (_lbOutlineEnd.HasValue)
                    AddMirroredSegment(_lbPrimary.Value, lbWidest, _lbOutlineEnd.Value, clockwise: true);
            }
        }

        // Adds a circle on each side of the Y axis, centred at (±centreX, y), and returns the right-side one.
        Circle AddMirroredCircles(double centreX, double y, double radius)
        {
            var right = new Circle(new Point3d(centreX, y, 0), radius);
            OutlineRadii.Add(right);
            OutlineRadii.Add(new Circle(new Point3d(-centreX, y, 0), radius));
            return right;
        }

        // Secondary radius: shares the primary's centre line, centre moved toward the body centre by the offset and
        // radius grown by the same amount, so its outer edge stays on the primary's. secondary is the right-side circle,
        // or null if the offset isn't connected. Returns false if the offset is invalid.
        bool AddSecondaryCircles(double? secondaryOffset, string boutName, Circle primary, out Circle? secondary)
        {
            secondary = null;
            if (!secondaryOffset.HasValue) return true;

            double offset = secondaryOffset.Value;
            if (offset < 0)
                return Fail($"{boutName} secondary offset can't be negative.");

            secondary = AddMirroredCircles(primary.Center.X - offset, primary.Center.Y, primary.Radius + offset);
            return true;
        }

        // A secondary radius can't overlap the waist radius on the same side: the bout curve must meet the waist curve
        // tangentially, so the centres must be at least the two radii apart.
        bool CheckSecondaryClearsWaist(Circle secondary, string boutName, string inputName)
        {
            var waist = _waist.Value;
            double gap = secondary.Center.DistanceTo(waist.Center) - (secondary.Radius + waist.Radius);
            if (gap >= -_tolerance) return true;

            return Fail(
                $"The {boutName.ToLowerInvariant()} secondary radius overlaps the waist radius by {-gap:0.###}, which is an impossible shape. " +
                $"Reduce the {inputName}, or adjust the waist radius, width or offset.");
        }

        // Straight outline line from a bout secondary circle to the waist circle on the right side. It is the internal
        // (crossing) tangent: it touches the bout circle on its outer side and the waist circle on its inner side, so
        // the two circles sit on opposite sides of it. Once the circles touch, the line shrinks to their contact point
        // (From == To). The caller has already rejected overlapping circles.
        static Line WaistTangentLine(Circle bout, Circle waist, double tolerance)
        {
            Vector3d toWaist = waist.Center - bout.Center;
            double distance = toWaist.Length;
            double radiusSum = bout.Radius + waist.Radius;
            toWaist.Unitize();
            if (distance - radiusSum <= tolerance)
            {
                var contact = bout.Center + toWaist * bout.Radius;
                return new Line(contact, contact);
            }

            double angle = Math.Acos(radiusSum / distance);

            // Of the two internal tangents, take the one touching the bout circle further from the body centre.
            Line? best = null;
            foreach (double sign in new[] { 1.0, -1.0 })
            {
                var direction = toWaist;
                direction.Rotate(sign * angle, Vector3d.ZAxis);
                var boutPoint = bout.Center + direction * bout.Radius;
                var waistPoint = waist.Center - direction * waist.Radius;
                if (!best.HasValue || boutPoint.X > best.Value.From.X)
                    best = new Line(boutPoint, waistPoint);
            }
            return best.Value;
        }

        // Adds the outline segment of a right-side circle from one point on it to another, plus its mirror on the left.
        // Clockwise follows the outside of a bout circle from top to bottom; counter-clockwise follows the inner side of
        // the waist circle. Nothing is added if the segment is shorter than the tolerance (the two points coincide).
        void AddMirroredSegment(Circle circle, Point3d from, Point3d to, bool clockwise)
        {
            double startAngle = Math.Atan2(from.Y - circle.Center.Y, from.X - circle.Center.X);
            double endAngle = Math.Atan2(to.Y - circle.Center.Y, to.X - circle.Center.X);
            double sweep = clockwise ? startAngle - endAngle : endAngle - startAngle;
            sweep = ((sweep % (2 * Math.PI)) + 2 * Math.PI) % (2 * Math.PI);
            double minSweep = _tolerance / circle.Radius;
            if (sweep < minSweep || sweep > 2 * Math.PI - minSweep) return;

            double midAngle = clockwise ? startAngle - sweep / 2 : startAngle + sweep / 2;
            var mid = circle.Center + new Vector3d(Math.Cos(midAngle), Math.Sin(midAngle), 0) * circle.Radius;

            OutlineArcs.Add(new Arc(from, mid, to));
            OutlineArcs.Add(new Arc(MirrorX(from), MirrorX(mid), MirrorX(to)));
        }

        static Line HorizontalLine(double half, double y) => new Line(new Point3d(-half, y, 0), new Point3d(half, y, 0));

        static Point3d MirrorX(Point3d point) => new Point3d(-point.X, point.Y, point.Z);

        static Line MirrorX(Line line) => new Line(MirrorX(line.From), MirrorX(line.To));

        bool RequirePositive(double value, string name) =>
            value > 0 || Fail($"{name} must be greater than zero.");

        void Remark(string text) => Messages.Add(new GeometryMessage(MessageLevel.Remark, text));

        void Warning(string text) => Messages.Add(new GeometryMessage(MessageLevel.Warning, text));

        // Adds an error and returns false, so callers can write `return Fail(...)`.
        bool Fail(string text)
        {
            Messages.Add(new GeometryMessage(MessageLevel.Error, text));
            return false;
        }
    }
}
