-- MOCK UI evidence only: this exercises the live Security page and its real
-- command envelopes.  It intentionally makes no native LÖVE or human-play
-- claim.
local directory=arg and arg[1] or '/tmp/cosmonauts-g06-gui'
assert(directory:match('^[%w%_/%-%.]+$'),'Unsafe test directory')
local mock=require('tests.love_mock').install(directory)
local Campaign=require('src.campaign')
local History=require('src.campaign_history')
local Equipment=require('src.equipment')
local World=require('src.world')
local Commands=require('src.campaign_commands')
local main=require('main');local app=main.app

local function hasText(needle)
 for _,record in ipairs(mock.records) do if record.kind=='text' and record.text:find(needle,1,true) then return true end end
 return false
end
local function advance(n) for _=1,n do assert(app.history:advance()) end end

love.load()
local c=Campaign.newRegion(10606,{preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3,logistics=true,travel=true,knowledge=true,education=true,body=true,visibility=true,equipment=true,safe_excavation=true,psychology=true,industry=true,factions=true,security=true})
local w=c.sites[1].world;local guard=w.workers[1]
local gun=Equipment.create(c,1,'frontier_carbine',guard.x,guard.y);World.stack(w,'ammunition',4,guard.x,guard.y)
-- These are public reports, not a synthetic secret cell.  The panel must show
-- them while still leaving private cell state out of its normal rendering.
w.security.events={{tick=0,kind='raid_warning',text='Inbound hostile expedition detected.',subject=1},{tick=0,kind='suspicious_gathering',text='A Guard noticed a suspicious gathering.',subject=guard.personId},{tick=0,kind='insurgent_sabotage',text='Internal sabotage disabled Training target.',subject=2}}
w.security.cell={id=c.security.nextCellId,members={w.workers[2].personId,w.workers[3].personId},state='organizing',readyTick=400,cause='a_secret_cell_cause',formedTick=0,detected=false}
c.security.nextCellId=c.security.nextCellId+1
app.history=History.new(c);app.campaign=true;app.siteId=1;app.help=nil;app.newRun=nil;app.crew=nil;app.fieldnotes=nil;app.school=nil;app.paused=false;app.accumulator=0;app.selectedCell={x=guard.x,y=guard.y}
advance(1) -- Current sight is derived before spatial policy commands validate.

love.keypressed('h');assert(app.crew,'Crew panel did not open');love.keypressed('tab');love.keypressed('tab');love.keypressed('tab');assert(app.crew.section=='security','Crew Security page is not reachable')
local start=app.history.live.tick;for _=1,12 do love.update(1/60);love.draw() end
assert(app.history.live.tick>start and not app.paused,'Security panel paused live simulation')
mock.record=true;mock.records={};love.draw()
assert(hasText('SETTLEMENT SECURITY') and hasText('Inbound hostile expedition detected.') and hasText('suspicious gathering'),'Security page omitted public security state')
local leaks={};for _,record in ipairs(mock.records) do if record.kind=='text' and (record.text:find('organizing',1,true) or record.text:find('a_secret_cell_cause',1,true)) then leaks[#leaks+1]=record.text end end
assert(#leaks==0,'Security page leaked secret cell state: '..table.concat(leaks,' | '))

love.keypressed('2');local queued=assert(app.history.commands[app.history.live.tick+1]);assert(queued[#queued].payload.type=='security_posture' and queued[#queued].payload.posture=='alert','Alert control queued the wrong command');advance(1);assert(app.history.live.sites[1].world.security.posture=='alert')
love.keypressed('g');advance(1);assert(app.history.live.sites[1].world.workers[1].security.guardEnabled,'Guard toggle was not site/person bound')
love.keypressed('p');advance(1);assert(#app.history.live.sites[1].world.security.posts==1,'Defense-post control did not apply its policy command')
love.keypressed('f');advance(1);assert(app.history.live.sites[1].world.security.refuge,'Refuge control did not apply its policy command')
love.keypressed('e');advance(30);assert(Equipment.equipped(app.history.live,guard.personId,'weapon') and Equipment.equipped(app.history.live,guard.personId,'weapon').id==gun.id,'Security equip control did not use the physical item path')
love.keypressed('r');advance(30);assert(app.history.live.sites[1].world.workers[1].security.ammo>0,'Security reload control did not use physical ammunition')

local stale=Commands.apply(app.history.live,{scope='site',siteId=1,payload={type='security_posture',posture='normal',expectedPolicyRevision=1}})
assert(not stale,'Stale Security UI revision was accepted')
-- An owned empty second site has its own policy state and must not inherit the
-- first site's posts/refuge merely because the Crew page remains open.
app.history.live.sites[2].ownerSocietyId=1;app.history.view.sites[2].ownerSocietyId=1;app.siteId=2;mock.records={};love.draw();assert(hasText('Posts: none') and hasText('Refuge: none'),'Security page leaked another site\'s local policy')
love.keypressed('escape');assert(not app.crew,'Escape did not close Security page')
love.quit()
print('PASS G06-R MOCK UI: live Security posture/Guard/post/refuge/equip/reload controls, public reports, hidden-cell discipline, stale revision, and site isolation.')
print('NOTE: mocked LÖVE graphics/input/storage only; native rendering and human gameplay remain untested.')
