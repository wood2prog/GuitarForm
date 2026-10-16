# Glossary

The name and abbreviation of every design setting. The plug-in uses these everywhere it refers to a setting: field labels, messages, the docs and the setting search.

- Each abbreviation is 2–4 capital letters and unique across the whole plug-in.
- Words that recur are always abbreviated the same way:
  - A part the setting is about has a subject code: Back Plate is BK, Body is BD, Bridge is BR, Bridge Patch is BP, Cutaway is CT, Finger Brace is FN, Fretboard is FB, Fretwire is FW, Graft is GR, Headblock is HB, Heel is HL, Heel Cap is HCP, Lower Bout is LB, Neck is NK, Nut is NT, Saddle is SDL, Soundhole is SH, Soundhole Brace is SHB, String (one string of a set) is ST, Tailblock is TB, Tone Bar is TN, Top Plate is TP, Upper Bout is UB, Upper Transverse Bar is UT, Waist is WS, X Brace is XB (e.g. NTW, FBTN, STD).
  - A position the setting is measured at has a one-letter suffix: at Nut is N, at End is E, at Bridge is B, at Neck End is N, at Tail End is T (e.g. FBRN, FBTE, SSB, BDDT). Nut and neck end never apply to the same part, so they share N.
- A setting that appears in more than one place (e.g. a neck profile radius at the first fret and at the heel) gets its own name and abbreviation for each place.
- *(planned)* marks a setting that isn't in the model yet. *(calculated)* marks a value worked out from other settings, which can't be typed in.

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
The fretboard's width at the nut. It must be more than the Bass and Treble String Setbacks together. With a string set chosen, it must also be at least the setbacks plus the span between the outer strings' centerlines with every string touching.

**String Spacing at Bridge (SSB)**
The distance between the centerlines of the first and last strings at the saddle. With the Nut Width, it sets the fretboard's taper.

**Fretboard Relief (FBRL)**
The slight forward bow built into the fretboard's playing surface along its length: the gap between the surface and a straight line from the nut to the end of the fretboard. It's measured at the Fretboard Relief Fret when one is given, and is otherwise the greatest gap.

**Fretboard Relief Fret (FBRF)**
Optional. The fret at which the Fretboard Relief is measured.

**Nut Thickness (NTT)**
The nut's thickness along the length of the neck, from its fretboard face to its headstock face.

**Nut Height (NTH)** *(calculated)*
The nut's total height, from its base to its top: the Fretboard Thickness at Nut plus the Nut Reveal.

**Nut Reveal (NTR)**
The height of the nut's top above the fretboard's playing surface, measured at the centerline.

**Bass String Setback (BSS)**
At the nut, the distance from the bass edge of the fretboard to the centerline of the outermost bass string.

**Treble String Setback (TSS)**
At the nut, the distance from the treble edge of the fretboard to the centerline of the outermost treble string.

**Strings per Course (SPC)**
The number of strings in each course, 1 or 2, the same for every course: 1 for a standard six-string, 2 for a twelve-string.

**Course String Spacing at Nut (CSN)**
For courses of two strings, the distance between the centerlines of the two strings in a course at the nut. Required when Strings per Course is 2.

**Course String Spacing at Bridge (CSB)**
For courses of two strings, the distance between the centerlines of the two strings in a course at the saddle. Required when Strings per Course is 2.

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
The string's outside diameter (its gauge). Required.

**String Construction (STC)**
Whether the string is plain or wound.

**String Unit Weight (STUW)**
The string's mass per unit length, as published by the manufacturer. Used to calculate string tension. Required. Stored in kg/m.

**String Hex Core (STHC)**
For a wound string, whether its core is hexagonal (true) or round (false). Ignored for a plain string.

**String Core Material (STCM)**
For a wound string, what its core is made of: not specified, solid nylon, nylon floss, steel or gut. Ignored for a plain string.

**String Core Modulus (STCE)**
The core's modulus of elasticity (MOE), in GPa. Given only when the core material is not specified. Required for a wound string then.

**String Core Strength (STCS)**
The core's ultimate tensile strength (UTS), in MPa. Given only when the core material is not specified. Required for a wound string then.

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
In the marker list, the fret a marker sits at. Each fret can be listed only once.

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

