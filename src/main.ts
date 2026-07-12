import './style.css'
import { AudioBus } from './audio'
import { newRun, perform, quickCast } from './engine'
import { TerminalRenderer } from './renderer'
import { deleteRun, loadRecords, loadRun, saveRecords, saveRun } from './storage'
import type { Direction, Records, RunState } from './types'

const canvas = document.querySelector<HTMLCanvasElement>('#game')!
const renderer = new TerminalRenderer(canvas)
const audio = new AudioBus()
const visualToggle = document.querySelector<HTMLButtonElement>('#visual-toggle')!
let state: RunState | undefined
let saved: RunState | undefined
let records: Records = { bestDepth: 0, wins: 0, deaths: 0, runs: [] }
let recordedEnd = false

const loadVisualMode = (): boolean => { try { return localStorage.getItem('blockscape-visual-mode') === 'sprites' } catch { return false } }
renderer.setSpriteMode(loadVisualMode())
refreshVisualToggle()

void Promise.all([loadRun(), loadRecords()]).then(([run, loadedRecords]) => {
  saved = run
  records = loadedRecords
  renderer.render(state, records)
})
renderer.render(state, records)

window.addEventListener('keydown', event => {
  if (event.metaKey || event.ctrlKey) return
  const command = event.key
  if (shouldPrevent(command)) event.preventDefault()
  if (command.toLowerCase() === 'v') { toggleVisualMode(); return }
  if (!state || state.status === 'dead' || state.status === 'victory') {
    if (command.toLowerCase() === 'n') start()
    else if (command.toLowerCase() === 'l' && saved) { state = structuredClone(saved); recordedEnd = false; renderer.render(state, records) }
    else if (command.toLowerCase() === 'h') renderer.render({ ...newRun(1), status: 'playing', modal: { kind: 'help' } }, records)
    return
  }
  const direction = directionFor(command)
  let events = [] as ReturnType<typeof perform>
  if (event.altKey && direction && direction !== 'wait') events = quickCast(state, direction)
  else if (event.shiftKey && direction && direction !== 'wait' && !state.modal) events = run(state, command)
  else events = perform(state, command)
  audio.play(events)
  renderer.trigger(events, state)
  if (events.includes('floor')) { saved = structuredClone(state); void saveRun(state) }
  if ((events.includes('death') || events.includes('win')) && !recordedEnd) finish(events.includes('win'))
  renderer.render(state, records)
})

visualToggle.addEventListener('click', toggleVisualMode)

function start(): void {
  state = newRun()
  saved = structuredClone(state)
  recordedEnd = false
  void saveRun(state)
  audio.play(['menu'])
  renderer.trigger(['floor'], state)
  renderer.render(state, records)
}

function toggleVisualMode(): void {
  renderer.setSpriteMode(!renderer.isSpriteMode)
  try { localStorage.setItem('blockscape-visual-mode', renderer.isSpriteMode ? 'sprites' : 'ascii') } catch { }
  refreshVisualToggle()
  renderer.render(state, records)
}

function refreshVisualToggle(): void { visualToggle.textContent = renderer.isSpriteMode ? 'V · SPRITES' : 'V · ASCII' }

function finish(won: boolean): void {
  if (!state) return
  recordedEnd = true
  records.bestDepth = Math.max(records.bestDepth, state.floor.index + 1)
  if (won) records.wins++
  else records.deaths++
  records.runs.unshift({ seed: state.seed, floor: state.floor.index + 1, score: state.hero.gold, won, date: new Date().toISOString() })
  records.runs = records.runs.slice(0, 20)
  saved = undefined
  void Promise.all([deleteRun(), saveRecords(records)])
}

function run(game: RunState, command: string): ReturnType<typeof perform> {
  const events = [] as ReturnType<typeof perform>
  for (let i = 0; i < 18; i++) {
    const x = game.hero.x
    const y = game.hero.y
    const next = perform(game, command)
    events.push(...next)
    const threats = game.floor.actors.some(actor => actor.hostile && Math.max(Math.abs(actor.x - game.hero.x), Math.abs(actor.y - game.hero.y)) <= 7 && game.floor.tiles[actor.y * 48 + actor.x].visible)
    if (game.status !== 'playing' || game.modal || (x === game.hero.x && y === game.hero.y) || threats) break
  }
  return events
}

function directionFor(command: string): Direction | undefined {
  const directions: Record<string, Direction> = { i: 'nw', o: 'n', p: 'ne', k: 'w', ';': 'e', ',': 'sw', '.': 's', '/': 'se', ArrowUp: 'n', ArrowDown: 's', ArrowLeft: 'w', ArrowRight: 'e', Numpad7: 'nw', Numpad8: 'n', Numpad9: 'ne', Numpad4: 'w', Numpad5: 'wait', Numpad6: 'e', Numpad1: 'sw', Numpad2: 's', Numpad3: 'se', l: 'wait', Enter: 'wait' }
  return directions[command] ?? directions[command.toLowerCase()]
}

function shouldPrevent(command: string): boolean { return Boolean(directionFor(command)) || [' ', 'Escape', '`', 'h', 'H', 'u', 'U', 'd', 'D', 't', 'T', 'e', 'E', 'a', 'A', 'b', 'B', 'r', 'R', 'g', 'G', 'c', 'C', 'q', 'Q', 'x', 'X', 's', 'S'].includes(command) }
