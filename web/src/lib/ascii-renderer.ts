export const ASCII_GLYPHS = " .,:;i1tfLCG08@+-|";

export interface AsciiFrameOptions {
  readonly columns: number;
  readonly rows: number;
  readonly seed: string;
}

export interface TableCutsceneFrameOptions extends AsciiFrameOptions {
  readonly players: 3 | 4;
  readonly names: readonly string[];
  readonly activeSeat: number;
  readonly eventKind: "game_started" | "draw" | "discard" | "riichi" | "call" | "pass" | "win" | "draw_game" | "system" | null;
}

export function createAsciiFrame({ columns, rows, seed }: AsciiFrameOptions): readonly string[] {
  if (!Number.isInteger(columns) || !Number.isInteger(rows) || columns < 1 || rows < 1) {
    return Object.freeze([]);
  }
  const frame = Array.from({ length: rows }, () => Array.from({ length: columns }, () => " "));
  const centerX = (columns - 1) / 2;
  const centerY = (rows - 1) / 2;
  const seedValue = hash(seed);

  for (let y = 0; y < rows; y += 1) {
    for (let x = 0; x < columns; x += 1) {
      const distance = Math.hypot((x - centerX) / columns, (y - centerY) / rows);
      const wave = Math.sin((x * 0.29) + (y * 0.17) + seedValue) * 0.18;
      const noise = unitNoise(x, y, seedValue);
      const density = Math.max(0, 0.66 - distance * 1.25 + wave + noise * 0.24);
      const glyphIndex = Math.min(ASCII_GLYPHS.length - 4, Math.floor(density * 18));
      frame[y][x] = glyphIndex > 0 ? ASCII_GLYPHS[glyphIndex] : " ";
    }
  }

  drawFrame(frame, 1, 1, columns - 2, rows - 2);
  drawFrame(
    frame,
    Math.floor(columns * 0.22),
    Math.floor(rows * 0.28),
    Math.max(8, Math.floor(columns * 0.56)),
    Math.max(5, Math.floor(rows * 0.44))
  );
  drawCrosshair(frame, Math.round(centerX), Math.round(centerY));

  return Object.freeze(frame.map((row) => row.join("")));
}

export function createTableCutsceneFrame({ columns, rows, seed, players, names, activeSeat, eventKind }: TableCutsceneFrameOptions): readonly string[] {
  if (!Number.isInteger(columns) || !Number.isInteger(rows) || columns < 24 || rows < 12 || (players !== 3 && players !== 4)) {
    return Object.freeze([]);
  }
  const frame = Array.from({ length: rows }, () => Array.from({ length: columns }, () => " "));
  const seedValue = hash(seed);
  drawFilmGrain(frame, seedValue);
  drawStage(frame);
  drawTable(frame, seedValue, eventKind);
  const seats = seatAnchors(players, columns, rows);
  for (const [seat, anchor] of seats.entries()) {
    drawPlayer(frame, anchor.x, anchor.y, seat, seat === activeSeat, seatLabel(names[seat], seat), anchor.facing);
  }
  drawText(frame, Math.max(2, Math.floor(columns * 0.36)), Math.floor(rows * 0.49), eventCaption(eventKind));
  return Object.freeze(frame.map((row) => row.join("")));
}

function drawFilmGrain(frame: string[][], seedValue: number): void {
  for (let y = 0; y < frame.length; y += 1) {
    for (let x = 0; x < frame[y].length; x += 1) {
      const noise = unitNoise(x, y, seedValue);
      if (noise > 0.975) frame[y][x] = noise > 0.993 ? ":" : ".";
    }
  }
}

function drawStage(frame: string[][]): void {
  const columns = frame[0].length;
  const rows = frame.length;
  drawBox(frame, 1, 1, columns - 2, rows - 2);
  drawLine(frame, Math.floor(columns * 0.14), rows - 2, Math.floor(columns * 0.28), Math.floor(rows * 0.26), "/");
  drawLine(frame, Math.floor(columns * 0.86), rows - 2, Math.floor(columns * 0.72), Math.floor(rows * 0.26), "\\");
}

