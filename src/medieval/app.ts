import { GENERATION_CONFIG_PRESETS, WORLD_GENERATION_ADVANCED_SETTING_NAMES, WORLD_GENERATION_ADVANCED_SETTING_OPTIONS, type WorldGenerationAdvancedSettings, type WorldGenerationPreset } from './generation-config'
import { INITIAL_WORLD_GENERATION_STAGES, type InitialWorldGenerationProgress } from './initial-world'
import { JOMON_NON_COLOR_STATE_CUES, JOMON_PALETTE } from './palette'
import { CREATION_SETTINGS_PROFILE_LIMIT, CREATION_SETTINGS_PROFILE_NAME_LIMIT, defaultCreationSettings, normalizeCreationSeed, resolveCreationSettings, type CreationSettings, type CreationSettingsProfile, type CreationSettingsRecord } from './settings'
import { MedievalWorldRepository } from './storage'
import { MutableWorldSession } from './session'
import type { FoundationWorld, MedievalRoute, WorldChronicle, WorldIndex } from './types'
import { chronicleExport, chooseInitialCourier, createFoundationWorld } from './world'

type PersistenceState = 'loading' | 'saved' | 'error'
type SettingsPage = 'basic' | 'advanced'
type ResultPage = 'summary' | 'configuration' | 'provenance'

const palette = JOMON_PALETTE
const cues = JOMON_NON_COLOR_STATE_CUES
const width = 800
const height = 630
const left = 38
const lineHeight = 22
const contentWidth = width - left * 2
const terminalFont = '18px "BigBlueTerm", monospace'
const presets = Object.keys(GENERATION_CONFIG_PRESETS) as WorldGenerationPreset[]
const diagnosticFirstLine = 21
const diagnosticLastLine = 23

const errorMessage = (error: unknown): string => error instanceof Error ? error.message : 'Unknown local storage error'
const uppercase = (value: string | number): string => String(value).replaceAll('-', ' ').toUpperCase()
const settingLabel = (setting: keyof WorldGenerationAdvancedSettings): string => uppercase(setting.replace(/([A-Z])/gu, ' $1'))
const shortDigest = (value: string): string => value.length > 32 ? `${value.slice(0, 29)}...` : value

const row = (context: CanvasRenderingContext2D, line: number, text: string, color: string = palette.bodyText): void => {
  context.fillStyle = color
  context.fillText(text, left, 78 + line * lineHeight)
}

export const wrapMedievalCanvasText = (context: Pick<CanvasRenderingContext2D, 'measureText'>, text: string): readonly string[] => {
  const words = text.trim().split(/\s+/u).filter(Boolean)
  if (!words.length) return ['']
  const splitLongWord = (word: string): readonly string[] => {
    const pieces: string[] = []
    let piece = ''
    for (const character of word) {
      if (!piece || context.measureText(`${piece}${character}`).width <= contentWidth) piece += character
      else { pieces.push(piece); piece = character }
    }
    if (piece) pieces.push(piece)
    return pieces
  }
  const lines: string[] = []
  let current = ''
  for (const word of words) {
    const next = current ? `${current} ${word}` : word
    if (context.measureText(next).width <= contentWidth) { current = next; continue }
    if (current) { lines.push(current); current = '' }
    if (context.measureText(word).width <= contentWidth) { current = word; continue }
    const pieces = splitLongWord(word)
    lines.push(...pieces.slice(0, -1))
    current = pieces.at(-1) ?? ''
  }
  if (current) lines.push(current)
  return lines
}

const wrappedRows = (context: CanvasRenderingContext2D, line: number, text: string, color: string = palette.bodyText): number => {
  const lines = wrapMedievalCanvasText(context, text)
  lines.forEach((wrapped, index) => row(context, line + index, wrapped, color))
  return line + lines.length
}

const shortenedToFit = (context: Pick<CanvasRenderingContext2D, 'measureText'>, text: string): string => {
  if (context.measureText(text).width <= contentWidth) return text
  let shortened = text
  while (shortened && context.measureText(`${shortened}...`).width > contentWidth) shortened = shortened.slice(0, -1)
  return `${shortened}...`
}

/** Error text has a fixed in-panel budget, so a stale local record cannot paint outside the canvas. */
export const renderBoundedMedievalCanvasRows = (
  context: CanvasRenderingContext2D,
  line: number,
  lastLine: number,
  text: string,
  color: string = palette.bodyText
): number => {
  const availableRows = Math.max(0, lastLine - line + 1)
  if (!availableRows) return line
  const lines = [...wrapMedievalCanvasText(context, text)]
  const bounded = lines.length <= availableRows ? lines : [
    ...lines.slice(0, Math.max(0, availableRows - 1)),
    shortenedToFit(context, lines.slice(Math.max(0, availableRows - 1)).join(' '))
  ]
  bounded.forEach((wrapped, index) => row(context, line + index, wrapped, color))
  return line + bounded.length
}

