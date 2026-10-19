using System;
using System.Collections.Generic;
using System.Linq;
using GuitarForm.Model.Parameters;
using GuitarForm.Model.Parts;
using GeometryMessage = GuitarForm.Model.GeometryMessage;
using MessageLevel = GuitarForm.Model.MessageLevel;

namespace GuitarForm.Shapes
{
    // The neck seen from the side, on the body's side view, under the fretboard's side profile, cut along the
    // centerline.
    //
    // Its top is the fretboard's underside, from the nut's far face (Nut Thickness beyond the fretboard's nut end) to
    // the body's neck end. The back of the shaft is a straight line through the points the Neck Thickness at First
    // Fret and at Selected Fret below the playing surface, each measured square to it, and the nut end is cut square
    // to the playing surface. The heel/body joint runs along the body's neck end from the top down to the heel end,
    // which runs square to it, the Heel End Offset from Back (plus the Heel Cap Thickness, with a heel cap) in from
    // the back and the Heel End Thickness long. The heel section is the Heel Section Offset from Heel End further in,
    // its front the Heel Section Thickness from the joint.
    //
    // The front of the heel is a straight line through the fronts of the heel end and the heel section when the Heel
    // Shaft Offset is 0. Above 0 it's an arc through those two points and the point where that line meets the back of
    // the neck, moved toward the nut along the back by the offset. A fillet of the Heel Shaft Radius joins the front of
    // the heel to the back of the neck, tangent to both.
    public sealed class NeckSideShape
    {
        // The whole neck, clockwise from the top at the nut end: the top, the heel/body joint, the heel end, the front of
        // the heel, the heel shaft fillet, the back of the neck and the nut end. Null until it can be drawn. It's drawn as
        // the Heel and the Shaft, not on its own.
        public Path Outline { get; private set; }

        // The heel: the joint line from the seam to the heel end, the heel end, the front, the heel shaft fillet and the
        // seam, the back of the neck carried on from the end of the fillet to the joint line. Null until it can be
        // drawn, and when the seam doesn't land on the joint line between the heel end and the top of the joint.
        public Path Heel { get; private set; }

        // The shaft: the strip from the nut to the body. The top, the joint line from the top to the seam, the seam, the
        // back of the neck and the nut end. Null when the Heel is.
        public Path Shaft { get; private set; }

        // The heel cap on the heel end, when there is one.
        public Path HeelCap { get; private set; }

        // The nut, on the neck's top between the fretboard's nut end and the neck's, Neck.NutHeight tall. Null without
        // the values it needs.
        public Path Nut { get; private set; }

        // Where the neck thicknesses are measured, the heel section, and with a Heel Shaft Offset, the straight line
        // the front of the heel's arc is drawn from.
        public List<LineSegment> ConstructionLines { get; } = new List<LineSegment>();

        public List<GeometryMessage> Messages { get; } = new List<GeometryMessage>();

        // The least half-angle at the corner of the heel's front and the neck's back that a fillet is drawn in.
        const double MinimumHalfAngle = 0.001;

        readonly double _tolerance;
        readonly LengthUnit _unit;
        readonly IFormatProvider _culture;

        NeckSideShape(double tolerance, LengthUnit unit, IFormatProvider culture)
        {
            _tolerance = tolerance;
            _unit = unit;
            _culture = culture;
        }

        // Lengths in messages are shown in unit.
        public static NeckSideShape Solve(Guitar guitar, SideViewShape side, FretboardSideShape fretboardSide,
            double tolerance, LengthUnit unit = LengthUnit.Millimeters, IFormatProvider culture = null)
        {
            var shape = new NeckSideShape(tolerance, unit, culture);
            // The fretboard's side profile says what it needs.
            if (side.Outline == null || fretboardSide.Outline == null)
                return shape;
            shape.Build(guitar, side, fretboardSide);
            return shape;
        }

