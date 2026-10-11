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
                return shape;

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
            return shape;
        }
    }
}
