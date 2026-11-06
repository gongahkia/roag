import { emptyStock, total } from './work-content.ts';
import type { Stock } from './work-content.ts';
export const DUTIES = ['gatherer','builder','cook','healer','guard'] as const;
export type Duty = typeof DUTIES[number];
export const defaultDuties = (): Record<Duty,number> => ({gatherer:1,builder:1,cook:1,healer:1,guard:0});
export const NEEDS = {dayTicks:3600, mealAt:1800, starveAt:3600, restAt:2400, exhaustedAt:4800,
  mealReduction:1800, rawReduction:900, eatTicks:20, restReduction:12, restTarget:200,
  damageEvery:20, rescueWindow:900, rescueTicks:40, treatmentTicks:60, recoveryTicks:100,
  plantingTicks:20, tendingTicks:20, growthTicks:240, harvestTicks:20, harvestYield:5} as const;
const stock=(values:Partial<Stock>):Stock=>({...emptyStock(),...values});
export const RECIPES = {
  meal:{facility:'kitchen',duty:'cook',cost:stock({food:1,wood:1}),output:stock({meal:4}),ticks:30},
  ore:{facility:'workshop',duty:'builder',cost:stock({stone:2}),output:stock({ore:1}),ticks:30},
  medicine:{facility:'infirmary',duty:'healer',cost:stock({food:2,ore:1}),output:stock({medicine:2}),ticks:40},
  tool:{facility:'workshop',duty:'builder',cost:stock({ore:1,wood:1}),output:stock({tool:1}),ticks:30},
} as const;
export type Recipe = keyof typeof RECIPES;
export function validateSurvivalContent(n:typeof NEEDS=NEEDS,r:typeof RECIPES=RECIPES):void {
  if(Object.values(n).some(v=>!Number.isSafeInteger(v)||v<1||v>10000)||n.mealAt>=n.starveAt||n.restAt>=n.exhaustedAt||n.restTarget>=n.restAt)throw Error('Invalid survival tuning');
  for(const recipe of Object.values(r))if(!DUTIES.includes(recipe.duty)||!Number.isSafeInteger(recipe.ticks)||recipe.ticks<1||total(recipe.cost)<1||total(recipe.output)<1||total(recipe.output)>5||[...Object.values(recipe.cost),...Object.values(recipe.output)].some(v=>!Number.isSafeInteger(v)||v<0||v>100))throw Error('Invalid production recipe');
}
validateSurvivalContent();
