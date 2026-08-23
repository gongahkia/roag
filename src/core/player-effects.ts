import { newBall, type BallPhysicsModifiers } from './physics';
import { attachmentsFor, hasAttachment } from './strategy';
import type { BallForm, CaddyId, ChronoCard, Course, HoleRules, Player, PocketCard, PowerUp, ShotCommand, Upgrade } from './types';

export const BALL_FORMS: readonly BallForm[] = ['heavy', 'bouncy', 'ghost', 'magnet', 'ice', 'portal', 'glider', 'sticky', 'orbit', 'quantum', 'mirror', 'anvil', 'vampire', 'boomerang'];
export const GADGET_POWER_UPS: readonly PowerUp[] = ['popper pad', 'snare patch', 'blast mine', 'slick patch', 'sky spring', 'gravity well', 'mirror plate', 'toll booth', 'control inverter', 'portal gun'];
export const RECOVERY_POWER_UPS: readonly PowerUp[] = ['turbo', 'shield', 'two putts', 'bouncy', 'ice', 'magnet', 'glider', 'sticky', 'orbit', 'boomerang', 'cup magnet', 'slipstream', 'rebound rig', 'rescue drone', 'red tee', 'gravity gloves', 'wind sock', 'slope stabilizer', 'spring polish', 'bumper wax', 'cushion map', 'popper pad', 'snare patch', 'blast mine', 'slick patch', 'sky spring', 'gravity well', 'mirror plate', 'toll booth', 'phase shift', 'sandbag'];
export const CHAOS_POWER_UPS: readonly PowerUp[] = ['turbo', 'shield', 'bomb', 'freeze', 'swap', 'two putts', 'heavy', 'bouncy', 'ghost', 'magnet', 'ice', 'portal', 'glider', 'sticky', 'orbit', 'quantum', 'mirror', 'anvil', 'vampire', 'boomerang', 'cup magnet', 'slipstream', 'rebound rig', 'rescue drone', 'airhorn', 'club flipper', 'time dilator', 'mugger', 'scramble', 'gravity gloves', 'bunker buster', 'portal remote', 'red tee', 'black flag', 'cherry bomb', 'copycat', 'wind sock', 'slope stabilizer', 'spring polish', 'bumper wax', 'cushion map', 'popper pad', 'snare patch', 'blast mine', 'slick patch', 'sky spring', 'gravity well', 'mirror plate', 'toll booth', 'control inverter', 'portal gun', 'phase shift', 'sandbag'];
export const UPGRADES: readonly Upgrade[] = ['heavy ball', 'ice skates', 'extra charge', 'bank shot', 'hazard shield', 'chaos magnet', 'portal savvy', 'second wind', 'scavenger', 'aerial ace', 'cup reader', 'gadgeteer', 'backboard', 'pinball wizard', 'rough rider', 'sand wedge', 'conveyor cultist', 'gatecrasher', 'thornmail', 'air mail', 'shock absorber', 'first responder', 'pickpocket', 'revenge club', 'headwind', 'bogeyman', 'coin slot', 'broker', 'echo chamber', 'paradox partner', 'hole hunter', 'black market caddy', 'cushion keeper', 'spring coach', 'bumper apprentice', 'slope scout', 'wind warden'];

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
  backboard: 'wall bounces gain speed',
  'pinball wizard': 'wall bounces pay cash',
  'rough rider': 'rough loses drag',
  'sand wedge': 'sand kicks forward',
  'conveyor cultist': 'stronger terrain acceleration',
  gatecrasher: 'phases closed gates',
  thornmail: 'thorns kick toward the cup',
  'air mail': 'air rings pay cash',
  'shock absorber': 'resists knockback',
  'first responder': 'safer void recovery',
  pickpocket: 'opponents picking items pays cash',
  'revenge club': 'being hit empowers the next shot',
  headwind: 'sinking slows opponents',
  bogeyman: 'stronger while behind',
  'coin slot': 'cash pads pay more',
  broker: 'cheaper caddies',
  'echo chamber': 'repeats a trigger',
  'paradox partner': 'one position rewind',
  'hole hunter': 'trick shots enlarge the cup',
  'black market caddy': 'contraband buys include a bonus card',
  'cushion keeper': 'cushion turf holds less drag',
  'spring coach': 'spring tiles launch higher and farther',
  'bumper apprentice': 'bumper banks retain more speed',
  'slope scout': 'downhill pull is reduced',
  'wind warden': 'gust lanes push less per stack',
};

export const isBallForm = (powerUp: PowerUp): powerUp is BallForm => BALL_FORMS.includes(powerUp as BallForm);

/** Legacy upgrades are normalized into Caddy stacks on first access. */
export const caddyStacks = (player: Player) => {
  player.caddies ??= [];
  if (!player.caddies.length && player.upgrades.length) player.caddies = player.upgrades.map((id) => ({ id, stacks: 1 }));
  return player.caddies;
};

