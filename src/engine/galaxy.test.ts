import { describe, expect, it } from 'vitest'
import { advanceGalaxyRouteReckoning, createGalaxy, discoverLinkedSites, reconcileGalaxy, recordGalaxyRouteSituations, resolveGalaxyRouteSituations, saveGalaxySite } from './galaxy'
import { ROUTE_RECKONING_WORLD_TICK_UNITS } from './route-reckoning'
import { newHero, newRun } from './run'

describe('persistent galaxy', () => {
  it('builds a deterministic twenty-sector frontier around a single known landing', () => {
    const first = createGalaxy(77, newHero())
    const second = createGalaxy(77, newHero())
    expect(first.sectors).toHaveLength(21)
    expect(Object.keys(first.sites)).toHaveLength(210)
    expect(Object.values(first.sites).filter(site => site.discovered)).toHaveLength(1)
    expect(second.sectors).toEqual(first.sectors)
  })

  it('opens physical links only after a site is surveyed', () => {
    const galaxy = createGalaxy(88, newHero())
    const origin = galaxy.activeSiteId
    const linked = galaxy.sites[origin]!.links
    const next = discoverLinkedSites(galaxy, origin)
    expect(next.sites[origin]).toMatchObject({ completed: true })
    expect(linked.every(id => next.sites[id]!.discovered)).toBe(true)
  })

  it('records local run snapshots and advances the galaxy only through Route Reckoning marks', () => {
    const galaxy = createGalaxy(99, newHero(), 0)
    const run = newRun(99, galaxy.sites[galaxy.activeSiteId]!.biome)
    const saved = saveGalaxySite(galaxy, galaxy.activeSiteId, run, 0)
    const evolved = advanceGalaxyRouteReckoning(saved, ROUTE_RECKONING_WORLD_TICK_UNITS)
    expect(evolved.siteSnapshots[galaxy.activeSiteId]!.run.floor).toEqual(run.floor)
    expect(evolved.routeReckoning).toBe(ROUTE_RECKONING_WORLD_TICK_UNITS)
    expect(evolved.events.length).toBeGreaterThan(saved.events.length)
  })

  it('is invariant to Route Reckoning chunking and ignores legacy wall-clock reconciliation', () => {
    const galaxy = createGalaxy(101, newHero(), 0)
    const chunked = Array.from({ length: 10 }, () => 100).reduce((current, marks) => advanceGalaxyRouteReckoning(current, marks), galaxy)
    const whole = advanceGalaxyRouteReckoning(galaxy, 1_000)
    expect(chunked).toEqual(whole)
    expect(reconcileGalaxy(whole, Number.MAX_SAFE_INTEGER)).toEqual(whole)
  })

  it('keeps deterministic long-session state valid and bounds durable history', () => {
    const initial = createGalaxy(303, newHero(), 0)
    const first = advanceGalaxyRouteReckoning(initial, 90 * ROUTE_RECKONING_WORLD_TICK_UNITS)
    const second = advanceGalaxyRouteReckoning(initial, 90 * ROUTE_RECKONING_WORLD_TICK_UNITS)
    expect(first).toEqual(second)
    expect(first.generalManifest.entries.length).toBeLessThanOrEqual(160)
    expect(new Set(first.generalManifest.entries.map(entry => entry.id)).size).toBe(first.generalManifest.entries.length)
    expect(Object.values(first.sites).every(site => [site.integrity, site.ecology, site.construction, site.supplies, site.salvage].every(value => Number.isInteger(value) && value >= 0 && value <= 100))).toBe(true)
  })

  it('records reported route situations and their later confirmed resolution', () => {
    const galaxy = createGalaxy(705, newHero(), 0)
    const active = recordGalaxyRouteSituations(galaxy, 'site-a::site-b', ['quiet', 'patrol', 'hazard'])
    const resolved = resolveGalaxyRouteSituations(active, 'site-a::site-b', ['quiet', 'patrol', 'hazard'])
    expect(active.generalManifest.entries.slice(-2).map(entry => entry.kind)).toEqual(['routeSituationActivated', 'routeSituationActivated'])
    expect(active.generalManifest.entries.at(-1)).toMatchObject({ source: 'route', payload: { linkId: 'site-a::site-b', chunk: 2, situation: 'hazard', status: 'reported' } })
    expect(resolved.generalManifest.entries.slice(-2).map(entry => entry.kind)).toEqual(['routeSituationResolved', 'routeSituationResolved'])
  })

  it('keeps generic cargo compatible while expiring it through canonical time', () => {
    const initial = createGalaxy(917, newHero(), 0)
    const origin = initial.activeSiteId
    const destination = initial.sites[origin]!.links[0]!
    const surveyed = discoverLinkedSites(initial, origin)
    const accepted = surveyed.contracts.find(contract => contract.destinationSiteId === destination)!
    const expired = advanceGalaxyRouteReckoning(surveyed, accepted.deadlineReckoning + 1)
    expect(expired.contracts.find(contract => contract.id === accepted.id)).toMatchObject({ status: 'failed' })
    expect(expired.generalManifest.entries).toContainEqual(expect.objectContaining({ kind: 'contractExpired', contractId: accepted.id, source: 'contract' }))
  })
})
