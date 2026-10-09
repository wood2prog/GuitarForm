# Instrument tab

Values that apply to the whole instrument. Other parts of the design read them; the Instrument tab draws nothing itself.

Lengths are shown and typed in the Rhino document's units and stored in millimetres; see [Units](../README.md#units).

| Field | Description |
|---|---|
| Scale length | *Required.* Distance from the nut to the saddle. Must be greater than 0. |
| Neck join fret | *Required.* The fret at which the neck joins the body, e.g. 12 or 14. A whole number, 1 or more. |

The distance from the nut to any fret uses equal-tempered spacing: *L* − *L* / 2^(*n*/12). The distance to the neck join fret is the neck's length from the nut to the body.

**Quick test:** in a document in millimetres, open a design and type these values on the Instrument tab:

| Field | Value | Result |
|---|---|---|
| Scale length, Neck join fret | 650, 12 | No messages. The neck join is 325 from the nut. |
| Neck join fret | change to 14 | The neck join is 360.458 from the nut. |
| Scale length | change to 0 | Error: the scale length must be greater than zero. |
| Neck join fret | change to 0 | Error: the neck join fret must be 1 or more. |
