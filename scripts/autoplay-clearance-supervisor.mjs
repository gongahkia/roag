import { spawn } from 'node:child_process'
import { existsSync, readFileSync, writeFileSync } from 'node:fs'
import { resolve } from 'node:path'

const outDir = resolve(process.env.OUT_DIR ?? 'clearance')
const updateInterruptedAudit = () => {
  const reportPath = resolve(outDir, 'report.json')
  if (!existsSync(reportPath)) return
  const report = JSON.parse(readFileSync(reportPath, 'utf8'))
  if (report.status === 'complete') return
  report.status = 'interrupted'
  report.passed = false
  report.date = new Date().toISOString()
  writeFileSync(reportPath, JSON.stringify(report, null, 2))
  const markdownPath = resolve(outDir, 'report.md')
  if (existsSync(markdownPath)) writeFileSync(markdownPath, readFileSync(markdownPath, 'utf8').replace('- Status: running', '- Status: interrupted'))
  const progressPath = resolve(outDir, 'progress.json')
  if (existsSync(progressPath)) {
    const progress = JSON.parse(readFileSync(progressPath, 'utf8'))
    progress.status = 'interrupted'
    writeFileSync(progressPath, JSON.stringify(progress, null, 2))
  }
}

const child = spawn(resolve('node_modules/.bin/vite-node'), ['--script', resolve('scripts/autoplay-clearance.ts')], { cwd: process.cwd(), detached: true, env: process.env, stdio: 'inherit' })
let interrupted = false
const interrupt = () => {
  if (interrupted) return
  interrupted = true
  try { process.kill(-child.pid, 'SIGKILL') }
  catch { child.kill('SIGKILL') }
}
process.once('SIGINT', interrupt)
process.once('SIGTERM', interrupt)
child.once('error', error => { console.error(error); process.exitCode = 1 })
child.once('close', code => {
  if (interrupted) { updateInterruptedAudit(); process.exitCode = 130 }
  else process.exitCode = code ?? 1
})
