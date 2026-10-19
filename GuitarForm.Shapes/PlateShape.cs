using System;
using System.Collections.Generic;
using System.Linq;
using GuitarForm.Model.Parameters;
using GuitarForm.Model.Parts;
using GeometryMessage = GuitarForm.Model.GeometryMessage;
using MessageLevel = GuitarForm.Model.MessageLevel;

namespace GuitarForm.Shapes
{
    // The body's plate outline, with the tail end at the origin and the body length up +Y (see docs/Body.md). The
    // treble side (+X) is worked out first, top to bottom, as a chain of slots: the heel flat, shoulder arc, upper bout
    // primary and secondary arcs, upper waist tangent line, waist arc, lower waist tangent line, lower bout secondary
    // and primary arcs, and the tail. The bass side is its mirror. A slot whose values aren't all set is left empty,
    // so the outline comes out in pieces until it's complete, and then as one closed path, clockwise from the top of
    // the centerline. A cutaway then replaces part of the treble side.
    //
    // Values that Body.Validate reports as errors stop the plate (Failed) without a message of their own, so they
    // aren't reported twice. The plate reports only what the model can't see.
    public sealed class PlateShape
    {
        // Length of the marks at each end of the heel flat, as a fraction of the body length, so they're visible at any
        // scale. They're centered on the neck end.
        public const double HeelMarkFraction = 0.01;

        public List<LineSegment> ConstructionLines { get; } = new List<LineSegment>();
        public List<Circle> Radii { get; } = new List<Circle>();

        // The outline's pieces: open paths while values are missing, or one closed path.
        public List<Path> Outline { get; } = new List<Path>();

        // The whole outline, or null until it's complete.
        public Path ClosedOutline => Outline.Count == 1 && Outline[0].IsClosed ? Outline[0] : null;

        public List<GeometryMessage> Messages { get; } = new List<GeometryMessage>();

        // True when an error stopped the plate. Nothing of it should be drawn.
        public bool Failed { get; private set; }

        const int HeelFlat = 0, Shoulder = 1, UpperPrimary = 2, UpperSecondary = 3, UpperTangent = 4, Waist = 5,
            LowerTangent = 6, LowerSecondary = 7, LowerPrimary = 8, Tail = 9, SlotCount = 10;

        readonly Guitar _guitar;
        readonly Body _body;
        readonly double _tolerance;
        readonly ShapeMessages _messages;

        // The treble side's slots, top to bottom. Null when a value it needs isn't set; empty when it's known but has
        // nothing to draw (e.g. no shoulder arc when the upper bout circle reaches the neck end).
        readonly Segment[][] _slots = new Segment[SlotCount][];

        double _length;

        // Treble-side circles, where the outline joins the upper bout primary (from the heel) and leaves the lower bout
        // primary (to the tail), and the waist tangent lines, each from the bout circle to the waist circle.
        Circle? _ubPrimary, _ubSecondary, _waist, _lbPrimary, _lbSecondary;
        Point2? _ubOutlineStart, _lbOutlineEnd;
        LineSegment _ubTangent, _lbTangent;

        PlateShape(Guitar guitar, double tolerance, LengthUnit unit, IFormatProvider culture)
        {
            _guitar = guitar;
            _body = guitar.Body;
            _tolerance = tolerance;
            _messages = new ShapeMessages(Messages, unit, culture);
        }

        // Lengths in messages are shown in unit.
        public static PlateShape Solve(Guitar guitar, double tolerance, LengthUnit unit = LengthUnit.Millimeters,
            IFormatProvider culture = null)
        {
            var plate = new PlateShape(guitar, tolerance, unit, culture);
            plate.Failed = !plate.Build();
            if (plate.Failed)
            {
                plate.ConstructionLines.Clear();
                plate.Radii.Clear();
                plate.Outline.Clear();
            }
            return plate;
        }

