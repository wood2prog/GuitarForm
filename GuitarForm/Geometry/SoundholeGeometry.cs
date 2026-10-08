using System.Collections.Generic;
using GuitarForm.Model;
using Rhino.Geometry;

namespace GuitarForm.Geometry
{
    // Soundhole inputs. The horizontal offset is optional (null when not connected) and defaults to 0, the body centre.
    public sealed record SoundholeDimensions
    {
        public double Diameter { get; init; }
        public double? HorizontalOffset { get; init; }
        public double TailOffset { get; init; }
    }

    // Round soundhole: a circle centred HorizontalOffset across from the body centreline (+X is right) and TailOffset up
    // the Y axis from the tail end (the origin).
    public sealed class SoundholeGeometry
    {
        public Point3d Center { get; private set; }
        public Circle Circle { get; private set; }

        public List<GeometryMessage> Messages { get; } = new List<GeometryMessage>();

        // True when an error stopped the solution. The geometry is then incomplete and shouldn't be output.
        public bool Failed { get; private set; }

        readonly SoundholeDimensions _d;

        SoundholeGeometry(SoundholeDimensions dimensions)
        {
            _d = dimensions;
        }

        public static SoundholeGeometry Solve(SoundholeDimensions dimensions)
        {
            var soundhole = new SoundholeGeometry(dimensions);
            soundhole.Failed = !soundhole.Build();
            return soundhole;
        }

        bool Build()
        {
            if (!(_d.Diameter > 0)) return Fail("Soundhole diameter must be greater than zero.");

            Center = new Point3d(_d.HorizontalOffset ?? 0, _d.TailOffset, 0);
            Circle = new Circle(Center, _d.Diameter / 2);
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
