import { EMOTES, type Ball, type Course, type EmoteEvent, type Player, type Surface, type Tile } from '../core/types';

interface Point { x: number; y: number; }
interface Metrics { tileWidth: number; tileHeight: number; elevation: number; }
interface VisibleTile { x: number; y: number; tile: Tile; corners: Point[]; center: Point; }

const topColors: Record<Surface, string> = {
  void: '#08131d',
  fairway: '#aeb5ae',
  rough: '#7e8d79',
  sand: '#ceb57b',
  ice: '#7fc2cc',
  wall: '#6f7777',
  tee: '#91b7b1',
  cup: '#d5d9c7',
  booster: '#b65a36',
  conveyor: '#6972a9',
};

const faceColors = {
  light: '#f1a02e',
  dark: '#bd481e',
  wallLight: '#db7430',
  wallDark: '#88331d',
};

export interface Renderer {
  draw(course: Course, players: Player[], aim?: { angle: number; power: number }, emotes?: readonly EmoteEvent[]): void;
  aimFromPointer(event: PointerEvent, course: Course, ball: Ball): { angle: number; power: number };
  dispose(): void;
}

const project = (x: number, y: number, z: number, metrics: Metrics): Point => ({
  x: (x - y) * metrics.tileWidth / 2,
  y: (x + y) * metrics.tileHeight / 2 - z * metrics.elevation,
});

const polygon = (context: CanvasRenderingContext2D, points: readonly Point[]) => {
  context.beginPath();
  context.moveTo(points[0]!.x, points[0]!.y);
  points.slice(1).forEach((point) => context.lineTo(point.x, point.y));
  context.closePath();
};

const isVisible = (tile: Tile | undefined) => Boolean(tile && tile.surface !== 'void');

const visibleTilesFor = (course: Course, metrics: Metrics): VisibleTile[] => {
  const visible: VisibleTile[] = [];
  for (let y = 0; y < course.height; y += 1) {
    for (let x = 0; x < course.width; x += 1) {
      const tile = course.tiles[y * course.width + x]!;
      if (!isVisible(tile)) continue;
      const point = (tileX: number, tileY: number) => project(tileX, tileY, tile.height, metrics);
      const corners = [point(x, y), point(x + 1, y), point(x + 1, y + 1), point(x, y + 1)];
      const center = project(x + .5, y + .5, tile.height, metrics);
      visible.push({ x, y, tile, corners, center });
    }
  }
  return visible.sort((left, right) => left.center.y - right.center.y || left.center.x - right.center.x);
};

const boundsFor = (tiles: readonly VisibleTile[], metrics: Metrics) => {
  const points = tiles.flatMap(({ corners, tile }) => [...corners, ...corners.map((point) => ({ x: point.x, y: point.y + (tile.height + 1.1) * metrics.elevation }))]);
  const minX = Math.min(...points.map((point) => point.x));
  const maxX = Math.max(...points.map((point) => point.x));
  const minY = Math.min(...points.map((point) => point.y));
  const maxY = Math.max(...points.map((point) => point.y));
  return { minX, maxX, minY, maxY, width: maxX - minX, height: maxY - minY };
};

const metricsFor = (course: Course, width: number, height: number): Metrics => {
  const unitMetrics = { tileWidth: 1, tileHeight: .5, elevation: .34 };
  const bounds = boundsFor(visibleTilesFor(course, unitMetrics), unitMetrics);
  const tileWidth = Math.max(16, Math.min(64, (width - 38) / bounds.width, (height - 54) / bounds.height));
  return { tileWidth, tileHeight: tileWidth / 2, elevation: tileWidth * .34 };
};

const offsetFor = (tiles: readonly VisibleTile[], metrics: Metrics, width: number, height: number): Point => {
  const { minX, maxX, minY, maxY } = boundsFor(tiles, metrics);
  return {
    x: width / 2 - (minX + maxX) / 2,
    y: height / 2 - (minY + maxY) / 2 + 6,
  };
};

const withOffset = (point: Point, offset: Point): Point => ({ x: point.x + offset.x, y: point.y + offset.y });

const neighborFor = (course: Course, x: number, y: number, direction: Point) => course.tiles[(y + direction.y) * course.width + x + direction.x];

const drawSide = (context: CanvasRenderingContext2D, tile: VisibleTile, edge: number, neighbor: Tile | undefined, offset: Point, metrics: Metrics) => {
  if (isVisible(neighbor) && neighbor!.height >= tile.tile.height) return;
  const edgePoints = [tile.corners[edge]!, tile.corners[(edge + 1) % 4]!];
  const baseline = isVisible(neighbor) ? neighbor!.height : -1.1;
  const depth = Math.max(.15, tile.tile.height - baseline) * metrics.elevation;
  const face = [
    withOffset(edgePoints[0], offset),
    withOffset(edgePoints[1], offset),
    withOffset({ x: edgePoints[1].x, y: edgePoints[1].y + depth }, offset),
    withOffset({ x: edgePoints[0].x, y: edgePoints[0].y + depth }, offset),
  ];
  polygon(context, face);
  const brighter = edgePoints[0].x < edgePoints[1].x;
  const wall = tile.tile.surface === 'wall';
  context.fillStyle = wall ? (brighter ? faceColors.wallLight : faceColors.wallDark) : (brighter ? faceColors.light : faceColors.dark);
  context.fill();
  context.fillStyle = brighter ? '#fff3c342' : '#370d1240';
  for (let stripe = 6; stripe < depth; stripe += 9) {
    context.fillRect(Math.min(face[0].x, face[1].x), Math.max(face[0].y, face[1].y) + stripe, Math.abs(face[1].x - face[0].x), 1);
  }
};

