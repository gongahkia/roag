import { isGateOpen, motionColorFor, sweeperDirection } from '../core/hazards';
import { CHIP_GRAVITY, chipFlightSeconds, floorHeightAt, shotVelocityFor, tileCornerHeights } from '../core/physics';
import { EMOTES, type Ball, type Course, type EmoteEvent, type Gadget, type GadgetKind, type Player, type Point as WorldPoint, type PortalEndpoint, type ShotCommand, type Surface, type Tile } from '../core/types';

interface Point { x: number; y: number; }
export interface ProjectionMetrics { tileWidth: number; tileHeight: number; elevation: number; }
interface VisibleTile { x: number; y: number; tile: Tile; heights: [number, number, number, number]; corners: Point[]; center: Point; }

const topColors: Record<Surface, string> = {
  void: '#f8fcf4',
  fairway: '#78b96b',
  rough: '#4e914b',
  sand: '#ecd69c',
  ice: '#b8e4ee',
  wall: '#8c998a',
  tee: '#9dce7e',
  cup: '#d2edbd',
  booster: '#e49b36',
  conveyor: '#75a8d0',
};

const faceColors = {
  light: '#78a95e',
  dark: '#4d813f',
  wallLight: '#aab4a5',
  wallDark: '#6e786a',
};

export interface Renderer {
  draw(course: Course, players: Player[], hazardElapsedMs: number, aim?: ShotCommand, emotes?: readonly EmoteEvent[], showItems?: boolean, phaseCount?: number, buildProgress?: number, gadgets?: readonly Gadget[], placement?: { kind: GadgetKind; point?: WorldPoint; valid: boolean }, focus?: Ball): void;
  drawCampaign(frame: CampaignFrame): void;
  aimFromPointer(event: PointerEvent, course: Course, ball: Ball): { angle: number; power: number };
  tileFromPointer(event: PointerEvent, course: Course): WorldPoint | undefined;
  dispose(): void;
}

/** One deterministic course displayed on the persistent campaign atlas. */
export interface CampaignIsland {
  course: Course;
  hole: number;
  label: string;
  /** Lets an incoming course use the same tile-by-tile arrival language as a normal build. */
  buildProgress?: number;
}

export interface CampaignFrame {
  islands: readonly CampaignIsland[];
  focusIndex: number;
  /** A camera multiplier: 1 reveals the atlas, larger values focus an individual island. */
  zoom: number;
  /** Controls how much of the route joining the incoming island has been established. */
  bridgeProgress?: number;
  title?: string;
}

export interface CampaignSlot {
  index: number;
  x: number;
  y: number;
  width: number;
  height: number;
}

/**
 * Stable screen-space districts for the campaign. Keeping this pure makes the overview
 * independent of gameplay state and gives all clients the same visual ordering.
 */
export const campaignSlotsFor = (count: number, width: number, height: number): CampaignSlot[] => {
  const islands = Math.max(1, Math.floor(count));
  const columns = islands === 1 ? 1 : islands <= 4 ? 2 : 3;
  const rows = Math.ceil(islands / columns);
  const gutter = Math.max(16, Math.min(width, height) * .045);
  const cellWidth = (width - gutter * 2) / columns;
  const cellHeight = (height - gutter * 2) / rows;
  return Array.from({ length: islands }, (_, index) => {
    const column = index % columns;
    const row = Math.floor(index / columns);
    return {
      index,
      x: gutter + cellWidth * (column + .5),
      y: gutter + cellHeight * (row + .5),
      width: cellWidth,
      height: cellHeight,
    };
  });
};

const themeTopColors: Record<Course['theme'], Partial<Record<Surface, string>>> = {
  balanced: {},
  speedway: { fairway: '#6077b7', rough: '#465a91', booster: '#f0a63d', conveyor: '#83c5e8' },
  'hazard-run': { fairway: '#a76c58', rough: '#79483f', sand: '#e4b174', booster: '#e3753c' },
  'ice-rink': { fairway: '#8dcee0', rough: '#67a9c7', ice: '#d6f5fb', sand: '#c9d6db' },
  quarry: { fairway: '#9d9b6c', rough: '#72734f', sand: '#d6b57c', wall: '#79756d' },
  drift: { fairway: '#c5976d', rough: '#996b55', sand: '#e3c383', ice: '#b6d8d8' },
  bloom: { fairway: '#b482aa', rough: '#774a7a', sand: '#e4b4c8', booster: '#e498ba' },
  pulse: { fairway: '#536c9b', rough: '#334873', sand: '#aeb5d1', booster: '#ef6a8d', conveyor: '#b58aea' },
};

const topColorFor = (course: Course, surface: Surface, theme = course.theme) => themeTopColors[theme]?.[surface] ?? topColors[surface];

const project = (x: number, y: number, z: number, metrics: ProjectionMetrics): Point => ({
  x: (x - y) * metrics.tileWidth / 2,
  y: (x + y) * metrics.tileHeight / 2 - z * metrics.elevation,
});

export const projectWorldDirection = (direction: Point, metrics: Pick<ProjectionMetrics, 'tileWidth' | 'tileHeight'>): Point => ({
  x: (direction.x - direction.y) * metrics.tileWidth / 2,
  y: (direction.x + direction.y) * metrics.tileHeight / 2,
});

const polygon = (context: CanvasRenderingContext2D, points: readonly Point[]) => {
  context.beginPath();
  context.moveTo(points[0]!.x, points[0]!.y);
  points.slice(1).forEach((point) => context.lineTo(point.x, point.y));
  context.closePath();
};

const isVisible = (tile: Tile | undefined, includeVoid = false) => Boolean(tile && (includeVoid || tile.surface !== 'void'));

const visibleTilesFor = (course: Course, metrics: ProjectionMetrics, includeVoid = false): VisibleTile[] => {
  const visible: VisibleTile[] = [];
  for (let y = 0; y < course.height; y += 1) {
    for (let x = 0; x < course.width; x += 1) {
      const tile = course.tiles[y * course.width + x]!;
      if (!isVisible(tile, includeVoid)) continue;
      const heights = tileCornerHeights(tile);
      const corners = [project(x, y, heights[0], metrics), project(x + 1, y, heights[1], metrics), project(x + 1, y + 1, heights[2], metrics), project(x, y + 1, heights[3], metrics)];
      const center = project(x + .5, y + .5, heights.reduce((sum, height) => sum + height, 0) / 4, metrics);
      visible.push({ x, y, tile, heights, corners, center });
    }
  }
  return visible.sort((left, right) => {
    const leftDepth = left.x + left.y;
    const rightDepth = right.x + right.y;
    return leftDepth - rightDepth || left.x - right.x;
  });
};

