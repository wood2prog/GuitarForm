using GuitarForm.Model.Parts;

namespace GuitarForm.Shapes
{
    // The top plate's round soundhole: centered the Soundhole Offset from Centerline across from the centerline (+X is
    // the treble side) and the Soundhole Offset from Tail End up from the tail end.
    public static class SoundholeShape
    {
        // The soundhole, or null when the top has none or a value it needs isn't set or is out of range (the model
        // reports it).
        public static Circle? Solve(TopPlate top)
        {
            if (!(top.Soundhole.Value && top.SoundholeDiameter.Value is double diameter && diameter > 0 &&
                  top.SoundholeOffsetFromTailEnd.Value is double fromTail))
                return null;
            return new Circle(new Point2(top.SoundholeOffsetFromCenterline.Value ?? 0, fromTail), diameter / 2);
        }
    }
}
