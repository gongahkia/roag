import { CADDIES, CONTENT, definitionFor } from './catalog';
import { addMessage, beginCourseTransition, recordInstrumentation } from './game-state';
import { addCaddy, caddyCount, pocketCapacity, pocketsFor, removeCaddy, syncPocketMirrors } from './player-effects';
import { Random } from './random';
import { PARTY_TRICK_CARDS, rulesetFor } from './rulesets';
import { resolveHoleAttachments } from './strategy';
import type { CaddyId, ContentCategory, ContentId, GameState, RealityCard, ShopOffer } from './types';

const contentFor = (categories: readonly ContentCategory[]) => CONTENT.filter((definition) => categories.includes(definition.category));
const durationFor = (state: GameState, contentId: ContentId, source: string) => {
  const definition = CONTENT.find((candidate) => candidate.id === contentId)!;
  if (!definition.duration) return undefined;
  const random = new Random(`${state.config.seed}:card-duration:${state.hole}:${source}:${contentId}`);
  return { unit: definition.duration.unit, amount: random.int(definition.duration.min, definition.duration.max) } as const;
};

const offer = (state: GameState, contentId: ContentId, index: number, source: string): ShopOffer => {
  const definition = CONTENT.find((candidate) => candidate.id === contentId)!;
  return { id: `shop-${index}-${contentId.replaceAll(' ', '-')}`, contentId, category: definition.category, price: definition.price, duration: durationFor(state, contentId, source) };
};

const pick = (random: Random, categories: readonly ContentCategory[], selected: Set<ContentId>) => {
  const choices = contentFor(categories).filter((definition) => !selected.has(definition.id));
  const choice = random.pick(choices.length ? choices : contentFor(categories));
  selected.add(choice.id);
  return choice.id;
};

const shelfFor = (state: GameState, visit: number, reroll = 0) => {
  const random = new Random(`${state.config.seed}:shop:${visit}:reroll:${reroll}`);
  const selected = new Set<ContentId>();
  const ruleset = rulesetFor(state.config);
  if (ruleset.id === 'party') {
    const ids: ContentId[] = [];
    while (ids.length < ruleset.shop.offerCount) {
      const candidate = random.pick(PARTY_TRICK_CARDS);
      if (!ids.includes(candidate)) ids.push(candidate);
    }
    return ids.map((id, index) => offer(state, id, index, `${visit}:${reroll}:${index}`));
  }
  const ids = [
    pick(random, ['caddy'], selected), pick(random, ['caddy'], selected), pick(random, ['caddy'], selected),
    pick(random, ['pocket', 'form', 'gadget'], selected), pick(random, ['pocket', 'form', 'gadget'], selected),
    pick(random, ['reality'], selected), pick(random, ['chrono'], selected),
  ];
  return ids.map((id, index) => offer(state, id, index, `${visit}:${reroll}:${index}`));
};

const buyerOrderFor = (state: GameState, opening: boolean) => {
  if (opening) return state.players.map((player) => player.id);
  const completed = state.players.filter((player) => player.ball.complete).sort((left, right) => (left.holeFinishOrder ?? Number.MAX_SAFE_INTEGER) - (right.holeFinishOrder ?? Number.MAX_SAFE_INTEGER) || left.ball.strokes - right.ball.strokes || left.id.localeCompare(right.id));
  const unfinished = state.players.filter((player) => !player.ball.complete).sort((left, right) => left.ball.strokes - right.ball.strokes || left.id.localeCompare(right.id));
  return [...completed, ...unfinished].map((player) => player.id);
};

export const activeShopper = (state: GameState) => state.shop ? state.players.find((player) => player.id === state.shop!.buyerOrder[state.shop!.buyerIndex]) : undefined;

export const openShop = (state: GameState, opening = false) => {
  const ruleset = rulesetFor(state.config);
  const visit = (state.shop?.visit ?? 0) + 1;
  state.shop = { visit, opening, shelf: shelfFor(state, visit), buyerOrder: buyerOrderFor(state, opening), buyerIndex: 0, completedBuyerIds: [], rerollVotes: {}, rerollResolved: !ruleset.shop.allowsRerollVote, rerolled: false, secondsLeft: ruleset.shop.seconds };
  state.status = 'shopping';
  state.paused = false;
  addMessage(state, opening ? 'the clubhouse merchant opens before tee-off' : 'hole scored — the clubhouse merchant opens');
};

export const awardHoleCash = (state: GameState) => {
  const leader = Math.min(...state.players.map((player) => player.total));
  state.players.forEach((player) => {
    const comeback = player.total - leader >= 2 ? 3 : 0;
    player.cash += 4 + comeback;
    if (comeback) addMessage(state, `${player.name} collects a $${comeback} comeback bounty`);
  });
  resolveHoleAttachments(state);
};

