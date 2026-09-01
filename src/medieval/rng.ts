const hash = (value: string, initial = 0x811c9dc5): number => {
  let current = initial >>> 0
  for (let index = 0; index < value.length; index++) {
    current ^= value.charCodeAt(index)
    current = Math.imul(current, 0x01000193) >>> 0
  }
  return current >>> 0
}

export const hashSeed = (value: string, initial?: number): number => hash(value, initial)

export class SeededRng {
  private state: number

  constructor(seed: string) { this.state = hashSeed(seed) || 0x6d2b79f5 }

  next(): number {
    let value = this.state += 0x6d2b79f5
    value = Math.imul(value ^ value >>> 15, value | 1)
    value ^= value + Math.imul(value ^ value >>> 7, value | 61)
    return ((value ^ value >>> 14) >>> 0) / 0x100000000
  }

  integer(maximumExclusive: number): number {
    if (!Number.isSafeInteger(maximumExclusive) || maximumExclusive <= 0) throw new Error('maximumExclusive must be a positive safe integer')
    return Math.floor(this.next() * maximumExclusive)
  }

  between(minimum: number, maximum: number): number {
    if (!Number.isSafeInteger(minimum) || !Number.isSafeInteger(maximum) || maximum < minimum) throw new Error('invalid integer range')
    return minimum + this.integer(maximum - minimum + 1)
  }

  pick<T>(values: readonly T[]): T {
    if (!values.length) throw new Error('cannot choose from an empty collection')
    return values[this.integer(values.length)]!
  }
}
