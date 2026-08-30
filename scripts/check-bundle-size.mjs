import { readdir, stat } from 'node:fs/promises'
import { join } from 'node:path'

const LIMIT = 500 * 1000
const assets = join(process.cwd(), 'dist', 'assets')
const files = await readdir(assets)
const chunks = await Promise.all(files.filter(file => file.endsWith('.js')).map(async file => ({ file, bytes: (await stat(join(assets, file))).size })))
const oversized = chunks.filter(chunk => chunk.bytes > LIMIT)
for (const chunk of chunks.sort((left, right) => right.bytes - left.bytes)) console.log(`${chunk.file} ${(chunk.bytes / 1000).toFixed(1)} kB`)
if (oversized.length) {
  console.error(`JavaScript chunks exceed the ${LIMIT / 1000} kB production limit: ${oversized.map(chunk => chunk.file).join(', ')}`)
  process.exitCode = 1
}