const boundsFor = (tiles: readonly VisibleTile[], metrics: ProjectionMetrics) => {
  const points = tiles.flatMap(({ corners, heights }) => [...corners, ...corners.map((point, index) => ({ x: point.x, y: point.y + heights[index]! * metrics.elevation }))]);
  const minX = Math.min(...points.map((point) => point.x));
  const maxX = Math.max(...points.map((point) => point.x));
  const minY = Math.min(...points.map((point) => point.y));
  const maxY = Math.max(...points.map((point) => point.y));
  return { minX, maxX, minY, maxY, width: maxX - minX, height: maxY - minY };
};

const metricsFor = (course: Course, width: number, height: number, includeVoid = false): ProjectionMetrics => {
  const unitMetrics = { tileWidth: 1, tileHeight: .5, elevation: .34 };
  const bounds = boundsFor(visibleTilesFor(course, unitMetrics, includeVoid), unitMetrics);
  const fittedTileWidth = Math.min(64, (width - 38) / bounds.width, (height - 54) / bounds.height);
  const readableTileWidth = course.width * course.height > 360 ? 16 : 12;
  const tileWidth = Math.max(readableTileWidth, fittedTileWidth);
  return { tileWidth, tileHeight: tileWidth / 2, elevation: tileWidth * .34 };
};

const centeredOffsetFor = (tiles: readonly VisibleTile[], metrics: ProjectionMetrics, width: number, height: number): Point => {
  const { minX, maxX, minY, maxY } = boundsFor(tiles, metrics);
  return {
    x: width / 2 - (minX + maxX) / 2,
    y: height / 2 - (minY + maxY) / 2 + 6,
  };
};

export interface CameraLayout {
  metrics: ProjectionMetrics;
  offset: Point;
  followsFocus: boolean;
}

export const cameraFor = (course: Course, width: number, height: number, focus?: Pick<Ball, 'x' | 'y' | 'z'>): CameraLayout => {
  const metrics = metricsFor(course, width, height);
  const tiles = visibleTilesFor(course, metrics);
  const centered = centeredOffsetFor(tiles, metrics, width, height);
  const bounds = boundsFor(tiles, metrics);
  const followsFocus = Boolean(focus && (bounds.width > width - 38 || bounds.height > height - 54));
  if (!focus || !followsFocus) return { metrics, offset: centered, followsFocus: false };
  const point = project(focus.x, focus.y, focus.z, metrics);
  const paddingX = 19;
  const paddingY = 27;
  const clampOffset = (value: number, minimum: number, maximum: number, fallback: number) => minimum <= maximum ? Math.max(minimum, Math.min(maximum, value)) : fallback;
  return {
    metrics,
    followsFocus,
    offset: {
      x: clampOffset(width * .5 - point.x, width - paddingX - bounds.maxX, paddingX - bounds.minX, centered.x),
      y: clampOffset(height * .52 - point.y, height - paddingY - bounds.maxY, paddingY - bounds.minY, centered.y),
    },
  };
};

const withOffset = (point: Point, offset: Point): Point => ({ x: point.x + offset.x, y: point.y + offset.y });

const visibleInViewport = (tile: VisibleTile, offset: Point, width: number, height: number, metrics: ProjectionMetrics) => {
  const points = tile.corners.map((point, index) => ({ x: point.x + offset.x, y: point.y + offset.y + tile.heights[index]! * metrics.elevation }));
  const minX = Math.min(...points.map((point) => point.x));
  const maxX = Math.max(...points.map((point) => point.x));
  const minY = Math.min(...points.map((point) => point.y));
  const maxY = Math.max(...points.map((point) => point.y));
  const bleed = metrics.tileWidth * 1.5;
  return maxX >= -bleed && minX <= width + bleed && maxY >= -bleed && minY <= height + bleed;
};

const neighborFor = (course: Course, x: number, y: number, direction: Point) => course.tiles[(y + direction.y) * course.width + x + direction.x];

const neighborEdgeCorners = [[3, 2], [0, 3], [1, 0], [2, 1]] as const;

const drawSide = (context: CanvasRenderingContext2D, tile: VisibleTile, edge: number, neighbor: Tile | undefined, offset: Point, metrics: ProjectionMetrics) => {
  if (tile.tile.surface === 'void') return;
  const edgePoints = [tile.corners[edge]!, tile.corners[(edge + 1) % 4]!];
  const edgeHeights = [tile.heights[edge]!, tile.heights[(edge + 1) % 4]!];
  const indexes = neighborEdgeCorners[edge]!;
  const neighborHeights = isVisible(neighbor) ? tileCornerHeights(neighbor!) : [0, 0, 0, 0];
  const depths = edgeHeights.map((height, index) => Math.max(0, height - neighborHeights[indexes[index]!]!) * metrics.elevation);
  if (tile.tile.surface === 'wall' && !isVisible(neighbor)) depths.forEach((depth, index) => { depths[index] = Math.max(depth, metrics.elevation * .72); });
  if (Math.max(...depths) < .2) return;
  const face = [
    withOffset(edgePoints[0], offset),
    withOffset(edgePoints[1], offset),
    withOffset({ x: edgePoints[1].x, y: edgePoints[1].y + depths[1]! }, offset),
    withOffset({ x: edgePoints[0].x, y: edgePoints[0].y + depths[0]! }, offset),
  ];
  polygon(context, face);
  const brighter = edgePoints[0].x < edgePoints[1].x;
  const wall = tile.tile.surface === 'wall';
  context.fillStyle = wall ? (brighter ? faceColors.wallLight : faceColors.wallDark) : (brighter ? faceColors.light : faceColors.dark);
  context.fill();
  context.fillStyle = brighter ? '#ffffff4d' : '#24512228';
  for (let stripe = 6; stripe < Math.max(...depths); stripe += 9) {
    context.fillRect(Math.min(face[0].x, face[1].x), Math.max(face[0].y, face[1].y) + stripe, Math.abs(face[1].x - face[0].x), 1);
  }
};

