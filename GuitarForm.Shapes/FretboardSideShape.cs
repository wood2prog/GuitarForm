using System;
using System.Collections.Generic;
using System.Linq;
using GuitarForm.Model.Parameters;
using GuitarForm.Model.Parts;
using GeometryMessage = GuitarForm.Model.GeometryMessage;
using MessageLevel = GuitarForm.Model.MessageLevel;

namespace GuitarForm.Shapes
{
    // The fretboard seen from the side, on the body's side view, cut along the centerline. Its underside is the
    // Overstand above the top at the body's neck end, measured square to the top's line, and the neck join fret is
    // there. A positive Neck Angle tips the nut end toward the back, measured from the top's line, so it follows the
    // Body Profile Skew. Lengths along the fretboard are the plan view's fret distances. The thickness tapers evenly
    // from the Fretboard Thickness at Nut to the Fretboard Thickness at End.
    public sealed class FretboardSideShape
    {
        // Clockwise from the underside at the nut: nut end, playing surface, body end, underside. Null until it can be
        // drawn.
        public Path Outline { get; private set; }

        // The playing surface at the centerline, at the nut and the body end.
        public Point2 NutTop { get; private set; }
        public Point2 EndTop { get; private set; }

        // The underside, where the fretboard sits on the neck, at the nut and the body end.
        public Point2 NutUnder { get; private set; }
        public Point2 EndUnder { get; private set; }

        public List<GeometryMessage> Messages { get; } = new List<GeometryMessage>();

        double _length;

        // The point on the playing surface the given plan distance from the nut, along the fretboard.
        public Point2 PlayingSurfaceAt(double fromNut) => NutTop + (EndTop - NutTop) * (fromNut / _length);

        public static FretboardSideShape Solve(Guitar guitar, SideViewShape side)
        {
            var shape = new FretboardSideShape();
            if (side.Outline == null)
                return shape;

            var neck = guitar.Neck;
            var fretboard = neck.Fretboard;
            var needed = new Parameter[]
            {
                neck.Angle, neck.Overstand, fretboard.ThicknessAtNut, fretboard.ThicknessAtEnd, fretboard.FretCount,
                fretboard.LastFretOverhang, guitar.ScaleLength, guitar.NeckJoinFret,
            };
            var missing = needed.Where(p => !p.HasValue).Select(p => p.Label).ToList();
            if (missing.Count > 0)
            {
                shape.Messages.Add(new GeometryMessage(MessageLevel.Remark,
                    $"Set the {FretboardShape.Join(missing)} to draw the fretboard in the side view."));
                return shape;
            }
            // The model reports values out of range.
            if (!(fretboard.ThicknessAtNut.Value > 0 && fretboard.ThicknessAtEnd.Value > 0 &&
                  fretboard.FretCount.Value >= 1 && guitar.ScaleLength.Value > 0))
            {
                shape.Messages.Add(new GeometryMessage(MessageLevel.Error,
                    "The fretboard can't be drawn in the side view: a fretboard thickness, the scale length or the fret count is zero or less."));
                return shape;
            }

            // Along the top toward the neck, and square to it away from the back.
            var along = (side.TopAtNeckEnd - side.TopAtTailEnd).Unit;
            var away = along.Right;
            double angle = neck.Angle.Value.Value * Math.PI / 180;
            var toNut = along * Math.Cos(angle) - away * Math.Sin(angle);
            var up = toNut.Right;

            var joint = side.TopAtNeckEnd + away * neck.Overstand.Value.Value;
            double join = guitar.NeckJoinDistance.Value;
            double toEnd = guitar.FretDistance(fretboard.FretCount.Value.Value).Value + fretboard.LastFretOverhang.Value.Value - join;

            var nutUnder = shape.NutUnder = joint + toNut * join;
            var endUnder = shape.EndUnder = joint - toNut * toEnd;
            var nutTop = shape.NutTop = nutUnder + up * fretboard.ThicknessAtNut.Value.Value;
            var endTop = shape.EndTop = endUnder + up * fretboard.ThicknessAtEnd.Value.Value;
            shape._length = join + toEnd;
            shape.Outline = new Path(new Segment[]
            {
                new LineSegment(nutUnder, nutTop),
                new LineSegment(nutTop, endTop),
                new LineSegment(endTop, endUnder),
                new LineSegment(endUnder, nutUnder),
            }, isClosed: true);
            shape._up = up;
            shape._thicknessAtNut = fretboard.ThicknessAtNut.Value.Value;
            shape._thicknessAtEnd = fretboard.ThicknessAtEnd.Value.Value;
            shape.AddSideDots(guitar);
            shape.AddSlotLines(guitar);
            return shape;
        }

        // The side dots: a circle per dot, at its marker's position (halfway between its fret and the one before), the
        // Side Dot Offset up from the underside. A pair is centered on that position, the Side Double Dot Spacing apart
        // along the fretboard. Empty until the Side Dot Diameter and Offset are set.
        public List<Circle> SideDots { get; } = new List<Circle>();

        // A line across the fretboard at each fret, from the playing surface down to the slot bottom; the bottom is one
        // flat level, parallel to the underside, the slot depth below the playing surface at the thickest part.
        public List<LineSegment> SlotLines { get; } = new List<LineSegment>();