const drawPattern = (context: CanvasRenderingContext2D, tile: VisibleTile, offset: Point, metrics: Metrics) => {
  const center = withOffset(tile.center, offset);
  context.save();
  polygon(context, tile.corners.map((point) => withOffset(point, offset)));
  context.clip();
  const inset = metrics.tileWidth * .19;
  if (tile.tile.surface === 'fairway') {
    if ((tile.x * 5 + tile.y * 3) % 5 === 0) {
      context.fillStyle = '#f4f0d724';
      context.fillRect(center.x - inset, center.y - 1, inset * 2, 2);
    }
  } else if (tile.tile.surface === 'rough') {
    context.fillStyle = '#263d3740';
    for (let dot = -1; dot <= 1; dot += 1) context.fillRect(center.x + dot * 5, center.y + (dot % 2) * 3, 2, 2);
  } else if (tile.tile.surface === 'sand') {
    context.strokeStyle = '#fff3c85c';
    context.lineWidth = 1;
    for (let line = -2; line <= 2; line += 1) {
      context.beginPath();
      context.moveTo(center.x - inset, center.y + line * 3);
      context.lineTo(center.x + inset, center.y + line * 3 + 4);
      context.stroke();
    }
  } else if (tile.tile.surface === 'ice') {
    context.strokeStyle = '#e9ffff8f';
    context.lineWidth = 1.5;
    context.beginPath();
    context.moveTo(center.x - inset, center.y + 3);
    context.lineTo(center.x + inset, center.y - 3);
    context.stroke();
  } else if (tile.tile.surface === 'tee') {
    context.fillStyle = '#f5f4d7';
    context.fillRect(center.x - inset, center.y - 2, inset * 2, 4);
  } else if (tile.tile.surface === 'cup') {
    const size = metrics.tileWidth * .16;
    context.fillStyle = '#17222b';
    context.fillRect(center.x - size, center.y - size / 2, size * 2, size);
    context.fillStyle = '#f4eed3';
    context.fillRect(center.x - size, center.y - size / 2, size, size / 2);
    context.fillRect(center.x, center.y, size, size / 2);
  }
  context.restore();
};

const screenDirection = (x: number, y: number, direction: Point, metrics: Metrics): Point => {
  const startPoint = project(x + .5, y + .5, 0, metrics);
  const endPoint = project(x + .5 + direction.x, y + .5 + direction.y, 0, metrics);
  const length = Math.hypot(endPoint.x - startPoint.x, endPoint.y - startPoint.y) || 1;
  return { x: (endPoint.x - startPoint.x) / length, y: (endPoint.y - startPoint.y) / length };
};

const drawChevron = (context: CanvasRenderingContext2D, center: Point, direction: Point, size: number, color: string) => {
  const perpendicular = { x: -direction.y, y: direction.x };
  const point = (forward: number, sideways: number): Point => ({ x: center.x + direction.x * forward + perpendicular.x * sideways, y: center.y + direction.y * forward + perpendicular.y * sideways });
  polygon(context, [point(size, 0), point(-size * .48, size * .62), point(-size * .12, 0), point(-size * .48, -size * .62)]);
  context.fillStyle = color;
  context.fill();
};

const drawSurfaceMarker = (context: CanvasRenderingContext2D, tile: VisibleTile, offset: Point, metrics: Metrics) => {
  if (tile.tile.surface !== 'booster' && tile.tile.surface !== 'conveyor') return;
  const direction = tile.tile.direction ?? { x: 1, y: 0 };
  const vector = screenDirection(tile.x, tile.y, direction, metrics);
  drawChevron(context, withOffset(tile.center, offset), vector, metrics.tileWidth * .18, tile.tile.surface === 'booster' ? '#ffe4a3' : '#e8f3ff');
};

const drawRouteMarkers = (context: CanvasRenderingContext2D, course: Course, offset: Point, metrics: Metrics) => {
  for (let index = 2; index < course.route.length - 1; index += 4) {
    const point = course.route[index]!;
    const next = course.route[index + 1]!;
    const tile = course.tiles[point.y * course.width + point.x]!;
    if (!isVisible(tile) || tile.surface !== 'fairway') continue;
    const center = withOffset(project(point.x + .5, point.y + .5, tile.height + .025, metrics), offset);
    const direction = screenDirection(point.x, point.y, { x: next.x - point.x, y: next.y - point.y }, metrics);
    drawChevron(context, center, direction, metrics.tileWidth * .13, '#d94327');
  }
};

