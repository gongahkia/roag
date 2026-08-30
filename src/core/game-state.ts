import { defaultHoleRules, defaultTerrainSettings, generateCourse, generateCoursePackages } from './generator';
import { expandCourseAtCup, type CourseExpansion } from './campaign';
import { elapsedMsForPhase } from './hazards';
import { resetPlayerForCourse } from './player-effects';
import { newBall } from './physics';
import { Random } from './random';
import { COURSE_HEIGHT, COURSE_WIDTH, type ChaosModifier, type Course, type CoursePackage, type DieState, type GameConfig, type GameState, type PlannedHole, type Player, type SlotReel, type SlotReelKind, type SlotStop, type SlotWager } from './types';

const colors = ['#f6c26b', '#8bd5ca', '#f38ba8', '#cba6f7', '#a6e3a1', '#89b4fa', '#fab387', '#f9e2af', '#94e2d5', '#eba0ac', '#b4befe', '#f5c2e7'];

export const defaultConfig = (): GameConfig => ({
  seed: `enemy-${Math.random().toString(36).slice(2, 8)}`,
  holeCount: 9,
  botCount: 3,
  humanCount: 1,
  botSkill: 5,
  courseWidth: COURSE_WIDTH,
  courseHeight: COURSE_HEIGHT,
  skipDieBets: false,
});

export const addMessage = (state: GameState, message: string) => {
  state.messages = [message, ...state.messages].slice(0, 5);
};

export const activePlayer = (state: GameState) => state.players[state.turn.playerIndex]!;

const emptyPlayer = (id: string, index: number, kind: Player['kind'], skill: Player['skill'], course: Course): Player => ({
  id,
  name: kind === 'bot' ? `enemy-${index + 1}` : `golfer-${index + 1}`,
  color: colors[index]!,
  kind,
  skill,
  ball: newBall(course),
  upgrades: [],
  cash: 10,
  caddies: [],
  pockets: [],
  attachments: [],
  shotHistory: [],
  total: 0,
});

export const cloneCourse = (course: Course): Course => ({
  ...course,
  tiles: course.tiles.map((tile) => ({ ...tile, corners: tile.corners ? [...tile.corners] as [number, number, number, number] : undefined, direction: tile.direction ? { ...tile.direction } : undefined })),
  route: course.route.map((point) => ({ ...point })),
  tee: { ...course.tee },
  cup: { ...course.cup },
  hazards: course.hazards.map((hazard) => hazard.kind === 'updraft' ? { ...hazard, point: { ...hazard.point }, direction: { ...hazard.direction } } : { ...hazard, point: { ...hazard.point } }),
  theme: course.theme ?? 'balanced',
  archetype: course.archetype ?? 'ribbon',
  sizeProfile: course.sizeProfile ?? 'standard',
  features: (course.features ?? []).map((feature) => feature.kind === 'sinkhole'
    ? { ...feature, entrance: { ...feature.entrance }, exit: { ...feature.exit } }
    : feature.kind === 'pulse' || feature.kind === 'gust'
      ? { ...feature, point: { ...feature.point }, direction: { ...feature.direction } }
      : { ...feature, point: { ...feature.point } }),
  portals: course.portals?.map((pair) => ({ ...pair, entrance: pair.entrance ? { point: { ...pair.entrance.point }, direction: { ...pair.entrance.direction } } : undefined, exit: pair.exit ? { point: { ...pair.exit.point }, direction: { ...pair.exit.direction } } : undefined })),
  itemPads: course.itemPads.map((pad) => ({ ...pad, point: { ...pad.point } })),
});

