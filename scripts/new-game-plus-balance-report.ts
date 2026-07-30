import { mkdir, writeFile } from 'node:fs/promises'
import { dirname, resolve } from 'node:path'
import { AUTOPLAY_MAX_TURNS } from '../src/autoplay'
import { newGamePlusBalanceReport } from '../src/new-game-plus-balance'
import { NEW_GAME_PLUS_LIFECYCLE_MODES, type NewGamePlusLifecycleMode } from '../src/new-game-plus-lifecycle'

const mode = process.env.NEW_GAME_PLUS_BALANCE_MODE ?? 'full'
if (!NEW_GAME_PLUS_LIFECYCLE_MODES.includes(mode as NewGamePlusLifecycleMode)) throw new Error(`invalid NEW_GAME_PLUS_BALANCE_MODE: ${mode}`)
const turnLimit = Number(process.env.NEW_GAME_PLUS_BALANCE_TURN_LIMIT ?? AUTOPLAY_MAX_TURNS)
const report = newGamePlusBalanceReport(mode as NewGamePlusLifecycleMode, turnLimit)
const requestedPath = process.env.NEW_GAME_PLUS_BALANCE_REPORT_PATH
if (requestedPath) {
  const output = resolve(requestedPath)
  await mkdir(dirname(output), { recursive: true })
  await writeFile(output, `${JSON.stringify(report, null, 2)}\n`)
}
console.log(JSON.stringify(report, null, 2))