        bool Build()
        {
            if (!(_body.Length.Value is double length && length > 0))
                return false;
            _length = length;
            ConstructionLines.Add(new LineSegment(Point2.Origin, new Point2(0, length)));

            if (!(AddUpperBout() && AddWaist() && AddLowerBout() && FindWaistTangents() && AddHeel()))
                return false;
            AddCircleSegments();

            var treble = _slots.ToList();
            var bass = Enumerable.Reverse(_slots)
                .Select(slot => slot == null ? null : Enumerable.Reverse(slot).Select(segment => segment.MirrorX().Reversed()).ToArray())
                .ToList();
            if (_body.CutawayType.Value != CutawayType.None && !AddCutaway(treble))
                return false;

            AssembleOutline(treble.Concat(bass).ToList());
            return true;
        }

        // Upper bout: a line across the widest point, the primary radius's center one radius below the neck end, moved
        // down by the offset.
        bool AddUpperBout()
        {
            var width = _body.UpperBoutWidth; var offset = _body.UpperBoutOffsetFromNeckEnd; var radius = _body.UpperBoutPrimaryRadius;
            if (!AllSet("the upper bout", width, offset, radius))
                return true;
            if (!(width.Value > 0 && offset.Value >= 0 && radius.Value > 0))
                return false;

            double half = width.Value.Value / 2, r = radius.Value.Value;
            double y = _length - r - offset.Value.Value;
            if (y < 0)
                _messages.Warning($"The upper bout's widest point is past the tail end: {offset.Label} and {radius.Label} " +
                        $"together are more than the {_body.Length.Label}.");
            ConstructionLines.Add(Across(half, y));

            // Without a heel flat the model doesn't check this.
            if (r > half && !_body.HeelFlatWidth.HasValue)
                _messages.Warning($"{radius.Label} is more than half the {width.Label}.");
            _ubPrimary = AddRadii(half - r, y, r);
            return AddSecondary(_body.UpperBoutSecondaryOffset, _ubPrimary.Value, out _ubSecondary);
        }

        // Waist: a line across the narrowest point, with the waist radius's center one radius outside it.
        bool AddWaist()
        {
            var width = _body.WaistWidth; var offset = _body.WaistOffsetFromTailEnd; var radius = _body.WaistRadius;
            if (!AllSet("the waist", width, offset))
                return true;
            if (!(width.Value > 0 && offset.Value >= 0))
                return false;

            double half = width.Value.Value / 2, y = offset.Value.Value;
            if (y > _length)
                _messages.Warning($"The waist is past the neck end: {offset.Label} is more than the {_body.Length.Label}.");
            ConstructionLines.Add(Across(half, y));

            if (!radius.HasValue)
                return true;
            if (!(radius.Value > 0))
                return false;
            _waist = AddRadii(half + radius.Value.Value, y, radius.Value.Value);
            return true;
        }

        // Lower bout: a line across the widest point, the primary radius's center one radius above the tail end, moved
        // up by the offset.
        bool AddLowerBout()
        {
            var width = _body.LowerBoutWidth; var offset = _body.LowerBoutOffsetFromTailEnd; var radius = _body.LowerBoutPrimaryRadius;
            if (!AllSet("the lower bout", width, offset, radius))
                return true;
            if (!(width.Value > 0 && offset.Value >= 0 && radius.Value > 0))
                return false;

            double half = width.Value.Value / 2, r = radius.Value.Value, o = offset.Value.Value;
            double y = r + o;
            if (y > _length)
                _messages.Warning($"The lower bout's widest point is past the neck end: {offset.Label} and {radius.Label} " +
                        $"together are more than the {_body.Length.Label}.");
            ConstructionLines.Add(Across(half, y));

            if (r > half)
                _messages.Warning($"{radius.Label} is more than half the {width.Label}.");
            _lbPrimary = AddRadii(half - r, y, r);
            return AddSecondary(_body.LowerBoutSecondaryOffset, _lbPrimary.Value, out _lbSecondary) &&
                   AddTail(_lbPrimary.Value, o);
        }

