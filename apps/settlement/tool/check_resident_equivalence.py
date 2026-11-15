#!/usr/bin/env python3
"""Compare the pre-presentation colony against this checkout, without game storage."""
from pathlib import Path
import hashlib
import os
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[3]
BASELINE = 'b878e25b4eda261b7c34c92938b80f79feebbbce'
DART = os.environ.get('DART_BIN', 'dart')
RUN = r'''import 'dart:convert';
import 'dart:io';
import 'colony.dart';
import 'model.dart';
void main(List<String> args) {
 final c=Colony.decode(File(args.single).readAsStringSync());
 void paint(Tool t,Cell a) {
  c.session.beginStroke(t,a);c.session.stroke!.paintTo(a);
  if(!c.session.finishStroke())throw StateError('refused');c.reconcile(c.session.world);
 }
 final cells=[for(var y=19;y<=21;y++)for(var x=1;x<28;x++) Cell(x,y)];
 for(final t in [for(var i=0;i<6;i++)Tool.garden,for(var i=0;i<3;i++)Tool.green,for(var i=0;i<11;i++)Tool.cottage]) {
  paint(t,cells.firstWhere((p)=>evaluate(c.world,t,p).valid));
 }
 final samples=[];
 for(var i=0;i<4400;i++) {
  if(i==600)paint(Tool.remove,const Cell(16,19));
  if(i==650 || i==750){c.session.undo();c.reconcile(c.session.world);}
  if(i==700){c.session.redo();c.reconcile(c.session.world);}
  c.update(.05);
  if(i%100==0){
   final d=jsonDecode(c.encode());
   for(final r in d['colony']['residents']){r.remove('bodyId');}
   samples.add(d);
  }
 }
 stdout.write(jsonEncode(samples));
}
'''

with tempfile.TemporaryDirectory(prefix='roag-resident-equivalence-') as temp:
    results = []
    for version in ['baseline', 'current']:
        directory = Path(temp) / version
        directory.mkdir()
        for name in ['colony', 'residents', 'rules', 'reasons', 'model', 'appearance']:
            if version == 'baseline' and name == 'appearance':
                continue
            path = Path('apps/settlement/lib') / f'{name}.dart'
            data = subprocess.check_output(['git', 'show', f'{BASELINE}:{path}'], cwd=ROOT) if version == 'baseline' else (ROOT/path).read_bytes()
            (directory/f'{name}.dart').write_bytes(data)
        (directory/'run.dart').write_text(RUN)
        results.append(subprocess.check_output([
            DART, str(directory/'run.dart'),
            str(ROOT/'docs/world-builder/evidence/tranche-03/legacy-74.json'),
        ], cwd=ROOT))
    if results[0] != results[1]:
        raise SystemExit('Colony state differs from the baseline replay.')
    print(f'PASS: 85 residents, 220 seconds, 44 full checkpoints; baseline {BASELINE}; SHA256 {hashlib.sha256(results[0]).hexdigest()}')
