import { createWorld } from '../simulation/world.ts';
import { enqueue, step } from '../simulation/step.ts';
import type { Cell, World, CommandInput } from '../simulation/model.ts';
import { workCommandReason } from '../simulation/jobs.ts';
import { FixedClock } from './clock.ts';
import { makeSave, parseSave, encodeSave, MAX_SAVE_BYTES } from '../persistence/schema.ts';
import type { Save } from '../persistence/schema.ts';
import { SaveStore } from '../persistence/store.ts';
export class ColonyController {
  world:World;
  readonly clock=new FixedClock();
  selected:number|null=null;
  busy=false;
  message='Paused for planning. Select a founder and choose a destination.';
  storageError=false;
  visibilityTransitions:{state:string;tick:number;manualPaused:boolean}[]=[];
  private skipFrame=false;
  private listeners=new Set<(reset:boolean)=>void>();
  constructor(private store:SaveStore, seed=42, scenario:World['scenario']='colony'){this.world=createWorld(seed,{scenario});}
  subscribe(listener:(reset:boolean)=>void):()=>void{this.listeners.add(listener);return()=>this.listeners.delete(listener);}
  notify(reset=false):void{for(const listener of this.listeners)listener(reset);}
  snapshot():Save{return makeSave(this.world,{manualPaused:this.clock.manualPaused,speed:this.clock.speed});}
  private install(save:Save):void{this.world=save.world;this.clock.setManual(save.controls.manualPaused);this.clock.speed=save.controls.speed;this.clock.reset();this.skipFrame=true;this.selected=null;this.notify(true);}
  async restore():Promise<void>{
    try{const saved=await this.store.load();if(saved){this.install(saved);this.message=`Restored tick ${saved.world.tick}.`;}}
    catch(error){this.error('Restore failed; stored slot left intact. Running in memory',error);}
  }
  frame(elapsed:number):void{
    if(this.skipFrame){this.skipFrame=false;elapsed=0;}
    const ticks=this.clock.frame(elapsed,()=>step(this.world));
    if(this.world.terminal)this.clock.setManual(true);
    if(ticks)this.notify();
  }
  visibility(hidden:boolean):void{
    this.clock.setHidden(hidden);this.skipFrame=true;
    this.visibilityTransitions.push({state:hidden?'hidden':'visible',tick:this.world.tick,manualPaused:this.clock.manualPaused});
    if(this.visibilityTransitions.length>16)this.visibilityTransitions.shift();this.notify();
  }
  pause():void{this.clock.setManual(!this.clock.manualPaused);this.skipFrame=true;this.notify();}
  speed(value:1|2|4):void{this.clock.speed=value;this.notify();}
  select(id:number|null):void{this.selected=id;this.notify();}
  order(target:Cell):boolean{
    if(this.busy||this.selected===null)return false;
    const ok=enqueue(this.world,{kind:'move',actor:this.selected,target});
    this.message=ok?`Destination ${target.x}, ${target.y} queued for tick ${this.world.tick+1}.`:'Order rejected: choose a living founder and a tile inside the map.';this.notify();return ok;
  }
  work(input:CommandInput):boolean {
    if(this.busy)return false;
    const reason=workCommandReason(this.world,input);const ok=reason===null&&enqueue(this.world,input);
    this.message=ok?`Work command queued for tick ${this.world.tick+1}.`:reason??'Work command rejected';this.notify();return ok;
  }
  editGate():void{
    if(this.busy)return;
    const ok=enqueue(this.world,{kind:'edit',target:{x:64,y:60},solid:this.world.terrain[60*128+64]!==2});
    this.message=ok?'Routing-lab gate change queued.':'Gate edit rejected (occupied or outside routing lab).';this.notify();
  }
  async save():Promise<void>{
    if(this.busy)return;this.busy=true;this.notify();
    try{const snapshot=this.snapshot();await this.store.commit(snapshot);this.storageError=false;this.message=`Saved tick ${snapshot.world.tick}.`;}
    catch(error){this.error('Save failed; previous slot retained. Export JSON is still available',error);}
    finally{this.busy=false;this.notify();}
  }
  async import(text:string):Promise<void>{
    if(this.busy)return;this.busy=true;this.notify();
    try{const save=parseSave(text);await this.store.commit(save);this.install(save);this.storageError=false;this.message=`Imported and saved tick ${save.world.tick}.`;}
    catch(error){this.error('Import rejected; current world and stored slot retained',error);}
    finally{this.busy=false;this.notify();}
  }
  async importFile(file:File):Promise<void>{
    if(file.size>MAX_SAVE_BYTES){this.error('Import rejected; current world and stored slot retained',new Error('Save file is too large'));this.notify();return;}
    try{await this.import(await file.text());}catch(error){this.error('Import read failed',error);this.notify();}
  }
  async newRun(seed:number):Promise<void>{
    if(this.busy)return;
    try{const world=createWorld(seed,{scenario:this.world.scenario});this.install(makeSave(world,{manualPaused:true,speed:1}));this.message=`New colony, seed ${seed}. Paused for planning.`;await this.save();}
    catch(error){this.error('New run rejected',error);this.notify();}
  }
  exportText():string{return encodeSave(this.snapshot());}
  private error(prefix:string,error:unknown):void{this.storageError=true;this.message=`${prefix}: ${error instanceof Error?error.message:String(error)}`;}
}