        // Tail: when the lower bout circles sit on the tail end, a straight line between their bottoms. When they're
        // offset up, one arc through the origin, tangent to both.
        bool AddTail(Circle primary, double offset)
        {
            double cx = primary.Center.X;
            if (offset <= _tolerance)
            {
                _lbOutlineEnd = new Point2(cx, 0);
                _slots[Tail] = Slot(cx > _tolerance ? new LineSegment(new Point2(cx, 0), Point2.Origin) : null);
                return true;
            }

            // The arc leaves the origin sideways, so its center is on the centerline above it, and it wraps the circle,
            // touching it from outside: |arcCenter - circleCenter| = arcRadius - radius. It meets the circle below its
            // center only while cx is at least the offset; the model reports it otherwise.
            if (cx < offset)
                return false;
            double cy = primary.Center.Y;
            double arcRadius = (cx * cx + cy * cy - primary.Radius * primary.Radius) / (2 * offset);
            var tail = new Circle(new Point2(0, arcRadius), arcRadius);
            var touch = primary.Center + (primary.Center - tail.Center).Unit * primary.Radius;

            _lbOutlineEnd = touch;
            _slots[Tail] = Slot(ArcSegment.Around(tail, touch, Point2.Origin, clockwise: true, _tolerance));
            return true;
        }

        // The straight lines from each bout's secondary circle to the waist circle. The model reports overlapping
        // circles.
        bool FindWaistTangents()
        {
            if (!_waist.HasValue)
                return true;
            foreach (var bout in new[] { _ubSecondary, _lbSecondary })
                if (bout.HasValue && Gap(bout.Value, _waist.Value) < -_tolerance)
                    return false;
            if (_ubSecondary.HasValue)
                _ubTangent = WaistTangent(_ubSecondary.Value, _waist.Value);
            if (_lbSecondary.HasValue)
                _lbTangent = WaistTangent(_lbSecondary.Value, _waist.Value);
            return true;
        }

        // Heel flat: a straight line across the neck end, Heel Flat Width long. When the upper bout circles reach the
        // neck end it runs out to their centers. When they sit below it, a shoulder arc joins each end of the flat
        // (tangent to it) to the upper bout circle. Short marks show the heel flat's ends.
        bool AddHeel()
        {
            var heelFlat = _body.HeelFlatWidth;
            if (!heelFlat.HasValue)
                return true;
            if (!(heelFlat.Value > 0))
                return false;

            double heelHalf = heelFlat.Value.Value / 2, flatHalf = heelHalf;
            if (_ubPrimary is Circle ub)
            {
                double ubOffset = _body.UpperBoutOffsetFromNeckEnd.Value.Value;
                if (ub.Center.X < heelHalf)
                    return false;

                if (ubOffset <= _tolerance)
                {
                    flatHalf = ub.Center.X;
                    _ubOutlineStart = new Point2(ub.Center.X, _length);
                    _slots[Shoulder] = Slot(null);
                }
                else
                {
                    // The arc leaves the flat's end heading outward, so its center is straight below that point, and it
                    // wraps the circle, touching it from outside. It meets the circle above its center only while the
                    // gap to the center is at least the offset; the model reports it otherwise.
                    double dx = ub.Center.X - heelHalf;
                    if (dx < ubOffset)
                        return false;
                    double t = _length - ub.Center.Y;
                    double arcRadius = (dx * dx + t * t - ub.Radius * ub.Radius) / (2 * (t - ub.Radius));
                    var shoulder = new Circle(new Point2(heelHalf, _length - arcRadius), arcRadius);
                    var touch = ub.Center + (ub.Center - shoulder.Center).Unit * ub.Radius;

                    _slots[Shoulder] = Slot(ArcSegment.Around(shoulder, new Point2(heelHalf, _length), touch,
                        clockwise: true, _tolerance));
                    _ubOutlineStart = touch;
                }
            }

            _slots[HeelFlat] = Slot(new LineSegment(new Point2(0, _length), new Point2(flatHalf, _length)));

            double markHalf = HeelMarkFraction * _length / 2;
            foreach (double x in new[] { heelHalf, -heelHalf })
                ConstructionLines.Add(new LineSegment(new Point2(x, _length - markHalf), new Point2(x, _length + markHalf)));
            return true;
        }

