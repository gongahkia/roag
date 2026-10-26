import re,pathlib,subprocess
from translate_rules import ROOT,BASE,generate
out=ROOT/'tests/Roag.RuleParity/Generated'
queue=['tests.run'];seen=set();registry=[]
while queue:
 name=queue.pop(0)
 if name in seen:continue
 seen.add(name);rel=name.replace('.','/')+'.lua'
 raw=subprocess.check_output(['git','show',f'{BASE}:{rel}'],cwd=ROOT).decode()
 queue.extend(re.findall(r'''require\(["'](tests\.[^"']+)["']\)''',raw))
 _,cls=generate(rel,out)
 registry.append(f'rt.Factories["{name}"] = () => Roag.Core.Rules.Generated.{cls}.Load(rt).First;')
(out/'Register.cs').write_text('using Roag.Core.Rules;\ninternal static class Parity { internal static void Register(NativeRuntime rt) {\n'+'\n'.join(registry)+'\n} }')
print('Translated',len(seen),'parity suites')
