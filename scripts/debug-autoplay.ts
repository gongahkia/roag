import { newRun, perform } from '../src/engine'
import { autoplayDecision, autoplayStateFingerprint, createAutoplayContext, recordAutoplayTransition } from '../src/autoplay'
import { getTile } from '../src/world'

const state = newRun(7, 'mine')
const context = createAutoplayContext()
for (let step = 0; step < 14; step++) {
  const decision = autoplayDecision(state, 'omniscient', 'clear', context)
  console.log(JSON.stringify({ step, turn: state.turn, hero: state.hero, currentVisits: context.visits.get(autoplayStateFingerprint(state)), visits: [...context.visits.entries()].sort((a, b) => b[1] - a[1]).slice(0, 2), decision, targets: state.floor.tiles.flatMap((tile, index) => tile.kind === 'crate' || tile.kind === 'chest' ? [{ kind: tile.kind, x: index % 48, y: Math.floor(index / 48) }] : []), nearby: Array.from({ length: 7 }, (_, row) => Array.from({ length: 7 }, (_, column) => getTile(state.floor, state.hero.x + column - 3, state.hero.y + row - 3)?.kind?.[0] ?? '#').join('')) }))
  if (!decision) {
    const targets = state.floor.tiles.flatMap((tile, index) => tile.kind === 'crate' || tile.kind === 'chest' ? [{ x: index % 48, y: Math.floor(index / 48) }] : [])
    const goal = new Set(targets.flatMap(target => [[0, -1], [1, 0], [0, 1], [-1, 0]].map(([x, y]) => `${target.x + x},${target.y + y}`)))
    const blocked = new Set(['wall', 'lava', 'pit', 'rubble', 'bramble', 'crate', 'chest'])
    const queue = [{ x: state.hero.x, y: state.hero.y, path: '' }]
    const seen = new Set([`${state.hero.x},${state.hero.y}`])
    let route: string | undefined
    while (queue.length) {
      const current = queue.shift()!
      if (goal.has(`${current.x},${current.y}`)) { route = current.path; break }
      for (const [letter, x, y] of [['i', -1, -1], ['o', 0, -1], ['p', 1, -1], ['k', -1, 0], [';', 1, 0], [',', -1, 1], ['.', 0, 1], ['/', 1, 1]] as const) {
        const point = { x: current.x + x, y: current.y + y }
        const key = `${point.x},${point.y}`
        const tile = getTile(state.floor, point.x, point.y)
        if (seen.has(key) || !tile || blocked.has(tile.kind) || tile.kind === 'lockedDoor' || state.floor.actors.some(actor => actor.health > 0 && actor.x === point.x && actor.y === point.y)) continue
        seen.add(key)
        queue.push({ ...point, path: current.path + letter })
      }
    }
    console.log(JSON.stringify({ plainRoute: route, goal: [...goal] }))
    break
  }
  const before = structuredClone(state)
  perform(state, decision.command)
  recordAutoplayTransition(context, before, decision.command, state)
}
