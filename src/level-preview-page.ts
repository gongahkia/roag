import { ITEM, biomeName } from './content'
import { createLevelPreview, LEVEL_PREVIEW_BIOMES, levelPreviewConfig, levelPreviewQuery, type LevelPreview } from './level-preview'
import { propDefinition } from './props'
import { biomeVisualGrammar, flowGlyph, terminalTerrainLegend, terrainVisual } from './renderer/visual-grammar'
import type { Biome, Point, TileKind } from './types'

type PreviewResult = { preview: LevelPreview } | { error: string }
const CELL_WIDTH = 12
const CELL_HEIGHT = 15
const tileColors: Partial<Record<TileKind, string>> = {
  exit: '#f4d26a', door: '#c99f67', lockedDoor: '#e9c965', water: '#5c9fca', lava: '#ec7056', pit: '#05070b', rope: '#d8ae73', spikes: '#d9dce1', dart: '#d9dce1', fireVent: '#ff855d', crumble: '#b89a77', boulder: '#a7a0a0', web: '#d8dce1', gas: '#9bc585', support: '#b99b72', rail: '#d7b95f', rubble: '#a7afb8', bramble: '#7da56e', darkness: '#47556a', crate: '#c69a6b', chest: '#f4d26a', altar: '#d2a4e8', shop: '#f4d26a', rescue: '#8ae0b3', smoke: '#a3a8b3', lift: '#e9c47e', breakwall: '#bc8266', current: '#8edce1', deepWater: '#4b8ca0', anchor: '#82cbd1', cliffWall: '#71809d', ledge: '#c2d4dc', graveSoil: '#95836f', cairn: '#b9aa94', ossuary: '#d9d3c5', spiritPath: '#c9a6db', saltMirror: '#fff0ab', brine: '#77c4cb', ice: '#bfeeff', frostRime: '#dff8ff'
}
const nodeColors: Record<string, string> = { start: '#96d38b', landmark: '#f4d26a', fork: '#8fb8ed', objective: '#ee6f78', optionalReward: '#d2a4e8', exit: '#f4d26a', boss: '#ee6f78' }
const element = <T extends HTMLElement>(id: string): T => {
  const value = document.getElementById(id)
  if (!value) throw new Error('missing level preview element: ' + id)
  return value as T
}
const pointKey = (point: Point): string => point.x + ',' + point.y
const pointList = <T extends Point>(values: readonly T[], point: Point): T[] => values.filter(value => value.x === point.x && value.y === point.y)
const countLines = (values: readonly { id: string; count: number }[]): string => values.map(value => value.id + ' ' + value.count).join('\n') || 'none'
const coordinate = (point: Point | undefined): string => point ? point.x + ',' + point.y : 'none'
const selectedResult = (results: ReadonlyMap<Biome, PreviewResult>, biome: Biome): PreviewResult => results.get(biome) ?? { error: 'preview was not generated' }

