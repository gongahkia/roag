# BigBlue TerminalPlus

`BigBlue_TerminalPlus.ttf` is the original BigBlue TerminalPlus font by
VileR (© 2015), distributed under the Creative Commons Attribution-ShareAlike
4.0 International license in `LICENSE-BigBlue-Terminal.txt`.

Source: https://int10h.org/blog/2015/12/bigblue-terminal-oldschool-fixed-width-font/

The font file is unmodified. ROAG and ROAG Studio use it at 12 and 24 pixels,
which are integer multiples of its native 8×12 pixel cell. It is the default
UI font until a game-specific face is authored and imported.

Native MonoGame UI text now uses cached 10/12/14-pixel faces for small, normal and large text. The pixel option uses BigBlue TerminalPlus; the sans option uses the unmodified `SourceCodePro-Regular.otf` by Adobe (2023), under the SIL Open Font License in `LICENSE-SourceCodePro.md`. The font was copied from the installed Adobe Source Code Pro package; its copyright and reserved-name notice are preserved.

`python3 tools/build_ui_fonts.py` regenerates `ui-fonts.png` and `ui-fonts.json` from these bundled font files; `--check` verifies exact outputs. Regeneration needs a C compiler, pkg-config and FreeType development headers. The game consumes the checked-in PNG/JSON and needs no font compiler or FreeType runtime. Source font SHA-256 values are embedded in the metadata. These are UI text glyphs, separate from the unchanged universal catalog of Vagabonds gameplay sprites.
