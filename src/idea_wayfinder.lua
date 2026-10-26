local W={id='core:actor/wayfinder',beacon_id='core:object/return_beacon',range=6}
W.definition={id=W.id,name='Wayfinder',weapon_icon='dagger',description='Thrown knife, explored-room beacon, bounded return or short Dash',
 hp=22,damage=3,slash_damage=0,defense=0,speed=110,crit_chance_ppm=0,crit_damage_percent=150,
 costs={move=100,basic_attack=100,sweeping_slash=100,dash=110},dash_distance=2,
 basic_id='core:ability/wayfinder_knife',special_id='core:ability/wayfinder_plant',
 basic_name='Scout Knife',special_name='Plant Beacon',dash_name='Return / Dash',
 ui_controls='F Knife · R Plant beacon · X Return / Dash'}
function W.room(map,x,y)
 if not map.rooms then return 0 end -- the static test room is one room
 for _,room in ipairs(map.rooms)do
  if x>=room.x and x<room.x+room.width and y>=room.y and y<room.y+room.height then return room.id end
 end
end
function W.plant_preview(state,map,p,c,visible,explored,occupied)
 if not c or math.abs(c.x-p.position.x)+math.abs(c.y-p.position.y)~=1
  or not map.rows[c.y] or map.rows[c.y]:sub(c.x,c.x)~='.' then return false,'choose adjacent cardinal plain floor'end
 local room=W.room(map,c.x,c.y)
 if room==nil or room~=W.room(map,p.position.x,p.position.y)then return false,'plant within your current room'end
 if not visible(state,p.position.floor_id,c.x,c.y) or not explored(state,p.position.floor_id,c.x,c.y)then return false,'beacon needs explored visible ground'end
 if occupied(c.x,c.y)then return false,'beacon cell occupied'end
 for _,s in ipairs(map.placements and map.placements.stairs or {})do if s.x==c.x and s.y==c.y then return false,'stairs must stay clear'end end
 return true,nil,room
end
function W.plant(state,p,c,room)
 state.class_special.beacon={owner_id=p.id,floor_id=p.position.floor_id,x=c.x,y=c.y,room_id=room}
end
function W.return_preview(state,map,p,content,visible,explored,occupied)
 local b=state.class_special.beacon
 if not b or b.floor_id~=p.position.floor_id then return false,'beacon is on another floor or absent'end
 if p.position.x==b.x and p.position.y==b.y then return false,'already at beacon'end
 if W.room(map,p.position.x,p.position.y)~=b.room_id or W.room(map,b.x,b.y)~=b.room_id then return false,'return is limited to the beacon room'end
 if occupied(b.x,b.y) or not visible(state,b.floor_id,b.x,b.y) then return false,'beacon landing blocked or out of sight'end
 for _,s in ipairs(map.placements and map.placements.stairs or {})do if s.x==b.x and s.y==b.y then return false,'stairs must stay clear'end end
 local blocked={}
 for _,e in ipairs(state.entities)do local q=e.position
  if q.floor_id==b.floor_id and e.blocks_movement and e.controller~='player' and e.controller~='ai' then blocked[q.x..','..q.y]=true end
 end
 local queue={{x=p.position.x,y=p.position.y,d=0}};local seen={[p.position.x..','..p.position.y]=true};local head=1
 while head<=#queue do
  local at=queue[head];head=head+1
  if at.x==b.x and at.y==b.y then return true,nil,b,at.d end
  if at.d<W.range then for _,d in ipairs({{0,-1},{1,0},{0,1},{-1,0}})do
   local x,y=at.x+d[1],at.y+d[2];local key=x..','..y
   if not seen[key] and not blocked[key] and content.walkable(map,x,y) and W.room(map,x,y)==b.room_id
    and explored(state,b.floor_id,x,y)then seen[key]=true;queue[#queue+1]={x=x,y=y,d=at.d+1}end
  end end
 end
 return false,'beacon needs an explored route of at most six tiles'
end
function W.validate(state,maps,player)
 local b=state.class_special.beacon;if not b then return end
 local map=maps[b.floor_id]
 assert(player.definition_id==W.id and b.owner_id==player.id and map and type(b.x)=='number'
  and b.x%1==0 and type(b.y)=='number' and b.y%1==0 and b.x>=1 and b.x<=map.width and b.y>=1 and b.y<=map.height
  and map.rows[b.y]:sub(b.x,b.x)=='.' and W.room(map,b.x,b.y)==b.room_id,'invalid return beacon')
 assert(state.explored[b.floor_id] and state.explored[b.floor_id].cells[(b.y-1)*map.width+b.x],'unexplored beacon')
 for _,s in ipairs(map.placements and map.placements.stairs or {})do assert(s.x~=b.x or s.y~=b.y,'beacon on stairs')end
end
return W
