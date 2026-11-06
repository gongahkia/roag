import selection from '../content/asset-selection.json';
export type Sprite = Readonly<{ id: string; source: string; rect: readonly number[]; frame: readonly number[]; originalName: string; identityConfidence: string; sourceSha256: string; cropRgbaSha256: string }>;
export type Manifest = Readonly<{ version: 1; image: string; imageSha256: string; imageSize: readonly number[]; gutter: number; sprites: Readonly<Record<string, Sprite>> }>;
export const assetUrl = (file: string): string => `${import.meta.env.BASE_URL}game-assets/${file}`;
export function validateManifest(value: unknown): Manifest {
  if (typeof value !== 'object' || value === null) throw new Error('Missing artwork manifest');
  const m = value as Manifest;
  if (m.version !== 1 || m.image !== 'colony.png' || m.gutter !== 2 || !m.sprites || !Array.isArray(m.imageSize) || m.imageSize.length !== 2) throw new Error('Unsupported artwork manifest');
  const validRect = (r: readonly number[], width: number, height: number): boolean => Array.isArray(r) && r.length === 4 && r.every(Number.isInteger) && r[0]! >= 0 && r[1]! >= 0 && r[2]! > 0 && r[3]! > 0 && r[0]! + r[2]! <= width && r[1]! + r[3]! <= height;
  for (const [alias, id] of Object.entries(selection.bindings)) {
    const s = m.sprites[alias];
    if (!s || s.id !== id || !s.source || !validRect(s.rect, Number.MAX_SAFE_INTEGER, Number.MAX_SAFE_INTEGER) || !validRect(s.frame, m.imageSize[0]!, m.imageSize[1]!) || s.rect[2] !== s.frame[2] || s.rect[3] !== s.frame[3]) throw new Error(`Invalid artwork binding: ${alias}`);
  }
  if (Object.keys(m.sprites).length !== Object.keys(selection.bindings).length) throw new Error('Unexpected artwork bindings');
  return m;
}
