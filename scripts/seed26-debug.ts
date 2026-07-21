import { autoplayCandidateDiagnostics, autoplayDecision, createAutoplayContext, recordAutoplayTransitionSnapshot, snapshotAutoplayTransition } from '../src/autoplay'
import { newRun, perform } from '../src/engine'
import { nextArea } from '../src/engine/campaign'
import { observeTelemetryTurn, telemetrySnapshot } from '../src/telemetry'

let state = newRun(26)
let context = createAutoplayContext()
for (let count = 0; state.status === 'playing' && state.turn < 900; count++) {
  const decision = autoplayDecision(state, 'omniscient', 'clear', context)
  if (state.floor.index === 14 && state.hero.x === 12 && state.hero.y === 28) console.log(JSON.stringify({ turn: state.turn, hero: state.hero, telegraphs: state.floor.telegraphs, items: state.floor.items.filter(item => item.x === 12 && item.y === 28), actors: state.floor.actors.filter(actor => actor.hostile && actor.health > 0), diagnostics: autoplayCandidateDiagnostics(state, 'omniscient', 'clear', context), decision }, null, 2))
  if (!decision) break
  const before = telemetrySnapshot(state)
  const transition = snapshotAutoplayTransition(state)
  const events = perform(state, decision.command)
  observeTelemetryTurn(state, before, events, decision.command)
  recordAutoplayTransitionSnapshot(context, transition, decision.command, state)
  if (events.some(event => event.type === 'areaComplete')) {
    const successor = nextArea(state.area ?? state.floor.biome)
    if (!successor) break
    const next = newRun(state.seed, successor, 0, state.hero, state.rescuedNpcs, [])
    next.turn = state.turn
    next.lineageEvents = structuredClone(state.lineageEvents ?? [])
    next.telemetry = structuredClone(state.telemetry!)
    state = next
    context = createAutoplayContext()
  }
}
