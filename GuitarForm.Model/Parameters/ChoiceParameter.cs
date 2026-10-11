using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GuitarForm.Model.Parameters
{
    // One of an enum's options, e.g. the cutaway type. Always set; the enum's first option by default.
    public sealed record ChoiceParameter<T>(ParameterDefinition Definition, T Value = default) : Parameter(Definition)
        where T : struct, Enum
    {
        public static IReadOnlyList<T> Options { get; } = Enum.GetValues<T>();

        public override bool HasValue => true;

        // Static, so it isn't a field that record equality would compare.
        static readonly string[] Texts = Options.Select(OptionText).ToArray();

        public override IReadOnlyList<string> OptionTexts => Texts;

        public override string Format(LengthUnit unit, IFormatProvider culture = null) => OptionText(Value);

        // Saved by option name, so reordering the options doesn't change saved values.
        public override object StoredValue => Value.ToString();

        public override Parameter WithStoredValue(object value)
        {
            if (value == null)
                return this with { Value = default };
            if (value is string name && Enum.TryParse(name, out T option) && Enum.IsDefined(option))
                return this with { Value = option };
            throw new FormatException($"{Label}: \"{value}\" isn't one of its options.");
        }

        public override bool TryParse(string text, LengthUnit unit, IFormatProvider culture, out Parameter result,
            out ModelMessage error)
        {
            error = default;
            string key = Key(text ?? "");
            foreach (T option in Options)
            {
                if (Key(option.ToString()) == key)
                {
                    result = this with { Value = option };
                    return true;
                }
            }
            result = this;
            error = NotUnderstood(text ?? "", "one of: " + string.Join(", ", Options.Select(OptionText)));
            return false;
        }

        // An option as words in sentence case, e.g. NotSpecified as "Not specified".
        public static string OptionText(T option)
        {
            string name = option.ToString();
            var words = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]))
                    words.Append(' ').Append(char.ToLowerInvariant(name[i]));
                else
                    words.Append(name[i]);
            }
            return words.ToString();
        }

        static string Key(string text) => text.Replace(" ", "").ToLowerInvariant();
    }
}
