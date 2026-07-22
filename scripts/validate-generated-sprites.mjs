import { readFile } from 'node:fs/promises'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

const root = dirname(fileURLToPath(import.meta.url))
const assetDir = join(root, '..', 'src', 'assets', 'generated-sprites')
const manifest = JSON.parse(await readFile(join(assetDir, 'sprite-manifest.json'), 'utf8'))
const expectedItems = ['whip', 'machete', 'pickaxe', 'spear', 'tideSpear', 'sunblade', 'buckler', 'lantern', 'cap', 'mask', 'coat', 'mail', 'boots', 'featherboots', 'ward', 'sunseal', 'tonic', 'focusTonic', 'mapScroll', 'blinkRune', 'bombPack', 'ropeBundle', 'key', 'rock', 'fireJar', 'ember', 'mend', 'sight', 'root', 'waterScript', 'lull', 'blink', 'pull', 'gust', 'wardScript', 'gate']
const actorIds = manifest.sheets.flatMap(sheet => [...(sheet.actorRows ?? []), ...Object.keys(sheet.actorAliases ?? {})])
const itemIds = manifest.sheets.flatMap(sheet => [...(sheet.itemLayout ?? []), ...(sheet.animations ?? []).filter(animation => animation.id.startsWith('item.')).map(animation => animation.id.slice('item.'.length))]).filter(Boolean)
const missing = (expected, actual) => expected.filter(id => !actual.includes(id))
const duplicate = ids => ids.filter((id, index) => ids.indexOf(id) !== index)

if (duplicate(actorIds).length) throw new Error(`actor coverage failed: duplicate=${duplicate(actorIds)}`)
if (missing(expectedItems, itemIds).length || duplicate(itemIds).length) throw new Error(`item coverage failed: missing=${missing(expectedItems, itemIds)} duplicate=${duplicate(itemIds)}`)
if (manifest.terrainLayout.length !== 26 || duplicate(manifest.terrainLayout).length) throw new Error('terrain coverage failed')

for (const sheet of manifest.sheets) {
  if ((sheet.actorRows?.length ?? 0) > sheet.rows) throw new Error(`${sheet.file}: actor rows exceed sheet rows`)
  for (const [id, sourceId] of Object.entries(sheet.actorAliases ?? {})) if (!sheet.actorRows?.includes(sourceId)) throw new Error(`${sheet.file}: actor alias ${id} references missing row ${sourceId}`)
  const png = await readFile(join(assetDir, sheet.file))
  if (png.toString('ascii', 1, 4) !== 'PNG') throw new Error(`${sheet.file}: invalid PNG`)
  const width = png.readUInt32BE(16)
  const height = png.readUInt32BE(20)
  if (width !== sheet.columns * manifest.cellSize || height !== sheet.rows * manifest.cellSize) throw new Error(`${sheet.file}: expected ${sheet.columns * manifest.cellSize}x${sheet.rows * manifest.cellSize}, got ${width}x${height}`)
  if (sheet.id.startsWith('terrain-') && !Array.isArray(sheet.cellOffsets)) throw new Error(`${sheet.file}: missing cell offsets`)
  if (sheet.cellOffsets !== undefined) {
    const offsets = sheet.cellOffsets
    if (offsets.length !== sheet.columns * sheet.rows) throw new Error(`${sheet.file}: expected ${sheet.columns * sheet.rows} cell offsets`)
    if (offsets.some(offset => !Number.isInteger(offset?.x) || !Number.isInteger(offset?.y) || Math.abs(offset.x) > 6 || Math.abs(offset.y) > 6)) throw new Error(`${sheet.file}: invalid cell offset`)
  }
}

console.log(`validated ${manifest.sheets.length} sheets, ${actorIds.length} actors, ${itemIds.length} items, ${manifest.terrainLayout.length} terrain ids`)
