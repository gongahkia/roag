local G=require('src.game')
local T=require('src.card_trial')
local D=require('src.dice_trial')
local P=require('src.dice_presentation')
local V=require('src.dice_view')
local U=require('src.dice_ui')
local Codec=require('src.persistence')
local Policy=require('tests.dice_trial_policy')
local PB=require('src.playback')
local pass,fail=0,0
local function same(a,b)assert(Codec.encode(a)==Codec.encode(b),'state mismatch')end
local function test(name,f)local ok,why=xpcall(f,debug.traceback);if ok then pass=pass+1;print('PASS '..name)else fail=fail+1;print('FAIL '..name..'\n'..why)end end
local function fresh()return T.new(G,202,'bare','dice')end

test('full, reduced and immediately completed cosmetics preserve every action boundary over two fights',function()
 local reference
 for _,mode in ipairs({'full','reduced','skip'})do
  local s=fresh();local p=P.new(s.dice,mode=='reduced');local boundaries={};local actions=0
  for n=1,70 do
   local before=G.snapshot(s)
   for j=1,8 do P.inspect(p,j%2==0,j%3+1);P.select(p,j%3+1);P.update(p,.04,true,j%3+1)end
   same(s,before)
   if s.game_over then break end
   if s.card_trial.phase=='face_edit'then
    local old=s.dice.dice[1][1];assert(G.submit(s,{kind='dice_edit',die=1,index=1,replacement='quick_step'}));P.sync(p,s.dice);P.edit(p,1,1,old,'quick_step')
   end
   local i=Policy.choose(s);local face=s.dice.results[i.die].face
   local ok,why,e=G.submit(s,i);assert(ok,why);P.use(p,i.die,face,i.dx,i.dy);P.sync(p,s.dice)
   if mode=='skip'then P.finish(p)else for _=1,20 do P.update(p,.016,true);P.events(p,e,s.player_id)end end
   P.update(p,0,false);assert(not p.rolling and p.results[1].index==s.dice.results[1].index)
   boundaries[#boundaries+1]=G.snapshot(s);actions=actions+1
  end
  assert(s.card_trial.phase=='victory'and actions==25 and G.player(s).hp==11)
  if reference then same(reference,boundaries)else reference=boundaries end
 end
end)
test('each physical index maps to the settled cube and net including duplicates and edited index',function()
 local s=fresh();local p=P.new(s.dice)
 for n=1,3 do for j=1,6 do
  p.results[n]={index=j,face=p.dice[n][j]};P.finish(p)
  local ax,ay,az=V.pose(p,n);assert(type(ax)=='number'and type(ay)=='number'and az==0);assert(V.front(p,n)==j,'wrong settled physical face')
  assert(P.net_face(p,n,j)==D.catalogue[n][math.ceil(j/2)])
 end end
 s.card_trial.phase='face_edit';assert(G.submit(s,{kind='dice_edit',die=1,index=1,replacement='quick_step'}))
 P.sync(p,s.dice);P.edit(p,1,1,'step','quick_step');assert(P.net_face(p,1,1)=='step')
 P.update(p,.13,false);assert(P.net_face(p,1,1)=='quick_step'and P.net_face(p,1,2)=='step')
 same(p.dice,s.dice.dice)
end)
test('a repeated result revision tumbles once; inspection, draws and terminal changes never reroll',function()
 local s=fresh();local p=P.new(s.dice);P.finish(p);local before=G.snapshot(s)
 P.inspect(p,true,2);P.update(p,.2,false);P.inspect(p,false);P.update(p,.2,false)
 assert(not P.sync(p,s.dice)and p.roll_count==1);same(s,before)
 local copy=G.snapshot(s);copy.dice.revision=copy.dice.revision+1
 assert(P.sync(p,copy.dice)and p.roll_count==2 and p.rolling);same(p.results,before.dice.results)
 for _=1,5 do assert(not P.sync(p,copy.dice))end
 P.update(p,.02,false);assert(not p.rolling,'roll-only lock after playback')
 s.card_trial.phase='death';assert(not P.sync(p,s.dice)) -- old revision is obsolete, not another roll
end)
test('net hit testing waits for the actual settled net and selects each indexed physical slot',function()
 local s=fresh();local p=P.new(s.dice)
 for n=1,3 do P.inspect(p,true,n);p.net=.4;local x,y=V.net_cell(p,1);assert(not V.hit(p,x+10,y+10))
  p.net=1;for j=1,6 do x,y=V.net_cell(p,j);assert(V.hit(p,x+20,y+20)==j)end
 end
end)
test('input readiness is exactly existing playback readiness with zero extra cosmetic block',function()
 local s=fresh();local baseline,decorated={},{}
 for n=1,8 do
  local i=Policy.choose(s);local ok,_,events=G.submit(s,i);assert(ok)
  local a,b=PB.new(),PB.new();PB.add(a,events,s.player_id);PB.add(b,events,s.player_id)
  local p=P.new(s.dice);p.opening=false;local elapsed=0
  while PB.busy(a)or PB.busy(b)do PB.update(a,.005);PB.update(b,.005);P.update(p,.005,PB.busy(b));elapsed=elapsed+.005 end
  assert(not p.rolling);baseline[#baseline+1]=elapsed;decorated[#decorated+1]=elapsed
 end
 same(baseline,decorated);print('READINESS: 8 actions; 0 ms additional blocked time (internal playback clock)')
end)
test('opening skip consumes its input; replay and reduced toggles leave gameplay untouched',function()
 local state=fresh();local sounds={};U.attach({state=function()return state end,busy=function()return false end,target=function()end,
  start=function()state=fresh()end,submit=function(i)return G.submit(state,i)end,dice_sound=function(k)sounds[#sounds+1]=k end})
 U.seed=202;U.reduced=false;U.start();local before=G.snapshot(state);U.keypressed('d');same(state,before)
 U.update(.25);U.keypressed('d');assert(state.clock==70)
 before=G.snapshot(state);U.keypressed('escape');U.keypressed('m');U.keypressed('return');same(state,before)
 U.set_inspect(true,1);U.update(.1);U.set_inspect(false);U.update(.1);same(state,before)
 assert(#sounds>0);U.start();same(state,fresh())
end)
test('presentation audio is original PCM and uses the existing UI mix, effects volume and mute',function()
 local R=require('src.studio_audio_runtime');local made=0
 local function source()
  local s={playing=false};function s:clone()return source()end
  function s:setVolume(v)self.volume=v end;function s:setPitch(v)self.pitch=v end;function s:setLooping(v)end
  function s:play()self.playing=true end;function s:stop()self.playing=false end;function s:isPlaying()return self.playing end
  return s
 end
 local runtime=R.new(function()end,function(bytes)made=made+1;assert(bytes:sub(1,4)=='RIFF');return source()end)
 for _,kind in ipairs({'roll','land','fold','edit'})do assert(runtime:dice_cue(kind))end
 local p=fresh();local before=G.snapshot(p);runtime:set_user_mix(1,0);runtime.elapsed=runtime.elapsed+1
 local count=runtime.played_count;assert(not runtime:dice_cue('roll'));assert(runtime.played_count==count);same(p,before)
 runtime:stop_all();runtime:set_user_mix(1,.25);assert(runtime:dice_cue('roll'));local voice=runtime.voices[#runtime.voices]
 assert(math.abs(voice.source.volume-voice.base_volume*.25)<1e-9)
 runtime.pack.mix.ui.mute=true;runtime.elapsed=runtime.elapsed+1;assert(not runtime:dice_cue('land'))
 assert(#runtime.pack.sounds==8,'trial samples changed the shared audio pack')
end)
print(('Dice presentation: %d passed, %d failed'):format(pass,fail));os.exit(fail==0 and 0 or 1)