        // The arcs of the bout and waist circles, each between two points where the outline meets that circle, and the
        // waist tangent lines. Clockwise follows the outside of a bout circle down; counterclockwise follows the inner
        // side of the waist circle.
        void AddCircleSegments()
        {
            if (_ubPrimary is Circle ubPrimary)
            {
                var widest = ubPrimary.Center + Vector2.XAxis * ubPrimary.Radius;
                if (_ubOutlineStart.HasValue)
                    _slots[UpperPrimary] = Slot(ArcSegment.Around(ubPrimary, _ubOutlineStart.Value, widest, true, _tolerance));
                if (_ubTangent != null)
                    _slots[UpperSecondary] = Slot(ArcSegment.Around(_ubSecondary.Value, widest, _ubTangent.From, true, _tolerance));
            }
            if (_ubTangent != null)
                _slots[UpperTangent] = Slot(_ubTangent.Length > _tolerance ? _ubTangent : null);
            if (_waist.HasValue && _ubTangent != null && _lbTangent != null)
                _slots[Waist] = Slot(ArcSegment.Around(_waist.Value, _ubTangent.To, _lbTangent.To, false, _tolerance));
            if (_lbTangent != null)
                _slots[LowerTangent] = Slot(_lbTangent.Length > _tolerance ? _lbTangent.Reversed() : null);
            if (_lbPrimary is Circle lbPrimary)
            {
                var widest = lbPrimary.Center + Vector2.XAxis * lbPrimary.Radius;
                if (_lbTangent != null)
                    _slots[LowerSecondary] = Slot(ArcSegment.Around(_lbSecondary.Value, _lbTangent.From, widest, true, _tolerance));
                if (_lbOutlineEnd.HasValue)
                    _slots[LowerPrimary] = Slot(ArcSegment.Around(lbPrimary, widest, _lbOutlineEnd.Value, true, _tolerance));
            }
        }

        // The cutaway replaces the treble side between the wall's top and the cutaway's end point. The wall runs down
        // the offset line, the Cutaway Offset from Neck past the fretboard's treble edge, from where it meets the heel
        // flat, shoulder arc or upper bout above its widest point, to the Cutaway Depth below the neck end. The end
        // point is where the Cutaway Width line meets the upper bout above its widest point (toward the neck). Above the
        // widest point the treble side only moves outward, so each line meets it once, the end point after the wall. A
        // Florentine cutaway is one arc from the wall to the end point; a Venetian one is a valley arc, a straight line
        // and a point arc tangent to the outline at the end point.
        bool AddCutaway(List<Segment[]> treble)
        {
            var type = _body.CutawayType.Value;
            var offset = _body.CutawayOffsetFromNeck; var width = _body.CutawayWidth; var depth = _body.CutawayDepth;
            switch (CheckCutawayValues(type))
            {
                case CutawayValues.NotSet: return true;
                case CutawayValues.Invalid: return false;
            }
            if (!CutawayFretboardEdge(treble, out double edge))
                return true;

            double wallX = edge + offset.Value.Value, endX = wallX + width.Value.Value;
            double arcStartY = _length - depth.Value.Value;
            var segments = treble.SelectMany((slot, index) => slot.Select(segment => (segment, index))).ToList();

            if (!FindCutawayJoins(segments, wallX, endX, arcStartY, out var wallTop, out var end))
                return false;
            var endPoint = end.Point;
            var arcStart = new Point2(wallX, arcStartY);

            var cut = type == CutawayType.Florentine
                ? BuildFlorentineCut(wallX, arcStartY, arcStart, endPoint)
                : BuildVenetianCut(wallX, arcStartY, arcStart, end);
            if (cut == null)
                return false;

            ConstructionLines.Add(new LineSegment(new Point2(wallX, _length), arcStart));
            ConstructionLines.Add(new LineSegment(new Point2(endX, _length), endPoint));

            SpliceCutaway(treble, segments, wallTop, end, arcStart, cut);
            return true;
        }

