# UI presentation content packs

`jomon/content_packs/default/ui_text.json` contains wording for contracted
terminal chrome: titles, headings, help lines, notice labels, and selected
tavern and character-creation shell text. Its semantic keys and permitted
format placeholders are engine-owned in `content_packs/contract.json`.

```json
{
  "text": {
    "ui.title.game": "J O M O N",
    "ui.start.save_path": "Save: {path}"
  }
}
```

Writers may change the text values and must retain every required placeholder
exactly once. They must not add, remove, or rename semantic keys; change input
bindings; alter menu action identities; alter terminal geometry; or edit the
engine contract. The engine supplies template values and does not evaluate
expressions, attributes, indexing, conversions, or format specifications.

Notice labels are presentation only. Legacy persisted prefixes such as
`RUMOUR:` remain recognized by engine-owned compatibility code and are rendered
with the selected pack's label. Saved prose is not rewritten.