**Profile Includes Fretboard at First Fret (PIFF)**
At the first fret, whether the shoulder line starts at the top of the fretboard edge (true) or at its underside (false).

**Profile Uses First Fret at Selected Fret (PUFS)**
Whether the profile at the Neck Thickness Fret uses the first fret's profile settings instead of its own.

**Profile Includes Fretboard at Selected Fret (PIFS)**
At the Neck Thickness Fret, whether the shoulder line starts at the top of the fretboard edge (true) or at its underside (false).

**Heel Shaft Radius (HLSR)**
The radius of the curve that joins the front of the heel to the back of the neck shaft, tangent to both. Its center can't be past the Neck Thickness Fret, and it can't touch the front of the heel past the heel section, between the heel section and the heel end.

**Heel Shaft Offset (HLSO)**
How the front of the heel is shaped between the heel end and the neck. At 0 it's a straight line through the front of the heel end and the front of the heel section. Above 0 it's an arc through those two points and a third: the point where that straight line meets the back of the neck, moved away from the body along the back of the neck by this offset. The Heel Shaft Radius is tangent to the front of the heel either way.

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
Whether the heel end profile uses the first fret's profile settings instead of its own.

**Profile Side Length at Heel End (PSLE)**
At the heel end, the length of the heel's straight sides, running out from the heel/body joint before the profile's curves start. Zero or more (at 0 the curves start at the joint), and less than the Heel End Thickness.

**Heel Section Thickness (HLCT)**
At the heel section, the distance from the heel/body joint to the front of the heel's profile.

**Heel Section Offset from Heel End (HLCO)**
The distance from the heel end to the heel section, toward the neck.

**Heel Outline Straight Sides (HLOS)**
Seen from the heel end, whether the heel's sides run straight from the neck's edge at the fretboard to the heel end. When checked, the Heel Section Width is calculated from that line instead of being typed in.

**Heel Section Width (HLCW)**
The heel's width at the heel section. Calculated when Heel Outline Straight Sides is checked.

**Profile Uses First Fret at Heel Section (PUFH)**
Whether the heel section profile uses the first fret's profile settings instead of its own.

**Profile Side Length at Heel Section (PSLH)**
At the heel section, the length of the heel's straight sides, running out from the heel/body joint before the profile's curves start. Zero or more (at 0 the curves start at the joint), and less than the Heel Section Thickness.

**Profile Advanced Mode (PADV)**
Whether the neck profiles are shaped in advanced mode (true) or basic mode (false, the default). One setting for all four profiles. In basic mode the squareness sets each profile's shape and any blend is ignored; in advanced mode the blend sets it and any squareness is ignored. The asymmetry applies in both modes.

**Profile Squareness at First Fret (PSQF)**
At the first fret, the squareness of the profile's back (the superellipse exponent) in basic mode: 1 is a V, 1.5 a soft V, 2 a round C, 3.2 a D, and larger values squarer. A number from 1 to 10; 2 when not set. Ignored in advanced mode.

**Profile Asymmetry at First Fret (PASF)**
At the first fret, how far the profile's apex (its lowest point) is moved off the neck's centerline, as a percentage of half the profile's width there. Positive moves it toward the treble side, negative toward the bass side. A number from -50 to 50 (%); 0 when not set. The profile's width and depth don't change. Applies in both modes.

**Profile Blend at First Fret (PBLF)**
At the first fret, in advanced mode, the profile's shape as a position along the reference shapes: 0% is a V (squareness 1), 50% a round C (squareness 2) and 100% a D (squareness 3.2). Between two references the shape is mixed point for point. A number from 0 to 100 (%); 50 when not set. Ignored in basic mode.

**Profile Squareness at Selected Fret (PSQS)**
At the Neck Thickness Fret, the squareness of the profile's back (the superellipse exponent) in basic mode: 1 is a V, 1.5 a soft V, 2 a round C, 3.2 a D, and larger values squarer. A number from 1 to 10; 2 when not set. Ignored in advanced mode.

