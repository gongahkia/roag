import { describe, expect, it } from 'vitest'
import { AUTOPLAY_MAX_TURNS, autoplayCandidateDiagnostics, autoplayCommand, autoplayDecision, autoplayRecoveryFingerprint, autoplayStateFingerprint, createAutoplayContext, recordAutoplayTransition, snapshotAutoplayTransition } from './autoplay'
import { runAutoplay } from './autoplay-runner'
import { newRun, perform, skillChoices } from './engine'
import { propDefinition } from './props'
import { createEnemy, createRun } from './test/factories'

describe('autoplay', () => {
  it('does not mutate planning state and resolves level-up choices', () => {
    const state = newRun(71)
    const before = structuredClone(state)
    expect(autoplayCommand(state, 'visible')).toBeDefined()
    expect(state).toEqual(before)
    state.modal = { kind: 'skills', source: 'level' }
    const decision = autoplayDecision(state, 'omniscient', 'clear', createAutoplayContext())
    expect(skillChoices(state).map((_, index) => String(index + 1))).toContain(decision?.command)
  })

  it('tracks temporary prop fields without mutating them during planning', () => {
    const state = newRun(71, 'ruins')
    const definition = propDefinition('ruins.monolith')
    state.floor.props = [{ id: 'temporary-monolith', kind: definition.id, biome: definition.biome, x: state.hero.x + 1, y: state.hero.y, state: 'activated', tags: [...definition.tags], hooks: [...definition.hooks], effectCells: [{ x: state.hero.x + 1, y: state.hero.y }, { x: state.hero.x + 2, y: state.hero.y }], expiresAt: state.turn + 4 }]
    const before = structuredClone(state)
    const initial = snapshotAutoplayTransition(state)
    state.floor.props[0].effectCells = [{ x: state.hero.x + 1, y: state.hero.y }]
    const changedCells = snapshotAutoplayTransition(state)
    state.floor.props[0].expiresAt = state.turn + 5
    const changedExpiry = autoplayStateFingerprint(state)
    expect(changedCells.stateKey).not.toBe(initial.stateKey)
    expect(changedCells.progressKey).not.toBe(initial.progressKey)
    expect(changedExpiry).not.toBe(changedCells.stateKey)
    state.floor.props = before.floor.props
    autoplayDecision(state, 'omniscient', 'clear', createAutoplayContext())
    expect(state).toEqual(before)
  })

  it('persists a safe two-step prop plan for a Wilds parcel', () => {
    const state = createRun()
    const definition = propDefinition('wilds.lostParcel')
    state.floor.objective = { id: 'missing-guardian', kind: 'defeatGuardian', label: 'Defeat the guardian', status: 'active' }
    state.floor.props = [{ id: 'lost-parcel', kind: definition.id, biome: definition.biome, x: state.hero.x + 1, y: state.hero.y, state: 'dormant', tags: [...definition.tags], hooks: [...definition.hooks] }]
    const context = createAutoplayContext()
    expect(autoplayDecision(state, 'omniscient', 'survival', context)).toMatchObject({ command: 'c', reason: 'inspect prop:wilds.lostParcel' })
    perform(state, 'c')
    expect(state.turn).toBe(0)
    expect(autoplayDecision(state, 'omniscient', 'survival', context)).toMatchObject({ command: 'c', reason: 'operate prop:wilds.lostParcel' })
    perform(state, 'c')
    expect(state.floor.items).toContainEqual(expect.objectContaining({ id: 'ropeBundle' }))
  })

  it('routes to and opens an otherwise sealed root arch with its equipped tool', () => {
    const state = createRun()
    const definition = propDefinition('wilds.rootArch')
    state.floor.tiles.forEach(tile => { tile.kind = 'wall' })
    for (let x = 1; x <= 5; x++) state.floor.tiles[1 * 48 + x].kind = 'floor'
    state.hero.equipment.mainHand = 'machete'
    state.floor.actors = [{ ...createEnemy(), id: 'arch-guardian', role: 'guardian', x: 5, y: 1, health: 30 }]
    state.floor.objective = { id: 'arch-guardian-objective', kind: 'defeatGuardian', label: 'Defeat the guardian', status: 'active' }
    state.floor.props = [{ id: 'root-arch', kind: definition.id, biome: definition.biome, x: 2, y: 1, state: 'dormant', tags: [...definition.tags], hooks: [...definition.hooks] }]
    const context = createAutoplayContext()
    expect(autoplayDecision(state, 'omniscient', 'clear', context)).toMatchObject({ command: 'c', reason: 'inspect prop:wilds.rootArch' })
    perform(state, 'c')
    expect(autoplayDecision(state, 'omniscient', 'clear', context)).toMatchObject({ command: 'c', reason: 'operate prop:wilds.rootArch' })
    perform(state, 'c')
    expect(state.floor.props[0].state).toBe('activated')
  })

  it('does not inspect an optional hazardous brazier without tactical pressure', () => {
    const state = createRun()
    const definition = propDefinition('ruins.ritualBrazier')
    state.floor.objective = { id: 'missing-guardian', kind: 'defeatGuardian', label: 'Defeat the guardian', status: 'active' }
    state.floor.props = [{ id: 'brazier', kind: definition.id, biome: definition.biome, x: state.hero.x + 1, y: state.hero.y, state: 'dormant', tags: [...definition.tags], hooks: [...definition.hooks] }]
    expect(autoplayCandidateDiagnostics(state, 'omniscient', 'survival', createAutoplayContext()).some(candidate => candidate.reason.includes('ritualBrazier'))).toBe(false)
  })

  it('uses its final rope to open a boat route required by the guardian objective', () => {
    const state = createRun()
    const definition = propDefinition('caverns.brokenBoat')
    state.floor.tiles.forEach(tile => { tile.kind = 'wall' })
    for (let x = 1; x <= 5; x++) state.floor.tiles[1 * 48 + x].kind = 'floor'
    state.hero.ropes = 1
    state.floor.actors = [{ ...createEnemy(), id: 'boat-guardian', role: 'guardian', x: 5, y: 1, health: 30 }]
    state.floor.objective = { id: 'boat-guardian-objective', kind: 'defeatGuardian', label: 'Defeat the guardian', status: 'active' }
    state.floor.props = [{ id: 'boat', kind: definition.id, biome: definition.biome, x: 2, y: 1, state: 'dormant', tags: [...definition.tags], hooks: [...definition.hooks] }]
    const context = createAutoplayContext()
    expect(autoplayDecision(state, 'omniscient', 'survival', context)).toMatchObject({ command: 'c', reason: 'inspect prop:caverns.brokenBoat' })
    perform(state, 'c')
    expect(autoplayDecision(state, 'omniscient', 'survival', context)).toMatchObject({ command: 'r', reason: 'secure prop route:caverns.brokenBoat' })
    perform(state, 'r')
    expect(state.floor.props[0].state).toBe('activated')
    expect(state.hero.ropes).toBe(0)
  })

  it('uses a rope to release a required inspected mine cart onto a side rail', () => {
    const state = createRun()
    const definition = propDefinition('mine.brokenCart')
    state.floor.tiles.forEach(tile => { tile.kind = 'wall' })
    for (const [x, y, kind] of [[1, 1, 'rail'], [2, 1, 'rail'], [3, 1, 'rail'], [3, 2, 'floor'], [4, 2, 'floor'], [5, 2, 'floor']] as const) state.floor.tiles[y * 48 + x].kind = kind
    state.hero.ropes = 1
    state.floor.actors = [{ ...createEnemy(), id: 'cart-guardian', role: 'guardian', x: 5, y: 2, health: 30 }]
    state.floor.objective = { id: 'cart-guardian-objective', kind: 'defeatGuardian', label: 'Defeat the guardian', status: 'active' }
    state.floor.props = [{ id: 'cart', kind: definition.id, biome: definition.biome, x: 2, y: 1, state: 'dormant', tags: [...definition.tags], hooks: [...definition.hooks] }]
    const context = createAutoplayContext()
    expect(autoplayDecision(state, 'omniscient', 'clear', context)).toMatchObject({ command: 'c', reason: 'inspect prop:mine.brokenCart' })
    perform(state, 'c')
    expect(autoplayDecision(state, 'omniscient', 'clear', context)).toMatchObject({ command: 'r', reason: 'secure prop route:mine.brokenCart' })
  })

  it('uses a rope for a required collapsed arch but not an optional one', () => {
    const state = createRun()
    const definition = propDefinition('ruins.collapsedArch')
    state.floor.tiles.forEach(tile => { tile.kind = 'wall' })
    for (let x = 1; x <= 5; x++) state.floor.tiles[1 * 48 + x].kind = 'floor'
    state.hero.ropes = 1
    state.floor.actors = [{ ...createEnemy(), id: 'arch-rope-guardian', role: 'guardian', x: 5, y: 1, health: 30 }]
    state.floor.objective = { id: 'arch-rope-guardian-objective', kind: 'defeatGuardian', label: 'Defeat the guardian', status: 'active' }
    state.floor.props = [{ id: 'arch', kind: definition.id, biome: definition.biome, x: 2, y: 1, state: 'inspected', tags: [...definition.tags], hooks: [...definition.hooks] }]
    expect(autoplayDecision(state, 'omniscient', 'clear', createAutoplayContext())).toMatchObject({ command: 'r', reason: 'secure prop route:ruins.collapsedArch' })
    state.floor.tiles.forEach(tile => { tile.kind = 'floor' })
    expect(autoplayCandidateDiagnostics(state, 'omniscient', 'clear', createAutoplayContext()).some(candidate => candidate.reason === 'secure prop route:ruins.collapsedArch')).toBe(false)
  })

  it('does not offer an unresourced boat rope plan', () => {
    const state = createRun()
    const definition = propDefinition('caverns.brokenBoat')
    state.hero.ropes = 0
    state.floor.props = [{ id: 'boat', kind: definition.id, biome: definition.biome, x: state.hero.x + 1, y: state.hero.y, state: 'inspected', tags: [...definition.tags], hooks: [...definition.hooks] }]
    expect(autoplayCandidateDiagnostics(state, 'omniscient', 'survival', createAutoplayContext()).some(candidate => candidate.reason.includes('secure prop route:caverns.brokenBoat'))).toBe(false)
  })

  it('uses root-shrine screens and statue force only when they stop hostile projectile lines', () => {
    const root = createRun()
    const shrine = propDefinition('wilds.rootShrine')
    root.hero.inventory = ['root']
    root.hero.conditions = [{ kind: 'rooted', duration: 2, potency: 1 }]
    root.floor.actors = [{ ...createEnemy(), id: 'root-source', ai: 'ranged', x: 5, y: 1 }]
    root.floor.actors[0].role = 'guardian'
    root.floor.objective = { id: 'root-objective', kind: 'defeatGuardian', label: 'Defeat the guardian', status: 'active' }
    root.floor.tiles[0 * 48 + 1].kind = 'wall'
    root.floor.tiles[0 * 48 + 2].kind = 'wall'
    root.floor.tiles[0 * 48 + 3].kind = 'wall'
    root.floor.props = [{ id: 'root-shrine', kind: shrine.id, biome: shrine.biome, x: 2, y: 1, state: 'inspected', tags: [...shrine.tags], hooks: [...shrine.hooks] }]
    const rootContext = createAutoplayContext()
    rootContext.propPlanId = 'root-shrine'
    expect(autoplayDecision(root, 'omniscient', 'survival', rootContext)).toMatchObject({ command: 'c', reason: 'operate prop:wilds.rootShrine' })

    const rootCharm = createRun()
    rootCharm.hero.inventory = ['root']
    rootCharm.floor.actors = [{ ...createEnemy(), id: 'root-charm-source', ai: 'ranged', x: 5, y: 1 }]
    rootCharm.floor.actors[0].role = 'guardian'
    rootCharm.floor.objective = { id: 'root-charm-objective', kind: 'defeatGuardian', label: 'Defeat the guardian', status: 'active' }
    rootCharm.floor.tiles[0 * 48 + 2].kind = 'wall'
    rootCharm.floor.tiles[0 * 48 + 3].kind = 'wall'
    rootCharm.floor.tiles[0 * 48 + 4].kind = 'wall'
    rootCharm.floor.props = [{ id: 'root-shrine', kind: shrine.id, biome: shrine.biome, x: 3, y: 1, state: 'dormant', tags: [...shrine.tags], hooks: [...shrine.hooks] }]
    expect(autoplayCandidateDiagnostics(rootCharm, 'omniscient', 'clear', createAutoplayContext())).toContainEqual(expect.objectContaining({ command: 'u', reason: 'cast prop:root' }))

    const statue = createRun()
    const statueDefinition = propDefinition('ruins.brokenStatue')
    statue.hero.inventory = ['gust']
    statue.floor.actors = [{ ...createEnemy(), id: 'statue-source', ai: 'ranged', x: 5, y: 1 }]
    statue.floor.actors[0].role = 'guardian'
    statue.floor.objective = { id: 'statue-objective', kind: 'defeatGuardian', label: 'Defeat the guardian', status: 'active' }
    statue.floor.props = [{ id: 'statue', kind: statueDefinition.id, biome: statueDefinition.biome, x: 2, y: 1, state: 'dormant', tags: [...statueDefinition.tags], hooks: [...statueDefinition.hooks] }]
    expect(autoplayCandidateDiagnostics(statue, 'omniscient', 'clear', createAutoplayContext())).toContainEqual(expect.objectContaining({ command: 'u', reason: 'cast prop:gust' }))

    const operatedStatue = createRun()
    operatedStatue.floor.actors = [{ ...createEnemy(), id: 'operated-statue-source', ai: 'ranged', x: 5, y: 1 }]
    operatedStatue.floor.actors[0].role = 'guardian'
    operatedStatue.floor.objective = { id: 'operated-statue-objective', kind: 'defeatGuardian', label: 'Defeat the guardian', status: 'active' }
    operatedStatue.floor.props = [{ id: 'statue', kind: statueDefinition.id, biome: statueDefinition.biome, x: 2, y: 1, state: 'inspected', tags: [...statueDefinition.tags], hooks: [...statueDefinition.hooks] }]
    const statueContext = createAutoplayContext()
    statueContext.propPlanId = 'statue'
    expect(autoplayDecision(operatedStatue, 'omniscient', 'clear', statueContext)).toMatchObject({ command: 'c', reason: 'operate prop:ruins.brokenStatue' })

    const safeStatue = createRun()
    safeStatue.hero.inventory = ['gust']
    safeStatue.floor.props = [{ id: 'statue', kind: statueDefinition.id, biome: statueDefinition.biome, x: 2, y: 1, state: 'dormant', tags: [...statueDefinition.tags], hooks: [...statueDefinition.hooks] }]
    expect(autoplayCandidateDiagnostics(safeStatue, 'omniscient', 'clear', createAutoplayContext()).some(candidate => candidate.reason === 'cast prop:gust')).toBe(false)
  })

  it('refracts a crystal only when it opens a safe usable ranged line', () => {
    const crystal = createRun()
    const definition = propDefinition('caverns.crystalCluster')
    crystal.hero.inventory = ['gust', 'rock']
    crystal.floor.actors = [{ ...createEnemy(), id: 'crystal-target', x: 5, y: 1, ai: 'chase' }]
    crystal.floor.actors[0].role = 'guardian'
    crystal.floor.objective = { id: 'crystal-objective', kind: 'defeatGuardian', label: 'Defeat the guardian', status: 'active' }
    crystal.floor.tiles[0 * 48 + 4].kind = 'wall'
    crystal.floor.tiles[2 * 48 + 4].kind = 'wall'
    crystal.floor.props = [{ id: 'crystal', kind: definition.id, biome: definition.biome, x: 2, y: 1, state: 'dormant', tags: [...definition.tags], hooks: [...definition.hooks] }]
    expect(autoplayCandidateDiagnostics(crystal, 'omniscient', 'clear', createAutoplayContext())).toContainEqual(expect.objectContaining({ command: 'u', reason: 'cast prop:gust' }))

    crystal.hero.inventory = ['gust']
    expect(autoplayCandidateDiagnostics(crystal, 'omniscient', 'clear', createAutoplayContext()).some(candidate => candidate.reason === 'cast prop:gust')).toBe(false)
  })

  it('casts water, force, and fire at prop hooks only when they restore an objective route', () => {
    const routeState = (item: 'waterScript' | 'gust' | 'ember', kind: 'caverns.brokenBoat' | 'ruins.collapsedArch' | 'wilds.rootArch', x: number) => {
      const state = createRun()
      const definition = propDefinition(kind)
      state.floor.tiles.forEach(tile => { tile.kind = 'wall' })
      for (let column = 1; column <= 6; column++) state.floor.tiles[1 * 48 + column].kind = 'floor'
      state.hero.inventory = [item]
      state.floor.actors = [{ ...createEnemy(), id: `${kind}-guardian`, role: 'guardian', x: 6, y: 1, health: 30 }]
      state.floor.objective = { id: `${kind}-objective`, kind: 'defeatGuardian', label: 'Defeat the guardian', status: 'active' }
      state.floor.props = [{ id: kind, kind: definition.id, biome: definition.biome, x, y: 1, state: 'dormant', tags: [...definition.tags], hooks: [...definition.hooks] }]
      return state
    }
    for (const [item, kind, x] of [['waterScript', 'caverns.brokenBoat', 3], ['gust', 'ruins.collapsedArch', 2], ['ember', 'wilds.rootArch', 2]] as const) {
      const state = routeState(item, kind, x)
      expect(autoplayCandidateDiagnostics(state, 'omniscient', 'clear', createAutoplayContext())).toContainEqual(expect.objectContaining({ command: 'u', reason: `cast prop:${item}` }))
      state.modal = { kind: 'target', action: 'spell', item }
      expect(autoplayDecision(state, 'omniscient', 'clear', createAutoplayContext())).toMatchObject({ command: ';', reason: 'spell target' })
      perform(state, ';')
      perform(state, 'Enter')
      expect(state.floor.props[0].state).toMatch(/activated|destroyed/)
    }
  })

  it('targets cache and blocked-route hooks with throws and bombs', () => {
    const thrown = createRun()
    const cache = propDefinition('ruins.sealedCache')
    thrown.hero.inventory = ['rock']
    thrown.floor.props = [{ id: 'cache', kind: cache.id, biome: cache.biome, x: 6, y: 1, state: 'dormant', tags: [...cache.tags], hooks: [...cache.hooks] }]
    expect(autoplayCandidateDiagnostics(thrown, 'omniscient', 'clear', createAutoplayContext())).toContainEqual(expect.objectContaining({ command: 't', reason: 'throw prop:rock' }))
    thrown.modal = { kind: 'target', action: 'throw', item: 'rock' }
    expect(autoplayDecision(thrown, 'omniscient', 'clear', createAutoplayContext())).toMatchObject({ command: ';', reason: 'throw target' })
    perform(thrown, ';')
    perform(thrown, 'Enter')
    expect(thrown.floor.props[0].state).toBe('activated')

    const bombed = createRun()
    const arch = propDefinition('ruins.collapsedArch')
    bombed.floor.tiles.forEach(tile => { tile.kind = 'wall' })
    for (let column = 1; column <= 5; column++) bombed.floor.tiles[1 * 48 + column].kind = 'floor'
    bombed.hero.bombs = 1
    bombed.floor.actors = [{ ...createEnemy(), id: 'bomb-guardian', role: 'guardian', x: 5, y: 1, health: 30 }]
    bombed.floor.objective = { id: 'bomb-objective', kind: 'defeatGuardian', label: 'Defeat the guardian', status: 'active' }
    bombed.floor.props = [{ id: 'arch', kind: arch.id, biome: arch.biome, x: 3, y: 1, state: 'dormant', tags: [...arch.tags], hooks: [...arch.hooks] }]
    expect(autoplayCandidateDiagnostics(bombed, 'omniscient', 'survival', createAutoplayContext())).toContainEqual(expect.objectContaining({ command: 'b', reason: 'bomb prop route' }))
    bombed.modal = { kind: 'target', action: 'bomb' }
    expect(autoplayDecision(bombed, 'omniscient', 'survival', createAutoplayContext())).toMatchObject({ command: ';', reason: 'bomb target' })
    perform(bombed, ';')
    perform(bombed, 'Enter')
    expect(bombed.floor.props[0].state).toBe('destroyed')
  })

  it('uses ward and gate prop hooks only for an imminent defense or completed exit', () => {
    const warded = createRun()
    const monolith = propDefinition('ruins.monolith')
    warded.floor.props = [{ id: 'ward-monolith', kind: monolith.id, biome: monolith.biome, x: 2, y: 1, state: 'dormant', tags: [...monolith.tags], hooks: [...monolith.hooks] }]
    warded.hero.inventory = ['wardScript']
    warded.floor.telegraphs = [{ id: 'ward-threat', sourceId: 'oracle', actionId: 'ritual', cells: [{ x: 3, y: 1 }], danger: 'major', resolveTurn: warded.turn + 3 }]
    expect(autoplayCandidateDiagnostics(warded, 'omniscient', 'survival', createAutoplayContext())).toContainEqual(expect.objectContaining({ command: 'u', reason: 'cast prop:wardScript' }))
    warded.modal = { kind: 'target', action: 'spell', item: 'wardScript' }
    expect(autoplayDecision(warded, 'omniscient', 'survival', createAutoplayContext())).toMatchObject({ command: ';', reason: 'spell target' })
    perform(warded, ';')
    perform(warded, 'Enter')
    expect(warded.floor.props[0]).toMatchObject({ state: 'activated', effectCells: expect.any(Array) })

    const unthreatened = createRun()
    unthreatened.floor.props = [{ id: 'idle-monolith', kind: monolith.id, biome: monolith.biome, x: 2, y: 1, state: 'dormant', tags: [...monolith.tags], hooks: [...monolith.hooks] }]
    unthreatened.hero.inventory = ['wardScript']
    unthreatened.modal = { kind: 'target', action: 'spell', item: 'wardScript' }
    expect(autoplayDecision(unthreatened, 'omniscient', 'survival', createAutoplayContext())).toMatchObject({ command: 'Escape', reason: 'cancel spell' })

    const gated = createRun()
    gated.floor.props = [{ id: 'gate-monolith', kind: monolith.id, biome: monolith.biome, x: 2, y: 1, state: 'dormant', tags: [...monolith.tags], hooks: [...monolith.hooks] }]
    gated.hero.inventory = ['gate']
    gated.floor.objective.status = 'complete'
    gated.floor.guardianDefeated = true
    gated.modal = { kind: 'target', action: 'spell', item: 'gate' }
    expect(autoplayDecision(gated, 'omniscient', 'clear', createAutoplayContext())).toMatchObject({ command: ';', reason: 'spell target' })
    perform(gated, ';')
    perform(gated, 'Enter')
    expect(gated.floor.props[0].state).toBe('activated')
    expect(gated.hero).toMatchObject(gated.floor.exit)
  })

  it('does not target an optional prop hook that creates a hazard', () => {
    const state = createRun()
    const mushrooms = propDefinition('wilds.mushrooms')
    state.hero.inventory = ['ember']
    state.floor.props = [{ id: 'mushrooms', kind: mushrooms.id, biome: mushrooms.biome, x: 2, y: 1, state: 'dormant', tags: [...mushrooms.tags], hooks: [...mushrooms.hooks] }]
    expect(autoplayCandidateDiagnostics(state, 'omniscient', 'survival', createAutoplayContext()).some(candidate => candidate.reason === 'cast prop:ember')).toBe(false)
    state.modal = { kind: 'target', action: 'spell', item: 'ember' }
    expect(autoplayDecision(state, 'omniscient', 'survival', createAutoplayContext())).toMatchObject({ command: 'Escape', reason: 'cancel spell' })
  })

  it('keeps visible-only planning within explored terrain', () => {
    const state = newRun(71)
    state.floor.actors = []
    state.floor.items = []
    state.floor.props = []
    state.floor.objective.status = 'complete'
    state.floor.guardianDefeated = true
    state.floor.tiles.forEach(tile => { tile.explored = false; if (tile.kind === 'crate' || tile.kind === 'chest') tile.kind = 'floor' })
    state.floor.tiles[state.hero.y * 48 + state.hero.x].explored = true
    expect(autoplayCommand(state, 'visible')).toBe('l')
    expect(autoplayCommand(state, 'omniscient')).toBeDefined()
  })

  it('does not route to or attempt non-currency loot with a full pack', () => {
    const state = newRun(71)
    state.hero.inventory = Array.from({ length: 12 }, () => 'rock')
    state.floor.items = [{ id: 'tonic', x: state.hero.x, y: state.hero.y, count: 1 }, { id: 'fireJar', x: state.hero.x + 2, y: state.hero.y, count: 1 }]
    const decision = autoplayDecision(state, 'omniscient', 'clear', createAutoplayContext())
    expect(decision?.command).not.toBe('g')
    expect(decision?.candidates.some(candidate => candidate.reason === 'pickup:tonic')).toBe(false)
    expect(decision?.candidates.some(candidate => candidate.reason === 'reach loot')).toBe(false)
  })

  it('still collects gold with a full pack', () => {
    const state = newRun(71)
    state.hero.inventory = Array.from({ length: 12 }, () => 'rock')
    state.floor.items = [{ id: 'gold', x: state.hero.x, y: state.hero.y, count: 7 }]
    expect(autoplayDecision(state, 'omniscient', 'clear', createAutoplayContext())?.command).toBe('g')
  })

  it('discards only a duplicate to reach required stacked altar cash', () => {
    const state = createRun()
    state.floor.objective = { id: 'altar-objective', kind: 'invokeAltar', label: 'Make a shrine offering', status: 'active' }
    state.hero.inventory = Array.from({ length: 12 }, () => 'focusTonic')
    state.floor.items = [{ id: 'wardScript', x: state.hero.x, y: state.hero.y, count: 1 }, { id: 'gold', x: state.hero.x, y: state.hero.y, count: 25 }]
    const context = createAutoplayContext()
    expect(autoplayDecision(state, 'omniscient', 'clear', context)).toMatchObject({ command: 'd', reason: 'discard for offering:focusTonic' })
    perform(state, 'd')
    expect(autoplayDecision(state, 'omniscient', 'clear', context)).toMatchObject({ reason: 'drop:focusTonic' })
    perform(state, '1')
    expect(autoplayDecision(state, 'omniscient', 'clear', context)).toMatchObject({ command: 'g', reason: 'pickup:wardScript' })
    perform(state, 'g')
    expect(autoplayDecision(state, 'omniscient', 'clear', context)).toMatchObject({ command: 'g', reason: 'pickup:gold' })
  })

  it('descends from a completed exit instead of rejecting the next floor enemy roster', () => {
    const state = newRun(71)
    state.floor.actors = []
    state.floor.objective.status = 'complete'
    state.floor.guardianDefeated = true
    state.hero.x = state.floor.exit.x
    state.hero.y = state.floor.exit.y
    expect(autoplayDecision(state, 'omniscient', 'clear', createAutoplayContext())?.command).toBe('q')
  })

  it('steps onto an altar before operating it', () => {
    const state = newRun(71)
    state.floor.actors = []
    state.floor.items = []
    state.floor.guardianDefeated = true
    state.floor.objective = { id: 'altar-objective', kind: 'invokeAltar', label: 'Make a shrine offering', status: 'active' }
    state.floor.tiles.forEach(tile => { tile.kind = 'floor'; tile.explored = true })
    state.floor.tiles[state.floor.exit.y * 48 + state.floor.exit.x].kind = 'exit'
    state.hero.x = 5
    state.hero.y = 5
    state.hero.gold = 75
    state.floor.tiles[5 * 48 + 6].kind = 'altar'
    expect(autoplayDecision(state, 'omniscient', 'clear', createAutoplayContext())?.command).toBe(';')
    perform(state, ';')
    expect(state.hero).toMatchObject({ x: 6, y: 5 })
    expect(autoplayDecision(state, 'omniscient', 'clear', createAutoplayContext())?.command).toBe('c')
  })

  it('approaches an occupied altar from an adjacent tile', () => {
    const state = newRun(71)
    const keeper = state.floor.actors[0]!
    state.floor.tiles.forEach(tile => { tile.kind = 'floor'; tile.explored = true })
    state.hero.x = 5
    state.hero.y = 5
    state.hero.gold = 75
    state.floor.actors = [{ ...keeper, id: 'shrine-keeper', role: 'ally', hostile: false, name: 'shrine keeper', x: 7, y: 5, health: 99 }]
    state.floor.objective = { id: 'altar-objective', kind: 'invokeAltar', label: 'Make a shrine offering', status: 'active' }
    state.floor.tiles[5 * 48 + 7].kind = 'altar'
    const decision = autoplayDecision(state, 'omniscient', 'clear', createAutoplayContext())
    expect(decision?.command).toBeDefined()
    perform(state, decision!.command)
    expect(Math.max(Math.abs(state.hero.x - 7), Math.abs(state.hero.y - 5))).toBeLessThanOrEqual(1)
    expect(autoplayDecision(state, 'omniscient', 'clear', createAutoplayContext())?.command).toBe('c')
  })

  it('enters a free altar even when another altar has a shrine keeper', () => {
    const state = newRun(71)
    const ally = state.floor.actors[0]!
    state.floor.tiles.forEach(tile => { tile.kind = 'floor'; tile.explored = true })
    state.hero.x = 5
    state.hero.y = 5
    state.hero.gold = 75
    state.floor.actors = [{ ...ally, id: 'distant-shrine-keeper', role: 'ally', hostile: false, x: 20, y: 20, health: 99 }]
    state.floor.objective = { id: 'mixed-altar-objective', kind: 'invokeAltar', label: 'Make a shrine offering', status: 'active' }
    state.floor.tiles[6 * 48 + 6].kind = 'altar'
    state.floor.tiles[20 * 48 + 20].kind = 'altar'
    const decision = autoplayDecision(state, 'omniscient', 'clear', createAutoplayContext())
    expect(decision?.command).toBe('/')
    perform(state, decision!.command)
    expect(state.hero).toMatchObject({ x: 6, y: 6 })
    expect(autoplayDecision(state, 'omniscient', 'clear', createAutoplayContext())?.command).toBe('c')
  })

  it('does not route through a direction consumed by a cooling weapon attack', () => {
    const state = newRun(71)
    const hostile = state.floor.actors.find(actor => actor.hostile)!
    state.floor.tiles.forEach(tile => { tile.kind = 'floor'; tile.explored = true })
    state.floor.props = []
    state.hero.x = 5
    state.hero.y = 5
    state.hero.skills = ['agi2']
    state.hero.equipment.mainHand = 'pickaxe'
    state.hero.cooldowns = { pickaxe: 1 }
    state.floor.actors = [
      { ...hostile, id: 'cross-target', x: 7, y: 4, health: 20 },
      { ...hostile, id: 'guardian', role: 'guardian', x: 15, y: 5, health: 30 }
    ]
    state.floor.objective = { id: 'guardian-objective', kind: 'defeatGuardian', label: 'Pass the trail guardian', status: 'active' }
    const decision = autoplayDecision(state, 'omniscient', 'clear', createAutoplayContext())
    expect(decision?.command).not.toBe('p')
    const beforeTurn = state.turn
    perform(state, decision!.command)
    expect(state.turn).toBe(beforeTurn + 1)
  })

  it('equips a weapon whose shape can clear an adjacent diagonal blocker', () => {
    const state = newRun(71)
    const hostile = state.floor.actors.find(actor => actor.hostile)!
    state.floor.tiles.forEach(tile => { tile.kind = 'floor'; tile.explored = true })
    state.hero.x = 5
    state.hero.y = 5
    state.hero.bombs = 0
    state.hero.skills = ['agi2']
    state.hero.inventory = ['whip']
    state.hero.equipment.mainHand = 'pickaxe'
    state.floor.actors = [
      { ...hostile, id: 'diagonal-blocker', x: 6, y: 6, health: 5 },
      { ...hostile, id: 'guardian', role: 'guardian', x: 15, y: 5, health: 30 }
    ]
    state.floor.objective = { id: 'guardian-objective', kind: 'defeatGuardian', label: 'Pass the trail guardian', status: 'active' }
    const context = createAutoplayContext()
    expect(autoplayDecision(state, 'omniscient', 'clear', context)).toMatchObject({ command: 'e', reason: 'tactical equip:whip' })
    perform(state, 'e')
    perform(state, autoplayDecision(state, 'omniscient', 'clear', context)!.command)
    expect(state.hero.equipment.mainHand).toBe('whip')
    expect(autoplayDecision(state, 'omniscient', 'clear', context)?.reason).toBe('melee:diagonal-blocker')
  })

  it('routes around non-hostile actors instead of issuing a blocked move', () => {
    const state = newRun(71)
    const merchant = state.floor.actors[0]!
    state.floor.tiles.forEach(tile => { tile.kind = 'wall'; tile.explored = true })
    state.hero.x = 1
    state.hero.y = 1
    state.floor.exit = { x: 3, y: 3 }
    for (const [x, y] of [[1, 1], [1, 2], [2, 3], [3, 3]]) state.floor.tiles[y * 48 + x].kind = x === 3 && y === 3 ? 'exit' : 'floor'
    state.floor.tiles[2 * 48 + 2].kind = 'shop'
    state.floor.actors = [{ ...merchant, id: 'route-merchant', role: 'merchant', hostile: false, x: 2, y: 2, health: 99 }]
    state.floor.objective.status = 'complete'
    state.floor.guardianDefeated = true
    const decision = autoplayDecision(state, 'omniscient', 'clear', createAutoplayContext())
    expect(decision?.command).toBe('.')
    const beforeTurn = state.turn
    perform(state, decision!.command)
    expect(state.turn).toBe(beforeTurn + 1)
  })

  it('halts strategic loops even when volatile actor state masks exact repeats', () => {
    const context = createAutoplayContext()
    let state = newRun(71)
    let decision = autoplayDecision(state, 'omniscient', 'clear', context)
    for (let turn = 0; turn < 24 && decision; turn++) {
      const next = structuredClone(state)
      next.turn++
      next.floor.actors[0].energy = turn + 1
      recordAutoplayTransition(context, state, 'l', next)
      state = next
      decision = autoplayDecision(state, 'omniscient', 'clear', context)
    }
    expect(decision).toBeUndefined()
    expect(context.loopRecoveries).toBe(8)
  })

  it('stops a live autoplay session at the hard turn guard', () => {
    const state = newRun(71)
    const context = createAutoplayContext()
    context.startedTurn = state.turn
    state.turn += AUTOPLAY_MAX_TURNS
    expect(autoplayDecision(state, 'omniscient', 'clear', context)).toBeUndefined()
    expect(context.lastReason).toBe(`turn guard:${AUTOPLAY_MAX_TURNS}`)
  })

  it('stops after repeated commands that cannot advance a turn', () => {
    const state = newRun(71)
    const context = createAutoplayContext()
    context.noTurnCommands = 8
    expect(autoplayDecision(state, 'omniscient', 'clear', context)).toBeUndefined()
    expect(context.lastReason).toBe('non-turn guard:8')
  })

  it('stops after repeated recovery at the same strategic state', () => {
    const state = newRun(71)
    const context = createAutoplayContext()
    context.noProgressTurns = 32
    context.recoveryVisits.set(autoplayRecoveryFingerprint(state), 8)
    expect(autoplayDecision(state, 'omniscient', 'clear', context)).toBeUndefined()
    expect(context.lastReason).toBe('recovery cycle guard')
  })

  it('keeps a viable objective route ahead of a cycle-break recovery', () => {
    const state = newRun(71)
    state.floor.actors = []
    state.floor.items = []
    state.floor.tiles.forEach(tile => { tile.kind = 'floor'; tile.explored = true })
    state.hero.x = 2
    state.hero.y = 2
    state.hero.gold = 75
    state.floor.tiles[2 * 48 + 14].kind = 'altar'
    state.floor.objective = { id: 'recovery-altar', kind: 'invokeAltar', label: 'Make a shrine offering', status: 'active' }
    const context = createAutoplayContext()
    context.noProgressTurns = 32
    context.bestStrategicDistance = 0
    const decision = autoplayDecision(state, 'omniscient', 'clear', context)
    expect(decision?.reason).toBe('objective:invokeAltar')
    const before = structuredClone(state)
    perform(state, decision!.command)
    recordAutoplayTransition(context, before, decision!.command, state)
    expect(context.noProgressTurns).toBe(0)
    expect(context.recoveryVisits.size).toBe(1)
  })

  it('evades a Mine telegraph during recovery when every route is blocked', () => {
    const state = createRun()
    state.floor.tiles.forEach(tile => { tile.kind = 'wall' })
    state.floor.tiles[1 * 48 + 1].kind = 'floor'
    state.floor.tiles[1 * 48 + 2].kind = 'floor'
    state.floor.tiles[1 * 48 + 5].kind = 'exit'
    state.floor.exit = { x: 5, y: 1 }
    state.floor.objective.status = 'complete'
    state.floor.guardianDefeated = true
    state.hero.health = 1
    state.floor.telegraphs = [{ id: 'mine-shot', sourceId: 'mine-ranged', actionId: 'enemy-shot', cells: [{ x: 1, y: 1 }], danger: 'major', resolveTurn: state.turn + 1 }]
    const context = createAutoplayContext()
    context.noProgressTurns = 32
    context.recentPositions = Array.from({ length: 12 }, () => '2,1')
    expect(autoplayDecision(state, 'omniscient', 'clear', context)).toMatchObject({ command: ';', reason: 'evade telegraph' })
  })

  it('uses the final bomb when critically threatened', () => {
    const state = newRun(71)
    const hostile = state.floor.actors.find(actor => actor.hostile)!
    state.floor.actors = [hostile]
    state.floor.props = []
    hostile.x = state.hero.x + 1
    hostile.y = state.hero.y
    state.hero.health = 1
    state.hero.bombs = 1
    expect(autoplayDecision(state, 'omniscient', 'clear', createAutoplayContext())?.command).toBe('b')
  })

  it('uses a bomb to break a telegraphed ranged trap', () => {
    const state = newRun(71)
    const hostile = state.floor.actors.find(actor => actor.hostile)!
    state.floor.tiles.forEach(tile => { tile.kind = 'floor'; tile.explored = true })
    state.floor.props = []
    state.hero.x = 5
    state.hero.y = 5
    state.hero.health = 20
    state.hero.maxHealth = 25
    state.hero.bombs = 2
    state.floor.actors = [{ ...hostile, id: 'telegraph-source', ai: 'ranged', x: 4, y: 7, health: 10, attack: 7 }]
    state.floor.telegraphs = [{ id: 'telegraph-source:shot', sourceId: 'telegraph-source', actionId: 'enemy-shot', cells: [{ x: 5, y: 5 }], danger: 'major', resolveTurn: state.turn + 1 }]
    const decision = autoplayDecision(state, 'omniscient', 'clear', createAutoplayContext())
    expect(decision).toMatchObject({ command: 'b', reason: 'bomb telegraph source' })
  })

  it('does not reverse a repeated telegraph-source route', () => {
    const state = newRun(71)
    const hostile = state.floor.actors.find(actor => actor.hostile)!
    state.floor.tiles.forEach(tile => { tile.kind = 'floor'; tile.explored = true })
    state.floor.items = []
    state.floor.props = []
    state.hero.x = 5
    state.hero.y = 6
    state.hero.bombs = 0
    state.hero.skills = []
    state.floor.actors = [{ ...hostile, id: 'telegraph-source', ai: 'ranged', x: 5, y: 1, health: 100, attack: 2 }]
    state.floor.telegraphs = [{ id: 'telegraph-source:shot', sourceId: 'telegraph-source', actionId: 'enemy-shot', cells: [{ x: 5, y: 6 }], danger: 'major', resolveTurn: state.turn + 1 }]
    const context = createAutoplayContext()
    context.recentPositions = ['5,6']
    context.lastTelegraphRoute = { sourceId: 'telegraph-source', from: '4,5', to: '5,6' }
    expect(autoplayCandidateDiagnostics(state, 'omniscient', 'clear', context).find(candidate => candidate.reason === 'clear telegraph source:telegraph-source')).toBeUndefined()
    expect(context.lastTelegraphRoute).toEqual({ sourceId: 'telegraph-source', from: '4,5', to: '5,6' })
  })

  it('uses a tactical action instead of futile movement while rooted', () => {
    const state = newRun(71)
    const hostile = state.floor.actors.find(actor => actor.hostile)!
    state.floor.tiles.forEach(tile => { tile.kind = 'floor'; tile.explored = true })
    state.floor.actors = [{ ...hostile, id: 'rooted-target', x: 7, y: 5, health: 10 }]
    state.hero.x = 5
    state.hero.y = 5
    state.hero.bombs = 1
    state.hero.conditions = [{ kind: 'rooted', duration: 2, potency: 1 }]
    const decision = autoplayDecision(state, 'omniscient', 'clear', createAutoplayContext())
    expect(decision?.reason).toMatch(/^break root:/)
    expect(['b', 't', 'u']).toContain(decision?.command)
  })

  it('falls back to a reachable objective target when the pinned target is sealed off', () => {
    const state = newRun(71)
    state.floor.actors = []
    state.floor.items = []
    state.hero.gold = 75
    state.floor.tiles.forEach(tile => { tile.kind = 'wall'; tile.explored = true })
    state.hero.x = 2
    state.hero.y = 2
    for (let x = 2; x <= 4; x++) state.floor.tiles[2 * 48 + x].kind = 'floor'
    state.floor.tiles[2 * 48 + 5].kind = 'altar'
    state.floor.tiles[10 * 48 + 10].kind = 'altar'
    state.floor.objective = { ...state.floor.objective, kind: 'invokeAltar', label: 'Make a shrine offering', status: 'active' }
    const context = createAutoplayContext()
    context.objectiveId = state.floor.objective.id
    context.objectiveTarget = '10,10'
    autoplayDecision(state, 'omniscient', 'clear', context)
    expect(context.objectiveTarget).toBe('5,2')
  })

  it('keeps pursuing the selected supply cache instead of retargeting each turn', () => {
    const state = newRun(71)
    state.floor.actors = []
    state.floor.items = []
    state.floor.tiles.forEach(tile => { tile.kind = 'floor'; tile.explored = true })
    state.hero.x = 2
    state.hero.y = 2
    state.floor.tiles[2 * 48 + 5].kind = 'crate'
    state.floor.tiles[20 * 48 + 2].kind = 'crate'
    state.floor.objective = { id: 'supply-objective', kind: 'recoverSupplies', label: 'Secure a trail cache', status: 'active' }
    const context = createAutoplayContext()
    context.objectiveId = state.floor.objective.id
    context.objectiveTarget = '2,20'
    const decision = autoplayDecision(state, 'omniscient', 'clear', context)
    expect([',', '.', '/']).toContain(decision?.command)
    expect(context.objectiveTarget).toBe('2,20')
  })

  it('completes the Mine reference run using tactical actions', () => {
    const report = runAutoplay(newRun(7, 'mine'), { mode: 'omniscient', policy: 'clear', turnLimit: 800, chainAreas: false })
    expect(report.outcome).toBe('complete')
    expect(report.trace.some(entry => entry.reason.startsWith('bomb'))).toBe(true)
    expect(report.trace.some(entry => entry.reason.startsWith('throw:') || entry.reason.startsWith('cast:'))).toBe(true)
  }, 60_000)

  it('clears the pressure-detour regression seed across the full campaign', () => {
    const report = runAutoplay(newRun(4), { mode: 'omniscient', policy: 'clear', turnLimit: 3200 })
    expect(report.campaignComplete).toBe(true)
  }, 60_000)

  it('clears the ranged-corridor regression seed across the full campaign', () => {
    const report = runAutoplay(newRun(3), { mode: 'omniscient', policy: 'clear', turnLimit: 3200 })
    expect(report.campaignComplete).toBe(true)
  }, 60_000)

  it('clears the moving-route and mixed-altar regression seed across the full campaign', () => {
    const report = runAutoplay(newRun(16), { mode: 'omniscient', policy: 'clear', turnLimit: 3200 })
    expect(report.campaignComplete).toBe(true)
  }, 60_000)

  it('clears the telegraph-route reversal regression seed across the full campaign', () => {
    const report = runAutoplay(newRun(20), { mode: 'omniscient', policy: 'clear', turnLimit: 3200 })
    expect(report.campaignComplete).toBe(true)
  }, 60_000)

  it('clears the offering-cash regression seed across the full campaign', () => {
    const report = runAutoplay(newRun(26), { mode: 'omniscient', policy: 'clear', turnLimit: 3200 })
    expect(report.campaignComplete).toBe(true)
  }, 60_000)

  it('clears the telegraphed Mine exit regression seed across the full campaign', () => {
    const report = runAutoplay(newRun(50), { mode: 'omniscient', policy: 'clear', turnLimit: 3200 })
    expect(report.campaignComplete).toBe(true)
  }, 60_000)

  it('clears the long telegraph-detour regression seed across the full campaign', () => {
    const report = runAutoplay(newRun(41), { mode: 'omniscient', policy: 'clear', turnLimit: 3200 })
    expect(report.campaignComplete).toBe(true)
  }, 60_000)

  it('clears the rail-tunnel telegraph regression seed across the full campaign', () => {
    const report = runAutoplay(newRun(46), { mode: 'omniscient', policy: 'clear', turnLimit: 3200 })
    expect(report.campaignComplete).toBe(true)
  }, 60_000)

  it('clears the telegraphed guardian-route regression seed across the full campaign', () => {
    const report = runAutoplay(newRun(12), { mode: 'omniscient', policy: 'clear', turnLimit: 3200 })
    expect(report.campaignComplete).toBe(true)
  }, 60_000)

  it('chains completed areas into the next biome by default', () => {
    const state = newRun(7, 'mine', 3)
    state.floor.actors = []
    state.floor.objective.status = 'complete'
    state.floor.guardianDefeated = true
    state.hero.x = state.floor.exit.x
    state.hero.y = state.floor.exit.y
    const chained = runAutoplay(state, { mode: 'omniscient', policy: 'clear', turnLimit: 1 })
    const singleArea = runAutoplay(state, { mode: 'omniscient', policy: 'clear', turnLimit: 1, chainAreas: false })
    expect(chained.completedAreas).toEqual(['mine'])
    expect(chained.finalBiome).toBe('wilds')
    expect(singleArea.outcome).toBe('complete')
    expect(singleArea.finalBiome).toBe('mine')
  })

  it('replays deterministically without mutating its input', () => {
    const state = newRun(913, 'mine')
    const before = structuredClone(state)
    const first = runAutoplay(state, { mode: 'omniscient', turnLimit: 120 })
    const second = runAutoplay(state, { mode: 'omniscient', turnLimit: 120 })
    expect(state).toEqual(before)
    expect(first.outcome).not.toBe('error')
    expect(first.commands.length).toBeGreaterThan(0)
    expect(first).toEqual(second)
  }, 20_000)
})
