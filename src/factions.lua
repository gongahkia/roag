-- COS-G05: a deliberately bounded campaign-level foreign-society model.
-- Foreign societies are coarse state, never hidden local worlds.  Everything
-- in this module is data-only and ordered by persistent IDs for replay.
local U=require('src.util')
local R=require('src.campaign_random')
local W=require('src.world')
local F={version=1,externalCount=4}

local axes={'hierarchy','reciprocity','preservation','expansion','scholarship','commerce','hospitality','secrecy'}
local goalOrder={'secure_food','acquire_minerals','expand_industry','seek_knowledge','preserve_heritage','expand_influence'}
local forms={'Compact','League','Assembly','Syndicate','Covenant','Combine','Circle','Directorate','Communion','Freeholds'}
local onset={'v','r','s','t','k','m','n','l','d','z','sh','th','p','g'}
local vowel={'a','e','i','o','u','ae','ia'}
local coda={'r','n','sh','l','m','s','th','v'}
local kinds={'founding_compact','scarcity','migration','industrial_breakthrough','industrial_accident','ruin_encounter','scholar_schism','trade_boom','isolation_period','frontier_push'}
local inter={'trade_pact','trade_dispute','shared_expedition','border_competition','knowledge_exchange','cultural_offense'}
local values={food=2,rock=1,metal=3,component=7,rope_coil=5,pickaxe=10}
local goods={'food','rock','metal','component','rope_coil','pickaxe'}

