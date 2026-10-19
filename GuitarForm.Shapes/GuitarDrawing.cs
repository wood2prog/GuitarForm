using System;
using System.Collections.Generic;
using System.Linq;
using GuitarForm.Model.Parameters;
using GuitarForm.Model.Parts;
using GeometryMessage = GuitarForm.Model.GeometryMessage;
using MessageLevel = GuitarForm.Model.MessageLevel;

namespace GuitarForm.Shapes
{
    // Everything drawn for a guitar, in millimeters: the plate from the origin up +Y with its soundhole and the
    // fretboard over it; the bracing on its own copy of the plate, seen from inside, the gap left of the plate's widest
    // point; the side view, with the fretboard's side profile, to its right, the gap away from the plate's
    // widest point, with the neck under the fretboard; and the neck's profiles the gap right of that. Construction geometry is kept apart from the final
    // outline, which is drawn and built differently.
    public sealed class GuitarDrawing
    {
        // Everything drawn, in drawing order, tagged with its component and role.
        public List<DrawnItem> Items { get; } = new List<DrawnItem>();

        // Views of Items: construction (and center) lines and circles, and outline paths and circles.
        public IReadOnlyList<LineSegment> ConstructionLines => Items
            .Where(item => item.Role != DrawingRole.Outline && item.Path != null)
            .Select(item => (LineSegment)item.Path.Segments[0]).ToList();
        public IReadOnlyList<Circle> ConstructionCircles => Items
            .Where(item => item.Role != DrawingRole.Outline && item.Circle.HasValue)
            .Select(item => item.Circle.Value).ToList();
        public IReadOnlyList<Path> OutlinePaths => Items
            .Where(item => item.Role == DrawingRole.Outline && item.Path != null)
            .Select(item => item.Path).ToList();
        public IReadOnlyList<Circle> OutlineCircles => Items
            .Where(item => item.Role == DrawingRole.Outline && item.Circle.HasValue)
            .Select(item => item.Circle.Value).ToList();

        void AddLines(DrawingComponent component, DrawingRole role, IEnumerable<LineSegment> lines) =>
            Items.AddRange(lines.Select(line =>
                new DrawnItem(component, role, new Path(new Segment[] { line }, isClosed: false))));

        void AddCircles(DrawingComponent component, DrawingRole role, IEnumerable<Circle> circles) =>
            Items.AddRange(circles.Select(circle => new DrawnItem(component, role, Circle: circle)));

        void AddPaths(DrawingComponent component, DrawingRole role, IEnumerable<Path> paths) =>
            Items.AddRange(paths.Where(path => path != null).Select(path => new DrawnItem(component, role, path)));

        // Problems the model's checks can't see, from drawing the body.
        public List<GeometryMessage> BodyMessages { get; } = new List<GeometryMessage>();

        // Problems the model's checks can't see, from drawing the bracing.
        public List<GeometryMessage> BracingMessages { get; } = new List<GeometryMessage>();

        // Problems the model's checks can't see, from drawing the fretboard.
        public List<GeometryMessage> FretboardMessages { get; } = new List<GeometryMessage>();

        // Problems the model's checks can't see, from drawing the neck.
        public List<GeometryMessage> NeckMessages { get; } = new List<GeometryMessage>();

        public PlateShape Plate { get; private set; }
        public FretboardShape Fretboard { get; private set; }
        public FretboardSideShape FretboardSide { get; private set; }
        public SideViewShape SideView { get; private set; }
        public NeckSideShape NeckSide { get; private set; }
        public NeckProfilesShape NeckProfiles { get; private set; }
        public Circle? Soundhole { get; private set; }

        // The bracing where it's drawn, left of the plate; null when there's nothing to draw.
        public BracingShape Bracing { get; private set; }

