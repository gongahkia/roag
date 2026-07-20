import { readFileSync } from 'node:fs'
import { autoplayDecision, createAutoplayContext, recordAutoplayTransition } from '../src/autoplay'
import { perform } from '../src/engine'
import type { RunState } from '../src/types'

const snapshot = JSON.parse(readFileSync(process.env.STATE_FILE!, 'utf8')) as { report: { state: RunState; debug?: { recentPositions?: string[]; objectiveTarget?: string } } }
const prefixes = ['', '.', './', './;', './;/']
for (const prefix of prefixes) {
  const state = structuredClone(snapshot.report.state)
  const context = createAutoplayContext()
  context.recentPositions = [...(snapshot.report.debug?.recentPositions ?? [])]
  context.objectiveTarget = snapshot.report.debug?.objectiveTarget
  for (const command of prefix) perform(state, command)
  const decision = autoplayDecision(state, 'omniscient', 'clear', context)
  console.log(JSON.stringify({ prefix, recent: context.recentPositions, status: state.status, floor: state.floor.index + 1, objective: state.floor.objective.status, position: `${state.hero.x},${state.hero.y}`, decision }))
}
