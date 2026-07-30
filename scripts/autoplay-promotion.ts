import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { evaluateAutoplayPromotion } from '../src/autoplay-promotion'

const path = process.argv[2]
if (!path) throw new Error('usage: autoplay-promotion <held-out-ablation.json>')
const input = JSON.parse(readFileSync(resolve(path), 'utf8')) as { heuristic: Parameters<typeof evaluateAutoplayPromotion>[0]; candidate: Parameters<typeof evaluateAutoplayPromotion>[1]; visibleInput: unknown }
const report = evaluateAutoplayPromotion(input.heuristic, input.candidate, input.visibleInput)
process.stdout.write(`${JSON.stringify(report, null, 2)}\n${report.verdict}: ${report.reason}\n`)
