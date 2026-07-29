import type { AutoplayDiagnostic, AutoplayTerminal } from './types'

export const AUTOPLAY_LOG_KEY = 'jomon-autoplay-log'
const SUMMARY_LIMIT = 100
const TRACE_LIMIT = 600
type StorageLike = Pick<Storage, 'getItem' | 'setItem'>

const store = (): StorageLike | undefined => { try { return typeof localStorage === 'undefined' ? undefined : localStorage } catch { return undefined } }
const validOutcome = (value: unknown): value is AutoplayTerminal => ['complete', 'dead', 'stalled', 'turn-limit', 'manual'].includes(String(value))

export const loadAutoplayDiagnostics = (storage = store()): AutoplayDiagnostic[] => {
  if (!storage) return []
  try {
    const value: unknown = JSON.parse(storage.getItem(AUTOPLAY_LOG_KEY) ?? '[]')
    if (!Array.isArray(value)) return []
    return value.filter((entry): entry is AutoplayDiagnostic => Boolean(entry && typeof entry === 'object' && validOutcome((entry as { outcome?: unknown }).outcome))).slice(0, SUMMARY_LIMIT)
  } catch { return [] }
}

export const latestAutoplayDiagnostic = (storage = store()): AutoplayDiagnostic | undefined => loadAutoplayDiagnostics(storage)[0]

export const saveAutoplayDiagnostic = (entry: AutoplayDiagnostic, storage = store()): void => {
  if (!storage) return
  const trace = entry.trace.slice(-TRACE_LIMIT)
  const current = loadAutoplayDiagnostics(storage)
  const representative = entry.outcome === 'complete' && !current.some(existing => existing.outcome === 'complete' && existing.mode === entry.mode && existing.policy === entry.policy)
  const compact = entry.outcome === 'complete' && !representative ? { ...entry, trace: trace.slice(-40) } : { ...entry, trace }
  try { storage.setItem(AUTOPLAY_LOG_KEY, JSON.stringify([compact, ...current].slice(0, SUMMARY_LIMIT))) } catch { }
}
