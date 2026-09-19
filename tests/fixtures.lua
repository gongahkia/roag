local W=require('src.world')
local M=require('src.materials')
local F={}
function F.world(mode)
 local w=W.new(64,48,101,'fixture',mode or 'challenge')
 for y=1,w.height do for x=1,w.width do
  if x<=2 or x>=w.width-1 or y<=2 or y>=w.height-1 then W.put(w,x,y,M.BEDROCK)
  elseif y>=25 then W.put(w,x,y,M.ROCK) end
 end end
 return w
end
function F.worker(w,x,y,name)
 local a={id=W.id(w),name=name or 'Test settler',x=x or 10,y=y or 24,alive=true,hp=100,
  hunger=20,fatigue=10,breath=100,mine=1,build=1,status='Idle',reason='',
  fall=0,worked=false,thinkAt=0,progress=0}
 w.workers[#w.workers+1]=a;return a
end
function F.fill(w,x1,y1,x2,y2,m)
 for y=y1,y2 do for x=x1,x2 do W.put(w,x,y,m) end end
end
function F.steps(w,n,commands)
 local Sim=require('src.sim')
 for i=1,n do Sim.step(w,i==1 and commands or nil) end
end
function F.baseline(w) w.baseline=require('src.metrics').measure(w) end
return F
