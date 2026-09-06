import { GENERATION_CONFIG_PRESETS, WORLD_GENERATION_ADVANCED_SETTING_NAMES, WORLD_GENERATION_ADVANCED_SETTING_OPTIONS, type WorldGenerationAdvancedSettings, type WorldGenerationPreset } from './generation-config'
import { INITIAL_WORLD_GENERATION_STAGES, type InitialWorldGenerationProgress } from './initial-world'
import { MANAGEMENT_SIDEBAR_SECTION_IDS, MANAGEMENT_SIDEBAR_SECTION_LABELS, MANAGEMENT_SIDEBAR_SOURCE_LABELS, createManagementSidebarModel, managementSidebarAccessibleSummary, type ManagementSidebarFact, type ManagementSidebarModel } from './management-sidebar'
import { JOMON_ASCII_GLYPH_CATALOG } from './ascii-glyphs'
import { initialHouseholdActiveCrew } from './initial-household'
import { JOMON_NON_COLOR_STATE_CUES, JOMON_PALETTE } from './palette'
import { CREATION_SETTINGS_PROFILE_LIMIT, CREATION_SETTINGS_PROFILE_NAME_LIMIT, defaultCreationSettings, normalizeCreationSeed, resolveCreationSettings, type CreationSettings, type CreationSettingsProfile, type CreationSettingsRecord } from './settings'
import { MedievalWorldRepository } from './storage'
import { MutableWorldSession } from './session'
import { cancelTerminalPrompt, createJomonDeckContextualPrompt, createTerminalPresentationModel, type TerminalMapLegend, type TerminalMaterializedMap, type TerminalPrompt } from './terminal-presentation'
import { TERMINAL_CONTROL_IDS, captureTerminalControlBinding, createTerminalCommandHelpModel, createTerminalControlsEditorModel, cycleTerminalControlSelection, defaultTerminalControlPreferences, resetAllTerminalControls, resetTerminalControl, resolveTerminalWorldCommand, type TerminalControlId, type TerminalControlPreferences, type TerminalMovementDirection } from './terminal-controls'
import { EXPEDITION_LOADOUTS, EXPEDITION_SUPPORTS, HEARTHFORD_CONTACT, HEARTHFORD_CONTACT_COORDINATE, HEARTHFORD_GANGPLANK, HEARTHFORD_MAP_ROWS, expeditionPressure, hearthfordGlyphAt, type ExpeditionLoadoutId, type ExpeditionSupportId } from './expedition'
import type { FoundationWorld, MedievalRoute, WorldChronicle, WorldIndex } from './types'
import { acceptSettlementTradeContract, actInHearthfordExpedition, chooseHearthfordExpeditionLoadout, chooseHearthfordExpeditionSupport, chronicleExport, chooseInitialCourier, createFoundationWorld, decideHearthfordExpeditionObjective, deliverHearthfordExpeditionSealCord, departForHearthfordExpedition, deliverSettlementTradeContract, fitHearthfordMillIronwork, moveFoundationWorldCourier, moveHearthfordExpeditionCourier, recordVesselStationReadout, refuseSettlementTradeContract, returnFromHearthfordExpedition, switchTavernCourier, takeHearthfordMillLeaseCredit } from './world'

type PersistenceState = 'loading' | 'saved' | 'error'
type SettingsPage = 'basic' | 'advanced'
type ResultPage = 'summary' | 'configuration' | 'provenance'
type WorldOverlay = 'none' | 'contextual-prompt' | 'command-help' | 'controls-editor' | 'expedition-preparation' | 'expedition-departure' | 'expedition-contact'
type TerminalInteractionOutcome =
  | { kind: 'movement-completed'; direction: TerminalMovementDirection; column: number; row: number }
  | { kind: 'movement-blocked'; direction: TerminalMovementDirection; collision: string }
  | { kind: 'prompt-cancelled' }
  | { kind: 'prompt-option-disabled'; reason: string }
  | { kind: 'station-readout-recorded'; label: string }
  | { kind: 'courier-switched'; fromName: string; toName: string }
  | { kind: 'settlement-trade-recorded'; outcome: 'accepted' | 'refused' }
  | { kind: 'settlement-trade-delivered' }
  | { kind: 'hearthford-worksite-resolved'; outcome: 'ironwork-fitted' | 'lease-credit' }
  | { kind: 'overlay-dismissed'; overlay: Exclude<WorldOverlay, 'none'> }
  | { kind: 'controls-capture-cancelled' }
  | { kind: 'controls-binding-saved'; controlId: TerminalControlId }
  | { kind: 'controls-binding-rejected'; controlId: TerminalControlId; code: string }
  | { kind: 'controls-reset-current'; controlId: TerminalControlId }
  | { kind: 'controls-reset-all' }
  | { kind: 'expedition-event'; text: string; warning?: true }

const palette = JOMON_PALETTE
const cues = JOMON_NON_COLOR_STATE_CUES
const width = 800
const height = 630
const left = 38
const lineHeight = 22
const contentWidth = width - left * 2
/** Creep's local outline build preserves the original bitmap glyph grid in Chromium Canvas 2D. */
const terminalFont = '16px Creep, ui-monospace, monospace'
const presets = Object.keys(GENERATION_CONFIG_PRESETS) as WorldGenerationPreset[]
const diagnosticFirstLine = 21
const diagnosticLastLine = 23

const errorMessage = (error: unknown): string => error instanceof Error ? error.message : 'Unknown local storage error'
const uppercase = (value: string | number): string => String(value).replaceAll('-', ' ').toUpperCase()
const settingLabel = (setting: keyof WorldGenerationAdvancedSettings): string => uppercase(setting.replace(/([A-Z])/gu, ' $1'))
const shortDigest = (value: string): string => value.length > 32 ? `${value.slice(0, 29)}...` : value

/** The canvas consumes the household-owned projection without retaining a second roster. */
export const initialCourierSelectionCandidates = (world: FoundationWorld) => initialHouseholdActiveCrew(world.crew)

const row = (context: CanvasRenderingContext2D, line: number, text: string, color: string = palette.bodyText, x: number = left): void => {
  context.fillStyle = color
  context.fillText(text, x, 78 + line * lineHeight)
}

export const wrapMedievalCanvasText = (context: Pick<CanvasRenderingContext2D, 'measureText'>, text: string, maximumWidth: number = contentWidth): readonly string[] => {
  const words = text.trim().split(/\s+/u).filter(Boolean)
  if (!words.length) return ['']
  const splitLongWord = (word: string): readonly string[] => {
    const pieces: string[] = []
    let piece = ''
    for (const character of word) {
      if (!piece || context.measureText(`${piece}${character}`).width <= maximumWidth) piece += character
      else { pieces.push(piece); piece = character }
    }
    if (piece) pieces.push(piece)
    return pieces
  }
  const lines: string[] = []
  let current = ''
  for (const word of words) {
    const next = current ? `${current} ${word}` : word
    if (context.measureText(next).width <= maximumWidth) { current = next; continue }
    if (current) { lines.push(current); current = '' }
    if (context.measureText(word).width <= maximumWidth) { current = word; continue }
    const pieces = splitLongWord(word)
    lines.push(...pieces.slice(0, -1))
    current = pieces.at(-1) ?? ''
  }
  if (current) lines.push(current)
  return lines
}

const wrappedRows = (context: CanvasRenderingContext2D, line: number, text: string, color: string = palette.bodyText, x: number = left, maximumWidth: number = contentWidth): number => {
  const lines = wrapMedievalCanvasText(context, text, maximumWidth)
  lines.forEach((wrapped, index) => row(context, line + index, wrapped, color, x))
  return line + lines.length
}

const shortenedToFit = (context: Pick<CanvasRenderingContext2D, 'measureText'>, text: string, maximumWidth: number = contentWidth): string => {
  if (context.measureText(text).width <= maximumWidth) return text
  let shortened = text
  while (shortened && context.measureText(`${shortened}...`).width > maximumWidth) shortened = shortened.slice(0, -1)
  return `${shortened}...`
}

/** Error text has a fixed in-panel budget, so a stale local record cannot paint outside the canvas. */
export const renderBoundedMedievalCanvasRows = (
  context: CanvasRenderingContext2D,
  line: number,
  lastLine: number,
  text: string,
  color: string = palette.bodyText,
  x: number = left,
  maximumWidth: number = contentWidth
): number => {
  const availableRows = Math.max(0, lastLine - line + 1)
  if (!availableRows) return line
  const lines = [...wrapMedievalCanvasText(context, text, maximumWidth)]
  const bounded = lines.length <= availableRows ? lines : [
    ...lines.slice(0, Math.max(0, availableRows - 1)),
    shortenedToFit(context, lines.slice(Math.max(0, availableRows - 1)).join(' '), maximumWidth)
  ]
  bounded.forEach((wrapped, index) => row(context, line + index, wrapped, color, x))
  return line + bounded.length
}

const rule = (context: CanvasRenderingContext2D, line: number, startX: number = left, endX: number = width - left): void => {
  context.strokeStyle = palette.panelBorder
  context.beginPath()
  context.moveTo(startX, 86 + line * lineHeight)
  context.lineTo(endX, 86 + line * lineHeight)
  context.stroke()
}

const selectedMarker = (selected: boolean): string => selected ? cues.selectionMarker : ' '

interface WorldPanel {
  x: number
  width: number
}

const worldPanels = (expanded: boolean): { main: WorldPanel; sidebar: WorldPanel } => expanded
  ? { main: { x: 38, width: 445 }, sidebar: { x: 510, width: 252 } }
  : { main: { x: 38, width: 558 }, sidebar: { x: 620, width: 142 } }

const sidebarFactTitle = (item: ManagementSidebarFact): string => {
  switch (item.value.kind) {
    case 'jomon-status': return 'JOMON // MOORED'
    case 'active-courier': return item.value.name === undefined ? 'COURIER // UNASSIGNED' : `COURIER ${item.value.name.toUpperCase()}`
    case 'world-time': return `TIME // ${item.value.minutes}M`
    case 'world-era': return `ERA ${item.value.era.toUpperCase()} // ${item.value.remixCycle}`
    case 'household-person-work': return `${item.value.name.toUpperCase()} // ${item.value.currentWork.toUpperCase()}`
    case 'delegated-task': return `TASK ${uppercase(item.value.family)} // ${item.value.status.toUpperCase()}`
    case 'person-need-risk': return `! NEEDS // ${item.value.name.toUpperCase()} // ${item.value.needsMaximum}/5`
    case 'person-health-risk': return `! HEALTH // ${item.value.name.toUpperCase()} // ${item.value.health.toUpperCase()}`
    case 'delegated-task-risk': return `! TASK RISK // ${uppercase(item.value.family)} // ${item.value.risk.toUpperCase()}`
    case 'no-known-active-risk': return '+ NO KNOWN ACTIVE RISK'
    case 'frontier-revealed-fact': return `KNOWN ${item.value.factKind.toUpperCase()} // ${item.value.value.toUpperCase()}`
    case 'causal-command': return `#${item.value.sequence} // ${item.value.commandKind.toUpperCase()}`
    case 'compacted-history-segment': return `#${item.value.sequenceStart}-${item.value.sequenceEnd} // COMPACTED ${item.value.commandCount}`
    case 'social-memory': return `MEMORY // ${item.value.phase.toUpperCase()} // ${item.value.disposition.toUpperCase()}`
  }
}

/** Compact canvas source codes retain the typed source, known-at time, and freshness. */
const sidebarSourceCue = (item: ManagementSidebarFact): string => {
  switch (item.source.label) {
    case 'current-household-state': return 'HSHLD'
    case 'crew-record': return 'CREW'
    case 'delegated-task-record': return 'TASK'
    case 'autonomy-record': return 'AUTO'
    case 'social-memory': return 'MEM'
    case 'household-journal': return 'JOUR'
    case 'compacted-journal': return 'CMPCT'
    case 'institution-ledger': return 'INST'
    case 'cargo-mark': return 'CARGO'
    default: return MANAGEMENT_SIDEBAR_SOURCE_LABELS[item.source.label]
  }
}
const sidebarFreshnessCue = (item: ManagementSidebarFact): string => item.freshness.kind === 'current'
  ? 'NOW'
  : item.freshness.kind === 'timeless'
    ? 'TIMELESS'
    : `F${item.freshness.atWorldTime}M`
const sidebarFactMetadata = (item: ManagementSidebarFact): string => `SRC ${sidebarSourceCue(item)} K${item.discoveredAtWorldTime} ${sidebarFreshnessCue(item)}`

/** The canvas resolves glyph characters from the supplied legend references; it owns no symbol meanings. */
const terminalMapLegendEntries = (legend: TerminalMapLegend): readonly string[] => legend.entries.map(entry => {
  const glyph = JOMON_ASCII_GLYPH_CATALOG.entries.find(candidate => candidate.id === entry.glyph.id)
  if (!glyph) throw new Error(`terminal legend glyph is unavailable: ${entry.glyph.id}`)
  return `${glyph.character} ${entry.label.toUpperCase()}`
})

const terminalMapLegendText = (legend: TerminalMapLegend): string => terminalMapLegendEntries(legend).join(' // ')

export class MedievalApp {
  private readonly context: CanvasRenderingContext2D
  private readonly repository = new MedievalWorldRepository()
  private readonly session = new MutableWorldSession()
  private route: MedievalRoute = 'worlds'
  private index: WorldIndex = { version: 1, activeWorlds: [], chronicles: [] }
  private world: FoundationWorld | undefined
  private chronicle: WorldChronicle | undefined
  private selectedRow = 0
  private persistence: PersistenceState = 'loading'
  private error: string | undefined
  private creationSettings: CreationSettings = defaultCreationSettings()
  private creationProfiles: readonly CreationSettingsProfile[] = []
  private settingsPage: SettingsPage = 'basic'
  private profileName = 'new basin'
  private resultPage: ResultPage = 'summary'
  private generationProgress: readonly InitialWorldGenerationProgress[] = []
  /** Ephemeral presentation state only; the sidebar never writes the world. */
  private managementSidebar: ManagementSidebarModel | undefined
  private managementExpanded = true
  private managementSectionIndex = 0
  /** UI-only terminal state; none of it is part of a world save or causal history. */
  private terminalControls: TerminalControlPreferences = defaultTerminalControlPreferences()
  private worldOverlay: WorldOverlay = 'none'
  private contextualPrompt: TerminalPrompt | undefined
  private selectedTavernCandidateIndex = 0
  private selectedSettlementTradeChoiceIndex = 0
  /** Local menu cursors only; the chosen values live in ExpeditionState. */
  private expeditionPreparationStage: 'loadout' | 'support' = 'loadout'
  private selectedExpeditionLoadoutIndex = 0
  private selectedExpeditionSupportIndex = 0
  private selectedExpeditionContactChoice: 'accept' | 'refuse' = 'accept'
  private selectedTerminalControlId: TerminalControlId = TERMINAL_CONTROL_IDS[0]
  private terminalControlCapturePending = false
  private terminalInteractionOutcome: TerminalInteractionOutcome | undefined

