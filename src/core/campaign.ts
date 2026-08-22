import type { Course, CourseFeature, CourseHazard, Point, PortalPair, Tile } from './types';

/** Shared by local and authoritative online play. */
export const COURSE_TRANSITION_DURATION_MS = 2_900;
export const CAMPAIGN_EXCAVATION_RADIUS = 3;

type Rotation = 0 | 1 | 2 | 3;

export interface CourseExpansion {
  /** The old course embedded in the same coordinate system as `course`. */
  previous: Course;
  /** The authoritative stitched course that takes effect after the build animation. */
  course: Course;
  /** New-course tiles that should arrive during the transition. */
  added: Point[];
  /** Prior-course tiles that are excavated around the previous cup. */
  excavated: Point[];
  /** The shared old-cup/new-tee tile in the stitched coordinate system. */
  anchor: Point;
  /** Converts balls and UI focus from the prior course into the stitched course. */
  offset: Point;
  rotation: Rotation;
}

const isPlayable = (tile: Tile | undefined) => Boolean(tile && tile.surface !== 'void');
const distanceFrom = (from: Point, to: Point) => Math.max(Math.abs(from.x - to.x), Math.abs(from.y - to.y));
const rotateDirection = ({ x, y }: Point, rotation: Rotation): Point => rotation === 0 ? { x, y } : rotation === 1 ? { x: -y, y: x } : rotation === 2 ? { x: -x, y: -y } : { x: y, y: -x };
const rotatedCorners = (corners: Tile['corners'], rotation: Rotation): Tile['corners'] => {
  if (!corners || rotation === 0) return corners ? [...corners] as [number, number, number, number] : undefined;
  if (rotation === 1) return [corners[3], corners[0], corners[1], corners[2]];
  if (rotation === 2) return [corners[2], corners[3], corners[0], corners[1]];
  return [corners[1], corners[2], corners[3], corners[0]];
};
const copyTile = (tile: Tile, theme: Course['theme'], rotation: Rotation = 0): Tile => ({
  ...tile,
  theme: tile.theme ?? theme,
  corners: rotatedCorners(tile.corners, rotation),
  direction: tile.direction ? rotateDirection(tile.direction, rotation) : undefined,
});
const tileAt = (course: Course, point: Point) => point.x < 0 || point.y < 0 || point.x >= course.width || point.y >= course.height ? undefined : course.tiles[point.y * course.width + point.x];
const pointFor = (point: Point, tee: Point, anchor: Point, rotation: Rotation): Point => {
  const x = point.x - tee.x;
  const y = point.y - tee.y;
  if (rotation === 0) return { x: anchor.x + x, y: anchor.y + y };
  if (rotation === 1) return { x: anchor.x - y, y: anchor.y + x };
  if (rotation === 2) return { x: anchor.x - x, y: anchor.y - y };
  return { x: anchor.x + y, y: anchor.y - x };
};
const shifted = (point: Point, offset: Point): Point => ({ x: point.x + offset.x, y: point.y + offset.y });
const hashFor = (value: string) => [...value].reduce((hash, character) => (hash * 31 + character.charCodeAt(0)) >>> 0, 0);
const lineBetween = (from: Point, to: Point) => {
  const points: Point[] = [];
  let { x, y } = from;
  const dx = Math.abs(to.x - x);
  const dy = -Math.abs(to.y - y);
  const stepX = x < to.x ? 1 : -1;
  const stepY = y < to.y ? 1 : -1;
  let error = dx + dy;
  while (true) {
    points.push({ x, y });
    if (x === to.x && y === to.y) return points;
    const twiceError = error * 2;
    if (twiceError >= dy) { error += dy; x += stepX; }
    if (twiceError <= dx) { error += dx; y += stepY; }
  }
};
const transformedBounds = (course: Course, anchor: Point, rotation: Rotation) => {
  const corners = [
    { x: 0, y: 0 },
    { x: course.width - 1, y: 0 },
    { x: 0, y: course.height - 1 },
    { x: course.width - 1, y: course.height - 1 },
  ].map((point) => pointFor(point, course.tee, anchor, rotation));
  return {
    minX: Math.min(...corners.map((point) => point.x)),
    maxX: Math.max(...corners.map((point) => point.x)),
    minY: Math.min(...corners.map((point) => point.y)),
    maxY: Math.max(...corners.map((point) => point.y)),
  };
};

