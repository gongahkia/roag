import { isGateOpen, sweeperDirection } from '../core/hazards';
import { floorHeightAt, tileCornerHeights } from '../core/physics';
import { EMOTES, type Ball, type Course, type EmoteEvent, type Player, type PortalEndpoint, type Surface, type Tile } from '../core/types';

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
  draw(course: Course, players: Player[], phase: number, aim?: { angle: number; power: number }, emotes?: readonly EmoteEvent[], showItems?: boolean, phaseCount?: number, assemblyProgress?: number): void;
  aimFromPointer(event: PointerEvent, course: Course, ball: Ball): { angle: number; power: number };
  dispose(): void;
}

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
  const tileWidth = Math.max(16, Math.min(64, (width - 38) / bounds.width, (height - 54) / bounds.height));
  return { tileWidth, tileHeight: tileWidth / 2, elevation: tileWidth * .34 };
};

const offsetFor = (tiles: readonly VisibleTile[], metrics: ProjectionMetrics, width: number, height: number): Point => {
  const { minX, maxX, minY, maxY } = boundsFor(tiles, metrics);
  return {
    x: width / 2 - (minX + maxX) / 2,
    y: height / 2 - (minY + maxY) / 2 + 6,
  };
};

const withOffset = (point: Point, offset: Point): Point => ({ x: point.x + offset.x, y: point.y + offset.y });

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

const drawHazards = (context: CanvasRenderingContext2D, course: Course, phase: number, offset: Point, metrics: ProjectionMetrics, phaseCount = 8) => {
  for (const hazard of course.hazards) {
    const center = withOffset(project(hazard.point.x + .5, hazard.point.y + .5, heightAt(course, hazard.point) + .1, metrics), offset);
    if (hazard.kind === 'sweeper') {
      const direction = sweeperDirection(hazard, phase, phaseCount);
      const end = withOffset(project(hazard.point.x + .5 + direction.x * hazard.radius, hazard.point.y + .5 + direction.y * hazard.radius, heightAt(course, hazard.point) + .13, metrics), offset);
      context.strokeStyle = '#f0a232';
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
      context.fillStyle = '#d95a36';
      context.fill();
      context.strokeStyle = '#ffffff';
      context.lineWidth = 1;
      context.stroke();
      continue;
    }
    const open = isGateOpen(hazard, phase, phaseCount);
    const width = metrics.tileWidth * .23;
    const height = metrics.tileHeight * .48;
    context.fillStyle = open ? '#7acbbd88' : '#d95a36d9';
    context.fillRect(center.x - width, center.y - height, width * 2, height * 2);
    context.strokeStyle = open ? '#1d7168' : '#ffffff';
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
    context.fillStyle = pad.kind === 'recovery' ? '#73d9d5' : '#c88cf5';
    context.fill();
    context.strokeStyle = '#ffffff';
    context.lineWidth = 1.2;
    context.stroke();
    context.fillStyle = '#1c4321';
    context.font = `${Math.max(8, metrics.tileWidth * .16)}px BigBlueTerm, ui-monospace, monospace`;
    context.textAlign = 'center';
    context.textBaseline = 'middle';
    context.fillText(pad.kind === 'recovery' ? '+' : '!', center.x, center.y + 1);
  });
};

