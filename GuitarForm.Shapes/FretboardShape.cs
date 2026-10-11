using GuitarForm.Model.Parts;

namespace GuitarForm.Shapes
{
    // Where the fretboard sits over the plate. The neck join fret is at the body's neck end, the nut is that fret's
    // distance beyond it, and the saddle is the scale length from the nut (without compensation). The fretboard is
    // centered on the centerline: Nut Width wide at the nut, and as wide as the String Spacing at Bridge plus both
    // setbacks at the saddle, with straight edges between.
    public static class FretboardShape
    {
        // The distance from the centerline to the fretboard's treble edge at the body's neck end. Null when a value it
        // needs isn't set.
        public static double? TrebleEdgeAtNeckEnd(Guitar guitar)
        {
            var fretboard = guitar.Neck.Fretboard;
            if (!(guitar.ScaleLength.Value is double scale && guitar.NeckJoinDistance is double join &&
                  fretboard.NutWidth.Value is double nutWidth &&
                  fretboard.StringSpacingAtBridge.Value is double spacing &&
                  fretboard.BassStringSetback.Value is double bass && fretboard.TrebleStringSetback.Value is double treble))
                return null;
            double atNut = nutWidth / 2, atSaddle = (spacing + bass + treble) / 2;
            return atNut + (atSaddle - atNut) * join / scale;
        }
    }
}