**Profile Asymmetry at Selected Fret (PASS)**
At the Neck Thickness Fret, how far the profile's apex (its lowest point) is moved off the neck's centerline, as a percentage of half the profile's width there. Positive moves it toward the treble side, negative toward the bass side. A number from -50 to 50 (%); 0 when not set. The profile's width and depth don't change. Applies in both modes.

**Profile Blend at Selected Fret (PBLS)**
At the Neck Thickness Fret, in advanced mode, the profile's shape as a position along the reference shapes: 0% is a V (squareness 1), 50% a round C (squareness 2) and 100% a D (squareness 3.2). Between two references the shape is mixed point for point. A number from 0 to 100 (%); 50 when not set. Ignored in basic mode.

**Profile Squareness at Heel Section (PSQH)**
At the heel section, the squareness of the profile's back (the superellipse exponent) in basic mode: 1 is a V, 1.5 a soft V, 2 a round C, 3.2 a D, and larger values squarer. A number from 1 to 10; 2 when not set. Ignored in advanced mode.

**Profile Asymmetry at Heel Section (PASH)**
At the heel section, how far the profile's apex (its lowest point) is moved off the neck's centerline, as a percentage of half the profile's width there. Positive moves it toward the treble side, negative toward the bass side. A number from -50 to 50 (%); 0 when not set. The profile's width and depth don't change. Applies in both modes.

**Profile Blend at Heel Section (PBLH)**
At the heel section, in advanced mode, the profile's shape as a position along the reference shapes: 0% is a V (squareness 1), 50% a round C (squareness 2) and 100% a D (squareness 3.2). Between two references the shape is mixed point for point. A number from 0 to 100 (%); 50 when not set. Ignored in basic mode.

**Profile Squareness at Heel End (PSQE)**
At the heel end, the squareness of the profile's back (the superellipse exponent) in basic mode: 1 is a V, 1.5 a soft V, 2 a round C, 3.2 a D, and larger values squarer. A number from 1 to 10; 2 when not set. Ignored in advanced mode.

**Profile Asymmetry at Heel End (PASE)**
At the heel end, how far the profile's apex (its lowest point) is moved off the neck's centerline, as a percentage of half the profile's width there. Positive moves it toward the treble side, negative toward the bass side. A number from -50 to 50 (%); 0 when not set. The profile's width and depth don't change. Applies in both modes.

**Profile Blend at Heel End (PBLE)**
At the heel end, in advanced mode, the profile's shape as a position along the reference shapes: 0% is a V (squareness 1), 50% a round C (squareness 2) and 100% a D (squareness 3.2). Between two references the shape is mixed point for point. A number from 0 to 100 (%); 50 when not set. Ignored in basic mode.

### Neck profile terms

These describe the neck profile settings above; they aren't settings themselves and have no abbreviation.

**Squareness**
The superellipse exponent n that sets how square the back of a profile is. Each half of the profile is the curve x = a cos^(2/n) t, y = b (1 - sin^(2/n) t), with half-width a and height b. 1 is a straight-sided V, 2 a half ellipse (a round C), 3.2 a D, and larger values squarer; the plug-in accepts 1 to 10. Used in basic mode.

**Asymmetry**
The offset of the apex from the neck's centerline, as a percentage of half the profile's width. The apex moves by half-width x asymmetry / 100, toward the treble side when positive. The treble half is then half-width x (1 - asymmetry / 100) wide and the bass half half-width x (1 + asymmetry / 100). Used in both modes.

**Blend**
The V-C-D position used in advanced mode: 0% is the V, 50% the round C, 100% the D. Between two neighbouring reference shapes the profile is the point-for-point mix of them, weighted by how far along it is (25% is half V and half round C; 75% is half round C and half D).

**Basic mode**
Profile Advanced Mode off. A profile is set by its squareness and asymmetry; the blend is ignored.

**Advanced mode**
Profile Advanced Mode on. A profile is set by its blend and asymmetry; the squareness is ignored.

**Starting shape (preset)**
A named set of squareness, asymmetry and blend (Modern C, Soft V, Hard V, D, Asymmetric) that fills in one profile's values when chosen. The values stay editable and nothing records which shape was chosen.