const statusLabels = (player: Player) => {
  const upgrades: Record<string, string> = {
    'heavy ball': 'HB', 'ice skates': 'IS', 'extra charge': 'EC', 'bank shot': 'BK', 'hazard shield': 'HS', 'chaos magnet': 'CM', 'portal savvy': 'PS', 'second wind': 'SW', scavenger: 'SC',
  };
  return [
    player.inventory ? `I:${player.inventory}` : undefined,
    player.spareInventory ? `I:${player.spareInventory}` : undefined,
    player.ballForm ? `BALL:${player.ballForm}` : undefined,
    player.twoPuttsArmed ? '2P!' : undefined,
    player.secondWindAvailable ? 'SW!' : undefined,
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

export const createRenderer = (canvas: HTMLCanvasElement): Renderer => {
  const context = canvas.getContext('2d')!;
  let latest: { course: Course; players: Player[]; phase: number; aim?: { angle: number; power: number }; emotes: readonly EmoteEvent[]; showItems: boolean; phaseCount: number; assemblyProgress?: number } | undefined;

  const layoutFor = (course: Course) => {
    const { width, height } = canvas.getBoundingClientRect();
    const metrics = metricsFor(course, width, height);
    const tiles = visibleTilesFor(course, metrics);
    return { metrics, tiles, offset: offsetFor(tiles, metrics, width, height) };
  };

  const paint = (course: Course, players: Player[], phase: number, aim: { angle: number; power: number } | undefined, emotes: readonly EmoteEvent[], showItems: boolean, phaseCount: number, assemblyProgress?: number) => {
    const { width, height } = canvas.getBoundingClientRect();
    context.clearRect(0, 0, width, height);
    const voidGradient = context.createRadialGradient(width * .5, height * .4, 10, width * .5, height * .5, Math.max(width, height));
    voidGradient.addColorStop(0, '#ffffff');
    voidGradient.addColorStop(1, '#edf7e8');
    context.fillStyle = voidGradient;
    context.fillRect(0, 0, width, height);
    const { metrics, tiles, offset } = layoutFor(course);
    const directions = [{ x: 0, y: -1 }, { x: 1, y: 0 }, { x: 0, y: 1 }, { x: -1, y: 0 }];
    const clampedAssembly = assemblyProgress === undefined ? undefined : Math.max(0, Math.min(1, assemblyProgress));
    const drawTile = (tile: VisibleTile) => {
      const stagger = ((tile.x * 13 + tile.y * 7) % 23) / 23 * .58;
      const arrived = clampedAssembly === undefined ? 1 : Math.max(0, Math.min(1, (clampedAssembly - stagger) / .42));
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
      context.fillStyle = topColors[tile.tile.surface];
      context.fill();
      drawPattern(context, tile, offset, metrics);
      context.restore();
    };
    tiles.forEach(drawTile);
    const worldOpacity = clampedAssembly === undefined ? 1 : Math.max(0, Math.min(1, (clampedAssembly - .58) / .42));
    context.save();
    context.globalAlpha = worldOpacity;
    tiles.forEach((tile) => drawSurfaceMarker(context, tile, offset, metrics));
    drawRouteMarkers(context, course, offset, metrics);
    drawPortals(context, course, offset, metrics);
    if (showItems) drawItemPads(context, course, offset, metrics);
    drawHazards(context, course, phase, offset, metrics, phaseCount);
    if (aim) {
      const player = players.find((candidate) => !candidate.ball.complete);
      if (player) {
        const start = withOffset(project(player.ball.x, player.ball.y, player.ball.z + .15, metrics), offset);
        context.beginPath();
        context.moveTo(start.x, start.y);
        const projected = projectWorldDirection({ x: Math.cos(aim.angle) * aim.power * .4, y: Math.sin(aim.angle) * aim.power * .4 }, metrics);
        context.lineTo(start.x + projected.x, start.y + projected.y);
        context.strokeStyle = '#1d5527';
        context.setLineDash([4, 4]);
        context.lineWidth = 2;
        context.stroke();
        context.setLineDash([]);
      }
    }
    [...players].sort((left, right) => left.ball.y - right.ball.y).forEach((player) => drawBall(context, player, offset, metrics));
    drawEmotes(context, players, emotes, offset, metrics);
    context.restore();
  };

  const resize = () => {
    const ratio = window.devicePixelRatio || 1;
    const { width, height } = canvas.getBoundingClientRect();
    canvas.width = Math.max(1, Math.floor(width * ratio));
    canvas.height = Math.max(1, Math.floor(height * ratio));
    context.setTransform(ratio, 0, 0, ratio, 0, 0);
    if (latest) paint(latest.course, latest.players, latest.phase, latest.aim, latest.emotes, latest.showItems, latest.phaseCount, latest.assemblyProgress);
  };
  const observer = new ResizeObserver(resize);
  observer.observe(canvas);
  resize();

  return {
    draw(course, players, phase, aim, emotes = [], showItems = true, phaseCount = 8, assemblyProgress) {
      latest = { course, players, phase, aim, emotes, showItems, phaseCount, assemblyProgress };
      paint(course, players, phase, aim, emotes, showItems, phaseCount, assemblyProgress);
    },
    aimFromPointer(event, course, ball) {
      const rect = canvas.getBoundingClientRect();
      const { metrics, offset } = layoutFor(course);
      const ballPoint = withOffset(project(ball.x, ball.y, ball.z + .08, metrics), offset);
      const viewDelta = { x: event.clientX - rect.left - ballPoint.x, y: event.clientY - rect.top - ballPoint.y };
      const worldDelta = { x: viewDelta.x / metrics.tileWidth + viewDelta.y / metrics.tileHeight, y: -viewDelta.x / metrics.tileWidth + viewDelta.y / metrics.tileHeight };
      return {
        angle: Math.atan2(worldDelta.y, worldDelta.x),
        power: Math.max(1, Math.min(8, Math.hypot(worldDelta.x, worldDelta.y) * 1.7)),
      };
    },
    dispose() { observer.disconnect(); },
  };
};
