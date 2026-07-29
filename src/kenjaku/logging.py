from __future__ import annotations

import json as _json
import logging as _logging
from collections.abc import Mapping, Sequence
from typing import Any

_RESERVED_LOG_RECORD_KEYS = set(
    _logging.LogRecord(
        name="",
        level=0,
        pathname="",
        lineno=0,
        msg="",
        args=(),
        exc_info=None,
    ).__dict__
)
_LOG_LEVELS = {
    "debug": _logging.DEBUG,
    "info": _logging.INFO,
    "warning": _logging.WARNING,
    "error": _logging.ERROR,
}


def get_logger(name: str) -> _logging.Logger:
    return _logging.getLogger(name)


def configure_logging(level: str, json: bool = False) -> None:
    normalized = level.lower()
    if normalized not in _LOG_LEVELS:
        raise ValueError("log level must be one of: debug, info, warning, error")
    handler = _logging.StreamHandler()
    if json:
        handler.setFormatter(_JsonLogFormatter())
    else:
        handler.setFormatter(
            _logging.Formatter(
                "time=%(asctime)s level=%(levelname)s logger=%(name)s message=%(message)s"
            )
        )
    root = _logging.getLogger()
    root.handlers.clear()
    root.setLevel(_LOG_LEVELS[normalized])
    root.addHandler(handler)
    _logging.captureWarnings(True)


class _JsonLogFormatter(_logging.Formatter):
    def format(self, record: _logging.LogRecord) -> str:
        payload: dict[str, Any] = {
            "time": self.formatTime(record),
            "level": record.levelname,
            "logger": record.name,
            "message": record.getMessage(),
        }
        if record.exc_info is not None:
            payload["exc_info"] = self.formatException(record.exc_info)
        for key, value in record.__dict__.items():
            if key not in _RESERVED_LOG_RECORD_KEYS:
                payload[key] = _json_safe(value)
        return _json.dumps(payload, sort_keys=True, separators=(",", ":"))


def _json_safe(value: Any) -> Any:
    if value is None or isinstance(value, str | int | float | bool):
        return value
    if isinstance(value, Mapping):
        return {str(key): _json_safe(item) for key, item in value.items()}
    if isinstance(value, Sequence) and not isinstance(value, str | bytes | bytearray):
        return [_json_safe(item) for item in value]
    return str(value)
