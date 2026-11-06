// RoagRandom.cs / original-foundation.json parity. Products stay below 2^53.
export const MODULUS = 2147483647;
export const STREAMS = ['map', 'resources', 'combat', 'raids', 'incidents', 'loot'] as const;
export type Streams = Record<typeof STREAMS[number], number>;
export function validSeed(seed: number): boolean { return Number.isInteger(seed) && seed > 0 && seed < MODULUS; }
export function nextRaw(state: number): number {
  if (!validSeed(state)) throw new Error('Seed must be an integer from 1 to 2147483646');
  return state * 48271 % MODULUS;
}
export function draw(state: number, maximum: number): { state: number; value: number } {
  if (!Number.isInteger(maximum) || maximum < 1 || maximum >= MODULUS) throw new Error('Invalid random bound');
  const span = MODULUS - 1; const limit = span - span % maximum;
  do { state = nextRaw(state); } while (state - 1 >= limit);
  return { state, value: (state - 1) % maximum + 1 };
}
export function derive(seed: number, label: string): number {
  if (!validSeed(seed) || !label) throw new Error('Invalid stream seed/label');
  let value = seed;
  const byte = (part: number) => { value = (value * 131 + part) % (MODULUS - 1); };
  // Enumerate Unicode code points and encode UTF-8, not JS UTF-16 code units.
  for (const character of label) {
    let c = character.codePointAt(0)!;
    if (c >= 0xd800 && c <= 0xdfff) c = 0xfffd;
    if (c < 0x80) byte(c);
    else if (c < 0x800) { byte(0xc0 | c >> 6); byte(0x80 | c & 63); }
    else if (c < 0x10000) { byte(0xe0 | c >> 12); byte(0x80 | c >> 6 & 63); byte(0x80 | c & 63); }
    else { byte(0xf0 | c >> 18); byte(0x80 | c >> 12 & 63); byte(0x80 | c >> 6 & 63); byte(0x80 | c & 63); }
  }
  return value + 1;
}
export function streams(seed: number): Streams { return Object.fromEntries(STREAMS.map((label) => [label, derive(seed, label)])) as Streams; }
export function roll(rng: Streams, stream: typeof STREAMS[number], maximum: number): number {
  const result = draw(rng[stream], maximum); rng[stream] = result.state; return result.value;
}
