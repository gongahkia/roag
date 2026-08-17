import { newBall, type BallPhysicsModifiers } from './physics';
import type { BallForm, Course, HoleRules, Player, PowerUp, ShotCommand, Upgrade } from './types';

export const BALL_FORMS: readonly BallForm[] = ['heavy', 'bouncy', 'ghost', 'magnet', 'ice', 'portal', 'glider', 'sticky', 'orbit'];
export const GADGET_POWER_UPS: readonly PowerUp[] = ['popper pad', 'snare patch', 'blast mine', 'slick patch', 'sky spring'];
export const RECOVERY_POWER_UPS: readonly PowerUp[] = ['turbo', 'shield', 'two putts', 'bouncy', 'ice', 'magnet', 'glider', 'sticky', 'orbit', 'cup magnet', 'slipstream', 'rebound rig', 'rescue drone', 'popper pad', 'snare patch', 'blast mine', 'slick patch', 'sky spring', 'phase shift', 'sandbag'];
export const CHAOS_POWER_UPS: readonly PowerUp[] = ['turbo', 'shield', 'bomb', 'freeze', 'swap', 'two putts', 'heavy', 'bouncy', 'ghost', 'magnet', 'ice', 'portal', 'glider', 'sticky', 'orbit', 'cup magnet', 'slipstream', 'rebound rig', 'rescue drone', 'airhorn', 'popper pad', 'snare patch', 'blast mine', 'slick patch', 'sky spring', 'phase shift', 'sandbag'];
export const UPGRADES: readonly Upgrade[] = ['heavy ball', 'ice skates', 'extra charge', 'bank shot', 'hazard shield', 'chaos magnet', 'portal savvy', 'second wind', 'scavenger', 'aerial ace', 'cup reader', 'gadgeteer'];

export const UPGRADE_DESCRIPTIONS: Record<Upgrade, string> = {
  'heavy ball': '+12% launch power and 1.45× collision mass',
  'ice skates': 'slides farther across ice',
  'extra charge': 'empty chaos slot refills after every shot',
  'bank shot': 'retains 82% speed on wall rebounds',
  'hazard shield': 'one void rebound each hole',
  'chaos magnet': '65% chance to refill after using an item',
  'portal savvy': '18% more speed after portal exits',
  'second wind': 'one two-putts charge each hole',
  scavenger: 'hold a second chaos item',
  'aerial ace': 'chips fly farther and hold the air longer',
  'cup reader': '+18% cup capture radius',
  gadgeteer: 'maintain two placed gadgets',
};

export const isBallForm = (powerUp: PowerUp): powerUp is BallForm => BALL_FORMS.includes(powerUp as BallForm);

export const adjustedShotFor = (player: Player, shot: ShotCommand, rules?: HoleRules): ShotCommand => {
  const multiplier = (player.turboArmed ? 1.55 : 1) * (player.sandbagged ? .7 : 1) * (player.upgrades.includes('heavy ball') ? 1.12 : 1) * (player.ballForm === 'heavy' ? 1.16 : 1) * (shot.kind === 'chip' && player.upgrades.includes('aerial ace') ? 1.1 : 1) * (rules?.launchMultiplier ?? 1);
  return { ...shot, power: shot.power * multiplier };
};

export const physicsModifiersFor = (player: Player, rules?: HoleRules): BallPhysicsModifiers => ({
  mass: player.upgrades.includes('heavy ball') && player.ballForm === 'heavy' ? 1.9 : player.ballForm === 'heavy' ? 1.65 : player.upgrades.includes('heavy ball') ? 1.45 : 1,
  iceSkates: player.upgrades.includes('ice skates') || player.ballForm === 'ice',
  bankShot: player.upgrades.includes('bank shot'),
  bouncy: player.ballForm === 'bouncy',
  ghostBall: player.ballForm === 'ghost',
  magnetBall: player.ballForm === 'magnet',
  portalExitId: player.ballForm === 'portal' ? player.portalExitId : undefined,
  portalSpeedMultiplier: (player.upgrades.includes('portal savvy') ? 1.18 : 1) * (rules?.portalSpeedMultiplier ?? 1),
  hazardShield: player.hazardShield,
  rollingResistanceMultiplier: (rules?.rollingResistanceMultiplier ?? 1) * (player.ballForm === 'sticky' ? 2.5 : 1),
  wallRestitutionMultiplier: (rules?.wallRestitutionMultiplier ?? 1) * (player.ballForm === 'sticky' ? .35 : 1),
  terrainAccelerationMultiplier: rules?.terrainAccelerationMultiplier,
  hazardImpulseMultiplier: rules?.hazardImpulseMultiplier,
  cupRadius: (rules?.cupRadius ?? .28) * (player.ballForm === 'orbit' ? 1.28 : 1) * (player.upgrades.includes('cup reader') ? 1.18 : 1),
  cupMagnet: player.cupMagnetArmed,
  slipstream: player.slipstreamArmed,
  reboundRig: player.reboundRigArmed,
  chipGravityMultiplier: (player.ballForm === 'glider' ? .58 : 1) * (player.upgrades.includes('aerial ace') ? .82 : 1),
});

export const resetPlayerForCourse = (player: Player, course: Course, rules?: HoleRules) => {
  player.ball = newBall(course);
  player.inventory = rules?.powerUps ? rules.startingPowerUp : undefined;
  player.spareInventory = undefined;
  player.ballForm = undefined;
  player.portalExitId = undefined;
  player.twoPuttsArmed = undefined;
  player.upgrades = [...(rules?.sharedBoons ?? [])];
  player.secondWindAvailable = player.upgrades.includes('second wind');
  player.turboArmed = false;
  player.frozenTurns = undefined;
  player.hazardShield = player.upgrades.includes('hazard shield');
  player.cupMagnetArmed = undefined;
  player.slipstreamArmed = undefined;
  player.reboundRigArmed = undefined;
  player.sandbagged = undefined;
  player.forcedChip = undefined;
};

export const canStorePowerUp = (player: Player) => !player.inventory || (player.upgrades.includes('scavenger') && !player.spareInventory);

export const storePowerUp = (player: Player, powerUp: PowerUp) => {
  if (!player.inventory) {
    player.inventory = powerUp;
    return true;
  }
  if (player.upgrades.includes('scavenger') && !player.spareInventory) {
    player.spareInventory = powerUp;
    return true;
  }
  return false;
};

export const takePowerUp = (player: Player, powerUp: PowerUp) => {
  if (player.inventory === powerUp) {
    player.inventory = player.spareInventory;
    player.spareInventory = undefined;
    return true;
  }
  if (player.spareInventory === powerUp) {
    player.spareInventory = undefined;
    return true;
  }
  return false;
};