        enum CutawayValues { Ready, NotSet, Invalid }

        // The model reports missing values; the outline is drawn without the cutaway until they're set.
        CutawayValues CheckCutawayValues(CutawayType type)
        {
            var offset = _body.CutawayOffsetFromNeck; var width = _body.CutawayWidth; var depth = _body.CutawayDepth;
            var valley = _body.CutawayValleyRadius; var point = _body.CutawayPointRadius;
            if (!(offset.HasValue && width.HasValue && depth.HasValue))
                return CutawayValues.NotSet;
            if (type == CutawayType.Venetian && !(valley.HasValue && point.HasValue))
                return CutawayValues.NotSet;
            if (!(offset.Value >= 0 && width.Value > 0 && depth.Value >= 0) ||
                type == CutawayType.Venetian && !(valley.Value > 0 && point.Value > 0))
                return CutawayValues.Invalid;
            return CutawayValues.Ready;
        }

        // The fretboard's treble edge at the neck end, which the wall is measured from. False, with a remark, while it
        // or the rest of the outline isn't known yet.
        bool CutawayFretboardEdge(List<Segment[]> treble, out double edge)
        {
            edge = 0;
            if (FretboardShape.TrebleEdgeAtNeckEnd(_guitar) is not double known)
            {
                var fretboard = _guitar.Neck.Fretboard;
                var needed = new Parameter[]
                {
                    _guitar.ScaleLength, _guitar.NeckJoinFret, fretboard.NutWidth, fretboard.StringSpacingAtBridge,
                    fretboard.BassStringSetback, fretboard.TrebleStringSetback,
                };
                _messages.Remark("The cutaway is drawn once the fretboard's treble edge at the body is known: set the " +
                       ShapeSupport.Join(needed.Where(p => !p.HasValue).Select(p => p.Label)) + ".");
                return false;
            }
            edge = known;
            if (treble.Any(slot => slot == null))
            {
                _messages.Remark("The cutaway is drawn once the outline is complete.");
                return false;
            }
            return true;
        }

        // Where the wall and the end point meet the outline; fails, with an error, when they don't or the arc can't start.
        bool FindCutawayJoins(List<(Segment segment, int slot)> segments, double wallX, double endX, double arcStartY,
            out OutlineCrossing wallTop, out OutlineCrossing end)
        {
            var offset = _body.CutawayOffsetFromNeck; var width = _body.CutawayWidth; var depth = _body.CutawayDepth;
            wallTop = default; end = default;

            var top = FirstCrossing(segments, wallX);
            if (top == null)
                return Fail($"The cutaway's offset line, {_messages.Format(wallX)} from the centerline, doesn't meet the neck end " +
                            $"or the upper bout above its widest point: reduce the {offset.Label}.");
            wallTop = top.Value;
            if (arcStartY > wallTop.Point.Y + _tolerance)
                return Fail($"The cutaway's arc must start below where its offset line meets the outline: the " +
                            $"{depth.Label} must be at least {_messages.Format(_length - wallTop.Point.Y)}.");

            var found = FirstCrossing(segments, endX);
            if (found == null)
                return Fail($"The cutaway's end point, {_messages.Format(endX)} from the centerline, must be on the upper bout " +
                            $"above its widest point: no more than {_messages.Format(_body.UpperBoutWidth.Value.Value / 2)} from " +
                            $"the centerline. Reduce the {width.Label} or the {offset.Label}.");
            end = found.Value;
            return true;
        }

