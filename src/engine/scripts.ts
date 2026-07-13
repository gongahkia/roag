import { SCRIPT_BY_ITEM } from '../content'
import type { Hero, ItemId } from '../types'
import { intellectFocusDiscount, intellectScriptRange } from './intellect'

export const scriptForItem = (id: ItemId) => {
  const script = SCRIPT_BY_ITEM[id]
  if (!script) throw new Error(`unknown script item: ${id}`)
  return script
}
export const scriptCastProfile = (hero: Hero, id: ItemId) => {
  const script = scriptForItem(id)
  return { script, focusCost: Math.max(1, script.focusCost - intellectFocusDiscount(hero)), range: script.range + intellectScriptRange(hero) - 1 }
}