**Apex**
The lowest point of a profile, where its curve is level: the back of the neck's centerline unless the asymmetry moves it.

**Shoulder**
Where a profile's curve starts at each side, with the curve vertical there: the top or underside of the fretboard edge (per Profile Includes Fretboard) on the neck profiles, and the end of the heel's straight side (at the joint when its length is 0) on the heel profiles.

**Reference shapes (V, round C, D)**
The three squareness values the blend runs between: the V (n = 1, at 0%), the round C (n = 2, at 50%) and the D (n = 3.2, at 100%).

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
The distance from the neck end to the top of the Upper Bout Primary Radius's circle, along the centerline. The upper bout's widest point is this offset plus the primary radius from the neck end. At 0, the circle's top is level with the neck end. With a heel flat, the gap from the flat's end to the primary radius's center must be at least this offset, or the shoulder arc runs wider than the upper bout.

**Upper Bout Secondary Offset (UBSO)**
How far the center of the upper bout's secondary arc is moved from the primary arc's center toward the centerline. Its radius grows by the same amount, so its outer edge stays at the Upper Bout Width. The secondary arc carries the outline from the widest point toward the waist, and a larger offset swells that transition for a fuller waist. At 0 it's the same as the primary arc. The secondary arc can't overlap the Waist Radius; when the offset isn't set, the primary arc can't.

**Heel Flat Width (HFW)**
The width of the straight part of the outline at the neck end, where the neck's heel sits. Its ends can't reach past the centers of the Upper Bout Primary Radius, which a radius tangent to both joins them to: it can be at most the Upper Bout Width minus twice the primary radius.

**Waist Width (WSW)**
The plate's narrowest width across the waist. It can't be more than the Upper Bout Width or the Lower Bout Width.

**Waist Radius (WSR)**
The radius of the arc that forms the waist's outline at its narrowest point.

**Waist Offset from Tail End (WSO)**
The distance from the tail end to the waist's narrowest point, along the centerline. The waist must be between the bouts' widest points: farther from the tail end than the lower bout's, and closer than the upper bout's.

**Lower Bout Width (LBW)**
The plate's greatest width across the lower bout.

**Lower Bout Primary Radius (LBPR)**
The radius of the arc that forms the lower bout's outline at its widest point.

**Lower Bout Secondary Offset (LBSO)**
How far the center of the lower bout's secondary arc is moved from the primary arc's center toward the centerline. Its radius grows by the same amount, so its outer edge stays at the Lower Bout Width. The secondary arc carries the outline from the widest point toward the waist, and a larger offset swells that transition for a fuller waist. At 0 it's the same as the primary arc. The secondary arc can't overlap the Waist Radius; when the offset isn't set, the primary arc can't.

**Lower Bout Offset from Tail End (LBO)**
The distance from the tail end to the bottom of the Lower Bout Primary Radius's circle, along the centerline. The lower bout's widest point is this offset plus the primary radius from the tail end. At 0, the circle's bottom is level with the tail end. The primary radius's center must be at least this far from the centerline, or the tail arc runs wider than the lower bout.

**Cutaway Type (CTT)**
Which cutaway the body has: none, Florentine or Venetian. A Florentine cutaway is one arc from the wall to the cutaway's end point on the upper bout, which leaves a sharp point. A Venetian cutaway rounds the point: a valley arc, a point arc and a straight line between them. With a cutaway, the Cutaway Offset from Neck, Width and Depth are required, and a Venetian cutaway also needs its Valley Radius and Point Radius.

**Cutaway Offset from Neck (CTO)**
The distance from the fretboard's treble edge where the neck meets the body, square to the centerline, out to the offset line: a line parallel to the centerline that the cutaway's wall runs down. At 0 the wall carries straight on from the fretboard's edge. Above 0 the neck end leaves a step of this width between the fretboard and the cutaway. It can't be negative. The Heel Flat Width doesn't affect it. Florentine and Venetian cutaways both use it.

**Cutaway Width (CTW)**
The distance from the offset line, square to the centerline, out to a second line parallel to the centerline on the treble side. The cutaway ends where this line meets the upper bout outline above its widest point, toward the neck, so the widest point stays. Florentine and Venetian cutaways both use it.

