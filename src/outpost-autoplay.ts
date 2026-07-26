import { moveOutpost, outpostInteraction } from './engine'
import { DIRECTIONS, type Direction, type Point } from './types'

const commands: Record<Direction, string> = { nw: 'i', n: 'o', ne: 'p', w: 'k', wait: 'l', e: ';', sw: ',', s: '.', se: '/' }
const directions = (Object.keys(DIRECTIONS) as Direction[]).filter((direction): direction is Exclude<Direction, 'wait'> => direction !== 'wait')
const keyOf = (point: Point): string => `${point.x},${point.y}`

export const outpostAutoplayCommand = (position: Point): string | undefined => {
  if (outpostInteraction(position)?.destination === 'routes') return 'c'
  const queue: Array<{ point: Point; first: Exclude<Direction, 'wait'> }> = []
  const visited = new Set([keyOf(position)])
  for (const direction of directions) {
    const move = moveOutpost(position, direction)
    if (!move.moved || visited.has(keyOf(move.position))) continue
    if (outpostInteraction(move.position)?.destination === 'routes') return commands[direction]
    visited.add(keyOf(move.position))
    queue.push({ point: move.position, first: direction })
  }
  for (let index = 0; index < queue.length; index++) {
    const current = queue[index]
    for (const direction of directions) {
      const move = moveOutpost(current.point, direction)
      if (!move.moved || visited.has(keyOf(move.position))) continue
      if (outpostInteraction(move.position)?.destination === 'routes') return commands[current.first]
      visited.add(keyOf(move.position))
      queue.push({ point: move.position, first: current.first })
    }
  }
  return undefined
}
