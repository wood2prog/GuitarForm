# Instrument component

**GuitarForm › Instrument.** Collects the values that apply to the whole instrument into one **Instrument** object. Wire its output to the components that need those values, instead of wiring each value to each of them. It draws nothing.

Lengths are in the Rhino document's units; see [Using the components](../README.md#using-the-components).

| | Name | Type | Description |
|---|---|---|---|
| Input | Scale Length (`SL`) | Number | Distance from the nut to the saddle. Must be greater than 0. |
| Input | Neck Join Fret (`NJ`) | Integer | The fret at which the neck joins the body, e.g. 12 or 14. Must be 1 or more. |
| Output | Instrument (`I`) | Instrument | The values above, for other GuitarForm components. Nothing is output if an input is invalid. |

Components that read the Instrument can also work out the distance from the nut to any fret, with equal-tempered spacing: *L* − *L* / 2^(*n*/12). The distance to the neck join fret is the neck's length from the nut to the body.

**Quick test:** put an **Instrument** component on the canvas, connect **Number Sliders**, and wire the output to a **Panel**:

| Input | Value | Result |
|---|---|---|
| SL, NJ | 650, 12 | The panel shows `Instrument (Scale 650, Neck Join Fret 12)`. The neck join is 325 from the nut. |
| NJ | change to 14 | The neck join is 360.458 from the nut. |
| SL | change to 0 | Error: the scale length must be greater than zero. |
| NJ | change to 0 | Error: the neck join fret must be 1 or more. |
