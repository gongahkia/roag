-- At most eight cards: exhaustively inspect small contributing subsets.
-- Category/payout only; equivalent hands use ascending physical IDs as a tie-break.
local Cards=require('src.cards')
local C=require('src.card_trial_content')
local P={}
local function classify(d,ids)
    local n=#ids;local counts={};local suit;local flush=true;local ranks={}
    for _,id in ipairs(ids)do local c=Cards.card(d,id);if not c then return end
        counts[c.rank]=(counts[c.rank]or 0)+1
        if suit and c.suit~=suit then flush=false end;suit=c.suit
    end
    local groups={};for rank,k in pairs(counts)do ranks[#ranks+1]=rank;groups[#groups+1]=k end
    table.sort(groups);table.sort(ranks)
    local straight=false
    if n==5 and #ranks==5 then
        straight=ranks[5]-ranks[1]==4
        if ranks[1]==1 and ranks[2]==10 and ranks[3]==11 and ranks[4]==12 and ranks[5]==13 then straight=true end
    end
    if n==1 then return 'single'
    elseif n==2 and groups[1]==2 then return 'pair'
    elseif n==3 and groups[1]==3 then return 'three_kind'
    elseif n==4 and groups[1]==4 then return 'four_kind'
    elseif n==4 and #groups==2 and groups[1]==2 and groups[2]==2 then return 'two_pair'
    elseif n==5 then
        if straight and flush then return 'straight_flush'end
        if #groups==2 and groups[1]==2 and groups[2]==3 then return 'full_house'end
        if flush then return 'flush'end
        if straight then return 'straight'end
    end
end
P.classify=classify
local function less(a,b)
    for i=1,math.min(#a,#b)do if a[i]~=b[i]then return a[i]<b[i]end end
    return #a<#b
end
function P.best(d,pool,category)
    local ids={};local seen={}
    for _,id in ipairs(pool)do if not Cards.card(d,id) or seen[id]then return nil end;seen[id]=true;ids[#ids+1]=id end
    table.sort(ids);local best;local subset={}
    local function visit(start)
        if #subset>0 then
            local key=classify(d,subset)
            if key and (not category or key==category)then
                local value=C.payouts[key]
                if not best or value>best.stamina or value==best.stamina and less(subset,best.cards)then
                    local cards={};for i,id in ipairs(subset)do cards[i]=id end
                    best={key=key,name=C.poker_names[key],stamina=value,cards=cards}
                end
            end
        end
        if #subset==5 then return end
        for i=start,#ids do subset[#subset+1]=ids[i];visit(i+1);subset[#subset]=nil end
    end
    visit(1);return best
end
return P