        // Florentine: one arc tangent to the wall at the arc's start, so its center is level with it, and through the
        // end point. Null, after an error, when the arc would start at or below the end point.
        List<Segment> BuildFlorentineCut(double wallX, double arcStartY, Point2 arcStart, Point2 endPoint)
        {
            var width = _body.CutawayWidth; var depth = _body.CutawayDepth;
            double dy = arcStartY - endPoint.Y;
            if (dy <= _tolerance)
            {
                Fail($"The cutaway's arc must start above its end point: the {depth.Label} must be less " +
                     $"than {_messages.Format(_length - endPoint.Y)}.");
                return null;
            }
            double w = width.Value.Value, radius = (w * w + dy * dy) / (2 * w);
            var arc = new Circle(new Point2(wallX + radius, arcStartY), radius);
            Radii.Add(arc);
            return new List<Segment> { ArcSegment.Around(arc, arcStart, endPoint, clockwise: false, _tolerance) };
        }

        // Venetian: a valley arc, a straight line and a point arc tangent to the outline at the end point. Null, after an
        // error, when the two arcs overlap.
        List<Segment> BuildVenetianCut(double wallX, double arcStartY, Point2 arcStart, OutlineCrossing end)
        {
            var width = _body.CutawayWidth;
            var valley = _body.CutawayValleyRadius; var point = _body.CutawayPointRadius;
            var endPoint = end.Point;
            var valleyCircle = new Circle(new Point2(wallX + valley.Value.Value, arcStartY), valley.Value.Value);
            // Inside the body, the point radius away from the end point. The outline runs clockwise, so outward is
            // to the left of its direction.
            var outward = end.Segment.TangentAt(end.Parameter).Left;
            var pointCircle = new Circle(endPoint - outward * point.Value.Value, point.Value.Value);
            Radii.Add(valleyCircle);
            Radii.Add(pointCircle);

            // The valley arc turns counterclockwise and the point arc clockwise, so the line between them has the
            // valley circle on its left and the point circle on its right: the circles' crossing tangent.
            var between = pointCircle.Center - valleyCircle.Center;
            double distance = between.Length, radii = valleyCircle.Radius + pointCircle.Radius;
            if (distance <= radii + _tolerance)
            {
                Fail($"The cutaway's valley and point arcs overlap: reduce the {valley.Label} or the " +
                     $"{point.Label}, or move the end point with the {width.Label}.");
                return null;
            }
            var u = between / distance;
            double k = -radii / distance;
            var normal = u * k + u.Left * Math.Sqrt(1 - k * k);
            var lineStart = valleyCircle.Center - normal * valleyCircle.Radius;
            var lineEnd = pointCircle.Center + normal * pointCircle.Radius;

            return new List<Segment>
            {
                ArcSegment.Around(valleyCircle, arcStart, lineStart, clockwise: false, _tolerance),
                new LineSegment(lineStart, lineEnd),
                ArcSegment.Around(pointCircle, lineEnd, endPoint, clockwise: true, _tolerance),
            };
        }

        // Replaces the treble side between the wall's top and the end point with the cut.
        void SpliceCutaway(List<Segment[]> treble, List<(Segment segment, int slot)> segments, OutlineCrossing top,
            OutlineCrossing e, Point2 arcStart, List<Segment> cut)
        {
            var outline = new List<Segment>();
            outline.AddRange(segments.Take(top.Index).Select(s => s.segment));
            if (top.Parameter > 0)
                outline.Add(top.Segment.Between(0, top.Parameter));
            if (top.Point.DistanceTo(arcStart) > _tolerance)
                outline.Add(new LineSegment(top.Point, arcStart));
            outline.AddRange(cut.Where(segment => segment != null));
            if (e.Parameter < 1)
                outline.Add(e.Segment.Between(e.Parameter, 1));
            outline.AddRange(segments.Skip(e.Index + 1).Select(s => s.segment));

            treble.Clear();
            treble.Add(outline.Where(segment => segment.Length > _tolerance).ToArray());
        }

        readonly record struct OutlineCrossing(int Index, Segment Segment, Point2 Point, double Parameter);

