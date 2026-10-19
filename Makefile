ifeq ($(OS),Windows_NT)
PYTHON ?= python
else
PYTHON ?= python3
endif

ARGS ?=

.PHONY: all help setup build run check test clean

all: build

help:
	@echo make run   - install Odin if needed, build, and play
	@echo make build - install Odin if needed and compile
	@echo make setup - install or reuse the pinned Odin compiler
	@echo make check - check Odin source
	@echo make test  - run installer and native tests
	@echo make clean - remove build output
	@echo Use ARGS to pass game options. See README.md for examples.

setup:
	$(PYTHON) scripts/bootstrap.py

build:
	$(PYTHON) scripts/build.py build

run:
	$(PYTHON) scripts/build.py run -- $(ARGS)

check:
	$(PYTHON) scripts/build.py check

test:
	$(PYTHON) -m unittest discover -s scripts -p "test_*.py"
	$(PYTHON) scripts/build.py test

clean:
	$(PYTHON) -c "import shutil; shutil.rmtree('build', ignore_errors=True)"
