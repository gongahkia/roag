import { existsSync, readFileSync, writeFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { campaignAutoplayDelta, runCampaignAutoplaySuite, type CampaignAutoplaySuite } from '../src/autoplay-campaign'

const baselinePath = resolve('scripts/autoplay-campaign-baseline.json')
const updateBaseline = process.argv.includes('--update-baseline')
const baseline = existsSync(baselinePath) ? JSON.parse(readFileSync(baselinePath, 'utf8')) as CampaignAutoplaySuite : undefined
const current = runCampaignAutoplaySuite({ onRun: (run, completed, total) => console.error(`autoplay campaign ${completed}/${total}: ${run.seed}/${run.profile} ${run.outcome} at ${run.turns}`) })
if (updateBaseline) writeFileSync(baselinePath, `${JSON.stringify(current, null, 2)}\n`)
const comparison = baseline ? campaignAutoplayDelta(current, baseline) : undefined
console.log(JSON.stringify({ current, baseline: baseline ? { summary: baseline.summary, comparison } : undefined, baselineUpdated: updateBaseline }, null, 2))
