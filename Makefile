.PHONY: build test package play shell studio sprite-editor generation-inspector room-editor

COMPOSE ?= ./tools/docker-compose.sh

build:
	$(COMPOSE) build

test:
	$(COMPOSE) run --rm test

package:
	$(COMPOSE) run --rm package

play:
	$(COMPOSE) run --rm game

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
