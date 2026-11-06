import Phaser from 'phaser';
import { assetUrl } from '../assets.ts';
import type { Manifest } from '../assets.ts';
import { founders } from '../content.ts';
import { active, MOVE_TICKS } from '../simulation/model.ts';
import { total, WORK_CONTENT } from '../simulation/work-content.ts';
import { renderedPosition } from '../simulation/step.ts';
import type { ColonyController } from '../platform/controller.ts';
import { constrain, zoomAt, TILE, WORLD, MAX_ZOOM, minZoom } from './view.ts';
import type { View, Size } from './view.ts';
import type { UiCommand } from '../ui/panels.ts';
import { element } from '../ui/panels.ts';

export class ColonyScene extends Phaser.Scene {
  private view: View = { x: 64.5 * TILE, y: 64.5 * TILE, zoom: 3 };
  private workMarks!: Phaser.GameObjects.Graphics;
  private combatMarks!: Phaser.GameObjects.Graphics;
  private attackLabels = new Map<number, Phaser.GameObjects.Text>();
  private ring!: Phaser.GameObjects.Rectangle;
  private drag: { x: number; y: number; view: View; moved: boolean; button: number } | undefined;
  private keys = new Set<string>();
  private stage = element('stage');
  private controller: ColonyController;
  private manifest: Manifest;
  private map: Phaser.Tilemaps.Tilemap | undefined;
  private decorations: Phaser.GameObjects.Image[] = [];
  private people = new Map<number, Phaser.GameObjects.Image>();
  private terrainVersion = -1;
  private unsubscribe: (() => void) | undefined;
  private fail: (message: string) => void;
  private lastView = '';