/** upgrades snapshots written before biome features and gadgets without discarding their match state. */
export const normalizeGameState = (state: GameState): GameState => {
  state.config.courseWidth ??= COURSE_WIDTH;
  state.config.courseHeight ??= COURSE_HEIGHT;
  const legacy = state as unknown as Omit<GameState, 'status' | 'config'> & {
    status: string;
    vote?: { options?: CoursePackage[] };
    assembly?: unknown;
    config: GameConfig & { skipVoting?: boolean };
  };
  const hadLegacyVotingConfig = Object.hasOwn(legacy.config, 'skipVoting');
  state.config.skipDieBets ??= legacy.config.skipVoting === true;
  delete legacy.config.skipVoting;
  const normalizeTerrain = (terrain: ReturnType<typeof defaultTerrainSettings>) => {
    terrain.archetype ??= 'ribbon';
    terrain.sizeProfile ??= 'standard';
    terrain.cushionRate ??= 0;
    terrain.springRate ??= 0;
    terrain.bumperCount ??= 0;
    terrain.gustCount ??= 0;
    terrain.sinkholePairs ??= 0;
    terrain.thornCount ??= 0;
    terrain.pulseCount ??= 0;
    terrain.updraftCount ??= 0;
    terrain.lowBarCount ??= 0;
    terrain.airRingCount ??= 0;
    terrain.width ??= COURSE_WIDTH;
    terrain.height ??= COURSE_HEIGHT;
  };
  const normalizeCourse = (course: Course) => {
    course.theme ??= 'balanced';
    course.archetype ??= 'ribbon';
    course.sizeProfile ??= 'standard';
    course.features ??= [];
    course.portals ??= [];
    course.itemPads ??= [];
  };
  normalizeCourse(state.course);
  const legacyDie = state.die as unknown as { faces?: CoursePackage[]; reels?: SlotReel[] } | undefined;
  if (legacyDie?.faces && !legacyDie.reels) state.die = newDie(state.config, state.hole, state.players);
  state.gadgets ??= [];
  state.hazardElapsedMs ??= elapsedMsForPhase(state.coursePhase ?? 0);
  state.holeFinishSequence ??= 0;
  state.cardSequence ??= 0;
  if (state.shop) state.shop.opening ??= false;
  state.players.forEach((player) => {
    player.cash ??= 10;
    player.caddies ??= player.upgrades.map((id) => ({ id, stacks: 1 }));
    player.pockets ??= [player.inventory, player.spareInventory].filter(Boolean).map((id, index) => ({ id: id!, source: 'pad' as const, instanceId: `legacy:${player.id}:${index}` }));
    player.pockets.forEach((card, index) => { card.instanceId ??= `legacy:${player.id}:${index}`; });
    player.attachments ??= [];
    player.shotHistory ??= [];
    player.upgrades = player.caddies.map((caddy) => caddy.id);
  });
  state.coursePlan ??= [];
  if (hadLegacyVotingConfig && !state.config.skipDieBets) state.coursePlan = state.coursePlan.slice(0, state.hole);
  state.coursePlan.forEach((plan) => normalizeTerrain(plan.recipe.terrain));
  if (state.status !== 'rolling' && state.coursePlan.length < state.hole) {
    state.coursePlan.push({ id: `legacy-hole-${state.hole}`, label: `legacy hole ${state.hole}`, courseSeed: state.course.seed, recipe: { terrain: defaultTerrainSettings(), rules: { ...state.holeRules, sharedBoons: [...state.holeRules.sharedBoons] } } });
  }
  if (legacy.status === 'assembling') {
    delete legacy.assembly;
    state.status = 'playing';
  }
  if (legacy.status === 'voting') {
    delete legacy.vote;
    state.status = 'rolling';
    state.die = newDie(state.config, state.hole, state.players);
  }
  if (state.status === 'rolling' && !state.die) state.die = newDie(state.config, state.hole, state.players);
  state.emotes ??= [];
  state.emoteSequence ??= 0;
  return state;
};

const clonePlan = (plan: PlannedHole): PlannedHole => ({ ...plan, recipe: { terrain: { ...plan.recipe.terrain }, rules: { ...plan.recipe.rules, sharedBoons: [...plan.recipe.rules.sharedBoons] } } });

export const courseForPlan = (plan: PlannedHole) => generateCourse(plan.courseSeed, plan.recipe.terrain, plan.recipe.rules.hazardPhaseCount);

const applyReality = (course: Course, reality: GameState['queuedReality']) => {
  if (reality === 'void is fairway') course.tiles.forEach((tile) => { if (tile.surface === 'void') tile.surface = 'fairway'; });
  if (reality === 'fairway is ice') course.tiles.forEach((tile) => { if (tile.surface === 'fairway') tile.surface = 'ice'; });
  if (reality === 'spring fling') course.tiles.forEach((tile) => { if (tile.surface === 'booster') tile.surface = 'spring'; });
  if (reality === 'cushion league') course.tiles.forEach((tile) => { if (tile.surface === 'sand') tile.surface = 'cushion'; });
};

