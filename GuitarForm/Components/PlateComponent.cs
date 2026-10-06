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
            pManager.AddNumberParameter("Upper Bout Position", "UbP", "Distance of the upper bout line from the top of the body length line, measured down the Y axis", GH_ParamAccess.item);
            pManager[1].Optional = true;
            pManager[2].Optional = true;
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

            // Upper bout: horizontal line centred on the Y axis, Upper Bout Position below the top of the body.
            double ubWidth = 0, ubPosition = 0;
            bool hasUbWidth = DA.GetData(1, ref ubWidth);
            bool hasUbPosition = DA.GetData(2, ref ubPosition);
            if (hasUbWidth && hasUbPosition)
            {
                if (ubWidth <= 0)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Upper bout width must be greater than zero.");
                    return;
                }
                if (ubPosition < 0 || ubPosition > length)
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Upper bout position is outside the body length.");

                double half = ubWidth / 2;
                double ubY = length - ubPosition;
                lines.Add(new Line(new Point3d(-half, ubY, 0), new Point3d(half, ubY, 0)));
            }
            else if (hasUbWidth || hasUbPosition)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Upper bout width and upper bout position are both needed to draw the upper bout line.");
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
