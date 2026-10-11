using System;
using System.Collections.Generic;
using GuitarForm.Model.Parts;
using GeometryMessage = GuitarForm.Model.GeometryMessage;
using MessageLevel = GuitarForm.Model.MessageLevel;

namespace GuitarForm.Shapes
{
    // The body seen from the side, with the tail end at y = 0 and the neck end at the body length, the back on the left
    // and the top on the right. The tail and neck ends stay square to the centerline. At the tail end the top is the
    // Body Depth at Tail End right of the back; the Body Profile Skew moves the neck end's top that far right of the
    // tail end's (left when negative), and the back is the Body Depth at Neck End left of it. So at no skew the top is
    // straight and the back takes the slant. It's placed with its leftmost point at the given x. Nothing is drawn
    // until both depths are set.
    public sealed class SideViewShape
    {
        // Clockwise from the back at the tail end: tail end, top, neck end, back. Null when it isn't drawn.
        public Path Outline { get; private set; }

        public List<GeometryMessage> Messages { get; } = new List<GeometryMessage>();

        public static SideViewShape Solve(Body body, double left)
        {
            var side = new SideViewShape();
            var tailDepth = body.DepthAtTailEnd; var neckDepth = body.DepthAtNeckEnd;
            if (!(tailDepth.HasValue && neckDepth.HasValue))
            {
                side.Messages.Add(new GeometryMessage(MessageLevel.Remark,
                    $"Set the {tailDepth.Label} and {neckDepth.Label} to draw the side view."));
                return side;
            }
            // The model reports values out of range.
            if (!(body.Length.Value is double length && length > 0 && tailDepth.Value > 0 && neckDepth.Value > 0))
                return side;

            double neckTop = tailDepth.Value.Value + (body.ProfileSkew.Value ?? 0);
            double neckBack = neckTop - neckDepth.Value.Value;
            double shift = left - Math.Min(0, neckBack);

            var backTail = new Point2(shift, 0);
            var topTail = new Point2(shift + tailDepth.Value.Value, 0);
            var topNeck = new Point2(shift + neckTop, length);
            var backNeck = new Point2(shift + neckBack, length);
            side.Outline = new Path(new Segment[]
            {
                new LineSegment(backTail, topTail),
                new LineSegment(topTail, topNeck),
                new LineSegment(topNeck, backNeck),
                new LineSegment(backNeck, backTail),
            }, isClosed: true);
            return side;
        }
    }
}
