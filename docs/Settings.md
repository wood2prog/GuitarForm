# Settings tab

Where the design library and its backups are kept, how often backups are made, and how the drawing is laid out. Settings are kept in Rhino's settings for the plug-in, so they apply to every document.

## Library

All designs and presets are kept in one SQLite database, `GuitarForm.db`. The main copy is kept on this computer, outside any synced folder, so a sync service never sees a half-written file. By default it's in `%LOCALAPPDATA%\GuitarForm`.

**Library folder › Change…** picks another folder:

- If the folder already has a `GuitarForm.db`, you're asked whether to use it. The current library stays where it was.
- Otherwise you're asked whether to move the current library there (**Yes**) or start a new, empty library there (**No**).

The file is marked as a GuitarForm library; GuitarForm won't open another kind of database, or a library made by a newer version of GuitarForm. A library made by an older version is upgraded when it's opened.

## Backups

Backups are complete copies of the library, written to the backup folder as `GuitarForm 2026-10-08 183005.db` (the date and time). By default the folder is `Documents\GuitarForm\Backups`, which OneDrive usually syncs, so the backups are kept off this computer too. Each backup is written under a temporary name and renamed when it's complete, so the folder only ever holds whole files.

| Setting | Default | Description |
|---|---|---|
| Back up every (minutes) | 15 | How often to check whether the library has changed since the last backup, and back it up if it has. It's also checked when Rhino closes. |
| Keep the last (backups) | 20 | The newest backups always kept. |
| Also keep one a day for (days) | 30 | The newest backup from each of these days is kept as well (today counts as one). Older backups are deleted after each backup. |

- **Back up now** makes a backup straight away, changed or not.
- **Restore…** replaces the library with a backup you choose. The current library is backed up first, so a restore can be undone by restoring that backup. A file that isn't a GuitarForm library is refused and the library left alone. Restore is also how you start on another computer: point the backup folder at the synced folder and restore the newest backup.
- **Open backup folder** opens it in File Explorer.

Backups don't merge changes made on two computers: whichever library is restored last wins.

## Drawing

| Setting | Default | Description |
|---|---|---|
| Gap between plate and side view | 50 mm | The side view's bottom line sits this far right of the plate's widest point (its outline and construction lines). Shown in the document's units. |
