# Neck tab

The neck: its angle and overstand, the thickness at the first and selected frets, the shape of its profile (cross-section) at the first fret, the selected fret and through the heel, the heel shaft, and the heel end, heel cap and heel section. The fretboard, nut and headshape have their own tabs. Every field's meaning is in [Glossary.md](Glossary.md); this page covers how the profile fields work together.

The profiles are drawn beside the side view, fretboard (or heel/body joint) side up with the treble side to the right. Lengths are shown and typed in the Rhino document's units and stored in millimetres; see [Units](../README.md#units).

## Profiles

A neck profile is a cross-section through the neck, drawn fretboard (or heel/body joint) side up with the treble side to the right. Four are drawn, called stations:

- **First fret**, at the first fret, set by Neck Thickness at First Fret.
- **Selected fret**, at the Neck Thickness Fret, set by Neck Thickness at Selected Fret.
- **Heel section** and **heel end**, through the heel at those places, set by Heel Section Thickness and Heel End Thickness.

Each station has its own squareness, asymmetry and blend, and the heel stations also have a side length. The shape of each profile is only the part below the shoulder; the top is fixed by the fretboard (neck stations) or by the heel/body joint (heel stations).

### Basic and advanced mode

**Profile Advanced Mode (PADV)** is one setting for all four stations. The same curve is described two ways:

| Mode | Fields used | What sets the shape |
|---|---|---|
| Basic (unticked, the default) | Squareness, Asymmetry | Squareness; any Blend value is ignored. |
| Advanced (ticked) | Blend, Asymmetry | Blend; any Squareness value is ignored. |

Use basic mode to set an exact exponent, and advanced mode to move between the V, the round C and the D with one control. The values you aren't using are kept, so switching modes loses nothing.

| Field | Abbreviations (first fret / selected fret / heel section / heel end) | Range | When blank |
|---|---|---|---|
| Squareness | PSQF / PSQS / PSQH / PSQE | 1 to 10 | 2 (a round C) |
| Asymmetry | PASF / PASS / PASH / PASE | -50 to 50 (%) | 0 |
| Blend | PBLF / PBLS / PBLH / PBLE | 0 to 100 (%) | 50 (a round C) |

Blank values use the defaults silently, with no message.

### Squareness

Each half of the profile is a superellipse quadrant. With half-width a, height b and squareness n:

x = a cos^(2/n) t, y = b (1 - sin^(2/n) t), for t from 0 (the shoulder, where the curve is vertical) to 90 degrees (the apex, where it is level)

| n | Shape |
|---|---|
| 1 | V: straight sides meeting at the apex |
| 1.5 | Soft V |
| 2 | Round C: a half ellipse |
| 3.2 | D |
| 4 and up | Squarer: the sides stay wide lower down and turn sharply into the bottom |

The profile's width and depth don't depend on n, only how full the curve is between them.

### Asymmetry

Asymmetry moves the apex off the neck's centerline: by half-width x asymmetry / 100, toward the treble side when positive, toward the bass side when negative. The shoulders stay where they are, so the profile's overall width and depth don't change. With half-width a and asymmetry s (as a fraction, so 20% is 0.2), the treble half is a(1 - s) wide and the bass half a(1 + s), measured from the apex to its shoulder. Each half keeps the same squareness or blend, so the shallower side is steeper. Asymmetry applies in both modes.

### Blend

Blend is a position along three reference shapes: the V (n = 1) at 0%, the round C (n = 2) at 50% and the D (n = 3.2) at 100%. A blend between two neighbouring references is the point-for-point mix of their curves: for the same angle t, each point is (1 - w) x the lower reference's point + w x the upper's, where w is how far along between the two the blend is (blend / 50 between 0 and 50, and (blend - 50) / 50 between 50 and 100). It isn't a curve of any single squareness, so blend 25 is close to, but not the same as, squareness 1.5.

Examples, at t = 45 degrees, as a fraction of the half-width across from the apex and of the height up from the bottom:

| Blend | Mix | Point (across, up) |
|---|---|---|
| 0 | V | 0.500, 0.500 |
| 25 | half V, half round C | 0.604, 0.396 |
| 50 | round C | 0.707, 0.293 |
| 75 | half round C, half D | 0.756, 0.244 |
| 100 | D | 0.805, 0.195 |

### Starting shapes