const resolveReroll = (state: GameState) => {
  const shop = state.shop!;
  if (shop.rerollResolved || Object.keys(shop.rerollVotes).length < state.players.length) return;
  const approvals = Object.values(shop.rerollVotes).filter(Boolean).length;
  shop.rerollResolved = true;
  shop.rerolled = approvals > state.players.length / 2;
  if (shop.rerolled) {
    shop.shelf = shelfFor(state, shop.visit, 1);
    addMessage(state, 'the table votes to reshuffle the merchant shelf');
  } else addMessage(state, 'the merchant shelf stands');
};

export const voteShopReroll = (state: GameState, playerId: string, approve: boolean) => {
  if (state.status !== 'shopping' || !state.shop || state.shop.rerollResolved || !state.players.some((player) => player.id === playerId)) return;
  state.shop.rerollVotes[playerId] = approve;
  resolveReroll(state);
};

const completeBuyer = (state: GameState) => {
  const shop = state.shop!;
  const buyer = activeShopper(state);
  if (buyer) shop.completedBuyerIds.push(buyer.id);
  shop.buyerIndex += 1;
  shop.secondsLeft = rulesetFor(state.config).shop.seconds;
  if (shop.buyerIndex < shop.buyerOrder.length) return;
  const opening = shop.opening;
  state.shop = undefined;
  if (opening) {
    state.status = 'playing';
    addMessage(state, 'the merchant rings the bell — tee off');
  } else beginCourseTransition(state);
};

const offerCostFor = (player: GameState['players'][number], shopOffer: ShopOffer) => Math.max(0, shopOffer.price - (shopOffer.category === 'caddy' ? caddyCount(player, 'broker') : 0));

const terrainDemandFor = (state: GameState, contentId: ContentId) => {
  const surfaceCount = (surface: string) => state.course.tiles.filter((tile) => tile.surface === surface).length;
  const gustCount = (state.course.features ?? []).filter((feature) => feature.kind === 'gust').length;
  const elevation = Math.max(...state.course.tiles.map((tile) => tile.height));
  const values: Partial<Record<ContentId, number>> = {
    'cushion keeper': surfaceCount('cushion') * .35,
    'cushion map': surfaceCount('cushion') * .2,
    'spring coach': surfaceCount('spring') * .4,
    'spring polish': surfaceCount('spring') * .25,
    'bumper apprentice': surfaceCount('bumper') * .4,
    'bumper wax': surfaceCount('bumper') * .25,
    'slope scout': elevation * 1.4,
    'slope stabilizer': elevation * .75,
    'wind warden': gustCount * 1.8,
    'wind sock': gustCount * 1.1,
    'wind sail': gustCount * 1.1,
    'grounds crew': elevation * .8,
    'bank holiday': surfaceCount('bumper') * .35,
    'spring fling': surfaceCount('booster') * .25,
    'high winds': gustCount * .9,
    'cushion league': surfaceCount('sand') * .2,
  };
  return values[contentId] ?? 0;
};

/**
 * Picks a legal, deterministic preference rather than whichever card happens to
 * be first on the shared shelf. Keeping this in core makes local and server
 * bots make the same shop decision.
 */
export const chooseBotShopOffer = (state: GameState, player: GameState['players'][number]) => {
  const shop = state.shop;
  if (!shop) return undefined;
  const random = new Random(`${state.config.seed}:shop-bot:${shop.visit}:${shop.rerolled ? 1 : 0}:${player.id}`);
  const ruleset = rulesetFor(state.config);
  const pocketCount = Math.max(player.pockets.length, Number(Boolean(player.inventory)) + Number(Boolean(player.spareInventory)));
  const categoryValue: Record<ContentCategory, number> = { caddy: 14, reality: 13, chrono: 11, pocket: 10, form: 10, gadget: 10 };
  return shop.shelf
    .filter((shopOffer) => {
      if (shopOffer.sold || offerCostFor(player, shopOffer) > player.cash) return false;
      if (shopOffer.category === 'caddy') {
        const duplicate = player.caddies.some((caddy) => caddy.id === shopOffer.contentId);
        return duplicate || player.caddies.length < 3;
      }
      return shopOffer.category === 'reality' || pocketCount < pocketCapacity(player, ruleset.id === 'party' ? ruleset.cards.handLimit : undefined);
    })
    .map((shopOffer) => {
      const duplicateCaddy = shopOffer.category === 'caddy' && player.caddies.some((caddy) => caddy.id === shopOffer.contentId);
      const definition = definitionFor(shopOffer.contentId);
      const boonBonus = definition?.polarity === 'boon' ? .45 : 0;
      return { shopOffer, score: categoryValue[shopOffer.category] + terrainDemandFor(state, shopOffer.contentId) + boonBonus + (duplicateCaddy ? .35 : 0) + random.next() };
    })
    .sort((left, right) => right.score - left.score || left.shopOffer.id.localeCompare(right.shopOffer.id))[0]?.shopOffer;
};

