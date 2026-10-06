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

        readonly List<Line> _constructionLines = new List<Line>();

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
            pManager.AddNumberParameter("Upper Bout Position", "UbP", "Offset of the upper bout line down the Y axis from the upper bout primary radius centre", GH_ParamAccess.item);
            pManager.AddNumberParameter("Upper Bout Primary Radius", "UbR", "Primary radius of the upper bout. Its centre sits one radius below the top of the body length line", GH_ParamAccess.item);
            pManager.AddNumberParameter("Waist Width", "WW", "Width of the body at the waist", GH_ParamAccess.item);
            pManager.AddNumberParameter("Waist Offset", "WO", "Distance of the waist center line up the Y axis from the tail end (the origin)", GH_ParamAccess.item);
            for (int i = 1; i < pManager.ParamCount; i++)
                pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Construction Lines", "CL", "Construction lines for the plate", GH_ParamAccess.list);
        }

        protected override void BeforeSolveInstance()
        {
            _constructionLines.Clear();
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

            // Upper bout: horizontal line centred on the Y axis. The primary radius centre sits one radius below the
            // top of the body; Upper Bout Position offsets the line down from there.
            double ubWidth = 0, ubPosition = 0, ubRadius = 0;
            bool hasUbWidth = DA.GetData(1, ref ubWidth);
            bool hasUbPosition = DA.GetData(2, ref ubPosition);
            bool hasUbRadius = DA.GetData(3, ref ubRadius);
            if (hasUbWidth && hasUbPosition && hasUbRadius)
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
                double ubY = length - ubRadius - ubPosition;
                if (ubY < 0 || ubY > length)
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Upper bout line is outside the body length.");

                double half = ubWidth / 2;
                lines.Add(new Line(new Point3d(-half, ubY, 0), new Point3d(half, ubY, 0)));
            }
            else if (hasUbWidth || hasUbPosition || hasUbRadius)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Upper bout width, position and primary radius are all needed to draw the upper bout line.");
            }

            // Waist: horizontal center line centred on the Y axis, Waist Offset up from the tail end.
            double waistWidth = 0, waistOffset = 0;
            bool hasWaistWidth = DA.GetData(4, ref waistWidth);
            bool hasWaistOffset = DA.GetData(5, ref waistOffset);
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
            }
            else if (hasWaistWidth || hasWaistOffset)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Waist width and waist offset are both needed to draw the waist center line.");
            }

            _constructionLines.AddRange(lines);
            DA.SetDataList(0, lines);
        }

        public override BoundingBox ClippingBox
        {
            get
            {
                var box = BoundingBox.Empty;
                foreach (var line in _constructionLines)
                    box.Union(line.BoundingBox);
                return box;
            }
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (Hidden || Locked) return;
            foreach (var line in _constructionLines)
                args.Display.DrawLine(line, ConstructionColor, 1);
        }

        // Baking keeps the red colour and a solid (Continuous) linetype.
        public override bool IsBakeCapable => _constructionLines.Count > 0;

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
        }

        protected override Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("0b87450f-df46-4399-bedc-2321bb4d748c");
    }
}
