import json
import os
from pathlib import Path
import signal
import subprocess
import sys
import tempfile
import time
import unittest

ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'tools'))
from agent_support.workspace import Repository, atomic_json, read_json
from agent_support import runner, tasks

@unittest.skipUnless(sys.platform.startswith('linux'), 'Managed jobs use Linux/WSL process identities.')
class RunTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory(); self.root=Path(self.temp.name); self.repo=Repository(self.root); self.ids=[]
        subprocess.run(['git','init','-q',str(self.root)],check=True)
        (self.root/'.gitignore').write_text('artifacts/\n'); (self.root/'code.py').write_text('original')
        subprocess.run(['git','-C',str(self.root),'add','.'],check=True)
        subprocess.run(['git','-C',str(self.root),'-c','user.name=Fixture','-c','user.email=fixture@example.test','commit','-qm','fixture'],check=True)
    def tearDown(self):
        for rid in self.ids:
            runner.cancel(self.repo,rid)
            end=time.monotonic()+5
            while runner.alive(read_json(runner.run_dir(self.repo,rid)/'run.json').get('worker')) and time.monotonic()<end: time.sleep(.05)
        self.temp.cleanup()
    def launch(self,script="print('verified')",timeout=10,task=None):
        rid,_=runner.launch(self.repo,'core',task=task,steps=[{'name':'fixture','argv':[sys.executable,'-c',script],'timeout':timeout,'evidence':None}]); self.ids.append(rid); return rid
    def wait(self,rid,predicate=lambda s:s['status'] in runner.TERMINAL):
        end=time.monotonic()+15
        while time.monotonic()<end:
            state=runner.observe(self.repo,rid)
            if predicate(state): return state
            time.sleep(.05)
        self.fail('Timed out waiting for '+rid)
    def test_detached_success_has_logs_identity_and_checksum_evidence(self):
        rid=self.launch(); self.assertEqual('passed',self.wait(rid)['status'])
        state=runner.observe(self.repo,rid,True); self.assertTrue(state['fresh']); self.assertFalse(state['artifactErrors'])
        self.assertIn('verified',runner.tail(self.repo,rid)['text'])
        (runner.run_dir(self.repo,rid)/'fixture.log').write_text('tampered')
        self.assertEqual(['fixture.log'],runner.observe(self.repo,rid,True)['artifactErrors'])
    def test_failed_and_timed_out_processes_never_pass(self):
        rid=self.launch("print('intentional failure'); raise SystemExit(7)")
        self.assertEqual('failed',self.wait(rid)['status']); self.assertEqual(7,runner.observe(self.repo,rid)['results'][0]['exitCode'])
        rid=self.launch('import time; time.sleep(30)',timeout=.3)
        state=self.wait(rid); self.assertEqual('timed_out',state['status']); self.assertIsNone(state['child'])
    def test_cancel_and_reconnect_do_not_launch_duplicate_work(self):
        rid=self.launch('import time; time.sleep(30)',task='fixture-task')
        self.wait(rid,lambda s:s['status']=='running')
        same,reused=runner.launch(self.repo,'core',task='fixture-task')
        self.assertTrue(reused); self.assertEqual(rid,same)
        self.assertTrue(runner.cancel(self.repo,rid)['requested']); self.assertEqual('cancelled',self.wait(rid)['status'])
    def test_edits_during_a_run_mark_its_results_stale(self):
        rid=self.launch('import time; time.sleep(.6); print("done")')
        self.wait(rid,lambda s:s['status']=='running'); (self.root/'code.py').write_text('changed')
        self.assertEqual('stale',self.wait(rid)['status'])
    def test_missing_worker_is_interrupted_not_success(self):
        rid=self.launch(); self.wait(rid); path=runner.run_dir(self.repo,rid)/'run.json'; state=read_json(path)
        state.update(status='running',worker={'pid':999999999,'start':'bad','boot':'bad'},createdEpoch=0); atomic_json(path,state)
        self.assertEqual('interrupted',runner.observe(self.repo,rid)['status'])
    def test_task_completion_requires_fresh_intact_success_and_explained_coverage(self):
        spec=self.root/'task.json'; atomic_json(spec,{'schemaVersion':1,'id':'fixture','objective':'Verify real local workflow','requirements':[{'id':'behavior','description':'Check the behavior','profiles':['core']}]})
        tasks.create(self.repo,spec); self.assertFalse(tasks.complete(self.repo,'fixture')['readyToComplete'])
        rid=self.launch(); self.wait(rid)
        tasks.attach(self.repo,'fixture','behavior',rid,'Fixture command ran successfully and left its checked log.')
        tasks.checkpoint(self.repo,'fixture','Verified command completion.','Review the final behavior.')
        self.assertEqual('complete',tasks.complete(self.repo,'fixture')['status'])
        (self.root/'code.py').write_text('changed'); result=tasks.assess(self.repo,'fixture')
        self.assertEqual('needs-verification',result['status']); self.assertFalse(result['readyToComplete'])
        with self.assertRaises(ValueError): tasks.attach(self.repo,'fixture','behavior',rid,'Old result')
    def test_native_report_requires_actual_nonempty_passing_checks(self):
        folder=self.root/'artifacts/native-fixture'; (folder/'native').mkdir(parents=True)
        step={'evidence':'native'}
        for data in [{'nativeMouseInput':'passed','checks':{}},{'nativeMouseInput':'failed','checks':{'one':True}},{'nativeMouseInput':'passed','checks':{'one':False}}]:
            atomic_json(folder/'native/report.json',data); self.assertFalse(runner.inspect_evidence(step,folder,None)['valid'])
        atomic_json(folder/'native/report.json',{'nativeMouseInput':'passed','checks':{'one':True}})
        self.assertTrue(runner.inspect_evidence(step,folder,None)['valid'])

if __name__=='__main__': unittest.main()
