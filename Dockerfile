# syntax=docker/dockerfile:1

FROM python:3.13-slim AS runtime

ARG TORCH_VERSION=2.12.1+cpu
ARG TORCH_INDEX_URL=https://download.pytorch.org/whl/cpu

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    PIP_NO_CACHE_DIR=1 \
    PIP_ROOT_USER_ACTION=ignore

WORKDIR /app

COPY pyproject.toml README.md LICENSE ./
COPY src ./src

RUN python -m pip install --upgrade pip \
    && python -m pip install \
        --index-url "${TORCH_INDEX_URL}" \
        --extra-index-url https://pypi.org/simple \
        "torch==${TORCH_VERSION}" \
    && python -m pip install .

ENTRYPOINT ["kenjaku"]
CMD ["--version"]
