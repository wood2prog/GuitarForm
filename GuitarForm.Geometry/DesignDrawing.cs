using System;
using System.Collections.Generic;
using System.Linq;
using GuitarForm.Model;
using Rhino.Geometry;

namespace GuitarForm.Geometry
{
    // Everything drawn for a design, in millimetres: the plate outline from the origin up +Y, the side view to its
    // right (the gap away from the plate's widest point), and the soundhole. Construction geometry (construction lines,
    // outline radii circles) is kept apart from the final outline, which is drawn and built differently. Messages are
    // kept per section so each can be shown with that section's values.
    public sealed class DesignDrawing
    {
        public List<Curve> Construction { get; } = new List<Curve>();
        public List<Curve> Outline { get; } = new List<Curve>();

        public List<GeometryMessage> InstrumentMessages { get; } = new List<GeometryMessage>();
        public List<GeometryMessage> BodyMessages { get; } = new List<GeometryMessage>();
        public List<GeometryMessage> SoundholeMessages { get; } = new List<GeometryMessage>();

        // Where the side view's bottom line was drawn, or null if it wasn't.
        public double? SideViewOffset { get; private set; }

        // Lengths in messages are written with formatLength; see PlateGeometry.Solve.
        public static DesignDrawing Draw(GuitarDesign design, double tolerance, double gap, Func<double, string> formatLength = null)
        {
            var drawing = new DesignDrawing();
            drawing.InstrumentMessages.AddRange(design.Instrument.Validate());
            drawing.DrawBody(design.Body, tolerance, gap, formatLength);
            if (design.Soundhole != null)
                drawing.DrawSoundhole(design.Soundhole);
            return drawing;
        }

        void DrawBody(Body body, double tolerance, double gap, Func<double, string> formatLength)
        {
            var plate = PlateGeometry.Solve(body, tolerance, formatLength);
            BodyMessages.AddRange(plate.Messages);
            if (plate.Failed)
                return;

            Construction.AddRange(plate.ConstructionLines.Select(line => new LineCurve(line)));
            Construction.AddRange(plate.OutlineRadii.Select(circle => new ArcCurve(circle)));
            Outline.AddRange(plate.OutlineArcs.Select(arc => new ArcCurve(arc)));
            Outline.AddRange(plate.OutlineLines.Select(line => new LineCurve(line)));

            double offset = RightEdge(plate) + gap;
            var side = SideViewGeometry.Solve(body, offset);
            BodyMessages.AddRange(side.Messages);
            if (side.Failed || side.OutlineLines.Count == 0)
                return;

            Outline.AddRange(side.OutlineLines.Select(line => new LineCurve(line)));
            SideViewOffset = offset;
        }

        void DrawSoundhole(Soundhole soundhole)
        {
            var geometry = SoundholeGeometry.Solve(soundhole);
            SoundholeMessages.AddRange(geometry.Messages);
            if (!geometry.Failed)
                Outline.Add(new ArcCurve(geometry.Circle));
        }

        // The plate's widest point to the right of the centreline: its outline and construction lines, but not the
        // outline radii circles, which can reach past the outline. 0 if nothing has been drawn.
        static double RightEdge(PlateGeometry plate)
        {
            double right = 0;
            foreach (var arc in plate.OutlineArcs)
                right = Math.Max(right, arc.BoundingBox().Max.X);
            foreach (var line in plate.OutlineLines.Concat(plate.ConstructionLines))
                right = Math.Max(right, Math.Max(line.From.X, line.To.X));
            return right;
        }
    }
}