function drawTable(frame: string[][], seedValue: number, eventKind: TableCutsceneFrameOptions["eventKind"]): void {
  const columns = frame[0].length;
  const rows = frame.length;
  const centerX = Math.floor(columns / 2);
  const centerY = Math.floor(rows / 2);
  const radiusX = Math.max(8, Math.floor(columns * 0.27));
  const radiusY = Math.max(4, Math.floor(rows * 0.22));
  for (let y = centerY - radiusY; y <= centerY + radiusY; y += 1) {
    for (let x = centerX - radiusX; x <= centerX + radiusX; x += 1) {
      const normalX = (x - centerX) / radiusX;
      const normalY = (y - centerY) / radiusY;
      const distance = normalX * normalX + normalY * normalY;
      if (distance > 1.08) continue;
      if (distance > 0.84) frame[y][x] = Math.abs(normalY) > 0.72 ? "-" : "|";
      else if (unitNoise(x, y, seedValue) > 0.78) frame[y][x] = eventKind === "win" ? "@" : eventKind === "call" ? "+" : ".";
      else if (frame[y][x] === " ") frame[y][x] = " ";
    }
  }
  drawText(frame, centerX - 8, centerY - 1, "[ MAHJONG ]");
  drawText(frame, centerX - 11, centerY + 1, ":: :: :: :: :: ::");
  drawText(frame, centerX - 9, centerY + 3, eventKind === "win" ? "[ RON / TSUMO ]" : "[ WALL / DORA ]");
}

function seatAnchors(players: 3 | 4, columns: number, rows: number): Map<number, { x: number; y: number; facing: "up" | "down" | "left" | "right" }> {
  const seats = new Map<number, { x: number; y: number; facing: "up" | "down" | "left" | "right" }>();
  seats.set(0, { x: Math.floor(columns * 0.5), y: Math.floor(rows * 0.79), facing: "up" });
  seats.set(1, { x: Math.floor(columns * 0.79), y: Math.floor(rows * 0.54), facing: "left" });
  seats.set(2, { x: Math.floor(columns * 0.5), y: Math.floor(rows * 0.18), facing: "down" });
  if (players === 4) seats.set(3, { x: Math.floor(columns * 0.21), y: Math.floor(rows * 0.54), facing: "right" });
  return seats;
}

function drawPlayer(
  frame: string[][],
  x: number,
  y: number,
  seat: number,
  active: boolean,
  label: string,
  facing: "up" | "down" | "left" | "right"
): void {
  const ink = active ? "@" : "8";
  const edge = active ? "+" : ".";
  const labelY = facing === "down" ? y - 5 : y + 4;
  drawText(frame, x - Math.floor(label.length / 2), labelY, `${active ? ">" : "["}${label}${active ? "<" : "]"}`);
  if (active) drawBox(frame, x - 5, y - 3, 11, 7);
  if (facing === "up" || facing === "down") {
    setGlyph(frame, x - 1, y - 2, ink); setGlyph(frame, x, y - 2, ink); setGlyph(frame, x + 1, y - 2, ink);
    drawText(frame, x - 2, y - 1, `${edge}${ink}${ink}${ink}${edge}`);
    drawText(frame, x - 3, y, `${edge}${ink}${ink}${ink}${ink}${ink}${edge}`);
    drawText(frame, x - 4, y + 1, facing === "up" ? `/${ink}${ink}|${ink}${ink}\\` : `\\${ink}${ink}|${ink}${ink}/`);
    drawText(frame, x - 5, y + 2, facing === "up" ? ` ${edge}/${ink}${ink}${edge}${ink}${ink}\\ ` : ` ${edge}\\${ink}${ink}${edge}${ink}${ink}/ `);
    return;
  }
  drawText(frame, x - 2, y - 2, `${ink}${ink}${ink}`);
  drawText(frame, x - 3, y - 1, `${edge}${ink}${ink}${ink}${edge}`);
  drawText(frame, x - 4, y, facing === "left" ? `/${ink}${ink}${ink}${ink}${edge}` : `${edge}${ink}${ink}${ink}${ink}\\`);
  drawText(frame, x - 5, y + 1, facing === "left" ? `/${ink}${ink}${ink}${ink}${ink}${edge}` : `${edge}${ink}${ink}${ink}${ink}${ink}\\`);
  drawText(frame, x - 3, y + 2, `${edge}${ink}${ink}|${ink}${ink}${edge}`);
  void seat;
}

