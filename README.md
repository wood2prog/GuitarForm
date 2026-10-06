# GuitarForm

A guitar designer built as Grasshopper components for **Rhino 8**. Each component draws one part of the guitar, such as the body plate, neck, fretboard or strings.

## Requirements

- Rhino 8 (Windows), running on its default .NET runtime. The plug-in won't load if Rhino has been switched to .NET Framework with `SetDotNetRuntime`.
- .NET SDK 8 or later, to build.

## Build

From the repository folder:

```
dotnet build GuitarForm/GuitarForm.csproj
```

The plug-in is written to:

```
GuitarForm/bin/Debug/net7.0-windows/GuitarForm.gha
```

## Load in Grasshopper

**Option A: point Grasshopper at the build folder (best while developing)**

1. In Rhino, run the command `GrasshopperDeveloperSettings`.
2. Add the full path of `GuitarForm\bin\Debug\net7.0-windows`.
3. Make sure **Memory load *.GHA assemblies using COFF byte arrays** is unchecked.
4. Restart Rhino and open Grasshopper.

After this, each rebuild is picked up the next time you start Rhino.

**Option B: copy the plug-in**

1. Copy `GuitarForm.gha` into `%APPDATA%\Grasshopper\Libraries`.
2. If Windows marks the file as blocked, right-click it, open **Properties** and tick **Unblock**.
3. Restart Rhino and open Grasshopper.

Grasshopper locks the `.gha` while Rhino is running, so close Rhino before you rebuild.

## Debugging

Open `GuitarForm.sln` in Visual Studio and press **F5**. The **Rhino 8** launch profile (`GuitarForm/Properties/launchSettings.json`) builds the plug-in, then starts Rhino 8 with Grasshopper open and the debugger attached. It loads the plug-in from the build folder, so set up **Option A** above first. If Rhino is installed somewhere other than `C:\Program Files\Rhino 8`, change `executablePath` in the launch profile.

## Using the components

The components are on the **GuitarForm** tab of the Grasshopper toolbar.

The guitar is drawn **vertically**: it starts at the origin and the body length runs up the **+Y** axis. All lengths are in the Rhino document's units.

Construction geometry (construction lines, heel width marks and outline radii) is previewed **light grey**, so the outline stands out. It bakes light grey with the Continuous (solid) linetype. The **final outline** (Outline Lines and Outline Arcs) uses the default Grasshopper preview colour, and bakes with the default attributes so it takes its layer's colour.

### Plate (GuitarForm › Body)

