-- Small semantic art choices for the existing interface/playback. No timing,
-- simulation calls, new statuses, randomness or item/class behavior lives here.
local D=require('src.idea_environment_content')
local P={}
local icons={
 adventurer={'sword','slash'},ranger={'bow','volley'},arcanist={'staff','wand'},
 berserker={'mace','axe'},sentinel={'shield','shield'},alchemist={'vial','vial'},
 shade={'knife','wand'},hookrunner={'knife','hook'},trapper={'knife','trap'},
 duelist={'knife','shield'},geomancer={'staff','stone'},spiritbinder={'wand','spirit'},
 tinkerer={'bow','sentry'},stormcaller={'staff','spark'},wayfinder={'knife','beacon'},
}
local specific={trap='core:ui/signal_armed_effect',stone='core:object/geomancer_bulwark',
 spirit='core:object/bound_spirit',sentry='core:object/bolt_sentry',spark='core:ui/cue_spark_effect',
 beacon='core:object/return_beacon'}
function P.weapon(class_id,kind)
 if kind=='dash'then return 'core:ui/ability_move_icon'end
 local pair=icons[(class_id or ''):match('/([^/]+)$')] or icons.adventurer
 local name=pair[kind=='sweeping_slash' and 2 or 1]
 return specific[name] or 'core:ui/ability_'..name..'_icon'
end
function P.effect(event)
 if event.kind=='DamageTaken'then
  if (event.actual_loss or 0)<=0 then return end
  local name=event.damage_type=='fire' and 'fire' or event.damage_type=='lightning' and 'spark'
   or (event.damage_type=='echo' or event.damage_type=='spirit') and 'echo' or 'slash'
  return 'core:ui/cue_'..name..'_effect'
 elseif event.kind=='DamageBlocked'then return 'core:ui/cue_block_effect'
 elseif event.kind=='BoonActivated'then return 'core:ui/cue_boon_effect'
 elseif (event.kind=='Healed' or event.kind=='SupplyCollected') and (event.actual_gain or event.amount or 0)>0 then
  return 'core:ui/status_heal_icon'
 end
end
function P.statuses(player)
 local out={}
 if player.statuses and player.statuses.burning then out[#out+1]={id='core:ui/burning_effect',label='Burning'}end
 if (player.pickup_ward_actions or 0)>0 or player.storm_ward then out[#out+1]={id='core:ui/status_ward_icon',label='Ward'}end
 if (player.supply_focus_actions or 0)>0 then out[#out+1]={id='core:ui/status_focus_icon',label='Focus'}end
 if player.definition_id=='core:actor/stormcaller'then
  local charge=math.max(0,math.min(3,player.storm_charge or 0))
  out[#out+1]={id='core:ui/status_charge_'..charge..'_icon',label='Charge '..charge}
 end
 return out
end
function P.signal(phase)
 return (phase=='warning' or phase=='armed') and 'core:ui/signal_armed_effect'
  or phase=='closed' and 'core:ui/signal_spent_effect' or 'core:ui/signal_safe_effect'
end
function P.mural(map,x,y)
 local n=(x*3+y*5+(map.seed or 1))%3
 return D.prop_id(n==0 and 'mural_face' or n==1 and 'mural_sigil' or 'mural_leaf')
end
return P