export const caddyCount = (player: Player, id: CaddyId) => caddyStacks(player).find((caddy) => caddy.id === id)?.stacks ?? 0;
export const hasCaddy = (player: Player, id: CaddyId) => caddyCount(player, id) > 0;

export const addCaddy = (player: Player, id: CaddyId) => {
  const existing = caddyStacks(player).find((caddy) => caddy.id === id);
  if (existing) existing.stacks += 1;
  else player.caddies.push({ id, stacks: 1 });
  player.upgrades = player.caddies.map((caddy) => caddy.id);
};

export const removeCaddy = (player: Player, id: CaddyId) => {
  const existing = caddyStacks(player).find((caddy) => caddy.id === id);
  if (!existing) return false;
  existing.stacks -= 1;
  player.caddies = player.caddies.filter((caddy) => caddy.stacks > 0);
  player.upgrades = player.caddies.map((caddy) => caddy.id);
  return true;
};

export const syncPocketMirrors = (player: Player) => {
  const cards = player.pockets.filter((card): card is PocketCard & { id: PowerUp } => !['undo drive', 'second chance', 'echo putt', 'future sight', 'time theft', 'frozen frame', 'parallel parking', 'grandfather clause'].includes(card.id));
  player.inventory = cards[0]?.id;
  player.spareInventory = cards[1]?.id;
};

export const pocketsFor = (player: Player) => {
  player.pockets ??= [];
  if (!player.pockets.length) {
    if (player.inventory) player.pockets.push({ id: player.inventory, source: 'pad' });
    if (player.spareInventory) player.pockets.push({ id: player.spareInventory, source: 'pad' });
  }
  return player.pockets;
};

export const pocketCapacity = (player: Player) => 1 + caddyCount(player, 'scavenger');

export const adjustedShotFor = (player: Player, shot: ShotCommand, rules?: HoleRules): ShotCommand => {
  const multiplier = (player.turboArmed || hasAttachment(player, 'tailwind') ? 1.55 : 1)
    * (player.sandbagged || hasAttachment(player, 'sandbag slip') ? .7 : 1)
    * (hasAttachment(player, 'headwind gust') ? .8 : 1)
    * (1 + caddyCount(player, 'heavy ball') * .12)
    * (player.ballForm === 'heavy' ? 1.16 : 1)
    * (shot.kind === 'chip' ? 1 + caddyCount(player, 'aerial ace') * .1 : 1)
    * (player.timeDilated || hasAttachment(player, 'slow clock') ? .5 : 1)
    * (rules?.launchMultiplier ?? 1);
  const angle = player.controlInverted || hasAttachment(player, 'club flip') || hasAttachment(player, 'frayed grip') ? shot.angle + Math.PI : shot.angle;
  return { ...shot, angle, power: shot.power * multiplier };
};