const rotationFor = (previous: Course, next: Course, anchor: Point) => {
  const preferred = hashFor(`${previous.seed}:${next.seed}`) % 4;
  const score = (rotation: Rotation) => {
    let collision = 0;
    let localPressure = 0;
    for (let y = 0; y < next.height; y += 1) {
      for (let x = 0; x < next.width; x += 1) {
        const source = next.tiles[y * next.width + x]!;
        if (!isPlayable(source)) continue;
        const destination = pointFor({ x, y }, next.tee, anchor, rotation);
        if (!isPlayable(tileAt(previous, destination))) continue;
        if (distanceFrom(destination, anchor) > CAMPAIGN_EXCAVATION_RADIUS) collision += 1;
        else localPressure += 1;
      }
    }
    return [collision, localPressure, (rotation - preferred + 4) % 4] as const;
  };
  return ([0, 1, 2, 3] as Rotation[]).sort((left, right) => {
    const leftScore = score(left);
    const rightScore = score(right);
    return leftScore[0] - rightScore[0] || leftScore[1] - rightScore[1] || leftScore[2] - rightScore[2];
  })[0]!;
};

const translateHazards = (hazards: readonly CourseHazard[], point: (point: Point) => Point, direction: (value: Point) => Point, prefix = ''): CourseHazard[] => hazards.map((hazard) => hazard.kind === 'updraft'
  ? { ...hazard, id: `${prefix}${hazard.id}`, point: point(hazard.point), direction: direction(hazard.direction) }
  : { ...hazard, id: `${prefix}${hazard.id}`, point: point(hazard.point) });
const translateFeatures = (features: readonly CourseFeature[], point: (point: Point) => Point, direction: (value: Point) => Point, prefix = ''): CourseFeature[] => features.map((feature) => feature.kind === 'sinkhole'
  ? { ...feature, id: `${prefix}${feature.id}`, entrance: point(feature.entrance), exit: point(feature.exit) }
  : feature.kind === 'pulse'
    ? { ...feature, id: `${prefix}${feature.id}`, point: point(feature.point), direction: direction(feature.direction) }
    : { ...feature, id: `${prefix}${feature.id}`, point: point(feature.point) });
const translatePortals = (portals: readonly PortalPair[] | undefined, point: (point: Point) => Point, direction: (value: Point) => Point, prefix = '') => portals?.map((portal) => ({
  ...portal,
  id: `${prefix}${portal.id}`,
  entrance: portal.entrance ? { point: point(portal.entrance.point), direction: direction(portal.entrance.direction) } : undefined,
  exit: portal.exit ? { point: point(portal.exit.point), direction: direction(portal.exit.direction) } : undefined,
}));

const embeddedPrevious = (course: Course, width: number, height: number, offset: Point): Course => {
  const tiles = Array.from({ length: width * height }, (): Tile => ({ surface: 'void', height: 0 }));
  for (let y = 0; y < course.height; y += 1) for (let x = 0; x < course.width; x += 1) tiles[(y + offset.y) * width + x + offset.x] = copyTile(course.tiles[y * course.width + x]!, course.theme);
  const point = (value: Point) => shifted(value, offset);
  return {
    ...course,
    id: `${course.id}:embedded`,
    width,
    height,
    tiles,
    tee: point(course.tee),
    cup: point(course.cup),
    route: course.route.map(point),
    hazards: translateHazards(course.hazards, point, (value) => ({ ...value })),
    features: translateFeatures(course.features ?? [], point, (value) => ({ ...value })),
    portals: translatePortals(course.portals, point, (value) => ({ ...value })),
    itemPads: course.itemPads.map((pad) => ({ ...pad, point: point(pad.point) })),
  };
};

/**
 * Stitches a generated hole to the prior cup. Rotation is selected deterministically
 * by minimising overlap outside the fixed excavation neighborhood around that cup.
 */