  constructor(private readonly canvas: HTMLCanvasElement) {
    const context = canvas.getContext('2d')
    if (!context) throw new Error('Jomon needs a canvas-capable browser')
    this.context = context
    canvas.width = width
    canvas.height = height
    canvas.tabIndex = 0
    canvas.addEventListener('pointerdown', () => canvas.focus())
    canvas.addEventListener('keydown', event => this.handleKey(event))
    if ('fonts' in document) void document.fonts.load(terminalFont, 'JOMON').then(() => this.render()).catch(() => undefined)
  }

  async start(): Promise<void> {
    this.persistence = 'loading'
    this.render()
    try {
      const [index, savedSettings, controls] = await Promise.all([this.repository.loadIndex(), this.repository.loadCreationSettings(), this.repository.loadTerminalControls()])
      this.index = index
      this.applyCreationSettingsRecord(savedSettings)
      this.terminalControls = controls
      this.persistence = 'saved'
      this.error = undefined
    } catch (error) {
      this.persistence = 'error'
      this.error = errorMessage(error)
    }
    this.render()
  }

  private applyCreationSettingsRecord(record: CreationSettingsRecord): void {
    this.creationSettings = structuredClone(record.lastUsed)
    this.creationProfiles = structuredClone(record.profiles)
  }

  private async refreshIndex(): Promise<void> {
    try {
      this.index = await this.repository.loadIndex()
      this.persistence = 'saved'
    } catch (error) {
      this.persistence = 'error'
      this.error = errorMessage(error)
    }
  }

  private openSettings(): void {
    this.route = 'create-world'
    this.settingsPage = this.creationSettings.advancedMode ? 'advanced' : 'basic'
    this.selectedRow = 0
    this.error = undefined
    this.render()
  }

  private configurationResolution() { return resolveCreationSettings(this.creationSettings) }

  private setSettings(next: CreationSettings): void {
    this.creationSettings = next
    this.error = undefined
  }

  private selectPreset(direction: number): void {
    const current = presets.indexOf(this.creationSettings.configuration.preset)
    const preset = presets[(current + direction + presets.length) % presets.length]!
    this.setSettings({ ...this.creationSettings, configuration: { preset, advanced: {} } })
    this.render()
  }

  private toggleAdvanced(): void {
    this.settingsPage = 'advanced'
    this.setSettings({ ...this.creationSettings, advancedMode: true })
    this.selectedRow = 0
    this.render()
  }

  private advancedValue(setting: keyof WorldGenerationAdvancedSettings): string | number {
    return this.creationSettings.configuration.advanced[setting] ?? GENERATION_CONFIG_PRESETS[this.creationSettings.configuration.preset].defaults[setting]
  }

  private cycleAdvancedSetting(direction: number): void {
    const setting = WORLD_GENERATION_ADVANCED_SETTING_NAMES[this.selectedRow]
    if (!setting) return
    const options = WORLD_GENERATION_ADVANCED_SETTING_OPTIONS[setting] as readonly (string | number)[]
    const current = options.indexOf(this.advancedValue(setting))
    const value = options[(current + direction + options.length) % options.length]!
    const advanced = { ...this.creationSettings.configuration.advanced, [setting]: value }
    this.setSettings({ ...this.creationSettings, configuration: { ...this.creationSettings.configuration, advanced } })
    this.render()
  }

  private clearAdvancedSetting(): void {
    const setting = WORLD_GENERATION_ADVANCED_SETTING_NAMES[this.selectedRow]
    if (!setting) return
    const advanced = { ...this.creationSettings.configuration.advanced }
    delete advanced[setting]
    this.setSettings({ ...this.creationSettings, configuration: { ...this.creationSettings.configuration, advanced } })
    this.render()
  }

  private async saveProfile(): Promise<void> {
    const resolution = this.configurationResolution()
    if (resolution.status !== 'valid') {
      this.error = `SETTINGS INVALID // ${resolution.issues.map(issue => issue.field).join(', ')}`
      this.render()
      return
    }
    try {
      const saved = await this.repository.saveCreationSettingsProfile(this.profileName, resolution.settings)
      this.applyCreationSettingsRecord(saved)
      this.persistence = 'saved'
      this.error = undefined
    } catch (error) {
      this.persistence = 'error'
      this.error = errorMessage(error)
    }
    this.render()
  }

  private loadProfile(index: number): void {
    const profile = this.creationProfiles[index]
    if (!profile) return
    this.setSettings(structuredClone(profile.settings))
    this.settingsPage = this.creationSettings.advancedMode ? 'advanced' : 'basic'
    this.route = 'create-world'
    this.selectedRow = 0
    this.render()
  }

  private async createWorld(): Promise<void> {
    const resolution = this.configurationResolution()
    if (resolution.status !== 'valid') {
      this.error = `SETTINGS INVALID // ${resolution.issues.map(issue => issue.field).join(', ')}`
      this.render()
      return
    }
    this.creationSettings = resolution.settings
    this.generationProgress = []
    this.route = 'world-generation'
    this.persistence = 'loading'
    this.error = undefined
    this.render()
    try {
      const world = createFoundationWorld({
        seed: resolution.settings.seed,
        configuration: resolution.settings.configuration,
        onGenerationProgress: progress => {
          this.generationProgress = [...this.generationProgress, progress]
          this.render()
        }
      })
      this.world = world
      this.applyCreationSettingsRecord(await this.repository.saveLastUsedCreationSettings(resolution.settings))
      await this.repository.saveWorld(world)
      await this.refreshIndex()
      this.session.open(world.id)
      this.selectedRow = 0
      this.persistence = 'saved'
    } catch (error) {
      this.persistence = 'error'
      this.error = errorMessage(error)
    }
    this.render()
  }

  private inspectResult(): void {
    if (!this.world) return
    this.route = 'world-result'
    this.resultPage = 'summary'
    this.selectedRow = 0
    this.render()
  }

  private async selectCourier(): Promise<void> {
    if (!this.world) return
    const candidate = initialCourierSelectionCandidates(this.world)[this.selectedRow]
    if (!candidate) return
    this.persistence = 'loading'
    this.render()
    try {
      this.session.assertOwner(this.world.id)
      this.world = chooseInitialCourier(this.world, candidate.id)
      await this.repository.saveWorld(this.world)
      await this.refreshIndex()
      this.resetManagementSidebar()
      this.route = 'world'
      this.error = undefined
    } catch (error) {
      this.persistence = 'error'
      this.error = errorMessage(error)
    }
    this.render()
  }

  private async resumeSelectedWorld(): Promise<void> {
    const entry = this.index.activeWorlds[this.selectedRow]
    if (!entry) return
    this.persistence = 'loading'
    this.render()
    try {
      const world = await this.repository.loadWorld(entry.id)
      if (!world) throw new Error('Selected world is no longer present in local storage')
      this.session.open(world.id)
      this.world = world
      this.route = world.state.courier.initialCourierId ? 'world' : 'world-result'
      if (this.route === 'world') this.resetManagementSidebar()
      this.resultPage = 'summary'
      this.selectedRow = 0
      this.persistence = 'saved'
      this.error = undefined
    } catch (error) {
      this.persistence = 'error'
      this.error = errorMessage(error)
    }
    this.render()
  }

  private async openSelectedChronicle(): Promise<void> {
    const entry = this.index.chronicles[this.selectedRow]
    if (!entry) return
    this.persistence = 'loading'
    this.render()
    try {
      const chronicle = await this.repository.loadChronicle(entry.id)
      if (!chronicle) throw new Error('Selected chronicle is no longer present in local storage')
      this.chronicle = chronicle
      this.route = 'chronicle'
      this.persistence = 'saved'
      this.error = undefined
    } catch (error) {
      this.persistence = 'error'
      this.error = errorMessage(error)
    }
    this.render()
  }

  private exportChronicle(): void {
    if (!this.chronicle) return
    const blob = new Blob([chronicleExport(this.chronicle)], { type: 'application/json' })
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `${this.chronicle.world.manifest.creation.label.toLowerCase().replace(/[^a-z0-9]+/gu, '-')}-chronicle.json`
    link.click()
    URL.revokeObjectURL(url)
  }

  private handleKey(event: KeyboardEvent): void {
    if (event.metaKey || event.ctrlKey || event.altKey) return
    if (this.persistence === 'loading') return
    if (this.route === 'world') {
      if (this.handleWorldKey(event)) event.preventDefault()
      return
    }
    const key = event.key
    if (['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Enter', 'Escape', 'Backspace', 'n', 'N', 'c', 'C', 'e', 'E', 'r', 'R', 's', 'S'].includes(key) || key.length === 1) event.preventDefault()
    if (this.route === 'worlds') {
      if (key === 'n' || key === 'N') { this.openSettings(); return }
      if (key === 'c' || key === 'C') { this.route = 'chronicles'; this.selectedRow = 0; this.render(); return }
      if (key === 'ArrowUp') { this.selectedRow = Math.max(0, this.selectedRow - 1); this.render(); return }
      if (key === 'ArrowDown') { this.selectedRow = Math.min(Math.max(0, this.index.activeWorlds.length - 1), this.selectedRow + 1); this.render(); return }
      if (key === 'Enter') void this.resumeSelectedWorld()
      return
    }
    if (this.route === 'create-world') { this.handleSettingsKey(key); return }
    if (this.route === 'creation-profiles') { this.handleProfileKey(key); return }
    if (this.route === 'world-generation') {
      if (key === 'Enter' && this.world) this.inspectResult()
      if (key === 'Escape') { this.releaseWorld(); this.world = undefined; this.route = 'worlds'; this.selectedRow = 0; this.render() }
      return
    }
    if (this.route === 'world-result') {
      if (key === 'Escape') { this.releaseWorld(); this.world = undefined; this.route = 'worlds'; this.selectedRow = 0; this.render(); return }
      if (key === 'ArrowLeft' || key === 'ArrowRight') {
        const pages: ResultPage[] = ['summary', 'configuration', 'provenance']
        const index = pages.indexOf(this.resultPage)
        this.resultPage = pages[(index + (key === 'ArrowRight' ? 1 : -1) + pages.length) % pages.length]!
        this.render()
        return
      }
      if (key === 'Enter' && this.world) { this.route = 'choose-courier'; this.selectedRow = 0; this.render() }
      return
    }
    if (this.route === 'choose-courier') {
      const eligible = this.world ? initialCourierSelectionCandidates(this.world) : []
      if (key === 'Escape') { this.route = 'world-result'; this.selectedRow = 0; this.render(); return }
      if (key === 'ArrowUp') { this.selectedRow = Math.max(0, this.selectedRow - 1); this.render(); return }
      if (key === 'ArrowDown') { this.selectedRow = Math.min(Math.max(0, eligible.length - 1), this.selectedRow + 1); this.render(); return }
      if (key === 'Enter') void this.selectCourier()
      return
    }
    if (this.route === 'chronicles') {
      if (key === 'Escape') { this.route = 'worlds'; this.selectedRow = 0; this.render(); return }
      if (key === 'ArrowUp') { this.selectedRow = Math.max(0, this.selectedRow - 1); this.render(); return }
      if (key === 'ArrowDown') { this.selectedRow = Math.min(Math.max(0, this.index.chronicles.length - 1), this.selectedRow + 1); this.render(); return }
      if (key === 'Enter') void this.openSelectedChronicle()
      return
    }
    if (this.route === 'chronicle') {
      if (key === 'Escape') { this.route = 'chronicles'; this.chronicle = undefined; this.render(); return }
      if (key === 'e' || key === 'E') this.exportChronicle()
    }
  }

  private terminalWorldInteractionContext() {
    if (this.worldOverlay === 'contextual-prompt') return 'contextual-prompt' as const
    if (this.worldOverlay === 'command-help') return 'command-help' as const
    if (this.terminalControlCapturePending) return 'controls-key-capture' as const
    if (this.worldOverlay === 'controls-editor') return 'controls-editor' as const
    return 'world' as const
  }

  private saveTerminalControls(preferences: TerminalControlPreferences, outcome: TerminalInteractionOutcome): void {
    this.terminalControls = preferences
    this.terminalInteractionOutcome = outcome
    this.render()
    void this.repository.saveTerminalControls(preferences).then(saved => {
      this.terminalControls = saved
      this.error = undefined
      this.render()
    }).catch(error => {
      this.error = errorMessage(error)
      this.render()
    })
  }

  private moveCourier(direction: TerminalMovementDirection): void {
    if (!this.world) return
    try {
      this.session.assertOwner(this.world.id)
      const result = moveFoundationWorldCourier(this.world, direction)
      if (result.status === 'blocked') {
        this.terminalInteractionOutcome = { kind: 'movement-blocked', direction, collision: result.collision }
        this.render()
        return
      }
      this.persistence = 'loading'
      this.render()
      void this.repository.saveWorld(result.world).then(() => {
        this.world = result.world
        this.resetManagementSidebar()
        this.persistence = 'saved'
        this.error = undefined
        this.terminalInteractionOutcome = { kind: 'movement-completed', direction, column: result.to.column, row: result.to.row }
        this.render()
        void this.refreshIndex().then(() => this.render())
      }).catch(error => {
        this.persistence = 'error'
        this.error = errorMessage(error)
        this.render()
      })
    } catch (error) {
      this.error = errorMessage(error)
      this.render()
    }
  }

