export class Rng {
  private state: number

  constructor(seed: number) { this.state = seed >>> 0 || 0x9e3779b9 }

  next(): number {
    let x = this.state
    x ^= x << 13
    x ^= x >>> 17
    x ^= x << 5
    this.state = x >>> 0
    return this.state
  }

  int(min: number, max: number): number { return min + this.next() % (max - min + 1) }
  chance(percent: number): boolean { return this.int(1, 100) <= percent }
  pick<T>(values: readonly T[]): T { return values[this.int(0, values.length - 1)] }
  shuffle<T>(values: T[]): T[] {
    for (let i = values.length - 1; i > 0; i--) {
      const j = this.int(0, i)
      ;[values[i], values[j]] = [values[j], values[i]]
    }
    return values
  }
}

export const mixSeed = (seed: number, value: number): number => {
  let n = (seed ^ Math.imul(value + 0x9e3779b9, 0x85ebca6b)) >>> 0
  n ^= n >>> 16
  return Math.imul(n, 0xc2b2ae35) >>> 0
}
