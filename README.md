# GuitarForm

<img src="GuitarForm/Resources/Source/GuitarForm.png" alt="GuitarForm logo" width="280">

A guitar designer for **Rhino 8**. A dockable panel has a tab for each section of the guitar, such as the instrument, body and soundhole. Designs are saved in a library on your computer, with presets for each section and automatic backups. The design is previewed live in Rhino as you type, and built into the document when you're ready.

## Requirements

- Rhino 8 (Windows), running on its default .NET runtime. The plug-in won't load if Rhino has been switched to .NET Framework with `SetDotNetRuntime`.
- .NET SDK 8 or later, to build.

## Build

From the repository folder:

```
dotnet build GuitarForm/GuitarForm.csproj
```

The plug-in is written to `GuitarForm/bin/Debug/net7.0-windows/GuitarForm.rhp`, with the libraries it needs beside it. Keep them together.

## Load in Rhino

**Option A: drag and drop.** Drag `GuitarForm.rhp` from the build folder into Rhino. Rhino installs it and loads it again each time it starts. Close Rhino before rebuilding, because it locks the `.rhp`.

**Option B: Visual Studio (best while developing).** Open `GuitarForm.sln` and press **F5**. The **Rhino 8** launch profile (`GuitarForm/Properties/launchSettings.json`) builds the plug-in, starts Rhino 8 with the build folder in `RHINO_PACKAGE_DIRS` so it loads without being installed, opens the GuitarForm panel and attaches the debugger. If Rhino is installed somewhere other than `C:\Program Files\Rhino 8`, change `executablePath` in the launch profile.

Then run the **`GuitarForm`** command to open the panel. Dock it by dragging its tab into the panel area beside Properties and Layers: a floating panel hides whenever another app has focus.

## Using the panel

- **Design:** pick a design from the list, or use **New…** (asks for the name, body length, scale length and neck join fret), **Copy**, **Rename…**, **Delete**, **Import…** and **Export…** (a JSON design document).
- **Section tabs:** each section's values, with its errors, warnings and notes underneath. Hover over a field for its description. Optional fields can be left blank; the drawing shows whatever the values set so far allow.
- **Presets:** each section tab can **Save as…** its values as a named preset, **Load** a preset into the design (copying its values, so changing the preset later doesn't change the design), or **Delete** one.
- **Saving:** the first change after you open a design asks whether to overwrite it (**Yes**), save the change in a copy (**No**), or undo the change (**Cancel**). After that, every change is saved as you make it. A design you've just made with New, Copy or Import doesn't ask.
- **Preview:** the design is drawn in the viewports as you type, while the panel is visible and **Preview** is ticked. Construction geometry is light grey and the outline is in Rhino's feedback colour.
- **Build:** writes the design into the document, on the `GuitarForm::Outline` and `GuitarForm::Construction` layers (light grey). Building a design again replaces its previous build. One Undo removes a build.

The guitar is drawn **vertically**: the tail end of the body is at the origin and the body length runs up the **+Y** axis. The side view is drawn to the right of the plate.

### Units

Every length in GuitarForm is shown and typed in the **Rhino document's units**, and the unit is shown beside each field. GuitarForm stores everything in **millimetres**, so a design means the same in any document. Change the document's units, or open a document in other units, and the panel shows the same design in the new units. The JSON design documents are in millimetres too.

## Tabs

| Tab | What it holds | Details |
|---|---|---|
| Instrument | Values for the whole instrument: scale length, neck join fret. | [docs/Instrument.md](docs/Instrument.md) |
| Body | The body outline (plate) from its bouts, waist, tail and heel, and the side view from its depths. | [docs/Body.md](docs/Body.md) |
| Soundhole | A round soundhole. | [docs/Soundhole.md](docs/Soundhole.md) |
| Settings | Where the library and its backups are kept, backup timing, and the drawing gap. | [docs/Settings.md](docs/Settings.md) |

## Library and backups

Designs and presets are kept in one SQLite library, `GuitarForm.db`, in `%LOCALAPPDATA%\GuitarForm` by default. It's backed up every 15 minutes while it has changes, and when Rhino closes, to `Documents\GuitarForm\Backups` (which OneDrive usually syncs). See [docs/Settings.md](docs/Settings.md) to move either, change the backup timing, back up by hand or restore.

## Tests

The model, library and geometry are tested with NUnit and [Rhino.Testing](https://github.com/mcneel/Rhino.Testing), which loads the installed Rhino 8 into the test run, so Rhino 8 must be installed. From the repository folder:

```
dotnet test GuitarForm.Tests
```

The geometry tests check the Quick Test values in `docs/` and the error cases; the library tests use a temporary database for each test. If Rhino is installed somewhere other than `C:\Program Files\Rhino 8`, change `RhinoSystemDirectory` in `GuitarForm.Tests/Rhino.Testing.Configs.xml`.

## Troubleshooting

- **`GuitarForm` is an unknown command:** the plug-in isn't loaded. Drag `GuitarForm.rhp` into Rhino again, or check it's enabled in **Options › Plug-ins**.
- **The panel says it couldn't open the library:** the message says why. A file that isn't a GuitarForm library, or one made by a newer GuitarForm, isn't opened. Choose another library folder on the Settings tab, or restore a backup.
- **"Unable to load DLL 'e_sqlite3'":** the `.rhp` was copied without the files beside it. Load it from the build folder, or copy the whole folder.
- **Build fails because the file is in use:** close Rhino, then rebuild.
- **No preview:** open a design, make sure **Preview** is ticked and the panel is visible, and check the tabs for errors.