const activatePlan = (state: GameState, plan: PlannedHole) => {
  state.course = cloneCourse(courseForPlan(plan));
  state.activeReality = state.queuedReality;
  applyReality(state.course, state.queuedReality);
  state.queuedReality = undefined;
  state.holeRules = { ...plan.recipe.rules, sharedBoons: [...plan.recipe.rules.sharedBoons] };
  state.hazardElapsedMs = 0;
  state.coursePhase = 0;
  state.holeFinishSequence = 0;
  state.gadgets = [];
  state.players.forEach((player) => resetPlayerForCourse(player, state.course, state.holeRules));
  state.turn = { playerIndex: 0, secondsLeft: state.holeRules.timerSeconds, shotInFlight: false, cardPlayed: false };
};

/** Uses the same deterministic stitch for the client preview and the authoritative transition. */
export const expansionForTransition = (state: Pick<GameState, 'course' | 'transition' | 'queuedReality'>): CourseExpansion | undefined => {
  if (!state.transition) return undefined;
  const next = cloneCourse(courseForPlan(state.transition.next));
  applyReality(next, state.queuedReality);
  return expandCourseAtCup(state.course, next);
};

const activateExpansion = (state: GameState, plan: PlannedHole) => {
  const expansion = expansionForTransition(state);
  if (!expansion) return;
  state.course = expansion.course;
  state.activeReality = state.queuedReality;
  state.queuedReality = undefined;
  state.holeRules = { ...plan.recipe.rules, sharedBoons: [...plan.recipe.rules.sharedBoons] };
  state.hazardElapsedMs = 0;
  state.coursePhase = 0;
  state.holeFinishSequence = 0;
  state.gadgets = [];
  state.players.forEach((player) => resetPlayerForCourse(player, state.course, state.holeRules));
  state.turn = { playerIndex: 0, secondsLeft: state.holeRules.timerSeconds, shotInFlight: false, cardPlayed: false };
};

const courseDimensionsFor = (config: GameConfig) => ({ width: config.courseWidth ?? COURSE_WIDTH, height: config.courseHeight ?? COURSE_HEIGHT });

const SLOT_SECONDS = 18;
const SLOT_SPIN_SECONDS = 1.15;
const REVEAL_SECONDS = 8;
const REROLL_TARGETS = [3, 5] as const;
const CHAOS_MODIFIERS: readonly ChaosModifier[] = ['fast greens', 'bumper bank', 'weather front', 'hazard bloom'];
const reelLabel: Record<SlotReelKind, string> = { biome: 'biome', layout: 'layout', rules: 'rules', chaos: 'chaos' };

const cloneRules = (rules: ReturnType<typeof defaultHoleRules>) => ({ ...rules, sharedBoons: [...rules.sharedBoons] });
const cloneStop = (stop: SlotStop): SlotStop => ({ ...stop, terrain: stop.terrain ? { ...stop.terrain } : undefined, rules: stop.rules ? cloneRules(stop.rules) : undefined, augmentations: { ...stop.augmentations } });
const cloneReel = (reel: SlotReel): SlotReel => ({ ...reel, stops: reel.stops.map(cloneStop) });

const sourcePackage = (config: GameConfig, hole: number, kind: SlotReelKind, index: number) => {
  const packages = generateCoursePackages(`${config.seed}:slots:${hole}:${kind}:batch:${Math.floor(index / 3)}`, hole, courseDimensionsFor(config));
  return packages[index % packages.length]!;
};

const stopFromPackage = (config: GameConfig, hole: number, kind: Exclude<SlotReelKind, 'chaos'>, index: number, addedBy?: string): SlotStop => {
  const source = sourcePackage(config, hole, kind, index);
  if (kind === 'biome') return { id: `hole-${hole}-${kind}-${index + 1}`, label: source.recipe.terrain.theme.replace(/-/g, ' '), theme: source.recipe.terrain.theme, weight: 1, addedBy, augmentations: {} };
  if (kind === 'layout') return { id: `hole-${hole}-${kind}-${index + 1}`, label: `${source.recipe.terrain.archetype} · ${source.recipe.terrain.sizeProfile}`, terrain: { ...source.recipe.terrain }, archetype: source.recipe.terrain.archetype, sizeProfile: source.recipe.terrain.sizeProfile, weight: 1, addedBy, augmentations: {} };
  return { id: `hole-${hole}-${kind}-${index + 1}`, label: `${source.recipe.rules.timerSeconds}s · cap ${source.recipe.rules.strokeCap}`, rules: cloneRules(source.recipe.rules), weight: 1, addedBy, augmentations: {} };
};

