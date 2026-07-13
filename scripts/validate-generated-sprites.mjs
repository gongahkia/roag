import { readFile } from 'node:fs/promises'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

const root = dirname(fileURLToPath(import.meta.url))
const assetDir = join(root, '..', 'src', 'assets', 'generated-sprites')
const manifest = JSON.parse(await readFile(join(assetDir, 'sprite-manifest.json'), 'utf8'))
const expectedActors = ['rat', 'mole', 'sapper', 'beetle', 'driller', 'railguard', 'fusewarden', 'foreman', 'thornling', 'boar', 'spitter', 'wisp', 'frog', 'vinebinder', 'marshskater', 'webweaver', 'heartwood', 'crawler', 'magma', 'echo', 'seer', 'slug', 'cinderimp', 'fumeeel', 'gloomseer', 'crystalpuller', 'geode', 'scarab', 'sentinel', 'oracle', 'shade', 'cultist', 'wardacolyte', 'dartadept', 'lockkeeper', 'ritualist', 'regent']
const expectedItems = ['whip', 'machete', 'pickaxe', 'spear', 'sunblade', 'buckler', 'lantern', 'cap', 'mask', 'coat', 'mail', 'boots', 'featherboots', 'ward', 'sunseal', 'tonic', 'focusTonic', 'mapScroll', 'blinkRune', 'bombPack', 'ropeBundle', 'key', 'rock', 'fireJar', 'ember', 'mend', 'sight', 'root', 'waterScript', 'lull', 'blink', 'pull', 'gust', 'wardScript', 'gate']
const actorIds = manifest.sheets.flatMap(sheet => sheet.actorRows ?? [])
const itemIds = manifest.sheets.flatMap(sheet => sheet.itemLayout ?? []).filter(Boolean)
const missing = (expected, actual) => expected.filter(id => !actual.includes(id))
const duplicate = ids => ids.filter((id, index) => ids.indexOf(id) !== index)

if (missing(expectedActors, actorIds).length || duplicate(actorIds).length) throw new Error(`actor coverage failed: missing=${missing(expectedActors, actorIds)} duplicate=${duplicate(actorIds)}`)
if (missing(expectedItems, itemIds).length || duplicate(itemIds).length) throw new Error(`item coverage failed: missing=${missing(expectedItems, itemIds)} duplicate=${duplicate(itemIds)}`)
if (manifest.terrainLayout.length !== 26 || duplicate(manifest.terrainLayout).length) throw new Error('terrain coverage failed')

for (const sheet of manifest.sheets) {
  const png = await readFile(join(assetDir, sheet.file))
  if (png.toString('ascii', 1, 4) !== 'PNG') throw new Error(`${sheet.file}: invalid PNG`)
  const width = png.readUInt32BE(16)
  const height = png.readUInt32BE(20)
  if (width !== sheet.columns * manifest.cellSize || height !== sheet.rows * manifest.cellSize) throw new Error(`${sheet.file}: expected ${sheet.columns * manifest.cellSize}x${sheet.rows * manifest.cellSize}, got ${width}x${height}`)
}

console.log(`validated ${manifest.sheets.length} sheets, ${actorIds.length} actors, ${itemIds.length} items, ${manifest.terrainLayout.length} terrain ids`)
