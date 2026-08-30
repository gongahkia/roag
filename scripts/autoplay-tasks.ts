import assert from 'node:assert/strict'
import { runAutoplayTaskSuite } from '../src/autoplay-task-runner'

const report = runAutoplayTaskSuite()
process.stdout.write(`${JSON.stringify(report, null, 2)}\n`)
assert.equal(report.passed, true, `autoplay task failures: ${report.results.filter(result => result.status !== 'passed').map(result => `${result.id}=${result.status}`).join(', ')}`)
