.PHONY: build test package play shell

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
