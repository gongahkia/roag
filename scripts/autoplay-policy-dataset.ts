import { writeFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { autoplayPolicyDatasetRecords } from '../src/autoplay-policy-dataset'
import { CAMPAIGN_AUTOPLAY_PROFILES, campaignAutoplayEntries } from '../src/autoplay-campaign'
import { autoplayHeuristicProfile } from '../src/autoplay-heuristics'
import { runAutoplay } from '../src/autoplay-runner'
import { newSeededCampaignRun } from '../src/engine'
import type { AutoplaySeedCorpusPartition } from '../src/autoplay-seed-corpus'

const partitionFlag = process.argv.indexOf('--partition')
const outputFlag = process.argv.indexOf('--output')
const seedFlag = process.argv.indexOf('--seed')
if (partitionFlag < 0 || !process.argv[partitionFlag + 1] || process.argv[partitionFlag + 1].startsWith('--')) throw new Error('missing --partition value')
if (outputFlag < 0 || !process.argv[outputFlag + 1] || process.argv[outputFlag + 1].startsWith('--')) throw new Error('missing --output value')
if (seedFlag >= 0 && (!process.argv[seedFlag + 1] || process.argv[seedFlag + 1].startsWith('--'))) throw new Error('missing --seed value')
const partition = process.argv[partitionFlag + 1] as AutoplaySeedCorpusPartition
const requestedSeed = seedFlag >= 0 ? Number(process.argv[seedFlag + 1]) : undefined
if (requestedSeed !== undefined && (!Number.isSafeInteger(requestedSeed) || requestedSeed < 0)) throw new Error(`invalid --seed value: ${process.argv[seedFlag + 1]}`)
const entries = campaignAutoplayEntries(partition).filter(entry => requestedSeed === undefined || entry.seed === requestedSeed)
if (!entries.length) throw new Error(`seed ${requestedSeed} is not in the ${partition} autoplay corpus`)
const heuristicProfile = autoplayHeuristicProfile(process.env.HEURISTIC_PROFILE)
const documents = entries.flatMap(entry => CAMPAIGN_AUTOPLAY_PROFILES.map(profile => {
  const document = runAutoplay(newSeededCampaignRun(entry.seed), { mode: profile.mode, policy: profile.policy, heuristicProfile, turnLimit: entry.turnBudget, captureTrace: true }).traceDocument
  if (!document) throw new Error(`missing trace document for ${entry.seed}/${profile.id}`)
  return document
}))
const output = resolve(process.argv[outputFlag + 1]!)
const records = autoplayPolicyDatasetRecords(documents, partition)
const jsonl = records.length ? `${records.map(record => JSON.stringify(record)).join('\n')}\n` : ''
writeFileSync(output, jsonl)
process.stderr.write(`autoplay policy dataset ${partition}: ${records.length} records -> ${output}\n`)