const chaosStop = (hole: number, reelIndex: number, index: number): SlotStop => {
  const chaos = CHAOS_MODIFIERS[index % CHAOS_MODIFIERS.length]!;
  return { id: `hole-${hole}-chaos-${reelIndex + 1}-${index + 1}`, label: chaos, chaos, weight: 1, augmentations: {} };
};

const reelFor = (config: GameConfig, hole: number, kind: Exclude<SlotReelKind, 'chaos'>): SlotReel => ({
  id: kind,
  kind,
  label: reelLabel[kind],
  stops: Array.from({ length: 3 }, (_, index) => stopFromPackage(config, hole, kind, index)),
});

const chaosReel = (hole: number, index: number): SlotReel => ({
  id: `chaos-${index + 1}`,
  kind: 'chaos',
  label: `chaos ${index + 1}`,
  stops: Array.from({ length: CHAOS_MODIFIERS.length }, (_, stopIndex) => chaosStop(hole, index, stopIndex)),
});

const wagersFor = (players: readonly Player[]) => Object.fromEntries(players.map((player) => [player.id, { addedStops: 0, augmentations: {}, ready: false } satisfies SlotWager]));

const newDie = (config: GameConfig, hole: number, players: readonly Player[]): DieState => ({
  reels: [reelFor(config, hole, 'biome'), reelFor(config, hole, 'layout'), reelFor(config, hole, 'rules')],
  wagers: wagersFor(players),
  secondsLeft: SLOT_SECONDS,
  phase: 'wagering',
  rerolls: 0,
});

const selectedStop = (die: DieState, reelId: string, stopId: string) => die.reels.find((reel) => reel.id === reelId)?.stops.find((stop) => stop.id === stopId);
const selectedStops = (die: DieState) => die.roll?.stopIds.map((stopId, index) => selectedStop(die, die.reels[index]?.id ?? '', stopId)).filter((stop): stop is SlotStop => Boolean(stop)) ?? [];

const applyChaos = (terrain: ReturnType<typeof defaultTerrainSettings>, rules: ReturnType<typeof defaultHoleRules>, chaos: ChaosModifier) => {
  if (chaos === 'fast greens') {
    rules.rollingResistanceMultiplier = Math.max(.55, rules.rollingResistanceMultiplier * .8);
    rules.scoreMultiplier = Math.max(rules.scoreMultiplier, 1.25);
  }
  if (chaos === 'bumper bank') {
    terrain.bumperCount += 2;
    terrain.wallCount += 2;
  }
  if (chaos === 'weather front') {
    terrain.gustCount += 2;
    terrain.updraftCount += 1;
  }
  if (chaos === 'hazard bloom') {
    terrain.sweeperCount += 1;
    terrain.gateCount += 1;
    terrain.thornCount += 1;
    terrain.pulseCount += 1;
  }
};

const planFromRoll = (state: GameState, die: DieState): PlannedHole | undefined => {
  const [biome, layout, rules, ...chaos] = selectedStops(die);
  if (!biome?.theme || !layout?.terrain || !rules?.rules) return undefined;
  const terrain = { ...layout.terrain, theme: biome.theme };
  const holeRules = cloneRules(rules.rules);
  chaos.forEach((stop) => { if (stop.chaos) applyChaos(terrain, holeRules, stop.chaos); });
  const labels = [biome.label, layout.label, rules.label, ...chaos.map((stop) => stop.label)];
  const ids = die.roll?.stopIds.join(':') ?? 'unknown';
  return {
    id: `hole-${state.hole}-slot-${die.rerolls}-${ids}`,
    label: labels.join(' · '),
    courseSeed: `${state.config.seed}:slots:${state.hole}:${die.rerolls}:${ids}`,
    recipe: { terrain, rules: holeRules },
  };
};

const quickStartPlan = (config: GameConfig): PlannedHole[] => Array.from({ length: config.holeCount }, (_, index) => {
  const hole = index + 1;
  const die = newDie(config, hole, []);
  die.roll = { stopIds: die.reels.map((reel, reelIndex) => reel.stops[new Random(`${config.seed}:quick-slot:${hole}:${reelIndex}`).int(0, reel.stops.length - 1)]!.id), secondsLeft: 0 };
  const state = { config, hole } as GameState;
  return planFromRoll(state, die)!;
});

