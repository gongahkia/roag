import { CAMPAIGN_AUTOPLAY_PROFILES, CAMPAIGN_AUTOPLAY_TURN_LIMIT, compactCampaignAutoplayRun } from '../src/autoplay-campaign'
import { runAutoplay } from '../src/autoplay-runner'
import { newRun } from '../src/engine'

const seed = Number(process.env.CAMPAIGN_AUTOPLAY_SEED)
const profileId = process.env.CAMPAIGN_AUTOPLAY_PROFILE
const profile = CAMPAIGN_AUTOPLAY_PROFILES.find(candidate => candidate.id === profileId)
if (!Number.isInteger(seed) || seed < 0) throw new Error(`invalid CAMPAIGN_AUTOPLAY_SEED: ${process.env.CAMPAIGN_AUTOPLAY_SEED}`)
if (!profile) throw new Error(`invalid CAMPAIGN_AUTOPLAY_PROFILE: ${profileId}`)
const report = runAutoplay(newRun(seed), { mode: profile.mode, policy: profile.policy, turnLimit: CAMPAIGN_AUTOPLAY_TURN_LIMIT, captureTrace: process.env.CAMPAIGN_AUTOPLAY_TRACE === '1', traceLimit: 24 })
process.stdout.write(JSON.stringify(compactCampaignAutoplayRun(seed, profile, report)))
