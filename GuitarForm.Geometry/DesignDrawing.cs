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
        readonly List<DrawnCurve> _curves = new List<DrawnCurve>();

        // Everything drawn, tagged with its component and role: the single source of truth.
        public IReadOnlyList<DrawnCurve> Curves => _curves;

        // Views of Curves: construction (and center) lines and circles, and the final outline.
        public IReadOnlyList<Curve> Construction =>
            _curves.Where(c => c.Role != Shapes.DrawingRole.Outline).Select(c => c.Curve).ToList();
        public IReadOnlyList<Curve> Outline =>
            _curves.Where(c => c.Role == Shapes.DrawingRole.Outline).Select(c => c.Curve).ToList();

        void Add(Curve curve, Shapes.DrawingComponent component, Shapes.DrawingRole role) =>
            _curves.Add(new DrawnCurve(curve, component, role));

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

        // A drawing made by GuitarForm.Shapes, as Rhino curves, with each section's messages.
        public static DesignDrawing FromShapes(Shapes.GuitarDrawing shapes, IEnumerable<GeometryMessage> instrumentMessages,
            IEnumerable<GeometryMessage> bodyMessages, IEnumerable<GeometryMessage> soundholeMessages)
        {
            var drawing = new DesignDrawing();
            drawing._curves.AddRange(ShapeCurves.Curves(shapes, out int failed));
            drawing.InstrumentMessages.AddRange(instrumentMessages);
            drawing.BodyMessages.AddRange(bodyMessages);
            if (failed > 0)
                drawing.BodyMessages.Add(new GeometryMessage(MessageLevel.Error,
                    $"{failed} outline piece(s) couldn't be turned into curves and are left out."));
            drawing.SoundholeMessages.AddRange(soundholeMessages);
            drawing.SideViewOffset = shapes?.SideView?.Outline?.Bounds.MinX;
            return drawing;
        }

        void DrawBody(Body body, double tolerance, double gap, Func<double, string> formatLength)
        {
            var plate = PlateGeometry.Solve(body, tolerance, formatLength);
            BodyMessages.AddRange(plate.Messages);
            if (plate.Failed)
                return;

            const Shapes.DrawingComponent top = Shapes.DrawingComponent.TopPlate;
            foreach (var line in plate.ConstructionLines)
                Add(new LineCurve(line), top, Shapes.DrawingRole.Construction);
            foreach (var circle in plate.OutlineRadii)
                Add(new ArcCurve(circle), top, Shapes.DrawingRole.Construction);
            foreach (var arc in plate.OutlineArcs)
                Add(new ArcCurve(arc), top, Shapes.DrawingRole.Outline);
            foreach (var line in plate.OutlineLines)
                Add(new LineCurve(line), top, Shapes.DrawingRole.Outline);

            double offset = RightEdge(plate) + gap;
            var side = SideViewGeometry.Solve(body, offset);
            BodyMessages.AddRange(side.Messages);
            if (side.Failed || side.OutlineLines.Count == 0)
                return;

            foreach (var line in side.OutlineLines)
                Add(new LineCurve(line), Shapes.DrawingComponent.Sides, Shapes.DrawingRole.Outline);
            SideViewOffset = offset;
        }

        void DrawSoundhole(Soundhole soundhole)
        {
            var geometry = SoundholeGeometry.Solve(soundhole);
            SoundholeMessages.AddRange(geometry.Messages);
            if (!geometry.Failed)
                Add(new ArcCurve(geometry.Circle), Shapes.DrawingComponent.Soundhole, Shapes.DrawingRole.Outline);
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
