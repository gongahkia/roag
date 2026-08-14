import type { Ball, Course, CoursePickup, Player, Surface, Tile, WorldRotation } from '../core/types';

const TILE_W = 34;
const TILE_H = 17;
const ELEVATION = 11;

const colors: Record<Surface, string> = {
  void: '#07111f',
  fairway: '#174a43',
  rough: '#284f37',
  sand: '#8a6e3b',
  ice: '#397b92',
  wall: '#74524a',
  tee: '#346f5e',
  cup: '#5c3248',
  booster: '#8c5737',
  conveyor: '#454c8f',
};

const glyphs: Partial<Record<Surface, string>> = {
  rough: ',',
  sand: ':',
  ice: '~',
  wall: '#',
  tee: 'T',
  cup: 'O',
  booster: '>',
  conveyor: '=',
};

export interface Renderer {
  draw(course: Course, players: Player[], aim?: { angle: number; power: number }, rotation?: WorldRotation): void;
  pick(event: PointerEvent): { x: number; y: number };
  dispose(): void;
}

const project = (x: number, y: number, z: number) => ({
  x: (x - y) * TILE_W / 2,
  y: (x + y) * TILE_H / 2 - z * ELEVATION,
});

const rotate = (course: Course, x: number, y: number, rotation: WorldRotation) => {
  const centerX = course.width / 2;
  const centerY = course.height / 2;
  const deltaX = x - centerX;
  const deltaY = y - centerY;
  if (rotation === 1) return { x: centerX - deltaY, y: centerY + deltaX };
  if (rotation === 2) return { x: centerX - deltaX, y: centerY - deltaY };
  if (rotation === 3) return { x: centerX + deltaY, y: centerY - deltaX };
  return { x, y };
};

const polygon = (context: CanvasRenderingContext2D, points: { x: number; y: number }[]) => {
  context.beginPath();
  context.moveTo(points[0]!.x, points[0]!.y);
  points.slice(1).forEach((point) => context.lineTo(point.x, point.y));
  context.closePath();
};

const drawTile = (context: CanvasRenderingContext2D, course: Course, tile: Tile, x: number, y: number, offset: { x: number; y: number }, rotation: WorldRotation) => {
  if (tile.surface === 'void') return;
  if (tile.rotationGate !== undefined && tile.rotationGate !== rotation) return;
  const point = (tileX: number, tileY: number) => {
    const rotated = rotate(course, tileX, tileY, rotation);
    return project(rotated.x, rotated.y, tile.height);
  };
  const top = point(x, y);
  const north = point(x + 1, y);
  const east = point(x + 1, y + 1);
  const south = point(x, y + 1);
  const face = [top, north, east, south].map((point) => ({ x: point.x + offset.x, y: point.y + offset.y }));
  polygon(context, face);
  context.fillStyle = colors[tile.surface];
  context.fill();
  context.strokeStyle = '#0b1e2d';
  context.lineWidth = 1;
  context.stroke();
  const glyph = glyphs[tile.surface];
  if (glyph) {
    const rotated = rotate(course, x + 0.5, y + 0.5, rotation);
    const center = project(rotated.x, rotated.y, tile.height + 0.02);
    context.fillStyle = tile.surface === 'sand' ? '#ffe2a3' : '#e8f7ef';
    context.font = 'bold 13px ui-monospace, SFMono-Regular, Menlo, monospace';
    context.textAlign = 'center';
    context.textBaseline = 'middle';
    context.fillText(glyph, center.x + offset.x, center.y + offset.y);
  }
};

const drawBall = (context: CanvasRenderingContext2D, course: Course, ball: Ball, color: string, offset: { x: number; y: number }, rotation: WorldRotation) => {
  const rotated = rotate(course, ball.x, ball.y, rotation);
  const point = project(rotated.x, rotated.y, ball.z + 0.08);
  context.beginPath();
  context.arc(point.x + offset.x, point.y + offset.y, 5, 0, Math.PI * 2);
  context.fillStyle = color;
  context.fill();
  context.lineWidth = 1.5;
  context.strokeStyle = '#fff6e8';
  context.stroke();
};

const drawPickup = (context: CanvasRenderingContext2D, course: Course, pickup: CoursePickup, offset: { x: number; y: number }, rotation: WorldRotation) => {
  if (pickup.collected || pickup.rotation !== rotation) return;
  const tile = course.tiles[pickup.point.y * course.width + pickup.point.x]!;
  const rotated = rotate(course, pickup.point.x + 0.5, pickup.point.y + 0.5, rotation);
  const point = project(rotated.x, rotated.y, tile.height + 0.28);
  context.fillStyle = '#dca4ff';
  context.font = 'bold 15px BigBlueTerm, ui-monospace, monospace';
  context.textAlign = 'center';
  context.textBaseline = 'middle';
  context.fillText('✦', point.x + offset.x, point.y + offset.y);
};

export const createRenderer = (canvas: HTMLCanvasElement): Renderer => {
  const context = canvas.getContext('2d')!;
  let latest: { course: Course; players: Player[]; aim?: { angle: number; power: number }; rotation: WorldRotation } | undefined;
  const paint = (course: Course, players: Player[], aim: { angle: number; power: number } | undefined, rotation: WorldRotation) => {
    const { width, height } = canvas.getBoundingClientRect();
    context.clearRect(0, 0, width, height);
    context.fillStyle = '#07111f';
    context.fillRect(0, 0, width, height);
    const boardOffset = offset();
    for (let y = 0; y < course.height; y += 1) {
      for (let x = 0; x < course.width; x += 1) drawTile(context, course, course.tiles[y * course.width + x]!, x, y, boardOffset, rotation);
    }
    course.pickups.forEach((pickup) => drawPickup(context, course, pickup, boardOffset, rotation));
    if (aim) {
      const player = players.find((candidate) => !candidate.ball.complete);
      if (player) {
        const rotated = rotate(course, player.ball.x, player.ball.y, rotation);
        const start = project(rotated.x, rotated.y, player.ball.z + 0.15);
        context.beginPath();
        context.moveTo(start.x + boardOffset.x, start.y + boardOffset.y);
        context.lineTo(start.x + Math.cos(aim.angle) * aim.power * 13 + boardOffset.x, start.y + Math.sin(aim.angle) * aim.power * 6 + boardOffset.y);
        context.strokeStyle = '#f9e2af';
        context.setLineDash([4, 4]);
        context.lineWidth = 2;
        context.stroke();
        context.setLineDash([]);
      }
    }
    [...players].sort((left, right) => left.ball.y - right.ball.y).forEach((player) => drawBall(context, course, player.ball, player.color, boardOffset, rotation));
  };
  const resize = () => {
    const ratio = window.devicePixelRatio || 1;
    const { width, height } = canvas.getBoundingClientRect();
    canvas.width = Math.max(1, Math.floor(width * ratio));
    canvas.height = Math.max(1, Math.floor(height * ratio));
    context.setTransform(ratio, 0, 0, ratio, 0, 0);
    if (latest) paint(latest.course, latest.players, latest.aim, latest.rotation);
  };
  const observer = new ResizeObserver(resize);
  observer.observe(canvas);
  resize();

  const offset = () => ({ x: canvas.clientWidth / 2 - 2, y: 82 });
  return {
    draw(course, players, aim, rotation = 0) {
      latest = { course, players, aim, rotation };
      paint(course, players, aim, rotation);
    },
    pick(event) {
      const rect = canvas.getBoundingClientRect();
      return { x: event.clientX - rect.left, y: event.clientY - rect.top };
    },
    dispose() { observer.disconnect(); },
  };
};
