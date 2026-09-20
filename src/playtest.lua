-- Development-only storage isolation. Normal launches have no playtest state.
local P={}
local active=nil

local function under(root,path)
 return path==root or path:sub(1,#root+1)==root..'/'
end

function P.probeRequested()
 return os.getenv('COSMONAUTS_PLAYTEST_PROBE')=='1'
end

function P.activate()
 local root=os.getenv('COSMONAUTS_PLAYTEST_ROOT')
 if not root then
  assert(not P.probeRequested(),'A playtest probe requires COSMONAUTS_PLAYTEST_ROOT')
  return nil
 end
 assert(root:sub(1,1)=='/' and not root:find('/%.%.',1),'Invalid isolated playtest root')
 local saveDir=assert(love.filesystem.getSaveDirectory(),'LÖVE did not report a save directory')
 assert(under(root,saveDir),'Isolation refused: effective LÖVE save directory is outside the playtest root')
 active={root=root,saveDir=saveDir,adapterRoots={saveDir}}
 return active
end

function P.current() return active end

function P.writeAllowed()
 if not active then return true end
 local saveDir=love.filesystem.getSaveDirectory()
 if saveDir~=active.saveDir or not under(active.root,saveDir) then return false,'Isolation refused: writable LÖVE directory changed' end
 return true
end

function P.saveCandidateAllowed(name)
 if not active then return true end
 local info=love.filesystem.getInfo(name)
 if not info then return true end
 if not love.filesystem.getRealDirectory then return false,'Isolation refused: LÖVE cannot resolve the save candidate origin' end
 local source=love.filesystem.getRealDirectory(name)
 if not source or not under(active.root,source) then return false,'Isolation refused: '..name..' resolves outside the playtest root' end
 return true
end

function P.writeProbe()
 assert(active,'Playtest probe requires an active isolated root')
 local ok,why=P.writeAllowed();assert(ok,why)
 local text=table.concat({
  'format=cosmonauts-playtest-probe-v1',
  'root='..active.root,
  'save_directory='..active.saveDir,
  'adapter_roots='..table.concat(active.adapterRoots,':'),
 },'\n')..'\n'
 assert(love.filesystem.write('playtest-probe.txt',text))
 return active.saveDir..'/playtest-probe.txt'
end

return P