const drawPattern = (context: CanvasRenderingContext2D, tile: VisibleTile, offset: Point, metrics: ProjectionMetrics) => {
  const center = withOffset(tile.center, offset);
  context.save();
  polygon(context, tile.corners.map((point) => withOffset(point, offset)));
  context.clip();
  const inset = metrics.tileWidth * .19;
  if (tile.tile.surface === 'fairway') {
    if ((tile.x * 5 + tile.y * 3) % 5 === 0) {
      context.fillStyle = '#e9ffd83b';
      context.fillRect(center.x - inset, center.y - 1, inset * 2, 2);
    }
  } else if (tile.tile.surface === 'rough') {
    context.fillStyle = '#1d5c2542';
    for (let dot = -1; dot <= 1; dot += 1) context.fillRect(center.x + dot * 5, center.y + (dot % 2) * 3, 2, 2);
  } else if (tile.tile.surface === 'sand') {
    context.strokeStyle = '#fff9d58a';
    context.lineWidth = 1;
    for (let line = -2; line <= 2; line += 1) {
      context.beginPath();
      context.moveTo(center.x - inset, center.y + line * 3);
      context.lineTo(center.x + inset, center.y + line * 3 + 4);
      context.stroke();
    }
  } else if (tile.tile.surface === 'ice') {
    context.strokeStyle = '#ffffffb8';
    context.lineWidth = 1.5;
    context.beginPath();
    context.moveTo(center.x - inset, center.y + 3);
    context.lineTo(center.x + inset, center.y - 3);
    context.stroke();
  } else if (tile.tile.surface === 'tee') {
    context.fillStyle = '#f9fff4';
    context.fillRect(center.x - inset, center.y - 2, inset * 2, 4);
  } else if (tile.tile.surface === 'cup') {
    const size = metrics.tileWidth * .16;
    context.fillStyle = '#204e2a';
    context.fillRect(center.x - size, center.y - size / 2, size * 2, size);
    context.fillStyle = '#ffffff';
    context.fillRect(center.x - size, center.y - size / 2, size, size / 2);
    context.fillRect(center.x, center.y, size, size / 2);
  }
  context.restore();
};

const screenDirection = (_x: number, _y: number, direction: Point, metrics: ProjectionMetrics): Point => {
  const projected = projectWorldDirection(direction, metrics);
  const length = Math.hypot(projected.x, projected.y) || 1;
  return { x: projected.x / length, y: projected.y / length };
};

const drawChevron = (context: CanvasRenderingContext2D, center: Point, direction: Point, size: number, color: string) => {
  const perpendicular = { x: -direction.y, y: direction.x };
  const point = (forward: number, sideways: number): Point => ({ x: center.x + direction.x * forward + perpendicular.x * sideways, y: center.y + direction.y * forward + perpendicular.y * sideways });
  polygon(context, [point(size, 0), point(-size * .48, size * .62), point(-size * .12, 0), point(-size * .48, -size * .62)]);
  context.fillStyle = color;
  context.fill();
};

const portalColors = ['#4589e8', '#c66af0', '#32a892', '#e08a3e', '#e65b81', '#6b8fd9'];

const drawPortalEndpoint = (context: CanvasRenderingContext2D, endpoint: PortalEndpoint, label: string, color: string, course: Course, offset: Point, metrics: ProjectionMetrics) => {
  const center = withOffset(project(endpoint.point.x + .5, endpoint.point.y + .5, heightAt(course, endpoint.point) + .08, metrics), offset);
  const radius = Math.max(5, metrics.tileWidth * .2);
  context.beginPath();
  context.ellipse(center.x, center.y, radius, radius * .52, 0, 0, Math.PI * 2);
  context.fillStyle = '#18212ce8';
  context.fill();
  context.strokeStyle = color;
  context.lineWidth = Math.max(1.5, metrics.tileWidth * .045);
  context.stroke();
  const direction = screenDirection(endpoint.point.x, endpoint.point.y, endpoint.direction, metrics);
  drawChevron(context, center, direction, radius * .52, color);
  context.fillStyle = '#ffffff';
  context.font = `${Math.max(7, metrics.tileWidth * .13)}px BigBlueTerm, ui-monospace, monospace`;
  context.textAlign = 'center';
  context.textBaseline = 'middle';
  context.fillText(label, center.x, center.y - radius * .9);
};

const drawPortals = (context: CanvasRenderingContext2D, course: Course, offset: Point, metrics: ProjectionMetrics) => {
  course.portals?.forEach((pair, index) => {
    const color = portalColors[index % portalColors.length]!;
    if (pair.entrance) drawPortalEndpoint(context, pair.entrance, `${index + 1}A`, color, course, offset, metrics);
    if (pair.exit) drawPortalEndpoint(context, pair.exit, `${index + 1}B`, color, course, offset, metrics);
  });
};

const drawSurfaceMarker = (context: CanvasRenderingContext2D, tile: VisibleTile, offset: Point, metrics: ProjectionMetrics) => {
  if (tile.tile.surface !== 'booster' && tile.tile.surface !== 'conveyor') return;
  const direction = tile.tile.direction ?? { x: 1, y: 0 };
  const vector = screenDirection(tile.x, tile.y, direction, metrics);
  drawChevron(context, withOffset(tile.center, offset), vector, metrics.tileWidth * .18, tile.tile.surface === 'booster' ? '#fff9d8' : '#ffffff');
};

const drawRouteMarkers = (context: CanvasRenderingContext2D, course: Course, offset: Point, metrics: ProjectionMetrics) => {
  for (let index = 2; index < course.route.length - 1; index += 4) {
    const point = course.route[index]!;
    const next = course.route[index + 1]!;
    const tile = course.tiles[point.y * course.width + point.x]!;
    if (!isVisible(tile) || tile.surface !== 'fairway') continue;
    const center = withOffset(project(point.x + .5, point.y + .5, floorHeightAt(course, point.x + .5, point.y + .5) + .025, metrics), offset);
    const direction = screenDirection(point.x, point.y, { x: next.x - point.x, y: next.y - point.y }, metrics);
    drawChevron(context, center, direction, metrics.tileWidth * .13, '#e55d35');
  }
};

const heightAt = (course: Course, point: Point) => floorHeightAt(course, point.x + .5, point.y + .5);

export interface AimPathPoint { x: number; y: number; z: number; }

/** A grounded line for putts and an airborne carry arc for chips. */
export const aimPathFor = (course: Course, ball: Ball, shot: ShotCommand): AimPathPoint[] => {
  const startFloor = floorHeightAt(course, ball.x, ball.y);
  const start = { x: ball.x, y: ball.y, z: ball.z + .15 };
  if (shot.kind !== 'chip') {
    const endpoint = { x: ball.x + Math.cos(shot.angle) * shot.power * .4, y: ball.y + Math.sin(shot.angle) * shot.power * .4 };
    const segments = Math.max(2, Math.ceil(Math.hypot(endpoint.x - ball.x, endpoint.y - ball.y) * 4));
    const clearance = start.z - startFloor;
    return Array.from({ length: segments + 1 }, (_, index) => {
      const progress = index / segments;
      const x = ball.x + (endpoint.x - ball.x) * progress;
      const y = ball.y + (endpoint.y - ball.y) * progress;
      return { x, y, z: floorHeightAt(course, x, y) + clearance };
    });
  }
  const velocity = shotVelocityFor(shot);
  const duration = chipFlightSeconds(shot);
  return Array.from({ length: 17 }, (_, index) => {
    const progress = index / 16;
    const time = duration * progress;
    const x = ball.x + velocity.vx * time;
    const y = ball.y + velocity.vy * time;
    return {
      x,
      y,
      z: start.z + velocity.vz * time - .5 * CHIP_GRAVITY * time ** 2 + (floorHeightAt(course, x, y) - startFloor) * progress,
    };
  });
};

