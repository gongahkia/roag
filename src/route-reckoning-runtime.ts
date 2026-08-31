import { ROUTE_RECKONING_MAX_CATCH_UP_STEPS, ROUTE_RECKONING_STEP_MS } from './engine/route-reckoning'

/** Converts visible active runtime elapsed time into bounded integer simulation steps. */
export class ActiveSessionRouteClock {
  private lastRuntimeMs: number | undefined
  private remainderMs = 0

  reset(runtimeMs?: number): void {
    this.lastRuntimeMs = runtimeMs
    this.remainderMs = 0
  }

  requestSteps(runtimeMs: number, active: boolean): number {
    if (!active || !Number.isFinite(runtimeMs)) {
      this.reset(runtimeMs)
      return 0
    }
    if (this.lastRuntimeMs === undefined) {
      this.lastRuntimeMs = runtimeMs
      return 0
    }
    const elapsedMs = Math.max(0, runtimeMs - this.lastRuntimeMs)
    this.lastRuntimeMs = runtimeMs
    const maxElapsedMs = ROUTE_RECKONING_MAX_CATCH_UP_STEPS * ROUTE_RECKONING_STEP_MS
    if (elapsedMs > maxElapsedMs) {
      this.remainderMs = 0
      return 0
    }
    this.remainderMs = Math.min(maxElapsedMs, this.remainderMs + elapsedMs)
    const steps = Math.floor(this.remainderMs / ROUTE_RECKONING_STEP_MS)
    this.remainderMs -= steps * ROUTE_RECKONING_STEP_MS
    return steps
  }
}
