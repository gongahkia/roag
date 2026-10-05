.PHONY: build test package play shell studio sprite-editor generation-inspector room-editor graphics-doctor \
	debug doctor debug-content debug-scenario debug-expedition debug-modifier debug-determinism debug-bundle debug-test

COMPOSE ?= ./tools/docker-compose.sh

build:
	$(COMPOSE) build

test:
	$(COMPOSE) run --rm test

package:
	$(COMPOSE) run --rm package

play:
	$(COMPOSE) run --rm game

graphics-doctor:
	$(COMPOSE) run --rm graphics-doctor

shell:
	$(COMPOSE) run --rm shell

studio:
	$(COMPOSE) run --rm studio

sprite-editor:
	$(COMPOSE) run --rm sprite-editor

generation-inspector:
	$(COMPOSE) run --rm generation-inspector

room-editor:
	$(COMPOSE) run --rm room-editor

debug:
	$(COMPOSE) run --rm debug help

doctor:
	$(COMPOSE) run --rm debug doctor

debug-content:
	$(COMPOSE) run --rm debug doctor --out content

debug-scenario:
	$(COMPOSE) run --rm debug scenario --name "$(or $(SCENARIO),bomb-self)" --seed "$(or $(SEED),44001)" --out scenario

debug-expedition:
	$(COMPOSE) run --rm debug expedition --seed "$(or $(SEED),1337)" --character "$(or $(CHARACTER),expedition.gunner)" --out expedition

debug-modifier:
	$(COMPOSE) run --rm debug modifier --id "$(or $(ID),expedition.passive.arc_relay)" --stacks "$(or $(STACKS),5)" --trigger "$(or $(TRIGGER),on_pierce)" --tags "$(or $(TAGS),projectile,piercing)" --capabilities "$(or $(CAPABILITIES),ability.electrical.discharge)" --out modifier

debug-determinism:
	$(COMPOSE) run --rm debug determinism --seed "$(or $(SEED),1337)" --character "$(or $(CHARACTER),expedition.gunner)" --out determinism

debug-bundle:
	$(COMPOSE) run --rm debug bundle --label "$(or $(LABEL),bundle)"

debug-test:
	@test -n "$(TEST)" || (echo 'Usage: make debug-test TEST="text in test name"' >&2; exit 2)
	$(COMPOSE) run --rm -e ROAG_TEST_MATCH="$(TEST)" test