const drawHazards = (context: CanvasRenderingContext2D, course: Course, hazardElapsedMs: number, offset: Point, metrics: ProjectionMetrics, phaseCount = 8) => {
  for (const hazard of course.hazards) {
    const center = withOffset(project(hazard.point.x + .5, hazard.point.y + .5, heightAt(course, hazard.point) + .1, metrics), offset);
    if (hazard.kind === 'sweeper') {
      const color = motionColorFor(hazard);
      const direction = sweeperDirection(hazard, hazardElapsedMs, phaseCount);
      const end = withOffset(project(hazard.point.x + .5 + direction.x * hazard.radius, hazard.point.y + .5 + direction.y * hazard.radius, heightAt(course, hazard.point) + .13, metrics), offset);
      context.strokeStyle = color;
      context.lineWidth = Math.max(4, metrics.tileWidth * .11);
      context.beginPath();
      context.moveTo(center.x, center.y);
      context.lineTo(end.x, end.y);
      context.stroke();
      context.strokeStyle = '#623d26';
      context.lineWidth = Math.max(1, metrics.tileWidth * .025);
      context.stroke();
      context.beginPath();
      context.arc(center.x, center.y, Math.max(4, metrics.tileWidth * .11), 0, Math.PI * 2);
      context.fillStyle = color;
      context.fill();
      context.strokeStyle = '#ffffff';
      context.lineWidth = 1;
      context.stroke();
      continue;
    }
    if (hazard.kind === 'updraft') {
      const radius = metrics.tileWidth * hazard.radius * .45;
      context.beginPath();
      context.arc(center.x, center.y, radius, 0, Math.PI * 2);
      context.strokeStyle = '#8bdcf2';
      context.lineWidth = 1.6;
      context.setLineDash([3, 2]);
      context.stroke();
      context.setLineDash([]);
      drawChevron(context, center, screenDirection(hazard.point.x, hazard.point.y, hazard.direction, metrics), radius * .58, '#e9fbff');
      continue;
    }
    if (hazard.kind === 'low-bar') {
      const height = metrics.tileHeight * 1.5;
      const width = metrics.tileWidth * .34;
      context.strokeStyle = '#8e4c35';
      context.lineWidth = Math.max(3, metrics.tileWidth * .09);
      context.beginPath();
      context.moveTo(center.x - width, center.y - height);
      context.lineTo(center.x + width, center.y - height);
      context.stroke();
      context.strokeStyle = '#ffe3a5';
      context.lineWidth = 1;
      context.beginPath();
      context.moveTo(center.x - width, center.y - height);
      context.lineTo(center.x - width, center.y + height * .18);
      context.moveTo(center.x + width, center.y - height);
      context.lineTo(center.x + width, center.y + height * .18);
      context.stroke();
      continue;
    }
    const color = motionColorFor(hazard);
    const open = isGateOpen(hazard, hazardElapsedMs, phaseCount);
    const width = metrics.tileWidth * .23;
    const height = metrics.tileHeight * .48;
    context.fillStyle = open ? `${color}66` : `${color}d9`;
    context.fillRect(center.x - width, center.y - height, width * 2, height * 2);
    context.strokeStyle = open ? color : '#ffffff';
    context.lineWidth = open ? 1.5 : 2;
    context.strokeRect(center.x - width, center.y - height, width * 2, height * 2);
    if (!open) {
      context.strokeStyle = '#623d26';
      context.beginPath();
      context.moveTo(center.x - width, center.y - height);
      context.lineTo(center.x + width, center.y + height);
      context.moveTo(center.x + width, center.y - height);
      context.lineTo(center.x - width, center.y + height);
      context.stroke();
    }
  }
};

const drawItemPads = (context: CanvasRenderingContext2D, course: Course, offset: Point, metrics: ProjectionMetrics) => {
  course.itemPads.filter((pad) => !pad.collected).forEach((pad) => {
    const center = withOffset(project(pad.point.x + .5, pad.point.y + .5, heightAt(course, pad.point) + .06, metrics), offset);
    const size = metrics.tileWidth * .14;
    polygon(context, [{ x: center.x, y: center.y - size }, { x: center.x + size, y: center.y }, { x: center.x, y: center.y + size }, { x: center.x - size, y: center.y }]);
    context.fillStyle = pad.kind === 'recovery' ? '#73d9d5' : pad.kind === 'cash' ? '#e1ba57' : '#c88cf5';
    context.fill();
    context.strokeStyle = '#ffffff';
    context.lineWidth = 1.2;
    context.stroke();
    context.fillStyle = '#1c4321';
    context.font = `${Math.max(8, metrics.tileWidth * .16)}px BigBlueTerm, ui-monospace, monospace`;
    context.textAlign = 'center';
    context.textBaseline = 'middle';
    context.fillText(pad.kind === 'recovery' ? '+' : pad.kind === 'cash' ? '$' : '!', center.x, center.y + 1);
  });
};

