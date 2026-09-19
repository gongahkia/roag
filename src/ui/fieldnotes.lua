local U=require('src.util')
local Cat=require('src.catalog')
local UI={}
function UI.draw(app,r)
 if not app.fieldnotes then return end
 local w=app.history.view;local sw,sh=love.graphics.getDimensions()
 local pw,ph=math.min(960,sw-40),math.min(640,sh-40);local x,y=(sw-pw)/2,(sh-ph)/2
 love.graphics.setColor(0.035,0.048,0.06,0.96);love.graphics.rectangle('fill',0,0,sw,sh)
 love.graphics.setColor(0.07,0.09,0.105,1);love.graphics.rectangle('fill',x,y,pw,ph)
 love.graphics.setFont(r.sub);love.graphics.setColor(0.90,0.68,0.36);love.graphics.print('FIELD NOTES / OBSERVATION, NOT OMNISCIENCE',x+24,y+22)
 love.graphics.setFont(r.normal);love.graphics.setColor(0.7,0.8,0.82)
 love.graphics.printf('F4 or Escape closes. Up/Down scrolls. U designates a survey; a worker must reach and observe the target. Ruins, growths and creatures obey fixed rules, but their descriptions begin unknown.',x+24,y+68,pw-48,'left')
 local entries={};local e=w.content
 if e then
  local keys={};for key in pairs(e.observed) do keys[key]=true end;for key in pairs(e.discoveries) do keys[key]=true end
  for _,key in ipairs(U.keys(keys)) do
   local p=e.discoveries[key] or e.observed[key];local known=e.discoveries[key]
   local registry=p.category=='flora' and Cat.flora or p.category=='fauna' and Cat.fauna or Cat.sites
   local d=registry[p.kind]
   entries[#entries+1]={title=known and d.name or ('Unclassified '..p.category),note=known and d.note or 'Sighted but not surveyed. Its behaviour must be observed at a reachable location.',tick=p.tick,known=known}
  end
 end
 if #entries==0 then entries[1]={title=e and 'No encounters recorded' or 'Legacy expedition',note=e and 'Explore beyond the arrival chamber. Sightings require proximity and an unobstructed view.' or 'This saved terrain has no seeded ecology. Crew controls and charges still work; N starts a living frontier.',tick=w.tick} end
 local visible=math.max(1,math.floor((ph-164)/94));app.fieldnotes.scroll=U.clamp(app.fieldnotes.scroll or 1,1,math.max(1,#entries-visible+1))
 for i=app.fieldnotes.scroll,math.min(#entries,app.fieldnotes.scroll+visible-1) do
  local row=entries[i];local yy=y+137+(i-app.fieldnotes.scroll)*94
  love.graphics.setColor(0.18,0.23,0.27);love.graphics.line(x+24,yy-6,x+pw-24,yy-6)
  love.graphics.setColor(row.known and 0.45 or 0.65,0.78,0.76);love.graphics.setFont(r.normal)
  love.graphics.print(row.title..' / tick '..row.tick,x+24,yy)
  love.graphics.setFont(r.small);love.graphics.setColor(0.8,0.85,0.84);love.graphics.printf(row.note,x+24,yy+29,pw-48,'left')
 end
end
return UI