| | Name | Type | Description |
|---|---|---|---|
| Input | Body Length (`L`) | Number | Overall length of the guitar body. Must be greater than 0. |
| Input | Upper Bout Width (`UbW`) | Number | *Optional.* Width of the upper bout. Must be greater than 0. |
| Input | Upper Bout Offset (`UbO`) | Number | *Optional.* Offset of the upper bout line down Y from the upper bout primary radius centre. A warning appears if the line falls outside 0–L. |
| Input | Upper Bout Primary Radius (`UbR`) | Number | *Optional.* Primary radius of the upper bout. Must be greater than 0. A warning appears if it's more than half of UbW. |
| Input | Upper Bout Secondary Offset (`UbSO`) | Number | *Optional.* Moves the upper bout secondary radius centre toward the body centre and grows its radius by the same amount. 0 = same as the primary radius. Can't be negative. |
| Input | Waist Radius (`WR`) | Number | *Optional.* Radius of the waist curve. Must be greater than 0. |
| Input | Waist Width (`WW`) | Number | *Optional.* Width of the body at the waist. Must be greater than 0. |
| Input | Waist Offset (`WO`) | Number | *Optional.* Distance of the waist center line up Y from the tail end (the origin). A warning appears if it's outside 0–L. |
| Input | Lower Bout Width (`LbW`) | Number | *Optional.* Width of the lower bout. Must be greater than 0. |
| Input | Lower Bout Offset (`LbO`) | Number | *Optional.* Offset of the lower bout center line up Y from the lower bout primary radius centre. A warning appears if the line falls outside 0–L. Can't be negative. |
| Input | Lower Bout Primary Radius (`LbR`) | Number | *Optional.* Primary radius of the lower bout. Must be greater than 0. A warning appears if it's more than half of LbW. |
| Input | Lower Bout Secondary Offset (`LbSO`) | Number | *Optional.* Same as UbSO, for the lower bout. |
| Input | Heel Width (`HW`) | Number | *Optional.* Width of the flat at the top of the body where the heel of the neck attaches. Must be greater than 0, and no more than the distance between the upper bout circle centres. |
| Output | Construction Lines (`CL`) | Lines (list) | Construction lines for the plate (see below). |
| Output | Outline Radii (`OR`) | Circles (list) | Circles for the body outline radii (see below). |
| Output | Outline Arcs (`OA`) | Arcs (list) | Final outline arcs: the shoulder arcs, the bout and waist circle segments, then the tail arc (see below). |
| Output | Outline Lines (`OL`) | Lines (list) | Straight parts of the final outline: the heel flat, the waist tangent lines, then the straight tail (see below). |
| Output | Outline (`O`) | Curve | The whole body outline as one closed curve: Outline Arcs and Outline Lines joined. It's empty until every input needed for a complete outline is connected. It isn't previewed or baked separately, because it's the same curves as Outline Arcs and Outline Lines. |

The construction lines are output in this order:

1. **Centerline:** from (0, 0, 0) to (0, L, 0).
2. **Upper bout:** from (−UbW/2, L − UbR − UbO, 0) to (UbW/2, L − UbR − UbO, 0), a horizontal line centred on the Y axis. It's drawn only when Upper Bout Width, Upper Bout Offset and Upper Bout Primary Radius are all connected.
3. **Waist center line:** from (−WW/2, WO, 0) to (WW/2, WO, 0), a horizontal line centred on the Y axis. It's drawn only when both Waist Width and Waist Offset are connected.
4. **Lower bout center line:** from (−LbW/2, LbR + LbO, 0) to (LbW/2, LbR + LbO, 0), a horizontal line centred on the Y axis. It's drawn only when Lower Bout Width, Lower Bout Offset and Lower Bout Primary Radius are all connected.
5. **Heel width marks:** two vertical lines L/100 long (1% of the body length) at x = ±HW/2, centred on y = L. Right mark first, then left.

The heel flat and marks are drawn when Heel Width is connected. The component fails with an error and outputs nothing if the upper bout circle centres are closer together than the heel width. It also fails if Upper Bout Offset is negative, which would put the circles above the top of the body, or if the shoulder arc would meet the circle below its centre (see below).

**Heel flat** (on Outline Lines): a horizontal line at the top of the body (y = L), from −HW/2 to HW/2. It's lengthened only when Upper Bout Offset is 0, which means the upper bout circle tops are level with the top of the body. It then runs from −(UbW/2 − UbR) to (UbW/2 − UbR), directly above the two upper bout circle centres.

When Upper Bout Offset is more than 0, the upper bout circle tops sit below the top of the body. The heel flat then stays HW long, and a **shoulder arc** joins each end of the flat to its upper bout circle. The arcs are output on **Outline Arcs**, right side then left side:

- The arc starts at (±HW/2, L), heading straight outward, so it joins the flat smoothly (G1). Its centre is directly below that point.
- It touches the upper bout circle from outside, wrapping around it.
- Its radius is (dx² + t² − UbR²) / (2·UbO), where dx = UbW/2 − UbR − HW/2 and t = UbR + UbO.
- The component fails with an error and outputs nothing if dx is less than UbO. The arc would then meet the circle below its centre and swing wider than the upper bout width.

The outline radii are drawn as full circles, a mirrored pair for each region. They're output in this order, right side (+X) then left side (−X) for each pair:

