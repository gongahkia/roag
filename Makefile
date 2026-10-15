# ROAG launch and validation shortcuts. Run from this checkout's root.
OS := $(shell uname -s)
LUAJIT ?= luajit
PUBLISHER ?= art
PROJECT ?=
OVERWRITE ?= no

ifeq ($(OS),Darwin)
LOVE_BIN ?= $(HOME)/Applications/ROAG-Love-11.5.app/Contents/MacOS/love
LOVE_RUN = /usr/bin/arch -x86_64 "$(LOVE_BIN)"
GUI_RUN = env PATH="$(CURDIR)/tests/mac-bin:$(PATH)" ROAG_LOVE_BIN="$(LOVE_BIN)"
SAVE_DIR ?= $(HOME)/Library/Application Support/LOVE/roag-foundation
else
LOVE_BIN ?= love
LOVE_RUN = "$(LOVE_BIN)"
GUI_RUN = env
SAVE_DIR ?=
endif

.PHONY: help game studio atlas-build test gui gui-game gui-studio gui-expansion \
        publish-preview publish-apply publish-check

help:
	@printf '%s\n' \
	  'make game                       Play ROAG' \
	  'make studio                     Open ROAG Studio' \
	  'make atlas-build                Compile a draft atlas candidate' \
	  'make test                       Run the full headless LuaJIT suite' \
	  'make gui                        Run game, Studio, and expansion GUI smokes' \
	  'make gui-game | gui-studio | gui-expansion   Run one GUI group' \
	  'make publish-preview PROJECT=id [PUBLISHER=art|world|theme|audio|atlas]' \
	  'make publish-apply PROJECT=id APPLY=yes [PUBLISHER=...] [OVERWRITE=yes]' \
	  'Set LOVE_BIN for a different LÖVE 11.5 executable; set SAVE_DIR if needed.'

game:
	$(LOVE_RUN) .

studio:
	$(LOVE_RUN) . --studio

atlas-build:
	$(LOVE_RUN) . --atlas-build

test:
	$(LUAJIT) tests/run.lua

gui: gui-game gui-studio gui-expansion

gui-game:
	$(GUI_RUN) sh tests/repeat_gui_smokes.sh 1
	$(GUI_RUN) sh tests/tranche15_gui_smoke.sh
	$(GUI_RUN) sh tests/tranche15_chain_gui_smoke.sh

gui-studio:
	@set -e; for suite in studio_gui_smoke studio_e01_gui_smoke \
	  studio_e03_gui_smoke studio_e04_gui_smoke studio_e05_gui_smoke \
	  studio_e06_gui_smoke studio_reference_gui_smoke \
	  studio_gui_publish_smoke studio_visual_slots_gui_smoke; do \
	    $(GUI_RUN) sh "tests/$$suite.sh"; \
	  done

gui-expansion:
	@set -e; for suite in expansion_floor_gui_smoke expansion_terrain_gui_smoke \
	  expansion_terrain_studio_gui_smoke expansion_biome_gui_smoke \
	  expansion_encounter_gui_smoke expansion_class_gui_smoke \
	  expansion_boon_gui_smoke expansion_audio_gui_smoke \
	  expansion_gamepad_gui_smoke; do \
	    $(GUI_RUN) sh "tests/$$suite.sh"; \
	  done

publish-check:
	@test -n "$(PROJECT)" || { echo 'Set PROJECT=<Studio project ID>' >&2; exit 2; }
	@test -n "$(SAVE_DIR)" && test -d "$(SAVE_DIR)" || { \
	  echo 'Set SAVE_DIR to the directory shown in Studio' >&2; exit 2; }

publish-preview: publish-check
	@$(MAKE) --no-print-directory publish-run

publish-apply: publish-check
	@test "$(APPLY)" = yes || { echo 'Set APPLY=yes to publish' >&2; exit 2; }
	@$(MAKE) --no-print-directory publish-run PUBLISH_FLAGS="--apply $(if $(filter yes,$(OVERWRITE)),--overwrite,)"

.PHONY: publish-run
publish-run:
	@case "$(PUBLISHER)" in \
	  art) script=studio_publish ;; \
	  world) script=studio_world_publish ;; \
	  theme) script=studio_theme_publish ;; \
	  audio) script=studio_audio_publish ;; \
	  atlas) script=studio_atlas_publish ;; \
	  *) echo 'PUBLISHER must be art, world, theme, audio, or atlas' >&2; exit 2 ;; \
	 esac; \
	 $(LUAJIT) "tools/$$script.lua" --save-dir "$(SAVE_DIR)" --project "$(PROJECT)" $(PUBLISH_FLAGS)
