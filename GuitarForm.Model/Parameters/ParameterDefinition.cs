namespace GuitarForm.Model.Parameters
{
    public enum ParameterKind
    {
        // A length, stored in millimeters and shown in the display unit.
        Length,

        // An angle in degrees.
        Angle,

        // A number in a fixed unit that isn't converted (e.g. lb/in or GPa).
        Number,

        // A whole number, e.g. a fret or a count of braces.
        Count,

        // True or false.
        Flag,

        // One of a fixed set of options.
        Choice,

        Text,
    }

    // What a setting is and the values it accepts: its glossary name and abbreviation (docs/Glossary.md), whether it
    // must be set, its limits, and the resolution it's shown to. One definition is shared by every guitar; a
    // Parameter holds one guitar's value for it. Lengths are in millimeters and angles in degrees.
    public sealed record ParameterDefinition
    {
        public string Name { get; init; }
        public string Abbreviation { get; init; }
        public ParameterKind Kind { get; init; }
        public bool Required { get; init; }
        public double? Minimum { get; init; }
        public bool MinimumExclusive { get; init; }
        public double? Maximum { get; init; }
        public bool MaximumExclusive { get; init; }

        // The smallest step the value is shown to, in the stored unit. Text is rounded to it.
        public double Resolution { get; init; }

        // The unit of a Number, e.g. "lb/in". Empty for the other kinds.
        public string Unit { get; init; } = "";

        // The name with its abbreviation, as it appears in field labels and messages, e.g. "Scale Length (SL)".
        public string Label => $"{Name} ({Abbreviation})";

        const double LengthResolution = 0.01;
        const double AngleResolution = 0.1;

        // A width, height, thickness, depth, radius or diameter: greater than zero.
        public static ParameterDefinition Size(string name, string abbreviation, bool required = false) =>
            Length(name, abbreviation, required) with { Minimum = 0, MinimumExclusive = true };

        // A distance from one place to another: zero or more.
        public static ParameterDefinition Distance(string name, string abbreviation) =>
            Length(name, abbreviation) with { Minimum = 0 };

        // A length that can be positive or negative, e.g. a shift toward the treble or bass side.
        public static ParameterDefinition Offset(string name, string abbreviation) => Length(name, abbreviation);

        public static ParameterDefinition Angle(string name, string abbreviation, double? minimum = null,
            double? maximum = null, bool exclusive = false) =>
            new ParameterDefinition
            {
                Name = name,
                Abbreviation = abbreviation,
                Kind = ParameterKind.Angle,
                Minimum = minimum,
                MinimumExclusive = exclusive,
                Maximum = maximum,
                MaximumExclusive = exclusive,
                Resolution = AngleResolution,
            };

        // A positive number in a fixed unit.
        public static ParameterDefinition Number(string name, string abbreviation, string unit, double resolution) =>
            new ParameterDefinition
            {
                Name = name,
                Abbreviation = abbreviation,
                Kind = ParameterKind.Number,
                Unit = unit,
                Minimum = 0,
                MinimumExclusive = true,
                Resolution = resolution,
            };

        public static ParameterDefinition Count(string name, string abbreviation, int minimum, int? maximum = null,
            bool required = false) =>
            new ParameterDefinition
            {
                Name = name,
                Abbreviation = abbreviation,
                Kind = ParameterKind.Count,
                Required = required,
                Minimum = minimum,
                Maximum = maximum,
                Resolution = 1,
            };

        public static ParameterDefinition Flag(string name, string abbreviation) =>
            new ParameterDefinition { Name = name, Abbreviation = abbreviation, Kind = ParameterKind.Flag };

        public static ParameterDefinition Choice(string name, string abbreviation) =>
            new ParameterDefinition { Name = name, Abbreviation = abbreviation, Kind = ParameterKind.Choice };

        public static ParameterDefinition Text(string name, string abbreviation, bool required = false) =>
            new ParameterDefinition
            {
                Name = name,
                Abbreviation = abbreviation,
                Kind = ParameterKind.Text,
                Required = required,
            };

        static ParameterDefinition Length(string name, string abbreviation, bool required = false) =>
            new ParameterDefinition
            {
                Name = name,
                Abbreviation = abbreviation,
                Kind = ParameterKind.Length,
                Required = required,
                Resolution = LengthResolution,
            };
    }
}
