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

.PHONY: help game spectator sprite-audit sprite-audit-check surplus-audit surplus-audit-check sprite-ideas sprite-ideas-check sprite-ideas-test item-room-review item-room-review-check item-room-review-test sprite-review enemy-review enemy-review-check review-test room-review room-expansion environment-preview environment-preview-check test gui gui-game gui-expansion gui-enemy-squads
help:
	@printf '%s\n' \
	 'make game             Play ROAG with fixed Kenney 1-Bit artwork' \
	 'make spectator        Inspect Kenney art without saves' \
	 'make sprite-audit     Rebuild read-only coverage and unused-art catalogue' \
	 'make sprite-audit-check Check saved catalogue freshness without writing' \
	 'make surplus-audit    Rebuild the checked unused-art visual shortlist' \
	 'make surplus-audit-check Check the shortlist against live mappings' \
	 'make sprite-ideas     Build the full unused-art idea approval page' \
	 'make sprite-ideas-test Check idea coverage, feedback and page freshness' \
	 'make item-room-review Build the focused item/interactive-room voting page' \
	 'make item-room-review-test Check the focused review and feedback safety' \
	 'make sprite-review    Refresh embedded art in the review page' \
	 'make enemy-review     Build the five-combination approval page' \
	 'make review-test      Check the review data, client and generated page' \
	 'make room-review      Refresh the offline room layout review page' \
	 'make room-expansion   Compile the approved room layouts for the game' \
	 'make environment-preview Rebuild offline source-pixel room previews (no LÖVE)' \
	 'make environment-preview-check Check exact generated preview pixels' \
	 'make test             Run gameplay and runtime headless tests' \
	 'make gui              Run game GUI smoke groups' \
	 'make gui-enemy-squads  Check approved squads with the fixed 1-Bit artwork' \
	 'Title Settings or Esc -> 3       Sound, effects and accessibility'
game:
	$(LOVE_RUN) .
sprite-audit:
	python3 tools/sprite_audit.py
	python3 tools/build_sprite_review.py
	python3 tools/build_enemy_review.py
sprite-audit-check:
	python3 tools/sprite_audit.py --check
surplus-audit: sprite-audit-check
	python3 tools/build_sprite_surplus.py
surplus-audit-check: sprite-audit-check
	python3 tools/build_sprite_surplus.py --check
sprite-ideas: sprite-audit-check
	python3 tools/build_sprite_ideas.py
sprite-ideas-check: sprite-audit-check
	python3 tools/build_sprite_ideas.py --check
sprite-ideas-test: sprite-ideas-check
	node tests/sprite_ideas_page_test.js
item-room-review: sprite-audit-check
	python3 tools/build_item_room_review.py
item-room-review-check: sprite-audit-check
	python3 tools/build_item_room_review.py --check
item-room-review-test: item-room-review-check
	node tests/item_room_review_page_test.js
sprite-review:
	python3 tools/build_sprite_review.py
	python3 tools/build_enemy_review.py
enemy-review:
	python3 tools/build_enemy_review.py
enemy-review-check:
	python3 tools/build_enemy_review.py --check
review-test: enemy-review-check
	node tests/enemy_review_page_test.js
room-review:
	python3 tools/build_room_review.py

room-expansion:
	python3 tools/write_room_expansion.py
environment-preview:
	python3 tools/environment_preview.py
environment-preview-check:
	python3 tools/environment_preview.py --check
spectator:
	env ROAG_SPECTATOR=1 $(LOVE_RUN) .
test: sprite-audit-check
	$(LUAJIT) tests/run.lua
gui: gui-game gui-expansion gui-enemy-squads
gui-enemy-squads:
	@$(GUI_RUN) sh tests/enemy_squad_gui_smoke.sh
gui-game:
	@$(GUI_RUN) sh tests/repeat_gui_smokes.sh 1
	@$(GUI_RUN) sh tests/menu_gui_smoke.sh
	@$(GUI_RUN) sh tests/attack_preview_gui_smoke.sh
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
