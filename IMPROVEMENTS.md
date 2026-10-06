# Improvements

Project-organisation work, in the order we plan to do it. Tick items off as they're done. Items marked **(decision)** need direction from the user before any code is written.

## 1. Development setup

Makes the refactors below easier to check in Rhino.

- [x] Add a `.sln` file so the project opens in Visual Studio.
- [x] Add `Properties/launchSettings.json` that starts Rhino 8 with Grasshopper, so F5 builds and attaches the debugger.
- [x] Commit a clean example definition (`Examples/Plate.gh`) wired with the README Quick Test slider values. `Test File.gh` stays ignored as a scratch file. *(The user saves the .gh from Grasshopper.)*

## 2. Shared base class for display and baking

Every component draws construction geometry light grey and the final outline in the default preview colour (see CLAUDE.md). That code is currently in `PlateComponent` (`ClippingBox`, `DrawViewportWires`, `IsBakeCapable`, `BakeGeometry`, ~60 lines) and would be copied into each new component.

- [x] Add a `GuitarFormComponent` base class that owns the construction and outline geometry lists and does the clipping box, preview and baking.
- [x] Move `PlateComponent` onto it, with no change in behaviour.
- [x] Update the "Component conventions" in CLAUDE.md to point at the base class.

## 3. Separate the Plate geometry from the component

`PlateComponent.cs` is 570 lines, and `SolveInstance` alone is about 325 of them. It mixes reading inputs, validating them, the geometry maths and setting outputs.

- [ ] Move the geometry into a plain class (e.g. `Geometry/PlateGeometry.cs`) that takes the dimensions and returns the construction lines, circles, outline arcs and outline lines, plus errors, warnings and remarks.
- [ ] The component then reads inputs, calls it, reports its messages and sets outputs.
- [ ] Add `Out*` constants for output positions, the same way inputs use `In*` constants (the code currently calls `SetDataList(0..3, …)`).
- [ ] Replace the repeated "must be greater than zero" checks (about 7) with one helper.
- [ ] Make tolerance use consistent: `AddMirroredSegment` uses a hard-coded `1e-9`, and the rest of the code uses `DocumentTolerance()`.

## 4. Automated tests

- [ ] Add a test project (xUnit) for the geometry class.
- [ ] Turn the README Quick Test values into tests: tail arc radius 370, shoulder arc radius 116.9, tangent line end points, the largest LbSO (about 61.4), and the error cases.
- [ ] Check which RhinoCommon types work without Rhino running; use [Rhino.Testing](https://github.com/mcneel/Rhino.Testing) if native code is needed.
- [ ] Update CLAUDE.md and the README with how to run the tests.

## 5. Plate decisions and polish

- [ ] **(decision)** `HeelMarkLength = 0.25` is in document units: visible in inches, tiny in millimetres. Keep it, scale it to the body length, or make it an input?
- [ ] **(decision)** Also output the whole body outline as one closed, joined curve (useful for offsetting, extruding or kerfing downstream)?
- [ ] **(decision)** Plug-in metadata in `GuitarFormInfo.cs`: author name, contact and a plug-in icon (it's currently `null`).

## 6. How components share data (before the second component)

- [ ] **(decision)** Will downstream components (neck, fretboard, strings) take plain numbers wired by hand (body length, heel width, …), or will Plate output a "Body" object that they read? This shapes every later component's inputs and outputs.

## 7. Documentation layout (when the second component is added)

- [ ] Move per-component detail out of the README into `docs/Plate.md`, `docs/Neck.md`, etc. Keep the README for requirements, build, loading and an overview.
- [ ] Fold `Spec.txt` into the README or `docs/`.

## 8. Later

- [ ] GitHub Actions workflow that builds the plug-in on each push.
- [ ] Yak package for distribution through Rhino's Package Manager.
