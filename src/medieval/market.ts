import { JOMON_COMMODITY_IDS, type JomonCommodityId } from './commodity-catalogue'
import { hashSeed } from './rng'
import { SETTLEMENT_TRADING_LOCATION_ID, type SettlementTradingState } from './settlement-trading'

/**
 * Local market ledgers are bounded facts, not an exchange, wallet, or shop.
 * The public tally's ironwork condition is the only current mutable market
 * consequence; later economy owners must version any broader mutation rule.
 */
export const WORLD_MARKETS_CONTRACT_VERSION = 2 as const
export const LEGACY_WORLD_MARKETS_CONTRACT_VERSION = 1 as const
export const HEARTHFORD_PUBLIC_TALLY_MARKET_ID = 'market:settlement-location:hearthford-mill-quay' as const

export const MARKET_STOCK_BANDS = ['none', 'limited', 'available'] as const
export type MarketStockBand = typeof MARKET_STOCK_BANDS[number]
export const MARKET_DEMAND_BANDS = ['low', 'steady', 'high'] as const
export type MarketDemandBand = typeof MARKET_DEMAND_BANDS[number]
export const MARKET_PRICE_BANDS = ['low', 'fair', 'high'] as const
export type MarketPriceBand = typeof MARKET_PRICE_BANDS[number]

export interface WorldMarketCommodityState {
  commodityId: JomonCommodityId
  stock: MarketStockBand
  demand: MarketDemandBand
  price: MarketPriceBand
}

export interface SiteWorldMarketState {
  id: string
  location: { kind: 'site'; id: string }
  commodityStates: readonly WorldMarketCommodityState[]
}

export interface PublicTallyWorldMarketState {
  id: typeof HEARTHFORD_PUBLIC_TALLY_MARKET_ID
  location: { kind: 'settlement-trading-location'; id: typeof SETTLEMENT_TRADING_LOCATION_ID }
  commodityStates: readonly [WorldMarketCommodityState, WorldMarketCommodityState]
}

export type WorldMarketState = SiteWorldMarketState | PublicTallyWorldMarketState

export interface WorldMarketsState {
  version: typeof WORLD_MARKETS_CONTRACT_VERSION
  markets: readonly WorldMarketState[]
}

export interface LegacyWorldMarketStateV1 {
  id: string
  siteId: string
  commodityStates: readonly []
}

export interface LegacyWorldMarketsStateV1 {
  version: typeof LEGACY_WORLD_MARKETS_CONTRACT_VERSION
  markets: readonly LegacyWorldMarketStateV1[]
}

export type MarketDiagnostic =
  | 'market.malformed-state'
  | 'market.invalid-version'
  | 'market.invalid-record'
  | 'market.invalid-location'
  | 'market.invalid-commodity-state'
  | 'market.noncanonical-order'
  | 'market.invalid-settlement-consequence'

export class MarketContractError extends Error {
  constructor(readonly diagnostics: readonly MarketDiagnostic[]) {
    super(`market rejected: ${diagnostics.join(', ')}`)
    this.name = 'MarketContractError'
  }
}

const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const compare = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1
const keys = (value: Record<string, unknown>, expected: readonly string[]): boolean => {
  const actual = Object.keys(value).sort(compare)
  const canonical = [...expected].sort(compare)
  return actual.length === canonical.length && actual.every((key, index) => key === canonical[index])
}
const oneOf = <Value>(values: readonly Value[], value: unknown): value is Value => values.includes(value as Value)
const canonicalByCommodity = (states: readonly WorldMarketCommodityState[]): boolean => states.every((state, index) => index === 0 || compare(states[index - 1]!.commodityId, state.commodityId) < 0)

const seededBand = <Value>(seed: string, marketId: string, commodityId: JomonCommodityId, field: string, values: readonly Value[]): Value => values[hashSeed(`market-v2:${seed}:${marketId}:${commodityId}:${field}`) % values.length]!

const seededCommodityState = (seed: string, marketId: string, commodityId: JomonCommodityId): WorldMarketCommodityState => ({
  commodityId,
  stock: seededBand(seed, marketId, commodityId, 'stock', MARKET_STOCK_BANDS),
  demand: seededBand(seed, marketId, commodityId, 'demand', MARKET_DEMAND_BANDS),
  price: seededBand(seed, marketId, commodityId, 'price', MARKET_PRICE_BANDS)
})

const contractIronworkState = (settlementTrading: SettlementTradingState): WorldMarketCommodityState => {
  const status = settlementTrading.contracts[0].status
  if (status === 'delivered') return { commodityId: 'commodity:ironwork', stock: 'none', demand: 'steady', price: 'fair' }
  if (status === 'accepted') return { commodityId: 'commodity:ironwork', stock: 'limited', demand: 'high', price: 'high' }
  if (status === 'refused') return { commodityId: 'commodity:ironwork', stock: 'available', demand: 'high', price: 'high' }
  return { commodityId: 'commodity:ironwork', stock: 'available', demand: 'high', price: 'high' }
}

const siteMarket = (seed: string, siteId: string): SiteWorldMarketState => ({
  id: `market:${siteId}`,
  location: { kind: 'site', id: siteId },
  commodityStates: JOMON_COMMODITY_IDS.map(commodityId => seededCommodityState(seed, `market:${siteId}`, commodityId))
})

