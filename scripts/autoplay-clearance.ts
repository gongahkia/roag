import { execFileSync } from 'node:child_process'
import { createHash } from 'node:crypto'
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
  return `# Jomon autoplay clearance\n\n- Commit: \`${report.commit}\`\n- Worktree: ${report.dirty ? 'dirty' : 'clean'}\n- Patch SHA-256: \`${report.patchSha256}\` ([worktree.patch](worktree.patch))\n- Status snapshot: [worktree-status.txt](worktree-status.txt)\n- Seeds: ${report.count} (${report.startSeed}–${report.startSeed + report.count - 1})\n- Progress: ${report.completed}/${report.count}\n- Full campaign clears: ${report.clears}/${report.completed || 1} (${(report.clearRate * 100).toFixed(2)}%)\n- Required rate: ${(report.minimumRate * 100).toFixed(2)}%\n- Status: ${report.status}\n\n## Outcomes\n\n| Outcome | Count |\n| --- | ---: |\n${rows}\n\n## Failed seed corpus\n\n${failures}\n\nArtifacts: \`${report.artifactDir}\``
}

interface ClearanceReport {
  commit: string
  dirty: boolean
  patchSha256: string
  date: string
  status: 'running' | 'interrupted' | 'complete'
  count: number
  completed: number
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
const runCommit = commit()
const runDirty = (() => {
  try { return Boolean(execFileSync('git', ['status', '--porcelain'], { encoding: 'utf8' }).trim()) }
  catch { return true }
})()
const worktreeStatus = (() => {
  try { return execFileSync('git', ['status', '--porcelain=v1'], { encoding: 'utf8' }) }
  catch { return 'unavailable\n' }
})()
const worktreePatch = (() => {
  try { return execFileSync('git', ['diff', '--binary', 'HEAD'], { encoding: 'utf8' }) }
  catch { return '' }
})()
const patchSha256 = createHash('sha256').update(worktreePatch).digest('hex')
writeFileSync(resolve(outDir, 'worktree-status.txt'), worktreeStatus)
writeFileSync(resolve(outDir, 'worktree.patch'), worktreePatch)
const failures: ReturnType<typeof compactFailure>[] = []
const outcomes: Record<string, number> = { clear: 0, 'generation-invalid': 0, dead: 0, stalled: 0, 'turn-limit': 0, error: 0 }
let completed = 0
let interrupted = false
const writeAudit = (status: ClearanceReport['status']): ClearanceReport => {
  const clears = outcomes.clear
  const report: ClearanceReport = { commit: runCommit, dirty: runDirty, patchSha256, date: new Date().toISOString(), status, count, completed, startSeed, turnLimit, retryLimit, minimumRate, clears, clearRate: completed ? clears / completed : 0, passed: completed === count && clears / count >= minimumRate, outcomes, failures, artifactDir: outDir }
  writeFileSync(resolve(outDir, 'report.json'), JSON.stringify(report, null, 2))
  writeFileSync(resolve(outDir, 'report.md'), markdown(report))
  return report
}
process.once('SIGINT', () => { interrupted = true })
writeAudit('running')
for (let offset = 0; offset < count; offset++) {
  const requestedSeed = startSeed + offset
  writeFileSync(resolve(outDir, 'progress.json'), JSON.stringify({ requestedSeed, completed: offset, count, status: 'running' }, null, 2))
  let result = retryLimit > 1 ? findPlayableCampaignSeed(requestedSeed, retryLimit, turnLimit) : validateCampaignSeed(requestedSeed, turnLimit)
  if (!result.accepted) result = { ...validateCampaignSeed(result.seed, turnLimit, { diagnostic: true }), requestedSeed }
  outcomes[result.kind] = (outcomes[result.kind] ?? 0) + 1
  writeFileSync(resolve(outDir, 'results.ndjson'), `${JSON.stringify({ requestedSeed, seed: result.seed, kind: result.kind, accepted: result.accepted })}\n`, { flag: 'a' })
  if (!result.accepted) {
    const failure = compactFailure(result)
    failures.push(failure)
    writeFileSync(resolve(outDir, `seed-${requestedSeed}.json`), JSON.stringify(failure, null, 2))
  }
  completed = offset + 1
  writeFileSync(resolve(outDir, 'progress.json'), JSON.stringify({ requestedSeed, completed, count, status: result.kind }, null, 2))
  writeAudit('running')
  console.error(`clearance ${offset + 1}/${count}: ${result.kind}`)
  if (interrupted) break
}
const report = writeAudit(interrupted ? 'interrupted' : 'complete')
const body = markdown(report)
if (!interrupted && reportIssue) upsertIssue(body, report.passed)
console.log(JSON.stringify(report, null, 2))
if (interrupted) process.exitCode = 130
else if (!report.passed) process.exitCode = 1