export const createGameState = (config: GameConfig): GameState => {
  const resolvedConfig = { ...config, skipDieBets: config.skipDieBets === true };
  const coursePlan = resolvedConfig.skipDieBets ? quickStartPlan(resolvedConfig) : [];
  const firstPlan = coursePlan[0];
  const starterDie = firstPlan ? undefined : newDie(resolvedConfig, 1, []);
  const course = firstPlan ? cloneCourse(courseForPlan(firstPlan)) : cloneCourse(sourcePackage(resolvedConfig, 1, 'layout', 0).course);
  const holeRules = firstPlan ? { ...firstPlan.recipe.rules, sharedBoons: [...firstPlan.recipe.rules.sharedBoons] } : defaultHoleRules();
  const players = Array.from({ length: config.humanCount }, (_, index) => emptyPlayer(`human-${index}`, index, 'human', 0, course));
  players.push(...Array.from({ length: config.botCount }, (_, index) => emptyPlayer(`bot-${index}`, players.length + index, 'bot', config.botSkill, course)));
  const die = starterDie ? { ...starterDie, wagers: wagersFor(players) } : undefined;
  if (firstPlan) players.forEach((player) => resetPlayerForCourse(player, course, holeRules));
  const state: GameState = {
    config: resolvedConfig,
    course,
    holeRules,
    hole: 1,
    holeFinishSequence: 0,
    hazardElapsedMs: 0,
    coursePhase: 0,
    die,
    coursePlan,
    emotes: [],
    emoteSequence: 0,
    cardSequence: 0,
    players,
    gadgets: [],
    turn: { playerIndex: 0, secondsLeft: holeRules.timerSeconds, shotInFlight: false, cardPlayed: false },
    paused: false,
    status: firstPlan ? 'playing' : 'rolling',
    messages: firstPlan ? [`quick start selected ${coursePlan.length} random courses — tee off`] : ['add reel stops, weight a stop, then pull the course slots'],
  };
  return state;
};

export const cloneGameState = (state: GameState): GameState => ({
  ...state,
  course: cloneCourse(state.course),
  holeRules: { ...state.holeRules, sharedBoons: [...state.holeRules.sharedBoons] },
  die: state.die ? {
    ...state.die,
    reels: state.die.reels.map(cloneReel),
    wagers: Object.fromEntries(Object.entries(state.die.wagers).map(([playerId, wager]) => [playerId, { ...wager, augmentations: { ...wager.augmentations } }])),
    roll: state.die.roll ? { ...state.die.roll, stopIds: [...state.die.roll.stopIds] } : undefined,
    revealed: state.die.revealed ? { plan: clonePlan(state.die.revealed.plan), secondsLeft: state.die.revealed.secondsLeft } : undefined,
    rerollPot: state.die.rerollPot ? { ...state.die.rerollPot, contributions: { ...state.die.rerollPot.contributions } } : undefined,
  } : undefined,
  coursePlan: (state.coursePlan ?? []).map(clonePlan),
  transition: state.transition ? { next: clonePlan(state.transition.next) } : undefined,
  emotes: state.emotes.map((emote) => ({ ...emote })),
  players: state.players.map((player) => ({ ...player, ball: { ...player.ball }, upgrades: [...player.upgrades], caddies: player.caddies.map((caddy) => ({ ...caddy })), pockets: player.pockets.map((pocket) => ({ ...pocket, duration: pocket.duration ? { ...pocket.duration } : undefined })), attachments: (player.attachments ?? []).map((attachment) => ({ ...attachment })), shotHistory: player.shotHistory.map((entry) => ({ ...entry, before: { ...entry.before }, after: { ...entry.after } })) })),
  gadgets: (state.gadgets ?? []).map((gadget) => ({ ...gadget, point: { ...gadget.point } })),
  shop: state.shop ? { ...state.shop, shelf: state.shop.shelf.map((offer) => ({ ...offer })), buyerOrder: [...state.shop.buyerOrder], completedBuyerIds: [...state.shop.completedBuyerIds], rerollVotes: { ...state.shop.rerollVotes } } : undefined,
  turn: { ...state.turn },
  messages: [...state.messages],
});

const wagerFor = (die: DieState, playerId: string) => die.wagers[playerId] ??= { addedStops: 0, augmentations: {}, ready: false };
const isWagering = (die: DieState) => die.phase === 'wagering' || die.phase === 'reroll-wagering';
const totalWeight = (reel: SlotReel) => reel.stops.reduce((total, stop) => total + stop.weight, 0);

