namespace GuitarForm.Model
{
    public enum MessageLevel
    {
        Remark,
        Warning,
        Error,
    }

    // A problem with a design's values or with building its geometry: an error stops the geometry being built.
    public readonly record struct GeometryMessage(MessageLevel Level, string Text);
}
