using System.Collections.Generic;
using GuitarForm.Model;
using Rhino.Geometry;

namespace GuitarForm.Geometry
{
    // Custom soundhole inputs. The horizontal offset is optional (null when not connected) and defaults to 0, the body
    // centre.
    public sealed record CustomSoundholeDimensions
    {
        public Curve Shape { get; init; }
        public double? HorizontalOffset { get; init; }
        public double TailOffset { get; init; }
    }

    // Custom-shaped soundhole: a closed curve in the XY plane, moved so its area centroid sits HorizontalOffset across
    // from the body centreline (+X is right) and TailOffset up the Y axis from the tail end (the origin). Where the shape
    // was drawn doesn't matter; only its form is used.
    public sealed class CustomSoundholeGeometry
    {
        // The shape moved into place, and its area centroid.
        public Curve Curve { get; private set; }
        public Point3d Center { get; private set; }

        public List<GeometryMessage> Messages { get; } = new List<GeometryMessage>();

        // True when an error stopped the solution. The geometry is then incomplete and shouldn't be output.
        public bool Failed { get; private set; }

        readonly CustomSoundholeDimensions _d;
        readonly double _tolerance;

        CustomSoundholeGeometry(CustomSoundholeDimensions dimensions, double tolerance)
        {
            _d = dimensions;
            _tolerance = tolerance;
        }

        public static CustomSoundholeGeometry Solve(CustomSoundholeDimensions dimensions, double tolerance)
        {
            var soundhole = new CustomSoundholeGeometry(dimensions, tolerance);
            soundhole.Failed = !soundhole.Build();
            return soundhole;
        }

        bool Build()
        {
            var shape = _d.Shape;
            if (shape == null || !shape.IsValid) return Fail("Soundhole shape isn't a valid curve.");
            if (!shape.IsClosed) return Fail("Soundhole shape must be a closed curve.");
            if (!shape.TryGetPlane(out var plane, _tolerance)) return Fail("Soundhole shape must be planar.");
            if (plane.ZAxis.IsParallelTo(Vector3d.ZAxis) == 0)
                return Fail("Soundhole shape must lie in a plane parallel to the XY plane.");

            var area = AreaMassProperties.Compute(shape);
            if (area == null) return Fail("Couldn't find the area centroid of the soundhole shape.");

            Center = new Point3d(_d.HorizontalOffset ?? 0, _d.TailOffset, 0);
            Curve = shape.DuplicateCurve();
            Curve.Translate(Center - area.Centroid);
            return true;
        }

        // Adds an error and returns false, so callers can write `return Fail(...)`.
        bool Fail(string text)
        {
            Messages.Add(new GeometryMessage(MessageLevel.Error, text));
            return false;
        }
    }
}
