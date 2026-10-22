"""Explicit requirements and evidence-based handoff; never invents or advances a user goal."""
from contextlib import contextmanager
from pathlib import Path
from .workspace import atomic_json, identifier, read_json, utc
from .discovery import PROFILES
from .runner import observe, run_dir

@contextmanager
def locked(repo, task_id):
    import fcntl
    folder = repo.artifacts/'tasks'/identifier(task_id); folder.mkdir(parents=True, exist_ok=True)
    with (folder/'task.lock').open('a') as stream:
        fcntl.flock(stream, fcntl.LOCK_EX); yield folder/'task.json'

def create(repo, spec_path):
    spec = read_json(spec_path); identifier(spec['id'])
    if spec.get('schemaVersion') != 1 or not isinstance(spec.get('objective'), str) or not spec['objective'].strip():
        raise ValueError('Task needs schemaVersion 1, id, objective and explicit requirements.')
    reqs = spec.get('requirements')
    if not isinstance(reqs, list) or not 1 <= len(reqs) <= 100: raise ValueError('Provide 1–100 requirements.')
    seen = set()
    for req in reqs:
        rid = identifier(req['id'])
        if rid in seen or not req.get('description') or not req.get('profiles'): raise ValueError('Each requirement needs a distinct id, description and verification profiles.')
        if any(p not in PROFILES for p in req['profiles']): raise ValueError('Unknown verification profile.')
        if not isinstance(req.get('manualReview', False), bool): raise ValueError('manualReview must be boolean.')
        seen.add(rid)
    state = {'schemaVersion': 1, 'id': spec['id'], 'objective': spec['objective'], 'requirements': reqs,
             'created': utc(), 'status': 'active', 'notes': [], 'evidence': [], 'reviews': []}
    with locked(repo, spec['id']) as path:
        if path.exists(): raise ValueError('Task already exists; show it and checkpoint rather than replacing its requirements.')
        atomic_json(path, state)
    return state

def load(repo, task_id): return read_json(repo.artifacts/'tasks'/identifier(task_id)/'task.json')

def tasks(repo):
    result=[]
    for path in sorted((repo.artifacts/'tasks').glob('*/task.json')):
        state = read_json(path); result.append({k:state[k] for k in ['id','objective','status']})
    return result

def requirement(state, rid):
    req = next((r for r in state['requirements'] if r['id'] == rid), None)
    if req is None: raise ValueError('Unknown task requirement: ' + rid)
    return req

def valid_run(repo, run_id):
    run = observe(repo, run_id, verify_artifacts=True)
    if run['status'] != 'passed' or not run.get('fresh') or run['artifactErrors']:
        raise ValueError(f"Run {run_id} is not usable evidence: status={run['status']}, fresh={run.get('fresh')}, artifactErrors={run['artifactErrors']}.")
    if not run['results'] or any(r['status'] != 'passed' or r['exitCode'] != 0 or not r['evidence']['valid'] for r in run['results']):
        raise ValueError('Run has no complete successful step evidence.')
    return run

def checkpoint(repo, task_id, note, next_action):
    if not note.strip(): raise ValueError('Describe the concrete result or decision.')
    with locked(repo, task_id) as path:
        state=read_json(path); state['notes'].append({'at':utc(),'note':note,'next':next_action,
            'head':repo.git('rev-parse','HEAD').decode().strip(),'source':repo.fingerprint()['sha256']})
        atomic_json(path,state)
    return state['notes'][-1]

def attach(repo, task_id, rid, run_id, note, artifact=None):
    if not note.strip(): raise ValueError('Explain what this evidence establishes for the requirement.')
    run=valid_run(repo,run_id)
    with locked(repo, task_id) as path:
        state=read_json(path); requirement(state,rid)
        entry={'requirement':rid,'run':run_id,'note':note,'at':utc()}
        if artifact:
            item=next((a for a in run['artifacts'] if a['path']==artifact),None)
            if item is None: raise ValueError('Review an artifact recorded by the run, using its exact relative path.')
            entry.update(artifact=artifact,sha256=item['sha256']); state['reviews'].append(entry)
        else: state['evidence'].append(entry)
        state['status']='active'; atomic_json(path,state)
    return entry

def assess(repo, task_id):
    state=load(repo,task_id); checked={}; results=[]
    def check(run_id):
        if run_id not in checked:
            try: checked[run_id]=valid_run(repo,run_id)
            except (ValueError,OSError,KeyError) as e: checked[run_id]={'error':str(e)}
        return checked[run_id]
    for req in state['requirements']:
        covered=set(); reasons=[]
        for evidence in state['evidence']:
            if evidence['requirement']!=req['id']: continue
            run=check(evidence['run'])
            if 'error' in run: reasons.append(run['error'])
            else: covered.update(PROFILES if run['profile']=='full' else [run['profile']])
        missing=sorted(set(req['profiles'])-covered)
        reviewed=not req.get('manualReview',False) or any(r['requirement']==req['id'] and 'error' not in check(r['run']) for r in state['reviews'])
        results.append({'id':req['id'],'description':req['description'],'satisfied':not missing and reviewed,
            'missingProfiles':missing,'manualReviewSatisfied':reviewed,'evidenceProblems':reasons})
    ready=all(r['satisfied'] for r in results)
    return {'id':task_id,'objective':state['objective'],'recordedStatus':state['status'],
            'status':state['status'] if ready or state['status']!='complete' else 'needs-verification',
            'readyToComplete':ready,'requirements':results,'recentNotes':state['notes'][-5:],
            'note':'Evidence checks freshness and requested profiles. The requirement-to-check mapping and review notes still require engineering judgment.'}

def complete(repo, task_id):
    with locked(repo,task_id) as path:
        result=assess(repo,task_id)
        if not result['readyToComplete']: return result
        state=read_json(path); state.update(status='complete',completed=utc(),completionSource=repo.fingerprint()['sha256']); atomic_json(path,state)
    return assess(repo,task_id)
