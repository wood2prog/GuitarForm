using GuitarForm.Model;

namespace GuitarForm
{
    // The fields shown on each section's tab, in order. Lengths are stored in mm and shown in the document's units.
    // Descriptions are shown as tooltips. See docs/Plate.md,
    // docs/SideView.md, docs/Soundhole.md and docs/Instrument.md for what each value does.
    static class Sections
    {
        public static readonly NumberField<Instrument>[] Instrument =
        {
            new("Scale length", true, "Distance from the nut to the saddle",
                s => s.ScaleLength, (s, v) => s with { ScaleLength = v.Value }, Required: true),
            new("Neck join fret", false, "The fret at which the neck joins the body",
                s => s.NeckJoinFret, (s, v) => s with { NeckJoinFret = (int)v.Value }, Required: true, Whole: true),
        };

        public static readonly NumberField<Body>[] Body =
        {
            new("Body length", true, "Overall length of the guitar body",
                s => s.BodyLength, (s, v) => s with { BodyLength = v.Value }, Required: true),
            new("Upper bout width", true, "Width of the upper bout",
                s => s.UbWidth, (s, v) => s with { UbWidth = v }),
            new("Upper bout offset", true, "Offset of the upper bout line down from the upper bout primary radius centre",
                s => s.UbOffset, (s, v) => s with { UbOffset = v }),
            new("Upper bout primary radius", true, "Primary radius of the upper bout. Its centre sits one radius below the top of the body",
                s => s.UbRadius, (s, v) => s with { UbRadius = v }),
            new("Upper bout secondary offset", true, "Moves the upper bout secondary radius centre toward the body centre and grows its radius by the same amount. 0 = same as the primary radius",
                s => s.UbSecondaryOffset, (s, v) => s with { UbSecondaryOffset = v }),
            new("Waist radius", true, "Radius of the waist curve. Its centre sits on the waist center line, one radius outside the waist width",
                s => s.WaistRadius, (s, v) => s with { WaistRadius = v }),
            new("Waist width", true, "Width of the body at the waist",
                s => s.WaistWidth, (s, v) => s with { WaistWidth = v }),
            new("Waist offset", true, "Distance of the waist center line up from the tail end",
                s => s.WaistOffset, (s, v) => s with { WaistOffset = v }),
            new("Lower bout width", true, "Width of the lower bout",
                s => s.LbWidth, (s, v) => s with { LbWidth = v }),
            new("Lower bout offset", true, "Offset of the lower bout center line up from the lower bout primary radius centre",
                s => s.LbOffset, (s, v) => s with { LbOffset = v }),
            new("Lower bout primary radius", true, "Primary radius of the lower bout. Its centre sits one radius above the tail end",
                s => s.LbRadius, (s, v) => s with { LbRadius = v }),
            new("Lower bout secondary offset", true, "Moves the lower bout secondary radius centre toward the body centre and grows its radius by the same amount. 0 = same as the primary radius",
                s => s.LbSecondaryOffset, (s, v) => s with { LbSecondaryOffset = v }),
            new("Heel width", true, "Width of the flat at the top of the body where the heel of the neck attaches",
                s => s.HeelWidth, (s, v) => s with { HeelWidth = v }),
            new("Tail depth", true, "Depth of the body at the tail end (side view)",
                s => s.TailDepth, (s, v) => s with { TailDepth = v }),
            new("Neck depth", true, "Depth of the body at the neck end (side view)",
                s => s.NeckDepth, (s, v) => s with { NeckDepth = v }),
        };

        public static readonly NumberField<Soundhole>[] Soundhole =
        {
            new("Diameter", true, "Diameter of the soundhole",
                s => s.Diameter, (s, v) => s with { Diameter = v.Value }, Required: true),
            new("Horizontal offset", true, "Distance of the soundhole centre from the body centreline (+ is right). Blank = centred",
                s => s.HorizontalOffset, (s, v) => s with { HorizontalOffset = v }),
            new("Tail offset", true, "Distance of the soundhole centre up from the tail end",
                s => s.TailOffset, (s, v) => s with { TailOffset = v.Value }, Required: true),
        };
    }
}