**Cutaway Depth (CTD)**
The distance from the neck end, along the offset line, to where the cutaway's arc starts. The wall runs straight down the offset line from the neck end to that point. The arc starts there, tangent to the offset line. In a Florentine cutaway, its radius and center are calculated so it also passes through the cutaway's end point on the upper bout. In a Venetian cutaway, its radius is the Cutaway Valley Radius.

**Cutaway Valley Radius (CTVR)**
Venetian cutaway only. The radius of the valley arc. The arc stays tangent to the offset line where the straight wall ends, at the Cutaway Depth, but its radius is set here instead of being calculated to pass through the end point. This lets the cutaway undercut the end point.

**Cutaway Point Radius (CTPR)**
Venetian cutaway only. The radius of the rounded point where the cutaway meets the upper bout. The point arc is tangent to the upper bout outline at the cutaway's end point, where the Cutaway Width line meets it. A straight line tangent to both the point arc and the valley arc joins them and completes the outline.

**Top Plate Thickness (TPT)**
The top plate's thickness, the same everywhere.

**Bridge Height (BRH)**
The height of the bridge's top above the top plate, measured at the saddle on the centerline.

**Saddle Exposure (SDLE)**
The height of the saddle's top above the bridge's top, measured on the centerline.

**Top Arching Type (TPAT)**
The top plate's shape: flat, spherical or custom. A spherical top needs the Top Spherical Radius, and a custom top the Top Arching Height at Bridge and Offset from Edge.

**Top Spherical Radius (TPSR)**
For a spherical top: the radius of the sphere the top plate's surface lies on.

**Top Arching Height at Bridge (TPAB)**
For a custom top: how far the top plate rises at the bridge, above the plane through the top plate's edge.

**Top Arching Offset from Edge (TPAO)**
For a custom top: the distance from the edge of the soundhole, toward the tail, to where the arch starts. A custom arch is only in the lower bout: it rises from this line to the Top Arching Height at Bridge, then slopes down to the outline's edges. Between the soundhole and this line, the top stays flat.

**Soundhole (SH)**
Whether the top plate has a soundhole. With a soundhole, the Soundhole Diameter and Soundhole Offset from Tail End are required.

**Soundhole Diameter (SHD)**
The soundhole's diameter.

**Soundhole Offset from Centerline (SHOC)**
The distance from the centerline to the soundhole's center, square to the centerline: positive toward the treble side, negative toward the bass side. At 0 the soundhole is centered.

**Soundhole Offset from Tail End (SHOT)**
The distance from the tail end of the outline to the soundhole's center, along the centerline. It must be less than the Body Length minus the Soundhole Diameter.

**Back Plate Thickness (BKT)**
The back plate's thickness, the same everywhere.

**Back Arching Type (BKAT)**
The back plate's shape: flat, spherical or custom. A spherical back needs the Back Spherical Radius, and a custom back the Back Arching Height at Center and Offset from Edge.

**Back Spherical Radius (BKSR)**
For a spherical back: the radius of the sphere the back plate's surface lies on.

**Back Arching Height at Center (BKAC)**
For a custom back: how far the back plate rises at its center, above the plane through the back plate's edge.

**Back Arching Offset from Edge (BKAO)**
For a custom back: the distance in from the body's outline to where the arch starts. The back stays flat between the outline and this line, and the arch rises from it to the Back Arching Height at Center.

## Fretwire

**Fretwire Manufacturer (FWM)**
The company that makes the fretwire.

**Fretwire Product Code (FWPC)**
The manufacturer's name or product code for the fretwire, e.g. 47104.

**Fretwire Bead Height (FWBH)**
The height of the fret's bead (the crown), from its base, which sits on the fretboard's playing surface, to its top.

**Fretwire Bead Width (FWBW)**
The width of the fret's bead along the length of the fretboard.

**Fretwire Tang Height (FWTH)**
The depth of the fret's tang below the bead's base, the part that goes into the fret slot.

**Fretwire Tang Thickness (FWTT)**
The thickness of the fret's tang along the length of the fretboard, not counting its barbs. It must be less than the Fretwire Bead Width.

