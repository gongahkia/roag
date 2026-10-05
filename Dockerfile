FROM alpine:3.20

RUN apk add --no-cache \
        bash \
        ca-certificates \
        git \
        ffmpeg \
        love \
        luajit \
        mesa-demos \
        mesa-dri-gallium \
        mesa-egl \
        mesa-gl \
        unzip \
        zip \
    && addgroup -g 1000 roag \
    && adduser -D -u 1000 -G roag roag

WORKDIR /workspace
USER roag

CMD ["luajit", "tests/run.lua"]
