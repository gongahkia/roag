# ROAG: Lua/LÖVE turn-based roguelike with catalogue-backed artwork.
LOVE_BIN ?= love
LUAJIT ?= luajit
PYTHON ?= python3
.PHONY: help game test gui assets assets-check catalog-test preservation package spectator
help:
	@printf '%s\n' 'make game          Play the Lua/LÖVE roguelike' \
	 'make spectator     Preview biomes/floors/classes with G/J/K/L/M' \
	 'make test          Run gameplay and current-art regression tests' \
	 'make gui           Verify actual LÖVE input, artwork and save/Continue' \
	 'make assets-check  Verify deterministic catalogue bindings' \
	 'make assets        Regenerate the small Lua artwork manifest' \
	 'make catalog-test  Check the preserved catalogue and its tools' \
	 'make preservation  Verify retained originals and archived old art' \
	 'make package       Build dist/roag.love using only current artwork'
game: assets-check
	"$(LOVE_BIN)" .
spectator: assets-check
	"$(LOVE_BIN)" . --spectator
assets:
	$(PYTHON) -B tools/build_classic_assets.py
assets-check:
	$(PYTHON) -B tools/build_classic_assets.py --check
test: assets-check
	$(LUAJIT) tests/classic_run.lua
catalog-test:
	$(PYTHON) -B -m unittest discover -s tests/catalog -v
gui: assets-check
	sh tests/classic_restore_gui.sh
preservation:
	$(PYTHON) -B tools/verify_classic_preservation.py
package: assets-check
	$(PYTHON) -B tools/package_classic.py
