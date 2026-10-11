using System;
using System.Collections.Generic;

namespace GuitarForm.Model.Parameters
{
    // True or false, e.g. whether the top plate has a soundhole. Always set; false by default.
    public sealed record FlagParameter(ParameterDefinition Definition, bool Value = false) : Parameter(Definition)
    {
        public override bool HasValue => true;

        static readonly string[] Texts = { "Yes", "No" };

        public override IReadOnlyList<string> OptionTexts => Texts;

        public override string Format(LengthUnit unit, IFormatProvider culture = null) => Value ? "Yes" : "No";

        public override object StoredValue => Value;

        public override Parameter WithStoredValue(object value) =>
            this with { Value = value != null && Convert.ToBoolean(value, System.Globalization.CultureInfo.InvariantCulture) };

        public override bool TryParse(string text, LengthUnit unit, IFormatProvider culture, out Parameter result,
            out ModelMessage error)
        {
            error = default;
            switch (text?.Trim().ToLowerInvariant())
            {
                case "yes":
                case "true":
                    result = this with { Value = true };
                    return true;
                case "no":
                case "false":
                    result = this with { Value = false };
                    return true;
                default:
                    result = this;
                    error = NotUnderstood(text ?? "", "yes or no");
                    return false;
            }
        }
    }
}
