# Settings tab

Where the design library and its backups are kept, how often backups are made, and how the drawing is laid out. Settings are kept in Rhino's settings for the plug-in, so they apply to every document.

## Library

All designs and saved copies are kept in one SQLite database, `GuitarForm.db`. The main copy is kept on this computer, outside any synced folder, so a sync service never sees a half-written file. By default it's in `%LOCALAPPDATA%\GuitarForm`.

**Library folder › Change…** picks another folder:

- If the folder already has a `GuitarForm.db`, you're asked whether to use it. The current library stays where it was.
- Otherwise you're asked whether to move the current library there (**Yes**) or start a new, empty library there (**No**). Moving takes the library's upgrade safety copies (`GuitarForm.before-v{N}.bak`) and notes files (`.notes.txt`) along. If a file with any of those names is already in the new folder, nothing is moved and you're told which file is in the way.

The file is marked as a GuitarForm library; GuitarForm won't open another kind of database, or a library made by a newer version of GuitarForm. A library made by an older version is upgraded when it's opened.

If the open design has unsaved changes, changing the library folder first asks whether to save them (**Yes**), discard them (**No**) or cancel. Unsaved changes are kept in a recovery file in `%LOCALAPPDATA%\GuitarForm\recovery`, not in the library; see [Using the panel](../README.md#using-the-panel).

## Backups

Backups are complete copies of the library, written to the backup folder as `GuitarForm 2026-10-08 183005.db` (the date and time). By default the folder is `Documents\GuitarForm\Backups`, which OneDrive usually syncs, so the backups are kept off this computer too. Each backup is written under a temporary name and renamed when it's complete, so the folder only ever holds whole files.

| Setting | Default | Description |
|---|---|---|
| Back up every (minutes) | 15 | How often to check whether the library has changed since the last backup, and back it up if it has. It's also checked when Rhino closes. |
| Keep the last (backups) | 20 | The newest backups always kept. |
| Also keep one a day for (days) | 30 | The newest backup from each of these days is kept as well (today counts as one). Older backups are deleted after each backup. |

- **Back up now** makes a backup straight away, changed or not.
- **Restore…** replaces the library with a backup you choose. The current library is backed up first, so a restore can be undone by restoring that backup. If the open design has unsaved changes, you're asked first whether to save them (**Yes**), discard them (**No**) or cancel. A file that isn't a GuitarForm library is refused and the library left alone. Restore is also how you start on another computer: point the backup folder at the synced folder and restore the newest backup.
- **Open backup folder** opens it in File Explorer.

When GuitarForm upgrades the library to a newer format, it first copies the file beside it as `GuitarForm.before-v1.bak` (the number is the version being upgraded from; a name already taken gets `.1`, `.2` and so on). One safety file is kept per upgrade, and you can delete it by hand once you're happy with the upgraded library. If the copy can't be made, the library isn't opened or changed.

The step to version 4 also converts every design of the old model into a guitar of the new one (the designs and their `NewFields` files stay where they are, as do the old tables). Body presets become saved bodies (they show in the Body tab's **Saved body copies** list); instrument and soundhole presets are not converted and stay in the old tables, with no row in the panel (a later version will drop the old tables). Anything renamed (a guitar with that name already existed), left out (a value that couldn't be read) or not converted is listed in a notes file beside the safety copy (`GuitarForm.before-v3.notes.txt`, numbered like the safety copy and never overwritten), and the panel shows the same notes once after the upgrade and in Rhino's command history.

Backups don't merge changes made on two computers: whichever library is restored last wins.

## Drawing

| Setting | Default | Description |
|---|---|---|
| Gap between plate and side view | 50 mm | The side view's bottom line sits this far right of the plate's widest point (its outline and construction lines). Shown in the document's units. |