const drawCourseFeatures = (context: CanvasRenderingContext2D, course: Course, offset: Point, metrics: ProjectionMetrics) => {
  (course.features ?? []).forEach((feature) => {
    if (feature.kind === 'sinkhole') {
      const entrance = withOffset(project(feature.entrance.x + .5, feature.entrance.y + .5, heightAt(course, feature.entrance) + .07, metrics), offset);
      const exit = withOffset(project(feature.exit.x + .5, feature.exit.y + .5, heightAt(course, feature.exit) + .07, metrics), offset);
      const radius = metrics.tileWidth * .19;
      [entrance, exit].forEach((center, index) => {
        context.beginPath();
        context.ellipse(center.x, center.y, radius, radius * .5, 0, 0, Math.PI * 2);
        context.fillStyle = index ? '#61d9dcbb' : '#26314dd9';
        context.fill();
        context.strokeStyle = index ? '#d8ffff' : '#c49bf7';
        context.lineWidth = 1.4;
        context.stroke();
      });
      context.fillStyle = '#ffffff';
      context.font = `${Math.max(8, metrics.tileWidth * .18)}px BigBlueTerm, ui-monospace, monospace`;
      context.textAlign = 'center';
      context.textBaseline = 'middle';
      context.fillText('↻', entrance.x, entrance.y);
      context.fillText('⇥', exit.x, exit.y);
      return;
    }
    const center = withOffset(project(feature.point.x + .5, feature.point.y + .5, heightAt(course, feature.point) + .07, metrics), offset);
    const size = metrics.tileWidth * .18;
    if (feature.kind === 'air-ring') {
      const elevated = withOffset(project(feature.point.x + .5, feature.point.y + .5, heightAt(course, feature.point) + .74, metrics), offset);
      context.beginPath();
      context.ellipse(elevated.x, elevated.y, size * 1.08, size * .56, 0, 0, Math.PI * 2);
      context.fillStyle = '#fff3a755';
      context.fill();
      context.strokeStyle = '#f0a232';
      context.lineWidth = 2;
      context.stroke();
      context.fillStyle = '#fff7cf';
      context.font = `${Math.max(8, metrics.tileWidth * .16)}px BigBlueTerm, ui-monospace, monospace`;
      context.textAlign = 'center';
      context.textBaseline = 'middle';
      context.fillText('↯', elevated.x, elevated.y);
      return;
    }
    if (feature.kind === 'thorn') {
      context.fillStyle = '#6a225e';
      context.strokeStyle = '#ffd7eb';
      context.lineWidth = 1.3;
      context.beginPath();
      for (let index = 0; index < 8; index += 1) {
        const angle = -Math.PI / 2 + index * Math.PI / 4;
        const radius = index % 2 ? size * .48 : size;
        const point = { x: center.x + Math.cos(angle) * radius, y: center.y + Math.sin(angle) * radius };
        if (index) context.lineTo(point.x, point.y); else context.moveTo(point.x, point.y);
      }
      context.closePath();
      context.fill();
      context.stroke();
      return;
    }
    const direction = screenDirection(feature.point.x, feature.point.y, feature.direction, metrics);
    context.beginPath();
    context.arc(center.x, center.y, size, 0, Math.PI * 2);
    context.fillStyle = '#f26c9fb8';
    context.fill();
    drawChevron(context, center, direction, size * .72, '#fff6ff');
  });
};

const gadgetGlyph: Record<GadgetKind, string> = { 'popper pad': '↑', 'snare patch': '⌁', 'blast mine': '✹', 'slick patch': '≋', 'sky spring': '⌃', 'gravity well': '◉', 'mirror plate': '◇', 'toll booth': '$', 'control inverter': '↻', 'portal gun': '◉' };
const gadgetColor: Record<GadgetKind, string> = { 'popper pad': '#e7a64f', 'snare patch': '#6b75c9', 'blast mine': '#d8615d', 'slick patch': '#74cdd5', 'sky spring': '#a987ec', 'gravity well': '#745ac4', 'mirror plate': '#9ab5c9', 'toll booth': '#d1a447', 'control inverter': '#ca526f', 'portal gun': '#5bb1be' };

const drawGadgets = (context: CanvasRenderingContext2D, course: Course, gadgets: readonly Gadget[], offset: Point, metrics: ProjectionMetrics) => {
  gadgets.forEach((gadget) => {
    const center = withOffset(project(gadget.point.x + .5, gadget.point.y + .5, heightAt(course, gadget.point) + .08, metrics), offset);
    const radius = metrics.tileWidth * .15;
    context.beginPath();
    context.arc(center.x, center.y, radius, 0, Math.PI * 2);
    context.fillStyle = gadgetColor[gadget.kind];
    context.fill();
    context.strokeStyle = '#ffffff';
    context.lineWidth = 1.3;
    context.stroke();
    context.font = `${Math.max(8, metrics.tileWidth * .17)}px BigBlueTerm, ui-monospace, monospace`;
    context.fillStyle = '#18221b';
    context.textAlign = 'center';
    context.textBaseline = 'middle';
    context.fillText(gadgetGlyph[gadget.kind], center.x, center.y + 1);
  });
};

const drawPlacement = (context: CanvasRenderingContext2D, course: Course, placement: { kind: GadgetKind; point?: WorldPoint; valid: boolean }, offset: Point, metrics: ProjectionMetrics) => {
  if (!placement.point) return;
  const center = withOffset(project(placement.point.x + .5, placement.point.y + .5, heightAt(course, placement.point) + .1, metrics), offset);
  const radius = metrics.tileWidth * .22;
  context.beginPath();
  context.arc(center.x, center.y, radius, 0, Math.PI * 2);
  context.strokeStyle = placement.valid ? '#f8fff0' : '#ed5e55';
  context.lineWidth = 2;
  context.setLineDash([4, 3]);
  context.stroke();
  context.setLineDash([]);
  context.font = `${Math.max(10, metrics.tileWidth * .22)}px BigBlueTerm, ui-monospace, monospace`;
  context.fillStyle = placement.valid ? gadgetColor[placement.kind] : '#ed5e55';
  context.textAlign = 'center';
  context.textBaseline = 'middle';
  context.fillText(gadgetGlyph[placement.kind], center.x, center.y);
};

const statusLabels = (player: Player) => {
  const upgrades: Record<string, string> = {
    'heavy ball': 'HB', 'ice skates': 'IS', 'extra charge': 'EC', 'bank shot': 'BK', 'hazard shield': 'HS', 'chaos magnet': 'CM', 'portal savvy': 'PS', 'second wind': 'SW', scavenger: 'SC', 'aerial ace': 'AA', 'cup reader': 'CR', gadgeteer: 'GT',
  };
  return [
    player.inventory ? `I:${player.inventory}` : undefined,
    player.spareInventory ? `I:${player.spareInventory}` : undefined,
    player.ballForm ? `BALL:${player.ballForm}` : undefined,
    player.twoPuttsArmed ? '2P!' : undefined,
    player.secondWindAvailable ? 'SW!' : undefined,
    player.cupMagnetArmed ? 'CUP!' : undefined,
    player.slipstreamArmed ? 'SLIP!' : undefined,
    player.reboundRigArmed ? 'RIG!' : undefined,
    player.sandbagged ? 'BAG!' : undefined,
    player.forcedChip ? 'CHIP!' : undefined,
    ...player.upgrades.map((upgrade) => upgrades[upgrade]),
  ].filter(Boolean) as string[];
};

