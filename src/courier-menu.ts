import type { CourierMenuEntry } from './types'

export const nextCourierSelection = (entries: readonly CourierMenuEntry[], selectedId: string | undefined, key: 'ArrowUp' | 'ArrowDown'): string | undefined => {
  if (!entries.length) return undefined
  const selectedIndex = Math.max(0, entries.findIndex(entry => entry.id === selectedId))
  const offset = key === 'ArrowUp' ? entries.length - 1 : 1
  return entries[(selectedIndex + offset) % entries.length]?.id
}
