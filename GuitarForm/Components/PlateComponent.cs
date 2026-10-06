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
        // Construction lines are drawn as solid red lines.
        static readonly Color ConstructionColor = Color.Red;

        // Length of the vertical marks at each end of the heel width, centred on the top of the body.
        const double HeelMarkLength = 0.25;

        readonly List<Line> _constructionLines = new List<Line>();
        readonly List<Circle> _outlineCircles = new List<Circle>();
        readonly List<Arc> _outlineArcs = new List<Arc>();

        public PlateComponent()
          : base("Plate", "Plate",
              "Guitar body plate. Outputs construction lines driven by the body dimensions.",
              "GuitarForm", "Body")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Body Length", "L", "Overall length of the guitar body", GH_ParamAccess.item);
            pManager.AddNumberParameter("Upper Bout Width", "UbW", "Width of the upper bout", GH_ParamAccess.item);
            pManager.AddNumberParameter("Upper Bout Offset", "UbO", "Offset of the upper bout line down the Y axis from the upper bout primary radius centre", GH_ParamAccess.item);
            pManager.AddNumberParameter("Upper Bout Primary Radius", "UbR", "Primary radius of the upper bout. Its centre sits one radius below the top of the body length line", GH_ParamAccess.item);
            pManager.AddNumberParameter("Waist Radius", "WR", "Radius of the waist curve. Its centre sits on the waist center line, one radius outside the waist width", GH_ParamAccess.item);
            pManager.AddNumberParameter("Waist Width", "WW", "Width of the body at the waist", GH_ParamAccess.item);
            pManager.AddNumberParameter("Waist Offset", "WO", "Distance of the waist center line up the Y axis from the tail end (the origin)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Lower Bout Width", "LbW", "Width of the lower bout", GH_ParamAccess.item);
            pManager.AddNumberParameter("Lower Bout Offset", "LbO", "Offset of the lower bout center line up the Y axis from the lower bout primary radius centre", GH_ParamAccess.item);
            pManager.AddNumberParameter("Lower Bout Primary Radius", "LbR", "Primary radius of the lower bout. Its centre sits one radius above the tail end (the origin)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Heel Width", "HW", "Width of the flat at the top of the body where the heel of the neck attaches", GH_ParamAccess.item);
            for (int i = 1; i < pManager.ParamCount; i++)
                pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Construction Lines", "CL", "Construction lines for the plate", GH_ParamAccess.list);
            pManager.AddCircleParameter("Outline Radii", "OR", "Circles for the upper bout, waist and lower bout radii, right side then left side for each", GH_ParamAccess.list);
            pManager.AddArcParameter("Outline Arcs", "OA", "Shoulder arcs from the heel flat ends to the upper bout radii, right side then left side", GH_ParamAccess.list);
        }

        protected override void BeforeSolveInstance()
        {
            _constructionLines.Clear();
            _outlineCircles.Clear();
            _outlineArcs.Clear();
        }

        // Adds a circle on each side of the Y axis, centred at (±centreX, y).
        static void AddMirroredCircles(List<Circle> circles, double centreX, double y, double radius)
        {
            circles.Add(new Circle(new Point3d(centreX, y, 0), radius));
            circles.Add(new Circle(new Point3d(-centreX, y, 0), radius));
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double length = 0;
            if (!DA.GetData(0, ref length)) return;

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

            // Upper bout: horizontal line centred on the Y axis. The primary radius centre sits one radius below the
            // top of the body; Upper Bout Offset moves the line down from there.
            double ubWidth = 0, ubOffset = 0, ubRadius = 0;
            bool hasUbWidth = DA.GetData(1, ref ubWidth);
            bool hasUbOffset = DA.GetData(2, ref ubOffset);
            bool hasUbRadius = DA.GetData(3, ref ubRadius);
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
            }
            else if (hasUbWidth || hasUbOffset || hasUbRadius)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Upper bout width, offset and primary radius are all needed to draw the upper bout line.");
            }

            // Waist: horizontal center line centred on the Y axis, Waist Offset up from the tail end.
            double waistWidth = 0, waistOffset = 0;
            bool hasWaistWidth = DA.GetData(5, ref waistWidth);
            bool hasWaistOffset = DA.GetData(6, ref waistOffset);
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
                if (DA.GetData(4, ref waistRadius))
                {
                    if (waistRadius <= 0)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Waist radius must be greater than zero.");
                        return;
                    }
                    AddMirroredCircles(circles, half + waistRadius, waistOffset, waistRadius);
                }
            }
            else if (hasWaistWidth || hasWaistOffset)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Waist width and waist offset are both needed to draw the waist center line.");
            }

            // Lower bout: horizontal center line centred on the Y axis. The primary radius centre sits one radius above
            // the tail end; Lower Bout Offset moves the line up from there.
            double lbWidth = 0, lbOffset = 0, lbRadius = 0;
            bool hasLbWidth = DA.GetData(7, ref lbWidth);
            bool hasLbOffset = DA.GetData(8, ref lbOffset);
            bool hasLbRadius = DA.GetData(9, ref lbRadius);
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
            }
            else if (hasLbWidth || hasLbOffset || hasLbRadius)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Lower bout width, offset and primary radius are all needed to draw the lower bout center line.");
            }

            // Heel flat: horizontal line at the top of the body, Heel Width long. When the upper bout circle tops are level
            // with the top of the body, it is lengthened to the upper bout radius centres. When they sit below the top, a
            // tangent arc runs from each end of the flat (G1 with the flat) to the upper bout circle.
            // Short vertical marks show the heel width ends.
            var arcs = new List<Arc>();
            double heelWidth = 0;
            if (DA.GetData(10, ref heelWidth))
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
                    }
                    else
                    {
                        // Shoulder arc: starts at the flat end heading outward, so its centre is directly below that
                        // point. It wraps the upper bout circle and touches it from outside:
                        // |arcCentre - circleCentre| = arcRadius - ubRadius.
                        double dx = ubCentreX.Value - heelHalf;
                        if (dx <= ubOffset)
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                                "The shoulder arc meets the upper bout radius below the upper bout line, so it runs wider than the upper bout width. " +
                                "Reduce the upper bout offset or the heel width.");
                        double t = length - ubCentreY; // = ubRadius + ubOffset
                        double arcRadius = (dx * dx + t * t - ubRadius * ubRadius) / (2 * (t - ubRadius));

                        var arcCentre = new Point3d(heelHalf, length - arcRadius, 0);
                        var circleCentre = new Point3d(ubCentreX.Value, ubCentreY, 0);
                        var toCircle = circleCentre - arcCentre;
                        toCircle.Unitize();
                        var tangentPoint = circleCentre + toCircle * ubRadius;

                        arcs.Add(new Arc(new Point3d(heelHalf, length, 0), Vector3d.XAxis, tangentPoint));
                        arcs.Add(new Arc(new Point3d(-heelHalf, length, 0), -Vector3d.XAxis,
                            new Point3d(-tangentPoint.X, tangentPoint.Y, 0)));
                    }
                }

                lines.Add(new Line(new Point3d(-flatHalf, length, 0), new Point3d(flatHalf, length, 0)));

                double markHalf = HeelMarkLength / 2;
                lines.Add(new Line(new Point3d(heelHalf, length - markHalf, 0), new Point3d(heelHalf, length + markHalf, 0)));
                lines.Add(new Line(new Point3d(-heelHalf, length - markHalf, 0), new Point3d(-heelHalf, length + markHalf, 0)));
            }

            _constructionLines.AddRange(lines);
            _outlineCircles.AddRange(circles);
            _outlineArcs.AddRange(arcs);
            DA.SetDataList(0, lines);
            DA.SetDataList(1, circles);
            DA.SetDataList(2, arcs);
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
                return box;
            }
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (Hidden || Locked) return;
            foreach (var line in _constructionLines)
                args.Display.DrawLine(line, ConstructionColor, 1);
            foreach (var circle in _outlineCircles)
                args.Display.DrawCircle(circle, ConstructionColor, 1);
            foreach (var arc in _outlineArcs)
                args.Display.DrawArc(arc, ConstructionColor, 1);
        }

        // Baking keeps the red colour and a solid (Continuous) linetype.
        public override bool IsBakeCapable => _constructionLines.Count > 0 || _outlineCircles.Count > 0 || _outlineArcs.Count > 0;

        public override void BakeGeometry(RhinoDoc doc, List<Guid> obj_ids)
        {
            BakeGeometry(doc, doc.CreateDefaultAttributes(), obj_ids);
        }

        public override void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            var attributes = (att ?? doc.CreateDefaultAttributes()).Duplicate();
            attributes.ColorSource = ObjectColorSource.ColorFromObject;
            attributes.ObjectColor = ConstructionColor;
            attributes.LinetypeSource = ObjectLinetypeSource.LinetypeFromObject;
            attributes.LinetypeIndex = -1; // Continuous

            foreach (var line in _constructionLines)
                obj_ids.Add(doc.Objects.AddLine(line, attributes));
            foreach (var circle in _outlineCircles)
                obj_ids.Add(doc.Objects.AddCircle(circle, attributes));
            foreach (var arc in _outlineArcs)
                obj_ids.Add(doc.Objects.AddArc(arc, attributes));
        }

        protected override Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("0b87450f-df46-4399-bedc-2321bb4d748c");
    }
}