**Fretwire Barb Thickness (FWBT)**
Optional. The thickness of the fret's tang along the length of the fretboard, measured across its barbs. With the Fretwire Tang Thickness, it shows how tightly the fret grips its slot. When given, it must be more than the Fretwire Tang Thickness.

## Bracing

The top plate's braces, which sit on the underside of the top plate, and the headblock and tailblock.

They're drawn on their own copy of the plate outline, seen from inside (treble on the left), the drawing gap left of the plate. Where a brace runs toward the outline, its end follows the outline, the brace's edge offset inside it; where it meets another brace, it ends along that brace's side. The bridge patch isn't drawn until the bridge's footprint is defined.

**X Brace Offset from Neck End (XBO)**
The distance from the neck end of the body, where the heel sits, to the point where the X brace's legs cross, along the centerline. It must be less than the Body Length.

**X Brace Angle (XBA)**
The angle between each of the X brace's legs and the centerline. The legs are mirror images of each other. The bar from the bass side lower leg to the treble side upper leg runs through the crossing, and the other bar's legs end against its sides, as at the lap joint. An upper leg that reaches the upper transverse bar before the outline ends against the bar.

**X Brace Edge Offset (XBEO)**
How far each leg of the X brace stops short of the body's outline. At 0 the legs run to the outline.

**X Brace Width (XBW)**
The X brace's width across the top plate.

**X Brace Height (XBH)**
The X brace's height, from the top plate to the top of the brace.

**X Brace Profile Type (XBPT)**
The shape of the X brace's profile along its legs: scalloped or tapered. Settings for each type (e.g. scallop depth) may be added later.

**Graft (GR)**
Whether the top has a graft: a flat piece glued under the top between the headblock and the upper transverse bar, which strengthens the top where the strings' pull presses on it. With a graft, the Graft Thickness, Width and Offset from Neck End are required. If the space between the headblock and the upper transverse bar is narrower than the Graft Width, the graft is left out, with a warning.

**Graft Thickness (GRT)**
The thickness of the graft.

**Graft Width (GRW)**
The graft's width along the centerline. The graft runs across the guitar from side to side.

**Graft Offset from Neck End (GRO)**
The distance from the neck end of the body to the graft's edge nearest the neck end, along the centerline. When it fits, the graft must sit between the headblock and the upper transverse bar: this offset is at least the Headblock Depth, and this offset plus the Graft Width is no more than the Upper Transverse Bar Offset from Neck End. It must be less than the Body Length.

**Graft Edge Offset (GREO)**
How far each end of the graft stops short of the body's outline. At 0 the graft runs to the outline on both sides.

**Upper Transverse Bar Offset from Neck End (UTO)**
The distance from the neck end of the body to the upper transverse bar's edge nearest the neck end, along the centerline. It must be less than the Body Length.

**Upper Transverse Bar Width (UTW)**
The upper transverse bar's width along the centerline. The bar runs across the guitar from side to side.

**Upper Transverse Bar Height (UTH)**
The upper transverse bar's height, from the top plate to the top of the bar.

**Upper Transverse Bar Edge Offset (UTEO)**
How far each end of the upper transverse bar stops short of the body's outline. At 0 the bar runs to the outline on both sides.

**Bass Finger Brace Count (BFNC)**
The number of finger braces on the bass side, running from the outer side of the X brace's bass side lower leg toward the side of the lower bout. Above 0, the Bass Finger Brace Angle and Spacing and the Finger Brace Width and Height are required.

**Bass Finger Brace Angle (BFNA)**
The angle of the bass side finger braces from square to the X brace's bass side lower leg. At 0 they're at 90° to the leg. A positive value turns them toward the neck end, a negative value toward the tail end. It must be more than -90° and less than 90°.

**Bass Finger Brace Spacing (BFNS)**
The distance between the bass side finger braces, measured along the X brace's bass side lower leg. The first finger brace is this distance from the crossing, and each one after it is this distance from the last. Each brace is centered on its place on the leg's centerline, and the last must be before the end of the leg.

**Treble Finger Brace Count (TFNC)**
The number of finger braces on the treble side, running from the outer side of the X brace's treble side lower leg toward the side of the lower bout. Above 0, the Treble Finger Brace Angle and Spacing and the Finger Brace Width and Height are required.