const startDieRoll = (state: GameState) => {
  const die = state.die;
  if (!die || !isWagering(die)) return;
  const selected = die.reels.map((reel, reelIndex) => {
    const random = new Random(`${state.config.seed}:slot:${state.hole}:${die.rerolls}:${reel.id}:${reel.stops.map((stop) => `${stop.id}:${stop.weight}`).join('|')}`);
    let remaining = random.next() * totalWeight(reel);
    let stop = reel.stops[reel.stops.length - 1]!;
    for (const candidate of reel.stops) {
      remaining -= candidate.weight;
      if (remaining < 0) { stop = candidate; break; }
    }
    return stop.id;
  });
  die.roll = { stopIds: selected, secondsLeft: SLOT_SPIN_SECONDS + Math.max(0, die.reels.length - 3) * .14 };
  die.phase = 'spinning';
  addMessage(state, `${die.reels.length} course reels start to spin`);
};

const commitPlan = (state: GameState, planned: PlannedHole) => {
  state.coursePlan = [...(state.coursePlan ?? []), planned];
  state.die = undefined;
  if (state.hole === 1) {
    activatePlan(state, planned);
    state.status = 'playing';
    addMessage(state, `${planned.label} locks in — tee off`);
    return;
  }
  state.transition = { next: clonePlan(planned) };
  state.gadgets = [];
  state.paused = false;
  state.status = 'transitioning';
  addMessage(state, `${planned.label} locks in — rebuilding hole ${state.hole}`);
};

const resolveDieRoll = (state: GameState) => {
  const die = state.die;
  if (!die) return;
  const planned = planFromRoll(state, die);
  if (!planned) return;
  die.phase = 'revealed';
  die.revealed = { plan: planned, secondsLeft: REVEAL_SECONDS };
  die.rerollPot = die.rerolls < REROLL_TARGETS.length ? { target: REROLL_TARGETS[die.rerolls]!, contributions: {}, secondsLeft: REVEAL_SECONDS } : undefined;
  addMessage(state, `${planned.label} is on the payline`);
};

export const addSlotStop = (state: GameState, playerId: string, reelId: string) => {
  const die = state.die;
  if (state.status !== 'rolling' || !die || !isWagering(die)) return;
  const player = state.players.find((candidate) => candidate.id === playerId);
  const reel = die.reels.find((candidate) => candidate.id === reelId && candidate.kind !== 'chaos');
  if (!player || !reel) return;
  const wager = wagerFor(die, playerId);
  const cost = 1 + wager.addedStops;
  if (player.cash < cost) return;
  player.cash -= cost;
  wager.addedStops += 1;
  reel.stops.push(stopFromPackage(state.config, state.hole, reel.kind as Exclude<SlotReelKind, 'chaos'>, reel.stops.length, playerId));
  addMessage(state, `${player.name} loads a wild ${reel.label} ticket for $${cost}`);
};

export const augmentSlotStop = (state: GameState, playerId: string, reelId: string, stopId: string) => {
  const die = state.die;
  if (state.status !== 'rolling' || !die || !isWagering(die)) return;
  const player = state.players.find((candidate) => candidate.id === playerId);
  const reel = die.reels.find((candidate) => candidate.id === reelId);
  const stop = reel?.stops.find((candidate) => candidate.id === stopId);
  if (!player || !reel || !stop) return;
  const wager = wagerFor(die, playerId);
  const key = `${reel.id}:${stop.id}`;
  const existing = wager.augmentations[key] ?? 0;
  const cost = 1 + Math.floor(existing / 2);
  if (player.cash < cost) return;
  player.cash -= cost;
  wager.augmentations[key] = existing + 1;
  stop.augmentations[playerId] = (stop.augmentations[playerId] ?? 0) + 1;
  stop.weight += 1;
  addMessage(state, `${player.name} loads another ${stop.label} ticket for $${cost}`);
};

export const addChaosReel = (state: GameState, playerId: string) => {
  const die = state.die;
  if (state.status !== 'rolling' || !die || die.phase !== 'reroll-wagering') return;
  const player = state.players.find((candidate) => candidate.id === playerId);
  const chaosCount = die.reels.filter((reel) => reel.kind === 'chaos').length;
  const cost = 2 + chaosCount;
  if (!player || chaosCount >= 2 || player.cash < cost) return;
  player.cash -= cost;
  die.reels.push(chaosReel(state.hole, chaosCount));
  addMessage(state, `${player.name} bolts on chaos reel ${chaosCount + 1} for $${cost}`);
};

