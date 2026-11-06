import Phaser from 'phaser';
import type { World } from '../simulation/model.ts';
import { same } from '../simulation/model.ts';
import { TILE } from './view.ts';
import { BOSS_CONTENT } from '../simulation/boss-content.ts';

/** Saved geometry only: drawing never places fixtures, advances effects or rolls RNG. */
export class BossLayer {
  private ground: Phaser.GameObjects.Graphics;
  private warnings: Phaser.GameObjects.Graphics;
  private images = new Map<string, Phaser.GameObjects.Image>();
  private labels = new Map<number, Phaser.GameObjects.Text>();
  constructor(private scene: Phaser.Scene) {
    this.ground = scene.add.graphics().setDepth(1);
    this.warnings = scene.add.graphics().setDepth(5);
  }
  draw(w: World): void {
    this.ground.clear(); this.warnings.clear();
    const keep = new Set<string>();
    const sprite = (key:string,x:number,y:number,alias:string,tint:number,alpha=1):void => {
      keep.add(key); let image = this.images.get(key);
      if (!image) { image=this.scene.add.image(0,0,'colony',alias).setDepth(2);this.images.set(key,image); }
      image.setFrame(alias).setPosition((x+.5)*TILE,(y+.5)*TILE).setTint(tint).setAlpha(alpha);
    };
    for(const c of w.environment.wet)this.ground.fillStyle(0x3a8ca0,.38).fillRect(c.x*TILE,c.y*TILE,TILE,TILE).lineStyle(1,0x8ac8d8,.8).lineBetween(c.x*TILE+2,c.y*TILE+7,c.x*TILE+10,c.y*TILE+7);
    for(const c of w.environment.ash){this.ground.fillStyle(0x9e4b32,.35).fillRect(c.x*TILE,c.y*TILE,TILE,TILE);sprite(`ash:${c.x},${c.y}`,c.x,c.y,'hazard-fire',0xef9e64,.6);}
    const channelTiles=new Set<string>();
    for(const encounter of w.bosses.encounters)encounter.layout.channels.forEach((channel,i)=>{
      for(const c of channel.cells){
        const key=`channel:${c.x},${c.y}`;if(channelTiles.has(key))continue;channelTiles.add(key);
        const horizontal=channel.cells.some(n=>n.y===c.y&&Math.abs(n.x-c.x)===1);
        sprite(key,c.x,c.y,horizontal?'channel-water-h':'channel-water-v',encounter.drained[i]?0x858476:0x8ac8d8,encounter.drained[i]?.45:.9);
      }
    });
    for(const f of w.bosses.features){
      const alias=f.kind==='resonator'?'feature-resonator':f.kind==='valve'?'feature-valve':f.kind==='anchor'?'stones':'hazard-web';
      const encounter=w.bosses.encounters.find(e=>e.owner===f.owner),drained=f.kind==='valve'&&f.channel!==null&&encounter?.drained[f.channel];
      sprite(`feature:${f.id}`,f.x,f.y,alias,drained?0x858476:f.kind==='resonator'?0xffdf88:f.kind==='valve'?0x8ac8d8:0xd5b5e8);
      const x=f.x*TILE,y=f.y*TILE;
      if(f.kind==='anchor')this.warnings.lineStyle(1,0xd5b5e8).strokeRect(x+1,y+1,TILE-2,TILE-2);
      if(f.kind==='resonator'||f.kind==='anchor')this.warnings.fillStyle(0x28242d).fillRect(x+1,y-2,TILE-2,2).fillStyle(0xd5b5e8).fillRect(x+1,y-2,(TILE-2)*f.health/BOSS_CONTENT.featureHealth[f.kind],2);
    }
    const colors={soundseek:0xffdf88,ring:0xffdf88,surge:0x8ac8d8,feast:0x8fcf9d,partition:0xd5b5e8,reel:0xffa0c0,fire:0xff9a60};
    for(const effect of w.bosses.effects){
      const color=colors[effect.kind];
      for(const c of effect.cells){const x=c.x*TILE,y=c.y*TILE;
        this.warnings.fillStyle(color,.18).fillRect(x,y,TILE,TILE).lineStyle(1,color).strokeRect(x+1,y+1,TILE-2,TILE-2);
        if(effect.kind==='ring')this.warnings.strokeCircle(x+6,y+6,3);
        else if(effect.kind==='surge')this.warnings.lineBetween(x+2,y+4,x+9,y+4).lineBetween(x+6,y+2,x+9,y+4).lineBetween(x+6,y+6,x+9,y+4);
        else if(effect.kind==='reel')this.warnings.lineBetween(x+2,y+2,x+10,y+10);
        else if(effect.kind==='fire')sprite(`fire:${effect.id}:${c.x},${c.y}`,c.x,c.y,'hazard-fire',color,.7);
      }
      const target=effect.cells.at(-1)??effect.origin;let label=this.labels.get(effect.id);
      if(!label){label=this.scene.add.text(0,0,'',{fontFamily:'monospace',fontSize:'7px',color:'#ffffff',backgroundColor:'#251d18'}).setOrigin(.5,1).setDepth(6);this.labels.set(effect.id,label);}
      label.setPosition((target.x+.5)*TILE,target.y*TILE).setText(`${effect.kind.slice(0,1).toUpperCase()}${Math.max(0,effect.due-w.tick)}`);
    }
    for(const [id,label]of this.labels)if(!w.bosses.effects.some(e=>e.id===id)){label.destroy();this.labels.delete(id);}
    for(const [key,image]of this.images)if(!keep.has(key)){image.destroy();this.images.delete(key);}
    for(const e of w.bosses.encounters)if(e.exposedUntil>w.tick){const r=w.combat.raiders.find(r=>r.id===e.owner);if(r)this.warnings.lineStyle(1,0xffdf88).strokeCircle((r.x+.5)*TILE,(r.y+.5)*TILE,9);}
    // A line is deliberately rendered independently of its original target body.
    for(const work of w.bosses.work){const f=w.bosses.features.find(f=>f.id===work.target),p=w.people.find(p=>p.id===work.actor);if(f&&p&&!same(f,p))this.warnings.lineStyle(1,0xffffff,.65).lineBetween((p.x+.5)*TILE,(p.y+.5)*TILE,(f.x+.5)*TILE,(f.y+.5)*TILE);}
  }
}