const main = document.querySelector<HTMLElement>('main')
if (!main) throw new Error('missing application root')
document.body.className = 'level-preview-page'
main.className = 'level-preview'
main.dataset.mode = 'level-preview'
main.innerHTML = '<header class="level-preview-header"><div><p class="level-preview-kicker">LOCAL DEBUG MODE</p><h1>Level generation audit</h1><p>Deterministic previews for all ten biome generators.</p></div><a class="level-preview-game-link" href="./">OPEN GAME</a></header>' +
  '<form id="level-preview-controls" class="level-preview-controls"><label>SEED<input id="level-preview-seed" type="number" min="0" max="4294967295" step="1"></label><label>AREA FLOOR<select id="level-preview-floor"><option value="0">01</option><option value="1">02</option><option value="2">03</option><option value="3">04</option></select></label><label>ROUTE SLOT<input id="level-preview-position" type="number" min="0" max="9" step="1"></label><button type="submit">GENERATE</button><button id="level-preview-random" type="button">RANDOM SEED</button></form>' +
  '<section id="level-preview-tabs" class="level-preview-tabs" aria-label="Biome previews"></section>' +
  '<section class="level-preview-workspace"><article class="level-preview-map-panel"><header><div><p class="level-preview-kicker">FULL MAP</p><h2 id="level-preview-title">Loading</h2></div><p id="level-preview-map-meta" class="level-preview-meta"></p></header><div id="level-preview-map-wrap" class="level-preview-map-wrap"><canvas id="level-preview-canvas" aria-label="Generated level preview"></canvas><p id="level-preview-map-error" class="level-preview-error" hidden></p></div><div class="level-preview-toggles"><label><input id="level-preview-macro" type="checkbox">macro graph</label><label><input id="level-preview-placements" type="checkbox">placement audit</label><label><input id="level-preview-entities" type="checkbox">actors, props, rewards</label></div><p class="level-preview-help">Hover a tile for terrain, entities, macro roles, and placement evidence.</p></article>' +
  '<aside class="level-preview-audit"><section><p class="level-preview-kicker">AUDIT RESULT</p><dl id="level-preview-summary" class="level-preview-summary"></dl></section><section><p class="level-preview-kicker">TILE INSPECTOR</p><pre id="level-preview-inspector">Hover the map.</pre></section><details open><summary>ROUTE CONTRACT + MACRO</summary><pre id="level-preview-contract"></pre></details><details open><summary>PLACEMENT AUDIT</summary><pre id="level-preview-placements-output"></pre></details><details><summary>TERRAIN + CONTENT</summary><pre id="level-preview-content"></pre></details><details><summary>VALIDATION</summary><pre id="level-preview-validation"></pre></details></aside></section>' +
  '<footer class="level-preview-footer">Share this exact audit: <code id="level-preview-query"></code></footer>'

const canvas = element<HTMLCanvasElement>('level-preview-canvas')
const context = (() => {
  const value = canvas.getContext('2d')
  if (!value) throw new Error('Canvas 2D is unavailable')
  return value
})()
const controls = element<HTMLFormElement>('level-preview-controls')
const seedInput = element<HTMLInputElement>('level-preview-seed')
const floorInput = element<HTMLSelectElement>('level-preview-floor')
const positionInput = element<HTMLInputElement>('level-preview-position')
const randomButton = element<HTMLButtonElement>('level-preview-random')
const tabs = element<HTMLElement>('level-preview-tabs')
const title = element<HTMLElement>('level-preview-title')
const mapMeta = element<HTMLElement>('level-preview-map-meta')
const mapError = element<HTMLElement>('level-preview-map-error')
const macroToggle = element<HTMLInputElement>('level-preview-macro')
const placementsToggle = element<HTMLInputElement>('level-preview-placements')
const entitiesToggle = element<HTMLInputElement>('level-preview-entities')
const summary = element<HTMLDListElement>('level-preview-summary')
const inspector = element<HTMLPreElement>('level-preview-inspector')
const contractOutput = element<HTMLPreElement>('level-preview-contract')
const placementsOutput = element<HTMLPreElement>('level-preview-placements-output')
const contentOutput = element<HTMLPreElement>('level-preview-content')
const validationOutput = element<HTMLPreElement>('level-preview-validation')
const queryOutput = element<HTMLElement>('level-preview-query')
let config = levelPreviewConfig(new URLSearchParams(location.search))
let previews = new Map<Biome, PreviewResult>()

function setSummary(values: readonly [string, string][]): void {
  summary.replaceChildren()
  for (const [label, value] of values) {
    const term = document.createElement('dt')
    term.textContent = label
    const definition = document.createElement('dd')
    definition.textContent = value
    summary.append(term, definition)
  }
}

function updateUrl(): void {
  const query = levelPreviewQuery(config)
  history.replaceState(null, '', query)
  queryOutput.textContent = location.origin + location.pathname + query
}

