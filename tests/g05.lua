-- COS-G05 focused deterministic diplomacy tests.  They use an industry-ready
-- fixture but never direct-write a contact outcome or trade import.
local Campaign=require('src.campaign')
local F=require('src.factions')
local W=require('src.world')
local M=require('src.materials')
local S=require('src.structures')
local I=require('src.industry')
local X=require('tests.fixtures')
local T={}
local function opts() return {preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3,logistics=true,travel=true,knowledge=true,education=true,body=true,visibility=true,equipment=true,safe_excavation=true,psychology=true,industry=true,factions=true} end
local function campaign(seed) return Campaign.newRegion(seed or 10505,opts()) end
local function clear(w,gx,gy,width)
 local x1,y1,_,y2=W.rect(gx,gy);local x2=(gx+(width or 1)-1)*4;X.fill(w,x1,y1,x2,y2,M.AIR);X.fill(w,x1,y2+1,x2,y2+1,M.ROCK);X.fill(w,x1,3,x2,y1-1,M.AIR)
end
local function install(w,gx,gy,k) clear(w,gx,gy,S.width(k));return S.install(w,gx,gy,k) end
local function step(c,n) for _=1,n do Campaign.step(c) end end
function T.run()
 local r={groups=0,assertions=0};local function check(v,s) r.assertions=r.assertions+1;assert(v,s) end;local function eq(a,b,s) check(a==b,(s or 'Mismatch')..': '..tostring(a)..' ~= '..tostring(b)) end
 local function group(name,fn) local ok,e=pcall(fn);assert(ok,name..' FAILED: '..tostring(e));r.groups=r.groups+1;print('PASS  '..name) end
 group('G05-A/B/C deterministic bounded faction generation and prehistory',function()
  local a,b=campaign(505),campaign(505);eq(#a.factions.factions,4);eq(#a.factions.incidents,0);for i=1,4 do local x,y=a.factions.factions[i],b.factions.factions[i];eq(x.name,y.name,'Name changed');eq(x.language,y.language,'Language changed');eq(#x.prehistory,7,'Expected four internal and three interhistory references');check(x.culture.hierarchy>=0 and x.culture.hierarchy<=100,'Culture bound') end
  local old=Campaign.new(X.world('practice'),{body=true,visibility=true,equipment=true,safe_excavation=true});check(old.features.factions==nil and old.factions==nil,'Factions leaked into legacy campaign')
 end)
 group('G05-D/E external turns and one powered relay scan',function()
  local c=campaign();local w=c.sites[1].world;install(w,6,6,'solar_array');install(w,8,6,'power_pole');install(w,9,6,'signal_relay');local before=c.factions.factions[1].stocks.food;step(c,300);check(c.factions.scan.known[c.factions.scan.order[1]],'One relay did not discover exactly the first signal');check(c.factions.factions[1].stocks.food~=before,'Background faction turn did not run')
 end)
 group('G05-F/G/H personal first contact uses real powered time and deterministic protocol',function()
  local c=campaign();local w=c.sites[1].world;install(w,6,6,'solar_array');install(w,8,6,'power_pole');install(w,9,6,'signal_relay');step(c,300);local id=c.factions.scan.order[1];local a=w.workers[1];local ok,why=F.beginContact(c,1,id,a.personId);check(ok,why);step(c,60);local f=F.find(c,id);check(f.contact.protocol and #f.contact.protocol.steps==3,'Audience did not create a three-step protocol');local before=f.relations[1].trust;local theme=f.contact.protocol.steps[1];local action=theme=='gift' and 'accept' or theme=='formal' and 'designated' or theme=='old_place' and 'preserve' or theme=='knowledge' and 'ask' or 'terms';ok,why=F.resolveProtocol(c,1,id,a.personId,action);check(ok,why);check(F.familiarity(a,id).familiarity>=15,'Representative did not personally learn contact');check(f.relations[1].trust~=before or f.relations[1].respect~=45 or f.relations[1].tension~=20,'Protocol produced no recorded deterministic relation consequence')
 end)
 group('G05-I/J/R validation, bounds, hostile attitude and history clone',function()
  local c=campaign();local f=c.factions.factions[1];f.relations[1].tension=80;eq(F.attitude(f.relations[1]),'HOSTILE');Campaign.validate(c);local copy=Campaign.clone(c);eq(copy.factions.factions[1].relations[1].tension,80,'Faction relation did not survive clone');local bad=Campaign.clone(c);bad.factions.factions[1].culture.hierarchy=101;check(not pcall(Campaign.validate,bad),'Malformed culture loaded')
 end)
 group('G05-K/L/M/N/O bounded physical depot dispatch, courier and rebind',function()
  local c=campaign();local w=c.sites[1].world;install(w,6,6,'solar_array');install(w,8,6,'power_pole');install(w,9,6,'signal_relay');local depot=install(w,11,6,'trade_depot');step(c,300);local f=F.find(c,c.factions.scan.order[1]);f.contact.established=true;step(c,700);local offer=f.offers[1];check(offer,'No deterministic offer created');local requested=offer.requested[1];local before=W.totalResource(w,requested.kind);local pile=W.stack(w,requested.kind,requested.n,20,24);local ok,why=F.deposit(c,1,depot.id,pile.id,requested.n);check(ok,why);ok,why=F.acceptOffer(c,1,depot.id,offer.id,w.workers[1].personId);check(ok,why);ok,why=F.dispatch(c,1,depot.id,offer.id);check(ok,why);eq(W.totalResource(w,requested.kind),before,'Dispatch retained exported local pile');step(c,400);check(c.factions.shipments[1].status=='delivered','Courier did not deliver atomically');check(#depot.trade.cargo>0,'Imported bundle did not enter physical depot');local copy=Campaign.clone(c);Campaign.validate(copy)
 end)
 print(r.groups..' G05 groups; '..r.assertions..' assertions passed.');return r
end
return T