export const expandCourseAtCup = (previous: Course, next: Course): CourseExpansion => {
  const rawAnchor = { ...previous.cup };
  const rotation = rotationFor(previous, next, rawAnchor);
  const bounds = transformedBounds(next, rawAnchor, rotation);
  const minX = Math.min(0, bounds.minX);
  const minY = Math.min(0, bounds.minY);
  const maxX = Math.max(previous.width - 1, bounds.maxX);
  const maxY = Math.max(previous.height - 1, bounds.maxY);
  const offset = { x: -minX, y: -minY };
  const width = maxX - minX + 1;
  const height = maxY - minY + 1;
  const previousEmbedded = embeddedPrevious(previous, width, height, offset);
  const anchor = shifted(rawAnchor, offset);
  const excavated: Point[] = [];
  const tiles: Tile[] = previousEmbedded.tiles.map((tile): Tile => ({ ...tile, corners: tile.corners ? [...tile.corners] as [number, number, number, number] : undefined, direction: tile.direction ? { ...tile.direction } : undefined }));
  for (let y = 0; y < height; y += 1) for (let x = 0; x < width; x += 1) {
    const point = { x, y };
    if (distanceFrom(point, anchor) <= CAMPAIGN_EXCAVATION_RADIUS && isPlayable(tiles[y * width + x])) {
      excavated.push(point);
      tiles[y * width + x] = { surface: 'void', height: 0 };
    }
  }

  const transform = (point: Point) => shifted(pointFor(point, next.tee, rawAnchor, rotation), offset);
  const added: Point[] = [];
  const addTile = (point: Point) => { if (!added.some((candidate) => candidate.x === point.x && candidate.y === point.y)) added.push(point); };
  for (let y = 0; y < next.height; y += 1) for (let x = 0; x < next.width; x += 1) {
    const source = next.tiles[y * next.width + x]!;
    if (!isPlayable(source)) continue;
    const destination = transform({ x, y });
    tiles[destination.y * width + destination.x] = copyTile(source, next.theme, rotation);
    addTile(destination);
  }
  const previousApproach = previousEmbedded.route.at(-2) ?? previousEmbedded.tee;
  const nextExit = transform(next.route[1] ?? next.cup);
  const connector = [
    ...lineBetween(anchor, previousApproach),
    ...lineBetween(anchor, nextExit),
  ].filter((point) => distanceFrom(point, anchor) <= CAMPAIGN_EXCAVATION_RADIUS);
  connector.forEach((point) => {
    const index = point.y * width + point.x;
    if (point.x < 0 || point.y < 0 || point.x >= width || point.y >= height || (point.x === anchor.x && point.y === anchor.y)) return;
    if (!isPlayable(tiles[index])) {
      tiles[index] = { surface: 'fairway', height: 0, theme: previous.theme };
      addTile(point);
    }
  });
  const wasExcavated = (point: Point) => distanceFrom(point, anchor) <= CAMPAIGN_EXCAVATION_RADIUS;
  const keepHazard = (hazard: CourseHazard) => !wasExcavated(hazard.point);
  const keepFeature = (feature: CourseFeature) => feature.kind === 'sinkhole'
    ? !wasExcavated(feature.entrance) && !wasExcavated(feature.exit)
    : !wasExcavated(feature.point);
  const keepPortal = (portal: PortalPair) => (!portal.entrance || !wasExcavated(portal.entrance.point)) && (!portal.exit || !wasExcavated(portal.exit.point));
  const prefix = `hole-${next.id}:`;
  const course: Course = {
    ...next,
    id: `${previous.id}:to:${next.id}`,
    seed: `${previous.seed}:to:${next.seed}`,
    width,
    height,
    tiles,
    tee: anchor,
    cup: transform(next.cup),
    route: next.route.map(transform),
    hazards: [
      ...translateHazards(previousEmbedded.hazards.filter(keepHazard), (point) => ({ ...point }), (value) => ({ ...value })),
      ...translateHazards(next.hazards, transform, (value) => rotateDirection(value, rotation), prefix),
    ],
    features: [
      ...translateFeatures(previousEmbedded.features.filter(keepFeature), (point) => ({ ...point }), (value) => ({ ...value })),
      ...translateFeatures(next.features ?? [], transform, (value) => rotateDirection(value, rotation), prefix),
    ],
    portals: [
      ...(translatePortals(previousEmbedded.portals?.filter(keepPortal), (point) => ({ ...point }), (value) => ({ ...value })) ?? []),
      ...(translatePortals(next.portals, transform, (value) => rotateDirection(value, rotation), prefix) ?? []),
    ],
    itemPads: [
      ...previousEmbedded.itemPads.filter((pad) => !wasExcavated(pad.point)),
      ...next.itemPads.map((pad) => ({ ...pad, id: `${prefix}${pad.id}`, point: transform(pad.point) })),
    ],
  };
  return { previous: previousEmbedded, course, added, excavated, anchor, offset, rotation };
};
