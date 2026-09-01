import { describe, expect, it } from 'vitest'
import { createInitialFrontierState } from './frontier'
import type { WorldGenerationConfigRequest } from './generation-config'
import { createFoundationWorld } from './world'

interface SnapshotFixture {
  seed: string
  configuration: WorldGenerationConfigRequest
}

/**
 * Deliberately small projections: stable IDs/counts and causal edges are
 * evidence of a generated world, unlike a brittle full object dump.
 */
const fixtures: readonly SnapshotFixture[] = [
  { seed: 'snapshot-reed-17', configuration: { preset: 'watershed' } },
  {
    seed: 'snapshot-coast-43',
    configuration: {
      preset: 'far-coast',
      advanced: { climate: 'temperate', historyYears: 350, settlementDensity: 4, populationDensity: 4 }
    }
  }
]

const projectionFor = ({ seed, configuration }: SnapshotFixture) => {
  const world = createFoundationWorld({ seed, configuration })
  const initial = world.initialWorld
  const frontier = createInitialFrontierState({
    seed: world.manifest.creation.seed,
    configuration: world.manifest.creation.resolvedConfiguration,
    initialWorld: initial
  })
  const settlement = initial.settlements[0]!
  const route = initial.routes[0]!
  const history = initial.history[0]!
  return {
    seed: world.manifest.creation.seed,
    worldId: world.id,
    manifestDigest: world.manifest.creation.digest,
    selected: world.manifest.creation.selectedConfiguration,
    candidate: {
      selectedAttempt: world.manifest.creation.validationHistory.initialWorld.selectedAttempt,
      candidates: world.manifest.creation.validationHistory.initialWorld.candidates.map(candidate => ({
        attempt: candidate.attempt,
        status: candidate.status,
        issueCodes: candidate.issues.map(issue => issue.code)
      }))
    },
    stages: {
      waterways: initial.waterways.length,
      seasons: initial.seasons.length,
      resources: initial.resources.length,
      ecologies: initial.ecologies.length,
      settlements: initial.settlements.length,
      institutions: initial.institutions.length,
      people: initial.people.length,
      routes: initial.routes.length,
      tradeLinks: initial.tradeLinks.length,
      routeHazards: initial.routeHazards.length,
      history: initial.history.length
    },
    causalLinks: {
      settlement: { id: settlement.id, waterwayId: settlement.waterwayId, resourceIds: settlement.resourceIds, ecologyId: settlement.ecologyId },
      route: { id: route.id, from: route.originSettlementId, to: route.destinationSettlementId, waterwayIds: route.waterwayIds },
      history: { id: history.id, personId: history.personId, institutionId: history.institutionId, settlementId: history.settlementId, routeId: history.routeId }
    },
    frontier: frontier.regions.map(region => ({
      id: region.commitment.id,
      coordinate: region.commitment.coordinate,
      order: region.commitment.generationOrder,
      connection: region.commitment.connection.kind,
      ...(region.status === 'ungenerated' ? {} : {
        fact: {
          kind: region.revealedFacts[0]!.kind,
          value: region.revealedFacts[0]!.value,
          source: region.revealedFacts[0]!.source.kind
        }
      })
    })),
    crew: world.crew.slice(0, 2).map(member => ({ id: member.id, name: member.name, role: member.role, conversation: member.conversation }))
  }
}

describe('medieval generation snapshots', () => {
  it('retains readable, intentional fixed-seed generation projections', () => {
    expect(fixtures.map(projectionFor)).toEqual([
      {
        seed: 'snapshot-reed-17',
        worldId: 'world:8xjiy6-1u8otnw',
        manifestDigest: '8xjiy6-1u8otnw',
        selected: { preset: 'watershed', advanced: {} },
        candidate: { selectedAttempt: 0, candidates: [{ attempt: 0, status: 'accepted', issueCodes: [] }] },
        stages: { waterways: 6, seasons: 4, resources: 5, ecologies: 3, settlements: 4, institutions: 12, people: 24, routes: 3, tradeLinks: 3, routeHazards: 3, history: 5 },
        causalLinks: {
          settlement: { id: 'initial:settlement:0', waterwayId: 'initial:waterway:tributary:0', resourceIds: ['initial:resource:0'], ecologyId: 'initial:ecology:0' },
          route: { id: 'initial:route:0', from: 'initial:settlement:0', to: 'initial:settlement:1', waterwayIds: ['initial:waterway:tributary:0', 'initial:waterway:tributary:1'] },
          history: { id: 'initial:history:0', personId: 'initial:person:0', institutionId: 'initial:institution:0', settlementId: 'initial:settlement:0', routeId: 'initial:route:0' }
        },
        frontier: [
          { id: 'frontier:region:initial:world:1gxnr9n:-1:0', coordinate: { x: -1, y: 0 }, order: 0, connection: 'initial-world-link' },
          { id: 'frontier:region:initial:world:1gxnr9n:1:0', coordinate: { x: 1, y: 0 }, order: 1, connection: 'initial-world-link', fact: { kind: 'region-name', value: 'Candle Marsh', source: 'chart' } },
          { id: 'frontier:region:initial:world:1gxnr9n:2:0', coordinate: { x: 2, y: 0 }, order: 2, connection: 'parent-region-link' }
        ],
        crew: [{ id: 'crew:0', name: 'Kesan Silt', role: 'guard', conversation: 1 }, { id: 'crew:1', name: 'Jorian Cairn', role: 'pilot', conversation: 1 }]
      },
      {
        seed: 'snapshot-coast-43',
        worldId: 'world:rsk7f9-6977gh',
        manifestDigest: 'rsk7f9-6977gh',
        selected: { preset: 'far-coast', advanced: { historyYears: 350, climate: 'temperate', settlementDensity: 4, populationDensity: 4 } },
        candidate: { selectedAttempt: 0, candidates: [{ attempt: 0, status: 'accepted', issueCodes: [] }] },
        stages: { waterways: 5, seasons: 4, resources: 4, ecologies: 4, settlements: 5, institutions: 20, people: 40, routes: 4, tradeLinks: 4, routeHazards: 4, history: 5 },
        causalLinks: {
          settlement: { id: 'initial:settlement:0', waterwayId: 'initial:waterway:tributary:0', resourceIds: ['initial:resource:0'], ecologyId: 'initial:ecology:0' },
          route: { id: 'initial:route:0', from: 'initial:settlement:0', to: 'initial:settlement:1', waterwayIds: ['initial:waterway:tributary:0', 'initial:waterway:tributary:1'] },
          history: { id: 'initial:history:0', personId: 'initial:person:0', institutionId: 'initial:institution:0', settlementId: 'initial:settlement:0', routeId: 'initial:route:0' }
        },
        frontier: [
          { id: 'frontier:region:initial:world:dnznri:-1:0', coordinate: { x: -1, y: 0 }, order: 0, connection: 'initial-world-link' },
          { id: 'frontier:region:initial:world:dnznri:1:0', coordinate: { x: 1, y: 0 }, order: 1, connection: 'initial-world-link', fact: { kind: 'region-name', value: 'Hearth Marsh', source: 'chart' } },
          { id: 'frontier:region:initial:world:dnznri:2:0', coordinate: { x: 2, y: 0 }, order: 2, connection: 'parent-region-link' }
        ],
        crew: [{ id: 'crew:0', name: 'Daraor Dike', role: 'carter', conversation: 2 }, { id: 'crew:1', name: 'Ivenis Wren', role: 'bargemaster', conversation: 4 }]
      }
    ])
  })
})
