FROM ubuntu:24.04

ENV DEBIAN_FRONTEND=noninteractive

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        love \
        luajit \
        unzip \
        zip \
    && rm -rf /var/lib/apt/lists/*

RUN groupadd --gid 1000 roag \
    && useradd --uid 1000 --gid roag --create-home --shell /bin/bash roag

WORKDIR /workspace
USER roag

CMD ["luajit", "tests/run.lua"]
