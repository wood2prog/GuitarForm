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
using Body = GuitarForm.Model.Body;
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
            var topPlate = guitar.Body.TopPlate.Validate(unit, culture).Select(FromModel).ToList();
            var bracing = guitar.Body.TopPlate.Bracing.Validate(unit, culture).Select(FromModel).ToList();
            var fretboard = guitar.Neck.Fretboard.Validate(unit, culture).Select(FromModel).ToList();
            var soundhole = topPlate.Except(bracing).ToList();
            var body = InputErrors(NewFields.BodyTab)
                .Concat(guitar.Body.Validate(unit, culture).Select(FromModel).Except(topPlate))
                .Concat(shapes.BodyMessages).ToList();
            _newTabMessages = new Dictionary<string, List<GeometryMessage>>
            {
                [NewFields.BracingTab] = InputErrors(NewFields.BracingTab).Concat(bracing).Concat(shapes.BracingMessages).ToList(),
                [NewFields.FretboardTab] = InputErrors(NewFields.FretboardTab).Concat(fretboard).Concat(shapes.FretboardMessages).ToList(),
                [NewFields.NeckTab] = InputErrors(NewFields.NeckTab)
                    .Concat(guitar.Neck.Validate(unit, culture).Select(FromModel).Except(fretboard))
                    .Concat(shapes.NeckMessages).ToList(),
            };
            return DesignDrawing.FromShapes(shapes, Design.Instrument.Validate(), body, soundhole);
        }

        // The errors from new fields on the tab whose text couldn't be read.
        IEnumerable<GeometryMessage> InputErrors(string tab) =>
            _newFieldErrors.Where((_, i) => NewFieldTabs[i] == tab &&
                    (NewFields.Fields[i].Visible?.Invoke(Fields) ?? true))
                .Append(tab == NewFields.FretboardTab ? _markersError : null)
                .Where(e => e != null)
                .Select(e => new GeometryMessage(MessageLevel.Error, e));

        // Shows each section's messages and updates the preview.
        void Redraw()
        {
            var doc = RhinoDoc.ActiveDoc;
            if (!_session.IsOpen)
            {
                _instrument.ShowMessages(null);
                _body.ShowMessages(null);
                _soundhole.ShowMessages(null);
                foreach (var messages in _tabMessages.Values)
                    messages.Show(null);
                _preview.Hide(doc);
                return;
            }

            var drawing = Draw();
            _instrument.ShowMessages(drawing.InstrumentMessages);
            _body.ShowMessages(drawing.BodyMessages);
            _soundhole.ShowMessages(drawing.SoundholeMessages);
            foreach (var (tab, messages) in _tabMessages)
                messages.Show(_newTabMessages[tab]);
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
            int errors = drawing.InstrumentMessages.Concat(drawing.BodyMessages).Concat(drawing.SoundholeMessages)
                .Concat(_newTabMessages.Values.SelectMany(m => m))
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
