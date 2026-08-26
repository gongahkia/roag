import { distanceToCup, simulateShot, tileAt } from './physics';
import { definitionFor } from './catalog';
import { physicsModifiersFor } from './player-effects';
import { Random } from './random';
import type { ChronoCard, Course, DieState, Gadget, HoleRules, Player, Point, PowerUp, ShotCommand, ShotKind } from './types';

export interface BotDecision {
  shot: ShotCommand;
  powerUp?: { type: PowerUp | ChronoCard; cardId?: string; targetId?: string; portalExitId?: string; placement?: Point };
  secondWind?: boolean;
  confidence: number;
}

const clampedSkill = (player: Player, opponents: Player[]) => {
  if (player.skill !== 'adaptive') return player.skill;
  const average = opponents.reduce((total, opponent) => total + opponent.total, 0) / Math.max(1, opponents.length);
  return Math.max(1, Math.min(10, Math.round(6 + (average - player.total) / 4)));
};

const rankedTarget = (bot: Player, players: Player[]): Player | undefined => players
  .filter((player) => player.id !== bot.id && !player.ball.complete)
  .sort((left, right) => left.total + left.ball.strokes - (right.total + right.ball.strokes))[0];

const boonTarget = (bot: Player, players: Player[]) => {
  const eligible = players.filter((player) => !player.ball.complete);
  const leader = Math.min(...eligible.map((player) => player.total + player.ball.strokes));
  return eligible.sort((left, right) => (right.total + right.ball.strokes - leader) - (left.total + left.ball.strokes - leader) || (left.id === bot.id ? -1 : 1))[0] ?? bot;
};

const bestPortalExit = (course: Course) => course.portals?.filter((pair) => pair.exit).map((pair) => ({ id: `${pair.id}:exit`, distance: Math.hypot(course.cup.x - pair.exit!.point.x, course.cup.y - pair.exit!.point.y) })).sort((left, right) => left.distance - right.distance)[0]?.id;

const gadgetPointFor = (course: Course, bot: Player, gadgets: readonly Gadget[]): Point | undefined => {
  if (gadgets.some((gadget) => gadget.ownerId === bot.id)) return undefined;
  const used = (point: Point) => gadgets.some((gadget) => gadget.point.x === point.x && gadget.point.y === point.y)
    || course.itemPads.some((pad) => pad.point.x === point.x && pad.point.y === point.y)
    || course.hazards.some((hazard) => hazard.point.x === point.x && hazard.point.y === point.y);
  const candidates = course.route.slice(3, -2).filter((point) => {
    const tile = tileAt(course, point.x + .5, point.y + .5);
    return tile && ['fairway', 'rough', 'sand', 'ice', 'booster', 'conveyor', 'cushion', 'spring'].includes(tile.surface) && !used(point);
  });
  return candidates.length ? candidates[(bot.ball.strokes + bot.id.length) % candidates.length] : undefined;
};

const wallBetween = (course: Course, from: Point, to: Point) => {
  const steps = Math.max(1, Math.ceil(Math.hypot(to.x - from.x, to.y - from.y) * 4));
  for (let step = 1; step < steps; step += 1) {
    const progress = step / steps;
    const surface = tileAt(course, from.x + (to.x - from.x) * progress, from.y + (to.y - from.y) * progress)?.surface;
    if (surface === 'wall' || surface === 'bumper') return true;
  }
  return false;
};