function syncControls(): void {
  seedInput.value = String(config.seed)
  floorInput.value = String(config.areaFloor)
  positionInput.value = String(config.routePosition)
  macroToggle.checked = config.macro
  placementsToggle.checked = config.placements
  entitiesToggle.checked = config.entities
}

function drawMap(preview: LevelPreview): void {
  const { floor, macro } = preview
  const grammar = biomeVisualGrammar[floor.biome]
  canvas.width = floor.width * CELL_WIDTH
  canvas.height = floor.height * CELL_HEIGHT
  canvas.dataset.biome = floor.biome
  canvas.dataset.layout = floor.layoutId
  const edgeCells = new Set((macro?.edges ?? []).flatMap(edge => edge.cells).map(pointKey))
  context.fillStyle = grammar.background
  context.fillRect(0, 0, canvas.width, canvas.height)
  context.font = '12px BigBlueTerm, monospace'
  context.textAlign = 'center'
  context.textBaseline = 'middle'
  for (let y = 0; y < floor.height; y++) for (let x = 0; x < floor.width; x++) {
    const point = { x, y }
    const tile = floor.tiles[y * floor.width + x]!
    const visual = terrainVisual(floor.biome, tile.kind, 'runes')
    const background = visual.background ?? grammar.background
    context.fillStyle = background
    context.fillRect(x * CELL_WIDTH, y * CELL_HEIGHT, CELL_WIDTH, CELL_HEIGHT)
    if (config.macro && edgeCells.has(pointKey(point))) {
      context.fillStyle = '#315d7d80'
      context.fillRect(x * CELL_WIDTH, y * CELL_HEIGHT, CELL_WIDTH, CELL_HEIGHT)
    }
    context.fillStyle = tile.flow ? tile.flow.hazard ? '#ee6f78' : '#a8eff0' : visual.color ?? tileColors[tile.kind] ?? '#d6dce8'
    context.fillText(tile.flow ? flowGlyph(tile.flow.direction, 'runes') : visual.glyph ?? terminalTerrainLegend[tile.kind].glyph, x * CELL_WIDTH + CELL_WIDTH / 2, y * CELL_HEIGHT + CELL_HEIGHT / 2 + 1)
  }
  if (config.entities) {
    for (const prop of floor.props) {
      const definition = propDefinition(prop.kind)
      context.fillStyle = definition.color
      context.fillText(definition.glyph, prop.x * CELL_WIDTH + CELL_WIDTH / 2, prop.y * CELL_HEIGHT + CELL_HEIGHT / 2 + 1)
    }
    for (const milestone of floor.milestones) {
      context.fillStyle = milestone.kind === 'waycache' || milestone.kind === 'relic' ? '#f4d26a' : milestone.kind === 'augment' ? '#ee6f78' : '#d2a4e8'
      context.fillText(milestone.kind === 'waycache' ? 'W' : milestone.kind === 'augment' ? '!' : milestone.kind === 'relic' ? 'R' : '*', milestone.x * CELL_WIDTH + CELL_WIDTH / 2, milestone.y * CELL_HEIGHT + CELL_HEIGHT / 2 + 1)
    }
    for (const encounter of floor.encounters ?? []) {
      context.fillStyle = encounter.kind === 'bloodBargain' ? '#ee6f78' : encounter.kind === 'shiftingChamber' ? '#8fb8ed' : '#96d38b'
      context.fillText(encounter.kind === 'wayfarer' ? '&' : encounter.kind === 'bloodBargain' ? '$' : '?', encounter.x * CELL_WIDTH + CELL_WIDTH / 2, encounter.y * CELL_HEIGHT + CELL_HEIGHT / 2 + 1)
    }
    for (const item of floor.items) {
      context.fillStyle = ITEM[item.id]?.color ?? '#f4d26a'
      context.fillText(ITEM[item.id]?.glyph ?? '*', item.x * CELL_WIDTH + CELL_WIDTH / 2, item.y * CELL_HEIGHT + CELL_HEIGHT / 2 + 1)
    }
    for (const actor of floor.actors) {
      context.fillStyle = actor.color
      context.fillText(actor.glyph, actor.x * CELL_WIDTH + CELL_WIDTH / 2, actor.y * CELL_HEIGHT + CELL_HEIGHT / 2 + 1)
    }
  }
  context.fillStyle = '#96d38b'
  context.fillText('@', floor.start.x * CELL_WIDTH + CELL_WIDTH / 2, floor.start.y * CELL_HEIGHT + CELL_HEIGHT / 2 + 1)
  if (config.placements) for (const placement of preview.placements) if (placement.selected) {
    context.fillStyle = placement.usedFallback ? '#ee6f78' : '#f4d26a'
    context.fillRect(placement.selected.x * CELL_WIDTH + CELL_WIDTH - 3, placement.selected.y * CELL_HEIGHT + 1, 2, 2)
  }
  if (config.macro) for (const node of macro?.nodes ?? []) {
    context.strokeStyle = nodeColors[node.kind]
    context.lineWidth = 1
    context.strokeRect(node.footprint.x * CELL_WIDTH + .5, node.footprint.y * CELL_HEIGHT + .5, node.footprint.width * CELL_WIDTH - 1, node.footprint.height * CELL_HEIGHT - 1)
    context.fillStyle = nodeColors[node.kind]
    context.fillText(node.kind[0]!.toUpperCase(), node.footprint.x * CELL_WIDTH + 5, node.footprint.y * CELL_HEIGHT + 6)
  }
}