        Vector2 _up;
        double _thicknessAtNut, _thicknessAtEnd;

        // The fretboard's thickness at the centerline, the given distance from the nut.
        double ThicknessAt(double fromNut) => _thicknessAtNut + (_thicknessAtEnd - _thicknessAtNut) * (fromNut / _length);

        // The underside the given distance from the nut.
        Point2 UndersideAt(double fromNut) => NutUnder + (EndUnder - NutUnder) * (fromNut / _length);

        void AddSideDots(Guitar guitar)
        {
            var fretboard = guitar.Neck.Fretboard;
            int fretCount = fretboard.FretCount.Value.Value;
            var markers = fretboard.Markers.Where(m => m.Fret.Value is int fret && fret >= 1 && fret <= fretCount).ToList();
            bool anySideField = fretboard.SideDotDiameter.HasValue || fretboard.SideDotOffset.HasValue ||
                                fretboard.SideDoubleDotSpacing.HasValue;
            if (!(fretboard.SideDotDiameter.Value > 0 && fretboard.SideDotOffset.Value >= 0))
            {
                if (markers.Count > 0 || anySideField)
                    Messages.Add(new GeometryMessage(MessageLevel.Remark,
                        "Set the Side Dot Diameter (SDD) and Offset (SDO) to draw the side dots."));
                return;
            }
            double diameter = fretboard.SideDotDiameter.Value.Value, offset = fretboard.SideDotOffset.Value.Value;
            var along = (EndUnder - NutUnder) / _length;
            var breakingThrough = new List<int>();
            bool spacingMissing = false;
            foreach (var marker in markers)
            {
                int fret = marker.Fret.Value.Value;
                double distance = FretboardShape.DotDistanceFromNut(guitar, fret);
                var center = UndersideAt(distance) + _up * offset;
                // As for the face dots, any count but 2 is a single dot.
                if (marker.DotCount.Value != 2)
                    SideDots.Add(new Circle(center, diameter / 2));
                else if (fretboard.SideDoubleDotSpacing.Value is double spacing)
                {
                    SideDots.Add(new Circle(center - along * (spacing / 2), diameter / 2));
                    SideDots.Add(new Circle(center + along * (spacing / 2), diameter / 2));
                }
                else
                {
                    spacingMissing = true;
                    continue;
                }

                // The fretboard is thinner at its edge than on the centerline, by how far the radius drops.
                if (fretboard.Radius.Value is double radius && radius > 0 &&
                    FretboardShape.HalfWidthAtDistance(guitar, distance) is double half)
                {
                    double sag = half >= radius ? radius : radius - Math.Sqrt(radius * radius - half * half);
                    if (offset + diameter / 2 > ThicknessAt(distance) - sag)
                        breakingThrough.Add(fret);
                }
            }
            if (spacingMissing)
                Messages.Add(new GeometryMessage(MessageLevel.Remark,
                    $"Set the {fretboard.SideDoubleDotSpacing.Label} to draw the double side dots."));
            foreach (int fret in breakingThrough)
                Messages.Add(new GeometryMessage(MessageLevel.Warning,
                    $"The side dot at fret {fret} would break through the top of the fretboard at its edge: " +
                    "lower the Side Dot Offset (SDO), or use a smaller Side Dot Diameter (SDD)."));
        }

        void AddSlotLines(Guitar guitar)
        {
            var fretboard = guitar.Neck.Fretboard;
            var wire = guitar.Neck.Fretwire;
            // With no fretwire, the Frets tab's remark already says to choose one.
            if (wire == null)
                return;
            if (!(wire.SlotDepth.Value > 0))
            {
                Messages.Add(new GeometryMessage(MessageLevel.Remark,
                    "Set the fretwire's Slot Depth (FWSD) to draw the fret slots in the side view."));
                return;
            }
            double depth = wire.SlotDepth.Value.Value + (fretboard.SlotOverage.Value ?? 0);
            // The slot bottom is the depth below the playing surface where the fretboard is thickest.
            double bottom = Math.Max(_thicknessAtNut, _thicknessAtEnd) - depth;
            if (bottom <= 0)
            {
                Messages.Add(new GeometryMessage(MessageLevel.Warning,
                    "The fret slots would cut through the fretboard: the slot depth (the fretwire's Slot Depth " +
                    "plus the Fretboard Slot Overage) is at least as much as the fretboard's thickness. No slot lines are drawn."));
                return;
            }
            var tooThin = new List<int>();
            for (int fret = 1; fret <= fretboard.FretCount.Value.Value; fret++)
            {
                double distance = guitar.FretDistance(fret).Value;
                if (bottom >= ThicknessAt(distance))
                {
                    tooThin.Add(fret);
                    continue;
                }
                SlotLines.Add(new LineSegment(PlayingSurfaceAt(distance), UndersideAt(distance) + _up * bottom));
            }
            if (tooThin.Count > 0)
                Messages.Add(new GeometryMessage(MessageLevel.Warning,
                    $"The slot bottom is above the playing surface at fret {FretboardShape.Join(tooThin.Select(f => f.ToString()).ToList())}, " +
                    "so no slot line is drawn there: the fretboard tapers more than the slot depth."));
        }
    }
}