export const chooseBotDecision = (course: Course, bot: Player, players: Player[], hazardElapsedMs = 0, rules?: HoleRules, gadgets: readonly Gadget[] = []): BotDecision => {
  const skill = clampedSkill(bot, players.filter((player) => player.id !== bot.id));
  const random = new Random(`${course.seed}:${bot.id}:${bot.ball.strokes}`);
  const cup = { x: course.cup.x + 0.5, y: course.cup.y + 0.5 };
  const baseAngle = Math.atan2(cup.y - bot.ball.y, cup.x - bot.ball.x);
  const angleStep = 0.24 - skill * 0.018;
  const powerStep = 0.75 - skill * 0.055;
  const sampleCount = 5 + skill * 4;
  const candidates: { shot: ShotCommand; score: number }[] = [];
  const shotKinds: readonly ShotKind[] = wallBetween(course, bot.ball, cup) ? ['putt', 'chip'] : ['putt'];

  for (let index = 0; index < sampleCount; index += 1) {
    const angle = baseAngle + (index - (sampleCount - 1) / 2) * angleStep;
    for (const kind of shotKinds) {
      for (let power = 2; power <= 7.5; power += powerStep) {
        const shot: ShotCommand = { angle, power, kind };
        const result = simulateShot(course, bot.ball, shot, undefined, { hazardElapsedMs, phaseCount: rules?.hazardPhaseCount, modifiers: physicsModifiersFor(bot, rules) });
        const score = (result.holed ? -1000 : distanceToCup(course, result.ball) * 8)
          + result.ball.resetCount * 45
          + Math.max(0, result.ball.z - 1.4) * 3
          + random.next() * (11 - skill) * 3;
        candidates.push({ shot, score });
      }
    }
  }
  const selected = candidates.sort((left, right) => left.score - right.score)[0]!;
  const error = (11 - skill) * 0.006;
  const shot = {
    angle: selected.shot.angle + (random.next() - 0.5) * error,
    power: Math.max(1, selected.shot.power + (random.next() - 0.5) * error * 10),
  };
  const target = rankedTarget(bot, players);
  let powerUp: BotDecision['powerUp'];
  const heldCard = bot.pockets[0] ?? (bot.inventory ? { id: bot.inventory, instanceId: undefined } : bot.spareInventory ? { id: bot.spareInventory, instanceId: undefined } : undefined);
  const held = heldCard?.id;
  if (held && skill >= 5 && random.chance(0.18 + skill * 0.025)) {
    const definition = definitionFor(held as never);
    if (definition?.targetMode === 'player') {
      const recipient = definition.polarity === 'curse' ? target : boonTarget(bot, players);
      if (recipient) powerUp = { type: held, cardId: heldCard?.instanceId, targetId: recipient.id };
    } else if (held === 'wind sock' || held === 'slope stabilizer' || held === 'spring polish' || held === 'bumper wax' || held === 'cushion map') {
      const recipient = boonTarget(bot, players);
      powerUp = { type: held, cardId: heldCard?.instanceId, targetId: recipient.id };
    } else if (held === 'turbo' || held === 'shield' || held === 'two putts' || held === 'heavy' || held === 'bouncy' || held === 'ghost' || held === 'magnet' || held === 'ice' || held === 'glider' || held === 'sticky' || held === 'orbit' || held === 'cup magnet' || held === 'slipstream' || held === 'rebound rig' || held === 'rescue drone') powerUp = { type: held, cardId: heldCard?.instanceId };
    else if (held === 'portal') {
      const portalExitId = bestPortalExit(course);
      if (portalExitId) powerUp = { type: held, cardId: heldCard?.instanceId, portalExitId };
    } else if (held === 'popper pad' || held === 'snare patch' || held === 'blast mine' || held === 'slick patch' || held === 'sky spring') {
      const placement = gadgetPointFor(course, bot, gadgets);
      if (placement) powerUp = { type: held, cardId: heldCard?.instanceId, placement };
    } else if (target) powerUp = { type: held, cardId: heldCard?.instanceId, targetId: target.id };
  }
  return { shot, powerUp, secondWind: bot.secondWindAvailable && !bot.twoPuttsArmed && skill >= 6 && random.chance(.45), confidence: Math.max(0, 1 - selected.score / 150) };
};

export type BotDieAction =
  | { type: 'add-die-side'; playerId: string }
  | { type: 'augment-die-face'; playerId: string; faceId: string }
  | { type: 'ready-die-roll'; playerId: string };

/** A small deterministic stake keeps bots involved without draining their shop budget. */
export const chooseBotDieAction = (seed: string, hole: number, bot: Player, die: DieState): BotDieAction => {
  const wager = die.wagers[bot.id] ?? { addedSides: 0, augmentations: {}, ready: false };
  const random = new Random(`${seed}:hole:${hole}:die:${bot.id}:${wager.addedSides}:${Object.values(wager.augmentations).reduce((total, count) => total + count, 0)}`);
  const sideCost = 1 + wager.addedSides;
  if (bot.cash >= sideCost && random.chance(.22)) return { type: 'add-die-side', playerId: bot.id };
  const face = random.pick(die.faces);
  const augmented = wager.augmentations[face.id] ?? 0;
  const augmentationCost = 1 + Math.floor(augmented / 2);
  if (bot.cash >= augmentationCost && random.chance(.48)) return { type: 'augment-die-face', playerId: bot.id, faceId: face.id };
  return { type: 'ready-die-roll', playerId: bot.id };
};
