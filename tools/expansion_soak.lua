-- CPU-clock experiment, not a rendering benchmark. No saves or local colonies read.
-- Optional output path; 6 scenarios, 600 ticks by default. Larger tick count = longer run.
local G=require('src.generate')
local H=require('src.history')
local Metrics=require('src.metrics')
local W=require('src.world')
local U=require('src.util')
local Codec=require('src.codec')
local L=require('src.labor')
local ticks=tonumber(arg and arg[2]) or 600;U.integer(ticks,'soak ticks',100,10000)
local cases={
 {layout='hybrid',climate='balanced',crew=3,seed=12345},
 {layout='roots',climate='overgrown',crew=6,seed=7},
 {layout='labyrinth',climate='ruined',crew=9,seed=456},
 {layout='chasms',climate='abyssal',crew=3,seed=19},
 {layout='crystal',climate='frozen',crew=6,seed=23},
 {layout='karst',climate='volcanic',crew=9,seed=101},
}
local rows={U.csv({'layout','climate','crew','seed','ticks','CPU_ms_mean_tick','CPU_ms_p95_tick','ecology_CPU_ms_mean',
 'alive','fauna_alive','flora_alive','jobs_done','water_residual','mineral_residual','food_residual','state_hash'})}
for i,o in ipairs(cases) do
 local w=G.make(o.seed,'frontier','challenge',192,112,{layout=o.layout,climate=o.climate,crew=o.crew,density=1.4})
 local h=H.new(w);local gy=(w.home.floor-1)/4;local left=(w.home.left-1)/4
 h:queue({type='order',kind='build',build='farm',gx=left+7,gy=gy})
 h:queue({type='order',kind='build',build='bed',gx=left+5,gy=gy})
 if o.crew>3 then
  local p=L.default(w);p.quotas=true;p.weights={general=40,dig=0,build=30,haul=0,farm=30,pump=0,field=0}
  h:queue({type='labor',plan=p})
 end
 local times,eco={},{};local target,at
 for t=1,ticks do
  local start=os.clock();local _,timing=h:advance(os.clock);times[#times+1]=(os.clock()-start)*1000;eco[#eco+1]=timing.ecology*1000
  if t==math.floor(ticks/2) then target=Codec.encode(w);at=t end
  if t%100==0 then
   local m=Metrics.measure(w);assert(m.waterResidual==0 and m.mineralResidual==0 and m.foodResidual==0,'Budget failure case '..i..' tick '..t)
   W.validate(w)
  end
 end
 local m=Metrics.measure(w)
 -- Force reconstruction, rather than the shortcut which returns the live object.
 h.checkpoints={};h:seek(at);while h.seekTarget do h:updateSeek(50)end
 assert(Codec.encode(h.view)==target,'Archived ecosystem did not replay exactly')
 local loaded=H.fromText(h:saveText());assert(Codec.encode(loaded.live)==Codec.encode(w),'Saved ecosystem state mismatch')
 rows[#rows+1]=U.csv({o.layout,o.climate,o.crew,o.seed,ticks,U.mean(times),U.percentile(times,0.95),U.mean(eco),m.alive,m.creatures,m.growths,w.stats.jobsDone,m.waterResidual,m.mineralResidual,m.foodResidual,Codec.hash(w)})
 print('PASS soak '..i..'/6 '..o.layout..' crew '..o.crew..' survivors '..m.alive..' / accounting + replay + save')
end
local csv=table.concat(rows,'\n')..'\n'
if arg and arg[1] then local f=assert(io.open(arg[1],'wb'));assert(f:write(csv));assert(f:close()) else print(csv) end
print('Native '.._VERSION..' CPU time; 192x112; history checkpoint work included in tick durations. No renderer or GPU. Independent cases, not a quality ranking.')
