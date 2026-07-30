import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { createAutoplayScoreboard, formatAutoplayScoreboard, type AutoplayScoreboard } from '../src/autoplay-scoreboard'
import type { CampaignAutoplaySuite } from '../src/autoplay-campaign'

const path = process.argv.find(argument => !argument.startsWith('--') && argument !== process.argv[0] && argument !== process.argv[1])
if (!path) throw new Error('usage: autoplay-scoreboard <campaign-report.json> [--json]')
const report = JSON.parse(readFileSync(resolve(path), 'utf8')) as CampaignAutoplaySuite | { current: CampaignAutoplaySuite }
const suite = 'current' in report ? report.current : report
const scoreboard: AutoplayScoreboard = suite.scoreboard ?? createAutoplayScoreboard(suite)
process.stdout.write(`${process.argv.includes('--json') ? JSON.stringify(scoreboard, null, 2) : formatAutoplayScoreboard(scoreboard)}\n`)