  /** The adapter persists only the authoritative envelope returned by the pure reducer. */
  private switchCourierAtTavern(courierId: string, fromName: string, toName: string): void {
    if (!this.world) return
    try {
      this.session.assertOwner(this.world.id)
      const next = switchTavernCourier(this.world, courierId)
      this.persistence = 'loading'
      this.render()
      void this.repository.saveWorld(next).then(() => {
        this.world = next
        this.resetManagementSidebar()
        this.worldOverlay = 'none'
        this.contextualPrompt = undefined
        this.selectedTavernCandidateIndex = 0
        this.persistence = 'saved'
        this.error = undefined
        this.terminalInteractionOutcome = { kind: 'courier-switched', fromName, toName }
        this.render()
        void this.refreshIndex().then(() => this.render())
      }).catch(error => {
        this.persistence = 'error'
        this.error = errorMessage(error)
        this.render()
      })
    } catch (error) {
      this.error = errorMessage(error)
      this.render()
    }
  }

  /** The canvas saves only the typed zero-time transition returned by the world reducer. */
  private recordStationReadout(propId: Parameters<typeof recordVesselStationReadout>[1], label: string): void {
    if (!this.world) return
    try {
      this.session.assertOwner(this.world.id)
      const next = recordVesselStationReadout(this.world, propId)
      this.persistence = 'loading'
      this.render()
      void this.repository.saveWorld(next).then(() => {
        this.world = next
        this.resetManagementSidebar()
        this.worldOverlay = 'none'
        this.contextualPrompt = undefined
        this.persistence = 'saved'
        this.error = undefined
        this.terminalInteractionOutcome = { kind: 'station-readout-recorded', label }
        this.render()
        void this.refreshIndex().then(() => this.render())
      }).catch(error => {
        this.persistence = 'error'
        this.error = errorMessage(error)
        this.render()
      })
    } catch (error) {
      this.error = errorMessage(error)
      this.render()
    }
  }

  /** The adapter persists the one source-bound public-tally decision only after full-envelope save succeeds. */
  private recordSettlementTradeDecision(outcome: 'accepted' | 'refused'): void {
    if (!this.world) return
    try {
      this.session.assertOwner(this.world.id)
      const next = outcome === 'accepted' ? acceptSettlementTradeContract(this.world) : refuseSettlementTradeContract(this.world)
      this.persistence = 'loading'
      this.render()
      void this.repository.saveWorld(next).then(() => {
        this.world = next
        this.resetManagementSidebar()
        this.worldOverlay = 'none'
        this.contextualPrompt = undefined
        this.selectedSettlementTradeChoiceIndex = 0
        this.persistence = 'saved'
        this.error = undefined
        this.terminalInteractionOutcome = { kind: 'settlement-trade-recorded', outcome }
        this.render()
        void this.refreshIndex().then(() => this.render())
      }).catch(error => {
        this.persistence = 'error'
        this.error = errorMessage(error)
        this.render()
      })
    } catch (error) {
      this.error = errorMessage(error)
      this.render()
    }
  }

  /** Delivery is a single typed cargo-hold receipt, never a browser-side cargo mutation. */
  private deliverSettlementTrade(): void {
    if (!this.world) return
    try {
      this.session.assertOwner(this.world.id)
      const next = deliverSettlementTradeContract(this.world)
      this.persistence = 'loading'
      this.render()
      void this.repository.saveWorld(next).then(() => {
        this.world = next
        this.resetManagementSidebar()
        this.worldOverlay = 'none'
        this.contextualPrompt = undefined
        this.persistence = 'saved'
        this.error = undefined
        this.terminalInteractionOutcome = { kind: 'settlement-trade-delivered' }
        this.render()
        void this.refreshIndex().then(() => this.render())
      }).catch(error => {
        this.persistence = 'error'
        this.error = errorMessage(error)
        this.render()
      })
    } catch (error) {
      this.error = errorMessage(error)
      this.render()
    }
  }

  /** The terminal persists the typed time-bearing worksite result only after a full local save. */
  private resolveHearthfordWorksite(outcome: 'ironwork-fitted' | 'lease-credit'): void {
    if (!this.world) return
    try {
      this.session.assertOwner(this.world.id)
      const next = outcome === 'ironwork-fitted' ? fitHearthfordMillIronwork(this.world) : takeHearthfordMillLeaseCredit(this.world)
      this.persistence = 'loading'
      this.render()
      void this.repository.saveWorld(next).then(() => {
        this.world = next
        this.resetManagementSidebar()
        this.worldOverlay = 'none'
        this.contextualPrompt = undefined
        this.selectedSettlementTradeChoiceIndex = 0
        this.persistence = 'saved'
        this.error = undefined
        this.terminalInteractionOutcome = { kind: 'hearthford-worksite-resolved', outcome }
        this.render()
        void this.refreshIndex().then(() => this.render())
      }).catch(error => {
        this.persistence = 'error'
        this.error = errorMessage(error)
        this.render()
      })
    } catch (error) {
      this.error = errorMessage(error)
      this.render()
    }
  }

  /** Saves the same full envelope used by every other local world transition. */
  private persistExpeditionWorld(next: FoundationWorld, outcome: TerminalInteractionOutcome, overlay: WorldOverlay = 'none'): void {
    if (!this.world) return
    try {
      this.session.assertOwner(this.world.id)
      this.persistence = 'loading'
      this.render()
      void this.repository.saveWorld(next).then(() => {
        this.world = next
        this.resetManagementSidebar()
        this.worldOverlay = overlay
        this.contextualPrompt = undefined
        this.persistence = 'saved'
        this.error = undefined
        this.terminalInteractionOutcome = outcome
        this.render()
        void this.refreshIndex().then(() => this.render())
      }).catch(error => {
        this.persistence = 'error'
        this.error = errorMessage(error)
        this.render()
      })
    } catch (error) {
      this.error = errorMessage(error)
      this.render()
    }
  }

  /** A fatal expedition action first persists its exact defeat state, then uses the existing atomic succession path. */
  private persistExpeditionDeath(next: FoundationWorld, confirmation: Parameters<MedievalWorldRepository['resolveCourierContinuityLoss']>[1], detail: string): void {
    if (!this.world) return
    try {
      this.session.assertOwner(this.world.id)
      this.persistence = 'loading'
      this.render()
      void this.repository.saveWorld(next).then(() => this.repository.resolveCourierContinuityLoss(next, confirmation)).then(resolution => {
        if (resolution.status === 'continued') {
          this.world = resolution.world
          this.resetManagementSidebar()
          this.worldOverlay = 'none'
          this.terminalInteractionOutcome = { kind: 'expedition-event', text: `${detail} A successor takes the Jomon deck.`, warning: true }
        } else {
          this.releaseWorld()
          this.world = undefined
          this.route = 'worlds'
          this.selectedRow = 0
          this.terminalInteractionOutcome = { kind: 'expedition-event', text: `${detail} The household has no successor.`, warning: true }
        }
        this.persistence = 'saved'
        this.error = undefined
        this.render()
        void this.refreshIndex().then(() => this.render())
      }).catch(error => {
        this.persistence = 'error'
        this.error = errorMessage(error)
        this.render()
      })
    } catch (error) {
      this.error = errorMessage(error)
      this.render()
    }
  }

  private handleExpeditionPreparationKey(key: string): boolean {
    if (key === 'Escape') {
      this.worldOverlay = 'none'
      this.terminalInteractionOutcome = { kind: 'overlay-dismissed', overlay: 'expedition-preparation' }
      this.render()
      return true
    }
    const selectionDelta = key === 'ArrowUp' ? -1 : key === 'ArrowDown' ? 1 : 0
    if (selectionDelta) {
      if (this.expeditionPreparationStage === 'loadout') this.selectedExpeditionLoadoutIndex = (this.selectedExpeditionLoadoutIndex + selectionDelta + EXPEDITION_LOADOUTS.length) % EXPEDITION_LOADOUTS.length
      else this.selectedExpeditionSupportIndex = (this.selectedExpeditionSupportIndex + selectionDelta + EXPEDITION_SUPPORTS.length) % EXPEDITION_SUPPORTS.length
      this.terminalInteractionOutcome = undefined
      this.render()
      return true
    }
    if (key !== 'Enter' || !this.world) return true
    try {
      if (this.expeditionPreparationStage === 'loadout') {
        const choice = EXPEDITION_LOADOUTS[this.selectedExpeditionLoadoutIndex]!
        this.expeditionPreparationStage = 'support'
        this.persistExpeditionWorld(chooseHearthfordExpeditionLoadout(this.world, choice.id as ExpeditionLoadoutId), { kind: 'expedition-event', text: `${choice.label} selected at the chart table. Choose crew support.`, warning: undefined }, 'expedition-preparation')
      } else {
        const choice = EXPEDITION_SUPPORTS[this.selectedExpeditionSupportIndex]!
        this.persistExpeditionWorld(chooseHearthfordExpeditionSupport(this.world, choice.id as ExpeditionSupportId), { kind: 'expedition-event', text: `${choice.label} prepared. The gangplank is ready.`, warning: undefined })
      }
    } catch (error) {
      this.error = errorMessage(error)
      this.render()
    }
    return true
  }

  private handleExpeditionDepartureKey(key: string): boolean {
    if (key === 'Escape') {
      this.worldOverlay = 'none'
      this.terminalInteractionOutcome = { kind: 'overlay-dismissed', overlay: 'expedition-departure' }
      this.render()
      return true
    }
    if (key !== 'Enter' || !this.world) return true
    try {
      this.persistExpeditionWorld(departForHearthfordExpedition(this.world), { kind: 'expedition-event', text: 'The courier leaves Jomon through the gangplank.' })
    } catch (error) {
      this.error = errorMessage(error)
      this.render()
    }
    return true
  }

  private handleExpeditionContactKey(key: string): boolean {
    if (!this.world) return true
    const expedition = this.world.state.expedition
    if (key === 'Escape') {
      this.worldOverlay = 'none'
      this.terminalInteractionOutcome = { kind: 'overlay-dismissed', overlay: 'expedition-contact' }
      this.render()
      return true
    }
    if (expedition.objective === 'unmet' && (key === 'ArrowUp' || key === 'ArrowDown')) {
      this.selectedExpeditionContactChoice = this.selectedExpeditionContactChoice === 'accept' ? 'refuse' : 'accept'
      this.render()
      return true
    }
    if (key !== 'Enter') return true
    try {
      if (expedition.objective === 'unmet') {
        const decision = this.selectedExpeditionContactChoice
        this.persistExpeditionWorld(decideHearthfordExpeditionObjective(this.world, decision), { kind: 'expedition-event', text: decision === 'accept' ? 'Mara Venn asks for the stranded mill seal cord.' : 'Mara Venn records the refusal.', warning: decision === 'refuse' ? true : undefined })
      } else if (expedition.objective === 'accepted' && expedition.resource === 'carried') {
        this.persistExpeditionWorld(deliverHearthfordExpeditionSealCord(this.world), { kind: 'expedition-event', text: 'Mara Venn receives the mill seal cord. Hearthford remembers the delivery.' })
      } else {
        this.worldOverlay = 'none'
        this.terminalInteractionOutcome = { kind: 'expedition-event', text: expedition.objective === 'completed' ? 'Mara Venn: the mill race holds because of your delivered seal cord.' : 'Mara Venn has no further request.' }
        this.render()
      }
    } catch (error) {
      this.error = errorMessage(error)
      this.render()
    }
    return true
  }

  private handleHearthfordMapKey(key: string): boolean {
    if (!this.world) return false
    const expedition = this.world.state.expedition
    if (this.worldOverlay === 'expedition-contact') return this.handleExpeditionContactKey(key)
    const direction = key === 'ArrowUp' || key === 'k' || key === 'K' ? 'north'
      : key === 'ArrowDown' || key === 'j' || key === 'J' ? 'south'
        : key === 'ArrowLeft' || key === 'h' || key === 'H' ? 'west'
          : key === 'ArrowRight' || key === 'l' || key === 'L' ? 'east'
            : undefined
    try {
      if (direction) {
        const transition = moveHearthfordExpeditionCourier(this.world, direction)
        if (transition.status === 'blocked') {
          this.terminalInteractionOutcome = { kind: 'expedition-event', text: transition.detail, warning: true }
          this.render()
        } else if (transition.status === 'courier-death') this.persistExpeditionDeath(transition.world, transition.confirmation, transition.detail)
        else this.persistExpeditionWorld(transition.world, { kind: 'expedition-event', text: transition.detail, warning: transition.world.state.expedition.injury === 'hurt' ? true : undefined })
        return true
      }
      const action = key === 'f' || key === 'F' ? 'attack'
        : key === 'r' || key === 'R' ? 'brace'
          : key === 'e' || key === 'E' ? 'lower-reed-screen'
            : key === 'v' || key === 'V' ? (expedition.injury === 'hurt' ? 'retreat' : 'evade')
              : undefined
      if (action) {
        const transition = actInHearthfordExpedition(this.world, action)
        if (transition.status === 'blocked') {
          this.terminalInteractionOutcome = { kind: 'expedition-event', text: transition.detail, warning: true }
          this.render()
        } else if (transition.status === 'courier-death') this.persistExpeditionDeath(transition.world, transition.confirmation, transition.detail)
        else this.persistExpeditionWorld(transition.world, { kind: 'expedition-event', text: transition.detail, warning: transition.world.state.expedition.injury === 'hurt' ? true : undefined })
        return true
      }
      if (key === 'Enter') {
        if (expedition.coordinate.column === HEARTHFORD_CONTACT_COORDINATE.column && expedition.coordinate.row === HEARTHFORD_CONTACT_COORDINATE.row) {
          this.worldOverlay = 'expedition-contact'
          this.selectedExpeditionContactChoice = 'accept'
          this.terminalInteractionOutcome = undefined
          this.render()
          return true
        }
        if (expedition.coordinate.column === HEARTHFORD_GANGPLANK.column && expedition.coordinate.row === HEARTHFORD_GANGPLANK.row) {
          const transition = returnFromHearthfordExpedition(this.world)
          if (transition.status === 'blocked') {
            this.terminalInteractionOutcome = { kind: 'expedition-event', text: transition.detail, warning: true }
            this.render()
          } else this.persistExpeditionWorld(transition.world, { kind: 'expedition-event', text: transition.detail })
          return true
        }
      }
      if (key === 'p' || key === 'P') {
        const pressure = expeditionPressure(expedition, this.world.state.temporal.worldTime)
        this.terminalInteractionOutcome = { kind: 'expedition-event', text: `Pressure ${pressure.band}: time ${pressure.elapsed}, depth ${pressure.depth}, noise ${pressure.noise}, valuables ${pressure.valuables}.` }
        this.render()
        return true
      }
    } catch (error) {
      this.error = errorMessage(error)
      this.render()
      return true
    }
    return true
  }

