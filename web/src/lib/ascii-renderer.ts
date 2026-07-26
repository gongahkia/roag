export const ASCII_GLYPHS = " .,:;i1tfLCG08@+-|";

export interface AsciiFrameOptions {
  readonly columns: number;
  readonly rows: number;
  readonly seed: string;
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
