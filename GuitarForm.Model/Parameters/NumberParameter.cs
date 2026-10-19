using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GuitarForm.Model.Parameters
{
    // A length (in millimeters), an angle (in degrees) or a number in a fixed unit. Null when not set.
    public sealed record NumberParameter(ParameterDefinition Definition, double? Value = null) : Parameter(Definition)
    {
        public override bool HasValue => Value.HasValue;

        public override string Format(LengthUnit unit, IFormatProvider culture = null) =>
            Value is double value ? FormatAmount(value, unit, culture) : "";

        public override object StoredValue => Value;

        // Only a number is taken (not text or true/false), and not an infinite one or NaN.
        public override Parameter WithStoredValue(object value)
        {
            if (value == null)
                return this with { Value = null };
            if (value is not (byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal))
                throw new FormatException($"{Label}: \"{value}\" isn't a number.");
            double number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            if (double.IsNaN(number) || double.IsInfinity(number))
                throw new FormatException($"{Label}: {value} isn't a finite number.");
            return this with { Value = number };
        }

        public override string Symbol(LengthUnit unit) => Definition.Kind switch
        {
            ParameterKind.Length => unit.Symbol(),
            ParameterKind.Angle => "°",
            _ => Definition.Unit,
        };

        public override bool TryParse(string text, LengthUnit unit, IFormatProvider culture, out Parameter result,
            out ModelMessage error)
        {
            error = default;
            if (string.IsNullOrWhiteSpace(text))
            {
                result = this with { Value = null };
                return true;
            }
            if (!double.TryParse(text, NumberStyles.Float, culture ?? CultureInfo.CurrentCulture, out double parsed) ||
                double.IsNaN(parsed) || double.IsInfinity(parsed))
            {
                result = this;
                error = NotUnderstood(text, "a number");
                return false;
            }
            result = this with { Value = IsLength ? unit.ToMillimeters(parsed) : parsed };
            return true;
        }

        public override IEnumerable<ModelMessage> Validate(LengthUnit unit, IFormatProvider culture = null)
        {
            var messages = base.Validate(unit, culture);
            if (Value is double value)
                messages = messages.Concat(CheckRange(value, limit => FormatWithSymbol(limit, unit, culture)));
            return messages;
        }

        bool IsLength => Definition.Kind == ParameterKind.Length;

        string FormatAmount(double amount, LengthUnit unit, IFormatProvider culture) => IsLength
            ? FormatNumber(unit.FromMillimeters(amount), unit.FromMillimeters(Definition.Resolution), culture)
            : FormatNumber(amount, Definition.Resolution, culture);

        string FormatWithSymbol(double amount, LengthUnit unit, IFormatProvider culture) =>
            (this with { Value = amount }).FormatWithUnit(unit, culture);
    }
}