const storePocket = (state: GameState, playerId: string, contentId: ContentId, duration?: ShopOffer['duration'], replaceCardId?: string) => {
  const player = state.players.find((candidate) => candidate.id === playerId)!;
  const pockets = pocketsFor(player);
  const ruleset = rulesetFor(state.config);
  const capacity = pocketCapacity(player, ruleset.id === 'party' ? ruleset.cards.handLimit : undefined);
  if (pockets.length >= capacity) {
    const replacement = replaceCardId ? pockets.findIndex((card) => card.instanceId === replaceCardId) : -1;
    if (replacement < 0) return false;
    pockets.splice(replacement, 1);
  }
  pockets.push({ id: contentId as never, source: 'shop', instanceId: `shop-card-${++state.cardSequence}`, duration: duration ? { ...duration } : undefined });
  syncPocketMirrors(player);
  return true;
};

export const buyShopOffer = (state: GameState, playerId: string, offerId: string, replaceCaddyId?: CaddyId, replaceCardId?: string) => {
  if (state.status !== 'shopping' || !state.shop || !state.shop.rerollResolved || activeShopper(state)?.id !== playerId) return;
  const shopOffer = state.shop.shelf.find((candidate) => candidate.id === offerId && !candidate.sold);
  const player = state.players.find((candidate) => candidate.id === playerId);
  if (!shopOffer || !player) return;
  const cost = offerCostFor(player, shopOffer);
  if (player.cash < cost) return;
  if (shopOffer.category === 'caddy') {
    const caddyId = shopOffer.contentId as CaddyId;
    const isDuplicate = caddyCount(player, caddyId) > 0;
    if (!isDuplicate && player.caddies.length >= 3) {
      if (!replaceCaddyId || !removeCaddy(player, replaceCaddyId)) return;
      player.cash += 3;
    }
    addCaddy(player, caddyId);
  } else if (shopOffer.category === 'reality') {
    state.queuedReality = shopOffer.contentId as RealityCard;
  } else if (!storePocket(state, playerId, shopOffer.contentId, shopOffer.duration, replaceCardId)) return;
  player.cash -= cost;
  shopOffer.sold = true;
  if (caddyCount(player, 'black market caddy') && ['pocket', 'form', 'gadget'].includes(shopOffer.category)) {
    const random = new Random(`${state.config.seed}:black-market:${state.hole}:${player.id}:${shopOffer.id}`);
    const bonus = pick(random, ['pocket'], new Set());
    storePocket(state, player.id, bonus, durationFor(state, bonus, `black-market:${player.id}:${shopOffer.id}`));
  }
  addMessage(state, `${player.name} buys ${shopOffer.contentId} for $${cost}`);
  recordInstrumentation(state, { type: 'shop', hole: state.hole, playerId, detail: shopOffer.contentId });
  completeBuyer(state);
};

export const sellShopCaddy = (state: GameState, playerId: string, caddyId: CaddyId) => {
  if (state.status !== 'shopping' || !state.shop || !state.shop.rerollResolved || activeShopper(state)?.id !== playerId) return;
  const player = state.players.find((candidate) => candidate.id === playerId)!;
  if (!removeCaddy(player, caddyId)) return;
  player.cash += 3;
  addMessage(state, `${player.name} sells ${caddyId} for $3`);
};

export const skipShopBuyer = (state: GameState, playerId: string) => {
  if (state.status !== 'shopping' || !state.shop || !state.shop.rerollResolved || activeShopper(state)?.id !== playerId) return;
  addMessage(state, `${activeShopper(state)!.name} leaves the merchant shelf alone`);
  completeBuyer(state);
};

export const tickShop = (state: GameState, elapsedSeconds: number) => {
  if (state.status !== 'shopping' || !state.shop || state.paused) return false;
  state.shop.secondsLeft = Math.max(0, state.shop.secondsLeft - elapsedSeconds);
  if (state.shop.secondsLeft > 0) return false;
  if (!state.shop.rerollResolved) {
    state.players.forEach((player) => { state.shop!.rerollVotes[player.id] ??= false; });
    resolveReroll(state);
  } else skipShopBuyer(state, activeShopper(state)?.id ?? '');
  return true;
};

export const caddyDefinitions = CADDIES;
