import { initialCombat } from './combat.ts';
import { initialEnvironment, placeEnvironment, biomeQuantity } from './biome.ts';
import { defaultDuties } from './survival-content.ts';
import { emptyStock } from './work-content.ts';
import { streams, roll, validSeed } from './random.ts';
import { WIDTH, HEIGHT, index, neighbors, walkable, same } from './model.ts';
import type { World, Cell, Resource } from './model.ts';
const SPAWNS = [[61,64],[60,62],[68,66],[64,68],[67,62],[63,60]] as const;
export function reachable(world: World, start: Cell): Set<number> {
  const seen = new Set<number>(); const queue = [start];
  if (walkable(world,start)) seen.add(index(start.x,start.y));
  for (let at=0;at<queue.length;at++) for (const p of neighbors(queue[at]!)) {
    const key=index(p.x,p.y); if (!seen.has(key) && walkable(world,p)) { seen.add(key); queue.push(p); }
  }
  return seen;
}
export function generationIssues(world: World): string[] {
  const seen=reachable(world,{x:64,y:65}); const errors:string[]=[];
  for (const p of world.people) if (!seen.has(index(p.x,p.y))) errors.push('spawn inaccessible');
  for (const r of world.resources) if (!neighbors(r).some((p)=>seen.has(index(p.x,p.y)))) errors.push('resource inaccessible');
  for (const p of [{x:64,y:1},{x:64,y:126},{x:1,y:64},{x:126,y:64}]) if (!seen.has(index(p.x,p.y))) errors.push('approach inaccessible');
  for (let y=0;y<HEIGHT;y++) for(let x=0;x<WIDTH;x++) if(walkable(world,{x,y})&&!seen.has(index(x,y))) { errors.push('disconnected floor');return errors; }
  if(world.resources.length!==18 || new Set(world.people.map(p=>index(p.x,p.y))).size!==6) errors.push('counts');
  return errors;
}
export function createWorld(seed: number, options: { forceFallback?: boolean; scenario?: World['scenario'] } = {}): World {
  if(!validSeed(seed)) throw new Error('Seed must be an integer from 1 to 2147483646');
  const world:World={combat:initialCombat(),environment:initialEnvironment(seed),bosses:{encounters:[],features:[],effects:[],work:[],status:'No environmental boss encounter'},seed,tick:0,nextId:8,nextSequence:1,rng:streams(seed),width:WIDTH,height:HEIGHT,terrain:[],terrainVersion:0,
    scenario:options.scenario??'colony',generation:{attempts:0,fallback:false},heart:{id:1,x:64,y:64,health:100},
    people:SPAWNS.map(([x,y],i)=>({id:i+2,founder:i,x,y,life:'alive',destination:null,next:null,progress:0,moveTicks:4,route:[],routeVersion:0,retryAt:0,blocked:null,job:null,cargo:emptyStock(),yieldTo:null,yieldUntil:0,failures:0,body:{hunger:0,fatigue:0,health:100,mood:100,deathAt:0,recoverAt:0,meals:0,rests:0,duties:defaultDuties(),routine:null,status:'Ready'}})),resources:[],pending:[],terminal:null,jobs:[],structures:[],piles:[],spent:emptyStock(),lost:emptyStock(),sourceRemoved:emptyStock(),produced:emptyStock(),consumed:emptyStock(),recipeCounts:{ammo:0,meal:0,ore:0,medicine:0,tool:0},statistics:{eatenFood:0,eatenMeals:0,harvests:0,recipes:0,rescues:0,treatments:0,deaths:0,workTicks:0}};
  const make = (fallback:boolean) => {
    world.terrain=[];world.resources=[];world.nextId=8;
    for(let y=0;y<HEIGHT;y++) for(let x=0;x<WIDTH;x++) {
      const border=x===0||y===0||x===127||y===127;
      const clearing=Math.abs(x-64)<=10&&Math.abs(y-64)<=10;
      const road=Math.abs(x-64)<=1||Math.abs(y-64)<=1;
      const rock=!fallback&&roll(world.rng,'map',100)<12;
      world.terrain.push(border?2:clearing||road?1:rock?2:0);
    }
    for(let i=0;i<18;i++) {
      let chosen:Cell|null=null;
      for(let attempt=0;attempt<256;attempt++) {
        const p=fallback?{x:[24,40,88,104,112,16][i%6]!,y:[28,40,88][Math.floor(i/6)]!}:{x:16+roll(world.rng,'resources',96),y:16+roll(world.rng,'resources',96)};
        if(!(Math.abs(p.x-64)<=12&&Math.abs(p.y-64)<=12)&&walkable(world,p)&&Math.abs(p.x-64)>3&&Math.abs(p.y-64)>3&&!world.resources.some(r=>same(r,p))&&!world.people.some(a=>same(a,p))) {chosen=p;break;}
      }
      if(!chosen)return false;
      const kind:Resource['kind']=i<6?'wood':i<12?'stone':'food';
      const quantity=biomeQuantity(world.environment.biome,kind,20+roll(world.rng,'resources',61));
      world.resources.push({...chosen,id:world.nextId++,kind,initial:quantity,remaining:quantity});
      if(kind!=='food')world.terrain[index(chosen.x,chosen.y)]=2;
    }
    const seen=reachable(world,{x:64,y:65});
    for(let y=1;y<127;y++)for(let x=1;x<127;x++)if(walkable(world,{x,y})&&!seen.has(index(x,y)))world.terrain[index(x,y)]=2;
    return generationIssues(world).length===0;
  };
  let valid=false;
  if(!options.forceFallback) for(let i=1;i<=3;i++){world.generation.attempts=i;if(make(false)){valid=true;break;}}
  if(!valid){world.generation.fallback=true;if(!make(true))throw new Error('Fallback world failed validation');}
  if(world.scenario==='routing-lab') {
    // A narrow corridor is solely a route-obstruction development fixture.
    for(let y=58;y<=62;y++)for(let x=55;x<=73;x++)world.terrain[index(x,y)]=2;
    for(let x=56;x<=72;x++)world.terrain[index(x,60)]=1;
    const ada=world.people[0]!;ada.x=58;ada.y=60;
    world.people[1]!.x=60;world.people[1]!.y=64;
    world.people[4]!.x=67;world.people[4]!.y=64;
    world.people[5]!.x=63;world.people[5]!.y=63;
  }
  placeEnvironment(world);
  return world;
}