function eventCaption(eventKind: TableCutsceneFrameOptions["eventKind"]): string {
  if (eventKind === "win") return "!! SHOWDOWN !!";
  if (eventKind === "call") return "[ CALL ]";
  if (eventKind === "riichi") return "[ RIICHI ]";
  if (eventKind === "discard") return "[ DISCARD ]";
  if (eventKind === "pass") return "[ REACTION ]";
  return "[ TABLE LIVE ]";
}

function seatLabel(name: string | undefined, seat: number): string {
  const fallback = ["YOU", "SHIMOCHA", "TOIMEN", "KAMICHA"][seat] ?? `SEAT ${seat + 1}`;
  return (name ?? fallback).toUpperCase().replace(/[^A-Z0-9 ]/g, "").slice(0, 9) || fallback;
}

function drawBox(frame: string[][], left: number, top: number, width: number, height: number): void {
  drawFrame(frame, left, top, width, height);
}

function drawLine(frame: string[][], startX: number, startY: number, endX: number, endY: number, glyph: string): void {
  const steps = Math.max(Math.abs(endX - startX), Math.abs(endY - startY));
  for (let step = 0; step <= steps; step += 1) {
    setGlyph(frame, Math.round(startX + (endX - startX) * step / steps), Math.round(startY + (endY - startY) * step / steps), glyph);
  }
}

function drawText(frame: string[][], x: number, y: number, text: string): void {
  for (const [offset, glyph] of Array.from(text).entries()) setGlyph(frame, x + offset, y, glyph);
}

function setGlyph(frame: string[][], x: number, y: number, glyph: string): void {
  if (y < 0 || y >= frame.length || x < 0 || x >= frame[y].length) return;
  frame[y][x] = glyph;
}

function drawFrame(frame: string[][], left: number, top: number, width: number, height: number): void {
  const right = left + width - 1;
  const bottom = top + height - 1;
  if (left < 0 || top < 0 || right >= frame[0]?.length || bottom >= frame.length || width < 2 || height < 2) {
    return;
  }
  for (let x = left + 1; x < right; x += 1) {
    frame[top][x] = "-";
    frame[bottom][x] = "-";
  }
  for (let y = top + 1; y < bottom; y += 1) {
    frame[y][left] = "|";
    frame[y][right] = "|";
  }
  frame[top][left] = "+";
  frame[top][right] = "+";
  frame[bottom][left] = "+";
  frame[bottom][right] = "+";
}

function drawCrosshair(frame: string[][], x: number, y: number): void {
  const points = [[x, y], [x - 1, y], [x + 1, y], [x, y - 1], [x, y + 1]];
  for (const [pointX, pointY] of points) {
    if (pointY >= 0 && pointY < frame.length && pointX >= 0 && pointX < frame[0]?.length) {
      frame[pointY][pointX] = pointX === x && pointY === y ? "@" : "+";
    }
  }
}

function hash(value: string): number {
  let result = 2_166_136_261;
  for (const character of value) {
    result ^= character.charCodeAt(0);
    result = Math.imul(result, 16_777_619);
  }
  return (result >>> 0) / 4_294_967_296;
}

function unitNoise(x: number, y: number, seed: number): number {
  const value = Math.sin((x + 1) * 12.9898 + (y + 1) * 78.233 + seed * 43_758.5453) * 43_758.5453;
  return value - Math.floor(value);
}
