-- LÖVE adapter; core never depends on a global love table.
local H=require('src.history')
local CampaignHistory=require('src.campaign_history')
local CampaignCodec=require('src.campaign_codec')
local Playtest=require('src.playtest')
local Store={}

-- POSIX can atomically rename over an existing file; Windows cannot. Keep the
-- prior save recoverable while using a same-directory two-step replacement only
-- when the direct atomic path is unavailable.
local function replaceTemporary(tempName,targetName)
 local dir=love.filesystem.getSaveDirectory();local temp=dir..'/'..tempName;local target=dir..'/'..targetName
 local renamed,why=os.rename(temp,target)
 if renamed then return true end
 if not love.filesystem.getInfo(targetName) then return false,tostring(why) end
 local backupName=targetName..'.replace-backup';local suffix=1
 while love.filesystem.getInfo(backupName) do backupName=targetName..'.replace-backup-'..suffix;suffix=suffix+1 end
 local backup=dir..'/'..backupName;local moved,moveWhy=os.rename(target,backup)
 if not moved then return false,tostring(why)..'; could not preserve prior save: '..tostring(moveWhy) end
 renamed,why=os.rename(temp,target)
 if renamed then
  -- The new save is committed. A failed cleanup only leaves a recoverable old copy.
  os.remove(backup)
  return true
 end
 local restored,restoreWhy=os.rename(backup,target)
 if restored then return false,tostring(why)..'. Prior save restored.' end
 return false,tostring(why)..'. Prior save remains at '..backupName..': '..tostring(restoreWhy)
end

function Store.save(h)
 local allowed,why=Playtest.writeAllowed()
 if not allowed then return false,why end
 local ok,text=pcall(function() return h:saveText() end)
 if not ok then return false,tostring(text) end
 local good,err=love.filesystem.write('run.tmp',text)
 if not good then return false,err end
 local renamed,why=replaceTemporary('run.tmp','run.dat')
 if not renamed then return false,'Could not commit save: '..tostring(why)..'. Temporary save retained.' end
 return true
end
function Store.load()
 local allowed,why=Playtest.saveCandidateAllowed('run.dat')
 if not allowed then return nil,why end
 if not love.filesystem.getInfo('run.dat') then return nil end
 local text,err=love.filesystem.read('run.dat')
 if not text then return nil,err end
 local ok,h=pcall(H.fromText,text)
 if not ok then return nil,'Save left unchanged: '..tostring(h) end
 return h
end
function Store.saveCampaign(h)
 local allowed,why=Playtest.writeAllowed()
 if not allowed then return false,why end
 local ok,text=pcall(function() return h:saveText() end)
 if not ok then return false,tostring(text) end
 if type(text)~='string' or #text>CampaignCodec.limit then return false,'Campaign save size limit' end
 local good,err=love.filesystem.write('campaign.run.tmp',text)
 if not good then return false,err end
 local renamed,why=replaceTemporary('campaign.run.tmp','campaign.run.dat')
 if not renamed then return false,'Could not commit campaign save: '..tostring(why)..'. Temporary campaign save retained.' end
 return true
end
function Store.loadCampaign()
 local allowed,why=Playtest.saveCandidateAllowed('campaign.run.dat')
 if not allowed then return nil,why end
 if not love.filesystem.getInfo('campaign.run.dat') then return nil end
 local text,err=love.filesystem.read('campaign.run.dat')
 if not text then return nil,err end
 local ok,h=pcall(CampaignHistory.fromText,text)
 if not ok then return nil,'Campaign save left unchanged: '..tostring(h) end
 return h
end
return Store
