from fnmatch import fnmatchcase
from pathlib import Path
from .workspace import read_json

PROFILES = ['quick', 'tools', 'core', 'assets', 'world', 'native', 'playtest', 'full']

def impact(repo, paths=None, base=None):
    paths = paths or repo.changed(base)
    areas = read_json(repo.root / 'devtools/areas.json')
    matched = {}; unknown = []
    for name in paths:
        hits = [area for area, rule in areas.items() if any(fnmatchcase(name, p) for p in rule['paths'])]
        if not hits: unknown.append(name)
        for area in hits: matched.setdefault(area, []).append(name)
    profiles = sorted({p for area in matched for p in areas[area]['profiles']})
    if unknown: profiles = ['full']
    return {'paths': paths, 'areas': matched, 'unmapped': unknown, 'recommendedProfiles': profiles,
            'docs': sorted({p for area in matched for p in areas[area]['docs']}),
            'nativeScenes': sorted({p for area in matched for p in areas[area]['scenes']}),
            'note': 'Conservative guidance, not proof of sufficient coverage. Unknown paths recommend full verification; review the actual behavior changed.'}

def capabilities():
    return {'entryPoint': './dev agent', 'profiles': PROFILES,
            'commands': {'doctor': 'Report SDK/native prerequisites without installing anything.',
                'context': 'Git/source identity, invariants and task/run handoff.',
                'impact': 'Conservative checks/docs/scenes for changed files or --base revision.',
                'verify': 'Run a verification profile; --detach returns a durable run ID.',
                'runs': 'List, inspect, tail, cancel or retry a verification run.',
                'task': 'Create explicit requirements, checkpoint, attach evidence and check completion.'},
            'gameTools': ['./dev content inspect ID', './dev content references ID', './dev preview KIND ID',
                './dev audit-world --seed 42 --samples 4', './dev audit-expedition --seeds 42,1337,-42,2026',
                './dev audit-encounters --seeds 42,1337,-42,2026', './dev audit-counterplay --seed 42', './dev content patch ID --patch FILE',
                './dev content impact ID', './dev content diff --against DIRECTORY',
                './dev scenario run FILE', './dev scenario suite devtools/scenarios',
                './dev diagnose save FILE', './dev diagnose diff BEFORE AFTER', './dev sessions list',
                './dev sessions report SESSION_ID', './dev playtest help', './dev playtest suite'],
            'protocol': 'Versioned JSON on stdout; nonzero for errors, failed/blocked/stale verification and incomplete completion checks.'}
