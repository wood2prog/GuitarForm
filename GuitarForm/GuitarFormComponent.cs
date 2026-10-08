using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using GuitarForm.Geometry;
using GuitarForm.Model;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace GuitarForm
{
    // Base class for GuitarForm components. Holds the geometry from the last solution and handles its preview and
    // baking: construction geometry is drawn and baked light grey with a solid linetype, and the final outline uses the
    // default Grasshopper preview colour and bakes with the default attributes.
    public abstract class GuitarFormComponent : GH_Component
    {
        // Construction geometry is drawn in light grey so the final outline stands out.
        protected static readonly Color ConstructionColor = Color.FromArgb(200, 200, 200);

        readonly List<Curve> _construction = new List<Curve>();
        readonly List<Curve> _outline = new List<Curve>();

        protected GuitarFormComponent(string name, string nickname, string description, string subCategory)
          : base(name, nickname, description, "GuitarForm", subCategory)
        {
        }

        // Geometry to preview and bake, in drawing order. Add to these from SolveInstance; they're cleared before
        // each solution.
        protected void AddConstruction(IEnumerable<Curve> curves) => _construction.AddRange(curves);

        protected void AddOutline(IEnumerable<Curve> curves) => _outline.AddRange(curves);

        // Reads an optional number input: null when it isn't connected or has no data.
        protected static double? GetOptionalNumber(IGH_DataAccess DA, int index)
        {
            double value = 0;
            return DA.GetData(index, ref value) ? value : null;
        }

        // Shows messages from building geometry as runtime messages of the same level.
        protected void AddMessages(IEnumerable<GeometryMessage> messages)
        {
            foreach (var message in messages)
            {
                var level = message.Level switch
                {
                    MessageLevel.Error => GH_RuntimeMessageLevel.Error,
                    MessageLevel.Warning => GH_RuntimeMessageLevel.Warning,
                    _ => GH_RuntimeMessageLevel.Remark,
                };
                AddRuntimeMessage(level, message.Text);
            }
        }

        protected override void BeforeSolveInstance()
        {
            _construction.Clear();
            _outline.Clear();
            base.BeforeSolveInstance();
        }

        public override BoundingBox ClippingBox
        {
            get
            {
                var box = BoundingBox.Empty;
                foreach (var curve in _construction)
                    box.Union(curve.GetBoundingBox(false));
                foreach (var curve in _outline)
                    box.Union(curve.GetBoundingBox(false));
                return box;
            }
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (Hidden || Locked) return;
            foreach (var curve in _construction)
                args.Display.DrawCurve(curve, ConstructionColor, 1);
            foreach (var curve in _outline)
                args.Display.DrawCurve(curve, args.WireColour, args.DefaultCurveThickness);
        }

        public override bool IsBakeCapable => _construction.Count > 0 || _outline.Count > 0;

        public override void BakeGeometry(RhinoDoc doc, List<Guid> obj_ids)
        {
            BakeGeometry(doc, doc.CreateDefaultAttributes(), obj_ids);
        }

        public override void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            var outlineAttributes = att ?? doc.CreateDefaultAttributes();
            var constructionAttributes = outlineAttributes.Duplicate();
            constructionAttributes.ColorSource = ObjectColorSource.ColorFromObject;
            constructionAttributes.ObjectColor = ConstructionColor;
            constructionAttributes.LinetypeSource = ObjectLinetypeSource.LinetypeFromObject;
            constructionAttributes.LinetypeIndex = -1; // Continuous

            foreach (var curve in _construction)
                obj_ids.Add(doc.Objects.AddCurve(curve, constructionAttributes));
            foreach (var curve in _outline)
                obj_ids.Add(doc.Objects.AddCurve(curve, outlineAttributes));
        }
    }
}
