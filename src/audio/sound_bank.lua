local SoundBank = {}
SoundBank.__index = SoundBank

local SOUND_FILES = {
  step = "footstep00",
  shoot = "drawKnife1",
  hit = "chop",
  boom = "metalPot3",
  flare = "metalClick",
  hurt = "knifeSlice",
  door = "doorOpen_1",
  pickup = "handleCoins",
  select = "bookFlip2",
}

function SoundBank.new()
  return setmetatable({ sources = {} }, SoundBank)
end

function SoundBank:load()
  for name, file in pairs(SOUND_FILES) do
    local ok, source = pcall(love.audio.newSource, "assets/sounds/OGG/" .. file .. ".ogg", "static")
    if ok then
      self.sources[name] = source
    end
  end
end

function SoundBank:play(name)
  local source = self.sources[name]
  if source then
    source:stop()
    source:play()
  end
end

return SoundBank
