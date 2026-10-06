namespace GuitarForm.Geometry
{
    public enum MessageLevel
    {
        Remark,
        Warning,
        Error,
    }

    // A message from building geometry, shown on the component as a runtime message of the same level.
    public readonly record struct GeometryMessage(MessageLevel Level, string Text);
}
