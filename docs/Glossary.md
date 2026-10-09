# Glossary

The name and abbreviation of every design setting. The plug-in uses these everywhere it refers to a setting: field labels, messages, the docs and the setting search.

- Each abbreviation is 2–4 capital letters and unique across the whole plug-in.
- Words that recur are always abbreviated the same way:
  - A part the setting is about has a subject code: Fretboard is FB, Neck is NK, Nut is NT, String (one string of a set) is ST (e.g. NTW, FBTN, STD).
  - A position the setting is measured at has a one-letter suffix: at Nut is N, at End is E, at Bridge is B (e.g. FBRN, FBTE, SSB).
- A setting that appears in more than one place (e.g. a neck profile radius at the first fret and at the heel) gets its own name and abbreviation for each place.

## Instrument

**Instrument Name (IN)**
A short name that identifies this instrument design.

**Designer (DSG)**
The name of the person who designed this instrument.

**Scale Length (SL)**
The vibrating string length from the nut to the saddle. Fret positions are computed from it.

**Neck Join Fret (NJF)**
The fret at which the neck meets the body, e.g. 12 or 14.

## Fretboard

**Fret Count (FC)**
The total number of frets on the fretboard.

**Last Fret Overhang (LFO)**
The length of fretboard beyond the last fret, measured from the last fret to the body end of the fretboard.

**Fretboard Radius (FBR)**
The radius of the fretboard's playing surface across its width, constant along its length.

**Fretboard Radius at Nut (FBRN)** *(planned)*
For a compound radius: the radius of the fretboard's playing surface at the nut. The radius changes evenly along the fretboard to the Fretboard Radius at End.

**Fretboard Radius at End (FBRE)** *(planned)*
For a compound radius: the radius of the fretboard's playing surface at the body end of the fretboard.

**Fretboard Thickness at Nut (FBTN)**
The fretboard's thickness at the nut end, measured at the crown (the centerline).

**Fretboard Thickness at End (FBTE)**
The fretboard's thickness at the body end, measured at the crown (the centerline).

**Nut Width (NTW)**
The fretboard's width at the nut.

**String Spacing at Bridge (SSB)**
The distance between the centerlines of the first and last strings at the saddle. With the Nut Width, it sets the fretboard's taper.

**Fretboard Relief (FBRL)**
The slight forward bow built into the fretboard's playing surface along its length: the gap between the surface and a straight line from the nut to the end of the fretboard. It's measured at the Fretboard Relief Fret when one is given, and is otherwise the greatest gap.

**Fretboard Relief Fret (FBRF)**
Optional. The fret at which the Fretboard Relief is measured.

**Nut Thickness (NTT)**
The nut's thickness along the length of the neck, from its fretboard face to its headstock face.

**Nut Height (NTH)**
The nut's total height, from its base to its top.

**Nut Reveal (NTR)**
The height of the nut's top above the fretboard's playing surface, measured at the centerline.

**Bass String Setback (BSS)**
At the nut, the distance from the bass edge of the fretboard to the centerline of the outermost bass string.

**Treble String Setback (TSS)**
At the nut, the distance from the treble edge of the fretboard to the centerline of the outermost treble string.

**Strings per Course (SPC)**
The number of strings in each course, 1 or 2, the same for every course: 1 for a standard six-string, 2 for a twelve-string.

**Course String Spacing at Nut (CSN)**
For courses of two strings, the distance between the centerlines of the two strings in a course at the nut.

**Course String Spacing at Bridge (CSB)**
For courses of two strings, the distance between the centerlines of the two strings in a course at the saddle.

## String Action

**Treble String Action (TSA)**
At the 12th fret, the gap between the fret crown and the bottom of the first (treble) string.

**Bass String Action (BSA)**
At the 12th fret, the gap between the fret crown and the bottom of the last (bass) string.

## String Sets

**String Set Manufacturer (SSM)**
The company that makes the string set.

**String Set Name (SSN)**
The manufacturer's name or product code for the set, e.g. EJ16.

**String Count (SC)**
The number of strings in the set.

**String Description (STD)**
A short label for the string, e.g. its note (E4) or the manufacturer's part number.

**String Diameter (STDI)**
The string's outside diameter (its gauge).

**String Construction (STC)**
Whether the string is plain or wound.

**String Unit Weight (STUW)**
The string's mass per unit length, as published by the manufacturer. Used to calculate string tension.

**String Hex Core (STHC)**
For a wound string, whether its core is hexagonal (true) or round (false).

**String Core Material (STCM)**
For a wound string, what its core is made of: not specified, solid nylon, nylon floss, steel or gut.

**String Core Modulus (STCE)**
The core's modulus of elasticity (MOE), in GPa. Given only when the core material is not specified.

**String Core Strength (STCS)**
The core's ultimate tensile strength (UTS), in MPa. Given only when the core material is not specified.

**String Used in Saddle Compensation (STSC)**
Whether the string's compensated position is used to fit the straight saddle line.

## Dot Markers

**Face Dot Diameter (FDD)**
The diameter of the position dots on the fretboard's playing surface.

**Double Dot Edge Distance (DDED)**
For a fret with two face dots, the distance from each fretboard edge to the near edge of the nearer dot.

**Side Dot Diameter (SDD)**
The diameter of the position dots on the bass edge of the fretboard.

**Side Dot Offset (SDO)**
The distance from the fretboard's bottom face to the center of the side dots.

**Side Double Dot Spacing (SDDS)**
For a fret with two side dots, the distance between the centers of the two dots.

**Marker Fret (MKF)**
In the marker list, the fret a marker sits at.

**Marker Dot Count (MKDC)**
In the marker list, the number of dots at that fret, 1 or 2, on both the face and the bass edge.

## Neck

**Neck Angle (NKA)**
The angle between the plane of the fretboard's playing surface and the plane of the top plate, taken as flat (ignoring any arching).

**Overstand (OVS)**
The height of the fretboard's underside above the top plate, at the body joint.

**Neck Thickness at First Fret (NKTF)**
At the first fret, the neck's thickness from the fretboard's playing surface to the back of the neck.

**Neck Thickness at Selected Fret (NKTS)**
At the Neck Thickness Fret, the neck's thickness from the fretboard's playing surface to the back of the neck.

**Neck Thickness Fret (NKSF)**
The fret at which the Neck Thickness at Selected Fret is set.