const drawBall = (context: CanvasRenderingContext2D, player: Player, offset: Point, metrics: ProjectionMetrics) => {
  const { ball, color } = player;
  const point = withOffset(project(ball.x, ball.y, ball.z + .08, metrics), offset);
  const shadowPoint = withOffset(project(ball.x, ball.y, ball.falling ? .02 : ball.z + .08, metrics), offset);
  const radius = Math.max(4, metrics.tileWidth * .13);
  context.beginPath();
  context.ellipse(shadowPoint.x, shadowPoint.y + radius * .38, radius * .82, radius * .32, 0, 0, Math.PI * 2);
  context.fillStyle = '#315e3360';
  context.fill();
  if (ball.falling) {
    context.beginPath();
    context.moveTo(shadowPoint.x, shadowPoint.y);
    context.lineTo(point.x, point.y);
    context.lineWidth = Math.max(1, radius * .28);
    context.strokeStyle = `${color}99`;
    context.stroke();
  }
  context.beginPath();
  context.arc(point.x, point.y, radius, 0, Math.PI * 2);
  context.fillStyle = color;
  context.fill();
  context.lineWidth = 1.4;
  context.strokeStyle = '#ffffff';
  context.stroke();
  context.beginPath();
  context.arc(point.x - radius * .3, point.y - radius * .34, Math.max(1.2, radius * .22), 0, Math.PI * 2);
  context.fillStyle = '#ffffffb8';
  context.fill();
  const labels = statusLabels(player);
  if (!labels.length) return;
  context.font = `${Math.max(7, metrics.tileWidth * .12)}px BigBlueTerm, ui-monospace, monospace`;
  const rows = [labels.slice(0, 3).join(' '), labels.slice(3).join(' ')].filter(Boolean);
  rows.forEach((row, index) => {
    const width = context.measureText(row).width + 6;
    const y = point.y - radius - 9 - (rows.length - index - 1) * 11;
    context.fillStyle = '#132015de';
    context.fillRect(point.x - width / 2, y - 5, width, 10);
    context.strokeStyle = color;
    context.lineWidth = 1;
    context.strokeRect(point.x - width / 2, y - 5, width, 10);
    context.fillStyle = '#ffffff';
    context.textAlign = 'center';
    context.textBaseline = 'middle';
    context.fillText(row, point.x, y);
  });
};

const drawEmotes = (context: CanvasRenderingContext2D, players: Player[], emotes: readonly EmoteEvent[], offset: Point, metrics: ProjectionMetrics) => {
  emotes.forEach((event, index) => {
    const player = players.find((candidate) => candidate.id === event.playerId);
    const definition = EMOTES.find((candidate) => candidate.id === event.emote);
    if (!player || !definition) return;
    const point = withOffset(project(player.ball.x, player.ball.y, player.ball.z + .62, metrics), offset);
    context.font = '12px BigBlueTerm, ui-monospace, monospace';
    const width = Math.max(23, context.measureText(definition.glyph).width + 10);
    const x = point.x + (index % 3 - 1) * 8 - width / 2;
    const y = point.y - 18 - Math.floor(index / 3) * 11;
    context.fillStyle = '#ffffffeb';
    context.strokeStyle = player.color;
    context.lineWidth = 1;
    context.fillRect(x, y, width, 15);
    context.strokeRect(x + .5, y + .5, width - 1, 14);
    context.fillStyle = '#17311b';
    context.textAlign = 'center';
    context.textBaseline = 'middle';
    context.fillText(definition.glyph, x + width / 2, y + 8);
  });
};

const clamped = (value: number) => Math.max(0, Math.min(1, value));

const drawCampaignIsland = (context: CanvasRenderingContext2D, island: CampaignIsland, slot: CampaignSlot, zoom: number, center: Point) => {
  const metrics: ProjectionMetrics = { tileWidth: 20, tileHeight: 10, elevation: 6.8 };
  const tiles = visibleTilesFor(island.course, metrics);
  const bounds = boundsFor(tiles, metrics);
  const fit = Math.max(.035, Math.min(slot.width * .76 / bounds.width, slot.height * .66 / bounds.height, 1.2));
  const scale = fit * zoom;
  const offset = { x: -(bounds.minX + bounds.maxX) / 2, y: -(bounds.minY + bounds.maxY) / 2 };
  const directions = [{ x: 0, y: -1 }, { x: 1, y: 0 }, { x: 0, y: 1 }, { x: -1, y: 0 }];
  const buildProgress = island.buildProgress === undefined ? undefined : clamped(island.buildProgress);

  context.save();
  context.translate(center.x, center.y);
  context.scale(scale, scale);
  const drawTile = (tile: VisibleTile) => {
    const stagger = ((tile.x * 13 + tile.y * 7) % 23) / 23 * .58;
    const arrived = buildProgress === undefined ? 1 : clamped((buildProgress - stagger) / .42);
    if (arrived === 0) return;
    const eased = 1 - (1 - arrived) ** 3;
    const angle = ((tile.x * 19 + tile.y * 11) % 8) * Math.PI / 4;
    const distance = metrics.tileWidth * (2.5 + ((tile.x + tile.y) % 3) * .35) * (1 - eased);
    context.save();
    context.translate(Math.cos(angle) * distance, Math.sin(angle) * distance - distance * .38);
    context.globalAlpha = .22 + eased * .78;
    for (let edge = 0; edge < 4; edge += 1) {
      const first = tile.corners[edge]!;
      const second = tile.corners[(edge + 1) % 4]!;
      if ((first.y + second.y) / 2 <= tile.center.y) continue;
      drawSide(context, tile, edge, neighborFor(island.course, tile.x, tile.y, directions[edge]!), offset, metrics);
    }
    polygon(context, tile.corners.map((point) => withOffset(point, offset)));
    context.fillStyle = topColorFor(island.course, tile.tile.surface, tile.tile.theme);
    context.fill();
    drawPattern(context, tile, offset, metrics);
    context.restore();
  };
  tiles.forEach(drawTile);

  const worldOpacity = buildProgress === undefined ? 1 : clamped((buildProgress - .58) / .42);
  context.save();
  context.globalAlpha = worldOpacity;
  tiles.forEach((tile) => drawSurfaceMarker(context, tile, offset, metrics));
  drawRouteMarkers(context, island.course, offset, metrics);
  drawPortals(context, island.course, offset, metrics);
  drawCourseFeatures(context, island.course, offset, metrics);
  drawHazards(context, island.course, 0, offset, metrics);
  context.restore();
  context.restore();
};

const paintCampaignBackground = (context: CanvasRenderingContext2D, width: number, height: number) => {
  const voidGradient = context.createRadialGradient(width * .48, height * .38, 8, width * .5, height * .5, Math.max(width, height));
  voidGradient.addColorStop(0, '#fbfff8');
  voidGradient.addColorStop(.58, '#e6f2dd');
  voidGradient.addColorStop(1, '#cadfc8');
  context.fillStyle = voidGradient;
  context.fillRect(0, 0, width, height);
  context.fillStyle = '#426f4740';
  for (let index = 0; index < 58; index += 1) {
    const x = ((Math.sin(index * 97.31) + 1) / 2) * width;
    const y = ((Math.sin(index * 41.27 + 2) + 1) / 2) * height;
    const size = index % 5 === 0 ? 2 : 1;
    context.fillRect(x, y, size, size);
  }
};

