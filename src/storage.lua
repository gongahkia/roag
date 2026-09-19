-- LÖVE adapter; core never depends on a global love table.
local H=require('src.history')
local CampaignHistory=require('src.campaign_history')
local CampaignCodec=require('src.campaign_codec')
local Store={}
function Store.save(h)
 local ok,text=pcall(function() return h:saveText() end)
 if not ok then return false,tostring(text) end
 local good,err=love.filesystem.write('run.tmp',text)
 if not good then return false,err end
 local dir=love.filesystem.getSaveDirectory()
 -- Atomic replacement on the target Linux/macOS platforms, same directory.
 local renamed,why=os.rename(dir..'/run.tmp',dir..'/run.dat')
 if not renamed then return false,'Could not commit save: '..tostring(why)..'. Temporary save retained.' end
 return true
end
function Store.load()
 if not love.filesystem.getInfo('run.dat') then return nil end
 local text,err=love.filesystem.read('run.dat')
 if not text then return nil,err end
 local ok,h=pcall(H.fromText,text)
 if not ok then return nil,'Save left unchanged: '..tostring(h) end
 return h
end
function Store.saveCampaign(h)
 local ok,text=pcall(function() return h:saveText() end)
 if not ok then return false,tostring(text) end
 if type(text)~='string' or #text>CampaignCodec.limit then return false,'Campaign save size limit' end
 local good,err=love.filesystem.write('campaign.run.tmp',text)
 if not good then return false,err end
 local dir=love.filesystem.getSaveDirectory()
 local renamed,why=os.rename(dir..'/campaign.run.tmp',dir..'/campaign.run.dat')
 if not renamed then return false,'Could not commit campaign save: '..tostring(why)..'. Temporary campaign save retained.' end
 return true
end
function Store.loadCampaign()
 if not love.filesystem.getInfo('campaign.run.dat') then return nil end
 local text,err=love.filesystem.read('campaign.run.dat')
 if not text then return nil,err end
 local ok,h=pcall(CampaignHistory.fromText,text)
 if not ok then return nil,'Campaign save left unchanged: '..tostring(h) end
 return h
end
return Store