export const physicsModifiersFor = (player: Player, rules?: HoleRules): BallPhysicsModifiers => {
  const form = player.ballForm ?? (hasAttachment(player, 'ghost pass') ? 'ghost' : hasAttachment(player, 'soft landing') ? 'sticky' : undefined);
  return {
  mass: (form === 'heavy' ? 1.65 : form === 'anvil' ? 2.35 : 1) + caddyCount(player, 'heavy ball') * .45,
  iceSkates: hasCaddy(player, 'ice skates') || form === 'ice',
  bankShot: hasCaddy(player, 'bank shot') || hasCaddy(player, 'backboard'),
  bouncy: form === 'bouncy',
  ghostBall: form === 'ghost',
  magnetBall: form === 'magnet',
  portalExitId: form === 'portal' ? player.portalExitId : undefined,
  portalSpeedMultiplier: (1 + caddyCount(player, 'portal savvy') * .18) * (rules?.portalSpeedMultiplier ?? 1),
  hazardShield: player.hazardShield || hasAttachment(player, 'guardian pin') || hasAttachment(player, 'windbreak') || hasAttachment(player, 'rescue pact') || hasCaddy(player, 'first responder') || (form === 'boomerang' && Boolean(player.redTee)),
  rollingResistanceMultiplier: (rules?.rollingResistanceMultiplier ?? 1) * (form === 'sticky' ? 2.5 : 1),
  wallRestitutionMultiplier: (rules?.wallRestitutionMultiplier ?? 1) * (form === 'sticky' ? .35 : 1),
  terrainAccelerationMultiplier: (rules?.terrainAccelerationMultiplier ?? 1) * (1 + caddyCount(player, 'conveyor cultist') * .2),
  hazardImpulseMultiplier: (rules?.hazardImpulseMultiplier ?? 1) * (hasAttachment(player, 'anchor line') ? .28 : 1) * Math.max(.35, 1 - caddyCount(player, 'shock absorber') * .22),
  cupRadius: (rules?.cupRadius ?? .28) * (form === 'orbit' ? 1.28 : 1) * (hasAttachment(player, 'line reader') ? 1.18 : 1) * (hasAttachment(player, 'steady hands') ? 1.14 : 1) * (1 + caddyCount(player, 'cup reader') * .18),
  cupMagnet: player.cupMagnetArmed || hasAttachment(player, 'line reader'),
  slipstream: player.slipstreamArmed || hasAttachment(player, 'fairway draft') || hasAttachment(player, 'shared draft'),
  reboundRig: player.reboundRigArmed || hasAttachment(player, 'banker advice'),
  chipGravityMultiplier: (form === 'glider' ? .58 : 1) * Math.max(.35, 1 - caddyCount(player, 'aerial ace') * .18),
  roughRider: hasCaddy(player, 'rough rider'),
  sandWedge: hasCaddy(player, 'sand wedge'),
  gatecrasher: hasCaddy(player, 'gatecrasher'),
  thornmail: hasCaddy(player, 'thornmail'),
  anvilBall: form === 'anvil',
  mirrorBall: form === 'mirror',
  cushionDragMultiplier: (player.cushionMapped || hasAttachment(player, 'cushion call') ? .42 : 1) * (hasAttachment(player, 'sticky forecast') ? 1.7 : 1) * Math.max(.3, 1 - caddyCount(player, 'cushion keeper') * .2),
  springLiftMultiplier: (player.springPolished || hasAttachment(player, 'spring ticket') ? 1.42 : 1) * (1 + caddyCount(player, 'spring coach') * .16),
  bumperRestitutionMultiplier: (player.bumperWaxed || hasAttachment(player, 'bumper lease') ? 1.28 : 1) * (hasAttachment(player, 'dead bounce') ? .45 : 1) * (1 + caddyCount(player, 'bumper apprentice') * .13),
  slopeGravityMultiplier: (player.slopeStabilized || hasAttachment(player, 'anchor line') ? .25 : 1) * (hasAttachment(player, 'grounds crew') ? .55 : 1) * Math.max(.3, 1 - caddyCount(player, 'slope scout') * .18),
  gustMultiplier: (player.gustReversed ? -1 : 1) * (hasAttachment(player, 'crosswind debt') ? -.65 : 1) * (hasAttachment(player, 'wind sail') ? 1.45 : 1) * Math.max(.3, 1 - caddyCount(player, 'wind warden') * .2),
};
};

export const resetPlayerForCourse = (player: Player, course: Course, _rules?: HoleRules) => {
  player.ball = newBall(course);
  player.cash ??= 10;
  caddyStacks(player);
  player.pockets = pocketsFor(player).filter((card) => card.source === 'shop');
  player.attachments = attachmentsFor(player).filter((attachment) => attachment.unit === 'hole');
  syncPocketMirrors(player);
  player.ballForm = undefined;
  player.portalExitId = undefined;
  player.twoPuttsArmed = undefined;
  player.secondWindAvailable = hasCaddy(player, 'second wind');
  player.turboArmed = false;
  player.frozenTurns = undefined;
  player.hazardShield = hasCaddy(player, 'hazard shield');
  player.cupMagnetArmed = undefined;
  player.slipstreamArmed = undefined;
  player.reboundRigArmed = undefined;
  player.sandbagged = undefined;
  player.forcedChip = undefined;
  player.controlInverted = undefined;
  player.timeDilated = undefined;
  player.gustReversed = undefined;
  player.slopeStabilized = undefined;
  player.springPolished = undefined;
  player.bumperWaxed = undefined;
  player.cushionMapped = undefined;
  player.redTee = undefined;
  player.holeFinishOrder = undefined;
};

export const canStorePowerUp = (player: Player) => pocketsFor(player).length < pocketCapacity(player);

export const storePowerUp = (player: Player, powerUp: PowerUp, source: PocketCard['source'] = 'pad') => {
  if (!canStorePowerUp(player)) return false;
  const pockets = pocketsFor(player);
  pockets.push({ id: powerUp, source, instanceId: `${source}:${powerUp}:${pockets.length}` });
  syncPocketMirrors(player);
  return true;
};

export const takePowerUp = (player: Player, powerUp: PowerUp) => {
  return takePocketCard(player, powerUp);
};

export const takePocketCard = (player: Player, powerUp: PowerUp | ChronoCard, instanceId?: string) => {
  const pockets = pocketsFor(player);
  const index = pockets.findIndex((card) => card.id === powerUp && (!instanceId || card.instanceId === instanceId));
  if (index < 0) return false;
  pockets.splice(index, 1);
  syncPocketMirrors(player);
  return true;
};
