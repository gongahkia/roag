import { mkdir, writeFile } from 'node:fs/promises'
import { dirname, resolve } from 'node:path'
import { NEW_GAME_PLUS_LIFECYCLE_MODES, type NewGamePlusLifecycleMode, validateNewGamePlusLifecycleCorpus } from '../src/new-game-plus-lifecycle'

const mode = process.env.NEW_GAME_PLUS_LIFECYCLE_MODE ?? 'smoke'
if (!NEW_GAME_PLUS_LIFECYCLE_MODES.includes(mode as NewGamePlusLifecycleMode)) throw new Error(`invalid NEW_GAME_PLUS_LIFECYCLE_MODE: ${mode}`)
const report = validateNewGamePlusLifecycleCorpus(mode as NewGamePlusLifecycleMode)
const requestedPath = process.env.NEW_GAME_PLUS_LIFECYCLE_REPORT_PATH
if (requestedPath) {
  const output = resolve(requestedPath)
  await mkdir(dirname(output), { recursive: true })
  await writeFile(output, `${JSON.stringify(report, null, 2)}\n`)
}
console.log(JSON.stringify(report, null, 2))
if (!report.accepted) process.exitCode = 1
