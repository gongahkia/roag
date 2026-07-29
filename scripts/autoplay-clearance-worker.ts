import { findPlayableCampaignSeed, validateCampaignSeed } from '../src/campaign-validation'

const requestedSeed = Number(process.env.CLEARANCE_WORKER_SEED)
const turnLimit = Number(process.env.CLEARANCE_WORKER_TURNS)
const retryLimit = Number(process.env.CLEARANCE_WORKER_RETRY_LIMIT)
if (!Number.isInteger(requestedSeed) || requestedSeed < 0) throw new Error(`invalid CLEARANCE_WORKER_SEED: ${process.env.CLEARANCE_WORKER_SEED}`)
if (!Number.isInteger(turnLimit) || turnLimit < 1) throw new Error(`invalid CLEARANCE_WORKER_TURNS: ${process.env.CLEARANCE_WORKER_TURNS}`)
if (!Number.isInteger(retryLimit) || retryLimit < 1) throw new Error(`invalid CLEARANCE_WORKER_RETRY_LIMIT: ${process.env.CLEARANCE_WORKER_RETRY_LIMIT}`)

let result = retryLimit > 1 ? findPlayableCampaignSeed(requestedSeed, retryLimit, turnLimit) : validateCampaignSeed(requestedSeed, turnLimit)
if (!result.accepted) result = { ...validateCampaignSeed(result.seed, turnLimit, { diagnostic: true }), requestedSeed }
process.stdout.write(JSON.stringify(result))
