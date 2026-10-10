# Glossary

The name and abbreviation of every design setting. The plug-in uses these everywhere it refers to a setting: field labels, messages, the docs and the setting search.

- Each abbreviation is 2–4 capital letters and unique across the whole plug-in.
- Words that recur are always abbreviated the same way:
  - A part the setting is about has a subject code: Body is BD, Fretboard is FB, Heel is HL, Heel Cap is HCP, Neck is NK, Nut is NT, String (one string of a set) is ST (e.g. NTW, FBTN, STD).
  - A position the setting is measured at has a one-letter suffix: at Nut is N, at End is E, at Bridge is B, at Neck End is N, at Tail End is T (e.g. FBRN, FBTE, SSB, BDDT). Nut and neck end never apply to the same part, so they share N.
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

**Profile Bottom Radius at First Fret (PBRF)**
At the first fret, the radius of the arc that forms the back of the neck.

**Profile Shoulder Angle at First Fret (PSAF)**
At the first fret, the angle of the straight shoulder line that runs down from the fretboard edge, measured from a flat line straight across the neck's width (perpendicular to its center plane), not from the radiused fretboard surface.

**Profile Midshoulder Radius at First Fret (PMRF)**
At the first fret, the radius of the arc that blends the shoulder line into the bottom arc, tangent to both.

**Profile Center Offset at First Fret (PCOF)**
At the first fret, how far the bottom arc is shifted from the neck's centerline: positive toward the treble side, negative toward the bass side.

**Profile Includes Fretboard at First Fret (PIFF)**
At the first fret, whether the shoulder line starts at the top of the fretboard edge (true) or at its underside (false).

**Profile Uses First Fret at Selected Fret (PUFS)**
Whether the profile at the Neck Thickness Fret uses all five of the first fret's profile settings instead of its own.

**Profile Bottom Radius at Selected Fret (PBRS)**
At the Neck Thickness Fret, the radius of the arc that forms the back of the neck.

**Profile Shoulder Angle at Selected Fret (PSAS)**
At the Neck Thickness Fret, the angle of the straight shoulder line that runs down from the fretboard edge, measured from a flat line straight across the neck's width (perpendicular to its center plane), not from the radiused fretboard surface.

**Profile Midshoulder Radius at Selected Fret (PMRS)**
At the Neck Thickness Fret, the radius of the arc that blends the shoulder line into the bottom arc, tangent to both.

**Profile Center Offset at Selected Fret (PCOS)**
At the Neck Thickness Fret, how far the bottom arc is shifted from the neck's centerline: positive toward the treble side, negative toward the bass side.

**Profile Includes Fretboard at Selected Fret (PIFS)**
At the Neck Thickness Fret, whether the shoulder line starts at the top of the fretboard edge (true) or at its underside (false).

**Heel Shaft Radius (HLSR)**
The radius of the curve that runs from the back of the neck shaft down into the heel, tangent to the back of the neck.

**Heel Shaft Offset (HLSO)**
How far the center of the Heel Shaft Radius is moved toward the peghead, measured parallel to the neck shaft so the curve stays tangent to the back of the neck. At 0, a straight line tangent to the curve joins it to the heel end. Above 0, a tangent arc joins it instead. The center can't move past the Neck Thickness Fret.

**Heel End Thickness (HLET)**
At the heel end, the distance from the heel/body joint to the front of the heel's profile.

**Heel End Offset from Back (HLEO)**
The distance from the back plate to the outer end of the heel: the heel cap's outer face when there is a heel cap, otherwise the heel end. At 0 it's flush with the back; a positive value moves it toward the top plate.

**Heel End Width (HLEW)**
The heel's width at the heel end, measured at the heel/body joint.

**Heel Cap (HCP)**
Whether a heel cap, a thin piece of wood glued onto the heel end, is included. It doesn't change the heel end: every heel end setting still refers to the end of the heel itself.

**Heel Cap Thickness (HCPT)**
The heel cap's thickness. The cap's outer face stays at the Heel End Offset from Back, and the heel end moves toward the fretboard by this thickness.

**Profile Uses First Fret at Heel End (PUFE)**
Whether the heel end profile uses the first fret's bottom radius, shoulder angle and midshoulder radius instead of its own.

**Profile Bottom Radius at Heel End (PBRE)**
At the heel end, the radius of the arc that forms the front of the heel's profile.

**Profile Shoulder Angle at Heel End (PSAE)**
At the heel end, the angle of the straight shoulder line that runs from the end of the heel's straight sides, measured from a flat line straight across the heel's width.

**Profile Midshoulder Radius at Heel End (PMRE)**
At the heel end, the radius of the arc that blends the shoulder line into the bottom arc, tangent to both.

**Profile Side Length at Heel End (PSLE)**
At the heel end, the length of the heel's straight sides, running out from the heel/body joint before the profile's curves start.

**Heel Section Thickness (HLCT)**
At the heel section, the distance from the heel/body joint to the front of the heel's profile.

