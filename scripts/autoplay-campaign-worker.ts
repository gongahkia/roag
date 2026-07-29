import { CAMPAIGN_AUTOPLAY_PROFILES, CAMPAIGN_AUTOPLAY_TURN_LIMIT, compactCampaignAutoplayRun } from '../src/autoplay-campaign'
import { runAutoplay } from '../src/autoplay-runner'
import { newSeededCampaignRun } from '../src/engine'

const seed = Number(process.env.CAMPAIGN_AUTOPLAY_SEED)
const profileId = process.env.CAMPAIGN_AUTOPLAY_PROFILE
const profile = CAMPAIGN_AUTOPLAY_PROFILES.find(candidate => candidate.id === profileId)
const turnLimit = Number(process.env.CAMPAIGN_AUTOPLAY_TURN_LIMIT ?? CAMPAIGN_AUTOPLAY_TURN_LIMIT)
if (!Number.isInteger(seed) || seed < 0) throw new Error(`invalid CAMPAIGN_AUTOPLAY_SEED: ${process.env.CAMPAIGN_AUTOPLAY_SEED}`)
if (!profile) throw new Error(`invalid CAMPAIGN_AUTOPLAY_PROFILE: ${profileId}`)
if (!Number.isInteger(turnLimit) || turnLimit < 1) throw new Error(`invalid CAMPAIGN_AUTOPLAY_TURN_LIMIT: ${process.env.CAMPAIGN_AUTOPLAY_TURN_LIMIT}`)
const report = runAutoplay(newSeededCampaignRun(seed), { mode: profile.mode, policy: profile.policy, turnLimit, captureTrace: true, traceLimit: process.env.CAMPAIGN_AUTOPLAY_TRACE === '1' ? undefined : 24 })
process.stdout.write(JSON.stringify(compactCampaignAutoplayRun(seed, profile, report)), () => process.exit(0))
