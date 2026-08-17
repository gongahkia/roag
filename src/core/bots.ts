import { distanceToCup, simulateShot, tileAt } from './physics';
import { physicsModifiersFor } from './player-effects';
import { Random } from './random';
import type { Course, Gadget, HoleRules, Player, Point, PowerUp, ShotCommand, VotingOption } from './types';

export interface BotDecision {
  shot: ShotCommand;
  powerUp?: { type: PowerUp; targetId?: string; portalExitId?: string; placement?: Point };
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

const bestPortalExit = (course: Course) => course.portals?.filter((pair) => pair.exit).map((pair) => ({ id: `${pair.id}:exit`, distance: Math.hypot(course.cup.x - pair.exit!.point.x, course.cup.y - pair.exit!.point.y) })).sort((left, right) => left.distance - right.distance)[0]?.id;

const gadgetPointFor = (course: Course, bot: Player, gadgets: readonly Gadget[]): Point | undefined => {
  if (gadgets.some((gadget) => gadget.ownerId === bot.id)) return undefined;
  const used = (point: Point) => gadgets.some((gadget) => gadget.point.x === point.x && gadget.point.y === point.y)
    || course.itemPads.some((pad) => pad.point.x === point.x && pad.point.y === point.y)
    || course.hazards.some((hazard) => hazard.point.x === point.x && hazard.point.y === point.y);
  const candidates = course.route.slice(3, -2).filter((point) => {
    const tile = tileAt(course, point.x + .5, point.y + .5);
    return tile && ['fairway', 'rough', 'sand', 'ice', 'booster', 'conveyor'].includes(tile.surface) && !used(point);
  });
  return candidates.length ? candidates[(bot.ball.strokes + bot.id.length) % candidates.length] : undefined;
};

export const chooseBotDecision = (course: Course, bot: Player, players: Player[], phase = 0, rules?: HoleRules, gadgets: readonly Gadget[] = []): BotDecision => {
  const skill = clampedSkill(bot, players.filter((player) => player.id !== bot.id));
  const random = new Random(`${course.seed}:${bot.id}:${bot.ball.strokes}`);
  const cup = { x: course.cup.x + 0.5, y: course.cup.y + 0.5 };
  const baseAngle = Math.atan2(cup.y - bot.ball.y, cup.x - bot.ball.x);
  const angleStep = 0.24 - skill * 0.018;
  const powerStep = 0.75 - skill * 0.055;
  const sampleCount = 5 + skill * 4;
  const candidates: { shot: ShotCommand; score: number }[] = [];

  for (let index = 0; index < sampleCount; index += 1) {
    const angle = baseAngle + (index - (sampleCount - 1) / 2) * angleStep;
    for (let power = 2; power <= 7.5; power += powerStep) {
      const shot = { angle, power };
      const result = simulateShot(course, bot.ball, shot, undefined, { phase, phaseCount: rules?.hazardPhaseCount, modifiers: physicsModifiersFor(bot, rules) });
      const score = (result.holed ? -1000 : distanceToCup(course, result.ball) * 8)
        + result.ball.resetCount * 45
        + Math.max(0, result.ball.z - 1.4) * 3
        + random.next() * (11 - skill) * 3;
      candidates.push({ shot, score });
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
  const held = bot.inventory ?? bot.spareInventory;
  if (held && skill >= 5 && random.chance(0.18 + skill * 0.025)) {
    if (held === 'turbo' || held === 'shield' || held === 'two putts' || held === 'heavy' || held === 'bouncy' || held === 'ghost' || held === 'magnet' || held === 'ice' || held === 'cup magnet' || held === 'slipstream' || held === 'rebound rig') powerUp = { type: held };
    else if (held === 'portal') {
      const portalExitId = bestPortalExit(course);
      if (portalExitId) powerUp = { type: held, portalExitId };
    } else if (held === 'popper pad' || held === 'snare patch' || held === 'blast mine' || held === 'slick patch') {
      const placement = gadgetPointFor(course, bot, gadgets);
      if (placement) powerUp = { type: held, placement };
    } else if (target) powerUp = { type: held, targetId: target.id };
  }
  return { shot, powerUp, secondWind: bot.secondWindAvailable && !bot.twoPuttsArmed && skill >= 6 && random.chance(.45), confidence: Math.max(0, 1 - selected.score / 150) };
};

export const chooseBotVote = (seed: string, hole: number, bot: Player, options: readonly VotingOption[]): string => {
  const random = new Random(`${seed}:hole:${hole}:vote:${bot.id}`);
  return random.pick(options).id;
};
