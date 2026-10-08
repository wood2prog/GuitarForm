namespace GuitarForm.Model
{
    // A round soundhole (see docs/Soundhole.md). The horizontal offset is optional (null when not set) and defaults to
    // 0, the body centre.
    public sealed record Soundhole
    {
        public double Diameter { get; init; }
        public double? HorizontalOffset { get; init; }
        public double TailOffset { get; init; }
    }
}
