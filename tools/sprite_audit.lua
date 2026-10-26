-- Run from the repository root. Stdout is deterministic machine-readable JSON.
local Audit=require('src.sprite_audit')
io.write(Audit.json(Audit.collect()),'\n')
