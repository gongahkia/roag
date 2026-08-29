export interface LoreCodexPage { title: string; lines: readonly string[] }

export const LORE_CODEX_PAGES: readonly LoreCodexPage[] = [
  { title: 'JOMON VOYAGER', lines: [
    'Jomon Voyager is a generation carrier on the final approach to New Edo.',
    'The carrier holds landing crews, route archives, and the people displaced by the long voyage.',
    'Each colony survey adds a safe approach, recovered supplies, and a record for the crews that follow.'
  ] },
  { title: 'THE ROUTE', lines: [
    'The Voyager visits isolated colony worlds before committing to its permanent docking at New Edo.',
    'Landing specialists secure a four-zone survey on every world, then the carrier burns for the next destination.',
    'The route is dangerous, but it is not a mystery to solve: it is work that has to be done.'
  ] },
  { title: 'CREW', lines: [
    'Mission command assigns landings. Navigation maintains the colony approaches and jump windows.',
    'Specialists are not chosen heroes. They are crew members trained to bring a useful report home.',
    'Rescued colonists can join the expedition roster between landings.'
  ] },
  { title: 'TWO DOCTRINES', lines: [
    'PRAGMATISM: preserve resources, secure routes, and make the next landing safer.',
    'IDEALISM: protect possibilities, honor local lives, and risk more for a better future.',
    'Both doctrines can guide the Voyager. Neither is a score or a faction meter.'
  ] },
  { title: 'COLONY ARCHIVE', lines: [
    'Kestrel · Verdant · Pelagos · Orison · Helion.',
    'Nerida · Aerie · Memorial · Halcyon · Borealis.',
    'Every colony is a different world with a distinct landing environment and a route worth recording.'
  ] },
  { title: 'VOYAGER PROTOCOL', lines: [
    'A landing report is a promise to the next crew: conditions observed, risks named, and no false certainty.',
    'The archive contains mission logs, recovered route data, and records from previous expeditions.',
    'New Edo is the Voyager’s intended home, not a reward that makes the journey meaningless.'
  ] }
]
