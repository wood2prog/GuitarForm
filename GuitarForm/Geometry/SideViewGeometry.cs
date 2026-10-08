using System.Collections.Generic;
using Rhino.Geometry;

namespace GuitarForm.Geometry
{
    // Side view inputs. The drawing offset is optional (null when not connected) and defaults to 0.
    public sealed record SideViewDimensions
    {
        public double BodyLength { get; init; }
        public double? DrawingOffset { get; init; }
    }

    // Side view of the body. It's built with the tail end at the origin and the body length up +Y, the same as Plate,
    // then moved DrawingOffset to the right (+X) so it can sit beside the plate.
    public sealed class SideViewGeometry
    {
        // Output geometry, in output order, after the move.
        public List<Line> ConstructionLines { get; } = new List<Line>();

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

            // Centerline, from the tail end to the top of the body.
            ConstructionLines.Add(new Line(Point3d.Origin, new Point3d(0, _d.BodyLength, 0)));

            MoveToDrawingOffset();
            return true;
        }

        // Moves everything built at the origin to the right by the drawing offset.
        void MoveToDrawingOffset()
        {
            var move = Transform.Translation(_d.DrawingOffset ?? 0, 0, 0);
            for (int i = 0; i < ConstructionLines.Count; i++)
            {
                var line = ConstructionLines[i];
                line.Transform(move);
                ConstructionLines[i] = line;
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
