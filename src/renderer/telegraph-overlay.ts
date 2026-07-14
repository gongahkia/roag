import type { Point } from '../types'

export const telegraphBeam = (source: Point | undefined, cells: readonly Point[]): Point[] | undefined => {
  if (!source || cells.length < 2) return undefined
  const points = [source, ...cells]
  let xDirection = 0
  let yDirection = 0
  for (let index = 1; index < points.length; index++) {
    const x = points[index].x - points[index - 1].x
    const y = points[index].y - points[index - 1].y
    if (Math.max(Math.abs(x), Math.abs(y)) !== 1 || (x && xDirection && Math.sign(x) !== xDirection) || (y && yDirection && Math.sign(y) !== yDirection)) return undefined
    xDirection ||= Math.sign(x)
    yDirection ||= Math.sign(y)
  }
  return points
}
