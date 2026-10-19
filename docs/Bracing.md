# Bracing tab

The top plate's bracing: the X brace, graft, upper transverse bar, finger braces, tone bars and soundhole braces. The headblock and tailblock are drawn with the bracing, but their fields are on the [Body tab](Body.md). Every field's meaning is in [Glossary.md](Glossary.md); this page covers how the fields are laid out and what is drawn.

The bracing is drawn on its own copy of the plate outline and soundhole, seen from inside the guitar, so the treble side is on the left. The copy is mirrored across the centerline and placed to the left of the plate, the drawing gap (see [Settings](Settings.md#drawing), 50 mm by default) from the plate's leftmost point. Lengths are shown and typed in the Rhino document's units and stored in millimeters; see [Units](../README.md#units).

The bracing is a part of the top plate that is saved on its own in the library, so a bracing pattern can be reused in another guitar. Nothing here is saved until the design is saved.

The **Saved bracing copies** row at the top of the tab keeps a bracing pattern in the library by name. **Save as…** saves the open design's bracing (not empty, and not a name already used for bracing). **Load** replaces only the bracing on the open design's top plate, as an unsaved edit. **Delete** removes a saved copy after asking. (A saved body on the [Body tab](Body.md) also carries its bracing.)

## Fields

Every brace is a straight strip. Its width is across it, and its height (from the top plate to the top of the brace) is stored but doesn't change the plan view. "Neck end" is the end of the body where the heel sits (y = Body Length).

Limits come from the model: a width or spacing must be greater than 0, an offset (or edge offset) 0 or more, and a count 0 or more. A field left blank is unset and has no default.

### X brace

| Field | Abbreviation | Limits | Meaning |
|---|---|---|---|
| X Brace Offset from Neck End | XBO | 0 or more; must be less than the Body Length | Distance from the neck end to where the legs cross, along the centerline. |
| X Brace Angle | XBA | more than 0 and less than 90 degrees | Angle between each leg and the centerline. |
| X Brace Edge Offset | XBEO | 0 or more | How far each leg stops short of the outline. At 0 the legs run to the outline. |
| X Brace Width | XBW | more than 0 | Width of each bar. |
| X Brace Height | XBH | more than 0 | Height from the top plate. |
| X Brace Profile Type | XBPT | Scalloped or Tapered (a list) | The shape of the X brace's profile. Saved with the design; it doesn't change the drawing, because no brace's height is drawn. |

### Graft

| Field | Abbreviation | Limits | Meaning |
|---|---|---|---|
| Graft | GR | ticked or not | Whether the top has a graft. When ticked, the thickness, width and offset are required. |
| Graft Thickness | GRT | more than 0 | Thickness. |
| Graft Width | GRW | more than 0 | Width along the centerline. |
| Graft Offset from Neck End | GRO | 0 or more; less than the Body Length | Distance from the neck end to the graft's edge nearest the neck end. |
| Graft Edge Offset | GREO | 0 or more | How far each end stops short of the outline. |

### Upper transverse bar

| Field | Abbreviation | Limits | Meaning |
|---|---|---|---|
| Upper Transverse Bar Offset from Neck End | UTO | 0 or more; less than the Body Length | Distance from the neck end to the bar's edge nearest the neck end. |
| Upper Transverse Bar Width | UTW | more than 0 | Width along the centerline. |
| Upper Transverse Bar Height | UTH | more than 0 | Height from the top plate. |
| Upper Transverse Bar Edge Offset | UTEO | 0 or more | How far each end stops short of the outline. |

### Finger braces

| Field | Abbreviation | Limits | Meaning |
|---|---|---|---|
| Bass Finger Brace Count | BFNC | 0 or more | Number of finger braces on the bass side. |
| Bass Finger Brace Angle | BFNA | more than -90 and less than 90 degrees | Angle from square to the bass side lower leg. |
| Bass Finger Brace Spacing | BFNS | more than 0 | Distance between braces along the leg's centerline; the first is this far from the crossing. |
| Treble Finger Brace Count | TFNC | 0 or more | Number on the treble side. |
| Treble Finger Brace Angle | TFNA | more than -90 and less than 90 degrees | Angle from square to the treble side lower leg. |
| Treble Finger Brace Spacing | TFNS | more than 0 | Distance between braces along the leg. |
| Finger Brace Width | FNW | more than 0 | Width of every finger brace. |
| Finger Brace Height | FNH | more than 0 | Height of every finger brace. |
| Finger Brace Edge Offset | FNEO | 0 or more | How far each finger brace stops short of the outline. |

When a side's count is above 0, that side's angle and spacing are required, and the width and height are required once (shared by both sides).

### Tone bars

| Field | Abbreviation | Limits | Meaning |
|---|---|---|---|
| Tone Bar Count | TNC | 0 or more | Number of tone bars (treble side only). |
| Tone Bar Spacing | TNS | more than 0 | Distance between bars along the treble side lower leg. |
| Tone Bar Angle | TNA | more than -90 and less than 90 degrees | Angle from square to the leg. |
| Tone Bar Width | TNW | more than 0 | Width of every tone bar. |
| Tone Bar Height | TNH | more than 0 | Height of every tone bar. |
| Tone Bar Edge Offset | TNEO | 0 or more | How far each tone bar stops short of the outline. |

When the count is above 0, the angle, spacing, width and height are required.

### Soundhole braces

| Field | Abbreviation | Limits | Meaning |
|---|---|---|---|
| Soundhole Braces | SHB | ticked or not | Whether the soundhole has its three braces. When ticked, the offset, width and height are required. |
| Soundhole Brace Offset | SHBO | 0 or more | Distance from the edge of the soundhole to the near side of each brace. |
| Soundhole Brace Width | SHBW | more than 0 | Width of every soundhole brace. |
| Soundhole Brace Height | SHBH | more than 0 | Height of every soundhole brace. |

### Bridge patch

| Field | Abbreviation | Limits | Meaning |
|---|---|---|---|
| Bridge Patch Thickness | BPT | more than 0 | Thickness of the plate glued under the top, centered under the bridge. |
| Bridge Patch Width Offset | BPWO | any (negative allowed) | How much wider the patch is than the bridge, along the centerline; negative is narrower. |
| Bridge Patch Offset | BPO | any (negative allowed) | How far the patch is moved along the centerline from centered under the bridge: positive toward the neck end, negative toward the tail end. |

The bridge patch fields are saved with the design, but the patch isn't drawn until the bridge's footprint is defined.

## Geometry

Plan view coordinates: the tail end at y = 0, the body length up +Y, treble toward +X. The copy beside the plate is the same drawing mirrored. Until the plate outline is complete, nothing is drawn, and a remark says the bracing waits for the outline.

Where a brace runs toward the outline, its end follows the outline, moved inside it by the brace's edge offset (a blank edge offset counts as 0). Where it starts or stops at another brace, its end runs along that brace's side.

1. **Centerline:** a construction line from (0, 0) to (0, Body Length).
2. **Headblock and tailblock** (fields on the Body tab: HBW, HBD, TBW, TBD): the headblock is HBD deep from the neck end and the tailblock TBD deep from the tail end, each HBW or TBW wide, centered on the centerline. The side against the end follows the outline.
3. **Upper transverse bar:** a bar across the guitar, UTW wide, its edge nearest the neck end UTO from the neck end. Its center is at y = Body Length - UTO - UTW/2.
4. **Graft:** drawn when Graft is ticked, its offset and width are set, and it fits. It is built like the upper transverse bar, GRW wide, its edge nearest the neck end GRO from the neck end. If the space between the headblock and the upper transverse bar (UTO minus HBD) is narrower than GRW, the graft is left out, with a warning from the model. When it fits, GRO must be at least HBD and GRO plus GRW no more than UTO, or the model reports an error.
5. **X brace:** the legs cross on the centerline, XBO from the neck end, each XBA from the centerline. One bar runs from the bass side lower leg through the crossing to the treble side upper leg. The other bar is cut at the crossing: its two legs (the bass side upper leg and the treble side lower leg) end against the first bar's sides. An upper leg that reaches the upper transverse bar before the outline ends against the bar.
6. **Finger braces:** run from the outer side of each lower leg toward the lower bout's sides. The k-th brace on a side is centered k times the spacing along the leg's centerline from the crossing. At an angle of 0 it runs square to the leg; a positive angle turns it toward the neck end, a negative one toward the tail end.
7. **Tone bars:** run from the inner side of the treble side lower leg, inside the X, toward the tail, placed by the spacing along that leg and the angle the same way. A tone bar can't cross the bass side lower leg.
8. **Soundhole braces:** each sits the Soundhole Brace Offset outside the soundhole. The bottom one is straight across on the tail end side. The bass and treble ones are 120 degrees around from it. Each runs both ways until it meets another brace, or the outline when it meets none. They need a soundhole.

The finger braces and tone bars are placed on the X brace, so they're drawn only when the X brace is.

Braces are named in the order drawn: The headblock, The tailblock, The upper transverse bar, The graft, The X brace, The X brace's bass side upper leg, The X brace's treble side lower leg, Treble finger brace 1.., Bass finger brace 1.., Tone bar 1.., then The bottom, treble and bass soundhole braces.

Each brace is built as a closed outline. Everything is previewed in Rhino's feedback color, and the centerline in light gray, as for the other tabs. Built objects go on the `GuitarForm::Outline` layer and the centerline on `GuitarForm::Construction`.

## Messages

The Messages bar below the tabs shows these for the bracing. Values the model reports as errors (for example an angle out of range, or a count above 0 with the fields it needs blank) leave their brace out with the model's message and none of their own.

| Level | When |
|---|---|
| Remark | A brace has some but not all of the fields it needs: "Set the X Brace Width (XBW) to draw the X brace." |
| Remark | A finger brace or tone bar count is above 0 but the X brace isn't drawn: "The finger braces and tone bars are placed on the X brace: set it to draw them." |
| Remark | The body outline can't be drawn, so the bracing waits for it. |
| Error | A brace doesn't fit: "<Name> doesn't fit inside the outline." |
| Error | A finger brace or tone bar is past the end of its leg: "Treble finger brace 2 is past the end of the X brace's treble side lower leg: ..." It and the ones after it are left out; the earlier ones are drawn. Reduce the count or the spacing. |
| Error | A tone bar would cross the bass side lower leg before reaching the outline: "Tone bar 1 runs into the X brace's bass side lower leg ..." That bar is left out; turn it toward the tail with a lower angle, or move it with the spacing. |
| Error | The plate outline has a piece that isn't a line or an arc, so the bracing can't be drawn. |

## Quick Test

`Examples/Quick Test.json` holds these values. They are the values `BracingShapeTests` uses, on the Body tab's Quick Test body (see [Body.md](Body.md#quick-test)) with its soundhole 100 across, centered 380 from the tail end. In a document in millimeters, with the Body tab set as in the Body Quick Test (UbSO 30 and LbSO 40 included) and HW 56:

| Field | Value |
|---|---|
| HBW, HBD, TBW, TBD (Body tab) | 70, 20, 90, 15 |
| XBO, XBA, XBEO, XBW, XBH | 300, 50, 5, 8, 15 |
| Graft, GRT, GRW, GRO, GREO | ticked, 2, 60, 30, 10 |
| UTO, UTW, UTH, UTEO | 130, 20, 12, 2 |
| BFNC, BFNA, BFNS | 3, 10, 50 |
| TFNC, TFNA, TFNS | 2, 0, 60 |
| FNW, FNH, FNEO | 6, 8, 3 |
| TNC, TNS, TNA, TNW, TNH, TNEO | 2, 80, -20, 7, 9, 4 |
| SHB, SHBO, SHBW, SHBH | ticked, 3, 6, 6 |

Expected results, all checked in `BracingShapeTests`:

| Check | Result |
|---|---|
| Braces drawn | 17, in the order listed above, each a closed outline, with no messages. |
| Headblock | From y = 580 to 600, x = -35 to 35. |
| Tailblock | From y = 0 to 15; its corners against the tail end are on the curved outline. |
| Upper transverse bar | From y = 450 to 470. Its ends follow the upper waist tangent line, 2 inside it. |
| Graft | From y = 510 to 570. Its ends follow the upper bout arcs, 10 inside them. |
| X brace | The legs cross at (0, 300). The through bar's corners are all 4 (half the width) from its centerline, which is 50 degrees from the centerline of the guitar. The two cut legs start on its sides 4 from that centerline, and their outer ends are 5 inside the outline. |
| Treble finger braces | Square to the lower leg (angle 0), from its outer side, centered 60 and 120 along it from the crossing; their outer ends are 3 inside the outline. |
| Bass finger braces | On the bass side, turned 10 degrees toward the neck end from square to the leg. |
| Tone bars | From the inner side of the treble side lower leg, turned 20 degrees toward the tail from square to the leg. |
| Bottom soundhole brace | From y = 321 to 327 (the soundhole's bottom edge is at y = 330, less the offset 3 and the width 6), ending on the facing sides of the X's upper legs. |
| Treble and bass soundhole braces | Their top edge is y = 450, against the upper transverse bar's underside. |
| Copy beside the plate | Its right edge is the drawing gap (50) left of the plate's leftmost point, and Treble finger brace 1 is left of its centerline. |

Changes to try:

| Change | Result |
|---|---|
| Tone Bar Angle to 60 | Error: "Tone bar 1 runs into the X brace's bass side lower leg ...", and no tone bars are drawn. |
| Treble Finger Brace Spacing to 200 | Treble finger brace 1 is drawn; an error says brace 2 is past the end of the leg. |
| UTO to 200 (SHB and Graft unticked) | The bar spans y = 380 to 400. The X's upper legs end against it at y = 380. |
| Clear X Brace Width | Remark: "Set the X Brace Width (XBW) to draw the X brace." and a second remark that the finger braces and tone bars need it. |
| Clear every bracing field and the block fields except HBD and TBD | No braces are drawn, so no copy of the plate appears. Remarks say to set the Headblock Width and the Tailblock Width (checked in `Drawing_WithoutBracing_HasNoCopyOfThePlate`). |

Not checked by a test: the exact corner coordinates of the finger braces, tone bars, graft ends and soundhole brace ends. Look at them in Rhino instead of expecting numbers here.

## Open questions

- The bridge patch (BPT, BPWO, BPO) isn't drawn until the bridge's footprint is defined, and no brace's height is drawn (so XBPT changes nothing visible). Is that intended until the bridge and a view that shows heights are designed?
