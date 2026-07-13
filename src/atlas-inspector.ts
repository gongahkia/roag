import { spriteSheetSpecs, type SpriteSheetSpec } from './sprites'

const params = new URLSearchParams(location.search)
const requestedSheet = params.get('sheet')
let sheet = spriteSheetSpecs.find(candidate => candidate.id === requestedSheet) ?? spriteSheetSpecs[0]
let selected = validCell(Number(params.get('cell'))) ? Number(params.get('cell')) : 0
let loaded = false

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
      <label>Sheet<select id="atlas-sheet"></select></label>
      <label>Cell index<input id="atlas-index" type="number" min="0" step="1"></label>
      <output id="atlas-position"></output>
      <output id="atlas-source"></output>
    </section>
    <section class="atlas-layout">
      <div class="atlas-grid-wrap"><div id="atlas-grid" class="atlas-grid" role="grid"></div></div>
      <aside class="atlas-detail" aria-live="polite">
        <canvas id="atlas-preview" width="320" height="320" aria-label="Selected sprite preview"></canvas>
        <h2 id="atlas-title"></h2>
        <p id="atlas-mappings"></p>
      </aside>
    </section>
    <p class="atlas-help">Choose a sheet, then click a cell or enter its zero-based index. Rows and columns are one-based.</p>
  </section>`

const sheetInput = document.querySelector<HTMLSelectElement>('#atlas-sheet')!
const indexInput = document.querySelector<HTMLInputElement>('#atlas-index')!
const positionOutput = document.querySelector<HTMLOutputElement>('#atlas-position')!
const sourceOutput = document.querySelector<HTMLOutputElement>('#atlas-source')!
const grid = document.querySelector<HTMLDivElement>('#atlas-grid')!
const preview = document.querySelector<HTMLCanvasElement>('#atlas-preview')!
const title = document.querySelector<HTMLHeadingElement>('#atlas-title')!
const mappingOutput = document.querySelector<HTMLParagraphElement>('#atlas-mappings')!
const image = new Image()
let cells: HTMLButtonElement[] = []

for (const candidate of spriteSheetSpecs) {
  const option = document.createElement('option')
  option.value = candidate.id
  option.textContent = candidate.id
  sheetInput.append(option)
}

function validCell(index: number): boolean { return Number.isInteger(index) && index >= 0 && index < sheet.columns * sheet.rows }

function drawSprite(canvas: HTMLCanvasElement, index: number): void {
  if (!loaded) return
  const context = canvas.getContext('2d')!
  const column = index % sheet.columns
  const row = Math.floor(index / sheet.columns)
  context.clearRect(0, 0, canvas.width, canvas.height)
  context.imageSmoothingEnabled = false
  context.drawImage(image, column * 16, row * 16, 16, 16, 0, 0, canvas.width, canvas.height)
}

function labelFor(index: number): string | undefined { return sheet.labels[index] || undefined }

function mappingText(index: number): string {
  const label = labelFor(index)
  if (!label) return 'No renderer mapping.'
  if (label.startsWith('prop.')) return `${label} — present in this sheet but not represented by a game TileKind.`
  return label
}

function select(index: number): void {
  if (!validCell(index)) return
  selected = index
  indexInput.value = String(index)
  const row = Math.floor(index / sheet.columns) + 1
  const column = index % sheet.columns + 1
  cells.forEach((cell, cellIndex) => {
    const active = cellIndex === index
    cell.classList.toggle('is-selected', active)
    cell.setAttribute('aria-selected', String(active))
  })
  positionOutput.textContent = `row ${row}, col ${column}`
  sourceOutput.textContent = `source x ${(column - 1) * 16}, y ${(row - 1) * 16} · 16 × 16`
  title.textContent = `${sheet.id} · cell ${index}`
  mappingOutput.textContent = mappingText(index)
  drawSprite(preview, index)
  const next = new URL(location.href)
  next.searchParams.set('atlas', '')
  next.searchParams.set('sheet', sheet.id)
  next.searchParams.set('cell', String(index))
  history.replaceState(null, '', next)
}

function populateGrid(): void {
  cells = []
  grid.replaceChildren()
  grid.style.gridTemplateColumns = `repeat(${sheet.columns},72px)`
  grid.setAttribute('aria-label', `${sheet.columns} by ${sheet.rows} ${sheet.id} texture sheet`)
  indexInput.max = String(sheet.columns * sheet.rows - 1)
  for (let index = 0; index < sheet.columns * sheet.rows; index++) {
    const cell = document.createElement('button')
    const canvas = document.createElement('canvas')
    const row = Math.floor(index / sheet.columns) + 1
    const column = index % sheet.columns + 1
    canvas.width = 72
    canvas.height = 72
    cell.type = 'button'
    cell.className = 'atlas-cell'
    cell.dataset.index = String(index)
    cell.setAttribute('role', 'gridcell')
    cell.setAttribute('aria-label', `${labelFor(index) ?? 'Unmapped'}; cell ${index}, row ${row}, column ${column}`)
    cell.append(canvas)
    cell.addEventListener('click', () => select(index))
    cells.push(cell)
    grid.append(cell)
  }
}

function loadSheet(nextSheet: SpriteSheetSpec): void {
  sheet = nextSheet
  selected = 0
  loaded = false
  sheetInput.value = sheet.id
  populateGrid()
  image.src = sheet.url
  select(selected)
}

sheetInput.addEventListener('change', () => loadSheet(spriteSheetSpecs.find(candidate => candidate.id === sheetInput.value) ?? spriteSheetSpecs[0]))
indexInput.addEventListener('input', () => select(Number(indexInput.value)))
image.onload = () => {
  loaded = true
  cells.forEach((cell, index) => drawSprite(cell.querySelector('canvas')!, index))
  select(selected)
}

loadSheet(sheet)
