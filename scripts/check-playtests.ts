import { readFile } from 'node:fs/promises'
import { resolve } from 'node:path'
import { summarizePlaytestRecords } from '../src/playtest-records'

const requireComplete = process.argv.includes('--require-complete') || process.env.PLAYTEST_REQUIRE_COMPLETE === '1'
const path = resolve(process.env.PLAYTEST_RECORDS ?? 'docs/playtests/records.json')
const document: unknown = JSON.parse(await readFile(path, 'utf8'))
const summary = summarizePlaytestRecords(document, requireComplete)
console.log(JSON.stringify({ path, requireComplete, ...summary }, null, 2))
if (!summary.valid || (requireComplete && !summary.complete)) process.exitCode = 1