const publicTallyMarket = (seed: string, settlementTrading: SettlementTradingState): PublicTallyWorldMarketState => ({
  id: HEARTHFORD_PUBLIC_TALLY_MARKET_ID,
  location: { kind: 'settlement-trading-location', id: SETTLEMENT_TRADING_LOCATION_ID },
  commodityStates: [
    contractIronworkState(settlementTrading),
    seededCommodityState(seed, HEARTHFORD_PUBLIC_TALLY_MARKET_ID, 'commodity:salt-fish')
  ]
})

/** Creates the complete local market projection from seed, known site IDs, and the one local tally contract. */
export const createWorldMarketsState = (input: {
  seed: string
  siteIds: readonly string[]
  settlementTrading: SettlementTradingState
}): WorldMarketsState => ({
  version: WORLD_MARKETS_CONTRACT_VERSION,
  markets: [...input.siteIds].sort(compare).map(siteId => siteMarket(input.seed, siteId) as WorldMarketState).concat(publicTallyMarket(input.seed, input.settlementTrading))
    .sort((left, right) => compare(left.id, right.id))
})

const validCommodityState = (value: unknown): value is WorldMarketCommodityState => record(value)
  && keys(value, ['commodityId', 'stock', 'demand', 'price'])
  && oneOf(JOMON_COMMODITY_IDS, value.commodityId)
  && oneOf(MARKET_STOCK_BANDS, value.stock)
  && oneOf(MARKET_DEMAND_BANDS, value.demand)
  && oneOf(MARKET_PRICE_BANDS, value.price)

const validSiteMarket = (value: unknown): value is SiteWorldMarketState => record(value)
  && keys(value, ['id', 'location', 'commodityStates'])
  && typeof value.id === 'string'
  && record(value.location)
  && keys(value.location, ['kind', 'id'])
  && value.location.kind === 'site'
  && typeof value.location.id === 'string'
  && value.id === `market:${value.location.id}`
  && Array.isArray(value.commodityStates)
  && value.commodityStates.length === JOMON_COMMODITY_IDS.length
  && value.commodityStates.every(validCommodityState)
  && canonicalByCommodity(value.commodityStates as WorldMarketCommodityState[])
  && (value.commodityStates as WorldMarketCommodityState[]).every((state, index) => state.commodityId === JOMON_COMMODITY_IDS[index])

const validPublicTallyMarket = (value: unknown): value is PublicTallyWorldMarketState => record(value)
  && keys(value, ['id', 'location', 'commodityStates'])
  && value.id === HEARTHFORD_PUBLIC_TALLY_MARKET_ID
  && record(value.location)
  && keys(value.location, ['kind', 'id'])
  && value.location.kind === 'settlement-trading-location'
  && value.location.id === SETTLEMENT_TRADING_LOCATION_ID
  && Array.isArray(value.commodityStates)
  && value.commodityStates.length === 2
  && value.commodityStates.every(validCommodityState)
  && canonicalByCommodity(value.commodityStates as WorldMarketCommodityState[])
  && (value.commodityStates as WorldMarketCommodityState[]).map(state => state.commodityId).join(',') === 'commodity:ironwork,commodity:salt-fish'

/** Exact validation keeps market facts local, seed-derived, and contract-bound. */
export const validateWorldMarketsState = (input: {
  seed: string
  siteIds: readonly string[]
  settlementTrading: SettlementTradingState
}, value: unknown): readonly MarketDiagnostic[] => {
  if (!record(value) || !keys(value, ['version', 'markets'])) return ['market.malformed-state']
  const diagnostics: MarketDiagnostic[] = []
  if (value.version !== WORLD_MARKETS_CONTRACT_VERSION) diagnostics.push('market.invalid-version')
  if (!Array.isArray(value.markets)) return [...new Set<MarketDiagnostic>([...diagnostics, 'market.malformed-state'])].sort()
  if (!value.markets.every(candidate => validSiteMarket(candidate) || validPublicTallyMarket(candidate))) diagnostics.push('market.invalid-record')
  const markets = value.markets as WorldMarketState[]
  if (!markets.every((market, index) => index === 0 || compare(markets[index - 1]!.id, market.id) < 0)) diagnostics.push('market.noncanonical-order')
  const expected = createWorldMarketsState(input)
  if (markets.length !== expected.markets.length) diagnostics.push('market.invalid-location')
  for (const expectedMarket of expected.markets) {
    const actual = markets.find(market => market.id === expectedMarket.id)
    if (actual === undefined || JSON.stringify(actual.location) !== JSON.stringify(expectedMarket.location)) diagnostics.push('market.invalid-location')
    if (actual === undefined || !canonicalByCommodity(actual.commodityStates) || actual.commodityStates.length !== expectedMarket.commodityStates.length) diagnostics.push('market.invalid-commodity-state')
    if (actual !== undefined && JSON.stringify(actual) !== JSON.stringify(expectedMarket)) {
      const ironwork = actual.id === HEARTHFORD_PUBLIC_TALLY_MARKET_ID ? actual.commodityStates.find(state => state.commodityId === 'commodity:ironwork') : undefined
      if (ironwork === undefined || JSON.stringify(ironwork) !== JSON.stringify(contractIronworkState(input.settlementTrading))) diagnostics.push('market.invalid-settlement-consequence')
      else diagnostics.push('market.invalid-commodity-state')
    }
  }
  return [...new Set(diagnostics)].sort()
}