const paintCampaign = (context: CanvasRenderingContext2D, width: number, height: number, frame: CampaignFrame) => {
  context.clearRect(0, 0, width, height);
  paintCampaignBackground(context, width, height);
  if (!frame.islands.length) return;
  const slots = campaignSlotsFor(frame.islands.length, width, height);
  const focus = slots[Math.max(0, Math.min(slots.length - 1, frame.focusIndex))]!;
  const zoom = Math.max(.2, frame.zoom);
  const pointFor = (slot: CampaignSlot): Point => ({ x: width * .5 + (slot.x - focus.x) * zoom, y: height * .5 + (slot.y - focus.y) * zoom });
  const bridgeProgress = clamped(frame.bridgeProgress ?? 1);

  context.save();
  context.lineCap = 'round';
  for (let index = 1; index < slots.length; index += 1) {
    const from = pointFor(slots[index - 1]!);
    const to = pointFor(slots[index]!);
    const progress = index === slots.length - 1 ? bridgeProgress : 1;
    const end = { x: from.x + (to.x - from.x) * progress, y: from.y + (to.y - from.y) * progress };
    context.beginPath();
    context.moveTo(from.x, from.y);
    context.lineTo(end.x, end.y);
    context.strokeStyle = '#6b936660';
    context.lineWidth = Math.max(2, 4 * zoom);
    context.setLineDash([Math.max(4, 7 * zoom), Math.max(5, 8 * zoom)]);
    context.stroke();
  }
  context.setLineDash([]);
  context.restore();

  frame.islands.forEach((island, index) => drawCampaignIsland(context, island, slots[index]!, zoom, pointFor(slots[index]!)));

  const labelOpacity = clamped(1 - (zoom - 1) / 1.8);
  if (labelOpacity > .02) {
    context.save();
    context.globalAlpha = labelOpacity;
    context.textAlign = 'center';
    context.textBaseline = 'middle';
    context.font = '11px BigBlueTerm, ui-monospace, monospace';
    frame.islands.forEach((island, index) => {
      const slot = slots[index]!;
      const point = pointFor(slot);
      const label = `hole ${island.hole} · ${island.label}`;
      const labelWidth = Math.min(slot.width * zoom * .9, context.measureText(label).width + 18);
      const y = point.y - slot.height * zoom * .38;
      context.fillStyle = '#18351cdb';
      context.fillRect(point.x - labelWidth / 2, y - 10, labelWidth, 20);
      context.strokeStyle = '#cfe5c8';
      context.lineWidth = 1;
      context.strokeRect(point.x - labelWidth / 2, y - 10, labelWidth, 20);
      context.fillStyle = '#f9fff3';
      context.fillText(label, point.x, y + 1);
    });
    if (frame.title) {
      context.textAlign = 'left';
      context.textBaseline = 'top';
      context.fillStyle = '#1a4827';
      context.font = '13px BigBlueTerm, ui-monospace, monospace';
      context.fillText(frame.title, 18, 17);
    }
    context.restore();
  }
};

