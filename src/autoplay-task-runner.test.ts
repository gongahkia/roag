import { describe, expect, it } from 'vitest'
import { autoplayTaskCatalog, type AutoplayTask } from './autoplay-task-catalog'
import { runAutoplayTaskSuite } from './autoplay-task-runner'

describe('autoplay task suite', () => {
  it('runs every required Voyager task in deterministic priority order', () => {
    const report = runAutoplayTaskSuite()
    expect(report.passed).toBe(true)
    expect(report.results.map(result => result.id)).toEqual(autoplayTaskCatalog().map(task => task.id))
    expect(report.results.every(result => result.status === 'passed')).toBe(true)
  }, 15_000)

  it('reports cyclic prerequisites as blocked instead of skipping them', () => {
    const cycle: readonly AutoplayTask[] = [
      { id: 'alpha', category: 'tactical', priority: 1, prerequisites: ['beta'], uiTaskId: 'ui.alpha', run: () => ({ actions: [], events: [], passed: true, summary: {} }) },
      { id: 'beta', category: 'tactical', priority: 1, prerequisites: ['alpha'], uiTaskId: 'ui.beta', run: () => ({ actions: [], events: [], passed: true, summary: {} }) }
    ]
    const report = runAutoplayTaskSuite(cycle)
    expect(report.passed).toBe(false)
    expect(report.results).toMatchObject([{ id: 'alpha', status: 'blocked' }, { id: 'beta', status: 'blocked' }])
  })
})
