import { readFileSync } from 'node:fs'

const path = new URL('../docs/traversal-content-brief.md', import.meta.url)
const brief = readFileSync(path, 'utf8')
const required = ['Stone Adze', 'Antler Prybar', 'Resin Fire Basket', 'Wooden Lever/Roller', 'Secret clues/rewards', 'Companion traversal motifs', 'Shortcuts', 'Jōmon-inspired fiction']
const sources = ['tnm.jp', 'jomon-japan.jp']
if (required.some(value => !brief.includes(value))) throw new Error('traversal content brief is missing a required family or fiction boundary')
if (sources.some(domain => !brief.includes(domain))) throw new Error('traversal content brief is missing an authoritative source link')
if (!brief.includes('Editorial checklist')) throw new Error('traversal content brief is missing editorial review checklist')
process.stdout.write('traversal content brief: valid\n')
