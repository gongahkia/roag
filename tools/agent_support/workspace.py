"""Repository evidence and concise context. Never reads player saves or environment secrets."""
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile
from datetime import datetime, timezone

SCHEMA = 1

def utc():
    return datetime.now(timezone.utc).isoformat()

def digest(path):
    h = hashlib.sha256()
    with Path(path).open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()

def atomic_json(path, value):
    path = Path(path); path.parent.mkdir(parents=True, exist_ok=True)
    fd, temporary = tempfile.mkstemp(prefix=path.name + '.', suffix='.tmp', dir=path.parent)
    try:
        with os.fdopen(fd, 'w') as stream:
            json.dump(value, stream, indent=2, ensure_ascii=False); stream.write('\n')
            stream.flush(); os.fsync(stream.fileno())
        os.replace(temporary, path)
    finally:
        if os.path.exists(temporary): os.unlink(temporary)

def read_json(path):
    return json.loads(Path(path).read_text())

def identifier(value):
    if not re.fullmatch(r'[a-z0-9][a-z0-9_-]{0,79}', value):
        raise ValueError('Use a lowercase identifier of 1–80 letters, digits, hyphens or underscores.')
    return value

class Repository:
    def __init__(self, root):
        self.root = Path(root).resolve()
        self.artifacts = self.root / 'artifacts/agent'

    def git(self, *args):
        p = subprocess.run(['git', '-C', str(self.root), *args], capture_output=True, timeout=30)
        if p.returncode: raise ValueError(p.stderr.decode(errors='replace').strip())
        return p.stdout

    def files(self):
        return sorted(set(p.decode('utf-8', errors='surrogateescape') for p in
            self.git('ls-files', '-z', '--cached', '--others', '--exclude-standard').split(b'\0') if p))

    def fingerprint(self):
        # Documentation edits and commits do not invalidate runtime test evidence.
        # Source, content, tests, tools, scripts, assets and build/CI config do.
        h = hashlib.sha256(); count = 0
        for name in self.files():
            if name.endswith('.md') or name.startswith('docs/'):
                continue
            p = self.root / name
            if p.is_symlink(): value = 'link:' + os.readlink(p)
            elif p.is_file(): value = f'{p.stat().st_mode & 0o111}:' + digest(p)
            else: value = 'deleted'
            h.update(os.fsencode(name) + b'\0' + value.encode() + b'\0'); count += 1
        return {'sha256': h.hexdigest(), 'files': count,
                'scope': 'Git tracked and nonignored untracked source/config/assets/tests; excludes Markdown and docs/.'}

    def changed(self, base=None):
        names = set()
        if base:
            # --end-of-options prevents revision strings from becoming Git flags.
            revision = self.git('rev-parse', '--verify', '--end-of-options', base + '^{commit}').decode().strip()
            names.update(os.fsdecode(p) for p in self.git('diff', '--name-only', '-z', revision, '--').split(b'\0') if p)
        else:
            names.update(os.fsdecode(p) for p in self.git('diff', '--name-only', '-z', 'HEAD', '--').split(b'\0') if p)
        names.update(os.fsdecode(p) for p in self.git('ls-files', '-z', '--others', '--exclude-standard').split(b'\0') if p)
        return sorted(names)

    def context(self):
        changes = self.changed()
        return {'root': str(self.root), 'head': self.git('rev-parse', 'HEAD').decode().strip(),
                'branch': self.git('branch', '--show-current').decode().strip(), 'fingerprint': self.fingerprint(),
                'changes': changes[:60], 'changeCount': len(changes), 'changesTruncated': len(changes) > 60,
                'readFirst': ['docs/DEVELOPMENT.md', 'docs/PROJECT_STATE.md', 'docs/AGENT_TOOLING.md'],
                'invariants': ['Mouse-first party RPG; deterministic Core, presentation in Client.',
                    'Keep save rejection, dungeon privacy, collisions and exact continuation.',
                    'Use temporary fixtures; never overwrite player saves during diagnostics.',
                    'Preserve source artwork and unrelated changes; completion needs requirement review.']}

    def doctor(self):
        wrapper = self.root / 'tools/dotnet.sh'
        sdk = subprocess.run([str(wrapper), '--version'], cwd=self.root, capture_output=True, text=True, timeout=30)
        optional = {name: shutil.which(name) for name in ['xdotool', 'ffmpeg', 'xvfb-run']}
        display = bool(os.environ.get('DISPLAY'))
        native = os.name == 'posix' and all(optional.values())
        return {'python': sys.version.split()[0], 'dotnet': sdk.stdout.strip() if sdk.returncode == 0 else None,
                'dotnetError': sdk.stderr.strip()[-1500:] if sdk.returncode else None,
                'git': shutil.which('git'), 'platform': sys.platform, 'managedRunsAvailable': sys.platform.startswith('linux'), 'native': {'available': native,
                    'displayPresent': display, 'tools': optional,
                    'reason': '' if native else 'Native checks require POSIX, xdotool, ffmpeg and xvfb-run.'},
                'agentInstructions': 'available' if (self.root / 'AGENTS.md').is_file() else 'Use tracked docs/DEVELOPMENT.md; local AGENTS.md is absent or a broken symlink.',
                'ready': sdk.returncode == 0, 'installsPerformed': False}
