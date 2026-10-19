# Fretboard tab

The fretboard: its width and string spacing, frets, thickness and radius, and the dot markers. Every field's meaning is in [Glossary.md](Glossary.md); this page covers how the fields work together and what is drawn.

The fretboard is drawn twice: from above, in place over the plate, and from the side, on the body's side view. Both use values from other tabs too: the Scale Length and Neck Join Fret on the [Instrument tab](Instrument.md), the Body Length on the [Body tab](Body.md), and the Neck Angle and Overstand on the [Neck tab](Neck.md). The Neck tab's profiles use the Fretboard Radius and thicknesses from this tab. Lengths are shown and typed in the Rhino document's units and stored in millimeters; see [Units](../README.md#units).

## Fields

Limits come from the model. A field left blank is unset and has no default. The groups below are the boxes on the tab.

### Width and string spacing

| Field | Abbreviation | Limits | Meaning |
|---|---|---|---|
| Nut Width | NTW | more than 0 | The fretboard's width at the nut. It must be more than the two setbacks together. |
| String Spacing at Bridge | SSB | more than 0 | Distance between the first and last strings' centerlines at the saddle. With the nut width, it sets the taper. |
| Bass String Setback | BSS | 0 or more | At the nut, the distance from the bass edge to the outermost bass string's centerline. |
| Treble String Setback | TSS | 0 or more | The same on the treble side. |

### Courses

| Field | Abbreviation | Limits | Meaning |
|---|---|---|---|
| Strings per Course | SPC | 1 or 2 | The number of strings in each course: 1 for a six-string, 2 for a twelve-string. |
| Course String Spacing at Nut | CSN | more than 0 | For courses of two, the distance between the two strings of a course at the nut. |
| Course String Spacing at Bridge | CSB | more than 0 | The same at the saddle. |

The two course spacings are disabled (grayed, not hidden) unless Strings per Course is 2. A disabled field keeps its value, so changing SPC to 1 and back loses nothing. When SPC is 2 the model requires both.

### Frets

| Field | Abbreviation | Limits | Meaning |
|---|---|---|---|
| Fret Count | FC | 1 or more | The number of frets. The Neck Join Fret can't be past it. |
| Last Fret Overhang | LFO | 0 or more | Length of fretboard beyond the last fret, to the body end of the fretboard. |

### Fret slots

| Field | Abbreviation | Limits | Meaning |
|---|---|---|---|
| Fretboard Slot Overage | FBSO | 0 or more | Extra slot depth added to the fretwire's Slot Depth, for when the fretboard's thickness tapers more than the wire's recommended depth covers. At 0 or blank, there is no extra. |

The Fretboard Slot Overage deepens the slot lines drawn in the side view (see Side view below).

### Thickness and radius

| Field | Abbreviation | Limits | Meaning |
|---|---|---|---|
| Fretboard Thickness at Nut | FBTN | more than 0 | Thickness at the nut end, at the centerline. |
| Fretboard Thickness at End | FBTE | more than 0 | Thickness at the body end, at the centerline. |
| Fretboard Radius | FBR | more than 0 | Radius of the playing surface across the width, constant along the length. |

### Dot markers

| Field | Abbreviation | Limits | Meaning |
|---|---|---|---|
| Face Dot Diameter | FDD | more than 0 | Diameter of the dots on the playing surface. |
| Double Dot Edge Distance | DDED | 0 or more | For a fret with two dots, the distance from each fretboard edge to the near edge of the nearer dot. |
| Side Dot Diameter | SDD | more than 0 | Diameter of the position dots on the bass edge of the fretboard. |
| Side Dot Offset | SDO | 0 or more | Distance from the fretboard's bottom face to the center of the side dots. |
| Side Double Dot Spacing | SDDS | more than 0 | For a fret with two side dots, the distance between the dots' centers. |

The side dots use the same marker list as the face dots, and are drawn in the side view only (see Side view below).

Below these fields is the **marker list**: a text box of frets, separated by spaces or commas, with `:2` after a fret for two dots, for example `3 5 7 9 12:2`. A fret without `:2` has one dot. Each fret can be listed only once, and none can be past the last fret. Text that isn't understood is not applied.

### Setup (information only)

