import { ecologyProfileFor } from './ecology'
import { escalationFor } from './escalation'
import { gateForArea } from './area-gates'
import { gateAlternativesForRun } from './engine/gates'
import { BIOME_POOL, isCampaignAreaOrder } from './engine/campaign'
import { generateRouteContract, validateRouteContract } from './route-contract'
import { visualIdentitySnapshot } from './renderer/visual-grammar'
import type { Biome } from './types'
import { areaFloorIndex, generateAreaFloor, layoutFor } from './world'

export interface CampaignCompositionReport {
  orders: number
  transitions: Record<string, number>
  repeatedRecipeEscalations: string[]
  weakDiversity: string[]
  toolOpportunities: string[]
  errors: string[]
}

const transitionKey = (from: Biome, to: Biome): string => `${from}->${to}`
const orderKey = (order: readonly Biome[]): string => order.join('>')
const range = (length: number): number[] => Array.from({ length }, (_, index) => index)

export const campaignAreaOrders = (length = 4): Biome[][] => {
  if (!Number.isInteger(length) || length < 1 || length > BIOME_POOL.length) throw new Error(`invalid campaign area count: ${length}`)
  const orders: Biome[][] = []
  const visit = (selected: Biome[], remaining: readonly Biome[]): void => {
    if (selected.length === length) { orders.push(selected); return }
    for (const biome of remaining) visit([...selected, biome], remaining.filter(candidate => candidate !== biome))
  }
  visit([], BIOME_POOL)
  return orders
}

interface BiomeCompositionSignature { visual: string; route: string; ecology: string; recipeEscalations: string[]; toolOpportunities: string[]; errors: string[] }
const signatureFor = (seed: number, biome: Biome): BiomeCompositionSignature => {
  const recipeEscalations: string[] = []
  const toolOpportunities: string[] = []
  const errors: string[] = []
  let route = ''
  for (const areaFloor of range(4)) {
    const recipeId = layoutFor(seed, biome, areaFloor)
    const escalation = escalationFor(seed, biome, areaFloor)
    const contract = generateRouteContract({ campaignSeed: seed, floorIndex: areaFloorIndex(biome, areaFloor), biome, areaFloor, recipeId, escalationVariant: `${escalation.arcId}:${escalation.phase}` })
    const validation = validateRouteContract(contract)
    if (!validation.valid) errors.push(...validation.errors.map(error => `${biome}:${areaFloor + 1}:${error}`))
    errors.push(...contract.rewardOffers.flatMap(offer => offer.choices.filter(choice => !choice.problem || !choice.payoff || !choice.terrain || (choice.role !== 'sidegrade' && choice.biomeFit !== 'local')).map(choice => `${biome}:${areaFloor + 1}:${offer.milestoneId}:${choice.id}: uncontextualized reward`)))
    const floor = generateAreaFloor(seed, biome, areaFloor)
    toolOpportunities.push(...(floor.rewardOffers ?? []).flatMap(offer => offer.kind === 'waycache' ? offer.choices.filter(choice => ['antlerPrybar', 'stoneAdze', 'resinFireBasket', 'woodenLeverRoller'].includes(choice.id)).map(choice => `${biome}:${areaFloor + 1}:waycache:${choice.id}`) : []), ...floor.items.flatMap(item => item.tool ? [`${biome}:${areaFloor + 1}:loot:${item.tool}`] : []), ...(floor.encounters ?? []).flatMap(encounter => encounter.toolOffer ? [`${biome}:${areaFloor + 1}:${encounter.social ? 'social' : 'encounter'}:${encounter.toolOffer}`] : []))
    recipeEscalations.push(`${biome}:${recipeId}:${escalation.arcId}:${escalation.phase}`)
    if (areaFloor === 0) {
      const fork = contract.nodes.find(node => node.kind === 'fork')!
      route = fork.tags.terrain.concat(fork.tags.encounter).join('/')
    }
  }
  const ecology = ecologyProfileFor(biome)
  return { visual: visualIdentitySnapshot(biome, 'normal').line, route, ecology: [ecology.kind, ecology.effect, ...ecology.terrain].join('/'), recipeEscalations, toolOpportunities, errors }
}

export const sweepCampaignComposition = (seed: number, orders = campaignAreaOrders()): CampaignCompositionReport => {
  const transitions: Record<string, number> = {}
  const repeatedRecipeEscalations = new Set<string>()
  const weakDiversity = new Set<string>()
  const errors: string[] = []
  const signatures = new Map<Biome, BiomeCompositionSignature>()
  const signature = (biome: Biome): BiomeCompositionSignature => {
    const current = signatures.get(biome)
    if (current) return current
    const created = signatureFor(seed, biome)
    signatures.set(biome, created)
    return created
  }
  for (const order of orders) {
    const key = orderKey(order)
    if (!isCampaignAreaOrder(order)) { errors.push(`${key}: invalid area order`); continue }
    const recipes = new Set<string>()
    for (const biome of order) {
      const current = signature(biome)
      errors.push(...current.errors.map(error => `${key}: ${error}`))
      for (const recipe of current.recipeEscalations) {
        if (recipes.has(recipe)) repeatedRecipeEscalations.add(`${key}: ${recipe}`)
        recipes.add(recipe)
      }
    }
    for (let index = 0; index < order.length - 1; index++) {
      const from = order[index]
      const to = order[index + 1]
      const transition = transitionKey(from, to)
      transitions[transition] = (transitions[transition] ?? 0) + 1
      const gate = gateAlternativesForRun(gateForArea(from, to))
      if (!gate.some(alternative => alternative.kind === 'body' || alternative.kind === 'oath')) errors.push(`${key}: ${transition}: no traversal-independent gate alternative`)
      const current = signature(from)
      const next = signature(to)
      if (current.visual === next.visual || current.route === next.route || current.ecology === next.ecology) weakDiversity.add(`${transition}: visual=${current.visual === next.visual} route=${current.route === next.route} ecology=${current.ecology === next.ecology}`)
    }
  }
  return { orders: orders.length, transitions, repeatedRecipeEscalations: [...repeatedRecipeEscalations].sort(), weakDiversity: [...weakDiversity].sort(), toolOpportunities: [...new Set([...signatures.values()].flatMap(signature => signature.toolOpportunities))].sort(), errors }
}
