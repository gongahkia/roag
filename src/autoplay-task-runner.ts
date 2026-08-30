import { assertAutoplayTaskCatalog, autoplayTaskCatalog, AUTOPLAY_TASK_CATALOG_VERSION, type AutoplayTask, type AutoplayTaskExecution, type AutoplayTaskStatus } from './autoplay-task-catalog'

export interface AutoplayTaskResult extends AutoplayTaskExecution {
  id: string
  category: AutoplayTask['category']
  status: AutoplayTaskStatus
  prerequisites: readonly string[]
}

export interface AutoplayTaskSuiteReport {
  version: typeof AUTOPLAY_TASK_CATALOG_VERSION
  scheduler: 'deterministic-priority'
  results: AutoplayTaskResult[]
  passed: boolean
}

const blocked = (task: AutoplayTask, completed: ReadonlySet<string>): AutoplayTaskResult => ({
  id: task.id,
  category: task.category,
  status: 'blocked',
  prerequisites: task.prerequisites,
  actions: [],
  events: [],
  passed: false,
  summary: { missingPrerequisites: task.prerequisites.filter(id => !completed.has(id)) },
  reason: 'required prerequisite did not pass'
})

export const runAutoplayTaskSuite = (catalog: readonly AutoplayTask[] = autoplayTaskCatalog()): AutoplayTaskSuiteReport => {
  assertAutoplayTaskCatalog(catalog)
  const pending = new Map(catalog.map(task => [task.id, task]))
  const completed = new Set<string>()
  const results: AutoplayTaskResult[] = []
  while (pending.size) {
    const eligible = [...pending.values()].filter(task => task.prerequisites.every(id => completed.has(id))).sort((left, right) => right.priority - left.priority || left.id.localeCompare(right.id))
    if (!eligible.length) {
      for (const task of [...pending.values()].sort((left, right) => left.id.localeCompare(right.id))) results.push(blocked(task, completed))
      break
    }
    const task = eligible[0]!
    pending.delete(task.id)
    try {
      const execution = task.run()
      const status: AutoplayTaskStatus = execution.passed ? 'passed' : 'failed'
      results.push({ ...execution, id: task.id, category: task.category, status, prerequisites: task.prerequisites })
      if (execution.passed) completed.add(task.id)
    } catch (error) {
      results.push({ id: task.id, category: task.category, status: 'failed', prerequisites: task.prerequisites, actions: [], events: [], passed: false, summary: {}, reason: error instanceof Error ? error.message : String(error) })
    }
  }
  return { version: AUTOPLAY_TASK_CATALOG_VERSION, scheduler: 'deterministic-priority', results, passed: results.length === catalog.length && results.every(result => result.status === 'passed') }
}