        // The first place, going down the treble side, where the vertical line at x crosses the heel flat, shoulder arc
        // or upper bout above its widest point.
        static OutlineCrossing? FirstCrossing(List<(Segment segment, int slot)> segments, double x)
        {
            for (int i = 0; i < segments.Count && segments[i].slot <= UpperPrimary; i++)
                foreach (var crossing in segments[i].segment.CrossingsAtX(x))
                    return new OutlineCrossing(i, segments[i].segment, crossing.Point, crossing.Parameter);
            return null;
        }

        // Joins the slots, treble side then bass side, into paths. A slot that isn't known breaks the outline.
        void AssembleOutline(List<Segment[]> ring)
        {
            int firstMissing = ring.FindIndex(slot => slot == null);
            if (firstMissing < 0)
            {
                Outline.Add(new Path(ring.SelectMany(slot => slot), isClosed: true));
                return;
            }

            var run = new List<Segment>();
            for (int k = 1; k <= ring.Count; k++)
            {
                var slot = ring[(firstMissing + k) % ring.Count];
                if (slot != null)
                {
                    run.AddRange(slot);
                    continue;
                }
                if (run.Count > 0)
                    Outline.Add(new Path(run, isClosed: false));
                run = new List<Segment>();
            }
        }

        // A secondary radius shares the primary's center line, its center moved toward the centerline by the offset and
        // its radius grown by the same amount, so its outer edge stays on the primary's. Without an offset it's the
        // primary.
        bool AddSecondary(NumberParameter offset, Circle primary, out Circle? secondary)
        {
            secondary = primary;
            if (!offset.HasValue)
                return true;
            if (!(offset.Value >= 0))
                return false;
            double o = offset.Value.Value;
            secondary = AddRadii(primary.Center.X - o, primary.Center.Y, primary.Radius + o);
            return true;
        }

        // The straight line from a bout circle to the waist circle on the treble side: the crossing tangent, touching
        // the bout circle on its outer side and the waist circle on its inner side. Once the circles touch, it shrinks
        // to their contact point.
        LineSegment WaistTangent(Circle bout, Circle waist)
        {
            var toWaist = waist.Center - bout.Center;
            double distance = toWaist.Length, radii = bout.Radius + waist.Radius;
            var u = toWaist.Unit;
            if (distance - radii <= _tolerance)
            {
                var contact = bout.Center + u * bout.Radius;
                return new LineSegment(contact, contact);
            }

            // Of the two crossing tangents, the one touching the bout circle farther from the centerline.
            double angle = Math.Acos(radii / distance);
            return new[] { angle, -angle }
                .Select(turn => Vector2.AtAngle(u.Angle + turn))
                .Select(direction => new LineSegment(bout.Center + direction * bout.Radius, waist.Center - direction * waist.Radius))
                .OrderByDescending(line => line.From.X)
                .First();
        }

        // A circle on each side of the centerline at (±centerX, y); returns the treble-side one.
        Circle AddRadii(double centerX, double y, double radius)
        {
            var treble = new Circle(new Point2(centerX, y), radius);
            Radii.Add(treble);
            Radii.Add(treble.MirrorX());
            return treble;
        }

        static double Gap(Circle a, Circle b) => a.Center.DistanceTo(b.Center) - (a.Radius + b.Radius);

        static LineSegment Across(double half, double y) => new LineSegment(new Point2(-half, y), new Point2(half, y));

        static Segment[] Slot(Segment segment) => segment == null ? Array.Empty<Segment>() : new[] { segment };

        // Whether every value a part of the outline needs is set. When only some are, a remark says which it needs.
        bool AllSet(string part, params Parameter[] parameters)
        {
            if (parameters.All(p => p.HasValue))
                return true;
            if (parameters.Any(p => p.HasValue))
                _messages.Remark($"The {ShapeSupport.Join(parameters.Select(p => p.Label))} are all needed to draw {part}.");
            return false;
        }

        bool Fail(string text)
        {
            _messages.Error(text);
            return false;
        }
    }
}
