import { readFileSync } from 'node:fs'
import { autoplayDecision, createAutoplayContext, recordAutoplayTransition } from '../src/autoplay'
import { perform } from '../src/engine'
import type { RunState } from '../src/types'

const snapshot = JSON.parse(readFileSync(process.env.STATE_FILE!, 'utf8')) as { report: { state: RunState } }
const prefixes = ['./', './;', './;/', './;;', './;;p', './;;po']
for (const prefix of prefixes) {
  const state = structuredClone(snapshot.report.state)
  const context = createAutoplayContext()
  for (const command of prefix) perform(state, command)
  let halt = 'turn-limit'
  for (let step = 0; step < 80 && state.status === 'playing'; step++) {
    const decision = autoplayDecision(state, 'omniscient', 'clear', context)
    if (!decision) { halt = context.lastReason ?? 'none'; break }
    const before = structuredClone(state)
    perform(state, decision.command)
    recordAutoplayTransition(context, before, decision.command, state)
  }
  console.log(JSON.stringify({ prefix, status: state.status, floor: state.floor.index + 1, objective: state.floor.objective.status, position: `${state.hero.x},${state.hero.y}`, halt, recoveries: context.loopRecoveries, lastReason: context.lastReason }))
}