| Field | Abbreviation | Limits | Meaning |
|---|---|---|---|
| Fretboard Relief | FBRL | 0 or more | The slight forward bow of the playing surface along its length. |
| Fretboard Relief Fret | FBRF | 1 or more; not past the Fret Count | The fret at which the relief is measured. |
| Treble String Action | TSA | more than 0 | The treble string's action. |
| Bass String Action | BSA | more than 0 | The bass string's action. |

These are saved with the design, shown in the exported JSON and checked by the model (for example a Relief Fret past the last fret is an error), but no shape uses them: they never change the drawing. Meanings are in the Glossary.

The string set the strings are spaced for is chosen on the [Strings tab](Strings.md), and the fretwire on the [Frets tab](Frets.md). There is no picker on this tab.

The **Show strings** box at the top of this tab hides or shows the strings in the preview and in Build. It is a plug-in setting for all designs, kept between sessions, not part of the design.

## Plan view

The guitar is drawn vertically, tail end at the origin, treble toward +X. The neck join fret is at the body's neck end (y = Body Length). The nut is that fret's distance beyond it (+Y), and the saddle is the Scale Length from the nut, without compensation:

- Nut at y = Body Length + the distance of the Neck Join Fret from the nut.
- Saddle at y = nut - Scale Length.
- Frets are at equal-tempered positions.

The drawing is made as far as the fields allow:

1. **Strings** (green, final outline): each string is a closed rectangle as wide as its diameter (from the string set), from the nut to the saddle, centered on the string's line and square to it. The outer strings run from the nut, at the setbacks in from the fretboard's edges, to the saddle, at half the String Spacing at Bridge each side of the centerline. With a string set that has a diameter on every string, all of its strings are drawn, spaced so the gaps between neighboring strings' edges are equal, at the nut and at the saddle. Without a usable set (or if the strings overlap), only the two outer strings are drawn, as center lines, since there is no diameter to draw. Needs the Nut Width, String Spacing at Bridge and both setbacks.
2. **Frets** (orange): a center line across the fretboard, edge to edge, at each fret from the first, marking where the tang goes. With a fretwire that has a Bead Width, each fret is also a closed four-sided shape (final outline) as wide as the Bead Width along the fretboard, centered on its fret line, with its ends on the fretboard's tapered edges. Without a Bead Width only the center lines are drawn, with a Remark. Needs the Fret Count as well.
3. **Fret slots:** each is a thin rectangle centered on its fret line, as wide as the chosen fretwire's Tang Thickness. They're drawn only when the neck has a fretwire with a tang thickness, which is chosen on the [Frets tab](Frets.md).
4. **Face dots:** halfway between a marker's fret and the fret before it (the nut for fret 1). A single dot is centered on the centerline. A pair is each Double Dot Edge Distance in from its edge (so each center is that distance plus the radius in from the edge). Markers past the last fret are skipped. Needs the Face Dot Diameter.
5. **Outline** (the final outline): a four-sided shape, from the nut to the body end square to the centerline, Last Fret Overhang past the last fret. It is Nut Width wide at the nut and as wide as the String Spacing at Bridge plus both setbacks at the saddle, with straight edges between. Needs the Last Fret Overhang as well.

## Side view

