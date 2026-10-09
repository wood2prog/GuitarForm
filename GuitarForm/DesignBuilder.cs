using System.Drawing;
using System.Globalization;
using System.Linq;
using GuitarForm.Geometry;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace GuitarForm
{
    // Writes a design's drawing into the document on the GuitarForm::Outline and GuitarForm::Construction layers. Each
    // object is tagged with the design's id, so building the design again replaces only its own objects. One undo step.
    static class DesignBuilder
    {
        public const string DesignIdKey = "GuitarForm.DesignId";

        // Returns the number of objects added.
        public static int Build(RhinoDoc doc, long designId, string designName, DesignDrawing drawing)
        {
            string id = designId.ToString(CultureInfo.InvariantCulture);
            var toDocument = DesignUnits.ToDocument(doc.ModelUnitSystem);
            uint undo = doc.BeginUndoRecord($"GuitarForm Build {designName}");
            try
            {
                var parent = doc.Layers[FindOrAddLayer(doc, "GuitarForm", null, Color.Black)];
                int outlineLayer = FindOrAddLayer(doc, "Outline", parent, Color.Black);
                int constructionLayer = FindOrAddLayer(doc, "Construction", parent, DesignPreview.ConstructionColor);

                var previous = doc.Objects
                    .GetObjectList(new ObjectEnumeratorSettings { HiddenObjects = true, LockedObjects = true })
                    .Where(o => o.Attributes.GetUserString(DesignIdKey) == id)
                    .ToList();
                foreach (var obj in previous)
                    doc.Objects.Delete(obj, true, true);

                int added = 0;
                foreach (var curve in drawing.Construction)
                    added += Add(doc, curve, toDocument, constructionLayer, id, designName);
                foreach (var curve in drawing.Outline)
                    added += Add(doc, curve, toDocument, outlineLayer, id, designName);
                return added;
            }
            finally
            {
                doc.EndUndoRecord(undo);
                doc.Views.Redraw();
            }
        }

        static int Add(RhinoDoc doc, Curve curve, Transform toDocument, int layer, string id, string name)
        {
            var copy = curve.DuplicateCurve();
            copy.Transform(toDocument);
            var attributes = new ObjectAttributes { LayerIndex = layer, Name = name };
            attributes.SetUserString(DesignIdKey, id);
            return doc.Objects.AddCurve(copy, attributes) == System.Guid.Empty ? 0 : 1;
        }

        // A new layer gets the colour given and the default (Continuous) linetype.
        static int FindOrAddLayer(RhinoDoc doc, string name, Layer parent, Color color)
        {
            string fullPath = parent == null ? name : parent.FullPath + ModelComponent.NamePathSeparator + name;
            int index = doc.Layers.FindByFullPath(fullPath, -1);
            if (index >= 0)
                return index;
            var layer = new Layer { Name = name, Color = color };
            if (parent != null)
                layer.ParentLayerId = parent.Id;
            return doc.Layers.Add(layer);
        }
    }
}
