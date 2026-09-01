import { MedievalWorldRepository } from './storage'
import { MutableWorldSession } from './session'
import type { FoundationCrewMember, FoundationWorld, MedievalRoute, WorldChronicle, WorldIndex } from './types'
import { chronicleExport, chooseInitialCourier, createFoundationWorld } from './world'

type PersistenceState = 'loading' | 'saved' | 'error'

const colors = { ink: '#080a0b', panel: '#111715', text: '#d9dfcb', dim: '#91a091', gold: '#e1bd72', green: '#89bc83', red: '#db8173', line: '#38513c' } as const
const width = 800
const height = 630
const left = 38
const lineHeight = 22

const errorMessage = (error: unknown): string => error instanceof Error ? error.message : 'Unknown local storage error'

const row = (context: CanvasRenderingContext2D, line: number, text: string, color: string = colors.text): void => {
  context.fillStyle = color
  context.fillText(text, left, 78 + line * lineHeight)
}

const rule = (context: CanvasRenderingContext2D, line: number): void => {
  context.strokeStyle = colors.line
  context.beginPath()
  context.moveTo(left, 86 + line * lineHeight)
  context.lineTo(width - left, 86 + line * lineHeight)
  context.stroke()
}

const activeCourier = (world: FoundationWorld): FoundationCrewMember | undefined => world.crew.find(member => member.id === world.manifest.initialCourierId)

export class MedievalApp {
  private readonly context: CanvasRenderingContext2D
  private readonly repository = new MedievalWorldRepository()
  private readonly session = new MutableWorldSession()
  private route: MedievalRoute = 'worlds'
  private index: WorldIndex = { version: 1, activeWorlds: [], chronicles: [] }
  private world: FoundationWorld | undefined
  private chronicle: WorldChronicle | undefined
  private selectedRow = 0
  private seed = 'jomon-foundation'
  private persistence: PersistenceState = 'loading'
  private error: string | undefined

  constructor(private readonly canvas: HTMLCanvasElement) {
    const context = canvas.getContext('2d')
    if (!context) throw new Error('Jomon needs a canvas-capable browser')
    this.context = context
    canvas.width = width
    canvas.height = height
    canvas.addEventListener('pointerdown', () => canvas.focus())
    window.addEventListener('keydown', event => this.handleKey(event))
  }

  async start(): Promise<void> {
    await this.refreshIndex()
    this.render()
  }

  private async refreshIndex(): Promise<void> {
    this.persistence = 'loading'
    this.render()
    try {
      this.index = await this.repository.loadIndex()
      this.persistence = 'saved'
      this.error = undefined
    } catch (error) {
      this.persistence = 'error'
      this.error = errorMessage(error)
    }
  }

  private async createWorld(): Promise<void> {
    const world = createFoundationWorld({ seed: this.seed })
    this.world = world
    this.selectedRow = 0
    this.persistence = 'loading'
    this.render()
    try {
      await this.repository.saveWorld(world)
      await this.refreshIndex()
      this.session.open(world.id)
      this.route = 'choose-courier'
    } catch (error) {
      this.persistence = 'error'
      this.error = errorMessage(error)
    }
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
      this.route = world.manifest.initialCourierId ? 'world' : 'choose-courier'
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
    link.download = `${this.chronicle.world.manifest.label.toLowerCase().replace(/[^a-z0-9]+/g, '-')}-chronicle.json`
    link.click()
    URL.revokeObjectURL(url)
  }

