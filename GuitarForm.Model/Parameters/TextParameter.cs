using System;

namespace GuitarForm.Model.Parameters
{
    // Free text, e.g. a name. Null when not set.
    public sealed record TextParameter(ParameterDefinition Definition, string Value = null) : Parameter(Definition)
    {
        public override bool HasValue => !string.IsNullOrWhiteSpace(Value);

        public override string Format(LengthUnit unit, IFormatProvider culture = null) => Value ?? "";

        public override object StoredValue => HasValue ? Value : null;

        public override Parameter WithStoredValue(object value) =>
            this with { Value = value == null ? null : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) };

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
