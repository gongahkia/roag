-- COS-G03 deliberately reuses the actual G02 excavation/education/travel
-- route.  The third G02 argument enables only the new versioned psychology
-- feature, so this remains a fresh feature-on integration trace rather than a
-- copy or synthetic travel shortcut.
local seed=tonumber(arg[1]) or 9703
local ticks=tonumber(arg[2]) or 20000
assert(seed and seed%1==0 and seed>=1 and seed<=2147483646,'Usage: luajit tools/g03_soak.lua [seed] [ticks]')
assert(ticks and ticks%1==0 and ticks>=20000 and ticks<=100000,'Ticks must be 20000..100000')
arg={tostring(seed),tostring(ticks),'psychology'}
dofile('tools/g02_soak.lua')
print('PASS G03 soak: feature-on psychology used the real G02 rope, school, P05/P06, cargo and P04 journey route.')
