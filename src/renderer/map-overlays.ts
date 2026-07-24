import { MAP_HEIGHT, MAP_WIDTH, type Actor, type Floor, type GroundItem, type Point, type Prop, type Telegraph } from '../types'

export interface TargetOverlay { path: readonly Point[]; cells: readonly Point[] }
export interface MapOverlays {
  items: Array<GroundItem | undefined>
  props: Array<Prop | undefined>
  actors: Array<Actor | undefined>
  telegraphs: Array<Telegraph | undefined>
  previewPath: Uint8Array
  previewCells: Uint8Array
}

const cellCount = MAP_WIDTH * MAP_HEIGHT
export const mapCellIndex = (x: number, y: number): number => y * MAP_WIDTH + x
const inMap = ({ x, y }: Point): boolean => x >= 0 && x < MAP_WIDTH && y >= 0 && y < MAP_HEIGHT

export const mapOverlays = (floor: Pick<Floor, 'items' | 'props' | 'actors' | 'telegraphs'>, preview?: TargetOverlay): MapOverlays => {
  const items = Array<GroundItem | undefined>(cellCount)
  const props = Array<Prop | undefined>(cellCount)
  const actors = Array<Actor | undefined>(cellCount)
  const telegraphs = Array<Telegraph | undefined>(cellCount)
  const previewPath = new Uint8Array(cellCount)
  const previewCells = new Uint8Array(cellCount)
  for (const item of floor.items) if (inMap(item)) items[mapCellIndex(item.x, item.y)] ??= item
  for (const prop of floor.props) if (prop.state !== 'destroyed' && inMap(prop)) props[mapCellIndex(prop.x, prop.y)] ??= prop
  for (const actor of floor.actors) if (actor.health > 0 && inMap(actor)) actors[mapCellIndex(actor.x, actor.y)] ??= actor
  for (const telegraph of floor.telegraphs ?? []) for (const point of telegraph.cells) if (inMap(point)) telegraphs[mapCellIndex(point.x, point.y)] ??= telegraph
  for (const point of preview?.path ?? []) if (inMap(point)) previewPath[mapCellIndex(point.x, point.y)] = 1
  for (const point of preview?.cells ?? []) if (inMap(point)) previewCells[mapCellIndex(point.x, point.y)] = 1
  return { items, props, actors, telegraphs, previewPath, previewCells }
}