The fretboard is also drawn on the side view, cut along the centerline. It is drawn once the side view exists (the Body tab's depths are set) and the Neck Angle, Overstand, both thicknesses, Fret Count, Last Fret Overhang, Scale Length and Neck Join Fret are set.

- The underside is the Overstand above the top at the body's neck end, measured square to the top's line, and the neck join fret is there.
- A positive Neck Angle tips the nut end toward the back, measured from the top's line, so it follows the Body Profile Skew.
- Lengths along the fretboard are the plan view's fret distances.
- The thickness tapers evenly from the Fretboard Thickness at Nut to the one at End.
- The outline runs clockwise from the underside at the nut: the nut end, the playing surface, the body end and the underside.

The Neck tab's side view and profiles are drawn on this outline.

**Side dots** (final outline circles) are drawn on the side view only. A marker's dots are at the same place along the fretboard as its face dots: halfway between its fret and the fret before it (the nut for fret 1). The Side Dot Offset is measured up from the fretboard's underside to the dot's center. A single dot is centered on that place; a pair is centered on it too, the Side Double Dot Spacing apart (center to center) along the fretboard, so a larger spacing only widens the pair. Any count other than 2 is drawn as a single dot, as for the face dots. Needs the Side Dot Diameter and Offset (and the spacing for a pair); markers past the last fret are skipped.

**Fret slot lines** (final outline) are drawn at each fret, from the playing surface at the centerline straight down to the slot bottom. The slot depth is the chosen fretwire's Slot Depth plus the Fretboard Slot Overage (0 if blank). The slot bottom is one flat level, parallel to the underside, that depth below the playing surface where the fretboard is thickest (the larger of the two thicknesses, since it tapers evenly), so the lines are shorter where the board is thinner. Only the line is drawn, not the slot's thickness. Needs a fretwire with a Slot Depth.

## Messages

The Messages bar below the tabs shows these for the fretboard (the model's own errors, for example a marker past the last fret or setbacks that leave no room at the nut, are shown as well):

| Level | When |
|---|---|
| Remark | Values are missing: "Set the Nut Width (NTW), String Spacing at Bridge (SSB), Bass String Setback (BSS) and Treble String Setback (TSS) to draw the fretboard." Likewise for the frets, face dots, double face dots, outline and the side view, which name what each needs. |
| Error | The scale length, nut width or string spacing isn't more than 0, or a setback is negative: the fretboard can't be drawn. |
| Error | Strings per Course isn't 1 or 2, so the strings can't be drawn. |
| Remark | The neck has no fretwire (or its Bead Width isn't set): "Choose a fretwire on the Frets tab to draw the frets at their width." Only the fret center lines are drawn. |
| Warning | The strings overlap: there isn't room for them between the setbacks or across the String Spacing at Bridge. Only the two outer strings are drawn then. |
| Error | A thickness, the Scale Length or the Fret Count is zero or less: the fretboard can't be drawn in the side view. |
| Remark | The Side Dot Diameter or Offset isn't set while there are markers (or another side-dot field is set): "Set the Side Dot Diameter (SDD) and Offset (SDO) to draw the side dots." |
| Remark | A marker has two dots but no Side Double Dot Spacing: that marker's dots aren't drawn. |
| Warning | A side dot's top (offset plus half the diameter) is above the fretboard's top at its edge there, which is lower than the centerline by the Fretboard Radius's drop at half the width: "The side dot at fret N would break through the top of the fretboard at its edge". One per marker; needs the Fretboard Radius. |
| Remark | The neck has a fretwire but no Slot Depth: "Set the fretwire's Slot Depth (FWSD) to draw the fret slots in the side view." (With no fretwire, the Frets tab's remark is the only one.) |
| Warning | The slot depth is at least the thickest part of the fretboard: the fret slots would cut through it, and no slot lines are drawn. |
| Warning | The slot bottom is above the playing surface at some frets (the fretboard tapers more than the slot depth): no slot line is drawn there. |

## Quick Test

The tests use the Quick Test body (Body Length 600, Scale Length 645, Neck Join Fret 14) with these values. In a document in millimeters:

| Field | Value |
|---|---|
| NTW, SSB, BSS, TSS | 44, 54, 3.5, 3.5 |
| FC, LFO | 20, 5 |
| FBTN, FBTE | 7, 6 |
| FDD, DDED | 6, 8 |
| Markers | 3 12:2 |
| Neck Angle, Overstand | 1.5, 2 |

Expected results, all checked in `FretboardShapeTests`:

| Check | Result |
|---|---|
| Nut and saddle | The nut is at y = 957.685 and the saddle at y = 312.685. |
| Outline | Corners at (-22, 957.685), (22, 957.685), (27.889, 510.847) and (-27.889, 510.847): 44 wide at the nut, tapering to 55.777 at the body end. The saddle is 61 wide (54 plus both setbacks). |
| Frets (center lines) | 20 lines. The first ends at (22.477, 921.484). Fret 14 is at y = 600, the body's neck end. |
| Dots | Three circles of radius 3: one at (0, 871.189) for fret 3, and a pair at (+/-15.124, 644.774) for fret 12. |
| Strings (no string set) | Two center lines, as there is no diameter: (18.5, 957.685) to (27, 312.685), and its mirror image on the bass side. |
| Strings (a six-string set of .012 .016 .024 .032 .042 .053 inch, treble first) | At the nut x = 18.5, 11.489, 4.325, -3.042, -10.638 and -18.5. At the saddle x = 27, 16.589, 6.025, -4.742, -15.738 and -27. |
| Strings (the twelve-string set in the test, Strings per Course 2, CSN 2, CSB 3) | At the nut x = 18.5, 16.5, 11.741, 9.741, 4.818, 2.818, -2.119, -4.119, -9.221, -11.221, -16.5 and -18.5. (Type Strings per Course 2, then the two spacings, to try it.) |
| Frets at bead width | With a fretwire whose Bead Width is 2.5, 20 closed four-sided shapes beside the 20 center lines. Fret 14 (y = 600) has its corners at y = 601.25 and 598.75 on the tapered edges, so it is 2.5 wide. Without a fretwire there are none, and the Remark above appears. (Checked by a test; in the panel, choose the fretwire on the Frets tab.) |
| String rectangles | With the six-string set, six closed rectangles. The first (.012 in, 0.3048 wide) has the middle of its ends at (18.5, 957.685) and (27, 312.685); the last is .053 in wide. With the twelve-string set there are twelve, each as wide as its string's diameter. |
| Slots | With a fretwire whose Tang Thickness is 0.53, 20 slots each 0.53 wide, the first centered on y = 921.484. (Checked by a test; in the panel, choose the fretwire on the Frets tab.) |
| Side view | With the Body tab's TD 120 and ND 95, and the side view's bottom line at x = 240 (the Body Quick Test), the fretboard's corners are (352.637, 957.563), (359.634, 957.746), (370.332, 511.035) and (364.334, 510.878): underside at the nut, top at the nut, top at the end and underside at the end. |

Side dots and slot lines, with the Quick Test body's depths (TD 120, ND 95) and the side view at x = 240, Fretboard Radius 241, SDD 2, SDO 3, SDDS 6, and a fretwire with Slot Depth 2 (checked in `FretboardSideShapeTests`; in the panel, choose the fretwire on the [Frets tab](Frets.md)):

| Check | Result |
|---|---|
| Side dots | Three circles of radius 1: one for fret 3, and a pair for fret 12. Each is 3 above the underside. The pair is centered halfway between frets 11 and 12 along the fretboard, 3 each side of it (6 apart). |
| Larger SDDS | With SDDS 10 the pair is 5 each side; the group stays centered. |
| Breakthrough | With SDO 4.5, one Warning, for fret 12 (the dot's top is 5.5; the edge there is about 5.07 thick). Fret 3's edge is about 5.87, so it doesn't warn. With SDO 3 there is no warning. |
| Slot lines | 20 lines, one per fret, each from the playing surface down to 5 above the underside (the thickest is 7 at the nut, less the depth 2). With FBSO 0.5 the bottom is 4.5 up; with slot depth 2 + FBSO 0.4, 4.6 up. |
| Line lengths | Fret 1's is 1.919 and fret 20's 1.011: shorter where the board is thinner. |
| No Slot Depth | No lines, and the Remark above. |
| Slot Depth 6.5 and FBSO 0.5 | The depth equals the 7 thickness: Warning that the slots cut through, and no slot lines. |
| Slot Depth 0.3 | The bottom is 6.7 up: fewer than 20 lines and a Warning naming the frets left out. |

Changes to try:

| Change | Result |
|---|---|
| Nut Width to 0 | Error: the fretboard can't be drawn, and no outline appears. |
| Clear the Neck Angle | Remark: "Set the Neck Angle (NKA) to draw the fretboard in the side view." and no side-view outline. |
| Clear the Nut Width | The outline, frets, dots and strings disappear, and a remark names what to set. |
| A marker fret above the Fret Count | An error from the model, and the marker's dots aren't drawn. |

Not checked by a test: the appearance of the preview and the built layers (see Open questions).

## Open questions

- Decided: the fretwire (which also supplies the slot width and depth) is chosen only on the [Frets tab](Frets.md), the way string sets are chosen on the Strings tab.
- Decided: each component is previewed and built in its own color on its own layer (frets orange, strings green); construction geometry is light gray on a Construction sublayer. See [Layers and colors](../README.md#layers-and-colors).
- Construction lines are printed at the outline weight. Is that right, or should they be thinner or not printed?
