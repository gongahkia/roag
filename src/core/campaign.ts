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
  /** Tiles that should arrive during the transition, including the rebuilt junction. */
  added: Point[];
  /** Prior-course tiles rebuilt as the next hole arrives. */
  excavated: Point[];
  /** The new tee in the stitched coordinate system; it normally shares the old cup. */
  anchor: Point;
  /** Converts balls and UI focus from the prior course into the stitched course. */
  offset: Point;
  rotation: Rotation;
  /** Overlap outside the shared cup-to-tee junction; new plans must keep this at zero. */
  trackOverlapCount: number;
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
const raisedTile = (tile: Tile, theme: Course['theme'], rotation: Rotation, elevationOffset: number): Tile => {
  const copied = copyTile(tile, theme, rotation);
  return {
    ...copied,
    height: copied.height + elevationOffset,
    corners: copied.corners ? copied.corners.map((height) => height + elevationOffset) as [number, number, number, number] : undefined,
  };
};
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
const pointKey = (point: Point) => `${point.x}:${point.y}`;
const hashFor = (value: string) => [...value].reduce((hash, character) => (hash * 31 + character.charCodeAt(0)) >>> 0, 0);
const cardinalDirections: readonly Point[] = [{ x: 1, y: 0 }, { x: -1, y: 0 }, { x: 0, y: 1 }, { x: 0, y: -1 }];
const finalApproachDirection = (route: readonly Point[]) => {
  const end = route.at(-1);
  if (!end) return undefined;
  for (let index = route.length - 2; index >= 0; index -= 1) {
    const point = route[index]!;
    const x = Math.sign(end.x - point.x);
    const y = Math.sign(end.y - point.y);
    if (x || y) return { x, y };
  }
  return undefined;
};
const smoothTerrain = (tiles: Tile[], width: number, height: number) => {
  const vertexWidth = width + 1;
  const vertexIndex = (x: number, y: number) => y * vertexWidth + x;
  const vertices = Array.from({ length: vertexWidth * (height + 1) }, () => 0);
  for (let y = 0; y <= height; y += 1) for (let x = 0; x <= width; x += 1) {
    const nearby: number[] = [];
    for (let tileY = y - 1; tileY <= y; tileY += 1) for (let tileX = x - 1; tileX <= x; tileX += 1) {
      if (tileX < 0 || tileY < 0 || tileX >= width || tileY >= height) continue;
      const tile = tiles[tileY * width + tileX]!;
      if (isPlayable(tile)) nearby.push(tile.height);
    }
    vertices[vertexIndex(x, y)] = nearby.length ? nearby.reduce((sum, value) => sum + value, 0) / nearby.length : 0;
  }
  for (let y = 0; y < height; y += 1) for (let x = 0; x < width; x += 1) {
    const tile = tiles[y * width + x]!;
    if (!isPlayable(tile)) continue;
    const corners: [number, number, number, number] = [
      vertices[vertexIndex(x, y)]!,
      vertices[vertexIndex(x + 1, y)]!,
      vertices[vertexIndex(x + 1, y + 1)]!,
      vertices[vertexIndex(x, y + 1)]!,
    ];
    tile.corners = corners;
    tile.height = corners.reduce((sum, value) => sum + value, 0) / corners.length;
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

interface RotationPlacement {
  rotation: Rotation;
  trackOverlapCount: number;
}

const departureDirection = (route: readonly Point[]) => {
  const start = route[0];
  if (!start) return { x: 1, y: 0 };
  for (const point of route.slice(1)) {
    const direction = { x: Math.sign(point.x - start.x), y: Math.sign(point.y - start.y) };
    if (direction.x || direction.y) return direction;
  }
  return { x: 1, y: 0 };
};

/**
 * Find a fairway-only connector around the old atlas. It may share the small
 * rebuilt apron at the old cup, but otherwise cannot walk through old terrain.
 */
const bridgeFor = (previous: Course, start: Point, target: Point) => {
  const margin = CAMPAIGN_EXCAVATION_RADIUS + 2;
  const minX = Math.min(-margin, start.x - margin, target.x - margin);
  const maxX = Math.max(previous.width - 1 + margin, start.x + margin, target.x + margin);
  const minY = Math.min(-margin, start.y - margin, target.y - margin);
  const maxY = Math.max(previous.height - 1 + margin, start.y + margin, target.y + margin);
  const startKey = pointKey(start);
  const targetKey = pointKey(target);
  const previousByKey = new Map<string, string | undefined>([[startKey, undefined]]);
  const queue = [{ ...start }];
  let cursor = 0;
  while (cursor < queue.length) {
    const current = queue[cursor++]!;
    const currentKey = pointKey(current);
    if (currentKey === targetKey) {
      const path: Point[] = [];
      let key: string | undefined = currentKey;
      while (key) {
        const [x, y] = key.split(':').map(Number);
        path.push({ x, y });
        key = previousByKey.get(key);
      }
      return path.reverse();
    }
    for (const direction of cardinalDirections) {
      const point = { x: current.x + direction.x, y: current.y + direction.y };
      if (point.x < minX || point.x > maxX || point.y < minY || point.y > maxY) continue;
      const key = pointKey(point);
      if (previousByKey.has(key)) continue;
      if (isPlayable(tileAt(previous, point)) && distanceFrom(point, start) > CAMPAIGN_EXCAVATION_RADIUS) continue;
      previousByKey.set(key, currentKey);
      queue.push(point);
    }
  }
  return undefined;
};

interface DetachedPlacement {
  rotation: Rotation;
  anchor: Point;
  bridge: Point[];
}

interface DetachedCandidate extends DetachedPlacement {
  edgeDistance: number;
  preference: number;
}

/**
 * A rare, late-campaign fallback. Move the next board beyond the current atlas
 * and connect it by a narrow fairway, rather than accepting two tracks on top
 * of one another. The new board's bounding box is separated on its chosen side.
 */
const detachedPlacementFor = (previous: Course, next: Course, cup: Point): DetachedPlacement => {
  const span = Math.max(next.width, next.height) + CAMPAIGN_EXCAVATION_RADIUS + 2;
  const departure = departureDirection(next.route);
  const preferred = hashFor(`${previous.seed}:${next.seed}:detached`) % cardinalDirections.length;
  const candidates: DetachedCandidate[] = [];
  cardinalDirections.forEach((direction, index) => {
    const anchor = direction.x > 0
      ? { x: previous.width + span, y: cup.y }
      : direction.x < 0
        ? { x: -span, y: cup.y }
        : direction.y > 0
          ? { x: cup.x, y: previous.height + span }
          : { x: cup.x, y: -span };
    const rotation = ([0, 1, 2, 3] as Rotation[]).sort((left, right) => {
      const leftAlignment = rotateDirection(departure, left).x * direction.x + rotateDirection(departure, left).y * direction.y;
      const rightAlignment = rotateDirection(departure, right).x * direction.x + rotateDirection(departure, right).y * direction.y;
      return rightAlignment - leftAlignment || (left - preferred + 4) % 4 - (right - preferred + 4) % 4;
    })[0]!;
    const bridge = bridgeFor(previous, cup, anchor);
    const edgeDistance = direction.x > 0 ? previous.width - 1 - cup.x : direction.x < 0 ? cup.x : direction.y > 0 ? previous.height - 1 - cup.y : cup.y;
    if (bridge) candidates.push({ rotation, anchor, bridge, edgeDistance, preference: (index - preferred + cardinalDirections.length) % cardinalDirections.length });
  });
  const best = candidates.sort((left, right) => left.bridge.length - right.bridge.length || left.edgeDistance - right.edgeDistance || left.preference - right.preference)[0];
  if (!best) throw new Error('could not route a collision-free campaign connector');
  return best;
};

const rotationFor = (previous: Course, next: Course, anchor: Point): RotationPlacement => {
  const preferred = hashFor(`${previous.seed}:${next.seed}`) % 4;
  const approach = finalApproachDirection(previous.route);
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
    const heading = rotateDirection({ x: 1, y: 0 }, rotation);
    const sidePenalty = approach ? Math.abs(heading.x * approach.x + heading.y * approach.y) : 0;
    return [collision, sidePenalty, localPressure, (rotation - preferred + 4) % 4] as const;
  };
  const rotation = ([0, 1, 2, 3] as Rotation[]).sort((left, right) => {
    const leftScore = score(left);
    const rightScore = score(right);
    // A clear append is always more important than visual heading preference.
    return leftScore[0] - rightScore[0] || leftScore[1] - rightScore[1] || leftScore[2] - rightScore[2] || leftScore[3] - rightScore[3];
  })[0]!;
  return { rotation, trackOverlapCount: score(rotation)[0] };
};

const translateHazards = (hazards: readonly CourseHazard[], point: (point: Point) => Point, direction: (value: Point) => Point, prefix = ''): CourseHazard[] => hazards.map((hazard) => hazard.kind === 'updraft'
  ? { ...hazard, id: `${prefix}${hazard.id}`, point: point(hazard.point), direction: direction(hazard.direction) }
  : { ...hazard, id: `${prefix}${hazard.id}`, point: point(hazard.point) });
const translateFeatures = (features: readonly CourseFeature[], point: (point: Point) => Point, direction: (value: Point) => Point, prefix = ''): CourseFeature[] => features.map((feature) => feature.kind === 'sinkhole'
  ? { ...feature, id: `${prefix}${feature.id}`, entrance: point(feature.entrance), exit: point(feature.exit) }
  : feature.kind === 'pulse' || feature.kind === 'gust'
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
 * by minimising overlap outside the small shared junction around that cup.
 */
export const expandCourseAtCup = (previous: Course, next: Course): CourseExpansion => {
  const junction = { ...previous.cup };
  let rawAnchor = { ...junction };
  let placement = rotationFor(previous, next, rawAnchor);
  let bridge: Point[] = [];
  if (placement.trackOverlapCount > 0) {
    const detached = detachedPlacementFor(previous, next, junction);
    rawAnchor = detached.anchor;
    placement = { rotation: detached.rotation, trackOverlapCount: 0 };
    bridge = detached.bridge;
  }
  const { rotation } = placement;
  const bounds = transformedBounds(next, rawAnchor, rotation);
  const minX = Math.min(0, bounds.minX, ...bridge.map((point) => point.x));
  const minY = Math.min(0, bounds.minY, ...bridge.map((point) => point.y));
  const maxX = Math.max(previous.width - 1, bounds.maxX, ...bridge.map((point) => point.x));
  const maxY = Math.max(previous.height - 1, bounds.maxY, ...bridge.map((point) => point.y));
  const offset = { x: -minX, y: -minY };
  const width = maxX - minX + 1;
  const height = maxY - minY + 1;
  const previousEmbedded = embeddedPrevious(previous, width, height, offset);
  const anchor = shifted(rawAnchor, offset);
  const sharedJunction = shifted(junction, offset);
  const tiles: Tile[] = previousEmbedded.tiles.map((tile): Tile => ({ ...tile, corners: tile.corners ? [...tile.corners] as [number, number, number, number] : undefined, direction: tile.direction ? { ...tile.direction } : undefined }));
  const junctionIndex = sharedJunction.y * width + sharedJunction.x;
  const excavated: Point[] = isPlayable(tiles[junctionIndex]) ? [{ ...sharedJunction }] : [];
  for (let y = 0; y < height; y += 1) for (let x = 0; x < width; x += 1) {
    const point = { x, y };
    const index = y * width + x;
    if (distanceFrom(point, sharedJunction) > CAMPAIGN_EXCAVATION_RADIUS || tiles[index]!.surface !== 'wall') continue;
    excavated.push(point);
    tiles[index] = { surface: 'void', height: 0 };
  }

  const transform = (point: Point) => shifted(pointFor(point, next.tee, rawAnchor, rotation), offset);
  const elevationOffset = tiles[junctionIndex]!.height - next.tiles[next.tee.y * next.width + next.tee.x]!.height;
  const routeTiles = new Set(next.route.map(pointKey));
  const added: Point[] = [];
  const addedKeys = new Set<string>();
  const addTile = (point: Point) => {
    const key = pointKey(point);
    if (addedKeys.has(key)) return;
    addedKeys.add(key);
    added.push(point);
  };
  for (let y = 0; y < next.height; y += 1) for (let x = 0; x < next.width; x += 1) {
    const source = next.tiles[y * next.width + x]!;
    if (!isPlayable(source)) continue;
    const destination = transform({ x, y });
    const index = destination.y * width + destination.x;
    const existing = previousEmbedded.tiles[index]!;
    const transformed = raisedTile(source, next.theme, rotation, elevationOffset);
    if (destination.x === anchor.x && destination.y === anchor.y && rawAnchor.x === junction.x && rawAnchor.y === junction.y) {
      tiles[index] = isPlayable(existing)
        ? { ...transformed, height: existing.height, corners: existing.corners ? [...existing.corners] as [number, number, number, number] : undefined }
        : transformed;
      addTile(destination);
    } else if (!isPlayable(existing) || source.surface === 'cup' || routeTiles.has(pointKey({ x, y }))) {
      tiles[index] = transformed;
      addTile(destination);
    }
  }
  // The old retaining walls deliberately disappear before the next hole arrives.
  // Leave their cells as playable terrain in the finished course so the shared tee
  // is an expanding junction rather than a thin, jagged gap between two boards.
  // New-course tiles still take priority where they overlap this apron.
  const junctionHeight = tiles[junctionIndex]!.height;
  for (const point of excavated) {
    if (point.x === sharedJunction.x && point.y === sharedJunction.y) continue;
    const index = point.y * width + point.x;
    if (isPlayable(tiles[index])) continue;
    tiles[index] = { surface: 'fairway', height: junctionHeight, theme: next.theme };
    addTile(point);
  }
  for (const rawPoint of bridge) {
    const point = shifted(rawPoint, offset);
    if ((point.x === sharedJunction.x && point.y === sharedJunction.y) || (point.x === anchor.x && point.y === anchor.y)) continue;
    const index = point.y * width + point.x;
    if (isPlayable(tiles[index])) continue;
    tiles[index] = { surface: 'fairway', height: junctionHeight, theme: next.theme };
    addTile(point);
  }
  const wasAdded = (point: Point) => addedKeys.has(pointKey(point));
  const wasRebuilt = (point: Point) => wasAdded(point) && isPlayable(previousEmbedded.tiles[point.y * width + point.x]);
  const keepHazard = (hazard: CourseHazard) => !wasRebuilt(hazard.point);
  const keepFeature = (feature: CourseFeature) => feature.kind === 'sinkhole'
    ? !wasRebuilt(feature.entrance) && !wasRebuilt(feature.exit)
    : !wasRebuilt(feature.point);
  const keepPortal = (portal: PortalPair) => (!portal.entrance || !wasRebuilt(portal.entrance.point)) && (!portal.exit || !wasRebuilt(portal.exit.point));
  const addNewFeature = (feature: CourseFeature) => feature.kind === 'sinkhole'
    ? wasAdded(transform(feature.entrance)) && wasAdded(transform(feature.exit))
    : wasAdded(transform(feature.point));
  const addNewPortal = (portal: PortalPair) => (!portal.entrance || wasAdded(transform(portal.entrance.point))) && (!portal.exit || wasAdded(transform(portal.exit.point)));
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
      ...translateHazards(next.hazards.filter((hazard) => wasAdded(transform(hazard.point))), transform, (value) => rotateDirection(value, rotation), prefix),
    ],
    features: [
      ...translateFeatures(previousEmbedded.features.filter(keepFeature), (point) => ({ ...point }), (value) => ({ ...value })),
      ...translateFeatures((next.features ?? []).filter(addNewFeature), transform, (value) => rotateDirection(value, rotation), prefix),
    ],
    portals: [
      ...(translatePortals(previousEmbedded.portals?.filter(keepPortal), (point) => ({ ...point }), (value) => ({ ...value })) ?? []),
      ...(translatePortals(next.portals?.filter(addNewPortal), transform, (value) => rotateDirection(value, rotation), prefix) ?? []),
    ],
    itemPads: [
      ...previousEmbedded.itemPads.filter((pad) => !wasRebuilt(pad.point)),
      ...next.itemPads.filter((pad) => wasAdded(transform(pad.point))).map((pad) => ({ ...pad, id: `${prefix}${pad.id}`, point: transform(pad.point) })),
    ],
  };
  smoothTerrain(course.tiles, width, height);
  return { previous: previousEmbedded, course, added, excavated, anchor, offset, rotation, trackOverlapCount: placement.trackOverlapCount };
};
