local U=require('src.util')
local Cat=require('src.catalog')
local Knowledge=require('src.knowledge')
local UI={}

local function observer(app,w)
 local people={};for _,worker in ipairs(w.workers) do if worker.alive then people[#people+1]=worker end end
 table.sort(people,function(a,b) return a.personId<b.personId end)
 local id=app.fieldnotes.observerId
 for _,worker in ipairs(people) do if worker.id==id then return worker,people end end
 for _,worker in ipairs(people) do if worker.id==app.selectedWorker then app.fieldnotes.observerId=worker.id;return worker,people end end
 local worker=people[1];app.fieldnotes.observerId=worker and worker.id or nil;return worker,people
end
local function legacyEntries(w)
 local entries={};local content=w.content
 if content then
  local keys={};for key in pairs(content.observed) do keys[key]=true end;for key in pairs(content.discoveries) do keys[key]=true end
  for _,key in ipairs(U.keys(keys)) do
   local record=content.discoveries[key] or content.observed[key];local known=content.discoveries[key]
   local registry=record.category=='flora' and Cat.flora or record.category=='fauna' and Cat.fauna or Cat.sites;local definition=registry[record.kind]
   entries[#entries+1]={title=known and definition.name or ('Unclassified '..record.category),note=known and definition.note or 'Sighted but not surveyed. Its behaviour must be observed at a reachable location.',tick=record.tick,known=known}
  end
 end
 return entries
end
local function personalEntries(worker)
 local entries={};if not worker then return entries end
 local personal=worker.frontier.knowledge
 for _,record in ipairs(personal.observations) do
  local source=record.source;local title=Knowledge.label(worker,source.category,source.kind);local note=Knowledge.note(worker,source.category,source.kind)
  local evidence={};for _,effect in ipairs(record.effects) do if #effect.ticks>0 then evidence[#evidence+1]=effect.kind:gsub('_',' ')..' at ticks '..table.concat(effect.ticks,', ') end end
  if #evidence>0 then note=note..' Firsthand observations: '..table.concat(evidence,'; ')..'.' end
  entries[#entries+1]={title=title,note=note,tick=record.lastSeenTick,known=Knowledge.identified(worker,source.category,source.kind)}
 end
 for _,record in ipairs(personal.studies) do
  local spec=Knowledge.spec(record.id);entries[#entries+1]={title='Field study / '..spec.label,note='Personal progress '..record.progress..'/120. Work remains tied to this researcher and supported observations.',tick=record.lastTick,known=true}
 end
 table.sort(entries,function(a,b) if a.tick~=b.tick then return a.tick<b.tick end return a.title<b.title end)
 return entries
end
function UI.cycleObserver(app,delta)
 local w=app.currentWorld();if not (w.frontier and w.frontier.knowledge==1) then return end
 local _,people=observer(app,w);if #people==0 then return end
 local at=1;for i,worker in ipairs(people) do if worker.id==app.fieldnotes.observerId then at=i;break end end
 app.fieldnotes.observerId=people[(at-1+delta)%#people+1].id;app.fieldnotes.scroll=1
end
function UI.draw(app,r)
 if not app.fieldnotes then return end
 local w=app.currentWorld();local sw,sh=love.graphics.getDimensions()
 local pw,ph=math.min(960,sw-40),math.min(640,sh-40);local x,y=(sw-pw)/2,(sh-ph)/2
 love.graphics.setColor(0.035,0.048,0.06,0.96);love.graphics.rectangle('fill',0,0,sw,sh)
 love.graphics.setColor(0.07,0.09,0.105,1);love.graphics.rectangle('fill',x,y,pw,ph)
 local enabled=w.frontier and w.frontier.knowledge==1
 love.graphics.setFont(r.sub);love.graphics.setColor(0.90,0.68,0.36);love.graphics.print(enabled and 'FIELD NOTES / PERSONAL OBSERVATION' or 'FIELD NOTES / OBSERVATION, NOT OMNISCIENCE',x+24,y+22)
 local current,people=nil,{}
 if enabled then current,people=observer(app,w) end
 love.graphics.setFont(r.normal);love.graphics.setColor(0.7,0.8,0.82)
 local guidance=enabled and ('F4 or Escape closes. Left/Right selects a local observer. U surveys; the visible Study tool designates evidence-backed fieldwork. Knowledge stays with people and is not teaching.') or 'F4 or Escape closes. Up/Down scrolls. U designates a survey; a worker must reach and observe the target. Ruins, growths and creatures obey fixed rules, but their descriptions begin unknown.'
 love.graphics.printf(guidance,x+24,y+68,pw-48,'left')
 if enabled then
  love.graphics.setColor(0.45,0.78,0.76);love.graphics.setFont(r.small)
  love.graphics.print(current and ('Observer: '..current.name..' / person '..current.personId..' ('..#people..' living local)') or 'No living local observer.',x+24,y+112)
 end
 local entries=enabled and personalEntries(current) or legacyEntries(w)
 if #entries==0 then entries[1]={title=enabled and 'No personal encounters recorded' or (w.content and 'No encounters recorded' or 'Legacy expedition'),note=enabled and 'Witness a nearby, unobstructed encounter or complete a survey. Historical findings do not make an absent expert locally capable.' or (w.content and 'Explore beyond the arrival chamber. Sightings require proximity and an unobstructed view.' or 'This saved terrain has no seeded ecology. Crew controls and charges still work; N starts a living frontier.'),tick=w.tick} end
 local top=enabled and 148 or 137;local visible=math.max(1,math.floor((ph-top-27)/94));app.fieldnotes.scroll=U.clamp(app.fieldnotes.scroll or 1,1,math.max(1,#entries-visible+1))
 for i=app.fieldnotes.scroll,math.min(#entries,app.fieldnotes.scroll+visible-1) do
  local row=entries[i];local yy=y+top+(i-app.fieldnotes.scroll)*94
  love.graphics.setColor(0.18,0.23,0.27);love.graphics.line(x+24,yy-6,x+pw-24,yy-6)
  love.graphics.setColor(row.known and 0.45 or 0.65,0.78,0.76);love.graphics.setFont(r.normal)
  love.graphics.print(row.title..' / tick '..row.tick,x+24,yy)
  love.graphics.setFont(r.small);love.graphics.setColor(0.8,0.85,0.84);love.graphics.printf(row.note,x+24,yy+29,pw-48,'left')
 end
end
return UI
