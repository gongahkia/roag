-- Small presentation-only controller for the finite service / utility menus.
local M={page_size=6,x=96,y=64,width=832,height=496,row_y=166,row_step=34}
function M.new(options,context)
    return {options=options,context=context or {},selected=1,confirm=false}
end
function M.page(model)return math.floor((model.selected-1)/M.page_size)end
function M.rows(model)
    local result={};local first=M.page(model)*M.page_size+1
    for i=first,math.min(first+M.page_size-1,#model.options.entries)do
        result[#result+1]={entry=model.options.entries[i],index=i,y=M.row_y+(i-first)*M.row_step}
    end
    return result
end
function M.key(model,key)
    local entries=model.options.entries;local count=#entries
    if key=='escape'then
        if model.confirm then model.confirm=false;return 'changed' end
        return 'close'
    end
    if count==0 then return 'changed' end
    local old=model.selected
    if key=='up' or key=='w'then model.selected=math.max(1,model.selected-1)
    elseif key=='down' or key=='s'then model.selected=math.min(count,model.selected+1)
    elseif key=='left'then model.selected=math.max(1,model.selected-M.page_size)
    elseif key=='right'then model.selected=math.min(count,model.selected+M.page_size)
    elseif tonumber(key) and tonumber(key)>=1 and tonumber(key)<=M.page_size then
        model.selected=math.min(count,M.page(model)*M.page_size+tonumber(key))
    elseif key=='return' or key=='kpenter'then
        local e=entries[model.selected]
        if e.enabled==false then return 'status',e.reason or 'Unavailable' end
        if e.select or model.context.nearby then return 'choose',e end
        if not model.confirm then model.confirm=true;return 'changed' end
        return 'choose',e
    end
    if model.selected~=old then model.confirm=false end
    return 'changed'
end
function M.mouse(model,x,y,presses)
    if x>=M.x+24 and x<=M.x+M.width-24 then
        for _,r in ipairs(M.rows(model))do
            if y>=r.y and y<r.y+30 then
                if model.selected~=r.index then model.confirm=false end
                model.selected=r.index
                if (presses or 1)>=2 then return M.key(model,'return')end
                return 'changed'
            end
        end
    end
    if y>=508 and y<=546 then
        if x>=600 and x<=896 then return M.key(model,'return')end
        if x>=120 and x<=360 then return M.key(model,'escape')end
    end
    return 'changed'
end
return M