1. **Upper bout primary pair:** radius UbR. Each centre is on the upper bout line, one radius **inside** the width, at (±(UbW/2 − UbR), L − UbR − UbO). Each circle touches the end of the upper bout line. The pair is drawn whenever the upper bout line is drawn.
2. **Upper bout secondary pair:** radius UbR + UbSO. Each centre is on the upper bout line, UbSO closer to the body centre than the primary centre, at (±(UbW/2 − UbR − UbSO), L − UbR − UbO). Its outer edge stays locked to the primary circle's at ±UbW/2. At UbSO = 0 it's the same as the primary circle. The pair is drawn when the upper bout line is drawn and Upper Bout Secondary Offset is connected.
3. **Waist pair:** radius WR. Each centre is on the waist center line, one radius **outside** the width, at (±(WW/2 + WR), WO). Each circle touches the end of the waist line from outside the body. The pair is drawn when the waist line is drawn and Waist Radius is connected.
4. **Lower bout primary pair:** radius LbR. Each centre is on the lower bout center line, one radius **inside** the width, at (±(LbW/2 − LbR), LbR + LbO). Each circle touches the end of the lower bout line. The pair is drawn whenever the lower bout line is drawn.
5. **Lower bout secondary pair:** radius LbR + LbSO. Each centre is on the lower bout center line, LbSO closer to the body centre than the primary centre, at (±(LbW/2 − LbR − LbSO), LbR + LbO). Its outer edge stays locked to the primary circle's at ±LbW/2. The pair is drawn when the lower bout line is drawn and Lower Bout Secondary Offset is connected.

A secondary offset can't be negative. A secondary circle also can't overlap the waist circle on the same side, because the bout curve has to meet the waist curve tangentially. They may touch, but if they overlap, the component fails with an error and outputs nothing.

**Waist tangent lines** (on Outline Lines): a straight line from each bout secondary circle to the waist circle on the same side. It touches the secondary circle on its **outer** side and the waist circle on its **inner** side (the side facing the body), so the two circles sit on opposite sides of the line.

- The lines come after the heel flat, in this order: upper bout right, upper bout left, lower bout right, lower bout left.
- A bout's pair is drawn when its secondary offset and Waist Radius are both connected.
- As the secondary circle grows toward the waist circle, the line shortens. It disappears once the circles touch, and the component fails with an error once they overlap.

**Tail end:** joins the two lower bout primary circles across the end of the body (y = 0).

- **Lower Bout Offset = 0:** the circle bottoms sit on the end of the body, so the tail is a straight line from (−(LbW/2 − LbR), 0) to (LbW/2 − LbR, 0). It's the last line on **Outline Lines**.
- **Lower Bout Offset > 0:** the tail is one arc through the origin, tangent to both lower bout primary circles. It's the last arc on **Outline Arcs**, after the shoulder arcs.
  - Its centre is on the Y axis at (0, ρ), and it wraps each circle, touching it from outside.
  - Its radius is ρ = (cx² + cy² − LbR²) / (2·LbO), where cx = LbW/2 − LbR and cy = LbR + LbO.
- **Errors:** the component fails and outputs nothing if Lower Bout Offset is negative, or if cx is less than LbO. In the second case, the arc would meet the circle above its centre and swing wider than the lower bout width.

**Outline segments of the circles** (on Outline Arcs): the parts of the bout and waist circles that form the outside of the body. Following the right side from top to bottom:

| Segment | Circle | From | To |
|---|---|---|---|
| Upper bout primary | UbR | the shoulder arc's touch point, or the circle top (x = UbW/2 − UbR, y = L) when UbO = 0 | the widest point (UbW/2, upper bout line), which it shares with the secondary circle |
| Upper bout secondary | UbR + UbSO | the widest point | the start of the upper waist tangent line |
| Waist | WR, around its inner side | the end of the upper waist tangent line | the end of the lower waist tangent line |
| Lower bout secondary | LbR + LbSO | the start of the lower waist tangent line | the widest point (LbW/2, lower bout line) |
| Lower bout primary | LbR | the widest point | the tail arc's touch point, or the circle bottom (x = LbW/2 − LbR, y = 0) when LbO = 0 |