export const contributeReroll = (state: GameState, playerId: string) => {
  const die = state.die;
  if (state.status !== 'rolling' || !die || die.phase !== 'revealed' || !die.rerollPot) return;
  const player = state.players.find((candidate) => candidate.id === playerId);
  if (!player || player.cash < 1) return;
  player.cash -= 1;
  die.rerollPot.contributions[playerId] = (die.rerollPot.contributions[playerId] ?? 0) + 1;
  const total = Object.values(die.rerollPot.contributions).reduce((sum, amount) => sum + amount, 0);
  if (total < die.rerollPot.target) return;
  die.rerolls += 1;
  die.phase = 'reroll-wagering';
  die.secondsLeft = SLOT_SECONDS;
  die.roll = undefined;
  die.revealed = undefined;
  die.rerollPot = undefined;
  Object.values(die.wagers).forEach((wager) => { wager.ready = false; });
  addMessage(state, `the shared reroll pot lands — load the machine again`);
};

export const readySlotSpin = (state: GameState, playerId: string) => {
  const die = state.die;
  if (state.status !== 'rolling' || !die || !isWagering(die) || !state.players.some((player) => player.id === playerId)) return;
  const wager = wagerFor(die, playerId);
  if (wager.ready) return;
  wager.ready = true;
  const player = state.players.find((candidate) => candidate.id === playerId)!;
  addMessage(state, `${player.name} is ready to pull the lever`);
  if (state.players.every((candidate) => wagerFor(die, candidate.id).ready)) startDieRoll(state);
};

export const tickDie = (state: GameState, elapsedSeconds: number) => {
  if (state.status !== 'rolling' || !state.die || state.paused) return false;
  const die = state.die;
  const elapsed = Math.max(0, Number.isFinite(elapsedSeconds) ? elapsedSeconds : 0);
  if (die.phase === 'spinning' && die.roll) {
    die.roll.secondsLeft = Math.max(0, die.roll.secondsLeft - elapsed);
    if (die.roll.secondsLeft === 0) resolveDieRoll(state);
    return true;
  }
  if (die.phase === 'revealed' && die.revealed) {
    die.revealed.secondsLeft = Math.max(0, die.revealed.secondsLeft - elapsed);
    if (die.rerollPot) die.rerollPot.secondsLeft = die.revealed.secondsLeft;
    if (die.revealed.secondsLeft === 0) {
      if (die.rerollPot) Object.entries(die.rerollPot.contributions).forEach(([playerId, amount]) => {
        const player = state.players.find((candidate) => candidate.id === playerId);
        if (player) player.cash += amount;
      });
      commitPlan(state, die.revealed.plan);
    }
    return true;
  }
  die.secondsLeft = Math.max(0, die.secondsLeft - elapsed);
  if (die.secondsLeft === 0) {
    state.players.forEach((player) => { wagerFor(die, player.id).ready = true; });
    startDieRoll(state);
  }
  return true;
};

export const completeTransition = (state: GameState) => {
  if (state.status !== 'transitioning' || !state.transition) return;
  activateExpansion(state, state.transition.next);
  state.transition = undefined;
  state.status = 'playing';
  addMessage(state, `hole ${state.hole} is ready — tee off`);
};

export const setPaused = (state: GameState, paused: boolean) => {
  if (state.status === 'finished') return;
  state.paused = paused;
  addMessage(state, paused ? 'match paused' : 'match resumed');
};

export const beginCourseTransition = (state: GameState) => {
  const nextHole = state.hole + 1;
  state.hole = nextHole;
  state.gadgets = [];
  state.paused = false;
  if (state.config.skipDieBets) {
    const next = state.coursePlan[nextHole - 1];
    if (!next) return;
    state.transition = { next: clonePlan(next) };
    state.status = 'transitioning';
    addMessage(state, `hole ${state.hole - 1} complete — automatic slot result rebuilds hole ${state.hole}`);
    return;
  }
  state.die = newDie(state.config, nextHole, state.players);
  state.status = 'rolling';
  addMessage(state, `hole ${state.hole - 1} complete — place bets for hole ${state.hole}`);
};
