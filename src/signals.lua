-- Bounded environmental signals. They are saved/replayed, not wall-clock effects.
local S={}
function S.emit(w,kind,x,y,strength)
 if not w.content then return end
 local q=w.content.signals
 q[#q+1]={kind=kind,x=x,y=y,strength=strength,tick=w.tick}
 if #q>48 then table.remove(q,1) end
end
function S.prune(w)
 if not w.content then return end
 local keep={}
 for _,v in ipairs(w.content.signals) do if w.tick-v.tick<=240 then keep[#keep+1]=v end end
 w.content.signals=keep
end
return S
