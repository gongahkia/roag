import { execFileSync, spawn } from 'node:child_process'
import { createHash } from 'node:crypto'
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs'
import { resolve } from 'node:path'
import type { CampaignValidation } from '../src/campaign-validation'

const count = Number(process.env.SEED_COUNT ?? 1000)
const startSeed = Number(process.env.START_SEED ?? 0)
const turnLimit = Number(process.env.TURNS ?? 3200)
const minimumRate = Number(process.env.MIN_RATE ?? .99)
const retryLimit = Number(process.env.RETRY_LIMIT ?? 1)
const outDir = resolve(process.env.OUT_DIR ?? 'clearance')
const resume = process.env.RESUME !== '0'
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
  return `# Jomon autoplay clearance\n\n- Commit: \`${report.commit}\`\n- Worktree: ${report.dirty ? 'dirty' : 'clean'}\n- Patch SHA-256: \`${report.patchSha256}\` ([worktree.patch](worktree.patch))\n- Status snapshot: [worktree-status.txt](worktree-status.txt)\n- Resume: ${report.resumed ? 'continued from prior checkpoint' : 'fresh run'}\n- Seeds: ${report.count} (${report.startSeed}–${report.startSeed + report.count - 1})\n- Progress: ${report.completed}/${report.count}\n- Full campaign clears: ${report.clears}/${report.completed || 1} (${(report.clearRate * 100).toFixed(2)}%)\n- Required rate: ${(report.minimumRate * 100).toFixed(2)}%\n- Status: ${report.status}\n\n## Outcomes\n\n| Outcome | Count |\n| --- | ---: |\n${rows}\n\n## Failed seed corpus\n\n${failures}\n\nArtifacts: \`${report.artifactDir}\``
}

interface ClearanceReport {
  commit: string
  dirty: boolean
  patchSha256: string
  resumed: boolean
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

interface ClearanceResult {
  requestedSeed: number
  seed: number
  kind: CampaignValidation['kind']
  accepted: boolean
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
const reportPath = resolve(outDir, 'report.json')
const resultsPath = resolve(outDir, 'results.ndjson')
const previousReport = existsSync(reportPath) ? JSON.parse(readFileSync(reportPath, 'utf8')) as Partial<ClearanceReport> : undefined
const priorResults: ClearanceResult[] = existsSync(resultsPath)
  ? readFileSync(resultsPath, 'utf8').split('\n').filter(Boolean).map((line, index) => {
    const result = JSON.parse(line) as ClearanceResult
    if (!Number.isInteger(result.requestedSeed) || !Number.isInteger(result.seed) || typeof result.kind !== 'string' || typeof result.accepted !== 'boolean') throw new Error(`invalid results.ndjson row ${index + 1}`)
    return result
  })
  : []
if (!resume && priorResults.length) throw new Error(`existing results in ${outDir}; use a new OUT_DIR or omit RESUME=0`)
if (priorResults.length) {
  const matchesRun = previousReport?.commit === runCommit && previousReport.patchSha256 === patchSha256 && previousReport.count === count && previousReport.startSeed === startSeed && previousReport.turnLimit === turnLimit && previousReport.retryLimit === retryLimit && previousReport.minimumRate === minimumRate
  if (!matchesRun) throw new Error(`cannot resume ${outDir}: run configuration or source provenance changed`)
  for (const [offset, result] of priorResults.entries()) if (result.requestedSeed !== startSeed + offset) throw new Error(`non-contiguous result at row ${offset + 1}`)
}
writeFileSync(resolve(outDir, 'worktree-status.txt'), worktreeStatus)
writeFileSync(resolve(outDir, 'worktree.patch'), worktreePatch)
const failures: ReturnType<typeof compactFailure>[] = priorResults.filter(result => !result.accepted).map(result => {
  const path = resolve(outDir, `seed-${result.requestedSeed}.json`)
  if (!existsSync(path)) throw new Error(`missing failure artifact for seed ${result.requestedSeed}`)
  return JSON.parse(readFileSync(path, 'utf8')) as ReturnType<typeof compactFailure>
})
const outcomes: Record<string, number> = { clear: 0, 'generation-invalid': 0, dead: 0, stalled: 0, 'turn-limit': 0, error: 0 }
for (const result of priorResults) outcomes[result.kind] = (outcomes[result.kind] ?? 0) + 1
let completed = priorResults.length
let interrupted = false
let activeChild: ReturnType<typeof spawn> | undefined
const writeAudit = (status: ClearanceReport['status']): ClearanceReport => {
  const clears = outcomes.clear
  const report: ClearanceReport = { commit: runCommit, dirty: runDirty, patchSha256, resumed: priorResults.length > 0, date: new Date().toISOString(), status, count, completed, startSeed, turnLimit, retryLimit, minimumRate, clears, clearRate: completed ? clears / completed : 0, passed: completed === count && clears / count >= minimumRate, outcomes, failures, artifactDir: outDir }
  writeFileSync(resolve(outDir, 'report.json'), JSON.stringify(report, null, 2))
  writeFileSync(resolve(outDir, 'report.md'), markdown(report))
  return report
}
const runSeed = (requestedSeed: number): Promise<CampaignValidation> => new Promise((resolveResult, reject) => {
  const child = spawn(resolve('node_modules/.bin/vite-node'), ['--script', resolve('scripts/autoplay-clearance-worker.ts')], {
    env: { ...process.env, CLEARANCE_WORKER_SEED: String(requestedSeed), CLEARANCE_WORKER_TURNS: String(turnLimit), CLEARANCE_WORKER_RETRY_LIMIT: String(retryLimit) },
    stdio: ['ignore', 'pipe', 'pipe']
  })
  activeChild = child
  let output = ''
  let errors = ''
  child.stdout?.on('data', chunk => { output += chunk })
  child.stderr?.on('data', chunk => { errors += chunk })
  child.once('error', error => { if (activeChild === child) activeChild = undefined; reject(error) })
  child.once('close', (code, signal) => {
    if (activeChild === child) activeChild = undefined
    if (interrupted) { reject(new Error('interrupted')); return }
    if (code !== 0) { reject(new Error(`seed ${requestedSeed} worker exited ${code ?? signal}: ${errors.trim()}`)); return }
    try {
      const result = JSON.parse(output) as CampaignValidation
      if (result.requestedSeed !== requestedSeed || !Number.isInteger(result.seed) || typeof result.kind !== 'string' || typeof result.accepted !== 'boolean' || !Array.isArray(result.errors)) throw new Error('invalid worker result')
      resolveResult(result)
    } catch (error) { reject(error instanceof Error ? error : new Error(String(error))) }
  })
})
const interrupt = () => { interrupted = true; activeChild?.kill('SIGTERM') }
process.once('SIGINT', interrupt)
process.once('SIGTERM', interrupt)
writeAudit('running')
for (let offset = completed; offset < count; offset++) {
  if (interrupted) break
  const requestedSeed = startSeed + offset
  writeFileSync(resolve(outDir, 'progress.json'), JSON.stringify({ requestedSeed, completed: offset, count, status: 'running' }, null, 2))
  let result: CampaignValidation
  try { result = await runSeed(requestedSeed) }
  catch (error) {
    if (interrupted) break
    result = { requestedSeed, seed: requestedSeed, kind: 'error', accepted: false, errors: [error instanceof Error ? error.message : String(error)] }
  }
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
