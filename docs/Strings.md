# Strings tab

The library of string sets: the set's manufacturer and name, and for each string its description, diameter, construction (plain or wound), unit weight, and the core of a wound string. A guitar chooses one set for its neck. Every field's meaning is in [Glossary.md](Glossary.md); this page covers how the tab works.

The tab works with no design open, since sets are library items shared by every guitar. Lengths are shown and typed in the Rhino document's units and stored in millimeters; the unit weight is shown in lb/in or kg/m and stored in kg/m. See [Units](../README.md#units).

## Picker and buttons

The **String set** picker lists the saved sets by name. Above the editor, the buttons are:

| Button | What it does |
|---|---|
| **New** | Starts a new, unsaved set with one plain string. It asks about unsaved changes first. |
| **Save** | Writes the working copy to the library. Enabled only while there are unsaved changes. |
| **Save as…** | Asks for a new manufacturer and name, and saves the working copy as a new set, which is then shown. The original is left as it was. A name already used by another set (ignoring case) is refused. |
| **Revert** | Throws away the unsaved changes to the shown set, after asking. Enabled only while there are unsaved changes. |
| **Delete** | Deletes the shown saved set (see below). Enabled only for a saved set. |
| **Use for this guitar** | Chooses the shown set for the open design's guitar (see below). |

The label beside the picker says **Unsaved changes** while the working copy differs from the saved set. Below the buttons, **Used by this guitar** names the set the open design uses, or **none**.

Save is refused, with the reason shown, while any text can't be read or the set has errors (for example a required field is blank). The errors are listed in the Messages bar.

### Delete

- A set that no saved neck uses is deleted after you confirm (**OK** or **Cancel**). Its unsaved changes are lost too, and the message says so.
- The check for necks that use the set covers only saved necks in the library, not designs that use it (a design using a deleted set shows **none** with a note).
- A set that saved necks use can't be deleted without a replacement. The dialog lists those necks and asks for the set they should use instead, chosen from the other saved sets. **Delete** replaces the set in those necks and then deletes it; **Cancel** does nothing.
- If no other set exists, the dialog says so and asks you to save another set first.

Only saved necks in the library are checked for this. A design's own copy of the choice is not (see Limitations).

### Use for this guitar

**Use for this guitar** is enabled only when a design is open, the shown set is saved, and it has no unsaved changes. Otherwise its tooltip says what to do first: open a design, save the new set, or save the changes to this set.

Choosing it makes the set the open design's guitar set, which marks the design as having unsaved changes. Save the design to keep it.

## Unsaved changes

- Choosing another set in the picker, **New**, or changing the library in Settings asks **Save changes to "…"?** with **Yes** (save, then carry on), **No** (discard, then carry on) or **Cancel** (stay where you are). If the save is refused, the switch is cancelled too.
- Switching, creating, copying, opening or importing a design asks only about the design's own unsaved changes. Unsaved string set edits are independent of the design and stay in the tab while you change designs. Changing the library in Settings asks about the design and then about unsaved set changes; **No** puts the tab back to the saved set, so the question isn't asked again.
- **Revert** asks to confirm before discarding.

## Recovery

- Unsaved edits to a string set are kept in one recovery file per library, outside the library. The file is written 2 s after an edit, and when the panel hides or Rhino closes.
- When the tab loads, and after a library change, a recovered set is offered with **Yes** or **No**: **Recover unsaved changes to the string set "…"?** for a saved set, or **Recover the unsaved new string set?** for one not yet saved. If the saved set has been deleted meanwhile, the offer is **Recover as a new string set?**. **Yes** restores the recovered content; **No** deletes the file.
- If the recovered content equals the saved set, the file is deleted without a prompt.
- The file is deleted on Save, Save as, Revert, Delete and New, and when the offer is declined.
- Not recovered: text typed in a box that doesn't parse, and the unit-weight display preference.

## Set fields

| Field | Abbreviation | Notes |
|---|---|---|
| String Set Manufacturer | SSM | The company that makes the set. |
| String Set Name | SSN | The manufacturer's name or product code, e.g. EJ16. |
| String Count | SC | Read-only. Always the number of strings listed below. |

## String fields

Each string has its own group, headed by its position: String 1 is the treble, and the last string is marked (bass). Each group has these fields:

