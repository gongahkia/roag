LUA ?= lua
.PHONY: run test gui soak benchmark generation
run:
	love .
test:
	$(LUA) tests/syntax.lua
	$(LUA) tests/run.lua
gui:
	$(LUA) tests/gui_smoke.lua
	$(LUA) tests/map_gui.lua
	$(LUA) tests/expansion_gui.lua
soak:
	$(LUA) tools/expansion_soak.lua soak.csv 800
benchmark:
	$(LUA) tools/benchmark.lua
generation:
	$(LUA) tools/generation_benchmark.lua generation.csv
