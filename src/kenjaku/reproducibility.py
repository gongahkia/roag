from __future__ import annotations

import importlib
import os
import random
import warnings
from hashlib import blake2b

_HASH_SEED_WARNED = False
_UINT32_MOD = 2**32
_TORCH_SEED_MOD = 2**63 - 1


def pin_seeds(seed: int | str, *, cudnn_deterministic: bool = True) -> None:
    seed_int = _seed_to_int(seed)
    random.seed(seed_int)
    _pin_python_hash_seed(seed_int)
    _pin_numpy(seed_int)
    _pin_torch(seed_int, cudnn_deterministic=cudnn_deterministic)


def _seed_to_int(seed: int | str) -> int:
    if isinstance(seed, int):
        return seed
    digest = blake2b(str(seed).encode("utf-8"), digest_size=8).digest()
    return int.from_bytes(digest, "big", signed=False)


def _pin_python_hash_seed(seed: int) -> None:
    global _HASH_SEED_WARNED
    hash_seed = str(seed % _UINT32_MOD)
    if os.environ.get("PYTHONHASHSEED") == hash_seed:
        return

    os.environ["PYTHONHASHSEED"] = hash_seed
    if not _HASH_SEED_WARNED:
        warnings.warn(
            "PYTHONHASHSEED was set after interpreter startup; restart Python for hash "
            "randomization to use this seed",
            RuntimeWarning,
            stacklevel=3,
        )
        _HASH_SEED_WARNED = True


def _pin_numpy(seed: int) -> None:
    try:
        numpy = importlib.import_module("numpy")
    except ImportError:
        return
    numpy.random.seed(seed % _UINT32_MOD)


def _pin_torch(seed: int, *, cudnn_deterministic: bool) -> None:
    try:
        torch = importlib.import_module("torch")
    except ImportError:
        return

    torch_seed = seed % _TORCH_SEED_MOD
    torch.manual_seed(torch_seed)
    if hasattr(torch, "cuda"):
        torch.cuda.manual_seed_all(torch_seed)
    if cudnn_deterministic and hasattr(torch, "backends") and hasattr(torch.backends, "cudnn"):
        torch.backends.cudnn.deterministic = True
        torch.backends.cudnn.benchmark = False
