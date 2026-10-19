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

        // A fraction isn't rounded: it's refused.
        public override Parameter WithStoredValue(object value)
        {
            if (value == null)
                return this with { Value = null };
            if (value is not (byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal))
                throw new FormatException($"{Label}: \"{value}\" isn't a whole number.");
            if (value is double or float or decimal)
            {
                double number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if (double.IsNaN(number) || double.IsInfinity(number) || number % 1 != 0)
                    throw new FormatException($"{Label}: {value} isn't a whole number.");
            }
            try
            {
                return this with { Value = Convert.ToInt32(value, CultureInfo.InvariantCulture) };
            }
            catch (OverflowException e)
            {
                throw new FormatException($"{Label}: {value} is too large for a whole number.", e);
            }
        }

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