export const createRenderer = (canvas: HTMLCanvasElement): Renderer => {
  const context = canvas.getContext('2d')!;
  type CourseFrame = { kind: 'course'; course: Course; players: Player[]; hazardElapsedMs: number; aim?: ShotCommand; emotes: readonly EmoteEvent[]; showItems: boolean; phaseCount: number; buildProgress?: number; gadgets: readonly Gadget[]; placement?: { kind: GadgetKind; point?: WorldPoint; valid: boolean }; focus?: Ball };
  type LatestFrame = CourseFrame | { kind: 'campaign'; frame: CampaignFrame };
  let latest: LatestFrame | undefined;
  let trackedCamera: { courseId: string; width: number; height: number; offset: Point; followsFocus: boolean } | undefined;

  const layoutFor = (course: Course, focus?: Ball, advanceCamera = false) => {
    const { width, height } = canvas.getBoundingClientRect();
    const target = cameraFor(course, width, height, focus);
    const previous = trackedCamera;
    const reusable = Boolean(previous && previous.courseId === course.id && previous.width === width && previous.height === height && previous.followsFocus === target.followsFocus);
    let offset = target.offset;
    if (previous && reusable) {
      offset = advanceCamera && target.followsFocus
        ? { x: previous.offset.x + (target.offset.x - previous.offset.x) * .18, y: previous.offset.y + (target.offset.y - previous.offset.y) * .18 }
        : previous.offset;
    }
    if (advanceCamera || !reusable) trackedCamera = { courseId: course.id, width, height, offset, followsFocus: target.followsFocus };
    return { ...target, offset, tiles: visibleTilesFor(course, target.metrics) };
  };

  const paint = (course: Course, players: Player[], hazardElapsedMs: number, aim: ShotCommand | undefined, emotes: readonly EmoteEvent[], showItems: boolean, phaseCount: number, buildProgress?: number, gadgets: readonly Gadget[] = [], placement?: { kind: GadgetKind; point?: WorldPoint; valid: boolean }, focus?: Ball) => {
    const { width, height } = canvas.getBoundingClientRect();
    context.clearRect(0, 0, width, height);
    const voidGradient = context.createRadialGradient(width * .5, height * .4, 10, width * .5, height * .5, Math.max(width, height));
    voidGradient.addColorStop(0, '#ffffff');
    voidGradient.addColorStop(1, '#edf7e8');
    context.fillStyle = voidGradient;
    context.fillRect(0, 0, width, height);
    const { metrics, offset, followsFocus, tiles: allTiles } = layoutFor(course, focus, true);
    const tiles = allTiles.filter((tile) => visibleInViewport(tile, offset, width, height, metrics));
    const directions = [{ x: 0, y: -1 }, { x: 1, y: 0 }, { x: 0, y: 1 }, { x: -1, y: 0 }];
    const clampedBuild = buildProgress === undefined ? undefined : Math.max(0, Math.min(1, buildProgress));
    const drawTile = (tile: VisibleTile) => {
      const stagger = ((tile.x * 13 + tile.y * 7) % 23) / 23 * .58;
      const arrived = clampedBuild === undefined ? 1 : Math.max(0, Math.min(1, (clampedBuild - stagger) / .42));
      if (arrived === 0) return;
      const eased = 1 - (1 - arrived) ** 3;
      const angle = ((tile.x * 19 + tile.y * 11) % 8) * Math.PI / 4;
      const distance = metrics.tileWidth * (2.5 + ((tile.x + tile.y) % 3) * .35) * (1 - eased);
      context.save();
      context.translate(Math.cos(angle) * distance, Math.sin(angle) * distance - distance * .38);
      context.globalAlpha = .22 + eased * .78;
      for (let edge = 0; edge < 4; edge += 1) {
        const first = tile.corners[edge]!;
        const second = tile.corners[(edge + 1) % 4]!;
        if ((first.y + second.y) / 2 <= tile.center.y) continue;
        drawSide(context, tile, edge, neighborFor(course, tile.x, tile.y, directions[edge]!), offset, metrics);
      }
      polygon(context, tile.corners.map((point) => withOffset(point, offset)));
      context.fillStyle = topColorFor(course, tile.tile.surface, tile.tile.theme);
      context.fill();
      drawPattern(context, tile, offset, metrics);
      context.restore();
    };
    tiles.forEach(drawTile);
    const worldOpacity = clampedBuild === undefined ? 1 : Math.max(0, Math.min(1, (clampedBuild - .58) / .42));
    context.save();
    context.globalAlpha = worldOpacity;
    tiles.forEach((tile) => drawSurfaceMarker(context, tile, offset, metrics));
    drawRouteMarkers(context, course, offset, metrics);
    drawPortals(context, course, offset, metrics);
    drawCourseFeatures(context, course, offset, metrics);
    if (showItems) drawItemPads(context, course, offset, metrics);
    drawGadgets(context, course, gadgets, offset, metrics);
    if (placement) drawPlacement(context, course, placement, offset, metrics);
    drawHazards(context, course, hazardElapsedMs, offset, metrics, phaseCount);
    if (aim) {
      const player = players.find((candidate) => !candidate.ball.complete);
      if (player) {
        const path = aimPathFor(course, player.ball, aim).map((point) => withOffset(project(point.x, point.y, point.z, metrics), offset));
        context.beginPath();
        context.moveTo(path[0]!.x, path[0]!.y);
        path.slice(1).forEach((point) => context.lineTo(point.x, point.y));
        context.strokeStyle = aim.kind === 'chip' ? '#c87824' : '#1d5527';
        context.setLineDash(aim.kind === 'chip' ? [3, 3] : [4, 4]);
        context.lineWidth = 2;
        context.stroke();
        context.setLineDash([]);
        if (aim.kind === 'chip') {
          const landing = path.at(-1)!;
          context.beginPath();
          context.arc(landing.x, landing.y, Math.max(3, metrics.tileWidth * .07), 0, Math.PI * 2);
          context.strokeStyle = '#c87824';
          context.lineWidth = 1.5;
          context.stroke();
        }
      }
    }
    [...players].sort((left, right) => left.ball.y - right.ball.y).forEach((player) => drawBall(context, player, offset, metrics));
    drawEmotes(context, players, emotes, offset, metrics);
    context.restore();
    if (followsFocus) {
      context.fillStyle = '#17311bba';
      context.font = '10px BigBlueTerm, ui-monospace, monospace';
      context.textAlign = 'right';
      context.textBaseline = 'bottom';
      context.fillText('FOLLOW CAM', width - 12, height - 10);
    }
  };

  const resize = () => {
    const ratio = window.devicePixelRatio || 1;
    const { width, height } = canvas.getBoundingClientRect();
    canvas.width = Math.max(1, Math.floor(width * ratio));
    canvas.height = Math.max(1, Math.floor(height * ratio));
    context.setTransform(ratio, 0, 0, ratio, 0, 0);
    if (!latest) return;
    if (latest.kind === 'campaign') paintCampaign(context, width, height, latest.frame);
    else paint(latest.course, latest.players, latest.hazardElapsedMs, latest.aim, latest.emotes, latest.showItems, latest.phaseCount, latest.buildProgress, latest.gadgets, latest.placement, latest.focus);
  };
  const observer = new ResizeObserver(resize);
  observer.observe(canvas);
  resize();

  return {
    draw(course, players, hazardElapsedMs, aim, emotes = [], showItems = true, phaseCount = 8, buildProgress, gadgets = [], placement, focus) {
      latest = { kind: 'course', course, players, hazardElapsedMs, aim, emotes, showItems, phaseCount, buildProgress, gadgets, placement, focus };
      paint(course, players, hazardElapsedMs, aim, emotes, showItems, phaseCount, buildProgress, gadgets, placement, focus);
    },
    drawCampaign(frame) {
      latest = { kind: 'campaign', frame };
      trackedCamera = undefined;
      const { width, height } = canvas.getBoundingClientRect();
      paintCampaign(context, width, height, frame);
    },
    aimFromPointer(event, course, ball) {
      const rect = canvas.getBoundingClientRect();
      const { metrics, offset } = layoutFor(course, ball);
      const ballPoint = withOffset(project(ball.x, ball.y, ball.z + .08, metrics), offset);
      const viewDelta = { x: event.clientX - rect.left - ballPoint.x, y: event.clientY - rect.top - ballPoint.y };
      const worldDelta = { x: viewDelta.x / metrics.tileWidth + viewDelta.y / metrics.tileHeight, y: -viewDelta.x / metrics.tileWidth + viewDelta.y / metrics.tileHeight };
      return {
        angle: Math.atan2(worldDelta.y, worldDelta.x),
        power: Math.max(1, Math.min(8, Math.hypot(worldDelta.x, worldDelta.y) * 1.7)),
      };
    },
    tileFromPointer(event, course) {
      const rect = canvas.getBoundingClientRect();
      const pointer = { x: event.clientX - rect.left, y: event.clientY - rect.top };
      const layout = layoutFor(course, latest?.kind === 'course' && latest.course === course ? latest.focus : undefined);
      const tiles = layout.tiles.filter((tile) => visibleInViewport(tile, layout.offset, rect.width, rect.height, layout.metrics));
      const { offset } = layout;
      const contains = (points: readonly Point[]) => {
        let inside = false;
        for (let current = 0, previous = points.length - 1; current < points.length; previous = current++) {
          const from = points[current]!;
          const to = points[previous]!;
          if ((from.y > pointer.y) !== (to.y > pointer.y) && pointer.x < (to.x - from.x) * (pointer.y - from.y) / (to.y - from.y) + from.x) inside = !inside;
        }
        return inside;
      };
      const tile = [...tiles].reverse().find((candidate) => contains(candidate.corners.map((point) => withOffset(point, offset))));
      return tile ? { x: tile.x, y: tile.y } : undefined;
    },
    dispose() { observer.disconnect(); },
  };
};
