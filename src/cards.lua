-- Physical instances and one independent shuffle stream; no combat events here.
local RNG=require('src.rng')
local C=require('src.card_trial_content')
local Cards={ranks={'A','2','3','4','5','6','7','8','9','10','J','Q','K'},suits={'Spades','Hearts','Clubs','Diamonds'}}
function Cards.face(face)
    if type(face)~='number' or face%1~=0 or face<1 or face>52 then return end
    local rank=(face-1)%13+1;local suit=math.floor((face-1)/13)+1
    return {face=face,rank=rank,suit=suit,label=Cards.ranks[rank],suit_name=Cards.suits[suit],name=Cards.ranks[rank]..' '..Cards.suits[suit]}
end
function Cards.card(deck,id)
    if type(id)~='number' or id%1~=0 then return end
    local c=deck.instances[id];if not c then return end
    local f=Cards.face(c.face);f.id=id;return f
end
function Cards.shuffle(pile,rng)
    for i=#pile,2,-1 do local j=RNG.next_int(rng,i);pile[i],pile[j]=pile[j],pile[i]end
end
local function draw(d)
    if #d.draw==0 then assert(#d.discard>0,'empty deck');d.draw,d.discard=d.discard,{};Cards.shuffle(d.draw,d.rng)end
    return table.remove(d.draw)
end
function Cards.refill(d)
    while #d.hand<C.hand_size do d.hand[#d.hand+1]=draw(d)end
end
function Cards.new(seed)
    local d={instances={},hand={},draw={},discard={},rng=RNG.new(RNG.derive(seed,'card-trial:poker/v3'))}
    for id=1,52 do d.instances[id]={id=id,face=id};d.draw[id]=id end
    Cards.shuffle(d.draw,d.rng);Cards.refill(d);return d
end
function Cards.owned(d,id)
    if type(id)~='number' or id%1~=0 then return end
    for slot,v in ipairs(d.hand)do if v==id then return slot end end
end
function Cards.selection(d,ids,min,max)
    if type(ids)~='table' then return nil,'Choose cards from the unused hand'end
    local n=0;for k in pairs(ids)do if type(k)~='number' or k%1~=0 or k<1 then return nil,'Invalid selection'end;n=n+1 end
    if n~=#ids or n<(min or 0) or n>(max or C.hand_size)then return nil,'Choose '..(min or 0)..' to '..(max or C.hand_size)..' cards'end
    local seen={};local out={}
    for _,id in ipairs(ids)do if not Cards.owned(d,id) or seen[id]then return nil,'Cards must be distinct unused instances'end;seen[id]=true;out[#out+1]=id end
    return out,seen
end
function Cards.discard(d,ids)
    local marked={};for _,id in ipairs(ids)do marked[id]=true end
    for i=#d.hand,1,-1 do if marked[d.hand[i]]then d.discard[#d.discard+1]=table.remove(d.hand,i)end end
end
function Cards.exchange(d,ids)
    local marked={};for _,id in ipairs(ids)do marked[id]=true end
    -- Quarantine exchanged instances until every replacement has been drawn.
    for slot,id in ipairs(d.hand)do if marked[id]then d.hand[slot]=draw(d)end end
    for _,id in ipairs(ids)do d.discard[#d.discard+1]=id end
end
function Cards.recover_begin(d,retained)
    local remove={};for _,id in ipairs(d.hand)do if not retained[id]then remove[#remove+1]=id end end
    Cards.discard(d,remove)
end
function Cards.add(d,face,count)
    assert(Cards.face(face));for _=1,count do local id=#d.instances+1;d.instances[id]={id=id,face=face};d.discard[#d.discard+1]=id end
end
function Cards.opening_reset(d)
    d.hand={};d.draw={};d.discard={}
    for id=1,#d.instances do d.draw[id]=id end
    Cards.shuffle(d.draw,d.rng);Cards.refill(d)
end
function Cards.face_count(d,face)
    local n=0;for _,v in ipairs(d.instances)do if v.face==face then n=n+1 end end;return n
end
function Cards.validate(d)
    assert(type(d)=='table' and #d.hand<=C.hand_size,'invalid hand size')
    local seen,n={},0
    for _,pile in ipairs({d.hand,d.draw,d.discard})do for _,id in ipairs(pile)do
        local c=Cards.card(d,id);assert(c and d.instances[id].id==id and not seen[id],'duplicate or invalid identity');seen[id]=true;n=n+1
    end end
    assert(n==#d.instances and n>=52,'card conservation failed');RNG.new(d.rng.state);return true
end
return Cards
