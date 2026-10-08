namespace GuitarForm.Model
{
    // A whole guitar design. Each section is one area of the guitar and one tab in the editor. Lengths are in
    // millimetres.
    public sealed record GuitarDesign
    {
        public string Name { get; init; } = "";

        public Instrument Instrument { get; init; }

        public Body Body { get; init; }

        // Null when the design has no soundhole.
        public Soundhole Soundhole { get; init; }
    }
}
