import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { chooseBotDecision } from '../src/core/bots';
import { applyCommand, createGame, defaultConfig, tickTurn } from '../src/core/game';
import { partyAwardsFor, partyPacingFor } from '../src/core/party-insights';
import { canPlaceGadget } from '../src/core/powerups';
import type { GameState, Point } from '../src/core/types';

interface Options { seed: string; players: number; holes: number; out: string; }

const usage = 'usage: npm run simulate:party -- [--seed value] [--players 2-4] [--holes 1-9] [--out file]';
const argument = (name: string) => {
  const index = process.argv.indexOf(name);
  return index >= 0 ? process.argv[index + 1] : undefined;
};
const options = (): Options => {
  const players = Number(argument('--players') ?? 4);
  const holes = Number(argument('--holes') ?? 9);
  if (!Number.isInteger(players) || players < 2 || players > 4 || !Number.isInteger(holes) || holes < 1 || holes > 9) throw new Error(usage);
  return { seed: argument('--seed') ?? 'party-simulation', players, holes, out: argument('--out') ?? path.join('output', 'simulations', 'party-simulation.json') };
};

const readyEveryPlayer = (state: GameState) => state.players.reduce((next, player) => next.die?.wagers[player.id]?.ready
  ? next
  : applyCommand(next, { type: 'ready-slot-spin', playerId: player.id }), state);

const playablePlacement = (state: GameState, ownerId: string): Point | undefined => state.course.route.find((point) => canPlaceGadget(state, ownerId, point));

const playHeldCard = (state: GameState) => {
  const player = state.players[state.turn.playerIndex]!;
  const card = player.pockets[0];
  if (!card || state.turn.cardPlayed) return state;
  const target = state.players.find((candidate) => candidate.id !== player.id && !candidate.ball.complete);
  if (card.id === 'airhorn' && target) return applyCommand(state, { type: 'use-power-up', powerUp: card.id, cardId: card.instanceId, targetId: target.id });
  if (card.id === 'freeze') {
    const hazard = state.course.hazards.find((candidate) => candidate.kind === 'sweeper' || candidate.kind === 'gate');
    if (hazard) return applyCommand(state, { type: 'use-power-up', powerUp: card.id, cardId: card.instanceId, hazardId: hazard.id });
    return state;
  }
  if (card.id === 'popper pad') {
    const placement = playablePlacement(state, player.id);
    return placement ? applyCommand(state, { type: 'use-power-up', powerUp: card.id, cardId: card.instanceId, placement }) : state;
  }
  return applyCommand(state, { type: 'use-power-up', powerUp: card.id, cardId: card.instanceId, targetId: player.id });
};

const actionCounts = (state: GameState) => Object.fromEntries((state.instrumentation?.events ?? []).reduce((counts, event) => {
  counts.set(event.type, (counts.get(event.type) ?? 0) + 1);
  return counts;
}, new Map<string, number>()));

const run = (options: Options) => {
  let state = createGame({ ...defaultConfig(), seed: options.seed, ruleset: 'party', humanCount: options.players, botCount: 0, holeCount: options.holes, skipDieBets: false });
  let steps = 0;
  let firstRevealFunded = false;
  while (state.status !== 'finished' && steps++ < 2_000) {
    if (state.status === 'rolling') {
      const die = state.die!;
      if (die.phase === 'wagering') {
        if (state.hole !== 1 || die.rerolls > 0) {
          state.players.forEach((player, index) => {
            if (die.wagers[player.id]!.influenceActions === 0 && index % 2 === state.hole % 2) state = applyCommand(state, { type: 'augment-slot-stop', playerId: player.id, reelId: die.reels[index % 3]!.id, stopId: die.reels[index % 3]!.stops[0]!.id });
          });
        }
        state = readyEveryPlayer(state);
        continue;
      }
      if (die.phase === 'spinning') { state = tickTurn(state, 2); continue; }
      if (die.phase === 'revealed') {
        if (state.hole === 1 && !firstRevealFunded && die.rerollPot) {
          state.players.slice(1, 4).forEach((player) => { state = applyCommand(state, { type: 'contribute-reroll', playerId: player.id }); });
          firstRevealFunded = true;
          continue;
        }
        state = tickTurn(state, 10);
        continue;
      }
      if (die.phase === 'reroll-wagering') {
        const chaosAuthor = state.players[0]!;
        if (die.wagers[chaosAuthor.id]!.influenceActions === 0) state = applyCommand(state, { type: 'add-chaos-reel', playerId: chaosAuthor.id });
        state = readyEveryPlayer(state);
        continue;
      }
    }
    if (state.status === 'playing') {
      state = tickTurn(state, 6);
      if (state.status !== 'playing') continue;
      state = playHeldCard(state);
      const player = state.players[state.turn.playerIndex]!;
      const decision = chooseBotDecision(state.course, { ...player, kind: 'bot', skill: 7 }, state.players, state.hazardElapsedMs, state.holeRules, state.gadgets);
      state = applyCommand(state, { type: 'shoot', shot: decision.shot });
      continue;
    }
    if (state.status === 'shopping') {
      const shop = state.shop!;
      if (!shop.rerollResolved) {
        state.players.forEach((player) => { state = applyCommand(state, { type: 'shop-vote-reroll', playerId: player.id, approve: false }); });
        continue;
      }
      const buyerId = shop.buyerOrder[shop.buyerIndex]!;
      const buyer = state.players.find((player) => player.id === buyerId)!;
      const offer = shop.shelf.find((candidate) => !candidate.sold && candidate.price <= buyer.cash && buyer.pockets.length < 2);
      state = applyCommand(state, offer ? { type: 'shop-buy', playerId: buyerId, offerId: offer.id } : { type: 'shop-skip', playerId: buyerId });
      continue;
    }
    if (state.status === 'transitioning') {
      state = applyCommand(state, { type: 'complete-transition' });
      continue;
    }
    throw new Error(`unexpected state ${state.status}`);
  }
  if (state.status !== 'finished') throw new Error(`simulation did not finish after ${steps} steps`);
  return {
    schemaVersion: 1,
    mode: 'deterministic-party-simulation',
    seed: options.seed,
    players: options.players,
    holes: options.holes,
    steps,
    totals: state.players.map((player) => ({ id: player.id, total: player.total })),
    recipes: state.coursePlan.map((plan) => plan.recipe.metadata),
    connections: state.connections,
    instrumentation: actionCounts(state),
    pacing: partyPacingFor(state),
    awards: partyAwardsFor(state),
  };
};

const main = async () => {
  const selected = options();
  const report = run(selected);
  const output = path.resolve(selected.out);
  await mkdir(path.dirname(output), { recursive: true });
  await writeFile(output, `${JSON.stringify(report, null, 2)}\n`);
  process.stdout.write(`${JSON.stringify({ output, seed: report.seed, players: report.players, holes: report.holes, steps: report.steps, connectionCount: report.connections.length, instrumentation: report.instrumentation, pacing: report.pacing, awards: report.awards }, null, 2)}\n`);
};

main().catch((error) => {
  process.stderr.write(`${error instanceof Error ? error.message : String(error)}\n`);
  process.exitCode = 1;
});
