namespace GuitarForm.Model
{
    // The body: its outline and its depths (see docs/Body.md). Body length is required; the
    // rest are optional (null when not set). The outline is drawn as far as the values set allow, and the side view
    // once both depths are set.
    public sealed record Body
    {
        public double BodyLength { get; init; }
        public double? UbWidth { get; init; }
        public double? UbOffset { get; init; }
        public double? UbRadius { get; init; }
        public double? UbSecondaryOffset { get; init; }
        public double? WaistRadius { get; init; }
        public double? WaistWidth { get; init; }
        public double? WaistOffset { get; init; }
        public double? LbWidth { get; init; }
        public double? LbOffset { get; init; }
        public double? LbRadius { get; init; }
        public double? LbSecondaryOffset { get; init; }
        public double? HeelWidth { get; init; }
        public double? TailDepth { get; init; }
        public double? NeckDepth { get; init; }
    }
}