        // Lengths in messages are shown in unit.
        public static GuitarDrawing Draw(Guitar guitar, double tolerance, double gap,
            LengthUnit unit = LengthUnit.Millimeters, IFormatProvider culture = null)
        {
            var drawing = new GuitarDrawing();
            var plate = drawing.Plate = PlateShape.Solve(guitar, tolerance, unit, culture);
            drawing.BodyMessages.AddRange(plate.Messages);
            // Each part is drawn on its own: a failed plate leaves only itself, and what's placed on it, blank.
            if (plate.Failed && !guitar.Body.Length.HasValue)
            {
                // Nothing to draw yet is not an error: say what to set, as the other shapes do.
                if (!plate.Messages.Any())
                    drawing.BodyMessages.Add(new GeometryMessage(MessageLevel.Remark,
                        $"Set the {guitar.Body.Length.Label} to draw the body outline."));
            }
            else if (plate.Failed)
            {
                // The model reports out-of-range values and the plate says nothing of them; a failure never goes unsaid.
                if (!plate.Messages.Any(m => m.Level == MessageLevel.Error))
                    drawing.BodyMessages.Add(new GeometryMessage(MessageLevel.Error,
                        $"The body outline can't be drawn: a value is out of range or the {guitar.Body.Length.Label} isn't set above zero."));
                drawing.BodyMessages.Add(new GeometryMessage(MessageLevel.Warning,
                    "The body outline has errors; the rest is drawn without it."));
            }

            drawing.AddLines(DrawingComponent.TopPlate, DrawingRole.Construction, plate.ConstructionLines);
            drawing.AddCircles(DrawingComponent.TopPlate, DrawingRole.Construction, plate.Radii);
            drawing.AddPaths(DrawingComponent.TopPlate, DrawingRole.Outline, plate.Outline);

            drawing.Soundhole = SoundholeShape.Solve(guitar.Body.TopPlate);
            if (drawing.Soundhole is Circle soundhole)
                drawing.AddCircles(DrawingComponent.Soundhole, DrawingRole.Outline, new[] { soundhole });

            var bracing = BracingShape.Solve(guitar, plate, drawing.Soundhole, unit, culture);
            drawing.BracingMessages.AddRange(bracing.Messages);
            if (plate.Failed)
                drawing.BracingMessages.Add(new GeometryMessage(MessageLevel.Remark,
                    "The bracing is drawn on the body outline: it isn't drawn until the body outline can be."));
            if (!bracing.IsEmpty)
            {
                // Mirrored, so the copy's right edge is the plate's left edge, moved the gap further left.
                double left = LeftEdge(plate);
                var placed = drawing.Bracing = bracing.Placed(2 * left - gap);
                drawing.AddLines(DrawingComponent.Bracing, DrawingRole.Construction, placed.ConstructionLines);
                drawing.AddPaths(DrawingComponent.Bracing, DrawingRole.Outline, new[] { placed.Plate });
                if (placed.Soundhole is Circle copy)
                    drawing.AddCircles(DrawingComponent.Bracing, DrawingRole.Outline, new[] { copy });
                foreach (var brace in placed.Braces)
                    drawing.AddPaths(brace.Kind switch
                    {
                        BracingShape.BraceKind.Headblock => DrawingComponent.Headblock,
                        BracingShape.BraceKind.Tailblock => DrawingComponent.Tailblock,
                        _ => DrawingComponent.Bracing,
                    }, DrawingRole.Outline, new[] { brace.Outline });
            }

            double sideLeft = RightEdge(plate) + gap;
            var side = drawing.SideView = SideViewShape.Solve(guitar.Body, sideLeft);
            drawing.BodyMessages.AddRange(side.Messages);
            drawing.AddPaths(DrawingComponent.Sides, DrawingRole.Outline, new[] { side.Outline });

            var fretboard = drawing.Fretboard = FretboardShape.Solve(guitar);
            drawing.FretboardMessages.AddRange(fretboard.Messages);
            drawing.AddLines(DrawingComponent.Frets, DrawingRole.Centerline, fretboard.FretLines);
            drawing.AddPaths(DrawingComponent.Frets, DrawingRole.Outline, fretboard.Beads);
            // A string with a diameter is a rectangle; one without keeps its centerline.
            for (int i = 0; i < fretboard.Strings.Count; i++)
            {
                if (fretboard.StringOutlines[i] != null)
                    drawing.AddPaths(DrawingComponent.Strings, DrawingRole.Outline, new[] { fretboard.StringOutlines[i] });
                else
                    drawing.AddLines(DrawingComponent.Strings, DrawingRole.Centerline, new[] { fretboard.Strings[i] });
            }
            drawing.AddPaths(DrawingComponent.Fretboard, DrawingRole.Outline, new[] { fretboard.Outline });
            drawing.AddPaths(DrawingComponent.FretSlots, DrawingRole.Outline, fretboard.Slots);
            drawing.AddCircles(DrawingComponent.FaceDots, DrawingRole.Outline, fretboard.Dots);

            var fretboardSide = drawing.FretboardSide = FretboardSideShape.Solve(guitar, side);
            drawing.FretboardMessages.AddRange(fretboardSide.Messages);
            drawing.AddPaths(DrawingComponent.FretboardSide, DrawingRole.Outline, new[] { fretboardSide.Outline });
            drawing.AddLines(DrawingComponent.FretSlots, DrawingRole.Outline, fretboardSide.SlotLines);
            drawing.AddCircles(DrawingComponent.SideDots, DrawingRole.Outline, fretboardSide.SideDots);

            var neckSide = drawing.NeckSide = NeckSideShape.Solve(guitar, side, fretboardSide, tolerance, unit, culture);
            drawing.NeckMessages.AddRange(neckSide.Messages);
            drawing.AddLines(DrawingComponent.NeckSide, DrawingRole.Construction, neckSide.ConstructionLines);
            // neckSide.Outline is the whole neck, kept for layout bounds only: Heel and Shaft are what get drawn.
            var neckSidePaths = new[] { neckSide.Outline, neckSide.HeelCap, neckSide.Nut }.Where(path => path != null).ToList();
            drawing.AddPaths(DrawingComponent.NeckSide, DrawingRole.Outline, new[] { neckSide.Shaft });
            drawing.AddPaths(DrawingComponent.Heel, DrawingRole.Outline, new[] { neckSide.Heel });
            drawing.AddPaths(DrawingComponent.HeelCap, DrawingRole.Outline, new[] { neckSide.HeelCap });
            drawing.AddPaths(DrawingComponent.Nut, DrawingRole.Outline, new[] { neckSide.Nut });

            // Beside the side view, or where it would be.
            var sideViews = new[] { side.Outline, fretboardSide.Outline }.Concat(neckSidePaths)
                .Where(path => path != null).ToList();
            double profilesLeft = (sideViews.Count > 0 ? sideViews.Max(path => path.Bounds.MaxX) : sideLeft) + gap;
            var profiles = drawing.NeckProfiles =
                NeckProfilesShape.Solve(guitar, fretboardSide, profilesLeft, gap, tolerance, unit, culture);
            drawing.NeckMessages.AddRange(profiles.Messages);
            foreach (var profile in profiles.All)
            {
                drawing.AddPaths(DrawingComponent.NeckProfiles, DrawingRole.Outline, new[] { profile.Outline });
                if (profile.FretboardUnderside != null)
                    drawing.AddPaths(DrawingComponent.NeckProfiles, DrawingRole.Outline,
                        new[] { new Path(new[] { profile.FretboardUnderside }, isClosed: false) });
            }
            return drawing;
        }

        // The plate's widest point right of the centerline: its outline and construction lines, but not the radii,
        // which can reach past the outline. 0 when nothing is drawn (e.g. the plate failed).
        static double RightEdge(PlateShape plate) =>
            plate.Outline.Select(path => path.Bounds.MaxX)
                .Concat(plate.ConstructionLines.Select(line => line.Bounds.MaxX))
                .DefaultIfEmpty(0)
                .Max();

        // The same on the left of the centerline.
        static double LeftEdge(PlateShape plate) =>
            plate.Outline.Select(path => path.Bounds.MinX)
                .Concat(plate.ConstructionLines.Select(line => line.Bounds.MinX))
                .DefaultIfEmpty(0)
                .Min();
    }
}