const drawBall = (context: CanvasRenderingContext2D, ball: Ball, color: string, offset: Point, metrics: Metrics) => {
  const point = withOffset(project(ball.x, ball.y, ball.z + .08, metrics), offset);
  const radius = Math.max(4, metrics.tileWidth * .13);
  context.beginPath();
  context.ellipse(point.x, point.y + radius * .38, radius * .82, radius * .32, 0, 0, Math.PI * 2);
  context.fillStyle = '#02101966';
  context.fill();
  context.beginPath();
  context.arc(point.x, point.y, radius, 0, Math.PI * 2);
  context.fillStyle = color;
  context.fill();
  context.lineWidth = 1.4;
  context.strokeStyle = '#fff7de';
  context.stroke();
  context.beginPath();
  context.arc(point.x - radius * .3, point.y - radius * .34, Math.max(1.2, radius * .22), 0, Math.PI * 2);
  context.fillStyle = '#ffffffb8';
  context.fill();
};

const drawEmotes = (context: CanvasRenderingContext2D, players: Player[], emotes: readonly EmoteEvent[], offset: Point, metrics: Metrics) => {
  emotes.forEach((event, index) => {
    const player = players.find((candidate) => candidate.id === event.playerId);
    const definition = EMOTES.find((candidate) => candidate.id === event.emote);
    if (!player || !definition) return;
    const point = withOffset(project(player.ball.x, player.ball.y, player.ball.z + .62, metrics), offset);
    context.font = '12px BigBlueTerm, ui-monospace, monospace';
    const width = Math.max(23, context.measureText(definition.glyph).width + 10);
    const x = point.x + (index % 3 - 1) * 8 - width / 2;
    const y = point.y - 18 - Math.floor(index / 3) * 11;
    context.fillStyle = '#08131dea';
    context.strokeStyle = player.color;
    context.lineWidth = 1;
    context.fillRect(x, y, width, 15);
    context.strokeRect(x + .5, y + .5, width - 1, 14);
    context.fillStyle = '#f8fffa';
    context.textAlign = 'center';
    context.textBaseline = 'middle';
    context.fillText(definition.glyph, x + width / 2, y + 8);
  });
};

export const createRenderer = (canvas: HTMLCanvasElement): Renderer => {
  const context = canvas.getContext('2d')!;
  let latest: { course: Course; players: Player[]; aim?: { angle: number; power: number }; emotes: readonly EmoteEvent[] } | undefined;

  const layoutFor = (course: Course) => {
    const { width, height } = canvas.getBoundingClientRect();
    const metrics = metricsFor(course, width, height);
    const tiles = visibleTilesFor(course, metrics);
    return { metrics, tiles, offset: offsetFor(tiles, metrics, width, height) };
  };

  const paint = (course: Course, players: Player[], aim: { angle: number; power: number } | undefined, emotes: readonly EmoteEvent[]) => {
    const { width, height } = canvas.getBoundingClientRect();
    context.clearRect(0, 0, width, height);
    const voidGradient = context.createRadialGradient(width * .5, height * .4, 10, width * .5, height * .5, Math.max(width, height));
    voidGradient.addColorStop(0, '#112b3a');
    voidGradient.addColorStop(1, '#050b12');
    context.fillStyle = voidGradient;
    context.fillRect(0, 0, width, height);
    const { metrics, tiles, offset } = layoutFor(course);
    const directions = [{ x: 0, y: -1 }, { x: 1, y: 0 }, { x: 0, y: 1 }, { x: -1, y: 0 }];
    tiles.forEach((tile) => {
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
    });
    tiles.forEach((tile) => drawSurfaceMarker(context, tile, offset, metrics));
    drawRouteMarkers(context, course, offset, metrics);
    if (aim) {
      const player = players.find((candidate) => !candidate.ball.complete);
      if (player) {
        const start = withOffset(project(player.ball.x, player.ball.y, player.ball.z + .15, metrics), offset);
        context.beginPath();
        context.moveTo(start.x, start.y);
        context.lineTo(start.x + Math.cos(aim.angle) * aim.power * metrics.tileWidth * .4, start.y + Math.sin(aim.angle) * aim.power * metrics.tileHeight * .4);
        context.strokeStyle = '#fff1b7';
        context.setLineDash([4, 4]);
        context.lineWidth = 2;
        context.stroke();
        context.setLineDash([]);
      }
    }
    [...players].sort((left, right) => left.ball.y - right.ball.y).forEach((player) => drawBall(context, player.ball, player.color, offset, metrics));
    drawEmotes(context, players, emotes, offset, metrics);
  };

  const resize = () => {
    const ratio = window.devicePixelRatio || 1;
    const { width, height } = canvas.getBoundingClientRect();
    canvas.width = Math.max(1, Math.floor(width * ratio));
    canvas.height = Math.max(1, Math.floor(height * ratio));
    context.setTransform(ratio, 0, 0, ratio, 0, 0);
    if (latest) paint(latest.course, latest.players, latest.aim, latest.emotes);
  };
  const observer = new ResizeObserver(resize);
  observer.observe(canvas);
  resize();

  return {
    draw(course, players, aim, emotes = []) {
      latest = { course, players, aim, emotes };
      paint(course, players, aim, emotes);
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
