local M = {AIR=0, BEDROCK=1, ROCK=2, SOIL=3, SAND=4, WATER=5, ORE=6, LAVA=7, STEAM=8, ICE=9}
M.def = {
 [0]={name='Air', color={0.058,0.077,0.096}},
 [1]={name='Bedrock', solid=true, fixed=true, color={0.16,0.17,0.19}},
 [2]={name='Rock', solid=true, resource='stone', work=4, color={0.34,0.34,0.35}},
 [3]={name='Loose soil', solid=true, powder=true, resource='soil', work=1, color={0.40,0.28,0.21}},
 [4]={name='Sand', solid=true, powder=true, resource='soil', work=1, color={0.72,0.53,0.29}},
 [5]={name='Water', fluid=true, color={0.16,0.48,0.60}},
 [6]={name='Ore', solid=true, resource='metal', work=6, color={0.57,0.33,0.24}},
 [7]={name='Lava', fluid=true, hot=true, color={0.96,0.27,0.08}},
 [8]={name='Steam', gas=true, color={0.44,0.54,0.59}},
 [9]={name='Ice', solid=true, work=2, color={0.54,0.79,0.85}},
}
return M