| Field | Abbreviation | Notes | Disabled when |
|---|---|---|---|
| String Description | STD | A short label, e.g. the note or part number. | Never. |
| String Diameter | STDI | The outside diameter (gauge). Required. | Never. |
| String Construction | STC | Plain or wound. A new string is plain. | Never. |
| String Unit Weight | STUW | The mass per unit length as published. Required. Stored in kg/m. | Never. |
| String Hex Core | STHC | Whether the core is hexagonal (ticked) or round. | Unless the string is wound. |
| String Core Material | STCM | Not specified, solid nylon, nylon floss, steel or gut. | Unless the string is wound. |
| String Core Modulus | STCE | The core's modulus of elasticity, in GPa. Required when wound with the core material not specified. | Unless the string is wound and the core material is not specified. |
| String Core Strength | STCS | The core's ultimate tensile strength, in MPa. Required when wound with the core material not specified. | Unless the string is wound and the core material is not specified. |
| String Used in Saddle Compensation | STSC | Whether the string's compensated position is used to fit the straight saddle line. | Never. |

A disabled field keeps its value, so switching a string back to wound brings its core settings back. The model ignores the values of disabled fields.

### Unit weight units

The **Unit weight shown in** drop-down at the top of the tab chooses **lb/in** (the default) or **kg/m** for every string's unit weight. It changes only what is shown and typed: the stored value is kg/m either way. The choice is remembered in the plug-in's settings. Conversion uses 1 lb/in = 17.857967 kg/m (0.45359237 kg per pound over 0.0254 m per inch). Switching the unit doesn't change the set, and a weight whose text can't be read loses that text, with its error.

## Strings

- **Add String** appends a plain string at the end (the bass). Its fields start blank.
- **Remove String**, on each string's group, removes that string. It's disabled when only one string is left.
- String Count follows the list, so it's never out of step.

## Used by a guitar

The design's chosen set is kept by its id with the design in the library, and the design's JSON export also writes it in full. When a design is opened, its set is looked up by that id. If the set has been deleted, the design shows **none**, with a note that the set is no longer in the library.

## Limitations

- Tension is not calculated. The unit weight is stored for a later calculation, which is not decided.
- Deleting a set can't see designs that use it, only saved necks in the library (see Delete). Such a design shows none the next time it's opened.

## Quick Test

Use a document in millimeters. The unit weight conversion (0.0002 lb/in) and the wound string's core values (5 GPa, 100 MPa) match `StringSetTests`.

1. Open the **Strings** tab with no design open. Click **New**.
2. Enter Manufacturer `Test` and Name `Quick 2`.
3. String 1 (treble), plain: Description `E4 plain`, Diameter `0.3` mm, Unit Weight `0.0002` lb/in. Hex Core and Core Material are disabled, and Core Modulus and Strength are disabled.
4. Click **Add String**. String 2 (bass) is plain. Set its Construction to **Wound**, then: Description `E2 wound`, Diameter `0.9` mm, Unit Weight `0.0008` lb/in, Hex Core unticked, Core Material **not specified**, Core Modulus `5` GPa, Core Strength `100` MPa. The core fields are now enabled, and String Count reads `2`.
5. Click **Save**. The picker shows `Test Quick 2`, the label clears, and **Use for this guitar** is disabled while no design is open.
6. Reopen the set: pick another set in the picker, then `Test Quick 2` again (or close and reopen the panel). Both strings, the Core Modulus and Strength, and the Unit Weight in lb/in are as entered.
7. Set **Unit weight shown in** to **kg/m**. String 1 shows `0.0035716` kg/m (0.0002 lb/in) and String 2 shows `0.0142864` kg/m (0.0008 lb/in). Set it back to **lb/in**; the stored values are unchanged, and the setting is kept after the panel is closed and reopened.
8. Open a design. Its guitar uses none. On the Strings tab, pick `Test Quick 2` and click **Use for this guitar**. **Used by this guitar** reads `Test Quick 2`, and the design shows Unsaved changes. Save the design, then use **Export…** and find `"Test Quick 2"` in the file's `newFields`.
9. Delete `Test Quick 2` from the Strings tab (no saved neck uses it). It's removed after the confirmation. Reopen the design: its guitar uses none, with the note that the set is no longer in the library.
