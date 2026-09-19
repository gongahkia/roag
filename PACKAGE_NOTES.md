# Package notes

The ZIP has one top-level folder: cosmonauts/. Extract into a new location and run
`love .` inside it. This is the entire project; no prior download or manual merge is
needed. It deliberately excludes personal saves, .git, binaries, fonts and build-host
tools. Example maps and regression fixtures are included.

Source manifests are generated after current verification. `tests/syntax.lua` has a
static manifest so it does not depend on a shell when used inside LÖVE. The copy/paste
source companion contains every Lua file and the current human documentation;
large JSON example maps and binary-like textual regression fixtures are in the ZIP.
The ZIP is the recommended runnable delivery.