function inspect(preview: LevelPreview, point: Point): void {
  const { floor, macro, placements } = preview
  const tile = floor.tiles[point.y * floor.width + point.x]
  if (!tile) { inspector.textContent = 'Outside level bounds.'; return }
  const lines = [point.x + ',' + point.y + '  ' + tile.kind + (tile.flow ? ' flow=' + tile.flow.direction + (tile.flow.hazard ? '/' + tile.flow.hazard : '') : '')]
  const actors = pointList(floor.actors, point)
  const props = pointList(floor.props, point)
  const items = pointList(floor.items, point)
  const milestones = pointList(floor.milestones, point)
  const encounters = pointList(floor.encounters ?? [], point)
  const selected = placements.filter(placement => placement.selected?.x === point.x && placement.selected?.y === point.y)
  const nodes = (macro?.nodes ?? []).filter(node => point.x >= node.footprint.x && point.x < node.footprint.x + node.footprint.width && point.y >= node.footprint.y && point.y < node.footprint.y + node.footprint.height)
  const edges = (macro?.edges ?? []).filter(edge => edge.cells.some(cell => cell.x === point.x && cell.y === point.y))
  for (const actor of actors) lines.push('actor: ' + actor.name + ' [' + actor.kind + ']')
  for (const prop of props) lines.push('prop: ' + propDefinition(prop.kind).name + ' [' + prop.kind + ']')
  for (const item of items) lines.push('item: ' + (ITEM[item.id]?.name ?? item.id) + ' x' + item.count)
  for (const milestone of milestones) lines.push('milestone: ' + milestone.kind + ' [' + (milestone.rewardKey ?? milestone.id) + ']')
  for (const encounter of encounters) lines.push('encounter: ' + encounter.kind)
  for (const node of nodes) lines.push('macro node: ' + node.nodeId + ' (' + node.kind + ', ' + node.visual + ')')
  for (const edge of edges) lines.push('macro edge: ' + edge.edgeId + ' (' + edge.modes.join('|') + ')')
  for (const placement of selected) lines.push('placement: ' + placement.id + ' rank=' + placement.ranked + (placement.usedFallback ? ' fallback' : ''))
  inspector.textContent = lines.join('\n')
}

