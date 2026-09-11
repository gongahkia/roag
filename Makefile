.PHONY: build run test check

build:
	go build -o bin/nightshift ./cmd/nightshift

run:
	go run ./cmd/nightshift

test:
	go test ./...

check:
	gofmt -w $$(find . -name '*.go' -not -path './vendor/*')
	go vet ./...
	go test ./...
	go build ./...