Each segment is output as a right-side arc then its left-side mirror. Segments go on Outline Arcs in the order above, after the shoulder arcs and before the tail arc. A segment is drawn only when both of its ends exist:
- the upper bout primary segment needs Heel Width
- the secondary segments need their secondary offset and Waist Radius
- the waist segment needs both secondary offsets

If the circles touch so that a waist tangent line shrinks to a point, the segments meet at that point.

Together with the heel flat, shoulder arcs, waist tangent lines and tail, these make one continuous outline.

The outline radii circles are construction geometry and are drawn light grey.

**Quick test:** open `Examples/Plate.gh`, which is already set up with the first values below. Or put a **Plate** component on the canvas and connect **Number Sliders** with these values:

| Input | Value | Result |
|---|---|---|
| L | 600 | Centerline from the origin up to y = 600. |
| UbW, UbO, UbR | 280, 5, 100 | Upper bout line 280 wide at y = 495. Circles of radius 100 centred at x = ±40. |
| WW, WO, WR | 240, 300, 60 | Waist line 240 wide at y = 300. Circles of radius 60 centred at x = ±180. |
| LbW, LbO, LbR | 380, 10, 120 | Lower bout line 380 wide at y = 130. Circles of radius 120 centred at x = ±70. |
| (tail) | with LbO = 10 | Tail arc of radius 370, centred at (0, 370), through the origin to about (±103.6, 14.8) on the lower bout primary circles. |
| HW | 56 | Heel flat from x = −28 to 28 at y = 600, with marks at x = ±28. Shoulder arcs of radius 116.9 run from (±28, 600) to about (±111.0, 565.4) on the upper bout circles. |
| UbO | change to 0 | The shoulder arcs disappear and the heel flat lengthens to run from x = −40 to 40. |
| UbSO | 30 | Upper bout secondary circles of radius 130 centred at x = ±10, with outer edges still at x = ±140. A waist tangent line runs from about (±139.2, 481.0) on each upper secondary circle to (±120.3, 306.5) on the waist circle. |
| LbSO | 40 | Lower bout secondary circles of radius 160 centred at x = ±30, with outer edges still at x = ±190. A waist tangent line runs from about (±161.7, 220.8) on each lower secondary circle to (±130.6, 265.9) on the waist circle. |
| LbSO | change to 70 | Error: the lower bout secondary circle overlaps the waist circle. With these values, the most LbSO can be is about 61.4. Just below that, the lower waist tangent lines become very short. |
| LbSO, LbO | change LbSO back to 40, and LbO to 0 | The tail arc disappears and the tail becomes a straight line from (−70, 0) to (70, 0). |

## Tests

The geometry is tested with NUnit and [Rhino.Testing](https://github.com/mcneel/Rhino.Testing), which loads the installed Rhino 8 into the test run, so Rhino 8 must be installed. From the repository folder:

```
dotnet test GuitarForm.Tests
```

The tests check the geometry classes (such as `PlateGeometry`) against the Quick Test values above, the error cases, and that the outline forms one closed loop. If Rhino is installed somewhere other than `C:\Program Files\Rhino 8`, change `RhinoSystemDirectory` in `GuitarForm.Tests/Rhino.Testing.Configs.xml`.

## Troubleshooting

- **Component doesn't appear:** check that the `.gha` is in the folder you added (or in `Libraries`) and that you restarted Rhino. In Grasshopper, open **File › Special Folders › Components Folder** to confirm the location.
- **Build fails because the file is in use:** close Rhino, then rebuild.
- **No preview:** make sure the component isn't hidden or disabled, and that **L** has a value greater than 0.
