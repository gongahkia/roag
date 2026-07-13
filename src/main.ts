import './style.css'
import { AudioBus } from './audio'
import { event, hasEvent, initialRoute, navigate, newRun, perform, quickCast, type ScreenRoute } from './engine'
import { TerminalRenderer } from './renderer'
import { deleteRun, loadRecords, loadRun, saveRecords, saveRun } from './storage'
import type { Direction, Records, RunState } from './types'

const canvas = document.querySelector<HTMLCanvasElement>('#game')!
const renderer = new TerminalRenderer(canvas)
const audio = new AudioBus()
let state: RunState | undefined
let saved: RunState | undefined
let records: Records = { bestDepth: 0, wins: 0, deaths: 0, runs: [] }
let recordedEnd = false
let route: ScreenRoute = initialRoute()

const loadVisualMode = (): boolean => { try { return localStorage.getItem('blockscape-visual-mode') === 'sprites' } catch { return false } }
renderer.setSpriteMode(loadVisualMode())

void Promise.all([loadRun(), loadRecords()]).then(([run, loadedRecords]) => {
  saved = run
  records = loadedRecords
  redraw()
})
redraw()

window.addEventListener('keydown', keyboardEvent => {
  if (keyboardEvent.metaKey || keyboardEvent.ctrlKey) return
  const command = keyboardEvent.key
  if (shouldPrevent(command)) keyboardEvent.preventDefault()
  if (command.toLowerCase() === 'v') { toggleVisualMode(); return }
  if (route.screen !== 'level') {
    let nextRoute = navigate(route, command, Boolean(saved))
    if (nextRoute.screen === route.screen) return
    if (nextRoute.screen === 'approach') nextRoute = { ...nextRoute, heirSeed: Math.floor(Math.random() * 0x7fffffff) }
    if (nextRoute.screen === 'title') nextRoute = { ...nextRoute, heirSeed: undefined }
    if (nextRoute.screen === 'level') {
      if (route.screen === 'area') start()
      else if (saved) { state = structuredClone(saved); recordedEnd = false }
      else return
    }
    route = nextRoute
    audio.play([event('menu')])
    redraw()
    return
  }
  if (!state || state.status === 'dead' || state.status === 'victory') return
  const direction = directionFor(command)
  let events = [] as ReturnType<typeof perform>
  if (keyboardEvent.altKey && direction && direction !== 'wait') events = quickCast(state, direction)
  else if (keyboardEvent.shiftKey && direction && direction !== 'wait' && !state.modal) events = run(state, command)
  else events = perform(state, command)
  audio.play(events)
  renderer.trigger(events, state)
  if (hasEvent(events, 'floor')) { saved = structuredClone(state); void saveRun(state) }
  if ((hasEvent(events, 'death') || hasEvent(events, 'win')) && !recordedEnd) finish(hasEvent(events, 'win'))
  redraw()
})

function start(): void {
  state = newRun(route.heirSeed)
  saved = structuredClone(state)
  recordedEnd = false
  void saveRun(state)
  audio.play([event('menu')])
  renderer.trigger([event('floor')], state)
}

function toggleVisualMode(): void {
  renderer.setSpriteMode(!renderer.isSpriteMode)
  try { localStorage.setItem('blockscape-visual-mode', renderer.isSpriteMode ? 'sprites' : 'ascii') } catch { }
  redraw()
}

function redraw(): void { renderer.render(route, state, records) }

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
