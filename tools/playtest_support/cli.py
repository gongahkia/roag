import argparse
import json
from pathlib import Path
import subprocess
import sys
from agent_support.workspace import Repository
from . import recordings, driver


class Parser(argparse.ArgumentParser):
    def error(self, message):
        raise ValueError(message)


def main(argv=None):
    command = 'help'
    try:
        parser = Parser(description='Vagabonds local session evidence and mouse playtesting (JSON output).')
        parser.add_argument('group', choices=['sessions', 'playtest'])
        parser.add_argument('command', nargs='?', default='help')
        parser.add_argument('id', nargs='?')
        parser.add_argument('--root', type=Path)
        parser.add_argument('--after', type=int, default=0)
        parser.add_argument('--limit', type=int, default=40)
        parser.add_argument('--kind')
        parser.add_argument('--full', action='store_true')
        parser.add_argument('--json', dest='action_json')
        parser.add_argument('--file', type=Path)
        parser.add_argument('--seed', type=int, default=42)
        parser.add_argument('--scene', choices=driver.SCENES, default='world')
        parser.add_argument('--visible', action='store_true')
        parser.add_argument('--no-build', action='store_true')
        parser.add_argument('--seconds', type=float, default=1)
        parser.add_argument('--message', default='')
        parser.add_argument('--label', default='Marked by developer')
        parser.add_argument('--category', default='usability')
        parser.add_argument('--severity', default='note')
        parser.add_argument('--expected', default='')
        parser.add_argument('--observation', type=int)
        parser.add_argument('--output', type=Path)
        parser.add_argument('--model')
        parser.add_argument('--reasoning', default='low')
        parser.add_argument('--objective')
        parser.add_argument('--max-steps', type=int, default=30)
        parser.add_argument('--timeout', type=int, default=600)
        parser.add_argument('--decision-timeout', type=int, default=60)
        parser.add_argument('--codex-executable', default='codex')
        parser.add_argument('--dry-run', action='store_true')
        parser.add_argument('--keep-open', action='store_true')
        args = parser.parse_args(argv); command = args.group + ' ' + args.command
        repo = Repository(Path(__file__).resolve().parents[2])
        if args.group == 'sessions':
            root = args.root or recordings.DEFAULT_ROOT
            if args.command == 'help':
                result = {'commands': ['sessions list [--root DIR]', 'sessions report ID [--root DIR]', 'sessions events ID [--after N --limit N --kind KIND]'],
                          'defaultRoot': str(root), 'readOnly': True}
            elif args.command == 'list':
                result = {'root': str(root), 'sessions': recordings.sessions(root, min(100, max(1, args.limit)))}
            elif args.command in ['show', 'report'] and args.id:
                result = recordings.report(root, args.id, args.full)
            elif args.command == 'events' and args.id:
                result = recordings.events(root, args.id, args.after, args.limit, args.kind)
            else:
                raise ValueError('Use sessions help or provide a session ID.')
        elif args.command == 'help':
            result = {'commands': ['playtest start [--seed N --scene SCENE --visible --no-build]', 'playtest list',
                'playtest observe ID [--full]', 'playtest act ID --json \'{"kind":"control","control":"panel:inventory"}\'',
                'playtest wait ID --seconds 1', 'playtest capture ID', 'playtest mark ID --label TEXT',
                'playtest feedback ID --message TEXT [--category combat --severity warning]', 'playtest report ID', 'playtest stop ID',
                'playtest suite [--output DIR --no-build]',
                'playtest agent ID --model MODEL --reasoning low --objective TEXT [--max-steps 30 --timeout 600 --dry-run]'],
                'actionKinds': driver.KINDS, 'scope': 'Isolated save; real Client mouse-state pipeline; held between decisions; explicit waits advance time.',
                'limits': {'actions': driver.MAX_ACTIONS, 'retainedStepEvidence': driver.RETAINED_STEPS, 'idleExitMinutes': 10},
                'guide': 'docs/SESSION_PLAYTESTING.md'}
        elif args.command == 'start':
            result = driver.start(repo, args.seed, args.scene, args.visible, args.no_build, args.root)
        elif args.command == 'list':
            root = args.root or repo.root / 'artifacts/playtests'
            found = []
            if root.exists():
                for path in sorted(root.glob('*/playtest.json'), reverse=True)[:40]:
                    state = recordings.read(path); found.append({k: state.get(k) for k in ['id', 'status', 'scene', 'seed', 'steps', 'sessionId']})
            result = {'playtests': found}
        elif args.command == 'suite':
            from .suite import run
            result = run(repo, args.output, args.no_build)
        elif not args.id:
            raise ValueError('Provide a playtest ID; use playtest start first.')
        elif args.command == 'observe':
            result = driver.observe(repo, args.id, args.root, args.full)
        elif args.command == 'act':
            if bool(args.action_json) == bool(args.file):
                raise ValueError('Provide exactly one of --json JSON or --file FILE.')
            action = json.loads(args.action_json) if args.action_json else recordings.read(args.file, limit=16384)
            result = driver.act(repo, args.id, action, args.root, args.observation)
        elif args.command in ['wait', 'capture', 'mark']:
            action = {'kind': args.command}
            if args.command == 'wait':
                action['seconds'] = args.seconds
            if args.command == 'mark':
                action['label'] = args.label
            result = driver.act(repo, args.id, action, args.root)
        elif args.command == 'feedback':
            result = driver.feedback(repo, args.id, args.message, args.category, args.severity, args.root, args.expected)
        elif args.command == 'report':
            result = driver.report(repo, args.id, args.root)
        elif args.command == 'stop':
            result = driver.stop(repo, args.id, args.root)
        elif args.command == 'agent':
            from .model import run
            if not args.model or not args.objective:
                raise ValueError('Agent playtests require explicit --model and --objective. No model is invoked by normal game launches.')
            result = run(repo, args.id, args.model, args.objective, args.reasoning, args.max_steps, args.timeout,
                         args.decision_timeout, args.root, args.codex_executable, args.keep_open, args.dry_run)
        else:
            raise ValueError('Unknown command; use playtest help.')
        ok = result.get('passed', True) and result.get('status') not in ['model-error', 'client-exited', 'interrupted']
        print(json.dumps({'schemaVersion': 1, 'ok': ok, 'command': command, 'result': result}, indent=2, ensure_ascii=False))
        return 0 if ok else 1
    except (ValueError, OSError, TimeoutError, KeyError, TypeError, subprocess.TimeoutExpired) as e:
        print(json.dumps({'schemaVersion': 1, 'ok': False, 'command': command, 'error': str(e)}))
        return 1
