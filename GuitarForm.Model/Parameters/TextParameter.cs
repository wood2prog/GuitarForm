using System;

namespace GuitarForm.Model.Parameters
{
    // Free text, e.g. a name. Null when not set.
    public sealed record TextParameter(ParameterDefinition Definition, string Value = null) : Parameter(Definition)
    {
        public override bool HasValue => !string.IsNullOrWhiteSpace(Value);

        public override string Format(LengthUnit unit, IFormatProvider culture = null) => Value ?? "";

        public override object StoredValue => HasValue ? Value : null;

        // Only text is taken: a number or true/false is a wrongly typed value, not a name.
        public override Parameter WithStoredValue(object value) => value switch
        {
            null => this with { Value = null },
            string text => this with { Value = text },
            _ => throw new FormatException($"{Label}: \"{value}\" isn't text."),
        };

        public override bool TryParse(string text, LengthUnit unit, IFormatProvider culture, out Parameter result,
            out ModelMessage error)
        {
            error = default;
            string trimmed = text?.Trim();
            result = this with { Value = string.IsNullOrEmpty(trimmed) ? null : trimmed };
            return true;
        }
    }
}
