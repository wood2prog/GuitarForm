using System;
using System.Collections.Generic;
using System.Linq;
using GuitarForm.Model.Parameters;
using GuitarForm.Model.Parts;
using GeometryMessage = GuitarForm.Model.GeometryMessage;
using MessageLevel = GuitarForm.Model.MessageLevel;

namespace GuitarForm.Shapes
{
    // Everything drawn for a guitar, in millimeters: the plate from the origin up +Y with its soundhole, and the side
    // view to its right, the gap away from the plate's widest point. Construction geometry is kept apart from the
    // final outline, which is drawn and built differently.
    public sealed class GuitarDrawing
    {
        public List<LineSegment> ConstructionLines { get; } = new List<LineSegment>();
        public List<Circle> ConstructionCircles { get; } = new List<Circle>();
        public List<Path> OutlinePaths { get; } = new List<Path>();
        public List<Circle> OutlineCircles { get; } = new List<Circle>();

        // Problems the model's checks can't see, from drawing the body.
        public List<GeometryMessage> BodyMessages { get; } = new List<GeometryMessage>();

        public PlateShape Plate { get; private set; }
        public SideViewShape SideView { get; private set; }
        public Circle? Soundhole { get; private set; }

        // Lengths in messages are shown in unit.
        public static GuitarDrawing Draw(Guitar guitar, double tolerance, double gap,
            LengthUnit unit = LengthUnit.Millimeters, IFormatProvider culture = null)
        {
            var drawing = new GuitarDrawing();
            var plate = drawing.Plate = PlateShape.Solve(guitar, tolerance, unit, culture);
            drawing.BodyMessages.AddRange(plate.Messages);
            if (plate.Failed)
                return drawing;

            drawing.ConstructionLines.AddRange(plate.ConstructionLines);
            drawing.ConstructionCircles.AddRange(plate.Radii);
            drawing.OutlinePaths.AddRange(plate.Outline);

            drawing.Soundhole = SoundholeShape.Solve(guitar.Body.TopPlate);
            if (drawing.Soundhole is Circle soundhole)
                drawing.OutlineCircles.Add(soundhole);

            var side = drawing.SideView = SideViewShape.Solve(guitar.Body, RightEdge(plate) + gap);
            drawing.BodyMessages.AddRange(side.Messages);
            if (side.Outline != null)
                drawing.OutlinePaths.Add(side.Outline);
            return drawing;
        }

        // The plate's widest point right of the centerline: its outline and construction lines, but not the radii,
        // which can reach past the outline. 0 when nothing is drawn.
        static double RightEdge(PlateShape plate) =>
            plate.Outline.Select(path => path.Bounds.MaxX)
                .Concat(plate.ConstructionLines.Select(line => line.Bounds.MaxX))
                .DefaultIfEmpty(0)
                .Max();
    }
}