  /** World-view keys become typed UI intents before the canvas performs an owned transition. */
  private handleWorldKey(event: KeyboardEvent): boolean {
    if (this.worldOverlay === 'expedition-preparation') return this.handleExpeditionPreparationKey(event.key)
    if (this.worldOverlay === 'expedition-departure') return this.handleExpeditionDepartureKey(event.key)
    if (this.world?.state.expedition.location === 'hearthford') return this.handleHearthfordMapKey(event.key)
    const command = resolveTerminalWorldCommand(this.terminalControls, {
      key: event.key,
      ctrlKey: event.ctrlKey,
      altKey: event.altKey,
      metaKey: event.metaKey,
      canvasFocused: document.activeElement === this.canvas,
      context: this.terminalWorldInteractionContext()
    })
    if (command.kind === 'ignored') return false
    if (command.kind === 'capture-control-key') {
      const capture = captureTerminalControlBinding(this.terminalControls, this.selectedTerminalControlId, event)
      if (capture.status === 'accepted') {
        this.terminalControlCapturePending = false
        this.saveTerminalControls(capture.preferences, { kind: 'controls-binding-saved', controlId: capture.controlId })
      } else if (capture.status === 'cancelled') {
        this.terminalControlCapturePending = false
        this.terminalInteractionOutcome = { kind: 'controls-capture-cancelled' }
        this.render()
      } else {
        this.terminalInteractionOutcome = { kind: 'controls-binding-rejected', controlId: capture.controlId, code: capture.code }
        this.render()
      }
      return true
    }
    switch (command.kind) {
      case 'return-to-worlds':
        this.releaseWorld()
        this.route = 'worlds'
        this.world = undefined
        this.selectedRow = 0
        this.render()
        return true
      case 'cancel-overlay':
        if (command.overlay === 'contextual-prompt' && this.contextualPrompt) {
          cancelTerminalPrompt(this.contextualPrompt)
          this.terminalInteractionOutcome = { kind: 'prompt-cancelled' }
        } else this.terminalInteractionOutcome = { kind: 'overlay-dismissed', overlay: this.worldOverlay === 'none' ? 'command-help' : this.worldOverlay }
        this.worldOverlay = 'none'
        this.contextualPrompt = undefined
        this.selectedTavernCandidateIndex = 0
        this.selectedSettlementTradeChoiceIndex = 0
        this.terminalControlCapturePending = false
        this.render()
        return true
      case 'controls-select':
        this.selectedTerminalControlId = cycleTerminalControlSelection(this.selectedTerminalControlId, command.direction)
        this.terminalInteractionOutcome = undefined
        this.render()
        return true
      case 'controls-begin-capture':
        this.terminalControlCapturePending = true
        this.terminalInteractionOutcome = undefined
        this.render()
        return true
      case 'controls-reset-current': {
        const preferences = resetTerminalControl(this.terminalControls, this.selectedTerminalControlId)
        this.saveTerminalControls(preferences, { kind: 'controls-reset-current', controlId: this.selectedTerminalControlId })
        return true
      }
      case 'controls-reset-all':
        this.saveTerminalControls(resetAllTerminalControls(this.terminalControls), { kind: 'controls-reset-all' })
        return true
      case 'open-controls-editor':
        this.worldOverlay = 'controls-editor'
        this.terminalControlCapturePending = false
        this.terminalInteractionOutcome = undefined
        this.render()
        return true
      case 'open-command-help':
        this.worldOverlay = 'command-help'
        this.contextualPrompt = undefined
        this.terminalInteractionOutcome = undefined
        this.render()
        return true
      case 'open-contextual-prompt':
        if (!this.world) return false
        this.contextualPrompt = createJomonDeckContextualPrompt(this.world)
        if (this.contextualPrompt.kind === 'vessel-station-readout' && this.contextualPrompt.source.propId === 'prop:chart-table') {
          this.contextualPrompt = undefined
          this.worldOverlay = 'expedition-preparation'
          this.expeditionPreparationStage = 'loadout'
          this.selectedExpeditionLoadoutIndex = Math.max(0, EXPEDITION_LOADOUTS.findIndex(item => item.id === this.world!.state.expedition.loadout))
          this.selectedExpeditionSupportIndex = Math.max(0, EXPEDITION_SUPPORTS.findIndex(item => item.id === this.world!.state.expedition.support))
          this.terminalInteractionOutcome = undefined
          this.render()
          return true
        }
        if (this.contextualPrompt.kind === 'vessel-station-readout' && this.contextualPrompt.source.propId === 'prop:gangplank') {
          this.contextualPrompt = undefined
          this.worldOverlay = 'expedition-departure'
          this.terminalInteractionOutcome = undefined
          this.render()
          return true
        }
        this.selectedTavernCandidateIndex = 0
        this.selectedSettlementTradeChoiceIndex = 0
        this.worldOverlay = 'contextual-prompt'
        this.terminalInteractionOutcome = undefined
        this.render()
        return true
      case 'prompt-select': {
        const prompt = this.contextualPrompt
        if (!prompt) return true
        if (prompt.kind === 'tavern-courier-switch' && prompt.options[0]?.availability === 'available' && prompt.candidates.length) {
          this.selectedTavernCandidateIndex = (this.selectedTavernCandidateIndex + command.direction + prompt.candidates.length) % prompt.candidates.length
        } else if (prompt.kind === 'settlement-trade' && (prompt.surface === 'public-tally' || prompt.surface === 'mill-race-worksite') && prompt.options[0]?.availability === 'available' && prompt.choices?.length) {
          this.selectedSettlementTradeChoiceIndex = (this.selectedSettlementTradeChoiceIndex + command.direction + prompt.choices.length) % prompt.choices.length
        } else return true
        this.terminalInteractionOutcome = undefined
        this.render()
        return true
      }
      case 'prompt-confirm': {
        const prompt = this.contextualPrompt
        if (!prompt) return true
        if (prompt.kind === 'vessel-station-readout') {
          if (prompt.options[0]?.availability !== 'available') {
            this.terminalInteractionOutcome = { kind: 'prompt-option-disabled', reason: prompt.options[0]?.disabledReason ?? 'requires-future-domain-rule' }
            this.render()
            return true
          }
          this.recordStationReadout(prompt.source.propId as Parameters<typeof recordVesselStationReadout>[1], prompt.label)
          return true
        }
        if (prompt.kind === 'settlement-trade') {
          if (prompt.options[0]?.availability !== 'available') {
            this.terminalInteractionOutcome = { kind: 'prompt-option-disabled', reason: prompt.options[0]?.disabledReason ?? 'requires-future-domain-rule' }
            this.render()
            return true
          }
          if (prompt.surface === 'cargo-hold-delivery') {
            this.deliverSettlementTrade()
            return true
          }
          const choice = prompt.choices?.[this.selectedSettlementTradeChoiceIndex]
          if (!choice) return true
          if (prompt.surface === 'mill-race-worksite') {
            this.resolveHearthfordWorksite(choice.id === 'fit-ironwork' ? 'ironwork-fitted' : 'lease-credit')
            return true
          }
          this.recordSettlementTradeDecision(choice.id === 'accept' ? 'accepted' : 'refused')
          return true
        }
        if (prompt.kind !== 'tavern-courier-switch') {
          this.terminalInteractionOutcome = { kind: 'prompt-option-disabled', reason: prompt.options[0]?.disabledReason ?? 'requires-future-domain-rule' }
          this.render()
          return true
        }
        if (prompt.options[0]?.availability !== 'available') {
          this.terminalInteractionOutcome = { kind: 'prompt-option-disabled', reason: prompt.options[0]?.disabledReason ?? 'requires-future-domain-rule' }
          this.render()
          return true
        }
        const candidate = prompt.candidates[this.selectedTavernCandidateIndex]
        if (!candidate) return true
        this.switchCourierAtTavern(candidate.id, prompt.current.name, candidate.name)
        this.render()
        return true
      }
      case 'toggle-management':
        this.managementExpanded = !this.managementExpanded
        this.terminalInteractionOutcome = undefined
        this.render()
        return true
      case 'previous-management-section':
      case 'next-management-section':
        if (!this.managementExpanded) return false
        this.managementSectionIndex = (this.managementSectionIndex + (command.kind === 'next-management-section' ? 1 : -1) + MANAGEMENT_SIDEBAR_SECTION_IDS.length) % MANAGEMENT_SIDEBAR_SECTION_IDS.length
        this.terminalInteractionOutcome = undefined
        this.render()
        return true
      case 'move-courier':
        this.moveCourier(command.direction)
        return true
    }
  }

