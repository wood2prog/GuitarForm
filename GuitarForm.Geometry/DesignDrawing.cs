using System.Collections.Generic;
using System.Linq;
using GuitarForm.Model;
using Rhino.Geometry;

namespace GuitarForm.Geometry
{
    // Everything drawn for a design, in millimeters: the plate outline from the origin up +Y, the side view to its
    // right (the gap away from the plate's widest point), and the soundhole. Construction geometry (construction lines,
    // outline radii circles) is kept apart from the final outline, which is drawn and built differently. Messages are
    // kept per section so each can be shown with that section's values.
    public sealed class DesignDrawing
    {
        readonly List<DrawnCurve> _curves = new List<DrawnCurve>();

        // Everything drawn, tagged with its component and role: the single source of truth.
        public IReadOnlyList<DrawnCurve> Curves => _curves;

        // Views of Curves: construction (and center) lines and circles, and the final outline.
        // Computed once, on first use: _curves is only filled in FromShapes, before the drawing is returned.
        IReadOnlyList<Curve> _construction, _outline;
        public IReadOnlyList<Curve> Construction =>
            _construction ??= _curves.Where(c => c.Role != Shapes.DrawingRole.Outline).Select(c => c.Curve).ToList();
        public IReadOnlyList<Curve> Outline =>
            _outline ??= _curves.Where(c => c.Role == Shapes.DrawingRole.Outline).Select(c => c.Curve).ToList();

        public List<GeometryMessage> InstrumentMessages { get; } = new List<GeometryMessage>();
        public List<GeometryMessage> BodyMessages { get; } = new List<GeometryMessage>();
        public List<GeometryMessage> SoundholeMessages { get; } = new List<GeometryMessage>();

        // Where the side view starts (its left edge), or null if it wasn't drawn.
        public double? SideViewOffset { get; private set; }

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
    }
}
