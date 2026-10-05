.PHONY: build test package play shell

COMPOSE ?= docker compose

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
