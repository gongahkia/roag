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

.PHONY: help game kenney spectator test gui gui-game gui-expansion
help:
	@printf '%s\n' \
	 'make game             Play ROAG with your selected sprite pack' \
	 'make kenney           Try Kenney artwork without changing settings' \
	 'make spectator        Inspect Kenney art without saves' \
	 'make test             Run gameplay and runtime headless tests' \
	 'make gui              Run game GUI smoke groups' \
	 'Title Settings or Esc -> 3       Choose Classic shapes or Kenney'
game:
	$(LOVE_RUN) .
kenney:
	env ROAG_SPRITE_PACK=kenney $(LOVE_RUN) .
spectator:
	env ROAG_SPECTATOR=1 ROAG_SPRITE_PACK=kenney $(LOVE_RUN) .
test:
	$(LUAJIT) tests/run.lua
gui: gui-game gui-expansion
gui-game:
	@$(GUI_RUN) sh tests/repeat_gui_smokes.sh 1
	@$(GUI_RUN) sh tests/menu_gui_smoke.sh
	@$(GUI_RUN) sh tests/attack_preview_gui_smoke.sh
	@$(GUI_RUN) sh tests/kenney_symbols_gui_smoke.sh
	@$(GUI_RUN) sh tests/kenney_terrain_gui_smoke.sh
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
