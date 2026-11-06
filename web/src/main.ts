import Phaser from 'phaser';
import './style.css';
import { assetUrl, validateManifest } from './assets.ts';
import { ColonyScene } from './presentation/ColonyScene.ts';
import { element, setupPanels } from './ui/panels.ts';
import { ColonyController } from './platform/controller.ts';
import { SaveStore } from './persistence/store.ts';
const fail=(message:string):void=>{const loading=element('loading');loading.hidden=false;loading.textContent=message;loading.setAttribute('role','alert');element('stage').dataset.ready='error';};
async function boot():Promise<void>{
  const response=await fetch(assetUrl('manifest.json'));if(!response.ok)throw new Error('Artwork manifest could not be loaded. Reload to try again.');
  const manifest=validateManifest(await response.json());
  const requested=new URLSearchParams(location.search).get('scenario');
  const scenario=requested==='routing-lab'?'routing-lab':requested==='work-lab'?'work-lab':'colony';
  const controller=new ColonyController(new SaveStore(scenario==='colony'?'active':scenario),42,scenario);
  await controller.restore();
  const update=setupPanels(manifest,controller,command=>scene.command(command));
  const scene=new ColonyScene(manifest,controller,fail);
  const unsubscribe=controller.subscribe(update);update();
  const visibility=()=>controller.visibility(document.hidden);document.addEventListener('visibilitychange',visibility);visibility();
  const game=new Phaser.Game({type:Phaser.AUTO,parent:'stage',backgroundColor:'#1b2720',pixelArt:true,roundPixels:true,banner:false,audio:{noAudio:true},input:{keyboard:false},scale:{mode:Phaser.Scale.RESIZE,width:element('stage').clientWidth,height:element('stage').clientHeight},scene:[scene]});
  if(import.meta.hot)import.meta.hot.dispose(()=>{game.destroy(true);unsubscribe();document.removeEventListener('visibilitychange',visibility);});
}
void boot().catch((error:unknown)=>fail(error instanceof Error?error.message:'The clearing could not be opened.'));
