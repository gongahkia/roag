#!/usr/bin/env python3
"""One-time static C# translation of the pinned Roag rules; no interpreter is shipped.
Development dependency: luaparser. Source can be read from the recorded Git commit.
Generated methods execute as .NET code. Handwritten integration belongs outside Generated.
"""
import pathlib,json,sys,subprocess,hashlib
from luaparser import ast
from luaparser import astnodes as n
ROOT=pathlib.Path(__file__).resolve().parents[2]
BASE='d498fe0889050d2a23f2ff86d80db6084ed6467a'
def quote(s): return json.dumps(s,ensure_ascii=True).replace('\\/','/')
class Compiler:
 def __init__(self): self.scopes=[{}];self.serial=0;self.argstack=[]
 def fresh(self,name='v'):
  self.serial+=1;return f'{name}_{self.serial}'
 def bind(self,name):
  v=self.fresh('v');self.scopes[-1][name]=v;return v
 def lookup(self,name):
  for scope in reversed(self.scopes):
   if name in scope:return scope[name]
  return f'rt.Global({quote(name)})'
 def multi(self,x):
  if isinstance(x,(n.Call,n.Invoke)) and not x.wrapped:return self.call(x)
  if isinstance(x,n.Dots):return f'R.Tail({self.argstack[-1][0]}, {self.argstack[-1][1]})'
  return f'new Results({self.expr(x)})'
 def args(self,xs):
  if not xs:return 'Results.Empty'
  return 'R.Join(new V[] {'+', '.join(self.expr(x) for x in xs[:-1])+'}, '+self.multi(xs[-1])+')'
 def call(self,x):
  if isinstance(x,n.Invoke):return f'R.Invoke({self.expr(x.source)}, {quote(x.func.id)}, {self.args(x.args)})'
  return f'R.Call({self.expr(x.func)}, {self.args(x.args)})'
 def expr(self,x):
  if isinstance(x,(int,float)):return f"(V){float(x)!r}d"
  if isinstance(x,n.Name):return self.lookup(x.id)
  if isinstance(x,n.Nil):return 'V.Nil'
  if isinstance(x,n.TrueExpr):return '(V)true'
  if isinstance(x,n.FalseExpr):return '(V)false'
  if isinstance(x,n.Number):return f'(V){float(x.n)!r}d'
  if isinstance(x,n.String):return '(V)'+quote(x.s.decode('latin1') if isinstance(x.s,bytes) else x.s.encode().decode('latin1'))
  if isinstance(x,n.Index):return f'R.Get({self.expr(x.value)}, '+('(V)'+quote(x.idx.id) if x.notation.name=='DOT' else self.expr(x.idx))+')'
  if isinstance(x,(n.Call,n.Invoke)):return self.call(x)+'.First'
  if isinstance(x,n.Dots):return self.multi(x)+'.First'
  if isinstance(x,n.AnonymousFunction):return self.function(x.args,x.body)
  if isinstance(x,n.Table):
   fields=[];ai=1
   for i,f in enumerate(x.fields):
    if f.key is None:
     if i==len(x.fields)-1 and isinstance(f.value,(n.Call,n.Invoke,n.Dots)):
      fields.append(f'new Entry({ai}, {self.multi(f.value)})');continue
     key=f'(V){ai}';ai+=1
    else:key="(V)"+quote(f.key.id) if isinstance(f.key,n.Name) and not f.between_brackets else self.expr(f.key)
    fields.append(f'new Entry({key}, {self.expr(f.value)})')
   return 'R.Table('+', '.join(fields)+')'
  if isinstance(x,n.AndLoOp):return f'R.And({self.expr(x.left)}, () => {self.expr(x.right)})'
  if isinstance(x,n.OrLoOp):return f'R.Or({self.expr(x.left)}, () => {self.expr(x.right)})'
  if isinstance(x,n.Concat):return f'R.Concat({self.expr(x.left)}, {self.expr(x.right)})'
  if isinstance(x,n.ULengthOP):return f'(V)R.Length({self.expr(x.operand)})'
  if isinstance(x,n.ULNotOp):return f'(V)(!R.Truth({self.expr(x.operand)}))'
  if isinstance(x,n.UMinusOp):return f'(-{self.expr(x.operand)})'
  ops={n.AddOp:'+',n.SubOp:'-',n.MultOp:'*',n.FloatDivOp:'/',n.ModOp:'%',n.EqToOp:'==',n.NotEqToOp:'!=',n.LessThanOp:'<',n.LessOrEqThanOp:'<=',n.GreaterThanOp:'>',n.GreaterOrEqThanOp:'>='}
  for cls,op in ops.items():
   if isinstance(x,cls):return f'(V)({self.expr(x.left)} {op} {self.expr(x.right)})'
  if isinstance(x,n.ExpoOp):return f'(V)Math.Pow(R.Num({self.expr(x.left)}), R.Num({self.expr(x.right)}))'
  raise TypeError(type(x).__name__)
 def function(self,args,body,method=False):
  self.scopes.append({});av=self.fresh('args');params=[]
  if method:params.append(f'V {self.bind("self")} = {av}[0];')
  offset=int(method);count=offset
  for a in args:
   if isinstance(a,n.Varargs):continue
   params.append(f'V {self.bind(a.id)} = {av}[{count}];');count+=1
  self.argstack.append((av,count))
  code=self.block(body,False)
  self.argstack.pop();self.scopes.pop()
  return f'new V((RuleFunction)({av} => {{\n'+ '\n'.join(params)+'\n'+code+'\nreturn Results.Empty;\n}))'
 def assign(self,target,value):
  if isinstance(target,n.Name):
   l=self.lookup(target.id)
   if l.startswith('rt.Global('):return f'rt.SetGlobal({quote(target.id)}, {value});'
   return f'{l} = {value};'
  if isinstance(target,n.Index):
   key='(V)'+quote(target.idx.id) if target.notation.name=='DOT' else self.expr(target.idx)
   return f'R.Set({self.expr(target.value)}, {key}, {value});'
  raise TypeError(type(target).__name__)
 def block(self,b,scope=True):
  if scope:self.scopes.append({})
  out='\n'.join(self.stmt(x) for x in b.body)
  if scope:self.scopes.pop()
  return out
 def stmt(self,x):
  if isinstance(x,n.SemiColon):return ''
  if isinstance(x,n.LocalFunction):
   name=self.bind(x.name.id);return f'V {name} = V.Nil;\n{name} = {self.function(x.args,x.body)};'
  if isinstance(x,n.Function):return self.assign(x.name,self.function(x.args,x.body))
  if isinstance(x,n.Method):return f'R.Set({self.expr(x.source)}, {quote(x.name.id)}, {self.function(x.args,x.body,True)});'
  if isinstance(x,(n.LocalAssign,n.Assign)):
   value=self.args(x.values);temp=self.fresh('values');lines=[f'var {temp} = {value};']
   # Freeze indexed lvalues before writes (a[i], i = ..., ...).
   refs=[]
   if not isinstance(x,n.LocalAssign):
    for t in x.targets:
     if isinstance(t,n.Index):
      obj=self.fresh('obj');key=self.fresh('key');lines.append(f'var {obj} = {self.expr(t.value)};')
      lines.append(f'V {key} = '+('(V)'+quote(t.idx.id) if t.notation.name=='DOT' else self.expr(t.idx))+';');refs.append((obj,key))
     else:refs.append(None)
   for i,t in enumerate(x.targets):
    if isinstance(x,n.LocalAssign):lines.append(f'V {self.bind(t.id)} = {temp}[{i}];')
    elif refs[i]:lines.append(f'R.Set({refs[i][0]}, {refs[i][1]}, {temp}[{i}]);')
    else:lines.append(self.assign(t,f'{temp}[{i}]'))
   return '\n'.join(lines)
  if isinstance(x,(n.Call,n.Invoke)):return self.call(x)+';'
  if isinstance(x,n.Return):return 'return '+self.args(x.values)+';'
  if isinstance(x,n.Break):return 'break;'
  if isinstance(x,(n.If,n.ElseIf)):
   result=f'if (R.Truth({self.expr(x.test)})) {{\n{self.block(x.body)}\n}}'
   if x.orelse:
    result+=' else '+(self.stmt(x.orelse) if isinstance(x.orelse,n.ElseIf) else '{\n'+self.block(x.orelse)+'\n}')
   return result
  if isinstance(x,n.While):return f'while (R.Truth({self.expr(x.test)})) {{\n{self.block(x.body)}\n}}'
  if isinstance(x,n.Repeat):
   self.scopes.append({});body=self.block(x.body,False);test=self.expr(x.test);self.scopes.pop()
   return f'while (true) {{\n{body}\nif (R.Truth({test})) break;\n}}'
  if isinstance(x,n.Fornum):
   a,b,s=self.expr(x.start),self.expr(x.stop),self.expr(x.step) if x.step else '(V)1'
   self.scopes.append({});var=self.bind(x.target.id);it=self.fresh('i');stop=self.fresh('end');step=self.fresh('step');body=self.block(x.body,False);self.scopes.pop()
   return f'{{ double {stop} = R.Num({b}), {step} = R.Num({s});\nfor (double {it} = R.Num({a}); {step} > 0 ? {it} <= {stop} : {it} >= {stop}; {it} += {step}) {{\nV {var} = {it};\n{body}\n}}\n}}'
  if isinstance(x,n.Forin):
   value=self.args(x.iter);self.scopes.append({});row=self.fresh('row');decls=[f'V {self.bind(t.id)} = {row}[{i}];' for i,t in enumerate(x.targets)];body=self.block(x.body,False);self.scopes.pop()
   return f'foreach (var {row} in R.Iterate({value})) {{\n'+'\n'.join(decls)+'\n'+body+'\n}'
  if isinstance(x,n.Do):return '{\n'+self.block(x.body)+'\n}'
  raise TypeError(type(x).__name__)
