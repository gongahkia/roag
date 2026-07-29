from __future__ import annotations

import argparse
import subprocess
from collections.abc import Callable, Sequence

REQUIRED_PLATFORMS = ("linux/amd64", "linux/arm64")
DEFAULT_TIMEOUT_SECONDS = 120.0


Runner = Callable[[Sequence[str]], subprocess.CompletedProcess[str]]


def _run_command(
    command: Sequence[str],
    *,
    timeout_seconds: float = DEFAULT_TIMEOUT_SECONDS,
) -> subprocess.CompletedProcess[str]:
    try:
        return subprocess.run(
            list(command),
            check=False,
            capture_output=True,
            text=True,
            timeout=timeout_seconds,
        )
    except subprocess.TimeoutExpired as error:
        return subprocess.CompletedProcess(
            list(command),
            124,
            stdout=str(error.stdout or ""),
            stderr=f"timed out after {timeout_seconds:g}s",
        )


def validate_image(image: str, version: str, *, runner: Runner = _run_command) -> list[str]:
    version = version[1:] if version.startswith("v") else version
    version_ref = f"{image}:{version}"
    latest_ref = f"{image}:latest"
    errors: list[str] = []

    manifest = runner(("docker", "buildx", "imagetools", "inspect", version_ref))
    if manifest.returncode != 0:
        errors.append(_command_error("inspect", version_ref, manifest))
    else:
        manifest_text = f"{manifest.stdout}\n{manifest.stderr}"
        for platform in REQUIRED_PLATFORMS:
            if platform not in manifest_text:
                errors.append(f"{version_ref} manifest missing {platform}")
    if errors:
        return errors

    for ref in (version_ref, latest_ref):
        for platform in REQUIRED_PLATFORMS:
            pull = runner(("docker", "pull", "--platform", platform, ref))
            if pull.returncode != 0:
                errors.append(_command_error("pull", f"{ref} ({platform})", pull))
                continue
            run = runner(("docker", "run", "--rm", "--platform", platform, ref))
            if run.returncode != 0:
                errors.append(_command_error("run", f"{ref} ({platform})", run))
                continue
            expected = f"kenjaku {version}"
            actual = run.stdout.strip()
            if actual != expected:
                errors.append(
                    f"{ref} ({platform}) printed {actual!r}; expected {expected!r}"
                )

    return errors


def main(argv: Sequence[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Validate a published Kenjaku GHCR image.")
    parser.add_argument("version", help="image version; v-prefix is accepted")
    parser.add_argument("--image", default="ghcr.io/gongahkia/kenjaku", help="image repository")
    parser.add_argument(
        "--timeout-seconds",
        default=DEFAULT_TIMEOUT_SECONDS,
        type=float,
        help="per-docker-command timeout",
    )
    args = parser.parse_args(argv)

    def run(command: Sequence[str]) -> subprocess.CompletedProcess[str]:
        return _run_command(command, timeout_seconds=args.timeout_seconds)

    errors = validate_image(args.image, args.version, runner=run)
    if errors:
        for error in errors:
            print(error)
        return 1
    version = args.version[1:] if args.version.startswith("v") else args.version
    print(f"ghcr image ok: {args.image}:{version}")
    return 0


def _command_error(action: str, ref: str, result: subprocess.CompletedProcess[str]) -> str:
    detail = (result.stderr or result.stdout).strip()
    return f"docker {action} failed for {ref}: {detail}"


if __name__ == "__main__":
    raise SystemExit(main())