local function stream(c,key) return R.new(R.derive(c.seed,'factions/v1/'..key)) end
local function pick(r,list) return list[R.uniform(r,#list)] end
local function clamp(v,a,b) return math.max(a,math.min(b,v)) end
local function sorted(t,field)
 local o={};for _,v in pairs(t) do o[#o+1]=v end;table.sort(o,function(a,b)return a[field or 'id']<b[field or 'id'] end);return o
end
local function word(r)
 local n=2+R.uniform(r,2);local out={}
 for i=1,n do out[#out+1]=pick(r,onset)..pick(r,vowel)..(i==n and pick(r,coda) or '') end
 local s=table.concat(out);return s:sub(1,1):upper()..s:sub(2)
end
local function relation()
 return {trust=45,respect=45,tension=20,grievance=10}
end
local function attitude(r)
 if r.tension>=80 or r.grievance>=80 or (r.trust<=10 and r.respect<=10) then return 'HOSTILE' end
 if r.tension>=55 or r.grievance>=55 then return 'WARY' end
 if r.trust>=60 and r.respect>=50 and r.tension<40 then return 'COOPERATIVE' end
 return 'NEUTRAL'
end
F.attitude=attitude
local function normList(culture)
 local rank={};for i,a in ipairs(axes) do rank[#rank+1]={axis=a,value=culture[a],order=i} end
 table.sort(rank,function(a,b)return a.value==b.value and a.order<b.order or a.value>b.value end)
 local out={};for i=1,3 do out[#out+1]={axis=rank[i].axis,direction='value',strength=rank[i].value>=75 and 'strong' or 'ordinary',version=1} end
 local values0={};for _,a in ipairs(axes) do values0[#values0+1]=culture[a] end;table.sort(values0);local median=values0[4]
 local low=rank[#rank];if median-low.value>=20 then out[#out+1]={axis=low.axis,direction='aversion',strength=low.value<=25 and 'strong' or 'ordinary',version=1} end
 return out
end
local function addIncident(state,record)
 record.id=state.nextIncidentId;state.nextIncidentId=record.id+1;state.incidents[#state.incidents+1]=record
 while #state.incidents>128 do table.remove(state.incidents,1) end
 return record
end
local function change(r,dt,dr,dte,dg)
 r.trust=clamp(r.trust+(dt or 0),0,100);r.respect=clamp(r.respect+(dr or 0),0,100)
 r.tension=clamp(r.tension+(dte or 0),0,100);r.grievance=clamp(r.grievance+(dg or 0),0,100)
end
local function stock(f,k) return f.stocks[k] or 0 end
local function setstock(f,k,v) f.stocks[k]=clamp(v,0,k=='population' and 100 or (k=='tools' and 128 or k=='components' and 500 or 1000)) end
local function goal(f)
 local candidates={
  secure_food=1000-stock(f,'food')+f.culture.hospitality,
  acquire_minerals=1000-stock(f,'minerals')+f.culture.expansion,
  expand_industry=500-stock(f,'components')*2+f.economy.manufacturing,
  seek_knowledge=f.culture.scholarship+f.economy.scholarshipCapacity,
  preserve_heritage=f.culture.preservation,
  expand_influence=f.culture.expansion+f.stocks.influence/5,
 }
 local best,bv=goalOrder[1],-1e9;for _,k in ipairs(goalOrder) do if candidates[k]>bv then best,bv=k,candidates[k] end end;return best
end
local function makeFaction(c,id)
 local r=stream(c,'faction/'..id);local root=word(r);local culture={}
 for _,a in ipairs(axes) do culture[a]=20+R.uniform(r,61) end
 local f={id=id,endonym=root,name=root..' '..pick(r,forms),language=root:lower()..'i',culture=culture,norms=normList(culture),
  economy={agriculture=R.uniform(r,101)-1,extraction=R.uniform(r,101)-1,manufacturing=R.uniform(r,101)-1,scholarshipCapacity=R.uniform(r,101)-1},
  population=20+R.uniform(r,61)-1,stocks={food=150+R.uniform(r,351),metal=80+R.uniform(r,201),minerals=100+R.uniform(r,401),components=R.uniform(r,151)-1,tools=R.uniform(r,25)-1,influence=R.uniform(r,251)-1},
  relations={},prehistory={},incidents={},contact={signal=false,established=false,protocol=nil,audienceProgress=0,audienceCooldown=0},offers={},reserved={},turns=0}
 f.strategicGoal=goal(f);return f
end
local function prehistory(c,state)
 local all=sorted(state.factions)
 for _,f in ipairs(all) do
  local r=stream(c,'prehistory/'..f.id)
  for i=1,4 do
   local k=pick(r,kinds);local e={id=state.nextEventId,era=i,kind=k,factions={f.id}};state.nextEventId=state.nextEventId+1
   if k=='scarcity' then setstock(f,'food',stock(f,'food')-60)
   elseif k=='industrial_breakthrough' then setstock(f,'components',stock(f,'components')+35)
   elseif k=='industrial_accident' then setstock(f,'minerals',stock(f,'minerals')-35)
   elseif k=='trade_boom' then setstock(f,'influence',stock(f,'influence')+30)
   elseif k=='migration' then f.population=clamp(f.population+5,20,80) end
   e.summary=f.name..' remembers '..k:gsub('_',' ')..'.';f.prehistory[#f.prehistory+1]=e
  end
 end
 for i,k in ipairs(inter) do
  local a,b=all[((i-1)%4)+1],all[((i+1)%4)+1];local e={id=state.nextEventId,era=5+i,kind=k,factions={a.id,b.id},summary=a.name..' and '..b.name..' '..k:gsub('_',' ')..'.'};state.nextEventId=state.nextEventId+1
  local ar,br=a.relations[b.id],b.relations[a.id]
  if k=='trade_pact' or k=='knowledge_exchange' then change(ar,8,4,-3,0);change(br,8,4,-3,0) else change(ar,-4,-2,6,4);change(br,-4,-2,6,4) end
  a.prehistory[#a.prehistory+1]=e;b.prehistory[#b.prehistory+1]=e
 end
end
function F.new(c)
 local state={version=1,rules={generation=1,culture=1,economy=1,protocol=1,offers=1,familiarity=1},nextEventId=1,nextIncidentId=1,nextOfferId=1,nextContractId=1,nextShipmentId=1,
  scan={progress=0,order={},known={}},factions={},incidents={},contracts={},shipments={},receipts={},totals={exports={},imports={}}}
 for id=2,5 do state.factions[#state.factions+1]=makeFaction(c,id) end
 for _,a in ipairs(state.factions) do
  a.relations[1]=relation();for _,b in ipairs(state.factions) do if a.id~=b.id then a.relations[b.id]=relation() end end
 end
 prehistory(c,state)
 local r=stream(c,'contact-order');for _,f in ipairs(state.factions) do state.scan.order[#state.scan.order+1]={id=f.id,key=R.uniform(r,2147483646)} end
 table.sort(state.scan.order,function(a,b)return a.key==b.key and a.id<b.id or a.key<b.key end);for i,v in ipairs(state.scan.order) do state.scan.order[i]=v.id end
 return state
end
function F.enabled(c) return c and c.features and c.features.factions==1 end
function F.find(c,id) for _,f in ipairs(c.factions and c.factions.factions or {}) do if f.id==id then return f end end end
function F.attach(w) w.factions={version=1} end
function F.install(w,s)
 if s.kind=='signal_relay' then s.powerPriority=1
 elseif s.kind=='trade_depot' then s.trade={cargo={},revision=1} end
end
function F.relay(w)
 for _,s in pairs(w.structures) do if s.kind=='signal_relay' and s.enabled and s._powerGranted and (not s.sabotagedUntil or s.sabotagedUntil<w.tick) then return s end end
end
function F.depot(w,id)
 for _,s in pairs(w.structures) do if s.kind=='trade_depot' and (not id or s.id==id) then return s end end
end
local function units(list) local n=0;for _,r in ipairs(list or {}) do n=n+(r.n or 1) end;return n end
local function addCargo(list,record,limit)
 if units(list)+(record.n or 1)>limit then return false end
 for _,old in ipairs(list) do if old.kind==record.kind and old.n and record.n then old.n=old.n+record.n;return true end end
 list[#list+1]={kind=record.kind,n=record.n};return true
end
local function takeCargo(list,kind,n)
 for i,r in ipairs(list or {}) do if r.kind==kind and r.n and r.n>=n then r.n=r.n-n;if r.n==0 then table.remove(list,i) end;return {kind=kind,n=n} end end
end
function F.deposit(c,siteId,depotId,itemId,n)
 local site=require('src.campaign').site(c,siteId);local depot=site and site.world and F.depot(site.world,depotId);if not depot then return false,'Trade depot is unavailable' end
 U.integer(n,'Trade deposit quantity',1,8);local item=W.find(site.world.items,itemId);if not item or item.reserved or item.n<n then return false,'Physical local goods are unavailable' end
 local ok=addCargo(depot.trade.cargo,{kind=item.kind,n=n},24);if not ok then return false,'Trade depot is full' end
 item.n=item.n-n;depot.trade.revision=depot.trade.revision+1;return true
end
function F.acceptOffer(c,siteId,depotId,offerId,representativeId)
 local site=require('src.campaign').site(c,siteId);local depot=site and site.world and F.depot(site.world,depotId);if not depot then return false,'Trade depot is unavailable' end
 for _,f in ipairs(c.factions.factions) do for _,o in ipairs(f.offers) do if o.id==offerId and not o.expired and not o.accepted and f.contact.established and attitude(f.relations[1])~='HOSTILE' then
  o.accepted=true;o.acceptedTick=c.tick;o.lockedUntil=c.tick+2000;o.siteId=siteId;o.depotId=depot.id;o.depotRevision=depot.trade.revision;o.representativeId=representativeId;return true
 end end end
 return false,'Trade offer is unavailable'
end
local function bundlePresent(list,bundle)
 for _,b in ipairs(bundle) do local have=0;for _,r in ipairs(list) do if r.kind==b.kind then have=have+(r.n or 0) end end;if have<b.n then return false end end;return true
end
local function totalsAdd(t,bundle)
 for _,b in ipairs(bundle) do t[b.kind]=(t[b.kind] or 0)+b.n end
end
function F.dispatch(c,siteId,depotId,offerId)
 local site=require('src.campaign').site(c,siteId);local depot=site and site.world and F.depot(site.world,depotId);if not depot or not F.relay(site.world) then return false,'Powered relay and exact trade depot are required' end
 local f,o
 for _,ff in ipairs(c.factions.factions) do for _,oo in ipairs(ff.offers) do if oo.id==offerId then f,o=ff,oo end end end
 if not f or not o or not o.accepted or o.siteId~=siteId or o.depotId~=depot.id or o.depotRevision~=depot.trade.revision or c.tick>o.lockedUntil or attitude(f.relations[1])=='HOSTILE' then return false,'Trade contract is stale or unavailable' end
 if not bundlePresent(depot.trade.cargo,o.requested) then return false,'Trade depot lacks the requested physical bundle' end
 for _,b in ipairs(o.requested) do assert(takeCargo(depot.trade.cargo,b.kind,b.n),'Trade cargo changed') end
 for _,b in ipairs(o.requested) do
  local key=b.kind;if key=='component' then key='components' elseif key=='rock' or key=='metal' then key='minerals' elseif key=='pickaxe' or key=='rope_coil' then key='tools' end
  setstock(f,key,stock(f,key)+b.n)
 end
 local state=c.factions;local shipment={id=state.nextShipmentId,factionId=f.id,siteId=siteId,depotId=depot.id,bundle=o.promised,departTick=c.tick,arrivalTick=c.tick+400,status='in_flight'};state.nextShipmentId=shipment.id+1;state.shipments[#state.shipments+1]=shipment;o.dispatched=true;totalsAdd(state.totals.exports,o.requested);change(f.relations[1],5,2,-2,0);addIncident(state,{tick=c.tick,kind='trade_dispatched',factionId=f.id,offerId=o.id,shipmentId=shipment.id});return true
end
function F.rebindShipment(c,shipmentId,siteId,depotId)
 local s;for _,x in ipairs(c.factions.shipments) do if x.id==shipmentId then s=x end end;local site=require('src.campaign').site(c,siteId);local d=site and site.world and F.depot(site.world,depotId)
 if not s or s.status~='awaiting_depot' or s.siteId~=siteId or not d then return false,'Shipment cannot be rebound' end;s.depotId=d.id;return true
end
function F.familiarity(a,id)
 a.factions=a.factions or {};return a.factions[id]
end
local function familiar(a,id,tick)
 a.factions=a.factions or {};local x=a.factions[id]
 if not x then x={familiarity=0,protocolExperienceCount=0,tradeExperienceCount=0};a.factions[id]=x end;x.lastInteractionTick=tick;return x
end
function F.addFamiliarity(a,id,n,tick,first)
 local x=familiar(a,id,tick);x.familiarity=clamp(x.familiarity+n,0,100);if first and not x.firstContactTick then x.firstContactTick=tick end;return x
end
local function representative(c,siteId,personId)
 local site=require('src.campaign').site(c,siteId);if not site then return nil end
 for _,a in ipairs(site and site.world and site.world.workers or {}) do if a.personId==personId and a.alive and not a.panic then return a,site end end
end
function F.beginContact(c,siteId,factionId,personId)
 local f=F.find(c,factionId);local a,site=representative(c,siteId,personId)
 if not f or not f.contact.signal then return false,'Signal is unknown' end
 if not a or not F.relay(site.world) then return false,'Representative or powered relay unavailable' end
 if f.contact.established then return false,'Formal contact already established' end
 f.contact.audience={siteId=siteId,personId=personId,progress=0};return true
end
local themes={'gift','formal','old_place','knowledge','bargain'}
local function protocol(f)
 local rank={};for _,n in ipairs(f.norms) do if n.direction=='value' then rank[#rank+1]=n.axis end end
 local map={reciprocity='gift',hierarchy='formal',preservation='old_place',expansion='old_place',scholarship='knowledge',secrecy='knowledge',commerce='bargain',hospitality='bargain'}
 local out,seen={},{};for _,axis in ipairs(rank) do local t=map[axis];if not seen[t] then seen[t]=true;out[#out+1]=t end;if #out==3 then break end end
 for _,t in ipairs(themes) do if #out==3 then break end;if not seen[t] then out[#out+1]=t;seen[t]=true end end
 return {index=1,steps=out,representatives={}}
end
local function reaction(f,theme,action,a)
 local c=f.culture;local dt,dr,dte,dg=0,0,0,0
 if theme=='gift' then if action=='reciprocate' then dt=math.floor(c.reciprocity/12) else dt=-math.floor(c.reciprocity/20);dte=math.floor(c.reciprocity/25) end
 elseif theme=='formal' then if action=='designated' then dr=math.floor(c.hierarchy/12) else dr=-math.floor(c.hierarchy/18);dte=math.floor(c.hierarchy/28) end
 elseif theme=='old_place' then if action=='preserve' then dt=math.floor(c.preservation/14) else dt=-math.floor(c.preservation/18);dg=math.floor(c.preservation/25) end
 elseif theme=='knowledge' then if action=='share' then dt=math.floor(c.scholarship/15)-math.floor(c.secrecy/35) else dt=-math.floor(c.scholarship/24);dte=math.floor(c.scholarship/35) end
 elseif theme=='bargain' then if action=='terms' then dr=math.floor(c.commerce/15) else dt=math.floor(c.hospitality/20)-math.floor(c.commerce/30);dte=math.max(0,math.floor(c.commerce/25)) end end
 dt,dr,dte,dg=clamp(dt,-12,12),clamp(dr,-12,12),clamp(dte,-12,12),clamp(dg,-12,12)
 local mitigation={empathy=0,composure=0};if a and a.psychology then if a.psychology.facets.empathy>=70 and dt<0 then dt=math.min(0,dt+2);mitigation.empathy=2 end;if a.psychology.facets.composure>=70 and dte>0 then dte=math.max(0,dte-2);mitigation.composure=2 end end
 return dt,dr,dte,dg,mitigation
end
function F.resolveProtocol(c,siteId,factionId,personId,action)
 local f=F.find(c,factionId);local a,site=representative(c,siteId,personId);local p=f and f.contact.protocol
 if not f or not a or not p or p.index>#p.steps or attitude(f.relations[1])=='HOSTILE' then return false,'Protocol action is unavailable' end
 local theme=p.steps[p.index];local allowed={gift={accept=true,reciprocate=true,inspect=true,decline=true},formal={designated=true,collective=true,bypass=true,challenge=true},old_place={preserve=true,extract=true,develop=true,avoid=true},knowledge={share=true,ask=true,private=true,future=true},bargain={terms=true,courtesy=true,concession=true,fixed=true}}
 if not allowed[theme][action] then return false,'Invalid protocol action' end
 if theme=='gift' and action=='reciprocate' then local item
  for _,p0 in ipairs(site.world.items) do if p0.kind=='food' and p0.n>0 and not p0.reserved then item=p0;break end end
  if not item then return false,'One accessible food unit is required' end;item.n=item.n-1
 elseif theme=='knowledge' and action=='share' then
  local facts=a.frontier and a.frontier.facts or {};if #facts==0 then return false,'Representative needs a firsthand operational fact' end
 end
 local dt,dr,dte,dg,mit=reaction(f,theme,action,a);change(f.relations[1],dt,dr,dte,dg)
 local fam=F.addFamiliarity(a,f.id,5,c.tick);fam.protocolExperienceCount=math.min(1000,fam.protocolExperienceCount+1)
 addIncident(c.factions,{tick=c.tick,kind='protocol_'..theme,factionId=f.id,personId=a.personId,theme=theme,action=action,deltas={trust=dt,respect=dr,tension=dte,grievance=dg},mitigation=mit})
 p.representatives[#p.representatives+1]=a.personId;p.index=p.index+1
 if p.index>#p.steps then f.contact.established=true;F.addFamiliarity(a,f.id,10,c.tick,true);f.contact.protocol=nil;addIncident(c.factions,{tick=c.tick,kind='first_contact_complete',factionId=f.id,personId=a.personId}) end
 return true
end
local function background(c,f)
 f.turns=f.turns+1;local e=f.economy;setstock(f,'food',stock(f,'food')+math.floor(e.agriculture/8)-math.ceil(f.population/12));setstock(f,'minerals',stock(f,'minerals')+math.floor(e.extraction/10))
 if stock(f,'components')<80 and stock(f,'minerals')>=4 then setstock(f,'minerals',stock(f,'minerals')-4);setstock(f,'components',stock(f,'components')+math.max(1,math.floor(e.manufacturing/25))) end
 if stock(f,'food')<30 then f.population=math.max(20,f.population-1) elseif stock(f,'food')>600 and f.turns%5==0 then f.population=math.min(80,f.population+1) end
 f.strategicGoal=goal(f)
end
local function interactions(c)
 local state=c.factions;local f=state.factions[((math.floor(c.tick/1000)-1)%4)+1];local other=state.factions[(f.id%4)+1];if other==f then other=state.factions[1] end
 local fr,orr=f.relations[other.id],other.relations[f.id];local k
 if stock(f,'food')<80 and stock(other,'food')>300 and fr.trust>=35 then setstock(f,'food',stock(f,'food')+20);setstock(other,'food',stock(other,'food')-20);change(fr,3,1,-2,0);change(orr,2,1,-1,0);k='external_trade'
 elseif f.culture.scholarship>70 and other.culture.scholarship>70 then change(fr,2,2,-1,0);change(orr,2,2,-1,0);k='knowledge_exchange'
 else change(fr,-1,0,4,2);change(orr,-1,0,4,2);k='external_dispute' end
 addIncident(state,{tick=c.tick,kind=k,factionId=f.id,otherFactionId=other.id})
end
local function offer(c,f)
 if not f.contact.established or attitude(f.relations[1])=='HOSTILE' then return end
 local active=0;for _,o in ipairs(f.offers) do if not o.expired then active=active+1 end end;if active>=3 then return end
 local give,want=(stock(f,'food')>150 and 'food' or 'metal'),(f.strategicGoal=='acquire_minerals' and 'metal' or 'food')
 if give==want then want='rock' end;if stock(f,give)==0 then return end
 local state=c.factions;local o={id=state.nextOfferId,version=1,factionId=f.id,createdTick=c.tick,expiresTick=c.tick+3000,requested={{kind=want,n=1}},promised={{kind=give,n=1}},accepted=false,reserved=true};state.nextOfferId=state.nextOfferId+1;setstock(f,give,stock(f,give)-1);f.reserved[give]=(f.reserved[give] or 0)+1;f.offers[#f.offers+1]=o
end
function F.step(c)
 if not F.enabled(c) then return end;local state=c.factions
 -- Relay scans are global institutional progress; a second relay is no faster.
 local powered=false;for _,site in ipairs(require('src.campaign').sites(c)) do if site.world and F.relay(site.world) then powered=true;break end end
 if powered then state.scan.progress=state.scan.progress+1;if state.scan.progress>=300 then state.scan.progress=0;for _,id in ipairs(state.scan.order) do if not state.scan.known[id] then state.scan.known[id]=true;F.find(c,id).contact.signal=true;addIncident(state,{tick=c.tick,kind='signal_detected',factionId=id});break end end end end
 for _,f in ipairs(state.factions) do local a=f.contact.audience;if a then local rep=representative(c,a.siteId,a.personId);if rep and F.relay(require('src.campaign').site(c,a.siteId).world) then a.progress=a.progress+1;if a.progress>=60 then f.contact.audience=nil;f.contact.protocol=protocol(f);F.addFamiliarity(rep,f.id,10,c.tick,true);addIncident(state,{tick=c.tick,kind='audience_ready',factionId=f.id,personId=rep.personId}) end end end end
 if c.tick%200==0 then for _,f in ipairs(state.factions) do background(c,f) end end
 if c.tick%1000==0 then interactions(c);for _,f in ipairs(state.factions) do offer(c,f) end end
 for _,shipment in ipairs(state.shipments) do if shipment.status=='in_flight' and c.tick>=shipment.arrivalTick then
  local site=require('src.campaign').site(c,shipment.siteId);local depot=site and site.world and F.depot(site.world,shipment.depotId)
  if depot and F.relay(site.world) and units(depot.trade.cargo)+units(shipment.bundle)<=24 then
   for _,b in ipairs(shipment.bundle) do assert(addCargo(depot.trade.cargo,b,24),'Shipment capacity changed') end;shipment.status='delivered';totalsAdd(state.totals.imports,shipment.bundle);state.receipts[#state.receipts+1]={id=shipment.id,tick=c.tick,kind='import',siteId=shipment.siteId,bundle=shipment.bundle};while #state.receipts>128 do table.remove(state.receipts,1) end;addIncident(state,{tick=c.tick,kind='trade_arrived',factionId=shipment.factionId,shipmentId=shipment.id})
  else shipment.status='awaiting_depot' end
 end end
 for _,f in ipairs(state.factions) do for _,o in ipairs(f.offers) do if not o.expired and not o.accepted and c.tick>=o.expiresTick then o.expired=true;for _,b in ipairs(o.promised) do setstock(f,b.kind,stock(f,b.kind)+b.n);f.reserved[b.kind]=math.max(0,(f.reserved[b.kind] or 0)-b.n) end end end end
end
function F.validate(c)
 local s=c.factions;assert(type(s)=='table' and s.version==1,'Invalid factions state');assert(#s.factions==4,'G05 needs exactly four external factions');assert(#s.scan.order==4,'Invalid contact order')
 local ids={};for _,f in ipairs(s.factions) do assert(not ids[f.id] and f.id>=2 and f.id<=5,'Invalid faction ID');ids[f.id]=true;assert(type(f.name)=='string' and #f.name<=64,'Invalid faction name');assert(#f.norms>=3 and #f.norms<=4,'Invalid faction norms');for _,a in ipairs(axes) do U.integer(f.culture[a],'culture '..a,0,100) end;U.integer(f.population,'faction population',20,80);local stocks={'food','minerals','components','tools','influence'};if c.features.security==1 then stocks[#stocks+1]='metal' end;for _,k in ipairs(stocks) do U.integer(f.stocks[k],'faction stock',0,k=='components' and 500 or k=='tools' and 128 or 1000) end;for id,r in pairs(f.relations) do U.integer(id,'relation target',1,5);assert(id~=f.id,'Self relation');for _,k in ipairs({'trust','respect','tension','grievance'}) do U.integer(r[k],'relation '..k,0,100) end end end
 assert(#s.incidents<=128 and #s.shipments<=16 and #s.receipts<=128,'Faction bounds exceeded');return true
end
return F
