import { sweepCampaignComposition } from '../src/campaign-composition'

const seed = Number(process.env.CAMPAIGN_COMPOSITION_SEED ?? 77123)
const report = sweepCampaignComposition(seed)
process.stdout.write(`${JSON.stringify({ seed, ...report }, null, 2)}\n`)
if (report.errors.length || report.repeatedRecipeEscalations.length || report.weakDiversity.length) process.exitCode = 1
