import { distance, index, neighbors, same, SEARCH_LIMIT, walkable } from './model.ts';
import type { Cell, World } from './model.ts';
export function route(world:World, start:Cell, goal:Cell, occupied:Map<number,number>, actor:number, limit=SEARCH_LIMIT): { path:Cell[]; reason:string|null; expanded:number } {
  if(!walkable(world,goal))return {path:[],reason:'Destination is blocked',expanded:0};
  const open:[Cell,number,number][]=[[start,0,0]], costs=new Map([[index(start.x,start.y),0]]), previous=new Map<number,Cell>();
  let tie=1,expanded=0;
  while(open.length&&expanded<limit) {
    // Stable f-score, insertion sequence. Six agents / bounded searches; no unbounded queue.
    let best=0;
    for(let i=1;i<open.length;i++) {
      const a=open[i]!,b=open[best]!;const scoreA=a[1]+distance(a[0],goal),scoreB=b[1]+distance(b[0],goal);
      if(scoreA<scoreB||scoreA===scoreB&&a[2]<b[2])best=i;
    }
    const [current,cost]=open.splice(best,1)[0]!;
    if(costs.get(index(current.x,current.y))!==cost)continue;
    expanded++;
    if(same(current,goal)) {
      const path:Cell[]=[];let p=current;
      while(!same(p,start)){path.push(p);p=previous.get(index(p.x,p.y))!;}
      return {path:path.reverse(),reason:null,expanded};
    }
    for(const p of neighbors(current)) {
      const key=index(p.x,p.y), occupant=occupied.get(key);
      if(!walkable(world,p)||occupant!==undefined&&occupant!==actor||costs.has(key)&&costs.get(key)!<=cost+1)continue;
      costs.set(key,cost+1);previous.set(key,current);open.push([p,cost+1,tie++]);
    }
  }
  return {path:[],reason:open.length?'Route search budget reached; retrying':'No route; waiting for access',expanded};
}
