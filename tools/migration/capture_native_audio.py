#!/usr/bin/env python3
"""Pin PCM from the original built-in placeholder samples (not authored replacement music)."""
import hashlib,json
from lupa.luajit21 import LuaRuntime
from inventory_boons import ROOT,REVISION,SOURCES,original_source
lua=LuaRuntime(unpack_returned_tuples=True,encoding=None)
lua.globals().original_source=lambda name:original_source(name.decode()).encode()
lua.execute(b"package.path='';package.cpath='';package.loaders={function(name)return assert(loadstring(original_source(name),'@original/'..name))end}")
samples=lua.eval(b"require('src.studio_audio_samples')")
rows=[]
for kind in ['step','hit','boon','menu','enemy','terrain','music','ambience']:
    wav=samples[b'wav'](kind.encode());pcm=wav[44:]
    rows.append(dict(kind=kind,bytes=len(pcm),sha256=hashlib.sha256(pcm).hexdigest()))
(ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-audio.json').write_text(json.dumps(dict(revision=REVISION,source=hashlib.sha256(original_source('src.studio_audio_samples').encode()).hexdigest(),samples=rows),separators=(',',':'))+'\n')
print('Captured eight original PCM samples.')
