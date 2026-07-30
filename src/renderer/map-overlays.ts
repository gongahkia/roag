import { MAP_HEIGHT, MAP_WIDTH, floorIndex, inFloorBounds, type Actor, type Floor, type GroundItem, type Point, type Prop, type Telegraph } from '../types'

export interface TargetOverlay { path: readonly Point[]; cells: readonly Point[] }
export interface MapOverlays {
  items: Array<GroundItem | undefined>
  props: Array<Prop | undefined>
  actors: Array<Actor | undefined>
  telegraphs: Array<Telegraph | undefined>
  previewPath: Uint8Array
  previewCells: Uint8Array
}

export function mapCellIndex(floor: Pick<Floor, 'width'>, x: number, y: number): number
export function mapCellIndex(x: number, y: number): number
export function mapCellIndex(floorOrX: Pick<Floor, 'width'> | number, xOrY: number, maybeY?: number): number {
  return typeof floorOrX === 'number' ? xOrY * MAP_WIDTH + floorOrX : floorIndex(floorOrX, xOrY, maybeY!)
}

export const mapOverlays = (floor: Pick<Floor, 'items' | 'props' | 'actors' | 'telegraphs'> & Partial<Pick<Floor, 'width' | 'height'>>, preview?: TargetOverlay): MapOverlays => {
  const dimensions = { width: floor.width ?? MAP_WIDTH, height: floor.height ?? MAP_HEIGHT }
  const cellCount = dimensions.width * dimensions.height
  const inMap = ({ x, y }: Point): boolean => inFloorBounds(dimensions, x, y)
  const items = Array<GroundItem | undefined>(cellCount)
  const props = Array<Prop | undefined>(cellCount)
  const actors = Array<Actor | undefined>(cellCount)
  const telegraphs = Array<Telegraph | undefined>(cellCount)
  const previewPath = new Uint8Array(cellCount)
  const previewCells = new Uint8Array(cellCount)
  for (const item of floor.items) if (inMap(item)) items[mapCellIndex(dimensions, item.x, item.y)] ??= item
  for (const prop of floor.props) if (prop.state !== 'destroyed' && inMap(prop)) props[mapCellIndex(dimensions, prop.x, prop.y)] ??= prop
  for (const actor of floor.actors) if (actor.health > 0 && inMap(actor)) actors[mapCellIndex(dimensions, actor.x, actor.y)] ??= actor
  for (const telegraph of floor.telegraphs ?? []) for (const point of telegraph.cells) if (inMap(point)) telegraphs[mapCellIndex(dimensions, point.x, point.y)] ??= telegraph
  for (const point of preview?.path ?? []) if (inMap(point)) previewPath[mapCellIndex(dimensions, point.x, point.y)] = 1
  for (const point of preview?.cells ?? []) if (inMap(point)) previewCells[mapCellIndex(dimensions, point.x, point.y)] = 1
  return { items, props, actors, telegraphs, previewPath, previewCells }
}

export const visibleMapActor = (floor: Pick<Floor, 'tiles' | 'width'>, actor: Actor | undefined): Actor | undefined => actor && floor.tiles[mapCellIndex(floor, actor.x, actor.y)]?.visible ? actor : undefined
