using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GuitarForm.Model.Parameters
{
    // A whole number, e.g. a fret or a count of braces. Null when not set.
    public sealed record CountParameter(ParameterDefinition Definition, int? Value = null) : Parameter(Definition)
    {
        public override bool HasValue => Value.HasValue;

        public override string Format(LengthUnit unit, IFormatProvider culture = null) =>
            Value?.ToString(culture ?? CultureInfo.CurrentCulture) ?? "";

        public override object StoredValue => Value;

        public override Parameter WithStoredValue(object value) =>
            this with { Value = value == null ? null : Convert.ToInt32(value, CultureInfo.InvariantCulture) };

        public override bool TryParse(string text, LengthUnit unit, IFormatProvider culture, out Parameter result,
            out ModelMessage error)
        {
            error = default;
            if (string.IsNullOrWhiteSpace(text))
            {
                result = this with { Value = null };
                return true;
            }
            if (!int.TryParse(text, NumberStyles.Integer, culture ?? CultureInfo.CurrentCulture, out int parsed))
            {
                result = this;
                error = NotUnderstood(text, "a whole number");
                return false;
            }
            result = this with { Value = parsed };
            return true;
        }

        public override IEnumerable<ModelMessage> Validate(LengthUnit unit, IFormatProvider culture = null)
        {
            var messages = base.Validate(unit, culture);
            if (Value is int value)
                messages = messages.Concat(CheckRange(value,
                    limit => limit.ToString(culture ?? CultureInfo.CurrentCulture)));
            return messages;
        }
    }
}
