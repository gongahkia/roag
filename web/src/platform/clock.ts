import { TICK_MS } from '../simulation/model.ts';
export class FixedClock {
  private accumulated=0;
  manualPaused=true;
  hidden=false;
  speed:1|2|4=1;
  get paused():boolean{return this.manualPaused||this.hidden;}
  get backlog():number{return Math.floor(this.accumulated/TICK_MS);}
  get alpha():number{return Math.min(1,this.accumulated/TICK_MS);}
  reset():void{this.accumulated=0;}
  setManual(paused:boolean):void{this.manualPaused=paused;if(paused)this.reset();}
  setHidden(hidden:boolean):void{this.hidden=hidden;this.reset();}
  frame(elapsedMs:number,step:()=>void):number {
    if(!Number.isFinite(elapsedMs)||elapsedMs<0)throw new Error('Invalid frame duration');
    if(this.paused){this.reset();return 0;}
    this.accumulated+=elapsedMs*this.speed;
    let ticks=0;
    while(this.accumulated>=TICK_MS&&ticks<8){step();this.accumulated-=TICK_MS;ticks++;}
    return ticks;
  }
}
