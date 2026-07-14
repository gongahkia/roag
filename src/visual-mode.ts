export type VisualMode = 'ascii' | 'sprites' | 'runes'

const modes: readonly VisualMode[] = ['ascii', 'sprites', 'runes']

export const normalizeVisualMode = (value: string | null): VisualMode => value === 'sprites' || value === 'runes' ? value : 'ascii'
export const nextVisualMode = (mode: VisualMode): VisualMode => modes[(modes.indexOf(mode) + 1) % modes.length]
export const visualModeLabel = (mode: VisualMode): string => mode === 'ascii' ? 'sprites' : mode === 'sprites' ? 'runes' : 'ascii'
