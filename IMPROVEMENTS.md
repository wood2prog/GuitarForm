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

- [x] Move the geometry into a plain class (e.g. `Geometry/PlateGeometry.cs`) that takes the dimensions and returns the construction lines, circles, outline arcs and outline lines, plus errors, warnings and remarks.
- [x] The component then reads inputs, calls it, reports its messages and sets outputs.
- [x] Add `Out*` constants for output positions, the same way inputs use `In*` constants (the code currently calls `SetDataList(0..3, …)`).
- [x] Replace the repeated "must be greater than zero" checks (about 7) with one helper.
- [x] Make tolerance use consistent: `AddMirroredSegment` uses a hard-coded `1e-9`, and the rest of the code uses `DocumentTolerance()`.

## 4. Automated tests

- [x] Add a test project for the geometry class. *(NUnit, because Rhino.Testing requires it.)*
- [x] Turn the README Quick Test values into tests: tail arc radius 370, shoulder arc radius 116.9, tangent line end points, the largest LbSO (about 61.4), and the error cases. *(This found that the README's upper waist tangent values were wrong; corrected to (±139.2, 481.0) → (±120.3, 306.5).)*
- [x] Check which RhinoCommon types work without Rhino running; use [Rhino.Testing](https://github.com/mcneel/Rhino.Testing) if native code is needed. *(None load without Rhino, so the tests use Rhino.Testing.)*
- [x] Update CLAUDE.md and the README with how to run the tests.

## 5. Plate decisions and polish

- [x] **(decision)** `HeelMarkLength = 0.25` is in document units: visible in inches, tiny in millimetres. Keep it, scale it to the body length, or make it an input? *(Scaled: 1% of the body length.)*
- [x] **(decision)** Also output the whole body outline as one closed, joined curve (useful for offsetting, extruding or kerfing downstream)? *(Yes: Outline (O) output, empty until the outline is complete.)*
- [x] **(decision)** Plug-in metadata in `GuitarFormInfo.cs`: author name and contact. *(Contact set to the user's email; name left blank.)*
- [x] Plug-in icon (`GuitarFormInfo.Icon` is `null`). *(From the user's logo; also used for the toolbar tab and the README.)*

## 6. How components share data (before the second component)

- [x] **(decision)** Will downstream components (neck, fretboard, strings) take plain numbers wired by hand (body length, heel width, …), or will Plate output a "Body" object that they read? This shapes every later component's inputs and outputs. *(Plain wiring: each component works on its own with its own inputs. Shared values come from one slider wired to each component that needs it, or from an upstream component's output. No settings component or custom data type. See CLAUDE.md.)* *(Later revised: an Instrument component outputs an Instrument object carrying whole-instrument values such as scale length; everything else is still wired plainly.)*

## 7. Documentation layout (when the second component is added)

- [x] Move per-component detail out of the README into `docs/Plate.md`, `docs/Neck.md`, etc. Keep the README for requirements, build, loading and an overview. *(`docs/Plate.md` and `docs/Soundhole.md`, which holds both soundhole components; one page per toolbar group. The README links to them from a components table.)*
- [x] Fold `Spec.txt` into the README or `docs/`. *(Its brief is in the README intro and its working agreement in CLAUDE.md; the file was deleted.)*

## Considerations for later components

Not decided yet; settle these when designing the component that needs them (probably the neck).

- [ ] **(decision)** Placing parts with planes. Plate stays fixed at the origin, since it's the starting point. A component that attaches to another part (e.g. the neck at the heel) would take an optional Plane input, defaulting to World XY, fed from an upstream output (e.g. a Heel Plane output on Plate at (0, L)). The neck would then follow the body when L changes, and the plane can carry a neck angle (tilt out of XY). Each component would still build its geometry in simple local coordinates, with the origin at its attachment point, and move the finished geometry onto the plane at the end. A plane input added to Plate later would go at the end of its inputs, as optional, so existing .gh files still work.

## 8. Move from Grasshopper to a Rhino plug-in with a design database

Replaces the Grasshopper components with a Rhino plug-in. A dockable panel has a tab for each area of the guitar, the whole design is stored in a SQLite database, and the design is drawn in Rhino live. The geometry classes in `Geometry/` carry over; the components, `GH_Instrument`, `InstrumentParameter` and `GuitarFormComponent` go.

Decided:

- **Grasshopper is dropped.** Keeping track of many wired values, and saving a copy of them, was the problem. In the plug-in every part reads shared values from the one design.
- **SQLite holds everything.** Opening a previous design rebuilds the model from the database.
- **A design owns copies of its sections.** Each area (Instrument, Body, Soundhole, String set, …) can also be saved as a named library preset. Loading a preset copies its values into the design, so editing a design never changes the library or other designs.
- **The first edit after loading a design asks: overwrite it, or save a copy and edit that.** After that, changes are written to the database as you make them.
- **Lengths are stored in millimetres** (database, presets, JSON, geometry) **and shown and typed in the Rhino document's units** everywhere in GuitarForm: the tab fields, dialogs, settings and messages. They're converted when drawn, too.
- **Live preview while editing, plus a Build command.** The preview is drawn, not added to the document. Build writes real objects to GuitarForm layers and replaces those from the previous build of that design.
- **One library database, kept locally, with backups to a synced folder.** The main copy is `%LOCALAPPDATA%\GuitarForm\GuitarForm.db` by default, which isn't synced, so OneDrive never sees a half-written file. Backups are written with SQLite's backup API to `Documents\GuitarForm\Backups` by default, which OneDrive syncs. Both locations are set on a Settings page, and kept in Rhino's plug-in settings rather than the database, since they're needed before it's opened.
- **A design can be exported** as a structured JSON document that other programs can read.

### 8a. Risks first

- [x] Plug-in shell: a `.rhp` project (RhinoCommon package, `PlugIn` class) with a `GuitarForm` command that opens an empty dockable Eto panel. Builds and loads in Rhino 8 alongside the existing `.gha`. *(`GuitarForm.Plugin/`. The panel shows the library's path and SQLite version. The launch profile loads the plug-in through `RHINO_PACKAGE_DIRS`, so it doesn't need installing.)*
- [x] Spike: `Microsoft.Data.Sqlite` loads and opens a database from inside Rhino 8. The native `e_sqlite3` library has to be found from the plug-in folder. *(Rhino doesn't search `runtimes/<platform>/native/`, so the plug-in builds for `win-x64` only, which puts `e_sqlite3.dll` next to the `.rhp`. The library file gets `PRAGMA application_id` "GtFm", and a database without it is refused.)*

### 8b. Model

- [x] **(decision)** The sections and their fields. Proposed: **Instrument** (scale length, neck join fret); **Body** (Plate's dimensions plus tail and neck depth, so Plate and Side View share one body length); **Soundhole** (round or custom). Side View's drawing offset places the drawing; it doesn't describe the guitar. Does it become a design setting, or is the side view laid out automatically? *(As proposed, with one round soundhole per design, or none. The side view is laid out automatically, so the drawing offset isn't part of the design. The tail and neck depths are optional, so the outline can be drawn before the side view.)*
- [x] **(decision)** Custom soundhole shape: how it's provided (picked from the Rhino document? imported?) and stored (e.g. the curve serialised into the database). *(Dropped for now: round soundholes only. See "Later".)*
- [x] Add a `GuitarForm.Model` project with no Rhino dependency: a `GuitarDesign` record holding a record for each section, lengths in mm, optional values nullable. `Instrument` and `GeometryMessage` move there. *(`Body` and `Soundhole` are plain values; their checks stay in the geometry classes, so they aren't written twice. The Grasshopper project references the model project until it's removed.)*
- [x] JSON export and import of a whole design, with `schemaVersion` and `"units": "mm"`. *(`DesignDocument`. Values not set are left out. Import rejects a newer schema version, other units, and a design without an instrument or body.)*

### 8c. Geometry

- [x] The geometry classes take the model's section records instead of their own `*Dimensions` records, and solve in mm. The plug-in scales the result to document units (`RhinoMath.UnitScale`) and converts the document tolerance to mm. Update the tests. *(The geometry classes moved to a `GuitarForm.Geometry` project shared by the plug-in, the components and the tests; the tests now reference it instead of copying the `.gha`. Plate takes a `Body`, Soundhole a `Soundhole`, and Side View a `Body` plus a drawing offset, which the layout will supply. Side View draws nothing, with a remark, until both depths are set. `DesignUnits` does the unit conversions. Custom Soundhole keeps its own record until custom shapes come back.)*

### 8d. Database

- [x] Schema: a `designs` table and one table per section. Each section row belongs to either a design or the library (as a named preset). Typed columns. Schema version kept in `PRAGMA user_version`, with a migration for each version. *(`GuitarForm.Data/Schema.cs`. Design names and each section's preset names are unique, ignoring case. Deleting a design deletes its sections.)*
- [x] Storage class: create, list, load, overwrite, copy and delete designs; save a section as a preset; list presets; copy a preset into a design. Tested against a temporary database. *(`Library` in the new `GuitarForm.Data` project, which has no Rhino dependency. A copy is named "OM copy", "OM copy 2", …. A preset is copied into a design by loading it and saving the design.)*
- [x] **(decision)** When backups are made and how many are kept. Proposed: when Rhino closes and every 15 minutes while there are changes since the last backup, as dated files (`GuitarForm 2026-10-08 1830.db`), keeping the last 20 plus one per day for 30 days. *(As proposed, as defaults: the interval and both numbers can be changed on the Settings page.)*
- [x] Backup: writes a complete copy of the library to the backup folder with SQLite's backup API, at the times decided above, plus a **Back up now** button. *(`Backups` in `GuitarForm.Data`: backing up, listing, `NeedsBackup` (the library changed since the newest backup) and pruning. Each backup is written under a temporary name and renamed. The timer, the Rhino-closing backup and the button are in the plug-in, with the Settings page.)*
- [x] Restore: replaces the main copy with a chosen backup, after backing up the current main copy first. Also used to start on another computer. *(`Backups.Restore`. A file that isn't a library this version can read is refused and the library left alone. Choosing the backup is on the Settings page.)*
- [x] Settings page in the panel: main library location and backup folder (defaults above). Changing the main location offers to move the existing library there or open the one already at the new place. *(The Settings tab also has the backup interval and the two numbers of backups kept, **Back up now**, **Restore…** and **Open backup folder**. The library is chosen by folder and is always `GuitarForm.db` in it. Settings are kept in Rhino's plug-in settings. `BackupScheduler` in the plug-in backs up on its timer and when Rhino closes.)*

### 8e. Editor panel

- [x] Design list: new, open, copy, rename, delete. New asks for the name, body length, scale length and neck join fret (the required values); optional values start blank, with no soundhole. *(A drop-down of designs with New…, Copy, Rename…, Delete, Import… and Export… buttons.)*
- [x] One tab per section, with number fields (blank means not set, for optional values), the section's errors and warnings, and **Load preset** / **Save as preset**. *(Fields are declared in `GuitarForm.Plugin/Sections.cs` and shown by `SectionEditor`. A blank required box or text that isn't a number is reported and doesn't change the design. Presets can also be deleted. The Soundhole tab has a box to say whether the design has one.)*
- [x] The overwrite-or-copy prompt on the first edit after loading, then saving as you edit. *(Yes overwrites, No saves the change in a copy, Cancel undoes it. Designs just made by New, Copy or Import don't ask.)*
- [x] Lay the drawings out automatically: the side view sits to the right of the plate, at half the lower bout width plus a gap. Later views (neck, fretboard) are placed the same way. *(`DesignDrawing` in `GuitarForm.Geometry` draws a whole design in mm. The side view sits the gap right of the plate's widest point (its outline and construction lines). The gap is a setting, 50 mm by default. Messages are kept per section.)*
- [x] Live preview with a display conduit: construction geometry light grey, outline in Rhino's feedback colour (so it doesn't look like a built object), redrawn on every change. *(`DesignPreview`, shown while the panel is visible and Preview is ticked.)*
- [x] Build: writes the geometry to the `GuitarForm::Outline` and `GuitarForm::Construction` layers, tagged with the design's id, replacing that design's previous build. Construction geometry is light grey with a solid linetype. *(`DesignBuilder`. The Construction layer is light grey; objects take their layer's colour and linetype. One undo step.)*
- [x] Export: saves the design as JSON. Import: adds a design from a JSON file.

### 8f. String sets

Deferred: string sets are covered by the user's notes on every section's variables, which will be worked into the plan once the plug-in is a working proof of concept (after 8g).

- [ ] **(decision)** What a string set holds (per string: gauge, plain or wound, material, …?), and whether tension is calculated from it with the scale length and tuning.

### 8g. Remove Grasshopper

Done last, once the panel covers every component, so the project stays usable throughout.

- [ ] Remove the components, `Types/`, `GuitarFormComponent`, `GuitarFormPriority`, the component icons and `Examples/Plate.gh`; drop the Grasshopper package. Point the launch profile at Rhino without Grasshopper.
- [ ] Rewrite the README (loading the `.rhp`, using the panel), `docs/` (one page per tab instead of per component) and CLAUDE.md's conventions.
- [ ] Revisit "Considerations for later components" (placing parts with planes) for the plug-in.

## 9. Later

- [ ] Custom soundhole shapes, as the Custom Soundhole component draws them, in the Soundhole section. Decide how the shape is provided (picked from the Rhino document? imported?) and stored.

- [ ] GitHub Actions workflow that builds the plug-in on each push.
- [ ] Yak package for distribution through Rhino's Package Manager.
