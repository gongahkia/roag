-- Offline reader/planner for the passive observer. Emits text for xdotool driver.
local f=assert(io.open(assert(arg[1]),'rb'));local v=require('src.persistence').decode(f:read('*a'));f:close()
local s=v.state
if v.busy then print('busy');return end
if v.page~='game'then print(v.page);return end
if not s.boggle then print(s.card_trial and s.card_trial.mode or 'classic',s.clock);return end
local phase=s.card_trial.phase
if phase=='edit'then
 for i,t in ipairs(s.boggle.board)do local d=require('src.boggle_content').types[t.type]
  if d.dir and not d.finite and s.boggle.pool[t.type]>0 then print('edit',i);return end
 end
end
if phase~='combat'then print(phase);return end
if arg[2]=='--status'then print('combat',s.clock,require('src.game').player(s).hp);return end
local ids=require('tests.boggle_policy').choose(s);assert(ids,'no legal route')
local cells={};for _,id in ipairs(ids)do cells[#cells+1]=assert(require('src.boggle').cell(s,id))end
print('route',table.concat(cells,','),s.clock,require('src.game').player(s).hp)
