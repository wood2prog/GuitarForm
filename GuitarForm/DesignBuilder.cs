using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using GuitarForm.Geometry;
using GuitarForm.Shapes;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace GuitarForm
{
    // Writes a design's drawing into the document on the layer tree that ComponentStyles describes. Each object is
    // tagged with the design's id, so building the design again replaces only its own objects. One undo step.
    static class DesignBuilder
    {
        public const string DesignIdKey = "GuitarForm.DesignId";

        // The layers earlier versions built on: removed after a build when nothing is left on them.
        static readonly string[] OldLayers = { "GuitarForm::Outline", "GuitarForm::Construction", "GuitarForm" };

        // Returns the number of objects added. Strings are left out unless showStrings.
        public static int Build(RhinoDoc doc, long designId, string designName, DesignDrawing drawing, bool showStrings = true)
        {
            string id = designId.ToString(CultureInfo.InvariantCulture);
            var toDocument = DesignUnits.ToDocument(doc.ModelUnitSystem);
            uint undo = doc.BeginUndoRecord($"GuitarForm Build {designName}");
            try
            {
                var wanted = drawing.Curves.Where(c => showStrings || c.Component != DrawingComponent.Strings).ToList();

                // Every layer first: a failure here throws before anything has been deleted.
                var layers = new Dictionary<string, int>();
                foreach (var drawn in wanted)
                {
                    var style = ComponentStyles.For(drawn.Component, drawn.Role);
                    string key = string.Join(ModelComponent.NamePathSeparator, style.LayerPath);
                    if (!layers.ContainsKey(key))
                        layers[key] = FindOrAddLayer(doc, style);
                }

                // Earlier objects of this design, wherever they are (including the old layers).
                var previous = doc.Objects
                    .GetObjectList(new ObjectEnumeratorSettings
                    {
                        HiddenObjects = true,
                        LockedObjects = true,
                        ReferenceObjects = false,
                        IncludeLights = false,
                    })
                    .Where(o => o.Attributes.GetUserString(DesignIdKey) == id)
                    .ToList();
                foreach (var obj in previous)
                    doc.Objects.Delete(obj, true, true);

                int added = 0;
                foreach (var drawn in wanted)
                {
                    var style = ComponentStyles.For(drawn.Component, drawn.Role);
                    string key = string.Join(ModelComponent.NamePathSeparator, style.LayerPath);
                    added += Add(doc, drawn, toDocument, layers[key], id, designName);
                }

                RemoveEmptyOldLayers(doc);
                return added;
            }
            finally
            {
                doc.EndUndoRecord(undo);
                doc.Views.Redraw();
            }
        }

        static int Add(RhinoDoc doc, DrawnCurve drawn, Transform toDocument, int layer, string id, string name)
        {
            var copy = drawn.Curve.DuplicateCurve();
            copy.Transform(toDocument);
            var attributes = new ObjectAttributes
            {
                LayerIndex = layer,
                Name = name,
                ColorSource = ObjectColorSource.ColorFromLayer,
                PlotWeightSource = ObjectPlotWeightSource.PlotWeightFromObject,
                PlotWeight = ComponentStyles.PlotWeight(drawn.Role),
            };
            attributes.SetUserString(DesignIdKey, id);
            return doc.Objects.AddCurve(copy, attributes) == System.Guid.Empty ? 0 : 1;
        }

        // Finds the layer for the style's path, adding it and any missing parents (root first). Colours are set only
        // on a layer this adds; an existing layer is left as the user has it.
        static int FindOrAddLayer(RhinoDoc doc, ComponentStyle style)
        {
            Layer parent = null;
            int index = -1;
            for (int depth = 0; depth < style.LayerPath.Length; depth++)
            {
                string fullPath = string.Join(ModelComponent.NamePathSeparator, style.LayerPath.Take(depth + 1));
                index = doc.Layers.FindByFullPath(fullPath, -1);
                if (index < 0)
                {
                    bool leaf = depth == style.LayerPath.Length - 1;
                    var layer = new Layer { Name = style.LayerPath[depth] };
                    ComponentStyles.ApplyNewLayerStyle(layer, style, leaf);
                    if (parent != null)
                        layer.ParentLayerId = parent.Id;
                    index = doc.Layers.Add(layer);
                    if (index < 0)
                        throw new System.InvalidOperationException($"Couldn't add the layer \"{layer.Name}\" to the document.");
                }
                parent = doc.Layers[index];
            }
            return index;
        }

        // Deletes the old GuitarForm::Outline and GuitarForm::Construction layers, then their parent, but only a layer
        // with nothing on it, no sublayers left, that isn't the current layer and isn't used by a block definition.
        // A layer with any object is never deleted. Children are looked at again after each delete.
        static void RemoveEmptyOldLayers(RhinoDoc doc)
        {
            foreach (string path in OldLayers)
            {
                int index = doc.Layers.FindByFullPath(path, -1);
                if (index < 0)
                    continue;
                var layer = doc.Layers[index];
                if (index == doc.Layers.CurrentLayerIndex)
                    continue;
                if (layer.GetChildren()?.Any(child => !child.IsDeleted) == true)
                    continue;
                bool hasObjects = doc.Objects.GetObjectList(new ObjectEnumeratorSettings
                {
                    HiddenObjects = true,
                    LockedObjects = true,
                    IncludeLights = true,
                    IncludeGrips = true,
                    ReferenceObjects = true,
                    LayerIndexFilter = index,
                }).Any();
                if (hasObjects || layer.IsReference || UsedByBlockDefinition(doc, index))
                    continue;
                if (!doc.Layers.Delete(index, true))
                    RhinoApp.WriteLine($"GuitarForm: couldn't remove the empty layer \"{path}\".");
            }
        }

        static bool UsedByBlockDefinition(RhinoDoc doc, int layerIndex) =>
            doc.InstanceDefinitions.Any(definition => !definition.IsDeleted &&
                definition.GetObjects().Any(o => o.Attributes.LayerIndex == layerIndex));
    }
}
