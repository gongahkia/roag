import { describe, expect, it } from 'vitest'
import { ActiveSessionRouteClock } from './route-reckoning-runtime'

describe('active-session Route Reckoning runtime clock', () => {
  it('requests the same canonical steps for chunked and unchunked visible elapsed time', () => {
    const chunked = new ActiveSessionRouteClock()
    const whole = new ActiveSessionRouteClock()
    chunked.requestSteps(0, true)
    whole.requestSteps(0, true)
    const chunkedSteps = Array.from({ length: 10 }, (_, index) => chunked.requestSteps((index + 1) * 100, true)).reduce((total, steps) => total + steps, 0)
    expect(chunkedSteps).toBe(whole.requestSteps(1_000, true))
    expect(chunkedSteps).toBe(10)
  })

  it('freezes for explicit pauses and discards hidden or suspended elapsed time', () => {
    const clock = new ActiveSessionRouteClock()
    clock.requestSteps(0, true)
    expect(clock.requestSteps(500, true)).toBe(5)
    expect(clock.requestSteps(5_000, false)).toBe(0)
    expect(clock.requestSteps(15_000, false)).toBe(0)
    expect(clock.requestSteps(15_100, true)).toBe(1)
    expect(clock.requestSteps(20_000, true)).toBe(0)
    expect(clock.requestSteps(20_100, true)).toBe(1)
  })
})
