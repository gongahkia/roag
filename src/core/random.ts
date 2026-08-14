export class Random {
  private state: number;

  constructor(seed: string) {
    let value = 2166136261;
    for (const character of seed) {
      value ^= character.charCodeAt(0);
      value = Math.imul(value, 16777619);
    }
    this.state = value >>> 0;
  }

  next(): number {
    this.state += 0x6d2b79f5;
    let value = this.state;
    value = Math.imul(value ^ (value >>> 15), value | 1);
    value ^= value + Math.imul(value ^ (value >>> 7), value | 61);
    return ((value ^ (value >>> 14)) >>> 0) / 4294967296;
  }

  int(min: number, max: number): number {
    return Math.floor(this.next() * (max - min + 1)) + min;
  }

  chance(probability: number): boolean {
    return this.next() < probability;
  }

  pick<T>(values: readonly T[]): T {
    return values[this.int(0, values.length - 1)]!;
  }
}

export const hashSeed = (seed: string, index: number) => `${seed}-${index.toString(36)}`;