def generate(rel,out,test=False):
 raw=subprocess.check_output(['git','show',f'{BASE}:{rel}'],cwd=ROOT)
 source=raw.decode()
 if rel=='src/game.lua':
  source=source.replace('if item.kind == "actor_ready" and item.actor_id == state.player_id then return end', 'if item.kind == "actor_ready" and (item.actor_id == state.player_id or _roag_party_ready(state,item)) then return end')
  source=source.replace('local function remove_dead(state, target, output, source_id, action_id)', 'local function remove_dead(state, target, output, source_id, action_id)\n    if _roag_downed(state,target) then return end')
  source=source.replace('contexts[output] = nil\n    return true, nil, output', 'contexts[output] = nil\n    _roag_finish_action(state)\n    return true, nil, output')
 c=Compiler();tree=ast.parse(source);code=c.block(tree.body)
 name=pathlib.Path(rel).stem;cls='Rules_'+name
 header='// Static C# port of '+rel+' at '+BASE+'.\n// Original SHA256: '+hashlib.sha256(raw).hexdigest()+'\n#nullable disable\n#pragma warning disable CS0162, CS0219, CS1718\nusing Roag.Core.Rules;\nnamespace Roag.Core.Rules.Generated;\n'
 out.mkdir(parents=True,exist_ok=True)
 (out/(cls+'.cs')).write_text(header+f'internal static class {cls} {{ internal static V Load(NativeRuntime rt) {{\n'+code+'\nreturn V.Nil;\n} }\n' if False else header+f'internal static class {cls} {{ internal static Results Load(NativeRuntime rt) {{\n'+code+'\nreturn Results.Empty;\n} }\n')
 return name,cls
if __name__=='__main__':
 out=ROOT/'src/Roag.Core/Rules/Generated';names=[]
 files=subprocess.check_output(['git','ls-tree','-r','--name-only',BASE,'src'],cwd=ROOT).decode().splitlines()
 for rel in files:
  if rel.endswith('.lua'):
   name,cls=generate(rel,out);names.append((name,cls))
 registry='// Native compiled rule factories. No source parsing or Lua dependency.\nnamespace Roag.Core.Rules;\npublic sealed partial class NativeRuntime {\nprivate void RegisterRules() {\n'+ '\n'.join(f'Factories["src.{name}"] = () => Generated.{cls}.Load(this).First;' for name,cls in names)+'\n}\n}\n'
 (out.parent/'RuleRegistry.cs').write_text(registry)
 print('Compiled',len(names),'rule modules to C# source')
