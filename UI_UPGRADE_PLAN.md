# UI upgrade plan

Revert point: git tag `pre-ui-upgrade` (state before any of this work). Each task below is committed on its own when it works, so any step can be reverted.

Owner decisions: Messages pane becomes a collapsible bar below the tabs; keep tabs (error counts in titles); collapsible field groups; keep the fader slider but make the typed value easy to see/edit; one comfortable density; Eto only, no public API or stored-data changes (apart from the save flow below).

## Part A: Save flow (replaces the overwrite/copy dialog)
Edits change an in-memory working copy only (`_design` + `_newFields`) and mark it dirty; nothing is written until Save. Dirty label beside the Design drop-down. Buttons: Save (enabled when dirty), Save as..., Revert. First save after opening a design asks Overwrite / Save as new / Cancel (asks again after switching away and back; New, Copy, Import open as already confirmed). Save as leaves the original untouched and switches to the new design. Rename is a deferred edit (applies on Save). Preset Load counts as an edit; preset Save as... still writes at once. Preview, Build and Export use the working copy (Build message notes "unsaved"). Switching design, New, Import, Copy, Delete and a Settings library restore/move prompt Save / Don't save / Cancel when dirty. Rhino 8 cannot veto panel or Rhino close, so the dirty working copy is autosaved quietly to a recovery file (never over the design); on opening a design with a newer recovery file, ask "Recover unsaved changes?"; delete it on Save / Don't save. Write NewFields JSON via temp file + move; DB write first.
- S1 working copy, dirty model, Save/Save as/Revert, first-save prompt (GuitarFormPanel.cs, NewFields.cs)
- S2 discard guards on switch/New/Import/Copy/Delete/Rename/LibraryChanged
- S3 RecoveryStore + PanelClosing/OnShutdown flush + debounced autosave + tests
- S4 docs (README, docs/, CLAUDE.md panel sentence, header comments), tick IMPROVEMENTS.md (do after Part B)

## Part B: Visual upgrade
- U1 UiMetrics.cs: spacing, widths, theme-aware colours, DPI scale helper (no visible change)
- U2 NumberSlider: fluid width (min ~120, max ~260), DPI-scaled constants, arrow-key stepping (Shift fine, Ctrl coarse), focus ring, theme-aware tick colour, typed value easy to see/edit
- U3 Field rows: consistent label / stretching box / auto-width unit columns; box widths via UiMetrics (SectionEditor FieldLayout, GuitarFormPanel NewFieldRows)
- U4 Messages: per-line theme-aware messages, collapsible bar below tabs, auto-hide when empty, error/warning counts in tab titles
- U5 Collapsible field groups; preset row pinned above scroll area; tidy design button rows (Import/Export compact)
- U6 Consistent tooltips (unit + range) and a one-line slider hint
- U7 SettingsPage grouped in GroupBoxes; dialogs theme-aware, horizontally resizable, widths from UiMetrics
- U8 Tab order and access keys

Order: S1 -> S2 -> S3 -> U1 -> U2 -> U3 -> U4 -> U5 -> U6 -> U7 -> U8 -> S4. Tasks mostly share GuitarFormPanel.cs, so they run in sequence.

## Status
(update as tasks complete: Pending / In progress / Completed / Verified)
S1 Completed, S2 Completed, S3 Completed, U1 Completed, U2 Completed, U3 Completed, U4 Completed, U5 Completed, U6 Completed, U7 Completed, U8 Completed, S4 Completed, F1 Completed (verification pending the owner's check in Rhino)
