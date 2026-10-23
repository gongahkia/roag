"""Player impressions with evidence, kept separate from deterministic assertions."""
from agent_support.workspace import atomic_json
from .recordings import read

TOPICS = ['orientation', 'navigation', 'interaction', 'world', 'combat', 'pacing', 'presentation']
COMMENT_FIELDS = ['topic', 'impression', 'observed', 'thought', 'expected', 'suggestion', 'confidence']
COMMENT_SCHEMA = {'type': 'object', 'additionalProperties': False, 'required': COMMENT_FIELDS, 'properties': {
    'topic': {'type': 'string', 'enum': TOPICS},
    'impression': {'type': 'string', 'enum': ['positive', 'negative', 'mixed', 'neutral']},
    **{k: {'type': 'string'} for k in ['observed', 'thought', 'expected', 'suggestion']},
    'confidence': {'type': 'string', 'enum': ['low', 'medium', 'high']}}}
PLAN_SCHEMA = {'type': 'object', 'additionalProperties': False, 'required': ['goal', 'targetId', 'nextStep', 'remember'],
               'properties': {**{k: {'type': 'string'} for k in ['goal', 'nextStep', 'remember']},
                              'targetId': {'type': ['string', 'null']}}}
DEBRIEF_FIELDS = ['summary', 'understanding', 'worldImpression', 'friction', 'nextAttempt', 'wouldContinue']
DEBRIEF_SCHEMA = {'type': 'object', 'additionalProperties': False, 'required': DEBRIEF_FIELDS,
                  'properties': {**{k: {'type': 'string'} for k in DEBRIEF_FIELDS[:-1]},
                                 'wouldContinue': {'type': 'string', 'enum': ['yes', 'no', 'unsure']}}}


def validate_comment(comment):
    if not isinstance(comment, dict) or set(comment) != set(COMMENT_FIELDS):
        raise ValueError('Invalid playtester comment.')
    if comment['topic'] not in TOPICS or comment['impression'] not in ['positive', 'negative', 'mixed', 'neutral'] or comment['confidence'] not in ['low', 'medium', 'high']:
        raise ValueError('Unknown playtester topic, impression or confidence.')
    if any(not isinstance(comment[k], str) or len(comment[k]) > 1500 for k in ('observed', 'thought', 'expected', 'suggestion')):
        raise ValueError('Playtester text must be bounded.')
    if not comment['observed'].strip() or not comment['thought'].strip():
        raise ValueError('A playtester comment needs an observation and an interpretation.')


def validate_debrief(value):
    if value is None:
        return
    if not isinstance(value, dict) or set(value) != set(DEBRIEF_FIELDS) or any(not isinstance(v, str) or len(v) > 1800 for v in value.values()):
        raise ValueError('Invalid or oversized playtester debrief.')
    if value['wouldContinue'] not in ['yes', 'no', 'unsure']:
        raise ValueError('Invalid willingness to continue.')


def add(repo, run_id, comments, root=None):
    from . import driver
    path, _ = driver.location(repo, run_id, root)
    for comment in comments:
        validate_comment(comment)
        previous = read(path / 'feedback.json', [])
        if any(e.get('thought') == comment['thought'] and e.get('observed') == comment['observed'] for e in previous):
            continue
        category = comment['topic'] if comment['topic'] in ['navigation', 'combat'] else 'usability'
        note = driver.feedback(repo, run_id, comment['thought'], category, 'note', root, comment['expected'])
        entries = read(path / 'feedback.json', [])
        entries[-1].update(comment, kind='playtester-impression',
                           observationMeaning='Observed describes evidence; thought and suggestion are model opinions.')
        atomic_json(path / 'feedback.json', entries)


def review(path):
    notes = read(path / 'feedback.json', [])
    state = read(path / 'player-memory.json', {})
    report = read(path / 'model-report.json', {})
    for note in notes:
        snapshot = read(path / note['evidence']['snapshot'], {}) if note.get('evidence') else {}
        note['evidenceRetained'] = snapshot.get('number') == note.get('number')
    return {'id': state.get('id', report.get('id')), 'status': report.get('status', 'not-started'),
            'objective': state.get('objective'), 'assistedJourney': report.get('journey', False),
            'checkpoints': state.get('milestones', {}), 'plan': state.get('plan'),
            'comments': [n for n in notes if n.get('kind') == 'playtester-impression'],
            'debrief': read(path / 'playtester-debrief.json'), 'notebook': str(path / 'playtester-notes.md'),
            'coverage': report.get('coverage'), 'limitations': [
                'Model impressions do not replace a human opinion about fun, aesthetics or accessibility.',
                'Observed-ground assistance is labelled; it does not establish unaided wayfinding.',
                'Simulation is held while the model thinks; elapsed testing time is not continuous human play.',
                'Screenshot evidence can rotate; evidenceRetained identifies what is still available.']}


def write(path):
    data = review(path)
    lines = ['# Playtester notebook', '', '**Status:** ' + data['status'], '', 'Objective: ' + (data['objective'] or ''), '',
             'Mode: ' + ('focused journey with observed-ground walking suggestions' if data['assistedJourney'] else 'player-driven exploration'), '',
             'These are a model playtester’s impressions. They are not deterministic test assertions or proof of human enjoyment.', '',
             '## What was actually reached', '']
    lines += ['- ' + k + ' (observation ' + str(v['observation']) + ')' for k, v in data['checkpoints'].items()]
    lines += ['', '## Comments during play', '']
    if not data['comments']:
        lines += ['No experiential comments were produced; feedback quality is unassessed.', '']
    for note in data['comments']:
        lines += ['### ' + note['topic'] + ' / ' + note['impression'], '', note['thought'], '',
                  '**Observed:** ' + note['observed'], '', '**Expected:** ' + (note['expected'] or 'No specific expectation stated.'), '']
        if note['suggestion']:
            lines += ['**Suggestion:** ' + note['suggestion'], '']
        lines += ['Confidence: ' + note['confidence'] + '. Step ' + str(note['step']) + ', observation ' + str(note['observation']) + '.', '']
        evidence = note.get('evidence', {})
        if note['evidenceRetained']:
            lines += ['[Observation](' + evidence['snapshot'] + ') · [Screenshot](' + evidence['screenshot'] + ')', '']
        else:
            lines += ['Evidence rotated out of the bounded screenshot history.', '']
    lines += ['## End-of-run reflection', '']
    debrief = data['debrief']
    if debrief:
        for key, label in [('summary', 'Overall'), ('understanding', 'What I understood'), ('worldImpression', 'The world'),
                           ('friction', 'What got in the way'), ('nextAttempt', 'What I would try next'), ('wouldContinue', 'Would I continue')]:
            lines += ['**' + label + ':** ' + debrief['assessment'][key], '']
        lines += ['This reflection is a model opinion based on the retained play history, not an assertion of completed objectives.', '']
    else:
        lines += ['No final reflection was returned. Use the recorded comments; do not infer a positive assessment.', '']
    lines += ['## Coverage and limits', '', str(data.get('coverage') or 'Coverage has not been assessed.'), '']
    lines += ['- ' + s for s in data['limitations']]
    (path / 'playtester-notes.md').write_text('\n'.join(lines) + '\n')
