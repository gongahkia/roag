import { describe, expect, it } from 'vitest'
import { createWorldMarketsState, HEARTHFORD_PUBLIC_TALLY_MARKET_ID, validateWorldMarketsState } from './market'
import { acceptSettlementTradeContract, deliverSettlementTradeContract, initialSettlementTradingState } from './settlement-trading'

const marketsFor = (settlementTrading = initialSettlementTradingState()) => createWorldMarketsState({
  seed: 'market-contract',
  siteIds: ['site:downstream', 'site:upstream'],
  settlementTrading
})

describe('seeded local market conditions', () => {
  it('derives a bounded, canonical local ledger without money, buyers, or exchange authority', () => {
    const state = marketsFor()
    const reordered = createWorldMarketsState({
      seed: 'market-contract',
      siteIds: ['site:upstream', 'site:downstream'],
      settlementTrading: initialSettlementTradingState()
    })

    expect(state).toEqual(reordered)
    expect(validateWorldMarketsState({
      seed: 'market-contract',
      siteIds: ['site:downstream', 'site:upstream'],
      settlementTrading: initialSettlementTradingState()
    }, state)).toEqual([])
    expect(state.markets.map(market => market.id)).toEqual([...state.markets.map(market => market.id)].sort())
    expect(state.markets.filter(market => market.location.kind === 'site').every(market => market.commodityStates.length === 8)).toBe(true)
    expect(JSON.stringify(state)).not.toMatch(/wallet|money|balance|buyer|shop|purchase/i)
  })

  it('binds the public tally ironwork condition to the local contract outcome and rejects forgery', () => {
    const accepted = acceptSettlementTradeContract(initialSettlementTradingState(), 0, 1)
    const delivered = deliverSettlementTradeContract(accepted, 0, 2)
    const acceptedMarket = marketsFor(accepted)
    const deliveredMarket = marketsFor(delivered)
    const ironwork = (state: ReturnType<typeof marketsFor>) => state.markets.find(market => market.id === HEARTHFORD_PUBLIC_TALLY_MARKET_ID)?.commodityStates[0]

    expect(ironwork(acceptedMarket)).toEqual({ commodityId: 'commodity:ironwork', stock: 'limited', demand: 'high', price: 'high' })
    expect(ironwork(deliveredMarket)).toEqual({ commodityId: 'commodity:ironwork', stock: 'none', demand: 'steady', price: 'fair' })
    const forged = structuredClone(deliveredMarket)
    const tally = forged.markets.find(market => market.id === HEARTHFORD_PUBLIC_TALLY_MARKET_ID)
    if (!tally) throw new Error('public tally market is missing')
    tally.commodityStates[0]!.price = 'high'
    expect(validateWorldMarketsState({ seed: 'market-contract', siteIds: ['site:downstream', 'site:upstream'], settlementTrading: delivered }, forged)).toContain('market.invalid-settlement-consequence')
  })
})
