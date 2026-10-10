namespace GuitarForm.Model
{
    // A problem with a guitar's values: a missing required value, a value out of range, or values that don't agree.
    public readonly record struct ModelMessage(MessageLevel Level, string Text)
    {
        public static ModelMessage Error(string text) => new ModelMessage(MessageLevel.Error, text);

        public static ModelMessage Warning(string text) => new ModelMessage(MessageLevel.Warning, text);
    }
}
