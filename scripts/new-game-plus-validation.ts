import { mkdir, writeFile } from 'node:fs/promises'
import { dirname, resolve } from 'node:path'
import { NEW_GAME_PLUS_VALIDATION_MODES, type NewGamePlusValidationMode, validateNewGamePlusCorpus } from '../src/new-game-plus-validation'

const mode = process.env.NEW_GAME_PLUS_VALIDATION_MODE ?? 'smoke'
if (!NEW_GAME_PLUS_VALIDATION_MODES.includes(mode as NewGamePlusValidationMode)) throw new Error(`invalid NEW_GAME_PLUS_VALIDATION_MODE: ${mode}`)
const report = validateNewGamePlusCorpus(mode as NewGamePlusValidationMode)
const requestedPath = process.env.NEW_GAME_PLUS_VALIDATION_REPORT_PATH
if (requestedPath) {
  const output = resolve(requestedPath)
  await mkdir(dirname(output), { recursive: true })
  await writeFile(output, `${JSON.stringify(report, null, 2)}\n`)
}
console.log(JSON.stringify(report, null, 2))
if (!report.accepted) process.exitCode = 1
