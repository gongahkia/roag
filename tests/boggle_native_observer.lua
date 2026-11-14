-- Passive native-input observer, loaded only by the disposable GUI test app.
-- No actor/board edits and no input callbacks are injected here.
local G=require('src.game')
local T=require('src.card_trial')
local B=require('src.boggle')
local Codec=require('src.persistence')
local out=assert(os.getenv('ROAG_BOGGLE_NATIVE_OUTPUT'))
local current,signature
local receipts={}
local new,restore=T.new,G.restore
function T.new(...)local s=new(...);current=s;return s end
function G.restore(...)local s=restore(...);current=s;return s end
assert(love.filesystem.load('roag_main.lua'))()
local update,quit,keypressed=love.update,love.quit,love.keypressed
local function write(name,value)
 local f=assert(io.open(out..'/'..name..'.tmp','wb'));f:write(Codec.encode(value));f:close();assert(os.rename(out..'/'..name..'.tmp',out..'/'..name))
end
function love.update(dt)
 update(dt)
 local view=love.roagUIState().page
 local busy=love.roagPlaybackPending()
 local stamp=current and (current.boggle and B.stamp(current)or current.next_action_id..':'..current.clock)or 'none'
 local sig=stamp..':'..view..':'..tostring(busy)
 if signature~=sig then
  signature=sig;write('native-state.roag',{state=current and G.snapshot(current),page=view,busy=busy})
  if not busy then
   write('native-metrics.roag',require('src.boggle_ui').metrics)
   if current and current.boggle and view=='game'then receipts[#receipts+1]=G.snapshot(current);write('native-actions.roag',receipts)end
  end
 end
end
function love.keypressed(key,...)
 if key=='f12'then love.event.quit()else return keypressed(key,...)end
end
function love.quit()
 write('native-metrics.roag',require('src.boggle_ui').metrics)
 if quit then return quit()end
end