  private handleSettingsKey(key: string): void {
    if (this.settingsPage === 'basic') {
      if (key === 'Escape') { this.route = 'worlds'; this.selectedRow = 0; this.render(); return }
      if (key === 'ArrowUp') { this.selectedRow = Math.max(0, this.selectedRow - 1); this.render(); return }
      if (key === 'ArrowDown') { this.selectedRow = Math.min(4, this.selectedRow + 1); this.render(); return }
      if (this.selectedRow === 0) {
        if (key === 'Backspace') { this.setSettings({ ...this.creationSettings, seed: this.creationSettings.seed.slice(0, -1) }); this.render(); return }
        if (key.length === 1 && /[\w .,'-]/u.test(key) && this.creationSettings.seed.length < 64) { this.setSettings({ ...this.creationSettings, seed: `${this.creationSettings.seed}${key}` }); this.render(); return }
      }
      if (this.selectedRow === 1 && (key === 'ArrowLeft' || key === 'ArrowRight' || key === 'Enter')) { this.selectPreset(key === 'ArrowLeft' ? -1 : 1); return }
      if (this.selectedRow === 2 && key === 'Enter') { this.toggleAdvanced(); return }
      if (this.selectedRow === 3 && key === 'Enter') { this.route = 'creation-profiles'; this.selectedRow = 0; this.render(); return }
      if (this.selectedRow === 4 && key === 'Enter') void this.createWorld()
      return
    }
    if (key === 'Escape') { this.settingsPage = 'basic'; this.selectedRow = 0; this.render(); return }
    if (key === 'ArrowUp') { this.selectedRow = Math.max(0, this.selectedRow - 1); this.render(); return }
    if (key === 'ArrowDown') { this.selectedRow = Math.min(WORLD_GENERATION_ADVANCED_SETTING_NAMES.length - 1, this.selectedRow + 1); this.render(); return }
    if (key === 'ArrowLeft' || key === 'ArrowRight' || key === 'Enter') { this.cycleAdvancedSetting(key === 'ArrowLeft' ? -1 : 1); return }
    if (key === 'r' || key === 'R') this.clearAdvancedSetting()
  }

  private handleProfileKey(key: string): void {
    const lastRow = this.creationProfiles.length + 1
    if (key === 'Escape') { this.route = 'create-world'; this.selectedRow = 0; this.render(); return }
    if (key === 'ArrowUp') { this.selectedRow = Math.max(0, this.selectedRow - 1); this.render(); return }
    if (key === 'ArrowDown') { this.selectedRow = Math.min(lastRow, this.selectedRow + 1); this.render(); return }
    if (this.selectedRow === 0) {
      if (key === 'Backspace') { this.profileName = this.profileName.slice(0, -1); this.render(); return }
      if (key.length === 1 && /[\w .,'-]/u.test(key) && this.profileName.length < CREATION_SETTINGS_PROFILE_NAME_LIMIT) { this.profileName += key; this.render(); return }
    }
    if (this.selectedRow === 1 && (key === 'Enter' || key === 's' || key === 'S')) { void this.saveProfile(); return }
    if (this.selectedRow > 1 && key === 'Enter') this.loadProfile(this.selectedRow - 2)
  }

  private releaseWorld(): void {
    if (this.world && this.session.current() === this.world.id) this.session.release(this.world.id)
    this.managementSidebar = undefined
    this.managementExpanded = true
    this.managementSectionIndex = 0
    this.worldOverlay = 'none'
    this.contextualPrompt = undefined
    this.selectedTavernCandidateIndex = 0
    this.expeditionPreparationStage = 'loadout'
    this.selectedExpeditionLoadoutIndex = 0
    this.selectedExpeditionSupportIndex = 0
    this.selectedExpeditionContactChoice = 'accept'
    this.terminalControlCapturePending = false
    this.terminalInteractionOutcome = undefined
  }

  private resetManagementSidebar(): void {
    if (!this.world) {
      this.managementSidebar = undefined
      return
    }
    this.managementSidebar = createManagementSidebarModel(this.world)
    this.managementExpanded = true
    this.managementSectionIndex = 0
  }

  private activeManagementSection() {
    return MANAGEMENT_SIDEBAR_SECTION_IDS[this.managementSectionIndex] ?? MANAGEMENT_SIDEBAR_SECTION_IDS[0]
  }

  private render(): void {
    const context = this.context
    context.fillStyle = palette.consoleGround
    context.fillRect(0, 0, width, height)
    context.fillStyle = palette.panelSurface
    context.fillRect(20, 20, width - 40, height - 40)
    context.strokeStyle = palette.panelBorder
    context.strokeRect(20.5, 20.5, width - 41, height - 41)
    context.font = terminalFont
    context.textBaseline = 'top'
    context.fillStyle = palette.titleText
    context.fillText('JOMON', left, 38)
    context.fillStyle = palette.mutedText
    context.fillText('RIVER AND COAST // FOUNDATION', left + 108, 39)
    rule(context, 0)
    this.canvas.dataset.route = this.route
    this.canvas.dataset.persistence = this.persistence
    this.canvas.dataset.settingsPreset = this.creationSettings.configuration.preset
    this.canvas.dataset.settingsProfiles = String(this.creationProfiles.length)
    this.canvas.dataset.settingsPage = this.settingsPage
    this.canvas.dataset.resultPage = this.resultPage
    this.canvas.dataset.settingsValid = this.configurationResolution().status
    delete this.canvas.dataset.worldId
    delete this.canvas.dataset.crewCount
    delete this.canvas.dataset.generationStage
    delete this.canvas.dataset.managementVisibility
    delete this.canvas.dataset.managementSection
    delete this.canvas.dataset.managementItemCount
    delete this.canvas.dataset.managementKnownFactCount
    delete this.canvas.dataset.managementUrgentFactCount
    delete this.canvas.dataset.terminalOverlay
    delete this.canvas.dataset.terminalControlCapture
    delete this.canvas.dataset.terminalSelectedControl
    delete this.canvas.dataset.terminalOutcome
    delete this.canvas.dataset.terminalControlsVersion
    delete this.canvas.dataset.terminalLegendVersion
    delete this.canvas.dataset.terminalLegendEntryCount
    if (this.world) {
      this.canvas.dataset.worldId = this.world.id
      this.canvas.dataset.crewCount = String(this.world.crew.length)
    }
    const lastProgress = this.generationProgress.at(-1)
    if (lastProgress) this.canvas.dataset.generationStage = lastProgress.stage
    this.route === 'worlds' ? this.renderWorlds(context)
      : this.route === 'create-world' ? this.renderCreateWorld(context)
        : this.route === 'creation-profiles' ? this.renderCreationProfiles(context)
          : this.route === 'world-generation' ? this.renderGeneration(context)
            : this.route === 'world-result' ? this.renderWorldResult(context)
              : this.route === 'choose-courier' ? this.renderCourierChoice(context)
                : this.route === 'world' ? this.renderWorld(context)
                  : this.route === 'chronicles' ? this.renderChronicles(context)
                    : this.renderChronicle(context)
    if (this.error) {
      context.fillStyle = palette.panelSurface
      context.fillRect(left, 78 + diagnosticFirstLine * lineHeight, contentWidth, (diagnosticLastLine - diagnosticFirstLine + 1) * lineHeight)
      renderBoundedMedievalCanvasRows(context, diagnosticFirstLine, diagnosticLastLine, `${cues.errorPrefix} LOCAL STORAGE: ${this.error}`, palette.errorText)
    }
  }

  private renderWorlds(context: CanvasRenderingContext2D): number {
    this.canvas.setAttribute('aria-label', `Jomon worlds. ${this.index.activeWorlds.length} active worlds and ${this.index.chronicles.length} finalized chronicles.`)
    row(context, 2, 'LOCAL WORLDS', palette.titleText)
    if (!this.index.activeWorlds.length) row(context, 4, 'No active worlds. Open settings to create a seeded river world.', palette.mutedText)
    this.index.activeWorlds.forEach((entry, index) => row(context, 4 + index, `${selectedMarker(index === this.selectedRow)} ${entry.label}  ${entry.initialCourierId ? cues.activeState : cues.awaitingCourierState}`, index === this.selectedRow ? palette.selectedText : palette.bodyText))
    rule(context, 14)
    row(context, 16, 'N  world settings     ENTER  resume selected world', palette.actionText)
    row(context, 17, 'C  inspect finalized chronicles', palette.actionText)
    let line = wrappedRows(context, 19, 'Foundation only: seeded crew and local persistence are ready.', palette.mutedText)
    return wrappedRows(context, line, 'The walkable deck, trade, travel, simulation, and combat follow the roadmap.', palette.mutedText)
  }

  private renderCreateWorld(context: CanvasRenderingContext2D): number {
    return this.settingsPage === 'basic' ? this.renderBasicSettings(context) : this.renderAdvancedSettings(context)
  }

  private renderBasicSettings(context: CanvasRenderingContext2D): number {
    const resolution = this.configurationResolution()
    const normalizedSeed = normalizeCreationSeed(this.creationSettings.seed)
    this.canvas.setAttribute('aria-label', `World settings. Seed entry, preset selection, advanced configuration, saved setting profiles, and create world. Current normalized seed ${normalizedSeed}.`)
    row(context, 2, 'WORLD SETTINGS // BASIC', palette.titleText)
    row(context, 4, `${selectedMarker(this.selectedRow === 0)} SEED  ${this.creationSettings.seed}_`, this.selectedRow === 0 ? palette.selectedText : palette.bodyText)
    row(context, 5, `  NORMALIZED SEED  ${normalizedSeed}`, palette.mutedText)
    row(context, 7, `${selectedMarker(this.selectedRow === 1)} PRESET  ${uppercase(this.creationSettings.configuration.preset)}  [LEFT/RIGHT]`, this.selectedRow === 1 ? palette.selectedText : palette.bodyText)
    let line = wrappedRows(context, 8, `  ${GENERATION_CONFIG_PRESETS[this.creationSettings.configuration.preset].description}`, palette.mutedText)
    line = Math.max(10, line + 1)
    row(context, line, `${selectedMarker(this.selectedRow === 2)} ADVANCED CONFIGURATION  [ENTER]`, this.selectedRow === 2 ? palette.selectedText : palette.bodyText)
    row(context, line + 1, `${selectedMarker(this.selectedRow === 3)} SAVED SETTINGS  ${this.creationProfiles.length}/${CREATION_SETTINGS_PROFILE_LIMIT}  [ENTER]`, this.selectedRow === 3 ? palette.selectedText : palette.bodyText)
    row(context, line + 3, `${selectedMarker(this.selectedRow === 4)} CREATE WORLD  [ENTER]`, this.selectedRow === 4 ? palette.selectedText : palette.actionText)
    const status = resolution.status === 'valid' ? `${cues.readyState} CONFIGURATION VALID // selected preset plus ${Object.keys(this.creationSettings.configuration.advanced).length} explicit values` : `${cues.errorPrefix} CONFIGURATION INVALID // ${resolution.issues.map(issue => issue.field).join(', ')}`
    row(context, line + 5, status, resolution.status === 'valid' ? palette.statusReady : palette.errorText)
    line = wrappedRows(context, line + 7, 'Selected values remain distinct from the complete resolved configuration shown in Advanced and Result inspection.', palette.mutedText)
    return wrappedRows(context, Math.max(21, line + 1), 'Arrow keys move. Type on Seed. Esc returns without changing worlds.', palette.actionText)
  }

  private renderAdvancedSettings(context: CanvasRenderingContext2D): number {
    const resolution = this.configurationResolution()
    this.canvas.setAttribute('aria-label', `Advanced world settings for preset ${this.creationSettings.configuration.preset}. Every generation control is listed. Left and right change an explicit value; R returns it to preset default.`)
    row(context, 2, 'WORLD SETTINGS // ADVANCED', palette.titleText)
    row(context, 3, `SELECTED PRESET  ${uppercase(this.creationSettings.configuration.preset)} // EXPLICIT VALUES MARKED *`, palette.mutedText)
    WORLD_GENERATION_ADVANCED_SETTING_NAMES.forEach((setting, index) => {
      const explicit = Object.hasOwn(this.creationSettings.configuration.advanced, setting)
      const color = index === this.selectedRow ? palette.selectedText : explicit ? palette.bodyText : palette.mutedText
      row(context, 5 + index, `${selectedMarker(index === this.selectedRow)} ${settingLabel(setting).padEnd(25)} ${uppercase(this.advancedValue(setting))}${explicit ? ' *' : '   '}`, color)
    })
    const status = resolution.status === 'valid' ? `${cues.readyState} RESOLVED CONFIGURATION VALID` : `${cues.errorPrefix} CONFIGURATION INVALID // ${resolution.issues.map(issue => issue.field).join(', ')}`
    row(context, 19, status, resolution.status === 'valid' ? palette.statusReady : palette.errorText)
    row(context, 21, 'LEFT/RIGHT change. R use preset. Esc returns to basic settings.', palette.actionText)
    return 21
  }

  private renderCreationProfiles(context: CanvasRenderingContext2D): number {
    this.canvas.setAttribute('aria-label', `Saved local world settings. Up to ${CREATION_SETTINGS_PROFILE_LIMIT} named profiles are held only in medieval local storage. Existing matching names are replaced in place.`)
    row(context, 2, 'SAVED SETTINGS // LOCAL ONLY', palette.titleText)
    row(context, 4, `${selectedMarker(this.selectedRow === 0)} PROFILE NAME  ${this.profileName}_`, this.selectedRow === 0 ? palette.selectedText : palette.bodyText)
    row(context, 5, `${selectedMarker(this.selectedRow === 1)} SAVE OR REPLACE PROFILE  [S / ENTER]`, this.selectedRow === 1 ? palette.selectedText : palette.actionText)
    if (!this.creationProfiles.length) row(context, 7, 'No named profiles saved. Last-used settings load automatically.', palette.mutedText)
    this.creationProfiles.forEach((profile, index) => {
      const line = 7 + index * 2
      row(context, line, `${selectedMarker(this.selectedRow === index + 2)} LOAD  ${profile.name.toUpperCase()}  [ENTER]`, this.selectedRow === index + 2 ? palette.selectedText : palette.bodyText)
      row(context, line + 1, `  ${uppercase(profile.settings.configuration.preset)} // ${profile.settings.seed}`, palette.mutedText)
    })
    const line = Math.max(20, 8 + this.creationProfiles.length * 2)
    return wrappedRows(context, line, `Names are 1-${CREATION_SETTINGS_PROFILE_NAME_LIMIT} normalized characters. Six profiles maximum; matching names replace in place. Esc returns.`, palette.actionText)
  }

  private renderGeneration(context: CanvasRenderingContext2D): number {
    const resolution = this.configurationResolution()
    const currentAttempt = this.generationProgress.at(-1)?.candidateAttempt
    const currentTrace = currentAttempt === undefined ? [] : this.generationProgress.filter(progress => progress.candidateAttempt === currentAttempt)
    const completed = currentTrace.filter(progress => progress.status === 'completed').length
    const validation = currentTrace.filter(progress => progress.stage === 'validation').at(-1)
    this.canvas.setAttribute('aria-label', `Actual deterministic world generation progress. ${completed} causal stages completed${validation ? `; candidate ${validation.candidateAttempt} ${validation.status}` : ''}.`)
    row(context, 2, 'WORLD CREATION // ACTUAL GENERATOR WORK', palette.titleText)
    row(context, 4, `SEED  ${this.creationSettings.seed} // ${resolution.status === 'valid' ? uppercase(resolution.resolvedConfiguration.preset) : 'INVALID SETTINGS'}`, palette.bodyText)
    INITIAL_WORLD_GENERATION_STAGES.forEach((stage, index) => {
      const done = currentTrace.some(progress => progress.stage === stage && (progress.status === 'completed' || progress.status === 'accepted'))
      const rejected = currentTrace.some(progress => progress.stage === stage && progress.status === 'rejected')
      const marker = rejected ? cues.errorPrefix : done ? cues.readyState : '...'
      row(context, 6 + index, `${marker} ${uppercase(stage)}`, rejected ? palette.errorText : done ? palette.statusReady : palette.mutedText)
    })
    if (this.world) {
      const candidate = this.world.manifest.creation.validationHistory.initialWorld.selectedAttempt
      row(context, 15, `${cues.readyState} CANDIDATE ${candidate} ACCEPTED // WORLD TIME ${this.world.state.temporal.worldTime}`, palette.statusReady)
      return wrappedRows(context, 18, 'Enter inspects seed, configuration, diagnostics, provenance, and crew before courier confirmation. Esc returns to local worlds.', palette.actionText)
    }
    return wrappedRows(context, 18, 'Generator work is bounded and stage-based; no timer or in-world time is used.', palette.mutedText)
  }

  private renderWorldResult(context: CanvasRenderingContext2D): number {
    const world = this.world
    if (!world) { this.route = 'worlds'; this.render(); return 0 }
    this.canvas.setAttribute('aria-label', `World creation result inspection, ${this.resultPage} page. Seed, configuration, diagnostics, provenance, and generated crew are available before initial courier confirmation.`)
    row(context, 2, `CREATION RESULT // ${uppercase(this.resultPage)} [LEFT/RIGHT]`, palette.titleText)
    if (this.resultPage === 'summary') return this.renderResultSummary(context, world)
    if (this.resultPage === 'configuration') return this.renderResultConfiguration(context, world)
    return this.renderResultProvenance(context, world)
  }

  private renderResultSummary(context: CanvasRenderingContext2D, world: FoundationWorld): number {
    const initial = world.initialWorld
    row(context, 4, `SEED  ${world.manifest.creation.seed}`, palette.bodyText)
    row(context, 5, `SELECTED PRESET  ${uppercase(world.manifest.creation.selectedConfiguration.preset)} // ${Object.keys(world.manifest.creation.selectedConfiguration.advanced).length} EXPLICIT`, palette.mutedText)
    row(context, 7, `${cues.readyState} CANDIDATE ${world.manifest.creation.validationHistory.initialWorld.selectedAttempt} ACCEPTED // ${world.manifest.creation.validationHistory.initialWorld.candidates.length} CHECKED`, palette.statusReady)
    row(context, 9, `INITIAL REGION  ${initial.watershed.name.toUpperCase()} // ${initial.settlements.length} SETTLEMENTS // ${initial.routes.length} ROUTES`, palette.bodyText)
    row(context, 10, `${initial.resources.length} RESOURCES // ${initial.institutions.length} INSTITUTIONS // ${initial.people.length} HISTORY SEEDS`, palette.mutedText)
    row(context, 12, 'GENERATED CREW', palette.titleText)
    world.crew.forEach((member, index) => row(context, 13 + index, `  ${member.name.toUpperCase()} // ${uppercase(member.role)} // TALK ${member.conversation}`, palette.bodyText))
    return wrappedRows(context, 20, 'Enter chooses the initial courier. Colour is paired with > selection, + ready, and text labels. Esc returns to local worlds.', palette.actionText)
  }

  private renderResultConfiguration(context: CanvasRenderingContext2D, world: FoundationWorld): number {
    const selected = world.manifest.creation.selectedConfiguration
    const resolved = world.manifest.creation.resolvedConfiguration
    row(context, 4, `SELECTED PRESET  ${uppercase(selected.preset)}`, palette.bodyText)
    let line = wrappedRows(context, 5, `SELECTED OVERRIDES  ${Object.keys(selected.advanced).length ? Object.entries(selected.advanced).map(([key, value]) => `${key}=${value}`).join(', ') : 'NONE'}`, palette.mutedText)
    line += 1
    row(context, line, 'FULLY RESOLVED CONFIGURATION', palette.titleText)
    line += 1
    WORLD_GENERATION_ADVANCED_SETTING_NAMES.forEach(setting => {
      row(context, line, `${settingLabel(setting).padEnd(25)} ${uppercase(resolved[setting])}`, palette.bodyText)
      line += 1
    })
    return wrappedRows(context, Math.max(22, line + 1), `CONFIGURATION FINGERPRINT  ${shortDigest(world.manifest.creation.configurationFingerprint)} // LEFT/RIGHT pages // ENTER courier`, palette.actionText)
  }

  private renderResultProvenance(context: CanvasRenderingContext2D, world: FoundationWorld): number {
    const creation = world.manifest.creation
    row(context, 4, `WORLD ID  ${shortDigest(world.id)}`, palette.bodyText)
    row(context, 5, `CREATION DIGEST  ${creation.digest}`, palette.mutedText)
    row(context, 6, `INITIAL WORLD  ${creation.initialWorld.id} // ${creation.initialWorld.digest}`, palette.mutedText)
    row(context, 7, `FRONTIER ROOTS  ${creation.frontier.roots.length} // ${creation.frontier.digest}`, palette.mutedText)
    row(context, 9, 'DETERMINISTIC CANDIDATE DIAGNOSTICS', palette.titleText)
    creation.validationHistory.initialWorld.candidates.forEach((candidate, index) => {
      const outcome = candidate.status === 'accepted' ? `${cues.readyState} ACCEPTED` : `${cues.errorPrefix} REJECTED`
      row(context, 10 + index, `  ${outcome} // ${candidate.attempt} // ${candidate.streamSeed} // ${candidate.issues.map(issue => issue.code).join(', ') || 'NO ISSUES'}`, candidate.status === 'accepted' ? palette.statusReady : palette.errorText)
    })
    row(context, 15, `VERSIONS  foundation ${creation.contractVersions.foundationGenerator} // initial ${creation.contractVersions.initialWorldGenerator}`, palette.mutedText)
    row(context, 16, `POLICY ${creation.contractVersions.contentSafetyPolicy} // FRONTIER ${creation.contractVersions.frontierContract} // PAGE ${['summary', 'configuration', 'provenance'].indexOf(this.resultPage) + 1}/3`, palette.mutedText)
    return wrappedRows(context, 19, 'This immutable creation provenance is separate from future mutable world state. Enter chooses courier; Esc returns locally.', palette.actionText)
  }

  private renderCourierChoice(context: CanvasRenderingContext2D): number {
    const world = this.world
    if (!world) { this.route = 'worlds'; this.render(); return 0 }
    const candidates = initialCourierSelectionCandidates(world)
    this.canvas.setAttribute('aria-label', `Choose an initial courier for ${world.manifest.creation.label}. ${candidates.length} deterministic eligible household members are available in canonical order. Arrow keys choose; Enter confirms a zero-time active courier; Escape returns without selection. Tavern switching is available only at Jomon's task ledger; permanent continuity remains an authoritative read-only outcome, not a creation choice.`)
    row(context, 2, `CHOOSE INITIAL COURIER // ${world.manifest.creation.label.toUpperCase()}`, palette.titleText)
    row(context, 3, `SEED ${world.manifest.creation.seed} // ZERO-TIME CONFIRMATION`, palette.mutedText)
    row(context, 4, 'CONFIRM FIXES INITIAL COURIER // TAVERN SWITCHING LATER // CONTINUITY IS NOT A CREATION CHOICE', palette.actionText)
    let line = 6
    candidates.forEach((candidate, index) => {
      const member = world.crew.find(crewMember => crewMember.id === candidate.id)
      if (!member) throw new Error('validated courier candidate is missing immutable household history')
      const color = index === this.selectedRow ? palette.selectedText : palette.bodyText
      line = wrappedRows(context, line, `${selectedMarker(index === this.selectedRow)} ${candidate.name.toUpperCase()} // ${candidate.role.toUpperCase()} // CONVERSATION ${candidate.conversation}`, color)
      line = wrappedRows(context, line, `   ${member.history}.`, palette.mutedText)
    })
    return wrappedRows(context, Math.max(19, line + 1), 'Arrow keys choose. Enter fixes the active courier. Esc cancels to the creation result.', palette.actionText)
  }

  private renderManagementSidebar(context: CanvasRenderingContext2D, model: ManagementSidebarModel, panel: WorldPanel): void {
    const selectedSection = this.activeManagementSection()
    const selected = model.sections.find(section => section.id === selectedSection)
    if (!selected) throw new Error('management sidebar selection is unavailable')
    context.strokeStyle = palette.panelBorder
    context.strokeRect(panel.x - 8.5, 108.5, panel.width + 16, 480)
    if (!this.managementExpanded) {
      renderBoundedMedievalCanvasRows(context, 2, 3, '[M] MANAGEMENT', palette.actionText, panel.x, panel.width)
      renderBoundedMedievalCanvasRows(context, 5, 6, `[${this.managementSectionIndex + 1}/6] ${MANAGEMENT_SIDEBAR_SECTION_LABELS[selected.id]}`, palette.selectedText, panel.x, panel.width)
      renderBoundedMedievalCanvasRows(context, 8, 10, `${selected.facts.length} KNOWN // ${model.summary.urgentFactCount} URGENT`, palette.mutedText, panel.x, panel.width)
      renderBoundedMedievalCanvasRows(context, 12, 13, 'M EXPAND', palette.actionText, panel.x, panel.width)
      return
    }
    renderBoundedMedievalCanvasRows(context, 2, 2, '[M] MANAGEMENT', palette.titleText, panel.x, panel.width)
    renderBoundedMedievalCanvasRows(context, 3, 3, `${this.managementSectionIndex + 1}/6 ${MANAGEMENT_SIDEBAR_SECTION_LABELS[selected.id]} // ${selected.facts.length}`, palette.actionText, panel.x, panel.width)
    rule(context, 4, panel.x, panel.x + panel.width)
    let line = 5
    for (const item of selected.facts) {
      if (line > 19) break
      const marker = item.priority === 'urgent' ? '!' : item.priority === 'essential' ? '*' : '-'
      line = renderBoundedMedievalCanvasRows(context, line, line, `${marker} ${sidebarFactTitle(item)}`, item.priority === 'urgent' ? palette.warningText : item.priority === 'essential' ? palette.bodyText : palette.mutedText, panel.x, panel.width)
      if (line > 20) break
      line = renderBoundedMedievalCanvasRows(context, line, line, sidebarFactMetadata(item), palette.mutedText, panel.x, panel.width)
      line += 1
    }
    renderBoundedMedievalCanvasRows(context, 22, 22, 'M COLLAPSE', palette.actionText, panel.x, panel.width)
  }

  /** Compact, zero-time UI feedback for terminal intents; no world data is added here. */
  private terminalOutcomeText(): string | undefined {
    const outcome = this.terminalInteractionOutcome
    if (!outcome) return undefined
    switch (outcome.kind) {
      case 'movement-completed': return `+ MOVED ${uppercase(outcome.direction)} // DECK ${outcome.column},${outcome.row} // +1 ACTION MINUTE`
      case 'movement-blocked': return `! MOVE BLOCKED // ${uppercase(outcome.direction)} // ${uppercase(outcome.collision)} // ZERO TIME`
      case 'prompt-cancelled': return '+ CONTEXT PROMPT CANCELLED // ZERO TIME'
      case 'prompt-option-disabled': return `! OPTION DISABLED // ${uppercase(outcome.reason)}`
      case 'station-readout-recorded': return `+ ${outcome.label.toUpperCase()} READOUT RECORDED // ZERO TIME`
      case 'courier-switched': return `+ ACTIVE COURIER ${outcome.fromName.toUpperCase()} -> ${outcome.toName.toUpperCase()} // TAVERN LEDGER // ZERO TIME`
      case 'settlement-trade-recorded': return outcome.outcome === 'accepted'
        ? '+ HEARTHFORD IRONWORK BURDEN ACCEPTED // DELIVER AT CARGO HOLD // ZERO TIME'
        : '+ HEARTHFORD IRONWORK HANDOFF REFUSED // CASE REMAINS AT QUAY // ZERO TIME'
      case 'settlement-trade-delivered': return '+ HEARTHFORD IRONWORK DELIVERED TO CARGO HOLD // ZERO TIME'
      case 'hearthford-worksite-resolved': return outcome.outcome === 'ironwork-fitted'
        ? '+ MILL LEASE RELIEVED // IRONWORK FITTED // +20 ACTION MINUTES'
        : '+ MILL LEASE CREDIT RECORDED // CARGO RETAINED // +35 ACTION MINUTES'
      case 'overlay-dismissed': return `+ ${uppercase(outcome.overlay)} CLOSED // ZERO TIME`
      case 'controls-capture-cancelled': return '+ KEY CAPTURE CANCELLED // BINDING UNCHANGED'
      case 'controls-binding-saved': return `+ ${uppercase(outcome.controlId)} BINDING SAVED // LOCAL UI ONLY`
      case 'controls-binding-rejected': return `! BINDING REJECTED // ${uppercase(outcome.code)}`
      case 'controls-reset-current': return `+ ${uppercase(outcome.controlId)} RESET TO DEFAULT`
      case 'controls-reset-all': return '+ ALL WORLD CONTROLS RESET TO DEFAULTS'
      case 'expedition-event': return `${outcome.warning ? '! ' : '+ '}${outcome.text.toUpperCase()}`
    }
  }

  private worldOverlayAccessibleSummary(legend?: TerminalMapLegend): string {
    const outcome = this.terminalOutcomeText()
    if (this.worldOverlay === 'expedition-preparation') return `Chart table preparation. ${this.expeditionPreparationStage === 'loadout' ? 'Choose a two-item loadout.' : 'Choose one crew support preparation.'} Arrow keys select and Enter confirms. Escape cancels without time.${outcome ? ` ${outcome}` : ''}`
    if (this.worldOverlay === 'expedition-departure') return `Hearthford gangplank. Enter leaves Jomon when loadout and support are prepared. Escape cancels without time.${outcome ? ` ${outcome}` : ''}`
    if (this.worldOverlay === 'expedition-contact') return `${HEARTHFORD_CONTACT.name}, adult ${HEARTHFORD_CONTACT.role}. ${HEARTHFORD_CONTACT.problem} Arrow keys choose and Enter confirms. Escape cancels without time.${outcome ? ` ${outcome}` : ''}`
    if (this.worldOverlay === 'contextual-prompt') return `${this.contextualPrompt?.accessibilityText ?? 'Context prompt unavailable.'}${outcome ? ` ${outcome}` : ''}`
    if (this.worldOverlay === 'command-help') return `${legend?.accessibilityText ?? 'Map legend unavailable.'} ${createTerminalCommandHelpModel(this.terminalControls).accessibilitySummary} Escape closes help.${outcome ? ` ${outcome}` : ''}`
    if (this.worldOverlay === 'controls-editor') return `${createTerminalControlsEditorModel(this.terminalControls, this.selectedTerminalControlId, this.terminalControlCapturePending).accessibilitySummary}${outcome ? ` ${outcome}` : ''}`
    return outcome ?? 'No terminal overlay is open.'
  }

  private renderWorldOverlay(context: CanvasRenderingContext2D, panel: WorldPanel, legend: TerminalMapLegend): void {
    const outcome = this.terminalOutcomeText()
    if (this.worldOverlay === 'expedition-preparation') {
      const expedition = this.world?.state.expedition
      row(context, 2, 'CHART TABLE // HEARTHFORD PREPARATION', palette.titleText, panel.x)
      renderBoundedMedievalCanvasRows(context, 4, 5, 'Choose exactly one small two-item loadout, then one crew-support preparation. Both choices are zero-time.', palette.bodyText, panel.x, panel.width)
      if (this.expeditionPreparationStage === 'loadout') {
        EXPEDITION_LOADOUTS.forEach((item, index) => renderBoundedMedievalCanvasRows(context, 7 + index, 7 + index, `${selectedMarker(index === this.selectedExpeditionLoadoutIndex)} ${item.label.toUpperCase()} // TWO ITEMS`, index === this.selectedExpeditionLoadoutIndex ? palette.selectedText : palette.bodyText, panel.x, panel.width))
        renderBoundedMedievalCanvasRows(context, 12, 13, `CURRENT SUPPORT // ${uppercase(expedition?.support ?? 'unprepared')}`, palette.mutedText, panel.x, panel.width)
      } else {
        renderBoundedMedievalCanvasRows(context, 7, 7, `LOADOUT // ${uppercase(expedition?.loadout ?? 'unprepared')}`, palette.actionText, panel.x, panel.width)
        EXPEDITION_SUPPORTS.forEach((item, index) => renderBoundedMedievalCanvasRows(context, 9 + index, 9 + index, `${selectedMarker(index === this.selectedExpeditionSupportIndex)} ${item.label.toUpperCase()}`, index === this.selectedExpeditionSupportIndex ? palette.selectedText : palette.bodyText, panel.x, panel.width))
      }
      rule(context, 18, panel.x, panel.x + panel.width)
      renderBoundedMedievalCanvasRows(context, 20, 22, outcome ?? 'ARROWS SELECT // ENTER CONFIRMS // ESC CANCELS // ZERO TIME', outcome?.startsWith('!') ? palette.warningText : palette.actionText, panel.x, panel.width)
      return
    }
    if (this.worldOverlay === 'expedition-departure') {
      const expedition = this.world?.state.expedition
      row(context, 2, 'GANGPLANK // HEARTHFORD', palette.titleText, panel.x)
      renderBoundedMedievalCanvasRows(context, 4, 5, 'Leave physically through Jomon\'s gangplank for the compact Hearthford settlement and marsh.', palette.bodyText, panel.x, panel.width)
      renderBoundedMedievalCanvasRows(context, 8, 8, `LOADOUT // ${uppercase(expedition?.loadout ?? 'unprepared')}`, palette.actionText, panel.x, panel.width)
      renderBoundedMedievalCanvasRows(context, 9, 9, `SUPPORT // ${uppercase(expedition?.support ?? 'unprepared')}`, palette.actionText, panel.x, panel.width)
      renderBoundedMedievalCanvasRows(context, 12, 14, expedition?.loadout === 'unprepared' || expedition?.support === 'unprepared' ? 'Preparation is incomplete. Return to the chart table before leaving.' : 'ENTER LEAVES JOMON // ESC CANCELS // DEPARTURE IS ZERO-TIME', expedition?.loadout === 'unprepared' || expedition?.support === 'unprepared' ? palette.warningText : palette.actionText, panel.x, panel.width)
      return
    }
    if (this.worldOverlay === 'expedition-contact') {
      const expedition = this.world?.state.expedition
      row(context, 2, 'HEARTHFORD // MARA VENN', palette.titleText, panel.x)
      renderBoundedMedievalCanvasRows(context, 4, 6, `${HEARTHFORD_CONTACT.name.toUpperCase()} // ADULT ${HEARTHFORD_CONTACT.role.toUpperCase()} // ${HEARTHFORD_CONTACT.problem}`, palette.bodyText, panel.x, panel.width)
      if (expedition?.objective === 'unmet') {
        renderBoundedMedievalCanvasRows(context, 9, 9, `${selectedMarker(this.selectedExpeditionContactChoice === 'accept')} ACCEPT // RETRIEVE THE MILL SEAL CORD`, this.selectedExpeditionContactChoice === 'accept' ? palette.selectedText : palette.bodyText, panel.x, panel.width)
        renderBoundedMedievalCanvasRows(context, 10, 10, `${selectedMarker(this.selectedExpeditionContactChoice === 'refuse')} REFUSE // LEAVE THE MATERIAL PROBLEM`, this.selectedExpeditionContactChoice === 'refuse' ? palette.selectedText : palette.bodyText, panel.x, panel.width)
      } else if (expedition?.objective === 'accepted' && expedition.resource === 'carried') {
        renderBoundedMedievalCanvasRows(context, 9, 11, 'ENTER DELIVERS THE CARRIED MILL SEAL CORD // ZERO TIME', palette.actionText, panel.x, panel.width)
      } else if (expedition?.objective === 'completed') {
        renderBoundedMedievalCanvasRows(context, 9, 11, 'THE MILL RACE HOLDS. MARA VENN REMEMBERS THE DELIVERED SEAL CORD.', palette.statusReady, panel.x, panel.width)
      } else {
        renderBoundedMedievalCanvasRows(context, 9, 11, `OBJECTIVE // ${uppercase(expedition?.objective ?? 'unmet')}`, palette.mutedText, panel.x, panel.width)
      }
      rule(context, 18, panel.x, panel.x + panel.width)
      renderBoundedMedievalCanvasRows(context, 20, 22, outcome ?? (expedition?.objective === 'unmet' ? 'ARROWS SELECT // ENTER CONFIRMS // ESC CANCELS // ZERO TIME' : 'ENTER CLOSES // ESC CANCELS // ZERO TIME'), outcome?.startsWith('!') ? palette.warningText : palette.actionText, panel.x, panel.width)
      return
    }
    if (this.worldOverlay === 'contextual-prompt') {
      const prompt = this.contextualPrompt
      if (!prompt) throw new Error('contextual prompt is unavailable')
      if (prompt.kind === 'settlement-trade') {
        const option = prompt.options[0]!
        row(context, 2, `${prompt.label.toUpperCase()} // LOCAL HANDOFF`, palette[option.availability === 'available' ? 'actionText' : 'mutedText'], panel.x)
        renderBoundedMedievalCanvasRows(context, 4, 4, `SOURCE ${prompt.surface.toUpperCase()} // WORLD TIME ${prompt.evidence.knownAtWorldTime} // ${uppercase(prompt.contract.status)}`, palette.mutedText, panel.x, panel.width)
        renderBoundedMedievalCanvasRows(context, 6, 9, prompt.accessibilityText, palette.bodyText, panel.x, panel.width)
        if (prompt.choices?.length) {
          const choice = prompt.choices[this.selectedSettlementTradeChoiceIndex]
          if (choice) renderBoundedMedievalCanvasRows(context, 11, 12, `SELECT ${selectedMarker(true)} ${choice.label.toUpperCase()} // ${this.selectedSettlementTradeChoiceIndex + 1}/${prompt.choices.length}`, palette[choice.paletteToken], panel.x, panel.width)
        }
        rule(context, 18, panel.x, panel.x + panel.width)
        renderBoundedMedievalCanvasRows(context, 20, 22, outcome ?? (option.availability === 'available'
          ? prompt.surface === 'public-tally'
            ? 'ARROWS SELECT // ENTER CONFIRMS LOCAL HANDOFF // ESC CANCELS // ZERO TIME'
            : prompt.surface === 'mill-race-worksite'
              ? 'ARROWS SELECT // ENTER CONFIRMS MILL LEASE WORK // ESC CANCELS // TIME ADVANCES'
              : 'ENTER RECORDS CARGO-HOLD DELIVERY // ESC CANCELS // ZERO TIME'
          : 'ENTER REPORTS RECORDED OUTCOME // ESC CANCELS // NO MUTATION OR TIME'), outcome?.startsWith('!') ? palette.warningText : palette.actionText, panel.x, panel.width)
        return
      }
      if (prompt.kind === 'vessel-station-readout') {
        const option = prompt.options[0]!
        row(context, 2, `${prompt.label.toUpperCase()} // READOUT`, palette[prompt.readout.paletteToken], panel.x)
        renderBoundedMedievalCanvasRows(context, 4, 4, `SOURCE ${prompt.source.propId.toUpperCase()} // ${prompt.source.areaId.toUpperCase()} // WORLD TIME ${prompt.evidence.knownAtWorldTime}`, palette.mutedText, panel.x, panel.width)
        renderBoundedMedievalCanvasRows(context, 6, 8, prompt.readout.text, palette[prompt.readout.paletteToken], panel.x, panel.width)
        renderBoundedMedievalCanvasRows(context, 10, 11, `${prompt.readout.nonColorCue.text} ${uppercase(prompt.operation.availability)} // ${uppercase(prompt.operation.reason ?? 'inspection-readout-only')}`, palette[prompt.readout.paletteToken], panel.x, panel.width)
        rule(context, 18, panel.x, panel.x + panel.width)
        renderBoundedMedievalCanvasRows(context, 20, 22, outcome ?? `${option.nonColorCue.text} ENTER RECORDS READOUT // ESC CANCELS // ZERO TIME`, outcome?.startsWith('!') ? palette.warningText : palette.actionText, panel.x, panel.width)
        return
      }
      if (prompt.kind !== 'tavern-courier-switch') {
        row(context, 2, 'CONTEXTUAL PROMPT // UNAVAILABLE', palette.titleText, panel.x)
        renderBoundedMedievalCanvasRows(context, 4, 9, prompt.accessibilityText, palette.mutedText, panel.x, panel.width)
        rule(context, 18, panel.x, panel.x + panel.width)
        renderBoundedMedievalCanvasRows(context, 20, 22, outcome ?? 'ENTER REPORTS UNAVAILABLE // ESC CANCELS // NO MUTATION OR TIME', outcome?.startsWith('!') ? palette.warningText : palette.actionText, panel.x, panel.width)
        return
      }
      const option = prompt.options[0]!
      row(context, 2, 'TAVERN TASK LEDGER // COURIER SWITCH', palette.titleText, panel.x)
      renderBoundedMedievalCanvasRows(context, 4, 4, `SOURCE ${prompt.source.propId.toUpperCase()} // TAVERN ${prompt.source.coordinate.column},${prompt.source.coordinate.row} // WORLD TIME ${prompt.evidence.knownAtWorldTime}`, palette.mutedText, panel.x, panel.width)
      if (prompt.ledger) {
        renderBoundedMedievalCanvasRows(context, 5, 5, 'HOUSEHOLD AVAILABILITY // CANONICAL ORDER // SOURCE-BACKED', palette.actionText, panel.x, panel.width)
        prompt.ledger.members.forEach((member, index) => {
          renderBoundedMedievalCanvasRows(context, 6 + index, 6 + index, `${member.nonColorCue.text} ${member.name.toUpperCase()} // ${member.role.toUpperCase()} // ${member.status.toUpperCase()}`, palette[member.paletteToken], panel.x, panel.width)
        })
        if (prompt.ledger.continuity) renderBoundedMedievalCanvasRows(context, 12, 12, 'CONTINUITY // ACTIVE PERSPECTIVE CONTINUED AFTER RECORDED LOSS // READ-ONLY', palette.warningText, panel.x, panel.width)
        const candidate = prompt.candidates[this.selectedTavernCandidateIndex]
        if (option.availability === 'available' && candidate) {
          renderBoundedMedievalCanvasRows(context, 14, 14, `SELECT ${selectedMarker(true)} ${candidate.name.toUpperCase()} // ${candidate.role.toUpperCase()} // ${this.selectedTavernCandidateIndex + 1}/${prompt.candidates.length} // LIVING AVAILABLE`, palette.selectedText, panel.x, panel.width)
        } else {
          renderBoundedMedievalCanvasRows(context, 14, 14, `! SWITCH UNAVAILABLE // ${uppercase(option.disabledReason ?? 'requires-future-domain-rule')}`, palette.warningText, panel.x, panel.width)
        }
        rule(context, 16, panel.x, panel.x + panel.width)
        renderBoundedMedievalCanvasRows(context, 18, 20, outcome ?? (option.availability === 'available' ? 'ARROWS SELECT // ENTER CONFIRMS ZERO-TIME SWITCH // ESC CANCELS // LOSS READ-ONLY' : 'ENTER REPORTS UNAVAILABLE // ESC CANCELS // NO MUTATION OR TIME'), outcome?.startsWith('!') ? palette.warningText : palette.actionText, panel.x, panel.width)
      } else {
        renderBoundedMedievalCanvasRows(context, 6, 8, `! SWITCH UNAVAILABLE // ${uppercase(option.disabledReason ?? 'not-at-tavern-ledger')}`, palette.warningText, panel.x, panel.width)
        renderBoundedMedievalCanvasRows(context, 9, 11, option.accessibilityText, palette.mutedText, panel.x, panel.width)
        rule(context, 18, panel.x, panel.x + panel.width)
        renderBoundedMedievalCanvasRows(context, 20, 22, outcome ?? 'RETURN TO THE PHYSICAL LEDGER FOR AVAILABILITY AND LOSS READOUT // ESC CANCELS // NO MUTATION OR TIME', outcome?.startsWith('!') ? palette.warningText : palette.actionText, panel.x, panel.width)
      }
      return
    }
    if (this.worldOverlay === 'command-help') {
      const help = createTerminalCommandHelpModel(this.terminalControls)
      const legendEntries = terminalMapLegendText(legend).split(' // ')
      row(context, 2, 'COMMAND HELP // WORLD CONTROLS', palette.titleText, panel.x)
      renderBoundedMedievalCanvasRows(context, 3, 3, `MAP ${legendEntries.slice(0, 2).join(' ')}`, palette.bodyText, panel.x, panel.width)
      renderBoundedMedievalCanvasRows(context, 4, 4, `MAP ${legendEntries.slice(2).join(' ')}`, palette.bodyText, panel.x, panel.width)
      let line = 5
      for (const entry of help.entries) {
        if (line > 17) break
        const availability = entry.operationalState === 'movement-available' ? 'LOCAL STEP +1M' : entry.operationalState === 'opens-contextual-prompt' ? 'STATION PROMPT' : 'READY'
        renderBoundedMedievalCanvasRows(context, line, line, `- ${entry.bindingText}  ${entry.label.toUpperCase()} // ${availability}`, entry.operationalState === 'movement-available' ? palette.bodyText : palette.mutedText, panel.x, panel.width)
        line += 1
      }
      rule(context, 18, panel.x, panel.x + panel.width)
      renderBoundedMedievalCanvasRows(context, 20, 20, 'FIXED KNOWN DECK // MOVED +1M', palette.actionText, panel.x, panel.width)
      renderBoundedMedievalCanvasRows(context, 21, 21, 'BLOCKED ZERO TIME // NO CARGO, NPC, HAZARD', palette.actionText, panel.x, panel.width)
      renderBoundedMedievalCanvasRows(context, 22, 22, 'STATION READOUTS RECORD LATEST FEEDBACK // LEDGER SWITCH ONLY // NO TRAVEL OR FOG // ESC CLOSE', palette.actionText, panel.x, panel.width)
      return
    }
    const editor = createTerminalControlsEditorModel(this.terminalControls, this.selectedTerminalControlId, this.terminalControlCapturePending)
    row(context, 2, 'WORLD CONTROLS // LOCAL UI ONLY', palette.titleText, panel.x)
    renderBoundedMedievalCanvasRows(context, 3, 3, this.terminalControlCapturePending ? 'CAPTURE KEY // ESC CANCELS' : 'SELECT A CONTROL // ARROWS MOVE', this.terminalControlCapturePending ? palette.warningText : palette.mutedText, panel.x, panel.width)
    let line = 5
    for (const entry of editor.entries) {
      if (line > 17) break
      renderBoundedMedievalCanvasRows(context, line, line, `${selectedMarker(entry.selected)} [${entry.key}] ${entry.label.toUpperCase()}`, entry.selected ? palette.selectedText : palette.bodyText, panel.x, panel.width)
      line += 1
    }
    rule(context, 18, panel.x, panel.x + panel.width)
    renderBoundedMedievalCanvasRows(context, 20, 22, outcome ?? (this.terminalControlCapturePending ? 'PRESS A SUPPORTED KEY // ESC CANCEL' : 'ENTER CAPTURE // R RESET // A ALL // ESC CLOSE'), outcome?.startsWith('!') ? palette.warningText : palette.actionText, panel.x, panel.width)
  }

  /** Draws only the already-validated common terminal map; it owns no deck geometry or glyph values. */
  private renderTerminalMap(context: CanvasRenderingContext2D, map: TerminalMaterializedMap, panel: WorldPanel): void {
    const cellWidth = 18
    const cellHeight = 18
    const mapWidth = map.viewport.width * cellWidth
    const mapHeight = map.viewport.height * cellHeight
    const originX = panel.x + Math.max(20, Math.floor((panel.width - mapWidth) / 2))
    const originY = 144
    context.strokeStyle = palette.panelBorder
    context.strokeRect(originX - 10.5, originY - 10.5, mapWidth + 20, mapHeight + 20)
    for (const cell of map.cells) {
      const glyph = JOMON_ASCII_GLYPH_CATALOG.entries.find(candidate => candidate.id === cell.glyph.id)
      if (!glyph) throw new Error(`terminal map glyph is unavailable: ${cell.glyph.id}`)
      context.fillStyle = palette[cell.paletteToken]
      context.fillText(glyph.character, originX + (cell.coordinate.column - map.viewport.origin.column) * cellWidth, originY + (cell.coordinate.row - map.viewport.origin.row + 1) * cellHeight)
    }
  }

  /** This is one authored settlement-and-marsh board, not a second map renderer. */
  private renderHearthfordMap(context: CanvasRenderingContext2D, world: FoundationWorld, panel: WorldPanel): void {
    const expedition = world.state.expedition
    const originX = panel.x + Math.max(20, Math.floor((panel.width - HEARTHFORD_MAP_ROWS[0]!.length * 18) / 2))
    const originY = 156
    context.strokeStyle = palette.panelBorder
    context.strokeRect(originX - 10.5, originY - 10.5, HEARTHFORD_MAP_ROWS[0]!.length * 18 + 20, HEARTHFORD_MAP_ROWS.length * 18 + 20)
    HEARTHFORD_MAP_ROWS.forEach((source, rowIndex) => {
      for (let column = 0; column < source.length; column++) {
        const glyph = hearthfordGlyphAt(expedition, { column, row: rowIndex })
        context.fillStyle = glyph === '@' ? palette.selectedText : glyph === 'H' ? palette.warningText : glyph === 'M' || glyph === 'S' || glyph === 'R' || glyph === 'G' ? palette.actionText : glyph === '#' ? palette.mutedText : palette.bodyText
        context.fillText(glyph, originX + column * 18, originY + rowIndex * 18)
      }
    })
  }

  private renderWorld(context: CanvasRenderingContext2D): number {
    const world = this.world
    if (!world) { this.route = 'worlds'; this.render(); return 0 }
    const courier = world.crew.find(member => member.id === world.state.courier.activeCourierId)
    // The canvas is only an adapter: status and bounded persisted action
    // feedback come from the renderer-neutral terminal model.
    const terminal = createTerminalPresentationModel(world)
    if (!this.managementSidebar) this.resetManagementSidebar()
    const model = this.managementSidebar
    if (!model) throw new Error('management sidebar model is unavailable')
    const selectedSection = this.activeManagementSection()
    const activeSection = model.sections.find(section => section.id === selectedSection)
    if (!activeSection) throw new Error('management sidebar section is unavailable')
    this.canvas.dataset.managementVisibility = this.managementExpanded ? 'expanded' : 'collapsed'
    this.canvas.dataset.managementSection = selectedSection
    this.canvas.dataset.managementItemCount = String(activeSection.facts.length)
    this.canvas.dataset.managementKnownFactCount = String(model.summary.knownFactCount)
    this.canvas.dataset.managementUrgentFactCount = String(model.summary.urgentFactCount)
    this.canvas.dataset.terminalOverlay = this.worldOverlay
    this.canvas.dataset.terminalControlCapture = this.terminalControlCapturePending ? 'pending' : 'idle'
    this.canvas.dataset.terminalSelectedControl = this.selectedTerminalControlId
    this.canvas.dataset.terminalOutcome = this.terminalInteractionOutcome?.kind ?? 'none'
    this.canvas.dataset.terminalControlsVersion = String(this.terminalControls.version)
    this.canvas.dataset.terminalPresentationVersion = String(terminal.version)
    this.canvas.dataset.terminalMapState = terminal.map.state
    this.canvas.dataset.terminalMapCellCount = String(terminal.map.cells.length)
    this.canvas.dataset.terminalStaticMapCellCount = String(terminal.map.cells.filter(cell => cell.id !== 'terminal-marker:active-courier').length)
    this.canvas.dataset.terminalCourierMarkerCount = String(terminal.map.cells.filter(cell => cell.id === 'terminal-marker:active-courier').length)
    this.canvas.dataset.terminalLegendVersion = String(terminal.legend.version)
    this.canvas.dataset.terminalLegendEntryCount = String(terminal.legend.entries.length)
    this.canvas.dataset.terminalFocus = terminal.map.camera.focus.coordinate === undefined
      ? 'unassigned'
      : `${terminal.map.camera.focus.coordinate.column},${terminal.map.camera.focus.coordinate.row}`
    this.canvas.dataset.terminalVisibility = terminal.map.camera.visibility
    this.canvas.dataset.terminalStatusCount = String(terminal.status.length)
    this.canvas.dataset.terminalMessageCount = String(terminal.messages.length)
    this.canvas.dataset.terminalMessageState = terminal.messages.length ? 'available' : 'empty'
    this.canvas.dataset.terminalPromptCount = String(terminal.prompts.length)
    this.canvas.setAttribute('aria-label', `Jomon foundation world ${world.manifest.creation.label}, active courier ${courier?.name ?? 'unassigned'}. ${terminal.accessibility.conciseSummary} ${terminal.accessibility.mapText} ${terminal.accessibility.legendText} ${terminal.accessibility.statusText.join(' ')} ${terminal.accessibility.messageText.join(' ')} ${terminal.accessibility.promptText.join(' ')} ${managementSidebarAccessibleSummary(model, selectedSection, this.managementExpanded)} ${this.worldOverlayAccessibleSummary(terminal.legend)}`)
    const panels = worldPanels(this.managementExpanded)
    context.strokeStyle = palette.panelBorder
    context.strokeRect(panels.main.x - 8.5, 108.5, panels.main.width + 16, 480)
    if (this.worldOverlay === 'none' && world.state.expedition.location === 'hearthford') {
      const expedition = world.state.expedition
      const pressure = expeditionPressure(expedition, world.state.temporal.worldTime)
      row(context, 2, 'HEARTHFORD // WEIR AND MARSH', palette.titleText, panels.main.x)
      this.renderHearthfordMap(context, world, panels.main)
      renderBoundedMedievalCanvasRows(context, 12, 12, 'MAP @ COURIER // M MARA VEN // H MARSH HOUND // R REED SCREEN // S SEAL CORD // G GANGPLANK', palette.bodyText, panels.main.x, panels.main.width)
      renderBoundedMedievalCanvasRows(context, 13, 13, `OBJECTIVE // ${uppercase(expedition.objective)} // RESOURCE // ${uppercase(expedition.resource)} // CONSEQUENCE // ${uppercase(expedition.consequence)}`, expedition.objective === 'completed' ? palette.statusReady : palette.bodyText, panels.main.x, panels.main.width)
      renderBoundedMedievalCanvasRows(context, 14, 14, `THREAT // ${uppercase(expedition.threat.status)} HP ${expedition.threat.health}/2 // INTENT ${uppercase(expedition.threat.intent)} // INJURY ${uppercase(expedition.injury)}`, expedition.threat.status === 'engaged' ? palette.warningText : palette.mutedText, panels.main.x, panels.main.width)
      renderBoundedMedievalCanvasRows(context, 15, 15, `PRESSURE ${uppercase(pressure.band)} // TIME ${pressure.elapsed} + DEPTH ${pressure.depth} + NOISE ${pressure.noise} + VALUABLES ${pressure.valuables} = ${pressure.total}`, pressure.band === 'critical' ? palette.warningText : palette.actionText, panels.main.x, panels.main.width)
      renderBoundedMedievalCanvasRows(context, 16, 17, 'ARROWS / HJKL MOVE +1M // F ATTACK // R BRACE // E LOWER REEDS // V EVADE OR INJURED RETREAT // ENTER TALK OR RETURN', palette.actionText, panels.main.x, panels.main.width)
      renderBoundedMedievalCanvasRows(context, 18, 18, 'EVASION NEEDS LOWERED REEDS, CARRIED SEAL CORD, QUIET FOOTING, AND SMOKE OR A SCOUT.', palette.mutedText, panels.main.x, panels.main.width)
      rule(context, 20, panels.main.x, panels.main.x + panels.main.width)
      renderBoundedMedievalCanvasRows(context, 21, 22, this.terminalOutcomeText() ?? 'P INSPECTS THE FOUR PRESSURE CONTRIBUTORS // BLOCKED MOVES AND CANCELLED CHOICES ARE ZERO-TIME', this.terminalInteractionOutcome?.kind === 'expedition-event' && this.terminalInteractionOutcome.warning ? palette.warningText : palette.actionText, panels.main.x, panels.main.width)
    } else if (this.worldOverlay === 'none') {
      row(context, 2, `${world.manifest.creation.label.toUpperCase()} // JOMON DECK`, palette.titleText, panels.main.x)
      this.renderTerminalMap(context, terminal.map, panels.main)
      renderBoundedMedievalCanvasRows(context, 12, 12, `ACTIVE COURIER  ${courier?.name.toUpperCase() ?? 'UNASSIGNED'} // ${courier?.role.toUpperCase() ?? 'NONE'}`, palette.statusReady, panels.main.x, panels.main.width)
      renderBoundedMedievalCanvasRows(context, 13, 13, `DECK FOCUS ${world.state.navigation.coordinate ? `${world.state.navigation.coordinate.column},${world.state.navigation.coordinate.row}` : 'UNASSIGNED'} // WORLD TIME ${world.state.temporal.worldTime}`, palette.bodyText, panels.main.x, panels.main.width)
      const legendEntries = terminalMapLegendEntries(terminal.legend)
      renderBoundedMedievalCanvasRows(context, 14, 14, `MAP ${legendEntries.slice(0, 2).join(' // ')}`, palette.bodyText, panels.main.x, panels.main.width)
      renderBoundedMedievalCanvasRows(context, 15, 15, `MAP ${legendEntries.slice(2, 4).join(' // ')}`, palette.bodyText, panels.main.x, panels.main.width)
      renderBoundedMedievalCanvasRows(context, 16, 16, `MAP ${legendEntries.slice(4).join(' // ')}`, palette.bodyText, panels.main.x, panels.main.width)
      renderBoundedMedievalCanvasRows(context, 17, 18, terminal.legend.limitationsText, palette.mutedText, panels.main.x, panels.main.width)
      const latestAction = terminal.status.find(item => item.value.kind === 'vessel-prop-action')
      renderBoundedMedievalCanvasRows(context, 19, 19, latestAction ? `STATUS // ${latestAction.accessibilityText}` : 'STATUS // NO RECORDED VESSEL PROP ACTION', palette.mutedText, panels.main.x, panels.main.width)
      renderBoundedMedievalCanvasRows(context, 20, 20, `MESSAGES // ${terminal.messages.length ? terminal.messages.map(item => item.text).join(' // ') : 'NO CURRENT AUTHORITATIVE MESSAGES.'}`, palette.mutedText, panels.main.x, panels.main.width)
      rule(context, 20, panels.main.x, panels.main.x + panels.main.width)
      const defaultHelp = this.managementExpanded
        ? 'ARROWS / HJKL / YUBN move // M collapse // [ / ] sections // ENTER prompt // ? help // F2 controls // ESC worlds'
        : 'ARROWS / HJKL / YUBN move // M expand // ENTER prompt // ? help // F2 controls // ESC worlds'
      renderBoundedMedievalCanvasRows(context, 21, 22, this.terminalOutcomeText() ?? defaultHelp, this.terminalInteractionOutcome?.kind === 'movement-blocked' ? palette.warningText : palette.actionText, panels.main.x, panels.main.width)
    } else this.renderWorldOverlay(context, panels.main, terminal.legend)
    this.renderManagementSidebar(context, model, panels.sidebar)
    return 22
  }

  private renderChronicles(context: CanvasRenderingContext2D): number {
    this.canvas.setAttribute('aria-label', `${this.index.chronicles.length} finalized Jomon chronicles.`)
    row(context, 2, 'FINALIZED CHRONICLES', palette.titleText)
    if (!this.index.chronicles.length) row(context, 4, 'No finalized chronicles are stored locally.', palette.mutedText)
    this.index.chronicles.forEach((entry, index) => row(context, 4 + index, `${selectedMarker(index === this.selectedRow)} ${entry.label} // ${entry.reason.toUpperCase()}`, index === this.selectedRow ? palette.selectedText : palette.bodyText))
    rule(context, 14)
    return wrappedRows(context, 16, 'Arrow keys choose. Enter inspects. Esc returns to local worlds.', palette.actionText)
  }

  private renderChronicle(context: CanvasRenderingContext2D): number {
    const chronicle = this.chronicle
    if (!chronicle) { this.route = 'chronicles'; this.render(); return 0 }
    this.canvas.setAttribute('aria-label', `Read-only Jomon chronicle ${chronicle.world.manifest.creation.label}, finalized by ${chronicle.reason}.`)
    row(context, 2, `${chronicle.world.manifest.creation.label.toUpperCase()} // READ-ONLY CHRONICLE`, palette.titleText)
    row(context, 4, `FINAL REASON  ${chronicle.reason.toUpperCase()}`, palette.statusRisk)
    row(context, 5, `SEED  ${chronicle.world.manifest.creation.seed} // WORLD TIME ${chronicle.world.state.temporal.worldTime}`, palette.bodyText)
    let line = 7
    chronicle.world.state.temporal.causalRecords.forEach(record => { line = wrappedRows(context, line, `${record.sequence}. ${record.detail}`, palette.mutedText) })
    rule(context, 14)
    return wrappedRows(context, Math.max(16, line + 1), 'E exports JSON. Esc returns to finalized chronicles.', palette.actionText)
  }
}