Each profile group has a **Starting shape** list. Choosing one fills in that profile's squareness, asymmetry and blend, which you can then adjust. Nothing remembers which shape was chosen.

| Starting shape | Squareness | Asymmetry | Blend |
|---|---|---|---|
| Modern C | 2.0 | 0 | 50 |
| Soft V | 1.5 | 0 | 25 |
| Hard V | 1.1 | 0 | 5 |
| D | 3.2 | 0 | 100 |
| Asymmetric | 2.0 | 15 | 50 |

### Other profile settings

- **Profile Includes Fretboard** (PIFF, PIFS), at the first and selected frets, says whether the shoulder starts at the top of the fretboard edge (ticked) or at its underside (unticked). Ticked, the profile outline includes the fretboard's edge, and the Neck Thickness must be more than the drop of the fretboard's surface at its edge. The heel profiles have no fretboard, so don't use it.
- **Profile Side Length** (PSLH, PSLE), at the heel section and heel end only, is the length of the heel's straight sides running down from the heel/body joint before the curves start. It must be 0 or more (at 0 the curves start at the joint) and less than the thickness there.
- **Profile Uses First Fret** (PUFS, PUFH, PUFE) makes that profile use the first fret's squareness, asymmetry and blend instead of its own. Its own thickness (and, for the heel, width and side length) still apply. The selected fret also takes the first fret's Profile Includes Fretboard.

Profiles saved before this version used a bottom radius, shoulder angle, midshoulder radius and center offset. Those values aren't converted: they're dropped when the design is opened, and every profile starts from the defaults above.

## Geometry

A neck profile's top is the fretboard's playing surface, an arc of the Fretboard Radius across the fretboard's width at that fret, which must be at least half that width. Its fretboard is as thick at the centerline as the taper from Fretboard Thickness at Nut to Fretboard Thickness at End makes it there, and the neck thickness must be more than that. A heel profile's top is the heel/body joint, a straight line as wide as the heel, with the heel's straight sides running down from it by the Profile Side Length.

Below that, the profile runs from the shoulder start down to the bottom, which is the neck thickness below the crown (the heel thickness below the joint). The shoulder start is the top or underside of the fretboard edge (per Profile Includes Fretboard), or the end of the heel's straight side. The curve is sampled by splitting the quarter turn into eight pieces and subdividing each until its chord is within half the document tolerance of the curve (down to six levels), so squarer curves get more points.

The neck profiles share a centerline, the drawing gap right of the side view, each centered on the height of its fret on the playing surface. The heel section and then the heel end follow, each the drawing gap further right, level with the heel.

## Quick Test

The tests use a 645 scale, a 600 body, 20 frets, a 44 nut and 54 at the bridge, a 400 fretboard radius and thicknesses of 7 at the nut and 6 at the end. In a document in millimetres, with Neck Thickness at First Fret 21, Neck Thickness at Selected Fret 23, Neck Thickness Fret 9 and Profile Includes Fretboard unticked at the first fret:

| Field | Value | Result |
|---|---|---|
| (defaults) | | First fret profile is a round C, 44.954 wide and 21 from crown to bottom. Its fretboard is 6.919 thick, so the neck below the underside is 14.081 deep, and the sides are half ellipses. |
| Squareness at first fret | 1 | The sides become straight lines meeting at the apex (a V). |
| Squareness at first fret | 5 | A squarer D: the same width and depth, fuller at half depth. |
| Asymmetry at first fret | 20 | The apex moves 4.495 (20% of half the width, 22.477) toward the treble side; the width and depth stay 44.954 and 21. |
| Profile Advanced Mode | ticked | Squareness is ignored. Blend 0, 50 and 100 give the same outlines as squareness 1, 2 and 3.2. |
| Blend at first fret (advanced) | 25 | Between the V and the round C. Blend 75 is between the round C and the D. |
| Uses First Fret at selected fret | ticked | The profile at fret 9 copies the first fret's shape, but is 23 deep. |
| Heel Section Width, with Heel Outline Straight Sides ticked and Heel End Width 60 | | The heel section is 57.785 wide, on the line from the heel end's width to the fretboard's width at the neck end. |

The example `Examples/Quick Test.json` sets squareness 2 and asymmetry 0 at the first fret, 2.2 at the selected fret and 2.5 at the heel section and end, for a neck that fills out toward the heel. The tests in `NeckProfilesShapeTests` check these values.
