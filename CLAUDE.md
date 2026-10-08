# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

GuitarForm is a guitar designer implemented as a set of Grasshopper (Rhino) components written in C#. Each component models one part of the guitar: body shape, strings, fretboard, neck, and so on.

## Working agreement

- The user gives the design direction for each component's C# code. Implement what they describe, and ask instead of guessing at guitar geometry or component inputs and outputs that haven't been specified.
- `IMPROVEMENTS.md` is the ordered checklist of project-organisation work. Tick items off as they're done.

## Build

```
dotnet build GuitarForm/GuitarForm.csproj
```

- This is a Rhino 8–only project. It uses the `Grasshopper` NuGet package (8.0.x, `ExcludeAssets="runtime"`), targets only `net7.0-windows` (Rhino 8's default .NET runtime), and outputs `bin/Debug/net7.0-windows/GuitarForm.gha`.
- To load it in Grasshopper, run `GrasshopperDeveloperSettings` in Rhino and add the `bin/Debug/net7.0-windows` folder, or copy the `.gha` into `%APPDATA%\Grasshopper\Libraries`.
- Tests: `dotnet test GuitarForm.Tests`. They cover the geometry classes in `GuitarForm/Geometry/` (NUnit plus Rhino.Testing, which loads the installed Rhino 8; the test project targets `net8.0-windows` to match Rhino 8's current runtime). Add tests when geometry changes, and keep the Quick Test values in `docs/` and the tests in agreement. Component wiring, preview and baking still need checking in Grasshopper.
- Test classes that use RhinoCommon types need `[RhinoTestFixture]`. Don't capture RhinoCommon structs (e.g. `Point3d`) in lambdas in test code: the compiler-generated closure class makes NUnit load RhinoCommon before Rhino is set up, and no tests are found. The test project references the plug-in project and copies the `.gha` in as `GuitarForm.dll`; don't compile the geometry sources into it, for the same reason.

## Component conventions

- The guitar is drawn vertically: the origin is the starting point and the body length runs along +Y. Lengths are in the Rhino document's units, with no conversion.
- `SolveInstance` reads inputs by position through the `In*` constants above `RegisterInputParams`. The user sometimes reorders inputs by hand, so whenever the registration order changes, update those constants and the input table in the component's `docs/` page to match.
- Component icons are 24x24 PNGs in `GuitarForm/Resources/Icons/`, embedded automatically by the `.csproj` and loaded with `Icons.Load("Name.png")`. Keep the full-size artwork they came from in `GuitarForm/Resources/Source/`. When shrinking line art to 24x24, thicken or boost the line first, or it fades to almost nothing.
- The project logo is `Resources/Source/GuitarForm.png`. It's scaled to `Icons/GuitarForm.png` (24x24, the plug-in icon in `GuitarFormInfo`) and `Icons/GuitarFormTab.png` (16x16, the toolbar tab icon, set in `GuitarFormPriority`).
- Components live in `GuitarForm/Components/`, use category `GuitarForm`, and each needs its own permanent `ComponentGuid`. Never change an existing GUID, because saved .gh files reference it.
- Each component works on its own, with its own plain inputs (numbers, points, planes, curves). A value several components need, such as heel width, comes from one slider wired to each of them, or from an upstream component's output (e.g. a point or plane the next part attaches to). The exception is values for the whole instrument (scale length, neck join fret): the Instrument component bundles them into an `Instrument` record (`Geometry/Instrument.cs`), passed along wires as `GH_Instrument` through an `InstrumentParameter` input or output (`GuitarForm/Types/`). Add a value to `Instrument` only when the user says it belongs to the whole instrument; everything else stays a plain input.
- Components derive from `GuitarFormComponent` (which sets the `GuitarForm` category), not `GH_Component` directly.
- Each component is documented in `docs/` (inputs, outputs, geometry and a Quick Test); components in the same toolbar group share a page, e.g. `docs/Soundhole.md`. When adding a component, add its page and a row to the components table in the README. The README itself stays an overview: requirements, build, loading and shared conventions.
- Keep the geometry maths out of the component, in a plain class in `GuitarForm/Geometry/` that doesn't depend on Grasshopper (see `PlateGeometry`). It takes the dimensions as a record, with optional inputs as nullable values, and reports problems as `GeometryMessage`s. The component reads its inputs (`GetOptionalNumber` for optional ones), calls the geometry class, passes its messages to `AddMessages`, and outputs nothing if it failed.
- Set outputs by position through `Out*` constants, and keep them matching `RegisterOutputParams` (and the output table in `docs/`) the same way.
- Geometry is output as plain geometry, so its display style isn't part of it. `GuitarFormComponent` handles the preview, clipping box and baking. In `SolveInstance`, pass curves to `AddConstruction` or `AddOutline` as well as setting the outputs. Construction geometry (construction lines, outline radii circles) is drawn and baked light grey (`ConstructionColor`) with a solid linetype, so the outline stands out. Final outline geometry (Outline Lines and Outline Arcs) uses the default Grasshopper preview colour (`args.WireColour`) and bakes with the default attributes.
