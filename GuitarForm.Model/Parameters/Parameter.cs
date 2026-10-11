using System;
using System.Collections.Generic;
using System.Globalization;

namespace GuitarForm.Model.Parameters
{
    // One guitar's value for a setting, with the setting's definition. Formatting, parsing and checking a value all
    // happen here, so every field, message and export treats a setting the same way. Text is shown and typed in the
    // given display unit and culture (the current culture when none is given); values are stored in millimeters.
    public abstract record Parameter(ParameterDefinition Definition)
    {
        public string Label => Definition.Label;

        public abstract bool HasValue { get; }

        // The value as text for a field, rounded to the definition's resolution. Empty when not set.
        public abstract string Format(LengthUnit unit, IFormatProvider culture = null);

        // The unit symbol shown after the value, e.g. "mm" or "°". Empty when the value has no unit.
        public virtual string Symbol(LengthUnit unit) => "";

        // The texts a field can offer to pick from, as Format gives them and TryParse reads them. Empty when the
        // value is typed rather than picked.
        public virtual IReadOnlyList<string> OptionTexts => Array.Empty<string>();

        // The value with its unit, for messages.
        public string FormatWithUnit(LengthUnit unit, IFormatProvider culture = null)
        {
            string text = Format(unit, culture);
            string symbol = Symbol(unit);
            if (text.Length == 0 || symbol.Length == 0)
                return text;
            return symbol == "°" ? text + symbol : text + " " + symbol;
        }

        // The value as it's saved: a double, int, bool, string, or a Choice's option name. Null when not set.
        public abstract object StoredValue { get; }

        // A copy with a saved value: one of the types StoredValue gives, or a long for a whole number (as SQLite
        // reads them). Null clears the value. Throws FormatException if the value can't be this parameter's.
        public abstract Parameter WithStoredValue(object value);

        // Reads text typed into a field. Empty text clears the value. On failure, error says why.
        public abstract bool TryParse(string text, LengthUnit unit, IFormatProvider culture, out Parameter result,
            out ModelMessage error);

        // Problems with the value on its own. Problems between values are checked by the part that owns them.
        public virtual IEnumerable<ModelMessage> Validate(LengthUnit unit, IFormatProvider culture = null)
        {
            if (Definition.Required && !HasValue)
                yield return ModelMessage.Error($"{Label} is required.");
        }

        // Checks a value against the definition's limits. formatLimit shows a limit in the display unit.
        protected IEnumerable<ModelMessage> CheckRange(double value, Func<double, string> formatLimit)
        {
            double? minimum = Definition.Minimum;
            if (minimum is double min && (Definition.MinimumExclusive ? value <= min : value < min))
            {
                string words = Definition.MinimumExclusive ? "greater than" : "at least";
                yield return ModelMessage.Error($"{Label} must be {words} {formatLimit(min)}.");
            }
            double? maximum = Definition.Maximum;
            if (maximum is double max && (Definition.MaximumExclusive ? value >= max : value > max))
            {
                string words = Definition.MaximumExclusive ? "less than" : "at most";
                yield return ModelMessage.Error($"{Label} must be {words} {formatLimit(max)}.");
            }
        }

        // A number rounded to show the given resolution, without trailing zeros.
        protected static string FormatNumber(double value, double resolution, IFormatProvider culture)
        {
            int decimals = resolution > 0 ? (int)Math.Ceiling(-Math.Log10(resolution) - 1e-9) : 5;
            decimals = Math.Clamp(decimals, 0, 10);
            // Adding zero turns a rounded -0 into 0.
            double rounded = Math.Round(value, decimals) + 0.0;
            string format = decimals == 0 ? "0" : "0." + new string('#', decimals);
            return rounded.ToString(format, culture ?? CultureInfo.CurrentCulture);
        }

        protected ModelMessage NotUnderstood(string text, string expected) =>
            ModelMessage.Error($"{Label}: \"{text.Trim()}\" isn't {expected}.");
    }
}
