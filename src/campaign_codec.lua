-- Campaign envelopes retain the local codec's data-only canonical representation.
local Codec=require('src.codec')
local CampaignCodec={limit=32*1024*1024}
function CampaignCodec.encode(value)
 local text=Codec.encode(value)
 assert(#text<=CampaignCodec.limit,'Campaign save size limit')
 return text
end
function CampaignCodec.decode(text)
 assert(type(text)=='string' and #text<=CampaignCodec.limit,'Campaign save size limit')
 return Codec.decode(text)
end
function CampaignCodec.hash(value) return Codec.hash(value) end
return CampaignCodec
