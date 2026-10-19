# Instrument tab

Values that apply to the whole instrument. Other parts of the design read them; the Instrument tab draws nothing itself.

Fields are labeled as in the [Glossary](Glossary.md); hover a label or box for its description, unit and limits. A required box shows "required" while blank; clearing it removes the value, and the Messages bar then reports it (Save, Export and Build are refused while it is blank). Lengths are shown and typed in the Rhino document's units and stored in millimeters; see [Units](../README.md#units).

| Field | Description |
|---|---|
| Scale Length (SL) | *Required.* Distance from the nut to the saddle. Must be greater than 0. |
| Neck Join Fret (NJF) | *Required.* The fret at which the neck joins the body, e.g. 12 or 14. A whole number, 1 or more. |

The distance from the nut to any fret uses equal-tempered spacing: *L* − *L* / 2^(*n*/12). The distance to the neck join fret is the neck's length from the nut to the body.

**Quick test:** in a document in millimeters, open a design and type these values on the Instrument tab. They change the working copy until you press **Save**:

| Field | Value | Result |
|---|---|---|
| Scale length, Neck join fret | 650, 12 | No messages. The neck join is 325 from the nut. |
| Neck join fret | change to 14 | The neck join is 360.458 from the nut. |
| Scale length | change to 0 | Error: Scale Length (SL) must be greater than 0 mm. |
| Neck join fret | change to 0 | Error: Neck Join Fret (NJF) must be at least 1. |