  constructor(manifest: Manifest, controller: ColonyController, fail: (message: string) => void) {
    super('clearing'); this.controller = controller; this.manifest = manifest; this.fail = fail;
  }
  preload(): void {
    this.load.on('loaderror', () => this.fail('Artwork could not be loaded. Reload to try again.'));
    this.load.atlas('colony', assetUrl('colony.png'), assetUrl('colony-atlas.json'));
  }
  create(): void {
    if (!this.textures.exists('colony')) return;
    this.textures.get('colony').setFilter(Phaser.Textures.FilterMode.NEAREST);
    // Tilemap coordinates use the complete sheet, not atlas-frame aggregate bounds.
    const terrainTexture = this.textures.addImage('terrain', this.textures.get('colony').getSourceImage() as HTMLImageElement);
    if (!terrainTexture) throw new Error('Terrain texture unavailable');
    terrainTexture.get('__BASE').setSize(this.manifest.imageSize[0]! - 4, this.manifest.imageSize[1]! - 4);
    terrainTexture.setFilter(Phaser.Textures.FilterMode.NEAREST);
    this.cameras.main.setBounds(0, 0, WORLD.width, WORLD.height);
    this.cameras.main.roundPixels = true;
    this.ring = this.add.rectangle(0, 0, 16, 16).setStrokeStyle(1, 0xe6ce85).setFillStyle(0, 0).setDepth(100).setVisible(false);
    this.workMarks = this.add.graphics().setDepth(4);
    this.combatMarks = this.add.graphics().setDepth(5);
    this.rebuild();
    this.unsubscribe = this.controller.subscribe((reset) => {
      if (reset) { this.rebuild(); this.command({ type: 'center' }); }
    });
    this.applyView();
    this.stage.dataset.ready = 'true';
    element('loading').hidden = true;
    const canvas = this.game.canvas;
    canvas.setAttribute('aria-hidden', 'true');
    canvas.addEventListener('contextmenu', this.contextMenu);
    this.stage.addEventListener('keydown', this.keyDown);
    this.stage.addEventListener('keyup', this.keyUp);
    this.stage.addEventListener('blur', this.clearKeys);
    window.addEventListener('blur', this.clearKeys);
    this.scale.on('resize', this.applyView, this);
    this.input.on('pointerdown', this.pointerDown, this);
    this.input.on('pointermove', this.pointerMove, this);
    this.input.on('pointerup', this.pointerUp, this);
    this.input.on('pointerupoutside', () => { this.drag = undefined; });
    this.input.on('wheel', (pointer: Phaser.Input.Pointer, _objects: unknown, _dx: number, dy: number) => {
      if (dy === 0) return;
      this.view = zoomAt(this.view, this.size(), dy < 0 ? 1 : -1, pointer); this.applyView();
    });
    this.events.once('shutdown', () => {
      this.unsubscribe?.();
      this.stage.removeEventListener('keydown', this.keyDown); this.stage.removeEventListener('keyup', this.keyUp);
      this.stage.removeEventListener('blur', this.clearKeys); window.removeEventListener('blur', this.clearKeys);
      canvas.removeEventListener('contextmenu', this.contextMenu); this.scale.off('resize', this.applyView, this);
    });
  }
  private contextMenu = (event: Event): void => event.preventDefault();
  private clearKeys = (): void => { this.keys.clear(); this.drag = undefined; };
  private keyDown = (event: KeyboardEvent): void => {
    const key = event.key.toLowerCase();
    if (['arrowleft', 'arrowright', 'arrowup', 'arrowdown', 'w', 'a', 's', 'd'].includes(key)) { event.preventDefault(); this.keys.add(key); }
    if (event.repeat) return;
    if (['+', '=', '-', '_', 'home', 'escape'].includes(key)) event.preventDefault();
    if (key === '+' || key === '=') this.command({ type: 'zoom', delta: 1 });
    if (key === '-' || key === '_') this.command({ type: 'zoom', delta: -1 });
    if (key === 'home') this.command({ type: 'center' });
    if (key === 'escape') this.command({ type: 'select', id: null });
  };
  private keyUp = (event: KeyboardEvent): void => { this.keys.delete(event.key.toLowerCase()); };
  private size(): Size { return { width: this.scale.width, height: this.scale.height }; }
  private pointerDown(pointer: Phaser.Input.Pointer): void {
    this.stage.focus({ preventScroll: true });
    this.drag = { x: pointer.x, y: pointer.y, view: this.view, moved: false, button: pointer.button };
  }
  private pointerMove(pointer: Phaser.Input.Pointer): void {
    if (!this.drag || !pointer.isDown) return;
    const dx = pointer.x - this.drag.x; const dy = pointer.y - this.drag.y;
    if (Math.hypot(dx, dy) > 5) this.drag.moved = true;
    if (this.drag.moved) {
      this.view = { ...this.view, x: this.drag.view.x - dx / this.view.zoom, y: this.drag.view.y - dy / this.view.zoom };
      this.applyView();
    }
  }
  private pointerUp(pointer: Phaser.Input.Pointer): void {
    if (this.drag && !this.drag.moved) {
      const x = this.view.x + (pointer.x - this.scale.width / 2) / this.view.zoom;
      const y = this.view.y + (pointer.y - this.scale.height / 2) / this.view.zoom;
      if (this.drag.button === 0) {
        const hit = [...this.people].find(([, image]) => image.visible && Math.abs(image.x - x) <= 7 && Math.abs(image.y - y) <= 7);
        this.command({ type: 'select', id: hit?.[0] ?? null });
      }
      if (this.drag.button === 2) this.controller.order({ x: Math.floor(x / TILE), y: Math.floor(y / TILE) });
    }
    this.drag = undefined;
  }
  command(command: UiCommand): void {
    if (this.stage.dataset.ready !== 'true') return;
    switch (command.type) {
      case 'select': {
        this.controller.select(command.id);
        const image = command.id === null ? undefined : this.people.get(command.id);
        if (image && command.focus) this.view = { ...this.view, x: image.x, y: image.y };
        break;
      }
      case 'zoom': this.view = zoomAt(this.view, this.size(), command.delta); break;
      case 'pan': this.view = { ...this.view, x: this.view.x + command.dx, y: this.view.y + command.dy }; break;
      case 'focus': this.view = {...this.view,x:(command.x+.5)*TILE,y:(command.y+.5)*TILE};break;
      case 'center': this.view = { x: 64.5 * TILE, y: 64.5 * TILE, zoom: 3 }; break;
    }
    this.applyView();
  }
  private applyView(): void {
    this.view = constrain(this.view, this.size());
    this.cameras.main.setZoom(this.view.zoom).centerOn(this.view.x, this.view.y);
    // Resize can change zoom limits while the camera view stays identical.
    element<HTMLButtonElement>('zoom-in').disabled = this.view.zoom >= Math.max(MAX_ZOOM, minZoom(this.size()));
    element<HTMLButtonElement>('zoom-out').disabled = this.view.zoom <= minZoom(this.size());
    const serialized = JSON.stringify(this.view);
    if (serialized === this.lastView) return;
    this.lastView = serialized;
    this.stage.dataset.camera = serialized;
    element<HTMLOutputElement>('zoom').value = `${this.view.zoom * 100}%`;
  }
  private rebuild(): void {
    this.map?.destroy();
    this.decorations.forEach(image => image.destroy()); this.decorations = [];
    this.people.forEach(image => image.destroy()); this.people.clear();
    const world = this.controller.world;
    const frameIndex = (alias: string): number => {
      const frame = this.manifest.sprites[alias]!.frame;
      return Math.floor(frame[1]! / 20) * 8 + Math.floor(frame[0]! / 20);
    };
    const data = Array.from({ length: 128 }, (_, y) => Array.from({ length: 128 }, (_, x) => frameIndex(world.terrain[y * 128 + x] === 2 ? 'rock' : world.terrain[y * 128 + x] === 1 ? 'path' : (x + y) % 5 ? 'grass' : 'grass-alt')));
    this.map = this.make.tilemap({ data, tileWidth: TILE, tileHeight: TILE });
    const tiles = this.map.addTilesetImage('terrain', 'terrain', TILE, TILE, 2, 8);
    if (!tiles) throw new Error('Terrain tileset unavailable');
    this.map.createLayer(0, tiles, 0, 0)!.setAlpha(0.38);
    for (const r of world.resources.filter(r=>r.remaining>0)) this.decorations.push(this.add.image((r.x + .5) * TILE, (r.y + .5) * TILE, 'colony', r.kind === 'wood' ? 'oak' : r.kind === 'stone' ? 'rock' : 'apple').setDepth(2));
    for (const s of world.structures) if(s.kind!=='stockpile') this.decorations.push(this.add.image((s.x+.5)*TILE,(s.y+.5)*TILE,'colony',WORK_CONTENT.buildings[s.kind].sprite).setTint(s.kind==='door'&&s.open?0x95cfa0:0xffffff).setDepth(2));
    this.decorations.push(this.add.image((world.heart.x + .5) * TILE, (world.heart.y + .5) * TILE, 'colony', 'heart').setScale(2).setDepth(2));
    for (const p of world.people) this.people.set(p.id, this.add.image((p.x + .5) * TILE, (p.y + .5) * TILE, 'colony', founders[p.founder]!.sprite).setDepth(3));
    this.terrainVersion = world.terrainVersion;
    this.drawPeople();
  }
  private drawPeople(): void {
    const world = this.controller.world;
    this.workMarks.clear();
    for(const s of world.structures) if(s.kind==='stockpile') this.workMarks.lineStyle(1,0x95cfa0,.9).strokeRect(s.x*TILE+1,s.y*TILE+1,TILE-2,TILE-2);
    for(const s of world.structures)if(s.kind==='infirmary')this.workMarks.fillStyle(0xffffff).fillRect((s.x+.42)*TILE,(s.y+.2)*TILE,2,8).fillRect((s.x+.23)*TILE,(s.y+.39)*TILE,8,2);
    for(const s of world.structures){
      const x=s.x*TILE,y=s.y*TILE;
      if(s.kind==='tower')this.workMarks.lineStyle(1,0xe6ce85).strokeRect(x+1,y+1,TILE-2,TILE-2).fillStyle(0xe6ce85).fillRect(x+4,y-3,4,4);
      if(s.kind==='trap')this.workMarks.lineStyle(1,s.inventory.ammo?0xe6ce85:0x888888).lineBetween(x+2,y+2,x+10,y+10).lineBetween(x+2,y+10,x+10,y+2);
      if(s.kind==='guardpost')this.workMarks.lineStyle(1,0x8fbcdc).lineBetween(x+3,y+1,x+3,y+11).fillStyle(0x8fbcdc).fillTriangle(x+4,y+1,x+11,y+4,x+4,y+6);
    }
    for(const j of world.jobs) if(j.kind==='build'&&active(j)) this.workMarks.lineStyle(1,j.state==='suspended'?0x999999:0xe6ce85,.9).strokeRect(j.x*TILE+1,j.y*TILE+1,TILE-2,TILE-2);
    for(const b of world.piles) if(total(b.inventory)) this.workMarks.fillStyle(0xe6ce85).fillRect((b.x+.3)*TILE,(b.y+.3)*TILE,5,5);
    for (const p of world.people) {
      const position = renderedPosition(p, this.controller.clock.paused ? 0 : this.controller.clock.alpha);
      this.people.get(p.id)?.setVisible(p.life!=='dead').setTint(p.life==='downed'?0xe36f62:p.body.health<70?0xe6ad85:0xffffff);
      if(p.life==='downed')this.workMarks.lineStyle(1,0xe36f62).strokeCircle((position.x+.5)*TILE,(position.y+.5)*TILE,8);
      if(p.body.routine)this.workMarks.fillStyle(p.body.routine.kind==='rest'?0x8fbcdc:p.body.routine.kind==='eat'?0x95cfa0:0xe36f62).fillRect((position.x+.1)*TILE,(position.y+.1)*TILE,3,3);
      if(total(p.cargo)) this.workMarks.fillStyle(0xe6ce85).fillRect((position.x+.8)*TILE,(position.y+.7)*TILE,3,3);
      this.people.get(p.id)?.setPosition((position.x + .5) * TILE, (position.y + .5) * TILE);
    }
    const selected = this.controller.selected === null ? undefined : this.people.get(this.controller.selected);
    this.ring.setVisible(Boolean(selected)); if (selected) this.ring.setPosition(selected.x, selected.y);
    this.drawCombat();
  }
  private drawCombat(): void {
    const world=this.controller.world, combat=world.combat, marks=this.combatMarks;
    marks.clear();
    const alpha=this.controller.clock.paused?0:this.controller.clock.alpha;
    for(const enemy of combat.raiders){
      const ratio=enemy.next?Math.min(1,(enemy.progress+alpha)/MOVE_TICKS):0;
      const x=(enemy.x+(enemy.next?enemy.next.x-enemy.x:0)*ratio+.5)*TILE;
      const y=(enemy.y+(enemy.next?enemy.next.y-enemy.y:0)*ratio+.5)*TILE;
      if(enemy.kind==='breacher')marks.fillStyle(0xf0a45e).fillRect(x-5,y-5,10,10).lineStyle(1,0x381c17).strokeRect(x-5,y-5,10,10).lineBetween(x-3,y-3,x+3,y+3);
      else marks.fillStyle(0xf07565).fillPoints([{x,y:y-5},{x:x+5,y},{x,y:y+5},{x:x-5,y}],true).lineStyle(1,0x381c17).strokePoints([{x,y:y-5},{x:x+5,y},{x,y:y+5},{x:x-5,y}],true);
    }
    for(const attack of combat.attacks){
      const color=attack.faction==='hostile'?0xff796b:0xffd877;
      for(const cell of attack.cells)marks.fillStyle(color,.2).fillRect(cell.x*TILE,cell.y*TILE,TILE,TILE).lineStyle(1,color).strokeRect(cell.x*TILE+1,cell.y*TILE+1,TILE-2,TILE-2);
      marks.lineStyle(1,color,.45).lineBetween((attack.origin.x+.5)*TILE,(attack.origin.y+.5)*TILE,(attack.target.x+.5)*TILE,(attack.target.y+.5)*TILE);
      let label=this.attackLabels.get(attack.id);
      if(!label){label=this.add.text(0,0,'',{fontFamily:'monospace',fontSize:'7px',color:'#ffffff',backgroundColor:'#251d18'}).setOrigin(.5,1).setDepth(6);this.attackLabels.set(attack.id,label);}
      label.setPosition((attack.target.x+.5)*TILE,attack.target.y*TILE).setText(String(Math.max(0,attack.due-world.tick)));
    }
    for(const [id,label] of this.attackLabels)if(!combat.attacks.some(attack=>attack.id===id)){label.destroy();this.attackLabels.delete(id);}
  }
  update(_time: number, delta: number): void {
    if (this.stage.dataset.ready !== 'true') return;
    this.controller.frame(this.game.loop.rawDelta);
    if (this.terrainVersion !== this.controller.world.terrainVersion) this.rebuild();
    this.drawPeople();
    if (document.activeElement !== this.stage) return;
    const dx = Number(this.keys.has('d') || this.keys.has('arrowright')) - Number(this.keys.has('a') || this.keys.has('arrowleft'));
    const dy = Number(this.keys.has('s') || this.keys.has('arrowdown')) - Number(this.keys.has('w') || this.keys.has('arrowup'));
    if (dx || dy) {
      const distance = Math.min(delta, 50) * 0.3 / this.view.zoom;
      this.view = { ...this.view, x: this.view.x + dx * distance, y: this.view.y + dy * distance }; this.applyView();
    }
  }
}
