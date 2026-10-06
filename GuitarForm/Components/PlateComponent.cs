using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace GuitarForm.Components
{
    public class PlateComponent : GH_Component
    {
        // Construction geometry is drawn in light grey so the final outline stands out.
        static readonly Color ConstructionColor = Color.FromArgb(200, 200, 200);

        // Length of the vertical marks at each end of the heel width, centred on the top of the body.
        const double HeelMarkLength = 0.25;

        readonly List<Line> _constructionLines = new List<Line>();
        readonly List<Circle> _outlineCircles = new List<Circle>();
        readonly List<Arc> _outlineArcs = new List<Arc>();
        readonly List<Line> _outlineLines = new List<Line>();

        public PlateComponent()
          : base("Plate", "Plate",
              "Guitar body plate. Outputs construction lines driven by the body dimensions.",
              "GuitarForm", "Body")
        {
        }

        // Input positions. These must match the registration order in RegisterInputParams.
        const int InBodyLength = 0;
        const int InUbWidth = 1;
        const int InUbOffset = 2;
        const int InUbRadius = 3;
        const int InUbSecondaryOffset = 4;
        const int InWaistRadius = 5;
        const int InWaistWidth = 6;
        const int InWaistOffset = 7;
        const int InLbWidth = 8;
        const int InLbOffset = 9;
        const int InLbRadius = 10;
        const int InLbSecondaryOffset = 11;
        const int InHeelWidth = 12;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Body Length", "L", "Overall length of the guitar body", GH_ParamAccess.item);
            pManager.AddNumberParameter("Upper Bout Width", "UbW", "Width of the upper bout", GH_ParamAccess.item);
            pManager.AddNumberParameter("Upper Bout Offset", "UbO", "Offset of the upper bout line down the Y axis from the upper bout primary radius centre", GH_ParamAccess.item);
            pManager.AddNumberParameter("Upper Bout Primary Radius", "UbR", "Primary radius of the upper bout. Its centre sits one radius below the top of the body length line", GH_ParamAccess.item);
            pManager.AddNumberParameter("Upper Bout Secondary Offset", "UbSO", "Moves the upper bout secondary radius centre toward the body centre and grows its radius by the same amount, keeping its outer edge on the primary radius. 0 = same as the primary radius", GH_ParamAccess.item);
            pManager.AddNumberParameter("Waist Radius", "WR", "Radius of the waist curve. Its centre sits on the waist center line, one radius outside the waist width", GH_ParamAccess.item);
            pManager.AddNumberParameter("Waist Width", "WW", "Width of the body at the waist", GH_ParamAccess.item);
            pManager.AddNumberParameter("Waist Offset", "WO", "Distance of the waist center line up the Y axis from the tail end (the origin)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Lower Bout Width", "LbW", "Width of the lower bout", GH_ParamAccess.item);
            pManager.AddNumberParameter("Lower Bout Offset", "LbO", "Offset of the lower bout center line up the Y axis from the lower bout primary radius centre", GH_ParamAccess.item);
            pManager.AddNumberParameter("Lower Bout Primary Radius", "LbR", "Primary radius of the lower bout. Its centre sits one radius above the tail end (the origin)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Lower Bout Secondary Offset", "LbSO", "Moves the lower bout secondary radius centre toward the body centre and grows its radius by the same amount, keeping its outer edge on the primary radius. 0 = same as the primary radius", GH_ParamAccess.item);
            pManager.AddNumberParameter("Heel Width", "HW", "Width of the flat at the top of the body where the heel of the neck attaches", GH_ParamAccess.item);
            for (int i = 1; i < pManager.ParamCount; i++)
                pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Construction Lines", "CL", "Construction lines for the plate", GH_ParamAccess.list);
            pManager.AddCircleParameter("Outline Radii", "OR", "Circles for the upper bout primary and secondary, waist, and lower bout primary and secondary radii, right side then left side for each", GH_ParamAccess.list);
            pManager.AddArcParameter("Outline Arcs", "OA", "Final outline arcs, right then left for each: the shoulder arcs, the upper bout primary and secondary, waist, and lower bout secondary and primary circle segments, then the tail arc", GH_ParamAccess.list);
            pManager.AddLineParameter("Outline Lines", "OL", "Straight parts of the final body outline: the heel flat, the waist tangent lines (upper bout right, left, lower bout right, left), then the straight tail", GH_ParamAccess.list);
        }

        protected override void BeforeSolveInstance()
        {
            _constructionLines.Clear();
            _outlineCircles.Clear();
            _outlineArcs.Clear();
            _outlineLines.Clear();
        }

        // Adds a circle on each side of the Y axis, centred at (±centreX, y).
        static void AddMirroredCircles(List<Circle> circles, double centreX, double y, double radius)
        {
            circles.Add(new Circle(new Point3d(centreX, y, 0), radius));
            circles.Add(new Circle(new Point3d(-centreX, y, 0), radius));
        }

        // Secondary radius: shares the primary's centre line, centre moved toward the body centre by the offset and
        // radius grown by the same amount, so its outer edge stays on the primary's. Returns the right-side circle,
        // or null if the offset isn't connected or is invalid (check RuntimeMessageLevel).
        Circle? AddSecondaryCircles(IGH_DataAccess DA, int inputIndex, string boutName,
            List<Circle> circles, double primaryCentreX, double y, double primaryRadius)
        {
            double secondaryOffset = 0;
            if (!DA.GetData(inputIndex, ref secondaryOffset)) return null;

            if (secondaryOffset < 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{boutName} secondary offset can't be negative.");
                return null;
            }

            double centreX = primaryCentreX - secondaryOffset;
            double radius = primaryRadius + secondaryOffset;
            AddMirroredCircles(circles, centreX, y, radius);
            return new Circle(new Point3d(centreX, y, 0), radius);
        }

        // A secondary radius can't overlap the waist radius on the same side: the bout curve must meet the waist
        // curve tangentially, so the centres must be at least the two radii apart. Returns false and adds an error
        // if they overlap.
        bool CheckSecondaryClearsWaist(Circle secondary, Circle waist, string boutName, string inputName)
        {
            double gap = secondary.Center.DistanceTo(waist.Center) - (secondary.Radius + waist.Radius);
            if (gap >= -DocumentTolerance()) return true;

            AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                $"The {boutName.ToLowerInvariant()} secondary radius overlaps the waist radius by {-gap:0.###}, which is an impossible shape. " +
                $"Reduce the {inputName}, or adjust the waist radius, width or offset.");
            return false;
        }

        // Straight outline line from a bout secondary circle to the waist circle on the right side. It is the internal
        // (crossing) tangent: it touches the bout circle on its outer side and the waist circle on its inner side, so
        // the two circles sit on opposite sides of it. Once the circles touch, the line shrinks to their contact point
        // (From == To). The caller has already rejected overlapping circles.
        static Line WaistTangentLine(Circle bout, Circle waist, double tolerance)
        {
            Vector3d toWaist = waist.Center - bout.Center;
            double distance = toWaist.Length;
            double radiusSum = bout.Radius + waist.Radius;
            toWaist.Unitize();
            if (distance - radiusSum <= tolerance)
            {
                var contact = bout.Center + toWaist * bout.Radius;
                return new Line(contact, contact);
            }

            double angle = Math.Acos(radiusSum / distance);

            // Of the two internal tangents, take the one touching the bout circle further from the body centre.
            Line? best = null;
            foreach (double sign in new[] { 1.0, -1.0 })
            {
                var direction = toWaist;
                direction.Rotate(sign * angle, Vector3d.ZAxis);
                var boutPoint = bout.Center + direction * bout.Radius;
                var waistPoint = waist.Center - direction * waist.Radius;
                if (!best.HasValue || boutPoint.X > best.Value.From.X)
                    best = new Line(boutPoint, waistPoint);
            }
            return best.Value;
        }

        static Point3d MirrorX(Point3d point) => new Point3d(-point.X, point.Y, point.Z);

        static Line MirrorX(Line line) => new Line(MirrorX(line.From), MirrorX(line.To));

        // Adds the outline segment of a right-side circle from one point on it to another, plus its mirror on the left.
        // Clockwise follows the outside of a bout circle from top to bottom; counter-clockwise follows the inner side of
        // the waist circle. Nothing is added if the two points coincide.
        static void AddMirroredSegment(List<Arc> arcs, Circle circle, Point3d from, Point3d to, bool clockwise)
        {
            double startAngle = Math.Atan2(from.Y - circle.Center.Y, from.X - circle.Center.X);
            double endAngle = Math.Atan2(to.Y - circle.Center.Y, to.X - circle.Center.X);
            double sweep = clockwise ? startAngle - endAngle : endAngle - startAngle;
            sweep = ((sweep % (2 * Math.PI)) + 2 * Math.PI) % (2 * Math.PI);
            if (sweep < 1e-9 || sweep > 2 * Math.PI - 1e-9) return;

            double midAngle = clockwise ? startAngle - sweep / 2 : startAngle + sweep / 2;
            var mid = circle.Center + new Vector3d(Math.Cos(midAngle), Math.Sin(midAngle), 0) * circle.Radius;

            arcs.Add(new Arc(from, mid, to));
            arcs.Add(new Arc(MirrorX(from), MirrorX(mid), MirrorX(to)));
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double length = 0;
            if (!DA.GetData(InBodyLength, ref length)) return;

            if (length <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Body Length must be greater than zero.");
                return;
            }

            // Guitar is drawn vertically: body centerline from the origin along +Y.
            var lines = new List<Line>
            {
                new Line(Point3d.Origin, new Point3d(0, length, 0))
            };
            var circles = new List<Circle>();
            double? ubCentreX = null;
            double ubCentreY = 0;
            Circle? ubSecondary = null, lbSecondary = null, waistCircle = null;
            // Right-side primary circles, where the outline joins the upper primary (from the heel end) and leaves the
            // lower primary (to the tail).
            Circle? ubPrimary = null, lbPrimary = null;
            Point3d? ubOutlineStart = null, lbOutlineEnd = null;

            // Upper bout: horizontal line centred on the Y axis. The primary radius centre sits one radius below the
            // top of the body; Upper Bout Offset moves the line down from there.
            double ubWidth = 0, ubOffset = 0, ubRadius = 0;
            bool hasUbWidth = DA.GetData(InUbWidth, ref ubWidth);
            bool hasUbOffset = DA.GetData(InUbOffset, ref ubOffset);
            bool hasUbRadius = DA.GetData(InUbRadius, ref ubRadius);
            if (hasUbWidth && hasUbOffset && hasUbRadius)
            {
                if (ubWidth <= 0)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Upper bout width must be greater than zero.");
                    return;
                }
                if (ubRadius <= 0)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Upper bout primary radius must be greater than zero.");
                    return;
                }
                double ubY = length - ubRadius - ubOffset;
                if (ubY < 0 || ubY > length)
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Upper bout line is outside the body length.");

                double half = ubWidth / 2;
                lines.Add(new Line(new Point3d(-half, ubY, 0), new Point3d(half, ubY, 0)));

                // Radius centre is one radius inside the width, so the circle touches the edge of the width.
                if (ubRadius > half)
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Upper bout primary radius is larger than half the upper bout width.");
                ubCentreX = half - ubRadius;
                ubCentreY = ubY;
                AddMirroredCircles(circles, ubCentreX.Value, ubY, ubRadius);
                ubPrimary = new Circle(new Point3d(ubCentreX.Value, ubY, 0), ubRadius);

                ubSecondary = AddSecondaryCircles(DA, InUbSecondaryOffset, "Upper bout", circles, ubCentreX.Value, ubY, ubRadius);
                if (RuntimeMessageLevel == GH_RuntimeMessageLevel.Error) return;
            }
            else if (hasUbWidth || hasUbOffset || hasUbRadius)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Upper bout width, offset and primary radius are all needed to draw the upper bout line.");
            }

            // Waist: horizontal center line centred on the Y axis, Waist Offset up from the tail end.
            double waistWidth = 0, waistOffset = 0;
            bool hasWaistWidth = DA.GetData(InWaistWidth, ref waistWidth);
            bool hasWaistOffset = DA.GetData(InWaistOffset, ref waistOffset);
            if (hasWaistWidth && hasWaistOffset)
            {
                if (waistWidth <= 0)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Waist width must be greater than zero.");
                    return;
                }
                if (waistOffset < 0 || waistOffset > length)
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Waist center line is outside the body length.");

                double half = waistWidth / 2;
                lines.Add(new Line(new Point3d(-half, waistOffset, 0), new Point3d(half, waistOffset, 0)));

                // Radius centre is one radius outside the width, so the circle touches the waist from outside the body.
                double waistRadius = 0;
                if (DA.GetData(InWaistRadius, ref waistRadius))
                {
                    if (waistRadius <= 0)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Waist radius must be greater than zero.");
                        return;
                    }
                    AddMirroredCircles(circles, half + waistRadius, waistOffset, waistRadius);
                    waistCircle = new Circle(new Point3d(half + waistRadius, waistOffset, 0), waistRadius);
                }
            }
            else if (hasWaistWidth || hasWaistOffset)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Waist width and waist offset are both needed to draw the waist center line.");
            }

            // Lower bout: horizontal center line centred on the Y axis. The primary radius centre sits one radius above
            // the tail end; Lower Bout Offset moves the line up from there.
            double lbWidth = 0, lbOffset = 0, lbRadius = 0;
            Line? tailLine = null;
            Arc? tailArc = null;
            bool hasLbWidth = DA.GetData(InLbWidth, ref lbWidth);
            bool hasLbOffset = DA.GetData(InLbOffset, ref lbOffset);
            bool hasLbRadius = DA.GetData(InLbRadius, ref lbRadius);
            if (hasLbWidth && hasLbOffset && hasLbRadius)
            {
                if (lbWidth <= 0)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Lower bout width must be greater than zero.");
                    return;
                }
                if (lbRadius <= 0)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Lower bout primary radius must be greater than zero.");
                    return;
                }
                double lbY = lbRadius + lbOffset;
                if (lbY < 0 || lbY > length)
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Lower bout center line is outside the body length.");

                double half = lbWidth / 2;
                lines.Add(new Line(new Point3d(-half, lbY, 0), new Point3d(half, lbY, 0)));

                // Radius centre is one radius inside the width, so the circle touches the edge of the width.
                if (lbRadius > half)
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Lower bout primary radius is larger than half the lower bout width.");
                AddMirroredCircles(circles, half - lbRadius, lbY, lbRadius);
                lbPrimary = new Circle(new Point3d(half - lbRadius, lbY, 0), lbRadius);

                lbSecondary = AddSecondaryCircles(DA, InLbSecondaryOffset, "Lower bout", circles, half - lbRadius, lbY, lbRadius);
                if (RuntimeMessageLevel == GH_RuntimeMessageLevel.Error) return;

                // Tail end: when the lower bout primary circles sit on the end of the body it is a straight line between
                // their bottoms. When they are offset upward it is one arc through the origin, tangent to both circles.
                double tolerance = DocumentTolerance();
                double lbCentreX = half - lbRadius;
                if (lbOffset < -tolerance)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                        "The lower bout circles reach below the end of the body. Lower bout offset can't be negative.");
                    return;
                }
                if (lbOffset <= tolerance)
                {
                    lbOutlineEnd = new Point3d(lbCentreX, 0, 0);
                    if (lbCentreX > tolerance)
                        tailLine = new Line(new Point3d(-lbCentreX, 0, 0), new Point3d(lbCentreX, 0, 0));
                }
                else
                {
                    // The arc passes through the origin heading sideways, so its centre is on the Y axis above it. It
                    // wraps each primary circle and touches it from outside: |arcCentre - circleCentre| = arcRadius - lbRadius.
                    // The tangent point sits below the lower bout line only while lbCentreX > lbOffset.
                    if (lbCentreX < lbOffset)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                            $"The tail arc meets the lower bout radius above its centre, so it would run wider than the lower bout width. " +
                            $"The lower bout radius centre's distance from the centerline ({lbCentreX:0.###}) must be at least the lower bout offset ({lbOffset:0.###}). " +
                            "Reduce the lower bout offset or the lower bout primary radius, or widen the lower bout.");
                        return;
                    }
                    double arcRadius = (lbCentreX * lbCentreX + lbY * lbY - lbRadius * lbRadius) / (2 * lbOffset);

                    var arcCentre = new Point3d(0, arcRadius, 0);
                    var circleCentre = new Point3d(lbCentreX, lbY, 0);
                    var toCircle = circleCentre - arcCentre;
                    toCircle.Unitize();
                    var tangentPoint = circleCentre + toCircle * lbRadius;

                    tailArc = new Arc(new Point3d(-tangentPoint.X, tangentPoint.Y, 0), Point3d.Origin, tangentPoint);
                    lbOutlineEnd = tangentPoint;
                }
            }
            else if (hasLbWidth || hasLbOffset || hasLbRadius)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Lower bout width, offset and primary radius are all needed to draw the lower bout center line.");
            }

            // Waist tangent lines: final outline from each bout secondary circle to the waist circle, right then left.
            // The right-side tangents are kept for the bout and waist outline segments. A tangent that has shrunk to a
            // point (circles touching) is not drawn as a line.
            var waistTangents = new List<Line>();
            Line? ubWaistTangent = null, lbWaistTangent = null;
            if (waistCircle.HasValue)
            {
                if (ubSecondary.HasValue &&
                    !CheckSecondaryClearsWaist(ubSecondary.Value, waistCircle.Value, "Upper bout", "upper bout secondary offset"))
                    return;
                if (lbSecondary.HasValue &&
                    !CheckSecondaryClearsWaist(lbSecondary.Value, waistCircle.Value, "Lower bout", "lower bout secondary offset"))
                    return;

                if (ubSecondary.HasValue)
                    ubWaistTangent = WaistTangentLine(ubSecondary.Value, waistCircle.Value, DocumentTolerance());
                if (lbSecondary.HasValue)
                    lbWaistTangent = WaistTangentLine(lbSecondary.Value, waistCircle.Value, DocumentTolerance());

                foreach (var tangent in new[] { ubWaistTangent, lbWaistTangent })
                {
                    if (!tangent.HasValue || tangent.Value.Length <= DocumentTolerance()) continue;
                    waistTangents.Add(tangent.Value);
                    waistTangents.Add(MirrorX(tangent.Value));
                }
            }

            // Heel flat: horizontal line at the top of the body, Heel Width long. When the upper bout circle tops are level
            // with the top of the body, it is lengthened to the upper bout radius centres. When they sit below the top, a
            // tangent arc runs from each end of the flat (G1 with the flat) to the upper bout circle.
            // Short vertical marks show the heel width ends.
            var arcs = new List<Arc>();
            var outlineLines = new List<Line>();
            double heelWidth = 0;
            if (DA.GetData(InHeelWidth, ref heelWidth))
            {
                if (heelWidth <= 0)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Heel width must be greater than zero.");
                    return;
                }

                double heelHalf = heelWidth / 2;
                double flatHalf = heelHalf;
                if (ubCentreX.HasValue)
                {
                    double centreSpacing = 2 * ubCentreX.Value;
                    if (centreSpacing < heelWidth)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                            $"The upper bout radius centres are {centreSpacing:0.###} apart, which is less than the heel width ({heelWidth:0.###}). " +
                            "Reduce the upper bout primary radius, widen the upper bout, or reduce the heel width.");
                        return;
                    }

                    double tolerance = DocumentTolerance();
                    if (ubOffset < -tolerance)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                            "The upper bout circles reach above the top of the body. Upper bout offset can't be negative when a heel width is set.");
                        return;
                    }

                    if (ubOffset <= tolerance)
                    {
                        // Circle tops are level with the top of the body: the flat runs out to them.
                        flatHalf = ubCentreX.Value;
                        ubOutlineStart = new Point3d(ubCentreX.Value, length, 0);
                    }
                    else
                    {
                        // Shoulder arc: starts at the flat end heading outward, so its centre is directly below that
                        // point. It wraps the upper bout circle and touches it from outside:
                        // |arcCentre - circleCentre| = arcRadius - ubRadius.
                        double dx = ubCentreX.Value - heelHalf;
                        // The tangent point sits above the upper bout radius centre only while dx > ubOffset.
                        if (dx < ubOffset)
                        {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                $"The shoulder arc meets the upper bout radius below its centre, so it would run wider than the upper bout width. " +
                                $"The gap from the heel flat end to the upper bout radius centre ({dx:0.###}) must be at least the upper bout offset ({ubOffset:0.###}). " +
                                "Reduce the upper bout offset or the heel width, or widen the upper bout.");
                            return;
                        }
                        double t = length - ubCentreY; // = ubRadius + ubOffset
                        double arcRadius = (dx * dx + t * t - ubRadius * ubRadius) / (2 * (t - ubRadius));

                        var arcCentre = new Point3d(heelHalf, length - arcRadius, 0);
                        var circleCentre = new Point3d(ubCentreX.Value, ubCentreY, 0);
                        var toCircle = circleCentre - arcCentre;
                        toCircle.Unitize();
                        var tangentPoint = circleCentre + toCircle * ubRadius;

                        arcs.Add(new Arc(new Point3d(heelHalf, length, 0), Vector3d.XAxis, tangentPoint));
                        ubOutlineStart = tangentPoint;
                        arcs.Add(new Arc(new Point3d(-heelHalf, length, 0), -Vector3d.XAxis,
                            new Point3d(-tangentPoint.X, tangentPoint.Y, 0)));
                    }
                }

                outlineLines.Add(new Line(new Point3d(-flatHalf, length, 0), new Point3d(flatHalf, length, 0)));

                double markHalf = HeelMarkLength / 2;
                lines.Add(new Line(new Point3d(heelHalf, length - markHalf, 0), new Point3d(heelHalf, length + markHalf, 0)));
                lines.Add(new Line(new Point3d(-heelHalf, length - markHalf, 0), new Point3d(-heelHalf, length + markHalf, 0)));
            }

            outlineLines.AddRange(waistTangents);
            if (tailLine.HasValue) outlineLines.Add(tailLine.Value);

            // Outline segments of the bout and waist circles, top to bottom, right then left for each. Each segment runs
            // between two points where the outline meets that circle, and is skipped if either point isn't known.
            if (ubPrimary.HasValue)
            {
                var ubWidest = ubPrimary.Value.Center + Vector3d.XAxis * ubPrimary.Value.Radius;
                if (ubOutlineStart.HasValue)
                    AddMirroredSegment(arcs, ubPrimary.Value, ubOutlineStart.Value, ubWidest, clockwise: true);
                if (ubSecondary.HasValue && ubWaistTangent.HasValue)
                    AddMirroredSegment(arcs, ubSecondary.Value, ubWidest, ubWaistTangent.Value.From, clockwise: true);
            }
            if (waistCircle.HasValue && ubWaistTangent.HasValue && lbWaistTangent.HasValue)
                AddMirroredSegment(arcs, waistCircle.Value, ubWaistTangent.Value.To, lbWaistTangent.Value.To, clockwise: false);
            if (lbPrimary.HasValue)
            {
                var lbWidest = lbPrimary.Value.Center + Vector3d.XAxis * lbPrimary.Value.Radius;
                if (lbSecondary.HasValue && lbWaistTangent.HasValue)
                    AddMirroredSegment(arcs, lbSecondary.Value, lbWaistTangent.Value.From, lbWidest, clockwise: true);
                if (lbOutlineEnd.HasValue)
                    AddMirroredSegment(arcs, lbPrimary.Value, lbWidest, lbOutlineEnd.Value, clockwise: true);
            }

            if (tailArc.HasValue) arcs.Add(tailArc.Value);

            _constructionLines.AddRange(lines);
            _outlineCircles.AddRange(circles);
            _outlineArcs.AddRange(arcs);
            _outlineLines.AddRange(outlineLines);
            DA.SetDataList(0, lines);
            DA.SetDataList(1, circles);
            DA.SetDataList(2, arcs);
            DA.SetDataList(3, outlineLines);
        }

        public override BoundingBox ClippingBox
        {
            get
            {
                var box = BoundingBox.Empty;
                foreach (var line in _constructionLines)
                    box.Union(line.BoundingBox);
                foreach (var circle in _outlineCircles)
                    box.Union(circle.BoundingBox);
                foreach (var arc in _outlineArcs)
                    box.Union(arc.BoundingBox());
                foreach (var line in _outlineLines)
                    box.Union(line.BoundingBox);
                return box;
            }
        }

        // Construction geometry is drawn light grey; the final outline (Outline Lines and Outline Arcs) uses the default
        // Grasshopper preview colour.
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (Hidden || Locked) return;
            foreach (var line in _constructionLines)
                args.Display.DrawLine(line, ConstructionColor, 1);
            foreach (var circle in _outlineCircles)
                args.Display.DrawCircle(circle, ConstructionColor, 1);
            foreach (var arc in _outlineArcs)
                args.Display.DrawArc(arc, args.WireColour, args.DefaultCurveThickness);
            foreach (var line in _outlineLines)
                args.Display.DrawLine(line, args.WireColour, args.DefaultCurveThickness);
        }

        // Baking keeps construction geometry light grey with a solid (Continuous) linetype; the final outline bakes with the
        // default attributes.
        public override bool IsBakeCapable =>
            _constructionLines.Count > 0 || _outlineCircles.Count > 0 || _outlineArcs.Count > 0 || _outlineLines.Count > 0;

        public override void BakeGeometry(RhinoDoc doc, List<Guid> obj_ids)
        {
            BakeGeometry(doc, doc.CreateDefaultAttributes(), obj_ids);
        }

        public override void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            var outlineAttributes = att ?? doc.CreateDefaultAttributes();
            var attributes = outlineAttributes.Duplicate();
            attributes.ColorSource = ObjectColorSource.ColorFromObject;
            attributes.ObjectColor = ConstructionColor;
            attributes.LinetypeSource = ObjectLinetypeSource.LinetypeFromObject;
            attributes.LinetypeIndex = -1; // Continuous

            foreach (var line in _constructionLines)
                obj_ids.Add(doc.Objects.AddLine(line, attributes));
            foreach (var circle in _outlineCircles)
                obj_ids.Add(doc.Objects.AddCircle(circle, attributes));
            foreach (var arc in _outlineArcs)
                obj_ids.Add(doc.Objects.AddArc(arc, outlineAttributes));
            foreach (var line in _outlineLines)
                obj_ids.Add(doc.Objects.AddLine(line, outlineAttributes));
        }

        static readonly Bitmap PlateIcon = Icons.Load("Plate.png");

        protected override Bitmap Icon => PlateIcon;

        public override Guid ComponentGuid => new Guid("0b87450f-df46-4399-bedc-2321bb4d748c");
    }
}
