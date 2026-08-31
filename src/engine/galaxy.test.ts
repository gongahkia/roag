import { describe, expect, it } from 'vitest'
import { createGalaxy, discoverLinkedSites, GALAXY_HOUR_MS, reconcileGalaxy, saveGalaxySite } from './galaxy'
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

  it('records local run snapshots and evolves the galaxy on sector time', () => {
    const galaxy = createGalaxy(99, newHero(), 0)
    const run = newRun(99, galaxy.sites[galaxy.activeSiteId]!.biome)
    const saved = saveGalaxySite(galaxy, galaxy.activeSiteId, run, 0)
    const evolved = reconcileGalaxy(saved, 12 * GALAXY_HOUR_MS)
    expect(evolved.siteSnapshots[galaxy.activeSiteId]!.run.floor).toEqual(run.floor)
    expect(evolved.sectorDay).toBe(12)
    expect(evolved.events.length).toBeGreaterThan(saved.events.length)
  })

  it('keeps the visible clock continuous between major simulation turns', () => {
    const galaxy = createGalaxy(101, newHero(), 0)
    const hour = reconcileGalaxy(galaxy, GALAXY_HOUR_MS)
    const secondHour = reconcileGalaxy(hour, 2 * GALAXY_HOUR_MS)
    expect(hour.sectorDay).toBe(1)
    expect(secondHour.sectorDay).toBe(2)
    expect(secondHour.events).toHaveLength(hour.events.length)
  })
})
