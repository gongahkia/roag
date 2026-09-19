-- Fictional biology and machinery, deliberately game rules rather than real science.
local C={}
C.flora={
 veil={name='Veil bloom',color={0.59,0.37,0.70},note='Absorbs reachable water. Mature blooms shed a harmful pulse; grazers eat their biomass.',period=320},
 filter={name='Glass reed',color={0.30,0.76,0.69},note='Condenses adjacent steam. Needs water to grow; edible biomass competes with grazers.',period=400},
 thorn={name='Iron thorn',color={0.73,0.54,0.25},note='Hardens nearby sand into rock. Contact damages settlers. Its roots still require water.',period=480},
}
C.fauna={
 grazer={name='Lantern grazer',color={0.74,0.83,0.37},hp=24,width=2,height=1,period=8,
  note='Ground-dwelling herbivore. Eats wild growth and ripe crops; reproduces only when it has stored enough food.'},
 leech={name='Silt leech',color={0.40,0.67,0.73},hp=18,width=1,height=1,period=6,
  note='Water-dweller. Draws energy from water and attacks nearby bodies. Draining its habitat strands it.'},
 stalker={name='Hollow stalker',color={0.77,0.36,0.35},hp=56,width=2,height=2,period=6,
  note='Hunts grazers and follows mining, blasts and bloom pulses. It attacks nearby settlers; walls obstruct its movement and reach.'},
 sentinel={name='Vault sentinel',color={0.66,0.52,0.80},hp=90,width=2,height=2,period=10,
  note='Territorial ruin guardian. Nearby disturbances wake it. It does not pursue indefinitely beyond its home.'},
}
C.sites={
 cache={name='Sealed cache',color={0.80,0.65,0.33},note='Stored finite supplies. Salvage requires a worker to reach and open it.'},
 nursery={name='Root reliquary',color={0.43,0.75,0.50},note='A dormant root nursery. Salvage releases a growth and a grazer, if its surroundings have room.'},
 resonator={name='Buried resonator',color={0.68,0.45,0.86},note='Disturbance activates periodic calls. Survey reveals its rhythm; dismantling stops future calls.'},
 vent={name='Thermal fault',color={0.88,0.46,0.29},note='Intermittently converts adjacent water to steam. It creates no water; removing it ends the local conversion.'},
}
C.ruins={cistern=true,ossuary=true,archive=true,forge=true,nursery=true}
return C
