import { ATLAS_SPEC, HERO_SPRITE, actorSprite, atlasSourceRect, itemSprite, tileSprite } from './sprites'

type Mapping = { label: string; index: number }

const params = new URLSearchParams(location.search)
const atlasUrl = new URL('./assets/jomon-atlas-source.png', import.meta.url).href
const mappings: Mapping[] = [
  { label: 'actor.hero', index: HERO_SPRITE },
  ...Object.entries(tileSprite).map(([name, index]) => ({ label: `tile.${name}`, index })),
  ...Object.entries(actorSprite).map(([name, index]) => ({ label: `actor.${name}`, index })),
  ...Object.entries(itemSprite).map(([name, index]) => ({ label: `item.${name}`, index }))
].sort((a, b) => a.index - b.index || a.label.localeCompare(b.label))
const mappingFor = (index: number): Mapping[] => mappings.filter(mapping => mapping.index === index)
const initialIndex = Number(params.get('cell'))
let selected = Number.isInteger(initialIndex) && initialIndex >= 0 && initialIndex < ATLAS_SPEC.columns * ATLAS_SPEC.rows ? initialIndex : HERO_SPRITE

document.title = 'Jomon Atlas Inspector'
document.body.classList.add('atlas-page')
const main = document.querySelector('main')!
main.innerHTML = `
  <section class="atlas-inspector" aria-label="Jomon texture atlas inspector">
    <header class="atlas-header">
      <div><p class="atlas-kicker">internal tool</p><h1>Texture atlas inspector</h1></div>
      <a class="atlas-game-link" href="/">game</a>
    </header>
    <section class="atlas-controls" aria-label="Sprite selection">
      <label>Cell index<input id="atlas-index" type="number" min="0" max="127" step="1"></label>
      <output id="atlas-position"></output>
      <output id="atlas-source"></output>
    </section>
    <section class="atlas-layout">
      <div class="atlas-grid-wrap"><div id="atlas-grid" class="atlas-grid" role="grid" aria-label="16 by 8 texture atlas"></div></div>
      <aside class="atlas-detail" aria-live="polite">
        <canvas id="atlas-preview" width="320" height="320" aria-label="Selected sprite preview"></canvas>
        <h2 id="atlas-title"></h2>
        <p id="atlas-mappings"></p>
      </aside>
    </section>
    <p class="atlas-help">Click a cell or enter its index. Rows and columns are 1-based; indexes are 0-based.</p>
  </section>`

const indexInput = document.querySelector<HTMLInputElement>('#atlas-index')!
const positionOutput = document.querySelector<HTMLOutputElement>('#atlas-position')!
const sourceOutput = document.querySelector<HTMLOutputElement>('#atlas-source')!
const grid = document.querySelector<HTMLDivElement>('#atlas-grid')!
const preview = document.querySelector<HTMLCanvasElement>('#atlas-preview')!
const title = document.querySelector<HTMLHeadingElement>('#atlas-title')!
const mappingOutput = document.querySelector<HTMLParagraphElement>('#atlas-mappings')!
const cells: HTMLButtonElement[] = []
const image = new Image()
let loaded = false

for (let index = 0; index < ATLAS_SPEC.columns * ATLAS_SPEC.rows; index++) {
  const cell = document.createElement('button')
  const canvas = document.createElement('canvas')
  const row = Math.floor(index / ATLAS_SPEC.columns) + 1
  const column = index % ATLAS_SPEC.columns + 1
  canvas.width = 72
  canvas.height = 72
  cell.type = 'button'
  cell.className = 'atlas-cell'
  cell.dataset.index = String(index)
  cell.setAttribute('role', 'gridcell')
  cell.setAttribute('aria-label', `Cell ${index}, row ${row}, column ${column}`)
  cell.append(canvas)
  cell.addEventListener('click', () => select(index))
  cells.push(cell)
  grid.append(cell)
}

function drawSprite(canvas: HTMLCanvasElement, index: number): void {
  if (!loaded) return
  const context = canvas.getContext('2d')!
  const rect = atlasSourceRect(index)
  context.clearRect(0, 0, canvas.width, canvas.height)
  context.imageSmoothingEnabled = false
  context.drawImage(image, rect.x, rect.y, rect.width, rect.height, 0, 0, canvas.width, canvas.height)
}

function select(index: number): void {
  selected = index
  indexInput.value = String(index)
  const row = Math.floor(index / ATLAS_SPEC.columns) + 1
  const column = index % ATLAS_SPEC.columns + 1
  const rect = atlasSourceRect(index)
  const assigned = mappingFor(index)
  cells.forEach((cell, cellIndex) => {
    const active = cellIndex === index
    cell.classList.toggle('is-selected', active)
    cell.setAttribute('aria-selected', String(active))
  })
  positionOutput.textContent = `row ${row}, col ${column}`
  sourceOutput.textContent = `source x ${rect.x}, y ${rect.y} · ${rect.width} × ${rect.height}`
  title.textContent = `Cell ${index}`
  mappingOutput.textContent = assigned.length ? assigned.map(mapping => mapping.label).join(' · ') : 'No renderer mapping.'
  drawSprite(preview, index)
  const next = new URL(location.href)
  next.searchParams.set('atlas', '')
  next.searchParams.set('cell', String(index))
  history.replaceState(null, '', next)
}

indexInput.addEventListener('input', () => {
  const index = Number(indexInput.value)
  if (Number.isInteger(index) && index >= 0 && index < cells.length) select(index)
})

image.onload = () => {
  loaded = true
  cells.forEach((cell, index) => drawSprite(cell.querySelector('canvas')!, index))
  select(selected)
}
image.src = atlasUrl
select(selected)