**Heel Section Offset from Heel End (HLCO)**
The distance from the heel end to the heel section, toward the neck.

**Heel Outline Straight Sides (HLOS)**
Seen from the heel end, whether the heel's sides run straight from the neck's edge at the fretboard to the heel end. When checked, the Heel Section Width is calculated from that line instead of being typed in.

**Heel Section Width (HLCW)**
The heel's width at the heel section. Calculated when Heel Outline Straight Sides is checked.

**Profile Uses First Fret at Heel Section (PUFH)**
Whether the heel section profile uses the first fret's bottom radius, shoulder angle and midshoulder radius instead of its own.

**Profile Bottom Radius at Heel Section (PBRH)**
At the heel section, the radius of the arc that forms the front of the heel's profile.

**Profile Shoulder Angle at Heel Section (PSAH)**
At the heel section, the angle of the straight shoulder line that runs from the end of the heel's straight sides, measured from a flat line straight across the heel's width.

**Profile Midshoulder Radius at Heel Section (PMRH)**
At the heel section, the radius of the arc that blends the shoulder line into the bottom arc, tangent to both.

**Profile Side Length at Heel Section (PSLH)**
At the heel section, the length of the heel's straight sides, running out from the heel/body joint before the profile's curves start.

## Body

**Body Length (BDL)**
The body's overall length, from the tail end to the neck end, along the centerline.

**Body Depth at Neck End (BDDN)**
The body's depth at the neck end, from the top plate to the back plate.

**Body Depth at Tail End (BDDT)**
The body's depth at the tail end, from the top plate to the back plate.

**Body Profile Skew (BDPS)**
In the side view, how far the neck end is shifted sideways relative to the tail end, slanting the body into a parallelogram. The tail end and neck end stay square to the centerline. At 0 the top runs parallel to the centerline and the back takes whatever slant the two depths give. A positive value shifts the neck end toward the top's side, a negative value toward the back's side, and the back follows it.

**Upper Bout Width (UBW)**
The plate's greatest width across the upper bout.

**Upper Bout Primary Radius (UBPR)**
The radius of the arc that forms the upper bout's outline at its widest point.

**Upper Bout Offset from Neck End (UBO)**
The distance from the neck end to the top of the Upper Bout Primary Radius's circle, along the centerline. The upper bout's widest point is this offset plus the primary radius from the neck end. At 0, the circle's top is level with the neck end.

**Upper Bout Secondary Offset (UBSO)**
How far the center of the upper bout's secondary arc is moved from the primary arc's center toward the centerline. Its radius grows by the same amount, so its outer edge stays at the Upper Bout Width. The secondary arc carries the outline from the widest point toward the waist, and a larger offset swells that transition for a fuller waist. At 0 it's the same as the primary arc.

**Heel Flat Width (HFW)**
The width of the straight part of the outline at the neck end, where the neck's heel sits.

**Waist Width (WSW)**
The plate's narrowest width across the waist.

**Waist Radius (WSR)**
The radius of the arc that forms the waist's outline at its narrowest point.

**Waist Offset from Tail End (WSO)**
The distance from the tail end to the waist's narrowest point, along the centerline.

**Lower Bout Width (LBW)**
The plate's greatest width across the lower bout.

**Lower Bout Primary Radius (LBPR)**
The radius of the arc that forms the lower bout's outline at its widest point.

**Lower Bout Secondary Offset (LBSO)**
How far the center of the lower bout's secondary arc is moved from the primary arc's center toward the centerline. Its radius grows by the same amount, so its outer edge stays at the Lower Bout Width. The secondary arc carries the outline from the widest point toward the waist, and a larger offset swells that transition for a fuller waist. At 0 it's the same as the primary arc.

**Lower Bout Offset from Tail End (LBO)**
The distance from the tail end to the bottom of the Lower Bout Primary Radius's circle, along the centerline. The lower bout's widest point is this offset plus the primary radius from the tail end. At 0, the circle's bottom is level with the tail end.

**Cutaway Offset from Neck (CTO)**
The distance from the fretboard's treble edge where the neck meets the body, square to the centerline, out to the offset line: a line parallel to the centerline that the cutaway's wall runs down. At 0 the wall carries straight on from the fretboard's edge. Above 0 the neck end leaves a step of this width between the fretboard and the cutaway. It can't be negative. The Heel Flat Width doesn't affect it. Florentine and Venetian cutaways both use it.

**Cutaway Width (CTW)**
The distance from the offset line, square to the centerline, out to a second line parallel to the centerline on the treble side. The cutaway ends where this line meets the upper bout outline. Florentine and Venetian cutaways both use it.

**Cutaway Depth (CTD)**
The distance from the neck end, along the offset line, to where the cutaway's arc starts. The wall runs straight down the offset line from the neck end to that point. The arc starts there tangent to the offset line and runs to the cutaway's end point on the upper bout, and its radius and center are calculated to do both. Florentine and Venetian cutaways both use it.
