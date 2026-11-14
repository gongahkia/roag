-- Physical dice policy; combat and forecasts use the shared Game resolver.
local RNG=require('src.rng')
local Content=require('src.content')
local Boons=require('src.boons')
local D={roles={'Footwork','Weapon','Tactics'},catalogue={{'step','guarded_step','quick_step'},{'strike','sweep','pierce'},{'guard','dash','heavy_strike'}}}
D.faces={
 step={name='Step',kind='move',cost=100,description='Move one tile.'},
 guarded_step={name='Guarded Step',kind='move',cost=100,guard=true,description='Move, then refresh 2 Guard.'},
 quick_step={name='Quick Step',kind='move',cost=70,description='Move one tile quickly.'},
 strike={name='Strike',kind='basic_attack',cost=100,description='Attack an adjacent cell.'},
 sweep={name='Sweep',kind='sweeping_slash',cost=150,description='Attack a three-cell arc.'},
 pierce={name='Pierce',kind='basic_attack',cost=100,description='Strike through a two-cell ray.'},
 guard={name='Guard',kind='dice_guard',cost=100,guard=true,description='Refresh 2 Guard for 200 time.'},
 dash={name='Dash',kind='dash',cost=130,description='Travel up to two tiles.'},
 heavy_strike={name='Heavy Strike',kind='basic_attack',cost=150,description='Adjacent attack with +2 base damage.'},
}
function D.roll(s)
 local d=s.dice;d.results={};d.revision=d.revision+1;d.rolls=d.rolls+1
 for n,die in ipairs(d.dice)do local i=RNG.next_int(d.rng,6);d.results[n]={index=i,face=die[i]}end
end
function D.initialize(s)
 s.dice={dice={},revision=0,rolls=0,guard=0,guard_until=0,rng=RNG.new(RNG.derive(s.run_seed,'dice-trial:faces/v1'))}
 for n,cat in ipairs(D.catalogue)do s.dice.dice[n]={cat[1],cat[1],cat[2],cat[2],cat[3],cat[3]}end
 local p=require('src.game').player(s)
 Boons.set_stacks(p,'core:boon/rhythm_of_steel','uncommon',1)
 Boons.set_stacks(p,'core:boon/detonation_bloom','rare',1)
 D.roll(s)
end
local function invalid(reason)return {valid=false,reason=reason,cells={}}end
function D.plan(s,i,G)
 local d=s.dice
 if type(i)~='table' or i.kind~='dice_action' or s.game_over or s.card_trial.phase~='combat'then return invalid('Choose a current die result')end
 if i.revision~=d.revision then return invalid('Stale result set')end
 if i.face~=nil or i.steps~=nil or i.vow~=nil then return invalid('Unsupported action option')end
 if type(i.die)~='number' or i.die%1~=0 then return invalid('Choose a die')end
 local r=d.results[i.die];if not r then return invalid('Choose a die')end
 local f=D.faces[r.face];local p=G.player(s);local plan
 if f.kind=='dice_guard'then plan={valid=true,cells={}}
 else
  plan=G.preview(s,f.kind,i.dx,i.dy)
  if plan.bump or plan.door_id or plan.vault_id or plan.stair then return invalid('Choose a free movement tile')end
  if not plan.valid then return plan end
 end
 plan.cost=G.effective_cost(f.cost,p.speed+Boons.speed_bonus(s,p))
 plan.intent={kind=f.kind,dx=i.dx,dy=i.dy};plan.face=r.face;plan.guard=f.guard
 if r.face=='pierce'then
  plan.cells={};local map=Content.map_for(s);local x,y=p.position.x,p.position.y
  for _=1,2 do
   local nx,ny=x+i.dx,y+i.dy
   if not Content.projectile_passable(map,nx,ny,s)then break end
   if i.dx~=0 and i.dy~=0 and (not Content.projectile_passable(map,x+i.dx,y,s) or not Content.projectile_passable(map,x,y+i.dy,s))then break end
   plan.cells[#plan.cells+1]={x=nx,y=ny};x,y=nx,ny
  end
 end
 return plan
end
function D.guard(s)
 local d=s.dice
 if s.clock>=d.guard_until then return 0 end
 return d.guard
end
function D.absorb(s,target,damage)
 if target.id~=s.player_id then return damage,0 end
 local d=s.dice;if s.clock>=d.guard_until then d.guard=0 end
 local n=math.min(damage,d.guard);d.guard=d.guard-n;return damage-n,n
end
function D.apply(s,plan,emit)
 s.card_trial.commits=s.card_trial.commits+1
 if plan.guard then
  s.dice.guard=2;s.dice.guard_until=s.clock+200
  emit('BoardGuarded',{guard=2,floor_id=s.active_floor_id},s.player_id)
 end
end
function D.after_action(s,G,emit)
 local t=s.card_trial
 if s.clock>=s.dice.guard_until then s.dice.guard=0 end
 if G.player(s).hp<=0 then t.phase='death';s.game_over=true;return end
 for _,e in ipairs(s.entities)do if e.faction=='enemy' and e.hp>0 then D.roll(s);return end end
 if t.fight==2 then t.phase='victory';s.game_over=true;emit('CardTrialWon',{floor_id=s.active_floor_id},s.player_id)
 else t.phase='face_edit'end
end
function D.edit(s,i,G)
 local d=s.dice
 if s.card_trial.phase~='face_edit' or d.edited then return false,'Face edit unavailable',{}end
 if not i.skip then
  local die=d.dice[i.die];local cat=D.catalogue[i.die];local valid=false
  for _,face in ipairs(cat or {})do if face==i.replacement then valid=true end end
  if not die or type(i.index)~='number' or i.index%1~=0 or i.index<1 or i.index>6 or not valid or die[i.index]==i.replacement then return false,'Choose a different compatible face',{}end
  die[i.index]=i.replacement
 end
 d.edited=true;d.edit_choice={die=i.die,index=i.index,replacement=i.replacement,skip=i.skip}
 require('src.card_trial').begin_second(s,G.spawn_enemy);D.roll(s);return true,nil,{}
end
function D.forecast(s,i,G)
 local plan=D.plan(s,i,G);if not plan.valid then return {valid=false,plan=plan}end
 local clone=G.snapshot(s);local ok,why,events=G.submit(clone,i)
 -- Future rolls remain private, including the RNG state which produced them.
 clone.dice.results=nil;clone.dice.rng=nil
 return {valid=ok,reason=why,state=clone,events=events,plan=plan}
end
return D
