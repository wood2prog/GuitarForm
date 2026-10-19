namespace GuitarForm.Shapes
{
    /// <summary>
    /// The part of the guitar a drawn item belongs to. The member NAMES are permanent keys (saved color overrides refer
    /// to them): never rename one, only add.
    /// </summary>
    public enum DrawingComponent
    {
        // Body group.

        /// <summary>The top plate's outline, including the cutaway, and its construction geometry.</summary>
        TopPlate,
        /// <summary>The body seen from the side.</summary>
        Sides,
        /// <summary>The soundhole.</summary>
        Soundhole,
        /// <summary>The braces and bars of the top, and the copy of the plate they're drawn on.</summary>
        Bracing,
        /// <summary>The block inside the body that the neck's heel attaches to.</summary>
        Headblock,
        /// <summary>The block inside the body at the tail end.</summary>
        Tailblock,

        // Neck group.

        /// <summary>The neck's shaft in the side view (the heel is drawn on its own, as Heel).</summary>
        NeckSide,
        /// <summary>The heel of the neck, the part that attaches to the body.</summary>
        Heel,
        /// <summary>The heel cap.</summary>
        HeelCap,
        /// <summary>The nut.</summary>
        Nut,
        /// <summary>The neck's cross-sections beside the side view.</summary>
        NeckProfiles,

        // Fretboard group.

        /// <summary>The fretboard's outline in the plan view.</summary>
        Fretboard,
        /// <summary>The fretboard's profile in the side view.</summary>
        FretboardSide,
        /// <summary>The frets.</summary>
        Frets,
        /// <summary>The fret slots.</summary>
        FretSlots,
        /// <summary>The face dots in the plan view.</summary>
        FaceDots,
        /// <summary>The side dots in the side view.</summary>
        SideDots,
        /// <summary>The strings.</summary>
        Strings,
    }

    /// <summary>How a drawn item is used.</summary>
    public enum DrawingRole
    {
        Construction,
        Outline,
        Centerline,
    }

    /// <summary>One thing drawn: a path or a circle (one of them is set), with its component and role.</summary>
    public sealed record DrawnItem(DrawingComponent Component, DrawingRole Role, Path Path = null, Circle? Circle = null);
}