        void Build(Guitar guitar, SideViewShape side, FretboardSideShape fretboardSide)
        {
            var neck = guitar.Neck;
            var needed = new List<Parameter>
            {
                neck.ThicknessAtFirstFret, neck.ThicknessAtSelectedFret, neck.ThicknessFret, neck.Nut.Thickness,
                neck.HeelEndThickness, neck.HeelEndOffsetFromBack, neck.HeelSectionThickness,
                neck.HeelSectionOffsetFromHeelEnd, neck.HeelShaftRadius,
            };
            if (neck.HeelCap.Value)
                needed.Add(neck.HeelCapThickness);
            var missing = needed.Where(p => !p.HasValue).Select(p => p.Label).ToList();
            if (missing.Count > 0)
            {
                Messages.Add(new GeometryMessage(MessageLevel.Remark,
                    $"Set the {FretboardShape.Join(missing)} to draw the neck in the side view."));
                return;
            }
            // The model reports values out of range, and a fret past the last.
            if (!(neck.ThicknessFret.Value <= neck.Fretboard.FretCount.Value && neck.ThicknessAtFirstFret.Value > 0 &&
                  neck.ThicknessAtSelectedFret.Value > 0 && neck.Nut.Thickness.Value > 0 && neck.HeelEndThickness.Value > 0 &&
                  neck.HeelSectionThickness.Value > 0 && neck.HeelShaftRadius.Value > 0))
            {
                Error("The neck can't be drawn in the side view: a thickness, the nut thickness or the heel shaft radius " +
                      "is zero or less, or the selected fret is past the last.");
                return;
            }
            int selectedFret = neck.ThicknessFret.Value.Value;
            if (selectedFret < 2)
            {
                Error($"The {neck.ThicknessFret.Label} must be past the first fret to draw the back of the neck.");
                return;
            }

            // Down the playing surface toward the body, and square to it away from the neck.
            var down = (fretboardSide.EndTop - fretboardSide.NutTop).Unit;
            var up = down.Left;
            var firstSurface = fretboardSide.PlayingSurfaceAt(guitar.FretDistance(1).Value);
            var selectedSurface = fretboardSide.PlayingSurfaceAt(guitar.FretDistance(selectedFret).Value);
            var firstBack = firstSurface - up * neck.ThicknessAtFirstFret.Value.Value;
            var selectedBack = selectedSurface - up * neck.ThicknessAtSelectedFret.Value.Value;
            ConstructionLines.Add(new LineSegment(firstSurface, firstBack));
            ConstructionLines.Add(new LineSegment(selectedSurface, selectedBack));
            // Along the back toward the nut, and square to it away from the neck, toward the guitar's back.
            var toNut = (firstBack - selectedBack).Unit;
            var outward = toNut.Left;

            // The top, from the nut's far face to the body's neck end.
            var boardToNut = (fretboardSide.NutUnder - fretboardSide.EndUnder).Unit;
            var nutEnd = fretboardSide.NutUnder + boardToNut * neck.Nut.Thickness.Value.Value;
            double neckEnd = side.BackAtNeckEnd.Y;
            var jointMeet = Intersect(fretboardSide.NutUnder, boardToNut, new Point2(0, neckEnd), Vector2.XAxis);
            var nutBackMeet = Intersect(nutEnd, up, firstBack, toNut);
            if (jointMeet == null || nutBackMeet == null)
            {
                Error(jointMeet == null
                    ? "The fretboard's underside runs along the body's neck end, so the neck can't be drawn: check the neck angle."
                    : "The back of the neck runs square to the playing surface at the nut, so the nut end can't be drawn: " +
                      "check the neck thicknesses at the first and selected frets.");
                return;
            }
            var joint = jointMeet.Value;
            var nutBack = nutBackMeet.Value;

            // The heel end and heel section, along the body's neck end from the back.
            double capOuter = side.BackAtNeckEnd.X + neck.HeelEndOffsetFromBack.Value.Value;
            double heelEndX = capOuter + (neck.HeelCap.Value ? neck.HeelCapThickness.Value.Value : 0);
            double heelEndThickness = neck.HeelEndThickness.Value.Value;
            var heelEndJoint = new Point2(heelEndX, neckEnd);
            var heelEndFront = new Point2(heelEndX, neckEnd + heelEndThickness);
            var sectionFront = new Point2(heelEndX + neck.HeelSectionOffsetFromHeelEnd.Value.Value,
                neckEnd + neck.HeelSectionThickness.Value.Value);
            ConstructionLines.Add(new LineSegment(new Point2(sectionFront.X, neckEnd), sectionFront));

            var alongFront = (sectionFront - heelEndFront).Unit;
            var meet = Intersect(heelEndFront, alongFront, firstBack, toNut);
            if (meet == null || (meet.Value - sectionFront).Dot(alongFront) <= _tolerance || meet.Value.Y <= neckEnd)
            {
                Error("The front of the heel, through the fronts of the heel end and the heel section, doesn't meet " +
                      "the back of the neck past the heel section and clear of the body.");
                return;
            }

            var shaft = neck.HeelShaftRadius;
            double radius = shaft.Value.Value;
            double offset = neck.HeelShaftOffset.Value ?? 0;
            Segment front;
            Circle fillet;
            Point2 onFront, onBack;
            Vector2 frontDirection;
            if (offset <= _tolerance)
            {
                // Between two lines: the fillet sits in the corner they make, on the bisector.
                var towardHeel = -alongFront;
                double half = Math.Acos(Math.Clamp(towardHeel.Dot(toNut), -1, 1)) / 2;
                if (half < MinimumHalfAngle || half > Math.PI / 2 - MinimumHalfAngle)
                {
                    Error($"The front of the heel and the back of the neck meet at too shallow an angle to fit the {shaft.Label}.");
                    return;
                }
                double reach = radius / Math.Tan(half);
                onFront = meet.Value + towardHeel * reach;
                onBack = meet.Value + toNut * reach;
                fillet = new Circle(meet.Value + (towardHeel + toNut).Unit * (radius / Math.Sin(half)), radius);
                if ((onFront - sectionFront).Dot(alongFront) < -_tolerance)
                {
                    PastHeelSection(shaft);
                    return;
                }
                front = new LineSegment(heelEndFront, onFront);
                frontDirection = alongFront;
            }
            else
            {
                var moved = meet.Value + toNut * offset;
                ConstructionLines.Add(new LineSegment(heelEndFront, meet.Value));
                var arc = ThroughPoints(heelEndFront, sectionFront, moved);
                bool counterclockwise = (sectionFront - heelEndFront).Cross(moved - sectionFront) > 0;
                if (radius >= arc.Radius)
                {
                    Error($"The {shaft.Label} must be less than the radius of the front of the heel ({Format(arc.Radius)}).");
                    return;
                }
                // The fillet's center is the radius out from the back of the neck and the radius in from the front's
                // circle. Of the two places, the one nearer the corner.
                var q = firstBack + outward * radius - arc.Center;
                double b = q.Dot(toNut);
                double discriminant = b * b - (q.Dot(q) - Math.Pow(arc.Radius - radius, 2));
                if (discriminant < 0)
                {
                    Error($"The {shaft.Label} is too large to fit between the front of the heel and the back of the neck.");
                    return;
                }
                var centers = new[] { -b - Math.Sqrt(discriminant), -b + Math.Sqrt(discriminant) }
                    .Select(along => firstBack + outward * radius + toNut * along);
                var center = centers.OrderBy(c => c.DistanceTo(moved)).First();
                fillet = new Circle(center, radius);
                onFront = arc.Center + (center - arc.Center).Unit * arc.Radius;
                onBack = center - outward * radius;
                // The fillet must meet the arc past the heel section.
                if (!(Turned(arc, heelEndFront, sectionFront, counterclockwise) <
                      Turned(arc, heelEndFront, onFront, counterclockwise)))
                {
                    PastHeelSection(shaft);
                    return;
                }
                front = ArcSegment.Around(arc, heelEndFront, onFront, !counterclockwise, _tolerance);
                var radial = onFront - arc.Center;
                frontDirection = counterclockwise ? radial.Left.Unit : radial.Right.Unit;
            }
            if ((fillet.Center - selectedBack).Dot(toNut) > _tolerance)
            {
                Error($"The {shaft.Label}'s center can't be past the {neck.ThicknessFret.Label}.");
                return;
            }

            var filletArc = ArcFrom(fillet, onFront, onBack, frontDirection);
            var segments = new List<Segment>
            {
                new LineSegment(nutEnd, joint),
                new LineSegment(joint, heelEndJoint),
                new LineSegment(heelEndJoint, heelEndFront),
            };
            segments.AddIfDrawn(front);
            segments.AddIfDrawn(filletArc);
            segments.Add(new LineSegment(onBack, nutBack));
            segments.Add(new LineSegment(nutBack, nutEnd));
            Outline = new Path(segments, isClosed: true);

            // The shaft is the strip from the nut to the body: the back of the neck carried on past where the heel shaft
            // fillet ends until it reaches the joint line. That line, the seam, is in both pieces, and both end flush on
            // the joint line. The heel is the rest of the outline, behind the seam.
            var seamMeet = Intersect(onBack, -toNut, joint, Vector2.XAxis);
            if (seamMeet is Point2 seam &&
                (seam - onBack).Dot(-toNut) > _tolerance &&
                seam.X > heelEndX + _tolerance && seam.X < joint.X - _tolerance)
            {
                var heel = new List<Segment>
                {
                    new LineSegment(seam, heelEndJoint),
                    new LineSegment(heelEndJoint, heelEndFront),
                };
                heel.AddIfDrawn(front);
                heel.AddIfDrawn(filletArc);
                heel.Add(new LineSegment(onBack, seam));
                Heel = new Path(heel, isClosed: true);
                Shaft = new Path(new Segment[]
                {
                    new LineSegment(nutEnd, joint),
                    new LineSegment(joint, seam),
                    new LineSegment(seam, onBack),
                    new LineSegment(onBack, nutBack),
                    new LineSegment(nutBack, nutEnd),
                }, isClosed: true);
            }
            else
                Error("The back of the neck, carried on to the joint line, doesn't land between the heel end and the top of " +
                      "the joint, so the neck can't be split into its shaft and heel: check the neck thicknesses and the " +
                      "heel end offset.");

            if (heelEndX - capOuter > _tolerance)
                HeelCap = Rectangle(new Point2(capOuter, neckEnd), new Point2(heelEndX, neckEnd),
                    new Point2(heelEndX, neckEnd + heelEndThickness), new Point2(capOuter, neckEnd + heelEndThickness));

            if (neck.NutHeight is double nutHeight && nutHeight > 0)
            {
                var nutUp = boardToNut.Right * nutHeight;
                Nut = Rectangle(fretboardSide.NutUnder, fretboardSide.NutUnder + nutUp, nutEnd + nutUp, nutEnd);
            }
            else
                Messages.Add(new GeometryMessage(MessageLevel.Remark,
                    $"Set the {neck.Nut.Reveal.Label} to draw the nut in the side view."));
        }