**Treble Finger Brace Angle (TFNA)**
The angle of the treble side finger braces from square to the X brace's treble side lower leg. At 0 they're at 90° to the leg. A positive value turns them toward the neck end, a negative value toward the tail end. It must be more than -90° and less than 90°.

**Treble Finger Brace Spacing (TFNS)**
The distance between the treble side finger braces, measured along the X brace's treble side lower leg. The first finger brace is this distance from the crossing, and each one after it is this distance from the last. Each brace is centered on its place on the leg's centerline, and the last must be before the end of the leg.

**Finger Brace Width (FNW)**
The width of every finger brace, on both sides.

**Finger Brace Height (FNH)**
The height of every finger brace, on both sides, from the top plate to the top of the brace.

**Finger Brace Edge Offset (FNEO)**
How far each finger brace stops short of the body's outline. The braces run from the X brace's lower leg toward the outline, and at 0 they reach it.

**Tone Bar Count (TNC)**
The number of tone bars, on the treble side only, running from the inner side of the X brace's treble side lower leg, inside the X, toward the tail end. A tone bar can't cross the bass side lower leg. Above 0, the Tone Bar Angle, Spacing, Width and Height are required.

**Tone Bar Spacing (TNS)**
The distance between the tone bars, measured along the X brace's treble side lower leg. The first tone bar is this distance from the crossing, and each one after it is this distance from the last. Each bar is centered on its place on the leg's centerline, and the last must be before the end of the leg.

**Tone Bar Angle (TNA)**
The angle of the tone bars from square to the X brace's treble side lower leg. At 0 they're at 90° to the leg. A positive value turns them toward the neck end, a negative value toward the tail end. It must be more than -90° and less than 90°.

**Tone Bar Width (TNW)**
The width of every tone bar.

**Tone Bar Height (TNH)**
The height of every tone bar, from the top plate to the top of the bar.

**Tone Bar Edge Offset (TNEO)**
How far each tone bar stops short of the body's outline. The bars run from the X brace's lower leg toward the outline, and at 0 they reach it.

**Soundhole Braces (SHB)**
Whether the soundhole has its three braces: a bottom brace straight across the guitar on the tail end side of the hole, and a bass and a treble brace, each 120° around the hole from it. Their lengths follow from where they sit: each runs both ways until it meets another brace (the X brace, the upper transverse bar or another soundhole brace), or the outline when it meets none. They need a soundhole, and require the Soundhole Brace Offset, Width and Height.

**Soundhole Brace Offset (SHBO)**
The distance from the edge of the soundhole to the near side of each soundhole brace.

**Soundhole Brace Width (SHBW)**
The width of every soundhole brace.

**Soundhole Brace Height (SHBH)**
The height of every soundhole brace, from the top plate to the top of the brace.

**Bridge Patch Thickness (BPT)**
The thickness of the bridge patch: a plate glued under the top, centered under the bridge. Its ends always run to the X brace's lower legs.

**Bridge Patch Width Offset (BPWO)**
How much wider the bridge patch is than the bridge, measured along the centerline: positive makes it wider, negative narrower. At 0 it's as wide as the bridge.

**Bridge Patch Offset (BPO)**
How far the bridge patch is moved along the centerline from centered under the bridge: positive toward the neck end, negative toward the tail end.

**Headblock Width (HBW)**
The headblock's width across the guitar, square to the centerline. Its height is the Body Depth at Neck End. Its side against the neck end follows the outline.

**Headblock Depth (HBD)**
The headblock's size along the centerline, from the neck end into the body.

**Tailblock Width (TBW)**
The tailblock's width across the guitar, square to the centerline. Its height is the Body Depth at Tail End. Its side against the tail end follows the outline.

**Tailblock Depth (TBD)**
The tailblock's size along the centerline, from the tail end into the body. The Headblock Depth and Tailblock Depth together must be less than the Body Length.

## Neck Joint

**Neck Joint Type (NKJT)** *(planned)*
How the neck is joined to the body: bolt-on, mortise and tenon, or dovetail. Each type will have settings of its own.