function renderTabs(): void {
  tabs.replaceChildren()
  for (const biome of LEVEL_PREVIEW_BIOMES) {
    const result = selectedResult(previews, biome)
    const button = document.createElement('button')
    button.type = 'button'
    button.dataset.biome = biome
    button.className = 'level-preview-tab' + (biome === config.biome ? ' is-active' : '') + ('error' in result || !result.preview.metrics.acceptance.valid ? ' is-fail' : ' is-pass')
    if ('error' in result) button.textContent = biomeName[biome] + ' · ERROR'
    else button.textContent = biomeName[biome] + ' · ' + result.preview.floor.layoutId + ' · ' + result.preview.metrics.topology.topology
    tabs.append(button)
  }
}

function renderAudit(preview: LevelPreview): void {
  const { floor, route, macro, placements, validation, metrics } = preview
  const pass = metrics.acceptance.valid
  setSummary([
    ['RESULT', pass ? 'PASS' : 'FAIL'],
    ['RECIPE', floor.layoutId],
    ['SIZE', floor.width + ' × ' + floor.height],
    ['MACRO', metrics.topology.topology ?? 'missing'],
    ['GRAPH', metrics.topology.nodes + ' nodes / ' + metrics.topology.edges + ' edges / ' + metrics.topology.loops + ' loops'],
    ['CHOICES', String(metrics.topology.routeChoices)],
    ['PLACEMENTS', metrics.placements.selected + ' selected / ' + metrics.placements.fallbacks + ' fallback'],
    ['ESCALATION', floor.escalation?.phase ?? 'none']
  ])
  contractOutput.textContent = [
    'contract ' + (route?.id ?? 'missing'),
    'recipe ' + (macro?.recipeId ?? 'missing') + ' · topology ' + (macro?.topology ?? 'missing') + ' · compile attempt ' + (macro?.attempt ?? 'n/a'),
    '',
    'nodes',
    ...(macro?.nodes.map(node => node.nodeId + ' ' + node.kind + ' @ ' + node.footprint.x + ',' + node.footprint.y + ' ' + node.footprint.width + 'x' + node.footprint.height + ' · ' + node.landmark + ' · ' + node.visual) ?? ['missing']),
    '',
    'edges',
    ...(macro?.edges.map(edge => edge.edgeId + ' ' + edge.modes.join('|') + ' · ' + edge.visual + ' · ' + coordinate(edge.from) + ' → ' + coordinate(edge.to)) ?? ['missing']),
    '',
    'route tags',
    ...(route?.nodes.map(node => node.id + ' ' + node.kind + ' · terrain=' + node.tags.terrain.join('|') + ' · encounter=' + node.tags.encounter.join('|') + ' · reward=' + node.tags.reward.join('|')) ?? ['missing'])
  ].join('\n')
  placementsOutput.textContent = placements.map(placement => placement.id + ' @ ' + coordinate(placement.selected) + ' · rank=' + placement.ranked + (placement.usedFallback ? ' · FALLBACK' : '') + (placement.diagnostics.length ? ' · ' + placement.diagnostics.join('; ') : '')).join('\n') || 'none'
  contentOutput.textContent = [
    'terrain',
    countLines(metrics.terrain),
    '',
    'actors',
    countLines(metrics.encounters.actors),
    '',
    'tactical',
    countLines(metrics.encounters.tactical),
    '',
    'events',
    countLines(metrics.encounters.events),
    '',
    'ecology',
    countLines(metrics.ecology),
    '',
    'boon distances',
    metrics.boonTiming.boonDistances.join(', ') || 'none',
    '',
    'escalation',
    floor.escalation ? floor.escalation.phase + ' · ' + floor.escalation.promise + ' · payoff: ' + floor.escalation.payoff : 'none'
  ].join('\n')
  validationOutput.textContent = [
    'generation validation: ' + (validation.valid ? 'PASS' : 'FAIL'),
    ...(validation.errors.length ? validation.errors : ['none']),
    '',
    'audit acceptance: ' + (metrics.acceptance.valid ? 'PASS' : 'FAIL'),
    ...(metrics.acceptance.errors.length ? metrics.acceptance.errors : ['none']),
    '',
    'macro diagnostics',
    ...(macro?.diagnostics.length ? macro.diagnostics : ['none'])
  ].join('\n')
}

