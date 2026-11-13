-- Stable technique instances. Drawing never shares the world/combat RNG.
local RNG=require('src.rng')
local C=require('src.card_trial_content')
local Cards={}
local function shuffle(pile,rng)
    for i=#pile,2,-1 do local j=RNG.next_int(rng,i);pile[i],pile[j]=pile[j],pile[i]end
end
local function draw(deck)
    if #deck.draw==0 then
        assert(#deck.discard>0,'no active cards to draw')
        deck.draw,deck.discard=deck.discard,{};shuffle(deck.draw,deck.rng)
    end
    return table.remove(deck.draw)
end
function Cards.new(seed)
    local d={instances={},hand={},draw={},discard={},retired={},
        rng=RNG.new(RNG.derive(seed,'card-trial:techniques/v2'))}
    for _,row in ipairs(C.deck)do for _=1,row.count do
        local id=#d.instances+1;d.instances[id]={id=id,kind=row.kind};d.draw[#d.draw+1]=id
    end end
    for _,kind in ipairs(C.opening)do
        for i,id in ipairs(d.draw)do if d.instances[id].kind==kind then
            d.hand[#d.hand+1]=table.remove(d.draw,i);break
        end end
    end
    assert(#d.hand==C.hand_size,'opening hand must match hand size')
    shuffle(d.draw,d.rng);return d
end
function Cards.card(deck,id)
    local instance=type(id)=='number' and deck.instances[id]
    if not instance then return end
    return C.techniques[instance.kind],instance
end
function Cards.owned(deck,id)
    if type(id)~='number' or id%1~=0 then return end
    for slot,held in ipairs(deck.hand)do if held==id then return slot end end
end
function Cards.consume(deck,id,retire)
    local slot=assert(Cards.owned(deck,id),'card is not in hand')
    local pile=retire and deck.retired or deck.discard;pile[#pile+1]=id
    deck.hand[slot]=draw(deck)
end
function Cards.knight_boots(deck)
    for _,c in ipairs(deck.instances)do if c.kind=='dash'then c.kind='knight_hop'end end
end
function Cards.validate(deck)
    assert(type(deck)=='table' and #deck.hand==C.hand_size,'invalid hand size')
    local expected=0;for _,row in ipairs(C.deck)do expected=expected+row.count end
    assert(#deck.instances==expected,'invalid instance count')
    local seen,n={},0
    for _,pile in ipairs({deck.hand,deck.draw,deck.discard,deck.retired})do
        for _,id in ipairs(pile)do
            local def,c=Cards.card(deck,id)
            assert(def and c.id==id and not seen[id],'missing or duplicate card identity')
            if pile==deck.retired then assert(c.kind=='mend','only Mend retires')end
            seen[id]=true;n=n+1
        end
    end
    assert(n==expected,'card conservation failed');RNG.new(deck.rng.state)
    return true
end
return Cards
