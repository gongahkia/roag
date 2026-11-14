-- Original short PCM cues. Fixed arithmetic is cosmetic, not gameplay RNG.
local A={}
local function le16(n)return string.char(n%256,math.floor(n/256)%256)end
local function le32(n)return le16(n%65536)..le16(math.floor(n/65536))end
function A.wav(kind)
 local specs={roll={.15,{{0,340},{.037,480},{.077,270},{.11,390}}},
  land={.075,{{0,180},{.014,120}}},fold={.085,{{0,620},{.032,880}}},
  edit={.13,{{0,420},{.035,630},{.07,840}}}}
 local spec=assert(specs[kind]);local rate=22050;local count=math.floor(rate*spec[1]);local pcm={}
 for i=0,count-1 do
  local t=i/rate;local wave=0
  for _,tap in ipairs(spec[2])do local a=t-tap[1]
   if a>=0 and a<.04 then wave=wave+(math.sin(a*tap[2]*math.pi*2)+.3*math.sin(a*tap[2]*5.12))*math.exp(-a*140)*.18 end
  end
  local sample=math.floor(math.max(-1,math.min(1,wave))*32767);if sample<0 then sample=sample+65536 end;pcm[#pcm+1]=le16(sample)
 end
 pcm=table.concat(pcm);local fmt=le16(1)..le16(1)..le32(rate)..le32(rate*2)..le16(2)..le16(16)
 return 'RIFF'..le32(36+#pcm)..'WAVEfmt '..le32(#fmt)..fmt..'data'..le32(#pcm)..pcm
end
return A
