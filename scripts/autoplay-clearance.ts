import { execFileSync } from 'node:child_process'
import { mkdirSync, writeFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { findPlayableCampaignSeed, validateCampaignSeed, type CampaignValidation } from '../src/campaign-validation'

const count = Number(process.env.SEED_COUNT ?? 1000)
const startSeed = Number(process.env.START_SEED ?? 0)
const turnLimit = Number(process.env.TURNS ?? 3200)
const minimumRate = Number(process.env.MIN_RATE ?? .99)
const retryLimit = Number(process.env.RETRY_LIMIT ?? 1)
const outDir = resolve(process.env.OUT_DIR ?? 'clearance')
const reportIssue = process.env.REPORT_ISSUE !== '0'
const issueLabel = process.env.ISSUE_LABEL ?? 'autoplay-clearance'
const repository = process.env.GITHUB_REPOSITORY ?? (() => {
  try { return execFileSync('gh', ['repo', 'view', '--json', 'nameWithOwner', '--jq', '.nameWithOwner'], { encoding: 'utf8' }).trim() }
  catch { return undefined }
})()
if (!Number.isInteger(count) || count < 1) throw new Error(`invalid SEED_COUNT: ${process.env.SEED_COUNT}`)
if (!Number.isInteger(startSeed) || startSeed < 0) throw new Error(`invalid START_SEED: ${process.env.START_SEED}`)
if (!Number.isInteger(turnLimit) || turnLimit < 1) throw new Error(`invalid TURNS: ${process.env.TURNS}`)
if (!Number.isFinite(minimumRate) || minimumRate <= 0 || minimumRate > 1) throw new Error(`invalid MIN_RATE: ${process.env.MIN_RATE}`)
if (!Number.isInteger(retryLimit) || retryLimit < 1) throw new Error(`invalid RETRY_LIMIT: ${process.env.RETRY_LIMIT}`)

const commit = (): string => {
  try { return execFileSync('git', ['rev-parse', 'HEAD'], { encoding: 'utf8' }).trim() }
  catch { return 'unknown' }
}

const compactFailure = (result: CampaignValidation) => ({
  requestedSeed: result.requestedSeed,
  seed: result.seed,
  kind: result.kind,
  errors: result.errors,
  report: result.report ? {
    outcome: result.report.outcome,
    turns: result.report.turns,
    finalBiome: result.report.finalBiome,
    floor: result.report.floor,
    completedAreas: result.report.completedAreas,
    campaignComplete: result.report.campaignComplete,
    final: result.report.final,
    debug: result.report.debug,
    stall: result.report.stall,
    trace: result.report.trace.slice(-96),
    state: result.report.state
  } : undefined
})

const markdown = (report: ClearanceReport): string => {
  const rows = Object.entries(report.outcomes).map(([kind, value]) => `| ${kind} | ${value} |`).join('\n')
  const failures = report.failures.length
    ? report.failures.slice(0, 50).map(failure => `- seed ${failure.requestedSeed} → ${failure.seed}: ${failure.kind}`).join('\n')
    : '- none'
  return `# Jomon autoplay clearance\n\n- Commit: \`${report.commit}\`\n- Seeds: ${report.count} (${report.startSeed}–${report.startSeed + report.count - 1})\n- Full campaign clears: ${report.clears}/${report.count} (${(report.clearRate * 100).toFixed(2)}%)\n- Required rate: ${(report.minimumRate * 100).toFixed(2)}%\n- Status: ${report.passed ? 'PASS' : 'FAIL'}\n\n## Outcomes\n\n| Outcome | Count |\n| --- | ---: |\n${rows}\n\n## Failed seed corpus\n\n${failures}\n\nArtifacts: \`${report.artifactDir}\``
}

interface ClearanceReport {
  commit: string
  date: string
  count: number
  startSeed: number
  turnLimit: number
  retryLimit: number
  minimumRate: number
  clears: number
  clearRate: number
  passed: boolean
  outcomes: Record<string, number>
  failures: ReturnType<typeof compactFailure>[]
  artifactDir: string
}

const upsertIssue = (body: string, passed: boolean): void => {
  if (!repository) throw new Error('GITHUB_REPOSITORY is required when REPORT_ISSUE is enabled')
  const title = `Autoplay clearance: ${passed ? 'PASS' : 'FAIL'}`
  execFileSync('gh', ['label', 'create', issueLabel, '--repo', repository, '--color', 'BFD4F2', '--description', 'Local autoplay clearance report', '--force'], { stdio: 'ignore' })
  const existing = JSON.parse(execFileSync('gh', ['issue', 'list', '--repo', repository, '--state', 'open', '--label', issueLabel, '--limit', '100', '--json', 'number'], { encoding: 'utf8' })) as Array<{ number: number }>
  const bodyPath = resolve(outDir, 'issue.md')
  writeFileSync(bodyPath, body)
  if (existing[0]) execFileSync('gh', ['issue', 'edit', String(existing[0].number), '--repo', repository, '--title', title, '--body-file', bodyPath], { stdio: 'inherit' })
  else execFileSync('gh', ['issue', 'create', '--repo', repository, '--title', title, '--label', issueLabel, '--body-file', bodyPath], { stdio: 'inherit' })
}

mkdirSync(outDir, { recursive: true })
const failures: ReturnType<typeof compactFailure>[] = []
const outcomes: Record<string, number> = { clear: 0, 'generation-invalid': 0, dead: 0, stalled: 0, 'turn-limit': 0, error: 0 }
for (let offset = 0; offset < count; offset++) {
  const requestedSeed = startSeed + offset
  writeFileSync(resolve(outDir, 'progress.json'), JSON.stringify({ requestedSeed, completed: offset, count, status: 'running' }, null, 2))
  let result = retryLimit > 1 ? findPlayableCampaignSeed(requestedSeed, retryLimit, turnLimit) : validateCampaignSeed(requestedSeed, turnLimit)
  if (!result.accepted) result = { ...validateCampaignSeed(result.seed, turnLimit, { diagnostic: true }), requestedSeed }
  outcomes[result.kind] = (outcomes[result.kind] ?? 0) + 1
  if (!result.accepted) {
    const failure = compactFailure(result)
    failures.push(failure)
    writeFileSync(resolve(outDir, `seed-${requestedSeed}.json`), JSON.stringify(failure, null, 2))
  }
  writeFileSync(resolve(outDir, 'progress.json'), JSON.stringify({ requestedSeed, completed: offset + 1, count, status: result.kind }, null, 2))
  console.error(`clearance ${offset + 1}/${count}: ${result.kind}`)
}
const clears = outcomes.clear
const report: ClearanceReport = { commit: commit(), date: new Date().toISOString(), count, startSeed, turnLimit, retryLimit, minimumRate, clears, clearRate: clears / count, passed: clears / count >= minimumRate, outcomes, failures, artifactDir: outDir }
const body = markdown(report)
writeFileSync(resolve(outDir, 'report.json'), JSON.stringify(report, null, 2))
writeFileSync(resolve(outDir, 'report.md'), body)
if (reportIssue) upsertIssue(body, report.passed)
console.log(JSON.stringify(report, null, 2))
if (!report.passed) process.exitCode = 1