const rule = (context: CanvasRenderingContext2D, line: number): void => {
  context.strokeStyle = palette.panelBorder
  context.beginPath()
  context.moveTo(left, 86 + line * lineHeight)
  context.lineTo(width - left, 86 + line * lineHeight)
  context.stroke()
}

const selectedMarker = (selected: boolean): string => selected ? cues.selectionMarker : ' '

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

  constructor(private readonly canvas: HTMLCanvasElement) {
    const context = canvas.getContext('2d')
    if (!context) throw new Error('Jomon needs a canvas-capable browser')
    this.context = context
    canvas.width = width
    canvas.height = height
    canvas.addEventListener('pointerdown', () => canvas.focus())
    window.addEventListener('keydown', event => this.handleKey(event))
    if ('fonts' in document) void document.fonts.load(terminalFont, 'JOMON').then(() => this.render()).catch(() => undefined)
  }

  async start(): Promise<void> {
    this.persistence = 'loading'
    this.render()
    try {
      const [index, savedSettings] = await Promise.all([this.repository.loadIndex(), this.repository.loadCreationSettings()])
      this.index = index
      this.applyCreationSettingsRecord(savedSettings)
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
    const candidate = this.world.crew.filter(member => member.eligible)[this.selectedRow]
    if (!candidate) return
    this.persistence = 'loading'
    this.render()
    try {
      this.session.assertOwner(this.world.id)
      this.world = chooseInitialCourier(this.world, candidate.id)
      await this.repository.saveWorld(this.world)
      await this.refreshIndex()
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
    const key = event.key
    if (['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Enter', 'Escape', 'Backspace', 'n', 'N', 'c', 'C', 'e', 'E', 'r', 'R', 's', 'S'].includes(key) || key.length === 1) event.preventDefault()
    if (this.persistence === 'loading') return
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
      const eligible = this.world?.crew.filter(member => member.eligible) ?? []
      if (key === 'Escape') { this.route = 'world-result'; this.selectedRow = 0; this.render(); return }
      if (key === 'ArrowUp') { this.selectedRow = Math.max(0, this.selectedRow - 1); this.render(); return }
      if (key === 'ArrowDown') { this.selectedRow = Math.min(Math.max(0, eligible.length - 1), this.selectedRow + 1); this.render(); return }
      if (key === 'Enter') void this.selectCourier()
      return
    }
    if (this.route === 'world') {
      if (key === 'Escape') { this.releaseWorld(); this.route = 'worlds'; this.world = undefined; this.selectedRow = 0; this.render() }
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
    this.canvas.setAttribute('aria-label', `Choose an initial courier for ${world.manifest.creation.label}. ${world.crew.length} generated crew members are eligible.`)
    row(context, 2, `CHOOSE INITIAL COURIER // ${world.manifest.creation.label.toUpperCase()}`, palette.titleText)
    row(context, 3, `SEED ${world.manifest.creation.seed} // time remains 0`, palette.mutedText)
    let line = 5
    world.crew.filter(member => member.eligible).forEach((member, index) => {
      const color = index === this.selectedRow ? palette.selectedText : palette.bodyText
      line = wrappedRows(context, line, `${selectedMarker(index === this.selectedRow)} ${member.name.toUpperCase()} // ${member.role.toUpperCase()} // CONVERSATION ${member.conversation}`, color)
      line = wrappedRows(context, line, `   ${member.history}.`, palette.mutedText)
    })
    return wrappedRows(context, Math.max(19, line + 1), 'Arrow keys choose. Enter confirms. Esc returns to the creation result.', palette.actionText)
  }

  private renderWorld(context: CanvasRenderingContext2D): number {
    const world = this.world
    if (!world) { this.route = 'worlds'; this.render(); return 0 }
    const courier = world.crew.find(member => member.id === world.state.courier.initialCourierId)
    this.canvas.setAttribute('aria-label', `Jomon foundation world ${world.manifest.creation.label}, active courier ${courier?.name ?? 'unassigned'}.`)
    row(context, 2, `${world.manifest.creation.label.toUpperCase()} // FOUNDATION WORLD`, palette.titleText)
    row(context, 4, `ACTIVE COURIER  ${courier?.name.toUpperCase() ?? 'UNASSIGNED'} // ${courier?.role.toUpperCase() ?? 'NONE'}`, palette.statusReady)
    row(context, 5, `SEED  ${world.manifest.creation.seed} // WORLD TIME ${world.state.temporal.worldTime}`, palette.bodyText)
    let line = wrappedRows(context, 7, 'The household exists. Time has not advanced and no map has been generated yet.', palette.mutedText)
    line = wrappedRows(context, line, 'This is the clean persistence boundary before the walkable Jomon foundation.', palette.mutedText)
    rule(context, 14)
    return wrappedRows(context, Math.max(16, line + 1), 'Esc returns to local worlds. All gameplay actions are still roadmap work.', palette.actionText)
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
