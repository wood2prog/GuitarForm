using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Eto.Drawing;
using Eto.Forms;
using GuitarForm.Data;
using GuitarForm.Geometry;
using GuitarForm.Model;
using GuitarForm.Model.Parameters;
using GuitarForm.Model.Parts;
using Rhino;
using GuitarDrawing = GuitarForm.Shapes.GuitarDrawing;
using IPanel = Rhino.UI.IPanel;
using ShowPanelReason = Rhino.UI.ShowPanelReason;

namespace GuitarForm
{
    public partial class GuitarFormPanel
    {
        // --- Drawing ---

        DesignDrawing Draw()
        {
            var doc = RhinoDoc.ActiveDoc;
            double tolerance = doc == null ? 0.01 : DesignUnits.ToleranceInMillimetres(doc.ModelAbsoluteTolerance, doc.ModelUnitSystem);
            var unit = _units.LengthUnit;
            var culture = CultureInfo.CurrentCulture;
            var guitar = _session.Working;
            var shapes = GuitarDrawing.Draw(guitar, tolerance, Plugin.GuitarFormSettings.DrawingGap, unit, culture);

            // A part's messages include its subparts', so each tab's are what's left after its subparts' are taken out.
            static GeometryMessage FromModel(ModelMessage m) => new GeometryMessage(m.Level, m.Text);
            var all = guitar.Validate(unit, culture).ToList();
            var bodyAll = guitar.Body.Validate(unit, culture).ToList();
            var neckAll = guitar.Neck.Validate(unit, culture).ToList();
            var topPlate = guitar.Body.TopPlate.Validate(unit, culture).ToList();
            var bracing = guitar.Body.TopPlate.Bracing.Validate(unit, culture).ToList();
            var fretboard = guitar.Neck.Fretboard.Validate(unit, culture).ToList();
            var instrument = all.Except(bodyAll).Except(neckAll).Select(FromModel);
            var soundhole = topPlate.Except(bracing).Select(FromModel);
            var body = bodyAll.Except(topPlate).Select(FromModel).Concat(shapes.BodyMessages);
            var drawing = DesignDrawing.FromShapes(shapes, instrument, body, soundhole);
            _tabMessageLists = new Dictionary<string, List<GeometryMessage>>
            {
                [GuitarFields.InstrumentTab] = InputErrors(GuitarFields.InstrumentTab).Concat(drawing.InstrumentMessages).ToList(),
                [GuitarFields.BodyTab] = InputErrors(GuitarFields.BodyTab).Concat(drawing.BodyMessages).ToList(),
                [GuitarFields.SoundholeTab] = InputErrors(GuitarFields.SoundholeTab).Concat(drawing.SoundholeMessages).ToList(),
                [GuitarFields.BracingTab] = InputErrors(GuitarFields.BracingTab).Concat(bracing.Select(FromModel)).Concat(shapes.BracingMessages).ToList(),
                [GuitarFields.FretboardTab] = InputErrors(GuitarFields.FretboardTab).Concat(fretboard.Select(FromModel)).Concat(shapes.FretboardMessages).ToList(),
                [GuitarFields.NeckTab] = InputErrors(GuitarFields.NeckTab)
                    .Concat(neckAll.Except(fretboard).Select(FromModel))
                    .Concat(shapes.NeckMessages).ToList(),
            };
            return drawing;
        }

        // Shows each section's messages and updates the preview.
        void Redraw()
        {
            var doc = RhinoDoc.ActiveDoc;
            if (!_session.IsOpen)
            {
                foreach (var messages in _tabMessages.Values)
                    messages.Show(null);
                _preview.Hide(doc);
                return;
            }

            var drawing = Draw();
            foreach (var (tab, messages) in _tabMessages)
                messages.Show(_tabMessageLists[tab]);
            if (_showPreview.Checked == true && _panelVisible && doc != null)
                _preview.Show(drawing, doc, _showStrings.Checked == true);
            else
                _preview.Hide(doc);
        }

        void Build()
        {
            var doc = RhinoDoc.ActiveDoc ?? throw new InvalidOperationException("There's no Rhino document to build into.");
            RequireNoInputErrors("built");
            var drawing = Draw();
            // A part with errors is drawn blank in the preview, so building would leave it out.
            int errors = _tabMessageLists.Values.SelectMany(m => m)
                .Where(m => m.Level == MessageLevel.Error).Distinct().Count();
            if (errors > 0 && MessageBox.Show(this,
                    $"The design has {errors} {(errors == 1 ? "error" : "errors")} (see Messages), so the parts with errors will be missing from the build.\n\nBuild anyway?",
                    "GuitarForm", MessageBoxButtons.YesNo, MessageBoxType.Warning) != DialogResult.Yes)
                return;
            int added = DesignBuilder.Build(doc, _session.BuildKey, _session.LegacyBuildKeys, WorkingName, drawing, _showStrings.Checked == true);
            RhinoApp.WriteLine($"GuitarForm: built \"{WorkingName}\"{(_session.Dirty ? " (unsaved)" : "")} ({added} objects).");
        }
    }
}
