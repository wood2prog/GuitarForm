# Neck tab

The neck: its angle and overstand, the thickness at the first and selected frets, the shape of its profile (cross-section) at the first fret, the selected fret and through the heel, the heel shaft, and the heel end, heel cap and heel section. The fretboard, nut and headshape have their own tabs. Every field's meaning is in [Glossary.md](Glossary.md); this page covers how the profile fields work together.

The profiles are drawn beside the side view, fretboard (or heel/body joint) side up with the treble side to the right. Lengths are shown and typed in the Rhino document's units and stored in millimetres; see [Units](../README.md#units).

## Profile mode

**Profile Advanced Mode (PADV)** switches the profile fields between two ways of describing the same shape. Each profile, at the first fret, the selected fret, the heel section and the heel end, has its own squareness, asymmetry and blend.

| Mode | Fields shown | What sets the shape |
|---|---|---|
| Simple (unticked, the default) | Squareness, Asymmetry | Squareness alone; any Blend value is ignored. |
| Advanced (ticked) | Blend, Asymmetry | Blend alone; any Squareness value is ignored. |

| Field | Abbreviations (first fret / selected fret / heel section / heel end) | Range | When blank |
|---|---|---|---|
| Squareness | PSQF / PSQS / PSQH / PSQE | 1 to 10 | 2 (a round C) |
| Asymmetry | PASF / PASS / PASH / PASE | -50 to 50 (%) | 0 |
| Blend | PBLF / PBLS / PBLH / PBLE | 0 to 100 (%) | 50 (a round C) |

- **Squareness** is how square the back of the neck is: 1 is a V, 2 a round C, and higher values a squarer D.
- **Asymmetry** moves the fullest point (the apex) of the profile off the neck's centerline, as a percentage of half the profile's width: positive toward the treble side, negative toward the bass side. The profile's overall width and depth don't change.
- **Blend** (advanced mode) is a position along three reference shapes: 0 is a V (squareness 1), 50 a round C (squareness 2) and 100 a D (squareness 3.2). Between two references the outlines are blended point for point.
- **Profile Includes Fretboard** (PIFF, PIFS) says whether the shoulder starts at the top of the fretboard edge (ticked) or at its underside (unticked).
- **Profile Uses First Fret** (PUFS, PUFH, PUFE) makes that profile use the first fret's squareness, asymmetry, blend and mode instead of its own.
- **Side length** (PSLE, PSLH) is the length of the heel's straight sides at the heel end and heel section. It must be 0 or more (at 0 the curves start at the joint) and less than the thickness there.

Each profile group has a **Starting shape** list. Choosing one fills in that profile's squareness, asymmetry and blend, which you can then adjust:

| Starting shape | Squareness | Asymmetry | Blend |
|---|---|---|---|
| Modern C | 2.0 | 0 | 50 |
| Soft V | 1.5 | 0 | 25 |
| Hard V | 1.1 | 0 | 5 |
| D | 3.2 | 0 | 100 |
| Asymmetric | 2.0 | 15 | 50 |

Profiles saved before this version used a bottom radius, shoulder angle, midshoulder radius and center offset. Those values aren't converted: they're dropped when the design is opened, and every profile starts from the defaults above.

## Geometry

A neck profile's top is the fretboard's playing surface, an arc of the Fretboard Radius across the fretboard's width at that fret. Its fretboard is as thick at the centerline as the taper from Fretboard Thickness at Nut to Fretboard Thickness at End makes it there. A heel profile's top is the heel/body joint, a straight line as wide as the heel, with the heel's straight sides running down from it by the Profile Side Length.

Below that, the profile runs from the shoulder start down to the bottom, which is the neck thickness below the crown (the heel thickness below the joint). The shoulder start is the top or underside of the fretboard edge (per Profile Includes Fretboard), or the end of the heel's straight side. Each half is a superellipse quadrant that is vertical at the shoulder and level at the apex. With half-width a, height b and squareness n:

x = a cos^(2/n) t, y = b (1 - sin^(2/n) t), for t from 0 to 90 degrees

so n = 1 is a straight-sided V, n = 2 a half ellipse (a round C) and larger n squarer. Asymmetry shifts the apex by that percentage of a, so the two halves have half-widths a(1 - asymmetry) and a(1 + asymmetry) toward the treble and bass sides. In advanced mode the squareness is ignored and the blend mixes the reference shapes n = 1 (blend 0), n = 2 (50) and n = 3.2 (100) point for point.

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
