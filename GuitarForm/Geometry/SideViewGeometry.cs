using System.Collections.Generic;
using GuitarForm.Model;
using Rhino.Geometry;

namespace GuitarForm.Geometry
{
    // Side view inputs. The drawing offset is optional (null when not connected) and defaults to 0.
    public sealed record SideViewDimensions
    {
        public double BodyLength { get; init; }
        public double TailDepth { get; init; }
        public double NeckDepth { get; init; }
        public double? DrawingOffset { get; init; }
    }

    // Side view of the body. It's built with the tail end at the origin and the body length up +Y, the same as Plate,
    // then moved DrawingOffset to the right (+X) so it can sit beside the plate. The bottom line runs up x = 0 and the
    // top line is the tail depth away from it at the tail end and the neck depth away at the neck end.
    public sealed class SideViewGeometry
    {
        // Output geometry, in output order, after the move.
        public List<Line> OutlineLines { get; } = new List<Line>();

        public List<GeometryMessage> Messages { get; } = new List<GeometryMessage>();

        // True when an error stopped the solution. The geometry is then incomplete and shouldn't be output.
        public bool Failed { get; private set; }

        readonly SideViewDimensions _d;

        SideViewGeometry(SideViewDimensions dimensions)
        {
            _d = dimensions;
        }

        public static SideViewGeometry Solve(SideViewDimensions dimensions)
        {
            var side = new SideViewGeometry(dimensions);
            side.Failed = !side.Build();
            return side;
        }

        bool Build()
        {
            if (!(_d.BodyLength > 0)) return Fail("Body length must be greater than zero.");
            if (!(_d.TailDepth > 0)) return Fail("Tail depth must be greater than zero.");
            if (!(_d.NeckDepth > 0)) return Fail("Neck depth must be greater than zero.");

            var tailBottom = Point3d.Origin;
            var tailTop = new Point3d(_d.TailDepth, 0, 0);
            var neckBottom = new Point3d(0, _d.BodyLength, 0);
            var neckTop = new Point3d(_d.NeckDepth, _d.BodyLength, 0);

            OutlineLines.Add(new Line(tailBottom, tailTop));
            OutlineLines.Add(new Line(neckBottom, neckTop));
            OutlineLines.Add(new Line(tailBottom, neckBottom));
            OutlineLines.Add(new Line(tailTop, neckTop));

            MoveToDrawingOffset();
            return true;
        }

        // Moves everything built at the origin to the right by the drawing offset.
        void MoveToDrawingOffset()
        {
            var move = Transform.Translation(_d.DrawingOffset ?? 0, 0, 0);
            for (int i = 0; i < OutlineLines.Count; i++)
            {
                var line = OutlineLines[i];
                line.Transform(move);
                OutlineLines[i] = line;
            }
        }

        // Adds an error and returns false, so callers can write `return Fail(...)`.
        bool Fail(string text)
        {
            Messages.Add(new GeometryMessage(MessageLevel.Error, text));
            return false;
        }
    }
}
