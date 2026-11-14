# ROAG: Lua/LÖVE turn-based roguelike with catalogue-backed artwork.
LOVE_BIN ?= love
LUAJIT ?= luajit
PYTHON ?= python3
.PHONY: help game test gui assets assets-check catalog-test preservation package spectator card-trial card-fixed card-test card-gui boggle-trial boggle-test boggle-gui
help:
	@printf '%s\n' 'make game          Play the Lua/LÖVE roguelike' \
	 'make spectator     Preview biomes/floors/classes with G/J/K/L/M' \
	 'make card-trial    Open Poker Stamina in the nonpersistent trial' \
	 'make card-fixed    Open the Fixed Stamina comparison' \
	 'make dice-trial    Open the custom-dice combat trial (seed 202)' \
	 'make dice-test     Verify dice actions, forecasts and input boundaries' \
	 'make boggle-trial  Open the four-tile action-board experiment' \
	 'make boggle-test   Verify routes, forecasts and board resources' \
	 'make boggle-gui    Verify rendered routes, latency and isolation' \
	 'make card-test     Run focused card/action boundary tests' \
	 'make card-gui      Play trials through actual LÖVE callbacks (needs display)' \
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
card-trial: assets-check
	"$(LOVE_BIN)" . --card-trial
card-fixed: assets-check
	"$(LOVE_BIN)" . --fixed-stamina
boggle-trial: assets-check
	"$(LOVE_BIN)" . --boggle-trial
boggle-test:
	$(LUAJIT) tests/boggle_tests.lua
boggle-gui: assets-check
	sh tests/boggle_gui.sh
card-test:
	$(LUAJIT) tests/card_trial_tests.lua
card-gui: assets-check
	sh tests/card_trial_gui.sh
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

.PHONY: dice-trial dice-test
dice-trial: assets-check
	"$(LOVE_BIN)" . --dice-trial --trial-seed=202
dice-test:
	$(LUAJIT) tests/dice_trial_tests.lua

.PHONY: dice-presentation-test
dice-presentation-test:
	$(LUAJIT) tests/dice_presentation_tests.lua
