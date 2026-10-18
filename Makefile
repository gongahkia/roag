# ROAG gameplay. Authoring and publication live in the sibling Paint project.
OS := $(shell uname -s)
LUAJIT ?= luajit
ifeq ($(OS),Darwin)
LOVE_BIN ?= $(HOME)/Applications/ROAG-Love-11.5.app/Contents/MacOS/love
LOVE_RUN = /usr/bin/arch -x86_64 "$(LOVE_BIN)"
GUI_RUN = env PATH="$(CURDIR)/tests/mac-bin:$(PATH)" ROAG_LOVE_BIN="$(LOVE_BIN)"
else
LOVE_BIN ?= love
LOVE_RUN = "$(LOVE_BIN)"
GUI_RUN = env
endif

.PHONY: help game kenney onebit spectator sprite-audit sprite-audit-check sprite-review room-review room-expansion test gui gui-game gui-expansion
help:
	@printf '%s\n' \
	 'make game             Play ROAG with your selected sprite pack' \
	 'make kenney           Try Kenney artwork without changing settings' \
	 'make onebit           Try standalone Kenney 1-Bit artwork' \
	 'make spectator        Inspect Kenney art without saves' \
	 'make sprite-audit     Rebuild read-only coverage and unused-art catalogue' \
	 'make sprite-audit-check Check saved catalogue freshness without writing' \
	 'make sprite-review    Refresh embedded art in the review page' \
	 'make room-review      Refresh the offline room layout review page' \
	 'make room-expansion   Compile the approved room layouts for the game' \
	 'make test             Run gameplay and runtime headless tests' \
	 'make gui              Run game GUI smoke groups' \
	 'Title Settings or Esc -> 3       Choose Classic, Kenney mix or Kenney 1-Bit'
game:
	$(LOVE_RUN) .
kenney:
	env ROAG_SPRITE_PACK=kenney $(LOVE_RUN) .
onebit:
	env ROAG_SPRITE_PACK=onebit $(LOVE_RUN) .
sprite-audit:
	python3 tools/sprite_audit.py
	python3 tools/build_sprite_review.py
sprite-audit-check:
	python3 tools/sprite_audit.py --check
sprite-review:
	python3 tools/build_sprite_review.py
room-review:
	python3 tools/build_room_review.py

room-expansion:
	python3 tools/write_room_expansion.py
spectator:
	env ROAG_SPECTATOR=1 ROAG_SPRITE_PACK=kenney $(LOVE_RUN) .
test: sprite-audit-check
	$(LUAJIT) tests/run.lua
gui: gui-game gui-expansion
gui-game:
	@$(GUI_RUN) sh tests/repeat_gui_smokes.sh 1
	@$(GUI_RUN) sh tests/menu_gui_smoke.sh
	@$(GUI_RUN) sh tests/attack_preview_gui_smoke.sh
	@$(GUI_RUN) sh tests/kenney_symbols_gui_smoke.sh
	@$(GUI_RUN) sh tests/kenney_terrain_gui_smoke.sh
	@$(GUI_RUN) sh tests/onebit_gui_smoke.sh
	@$(GUI_RUN) sh tests/tranche15_gui_smoke.sh
	@$(GUI_RUN) sh tests/tranche15_chain_gui_smoke.sh
gui-expansion:
	@set -e; for suite in expansion_floor_gui_smoke expansion_terrain_gui_smoke \
	 expansion_biome_gui_smoke expansion_encounter_gui_smoke \
	 expansion_class_gui_smoke expansion_boon_gui_smoke \
	 expansion_gamepad_gui_smoke; do \
	 printf 'RUN expansion %s\n' "$$suite"; \
	 $(GUI_RUN) sh "tests/$$suite.sh"; \
	 done
