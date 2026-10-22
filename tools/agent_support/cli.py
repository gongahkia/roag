import argparse
import json
from pathlib import Path
import sys
import time
import subprocess
from .workspace import Repository, SCHEMA
from .discovery import capabilities, impact, PROFILES
from . import runner, tasks

class Parser(argparse.ArgumentParser):
    def error(self, message): raise ValueError(message)

def main(argv=None):
    command = 'help'
    try:
        parser = Parser(description='Vagabonds agent development workflow (JSON output).')
        subs = parser.add_subparsers(dest='command')
        subs.add_parser('help'); subs.add_parser('doctor')
        p = subs.add_parser('context'); p.add_argument('--task')
        p = subs.add_parser('verify'); p.add_argument('--profile', choices=PROFILES, default='quick'); p.add_argument('--detach', action='store_true'); p.add_argument('--task'); p.add_argument('--scenes', nargs='+', choices=runner.NATIVE_SCENES)
        p = subs.add_parser('runs'); p.add_argument('action', choices=['list','show','tail','cancel','retry']); p.add_argument('id', nargs='?'); p.add_argument('--step'); p.add_argument('--lines',type=int,default=40)
        p = subs.add_parser('task'); p.add_argument('action', choices=['create','list','show','checkpoint','evidence','review','check','complete']); p.add_argument('--id'); p.add_argument('--spec'); p.add_argument('--requirement'); p.add_argument('--run'); p.add_argument('--note'); p.add_argument('--next', default=''); p.add_argument('--artifact')
        p = subs.add_parser('impact'); p.add_argument('paths', nargs='*'); p.add_argument('--base')
        args = parser.parse_args(argv); command = args.command or 'help'
        repo = Repository(Path(__file__).resolve().parents[2])
        if command == 'help': result = capabilities()
        elif command == 'doctor': result = repo.doctor()
        elif command == 'context':
            result = repo.context(); result['runs'] = runner.list_runs(repo)[:10]; result['tasks'] = tasks.tasks(repo)
            if args.task: result['task'] = tasks.assess(repo,args.task)
        elif command == 'verify':
            if args.task: tasks.load(repo,args.task)
            run_id, reused = runner.launch(repo,args.profile,args.task,args.scenes)
            if args.detach: result = {'id':run_id,'reconnected':reused,'inspect':'./dev agent runs show '+run_id}
            else:
                while True:
                    result = runner.observe(repo,run_id)
                    if result['status'] in runner.TERMINAL: break
                    time.sleep(.5)
                result['ready'] = result['status']=='passed' and result.get('fresh',False)
        elif command == 'runs':
            if args.action == 'list': result = {'runs':runner.list_runs(repo)}
            else:
                if not args.id: raise ValueError('Provide the run ID.')
                if args.action == 'show': result = runner.observe(repo,args.id,verify_artifacts=True)
                elif args.action == 'tail': result = runner.tail(repo,args.id,args.step,args.lines)
                elif args.action == 'cancel': result = runner.cancel(repo,args.id)
                else:
                    old=runner.observe(repo,args.id)
                    if old['status'] not in runner.TERMINAL: raise ValueError('This run is live; inspect it instead of starting a duplicate.')
                    run_id,reused=runner.launch(repo,old['profile'],old.get('task'),old.get('scenes')); result={'id':run_id,'retryOf':args.id,'reconnected':reused}
        elif command == 'task':
            if args.action == 'create':
                if not args.spec: raise ValueError('Provide --spec FILE with explicit requirements.')
                result=tasks.create(repo,args.spec)
            elif args.action == 'list': result={'tasks':tasks.tasks(repo)}
            else:
                if not args.id: raise ValueError('Provide --id TASK.')
                if args.action in ['show','check']:
                    result=tasks.assess(repo,args.id)
                    if args.action=='check': result['ready']=result['readyToComplete']
                elif args.action == 'checkpoint': result=tasks.checkpoint(repo,args.id,args.note or '',args.next)
                elif args.action == 'complete': result=tasks.complete(repo,args.id); result['ready']=result['readyToComplete']
                else:
                    if not args.requirement or not args.run: raise ValueError('Provide --requirement and --run.')
                    if args.action=='review' and not args.artifact: raise ValueError('Provide --artifact RUN_RELATIVE_PATH and a review note.')
                    result=tasks.attach(repo,args.id,args.requirement,args.run,args.note or '',args.artifact if args.action=='review' else None)
        else: result = impact(repo, args.paths, args.base)
        ok = result.get('ready', True)
        print(json.dumps({'schemaVersion': SCHEMA, 'ok': ok, 'command': command, 'result': result}, indent=2))
        return 0 if ok else 1
    except (ValueError, OSError, TimeoutError, KeyError, TypeError, subprocess.TimeoutExpired) as e:
        print(json.dumps({'schemaVersion': SCHEMA, 'ok': False, 'command': command, 'error': str(e)}))
        return 1
