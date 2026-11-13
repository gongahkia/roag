-- Physical cards are IDs 1..52. This stream never shares the combat RNG.
local RNG=require('src.rng')
local C=require('src.card_trial_content')
local Cards={}
function Cards.card(id)
    if type(id)~='number' or id%1~=0 or id<1 or id>52 then return end
    local rank=(id-1)%13+1;local suit=math.floor((id-1)/13)+1
    return {id=id,rank=rank,suit=suit,label=C.ranks[rank],suit_name=C.suits[suit]}
end
local function shuffle(cards,rng)
    for i=#cards,2,-1 do
        local j=RNG.next_int(rng,i);cards[i],cards[j]=cards[j],cards[i]
    end
end
local function draw(deck)
    if #deck.draw==0 then
        assert(#deck.discard>0,'no cards left to draw')
        deck.draw,deck.discard=deck.discard,{}
        shuffle(deck.draw,deck.rng)
    end
    return table.remove(deck.draw)
end
function Cards.new(seed)
    local deck={hand={},draw={},discard={},rng=RNG.new(RNG.derive(seed,'card-trial:deck/v1'))}
    for id=1,52 do deck.draw[id]=id end
    shuffle(deck.draw,deck.rng)
    for i=1,C.hand_size do deck.hand[i]=draw(deck)end
    return deck
end
function Cards.selection(deck,ids)
    if type(ids)~='table' then return nil,'Select cards 1-5 first' end
    local count=0
    for k in pairs(ids)do
        if type(k)~='number' or k%1~=0 or k<1 or k>C.hand_size then
            return nil,'Invalid card selection'
        end
        count=count+1
    end
    if count==0 then return nil,'Select at least one card' end
    if count~=#ids or count>C.hand_size then return nil,'Invalid card selection' end
    local owned={};for _,id in ipairs(deck.hand)do owned[id]=true end
    local selected={}
    for _,id in ipairs(ids)do
        if not Cards.card(id) or not owned[id] or selected[id]then
            return nil,'Cards must be distinct members of your hand'
        end
        selected[id]=true
    end
    local recipe=count==C.recipes.single and 'single' or nil
    if count==C.recipes.pair and Cards.card(ids[1]).rank==Cards.card(ids[2]).rank then recipe='pair' end
    return {ids=ids,selected=selected,count=count,recipe=recipe}
end
function Cards.spend(deck,selection)
    -- Pay all selected cards before any refill. Retained slots never move.
    for _,id in ipairs(deck.hand)do
        if selection.selected[id]then deck.discard[#deck.discard+1]=id end
    end
    for slot,id in ipairs(deck.hand)do
        if selection.selected[id]then deck.hand[slot]=draw(deck)end
    end
end
function Cards.validate(deck)
    assert(type(deck)=='table' and #deck.hand==C.hand_size,'invalid hand size')
    local seen,n={},0
    for _,pile in ipairs({deck.hand,deck.draw,deck.discard})do
        for _,id in ipairs(pile)do
            assert(Cards.card(id) and not seen[id],'missing or duplicate physical card')
            seen[id]=true;n=n+1
        end
    end
    assert(n==52,'card conservation failed');RNG.new(deck.rng.state)
    return true
end
return Cards