  private handleKey(event: KeyboardEvent): void {
    if (event.metaKey || event.ctrlKey || event.altKey) return
    const key = event.key
    if (['ArrowUp', 'ArrowDown', 'Enter', 'Escape', 'Backspace', 'n', 'N', 'c', 'C', 'e', 'E'].includes(key) || key.length === 1) event.preventDefault()
    if (this.persistence === 'loading') return
    if (this.route === 'worlds') {
      if (key === 'n' || key === 'N') { this.route = 'create-world'; this.seed = 'jomon-foundation'; this.selectedRow = 0; this.render(); return }
      if (key === 'c' || key === 'C') { this.route = 'chronicles'; this.selectedRow = 0; this.render(); return }
      if (key === 'ArrowUp') { this.selectedRow = Math.max(0, this.selectedRow - 1); this.render(); return }
      if (key === 'ArrowDown') { this.selectedRow = Math.min(Math.max(0, this.index.activeWorlds.length - 1), this.selectedRow + 1); this.render(); return }
      if (key === 'Enter') void this.resumeSelectedWorld()
      return
    }
    if (this.route === 'create-world') {
      if (key === 'Escape') { this.route = 'worlds'; this.render(); return }
      if (key === 'Backspace') { this.seed = this.seed.slice(0, -1); this.render(); return }
      if (key === 'Enter') { void this.createWorld(); return }
      if (key.length === 1 && this.seed.length < 64 && /[\w .,'-]/u.test(key)) { this.seed += key; this.render() }
      return
    }
    if (this.route === 'choose-courier') {
      const eligible = this.world?.crew.filter(member => member.eligible) ?? []
      if (key === 'Escape') { this.releaseWorld(); this.route = 'worlds'; this.world = undefined; this.render(); return }
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

  private releaseWorld(): void {
    if (this.world && this.session.current() === this.world.id) this.session.release(this.world.id)
  }

  private render(): void {
    const context = this.context
    context.fillStyle = colors.ink
    context.fillRect(0, 0, width, height)
    context.fillStyle = colors.panel
    context.fillRect(20, 20, width - 40, height - 40)
    context.strokeStyle = colors.line
    context.strokeRect(20.5, 20.5, width - 41, height - 41)
    context.font = '18px ui-monospace, SFMono-Regular, Menlo, Consolas, monospace'
    context.textBaseline = 'top'
    context.fillStyle = colors.gold
    context.fillText('JOMON', left, 38)
    context.fillStyle = colors.dim
    context.fillText('RIVER AND COAST // FOUNDATION', left + 108, 39)
    rule(context, 0)
    this.canvas.dataset.route = this.route
    this.canvas.dataset.persistence = this.persistence
    delete this.canvas.dataset.worldId
    delete this.canvas.dataset.crewCount
    if (this.world) {
      this.canvas.dataset.worldId = this.world.id
      this.canvas.dataset.crewCount = String(this.world.crew.length)
    }
    if (this.route === 'worlds') this.renderWorlds(context)
    else if (this.route === 'create-world') this.renderCreateWorld(context)
    else if (this.route === 'choose-courier') this.renderCourierChoice(context)
    else if (this.route === 'world') this.renderWorld(context)
    else if (this.route === 'chronicles') this.renderChronicles(context)
    else this.renderChronicle(context)
    if (this.error) row(context, 22, `LOCAL STORAGE: ${this.error}`, colors.red)
  }

  private renderWorlds(context: CanvasRenderingContext2D): void {
    this.canvas.setAttribute('aria-label', `Jomon worlds. ${this.index.activeWorlds.length} active world${this.index.activeWorlds.length === 1 ? '' : 's'} and ${this.index.chronicles.length} finalized chronicle${this.index.chronicles.length === 1 ? '' : 's'}.`)
    row(context, 2, 'LOCAL WORLDS', colors.gold)
    if (!this.index.activeWorlds.length) row(context, 4, 'No active worlds. Create a seeded river world to begin.', colors.dim)
    this.index.activeWorlds.forEach((entry, index) => row(context, 4 + index, `${index === this.selectedRow ? '>' : ' '} ${entry.label}  ${entry.initialCourierId ? 'ACTIVE' : 'CHOOSE COURIER'}`, index === this.selectedRow ? colors.green : colors.text))
    rule(context, 14)
    row(context, 16, 'N  create world     ENTER  resume selected world', colors.green)
    row(context, 17, 'C  inspect finalized chronicles', colors.green)
    row(context, 19, 'Foundation only: seeded crew and local persistence are ready.', colors.dim)
    row(context, 20, 'The walkable deck, trade, travel, simulation, and combat follow the roadmap.', colors.dim)
  }

  private renderCreateWorld(context: CanvasRenderingContext2D): void {
    this.canvas.setAttribute('aria-label', `Create a seeded Jomon world. Current seed ${this.seed}.`)
    row(context, 2, 'CREATE SEEDED WORLD', colors.gold)
    row(context, 4, 'Seed', colors.dim)
    row(context, 5, `> ${this.seed}_`, colors.green)
    row(context, 8, 'Enter creates the deterministic foundation world.', colors.text)
    row(context, 9, 'Changing seed or generation settings changes the household; no individual rerolls.', colors.dim)
    row(context, 11, 'Esc returns without changing local worlds.', colors.dim)
  }

  private renderCourierChoice(context: CanvasRenderingContext2D): void {
    const world = this.world
    if (!world) { this.route = 'worlds'; this.render(); return }
    this.canvas.setAttribute('aria-label', `Choose an initial courier for ${world.manifest.label}. ${world.crew.length} generated crew members are eligible.`)
    row(context, 2, `CHOOSE INITIAL COURIER // ${world.manifest.label.toUpperCase()}`, colors.gold)
    row(context, 3, `SEED ${world.manifest.seed} // time remains 0`, colors.dim)
    world.crew.filter(member => member.eligible).forEach((member, index) => {
      const color = index === this.selectedRow ? colors.green : colors.text
      row(context, 5 + index * 2, `${index === this.selectedRow ? '>' : ' '} ${member.name.toUpperCase()} // ${member.role.toUpperCase()} // CONVERSATION ${member.conversation}`, color)
      row(context, 6 + index * 2, `   ${member.history}.`, colors.dim)
    })
    row(context, 19, 'Arrow keys choose. Enter confirms. Esc returns to local worlds.', colors.green)
  }

  private renderWorld(context: CanvasRenderingContext2D): void {
    const world = this.world
    if (!world) { this.route = 'worlds'; this.render(); return }
    const courier = activeCourier(world)
    this.canvas.setAttribute('aria-label', `Jomon foundation world ${world.manifest.label}, active courier ${courier?.name ?? 'unassigned'}.`)
    row(context, 2, `${world.manifest.label.toUpperCase()} // FOUNDATION WORLD`, colors.gold)
    row(context, 4, `ACTIVE COURIER  ${courier?.name.toUpperCase() ?? 'UNASSIGNED'} // ${courier?.role.toUpperCase() ?? 'NONE'}`, colors.green)
    row(context, 5, `SEED  ${world.manifest.seed} // WORLD TIME ${world.worldTime}`, colors.text)
    row(context, 7, 'The household exists. Time has not advanced and no map has been generated yet.', colors.dim)
    row(context, 8, 'This is the clean persistence boundary before the walkable Jomon foundation.', colors.dim)
    rule(context, 14)
    row(context, 16, 'Esc returns to local worlds. All gameplay actions are still roadmap work.', colors.green)
  }

  private renderChronicles(context: CanvasRenderingContext2D): void {
    this.canvas.setAttribute('aria-label', `${this.index.chronicles.length} finalized Jomon chronicles.`)
    row(context, 2, 'FINALIZED CHRONICLES', colors.gold)
    if (!this.index.chronicles.length) row(context, 4, 'No finalized chronicles are stored locally.', colors.dim)
    this.index.chronicles.forEach((entry, index) => row(context, 4 + index, `${index === this.selectedRow ? '>' : ' '} ${entry.label} // ${entry.reason.toUpperCase()}`, index === this.selectedRow ? colors.green : colors.text))
    rule(context, 14)
    row(context, 16, 'Arrow keys choose. Enter inspects. Esc returns to local worlds.', colors.green)
  }

  private renderChronicle(context: CanvasRenderingContext2D): void {
    const chronicle = this.chronicle
    if (!chronicle) { this.route = 'chronicles'; this.render(); return }
    this.canvas.setAttribute('aria-label', `Read-only Jomon chronicle ${chronicle.world.manifest.label}, finalized by ${chronicle.reason}.`)
    row(context, 2, `${chronicle.world.manifest.label.toUpperCase()} // READ-ONLY CHRONICLE`, colors.gold)
    row(context, 4, `FINAL REASON  ${chronicle.reason.toUpperCase()}`, colors.red)
    row(context, 5, `SEED  ${chronicle.world.manifest.seed} // WORLD TIME ${chronicle.world.worldTime}`, colors.text)
    chronicle.world.causalHistory.forEach((record, index) => row(context, 7 + index, `${record.sequence}. ${record.detail}`, colors.dim))
    rule(context, 14)
    row(context, 16, 'E exports JSON. Esc returns to finalized chronicles.', colors.green)
  }
}
