# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

GuitarForm is a guitar designer: a Rhino 8 plug-in written in C#. A dockable panel has a tab for each section of the guitar (instrument, body, soundhole, and later neck, fretboard, strings and so on). Designs and per-section presets are stored in a SQLite library, previewed live in Rhino, and built into the document on request.

## Working agreement

- The user gives the design direction for each section. Implement what they describe, and ask instead of guessing at guitar geometry, or at a section's fields, that haven't been specified.
- `IMPROVEMENTS.md` is the ordered checklist of project-organisation work. Tick items off as they're done.
- `variables and settings.txt` is the user's working notes on every section's variables. It's untracked: don't commit it. It will be worked into the plan once the user is ready.

## Build

```
dotnet build GuitarForm/GuitarForm.csproj
```

- This is a Rhino 8–only project. It uses the `RhinoCommon` NuGet package (8.0.x, `ExcludeAssets="runtime"`), targets `net7.0-windows` (Rhino 8 runs it on its current .NET runtime), and outputs `GuitarForm/bin/Debug/net7.0-windows/GuitarForm.rhp`.
- It builds for `win-x64` only, so SQLite's native `e_sqlite3.dll` lands next to the `.rhp`: Rhino doesn't search `runtimes/<platform>/native/` for a plug-in.
- The launch profile (`GuitarForm/Properties/launchSettings.json`) starts Rhino 8 with the build folder in `RHINO_PACKAGE_DIRS`, so the plug-in loads without being installed, and runs the `GuitarForm` command, which opens the panel. Start Rhino for testing from PowerShell, not Git Bash, which rewrites `/`-arguments into paths.
- Tests: `dotnet test GuitarForm.Tests`. They cover the model, the library and the geometry (NUnit plus Rhino.Testing, which loads the installed Rhino 8; the test project targets `net8.0-windows` to match Rhino 8's current runtime). Add tests when the model, library or geometry changes, and keep the Quick Test values in `docs/` and the tests in agreement. The panel, preview and Build still need checking in Rhino.
- Test classes that use RhinoCommon types need `[RhinoTestFixture]`. Don't capture RhinoCommon structs (e.g. `Point3d`) in lambdas in test code: the compiler-generated closure class makes NUnit load RhinoCommon before Rhino is set up, and no tests are found. The test project references the model, geometry and data projects; don't compile the geometry sources into it, for the same reason.

## Projects

- `GuitarForm.Model`: the design model. `GuitarDesign` holds one record per section (`Instrument`, `Body`, `Soundhole`). Plain records, no Rhino dependency, lengths in mm, optional values nullable. `DesignDocument` is the JSON export and import: change `DesignDocument.SchemaVersion` whenever the JSON's shape changes, and keep reading older versions.
- `GuitarForm.Geometry`: the geometry maths (RhinoCommon, no UI). Each class (e.g. `PlateGeometry`) takes a section record, reports problems as `GeometryMessage`s, and builds in mm. `DesignDrawing` draws a whole design and lays it out (the side view goes the drawing gap right of the plate's widest point), keeping construction geometry apart from the final outline and messages per section. `DesignUnits` converts between mm and document units.
- `GuitarForm.Data`: the SQLite library, no Rhino dependency. `Library` stores designs and presets; `Schema.cs` holds the tables and migrations (never edit a released migration; add a new one); `SectionTables` maps each section to its table; `Backups` backs up, prunes and restores; `GuitarFormSettings` is the plug-in's settings record.
- `GuitarForm`: the plug-in. `GuitarFormPanel` (design list, section tabs, Preview and Build), `SectionEditor` with the fields declared in `Sections.cs`, `SettingsPage`, `DesignPreview` (display conduit), `DesignBuilder`, `BackupScheduler`.

## Conventions

- The guitar is drawn vertically: the origin is the tail end of the body and the body length runs along +Y.
- Lengths are stored in mm (database, presets, JSON, geometry). Every length the user sees or types in the plug-in (fields, dialogs, settings, messages) is in the active document's units, converted with `DisplayUnits`.
- A design owns its own copy of each section. A preset is a named section in the library; loading it copies its values into the design.
- Adding a section, or a field to one, touches: its record in `GuitarForm.Model` (and `GuitarDesign`), a migration in `Schema.cs` and its columns in `SectionTables`, its geometry class and `DesignDrawing` if it draws anything, its fields in `Sections.cs` and a tab in `GuitarFormPanel`, its `docs/` page, and tests (round trips in `LibraryTests` and `DesignDocumentTests`, plus the geometry). A value goes in `Instrument` only when the user says it belongs to the whole instrument.
- Construction geometry (construction lines, outline radii circles) is previewed light grey (`DesignPreview.ConstructionColor`) and built on the light grey `GuitarForm::Construction` layer. The final outline is previewed in Rhino's feedback colour and built on `GuitarForm::Outline`. Built objects are tagged with the design's id (`DesignBuilder.DesignIdKey`), so building again replaces only that design's objects.
- Each tab is documented in `docs/` (fields, geometry and a Quick Test), with a row in the README's tabs table. The README stays an overview: requirements, build, loading, using the panel and shared conventions.
- The plug-in's GUID (`Properties/AssemblyInfo.cs`) and the panel's GUID (`GuitarFormPanel`) are permanent; never change them.
- The project logo is `GuitarForm/Resources/Source/GuitarForm.png`, scaled to `Resources/Icons/GuitarForm.png` (24x24, the panel icon). Keep full-size artwork in `Resources/Source/`. When shrinking line art to 24x24, thicken or boost the line first, or it fades to almost nothing.
