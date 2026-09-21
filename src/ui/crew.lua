local L=require('src.labor')
local W=require('src.world')
local U=require('src.util')
local UI={}
local colors={bg={0.035,0.048,0.06},panel={0.07,0.09,0.105},edge={0.18,0.23,0.27},text={0.87,0.9,0.87},muted={0.5,0.6,0.64},cyan={0.35,0.76,0.76},amber={0.9,0.68,0.36},red={0.85,0.39,0.34}}
local function box(x,y,w,h,c) love.graphics.setColor(c);love.graphics.rectangle('fill',x,y,w,h) end
local function text(s,x,y,c,font)love.graphics.setColor(c or colors.text);love.graphics.setFont(font);love.graphics.print(s,x,y)end
local function wrap(s,x,y,width,c,font)love.graphics.setColor(c);love.graphics.setFont(font);love.graphics.printf(s,x,y,width,'left')end
function UI.open(app)
 app.crew={draft=L.snapshot(app.currentWorld()),section='people',row=1,col=0,buttons={},readonly=not app.history:atPresent() and app.liveWorld().mode=='challenge'}
 app.drag=nil
end
local function cycleRole(person)
 local list={'auto','general','dig','build','haul','farm','pump','field'};local at=1
 for i,r in ipairs(list) do if r==person.role then at=i end end
 person.role=list[at%#list+1]
end
local function cell(c,shift)
 local person=c.draft.people[c.row];if not person then return end
 if c.col==0 then cycleRole(person)
 else
  local role=L.duties[c.col]
  if shift then person.role=role;person.prefs[role]=math.max(2,person.prefs[role])
  else person.prefs[role]=(person.prefs[role]+1)%4 end
 end
end
function UI.key(app,key,queue)
 local c=app.crew
 if key=='escape' or key=='h' then app.crew=nil;return end
 if key=='return' or key=='kpenter' then
  if c.readonly then c.error='Archive is read-only. End returns to your live colony.';return end
  if queue({type='labor',plan=c.draft}) then app.crew=nil end
  return
 end
 if key=='tab' then
  local psychology=app.currentWorld().frontier and app.currentWorld().frontier.psychology==1
  c.section=c.section=='people' and 'quotas' or c.section=='quotas' and psychology and 'mind' or 'people';c.row=1;return
 end
 if c.readonly then return end
 if c.section=='people' then
  if key=='up' then c.row=math.max(1,c.row-1)
  elseif key=='down' then c.row=math.min(#c.draft.people,c.row+1)
  elseif key=='left' then c.col=math.max(0,c.col-1)
  elseif key=='right' then c.col=math.min(#L.duties,c.col+1)
  elseif key=='space' then cell(c,love.keyboard.isDown('lshift','rshift'))
  elseif key:match('^[0-3]$') and c.col>0 then c.draft.people[c.row].prefs[L.duties[c.col]]=tonumber(key)
  elseif key=='r' then
   local a=c.draft.people[c.row];a.role='auto';for _,r in ipairs(L.duties) do a.prefs[r]=2 end
  end
 elseif c.section=='quotas' then
  if key=='up' then c.row=math.max(1,c.row-1)
  elseif key=='down' then c.row=math.min(#L.roles,c.row+1)
  elseif key=='=' or key=='+' or key=='right' then c.draft.weights=L.adjust(c.draft.weights,L.roles[c.row],5)
  elseif key=='-' or key=='left' then c.draft.weights=L.adjust(c.draft.weights,L.roles[c.row],-5)
  elseif key=='q' or key=='space' then c.draft.quotas=not c.draft.quotas
  elseif key:match('^[1-4]$') then
   local p=({{34,33,33,0,0,0,0},{0,50,25,25,0,0,0},{25,0,0,0,50,25,0},{40,0,0,0,0,0,60}})[tonumber(key)]
   for i,r in ipairs(L.roles) do c.draft.weights[r]=p[i] end;c.draft.quotas=true
  end
 else
  if key=='up' then c.row=math.max(1,c.row-1)
  elseif key=='down' then c.row=math.min(#c.draft.people,c.row+1) end
 end
end
function UI.mouse(app,x,y,queue)
 local c=app.crew
 for _,b in ipairs(c.buttons or {}) do if x>=b.x and y>=b.y and x<=b.x+b.w and y<=b.y+b.h then
  if b.action=='tab' then UI.key(app,'tab',queue)
  elseif b.action=='apply' then UI.key(app,'return',queue)
  elseif not c.readonly then
   if b.action=='cell' then c.row=b.row;c.col=b.col;cell(c,love.keyboard.isDown('lshift','rshift'))
   elseif b.action=='toggle' then c.draft.quotas=not c.draft.quotas
   elseif b.action=='quota' then c.row=b.row;c.draft.weights=L.adjust(c.draft.weights,L.roles[b.row],b.delta) end
  end
  return
 end end
end
function UI.draw(app,r)
 local c=app.crew;if not c then return end
 local w=app.currentWorld();local sw,sh=love.graphics.getDimensions()
 local pw,ph=math.min(1060,sw-40),math.min(670,sh-40);local x,y=(sw-pw)/2,(sh-ph)/2
 box(0,0,sw,sh,colors.bg);box(x,y,pw,ph,colors.panel)
 text('CREW / '..(c.section=='people' and 'INDIVIDUAL DUTIES' or c.section=='quotas' and 'WORKFORCE SPLIT' or 'MIND AND RELATIONSHIPS'),x+22,y+18,colors.amber,r.sub)
 text('Enter queues changes   Escape cancels   Tab changes page   Simulation continues',x+24,y+57,colors.muted,r.small)
 c.buttons={{action='tab',x=x+pw-170,y=y+20,w=145,h=28},{action='apply',x=x+pw-140,y=y+ph-45,w=115,h=28}}
 box(x+pw-170,y+20,145,28,colors.edge);text('TAB / switch',x+pw-160,y+26,colors.cyan,r.small)
 if c.section=='people' then
  local first=x+220;local cw=(pw-250)/6;local header=y+101
  text('Worker',x+24,header,colors.muted,r.small);text('Pinned role',x+124,header,colors.muted,r.small)
  for i,role in ipairs(L.duties) do text(L.labels[role],first+(i-1)*cw,header,colors.muted,r.small) end
  for row,p in ipairs(c.draft.people) do
   local a=W.find(w.workers,p.id);local yy=y+130+(row-1)*34
   if row==c.row then box(x+16,yy-3,pw-32,32,colors.edge) end
   local mental=a.psychology and (' ['..require('src.psychology').band(a)..']') or ''
   text(a.name..(a.alive and '' or ' [dead]')..mental,x+24,yy+4,a.alive and colors.text or colors.red,r.small)
   text(p.role,x+124,yy+4,c.col==0 and row==c.row and colors.amber or colors.cyan,r.small)
   c.buttons[#c.buttons+1]={action='cell',row=row,col=0,x=x+116,y=yy,w=96,h=29}
   for col,role in ipairs(L.duties) do
    local xx=first+(col-1)*cw;local value=p.prefs[role]
    if row==c.row and col==c.col then box(xx-3,yy,cw-8,28,colors.bg) end
    local label=({'OFF','1 low','2 normal','3 high'})[value+1]
    text(label,xx+3,yy+4,value==0 and colors.red or value==3 and colors.amber or colors.cyan,r.small)
    c.buttons[#c.buttons+1]={action='cell',row=row,col=col,x=xx-3,y=yy,w=cw-8,h=28}
   end
  end
  local yy=y+145+#c.draft.people*34
  wrap('Click a cell or use arrows and Space. 0-3 sets priority. Shift-click a duty pins that role. R resets the selected worker. Auto workers enter the quota pool; pinned workers stay outside it.',x+24,yy,pw-48,colors.muted,r.small)
 elseif c.section=='quotas' then
  box(x+22,y+93,260,30,colors.edge);text('Q: quotas '..(c.draft.quotas and 'ENABLED' or 'DISABLED'),x+32,y+100,c.draft.quotas and colors.cyan or colors.amber,r.normal)
  c.buttons[#c.buttons+1]={action='toggle',x=x+22,y=y+93,w=260,h=30}
  local assignments,counts=L.allocate(w,c.draft)
  for row,role in ipairs(L.roles) do
   local yy=y+143+(row-1)*37
   if c.row==row then box(x+16,yy-3,pw-32,33,colors.edge) end
   text(L.labels[role],x+28,yy+4,colors.text,r.normal)
   text(string.format('%3d%%',c.draft.weights[role]),x+235,yy+4,colors.cyan,r.normal)
   text(string.format('%d AUTO worker slots',counts[role] or 0),x+375,yy+4,colors.muted,r.small)
   for i,delta in ipairs({-5,5}) do
    local xx=x+290+(i-1)*34;box(xx,yy,28,26,colors.bg);text(delta<0 and '-' or '+',xx+9,yy+3,colors.amber,r.normal)
    c.buttons[#c.buttons+1]={action='quota',row=row,delta=delta,x=xx,y=yy,w=28,h=26}
   end
  end
  wrap('Up/Down selects; +/- moves five percentage points. Total stays 100. Q enables quotas. Presets: 1 balanced, 2 expansion, 3 subsistence, 4 field expedition. Percentages apply only to living AUTO workers. Disabled duties can leave a slot unfilled.',x+24,y+421,pw-48,colors.muted,r.small)
 else
  local p=c.draft.people[c.row];local a=p and W.find(w.workers,p.id)
  if a and a.psychology then
   local Psychology=require('src.psychology');local mind=Psychology.describe(a)
   text(a.name..(a.alive and '' or ' [dead]'),x+24,y+102,a.alive and colors.text or colors.red,r.normal)
   text('Currently '..mind.mind..'.',x+24,y+136,mind.mind=='Panicked' and colors.red or mind.mind=='Steady' and colors.cyan or colors.amber,r.normal)
   local current=a.reason and a.reason~='' and a.reason or 'No immediate concern.'
   wrap('Right now: '..current,x+24,y+166,pw-48,colors.muted,r.small)
   text('PERSONAL STYLE',x+24,y+214,colors.amber,r.small)
   wrap(table.concat(mind.traits,', ')..'.',x+24,y+238,pw-48,colors.text,r.small)
   local ambition=mind.ambition:gsub('_',' ')
   wrap('Personal aim: '..ambition..(mind.complete and ' (fulfilled).' or '.'),x+24,y+278,pw-48,colors.cyan,r.small)
   text('RECENT MEMORIES',x+24,y+326,colors.amber,r.small)
   local yy=y+350;local shown=0
   for i=#a.psychology.memories,1,-1 do local m=a.psychology.memories[i];shown=shown+1
    local label=m.kind:gsub('_',' ')
    wrap((m.core and 'Lasting: ' or '')..label..'.',x+30,yy,pw*.52, m.valence<0 and colors.amber or colors.cyan,r.small);yy=yy+27
    if shown>=5 then break end
   end
   if shown==0 then text('No lasting memories yet.',x+30,yy,colors.muted,r.small) end
   text('RELATIONSHIPS',x+pw*.60,y+326,colors.amber,r.small)
   local ry=y+350;local relationShown=0
   for _,rel in ipairs(a.psychology.relations) do
    local other
    for _,candidate in ipairs(w.workers) do if candidate.personId==rel.personId then other=candidate end end
    local name=other and other.name or ('Person '..rel.personId)
    local feeling=rel.resentment>=45 and 'feels wary of' or rel.trust>=35 and 'trusts' or rel.affection>=35 and 'cares about' or rel.respect>=35 and 'respects' or 'knows'
    wrap(name..': '..feeling..' them.',x+pw*.60,ry,pw*.34,colors.text,r.small);ry=ry+27;relationShown=relationShown+1
    if relationShown>=5 then break end
   end
   if relationShown==0 then text('Still getting to know the crew.',x+pw*.60,ry,colors.muted,r.small) end
   wrap('Use Up/Down to view another person. These are personal explanations; management orders remain yours to give.',x+24,y+ph-112,pw-48,colors.amber,r.small)
  else wrap('This recorded campaign predates the Mind system. Its people keep their original behaviour.',x+24,y+110,pw-48,colors.muted,r.normal) end
 end
 local allocation=L.allocate(w,c.draft);local line={}
 for _,a in ipairs(w.workers) do if a.alive then line[#line+1]=a.name..': '..(L.labels[allocation[a.id]] or allocation[a.id] or 'General') end end
 wrap('Effective allocation: '..table.concat(line,' | '),x+24,y+ph-138,pw-48,colors.cyan,r.small)
 wrap(c.readonly and 'READ-ONLY ARCHIVE: return to the live frontier to issue orders.' or '0 forbids a duty. A named order bypasses a role/quota, not a forbidden duty. Food, rest and immediate danger still take precedence. Quotas assign people, not percentages of elapsed work time.',x+24,y+ph-96,pw-48,colors.amber,r.small)
 box(x+pw-140,y+ph-45,115,28,colors.edge);text('ENTER apply',x+pw-130,y+ph-39,colors.cyan,r.small)
 if c.error then text(c.error,x+24,y+ph-36,colors.red,r.small) end
end
return UI
