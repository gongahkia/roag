export type VisualMode = 'ascii' | 'runes'

const modes: readonly VisualMode[] = ['ascii', 'runes']

export const normalizeVisualMode = (value: string | null): VisualMode => value === 'runes' ? 'runes' : 'ascii'
export const nextVisualMode = (mode: VisualMode): VisualMode => modes[(modes.indexOf(mode) + 1) % modes.length]
export const visualModeLabel = (mode: VisualMode): string => mode === 'ascii' ? 'runes' : 'ascii'
