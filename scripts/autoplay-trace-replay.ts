import { readFileSync } from 'node:fs'
import { replayAutoplayTrace } from '../src/autoplay-trace-replay'
import type { AutoplayTraceDocument } from '../src/autoplay-trace'

const path = process.argv[2]
if (!path) throw new Error('usage: tsx scripts/autoplay-trace-replay.ts TRACE.json')
const result = replayAutoplayTrace(JSON.parse(readFileSync(path, 'utf8')) as AutoplayTraceDocument)
process.stdout.write(`${JSON.stringify(result)}\n`, () => process.exit(result.valid ? 0 : 1))