        void PastHeelSection(Parameter shaft) =>
            Error($"The {shaft.Label} can't touch the front of the heel between the heel section and the heel end.");

        // Where two lines cross, or null when they're parallel.
        static Point2? Intersect(Point2 p, Vector2 d, Point2 q, Vector2 e)
        {
            double cross = d.Cross(e);
            if (Math.Abs(cross) < 1e-12)
                return null;
            return p + d * ((q - p).Cross(e) / cross);
        }

        // The circle through three points that aren't in a line.
        static Circle ThroughPoints(Point2 a, Point2 b, Point2 c)
        {
            var ab = b - a;
            var ac = c - a;
            double d = 2 * ab.Cross(ac);
            double abSquared = ab.Dot(ab);
            double acSquared = ac.Dot(ac);
            var center = a + new Vector2(ac.Y * abSquared - ab.Y * acSquared, ab.X * acSquared - ac.X * abSquared) / d;
            return new Circle(center, center.DistanceTo(a));
        }

        // How far round the circle from one point to another, turning the given way, from 0 to 2π.
        static double Turned(Circle circle, Point2 from, Point2 to, bool counterclockwise)
        {
            double turned = (to - circle.Center).Angle - (from - circle.Center).Angle;
            if (!counterclockwise)
                turned = -turned;
            return (turned % (2 * Math.PI) + 2 * Math.PI) % (2 * Math.PI);
        }

        // The arc round the circle from one point to another, leaving the first in the given direction.
        ArcSegment ArcFrom(Circle circle, Point2 from, Point2 to, Vector2 direction) =>
            ArcSegment.Around(circle, from, to, clockwise: direction.Dot((from - circle.Center).Left) < 0, _tolerance);

        static Path Rectangle(params Point2[] corners) =>
            new Path(corners.Select((corner, i) => (Segment)new LineSegment(corner, corners[(i + 1) % corners.Length])),
                isClosed: true);

        void Error(string text) => Messages.Add(new GeometryMessage(MessageLevel.Error, text));

        string Format(double millimeters) =>
            new NumberParameter(Body.Definitions.Length, millimeters).FormatWithUnit(_unit, _culture);
    }
}