function renderSelected(): void {
  const result = selectedResult(previews, config.biome)
  title.textContent = biomeName[config.biome] + ' · stage ' + String(config.areaFloor + 1).padStart(2, '0')
  if ('error' in result) {
    canvas.hidden = true
    mapError.hidden = false
    mapError.textContent = result.error
    mapMeta.textContent = 'generation failed'
    setSummary([['RESULT', 'ERROR'], ['BIOME', biomeName[config.biome]]])
    inspector.textContent = 'No generated level.'
    contractOutput.textContent = 'No contract available.'
    placementsOutput.textContent = 'No placement audit available.'
    contentOutput.textContent = 'No content audit available.'
    validationOutput.textContent = result.error
    return
  }
  const preview = result.preview
  canvas.hidden = false
  mapError.hidden = true
  mapMeta.textContent = preview.floor.layoutId + ' · ' + preview.floor.width + '×' + preview.floor.height + ' · seed ' + config.seed + ' · route slot ' + config.routePosition
  drawMap(preview)
  renderAudit(preview)
  inspect(preview, preview.floor.start)
}

function render(): void {
  syncControls()
  renderTabs()
  renderSelected()
  updateUrl()
}

function generateAll(): void {
  previews = new Map()
  for (const biome of LEVEL_PREVIEW_BIOMES) {
    try { previews.set(biome, { preview: createLevelPreview({ seed: config.seed, biome, areaFloor: config.areaFloor, routePosition: config.routePosition }) }) }
    catch (error) { previews.set(biome, { error: error instanceof Error ? error.message : String(error) }) }
  }
  render()
}

function setOverlay(key: 'macro' | 'placements' | 'entities', value: boolean): void {
  config = { ...config, [key]: value }
  render()
}

controls.addEventListener('submit', event => {
  event.preventDefault()
  const query = new URLSearchParams(levelPreviewQuery(config).slice(1))
  query.set('seed', seedInput.value)
  query.set('floor', floorInput.value)
  query.set('position', positionInput.value)
  config = levelPreviewConfig(query)
  generateAll()
})
randomButton.addEventListener('click', () => {
  const value = new Uint32Array(1)
  crypto.getRandomValues(value)
  config = { ...config, seed: value[0]! }
  generateAll()
})
tabs.addEventListener('click', event => {
  const button = (event.target as Element).closest<HTMLButtonElement>('button[data-biome]')
  const biome = button?.dataset.biome
  if (!biome || !LEVEL_PREVIEW_BIOMES.includes(biome as Biome)) return
  config = { ...config, biome: biome as Biome }
  render()
})
macroToggle.addEventListener('change', () => setOverlay('macro', macroToggle.checked))
placementsToggle.addEventListener('change', () => setOverlay('placements', placementsToggle.checked))
entitiesToggle.addEventListener('change', () => setOverlay('entities', entitiesToggle.checked))
canvas.addEventListener('mousemove', event => {
  const result = selectedResult(previews, config.biome)
  if ('error' in result) return
  const bounds = canvas.getBoundingClientRect()
  const point = { x: Math.floor((event.clientX - bounds.left) * canvas.width / bounds.width / CELL_WIDTH), y: Math.floor((event.clientY - bounds.top) * canvas.height / bounds.height / CELL_HEIGHT) }
  inspect(result.preview, point)
})
canvas.addEventListener('mouseleave', () => { inspector.textContent = 'Hover the map.' })
window.addEventListener('popstate', () => {
  config = levelPreviewConfig(new URLSearchParams(location.search))
  generateAll()
})
void document.fonts?.ready.then(() => renderSelected())
generateAll()
