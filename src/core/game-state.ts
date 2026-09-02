import { courseHashFor, defaultHoleRules, defaultTerrainSettings, generateCourse, generateCoursePackages, validateCourse } from './generator';
import { expandCourseAtCup, type CourseExpansion } from './campaign';
import { elapsedMsForPhase } from './hazards';
import { resetPlayerForCourse } from './player-effects';
import { newBall } from './physics';
import { Random } from './random';
import { GENERATOR_VERSION, LEGACY_GENERATOR_VERSION, PARTY_TRICK_CARDS, RECIPE_SCHEMA_VERSION, generatorVersionFor, rulesetFor } from './rulesets';
import { COURSE_HEIGHT, COURSE_WIDTH, type ChaosModifier, type Course, type GameConfig, type GameState, type PlannedHole, type Player } from './types';

const colors = ['#f6c26b', '#8bd5ca', '#f38ba8', '#cba6f7', '#a6e3a1', '#89b4fa', '#fab387', '#f9e2af', '#94e2d5', '#eba0ac', '#b4befe', '#f5c2e7'];

export const defaultConfig = (): GameConfig => ({
  seed: `enemy-${Math.random().toString(36).slice(2, 8)}`,
  holeCount: 9,
  botCount: 3,
  humanCount: 1,
  botSkill: 5,
  courseWidth: COURSE_WIDTH,
  courseHeight: COURSE_HEIGHT,
  ruleset: 'party',
});

export const addMessage = (state: GameState, message: string) => {
  state.messages = [message, ...state.messages].slice(0, 5);
};

export const recordInstrumentation = (state: GameState, event: NonNullable<GameState['instrumentation']>['events'][number]) => {
  state.instrumentation ??= { events: [] };
  state.instrumentation.events = [...state.instrumentation.events, event].slice(-512);
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
  routeRoles: course.routeRoles?.map((assignment) => ({ ...assignment, marker: { ...assignment.marker }, points: assignment.points.map((point) => ({ ...point })) })),
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
  // Existing saved campaigns retain their wider catalogue; new campaigns default to Party Rules.
  state.config.ruleset ??= 'custom';
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
  state.connections ??= [];
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
  state.instrumentation ??= { events: [] };
  return state;
};

const clonePlan = (plan: PlannedHole): PlannedHole => ({
  ...plan,
  recipe: {
    terrain: { ...plan.recipe.terrain },
    rules: { ...plan.recipe.rules, sharedBoons: [...plan.recipe.rules.sharedBoons] },
    metadata: plan.recipe.metadata ? { ...plan.recipe.metadata, resolvedReels: { ...plan.recipe.metadata.resolvedReels, chaos: plan.recipe.metadata.resolvedReels.chaos ? [...plan.recipe.metadata.resolvedReels.chaos] : undefined }, contentIds: [...plan.recipe.metadata.contentIds], routeRoles: plan.recipe.metadata.routeRoles?.map((assignment) => ({ ...assignment, marker: { ...assignment.marker }, points: assignment.points.map((point) => ({ ...point })) })) } : undefined,
  },
});

/** Rebuild a planned hole with the generator that created its recorded recipe. */
export const courseForPlan = (plan: PlannedHole) => generateCourse(
  plan.courseSeed,
  plan.recipe.terrain,
  plan.recipe.rules.hazardPhaseCount,
  generatorVersionFor(plan.recipe.metadata?.generatorVersion ?? LEGACY_GENERATOR_VERSION),
);

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
  state.turn = { playerIndex: (state.hole - 1) % Math.max(1, state.players.length), secondsLeft: state.holeRules.timerSeconds, shotInFlight: false, cardPlayed: false };
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
  if (rulesetFor(state.config).id === 'party') {
    // The campaign atlas remains visible, while only the newly attached hole
    // continues simulating hazards, pads, portals, and biome interactions.
    const currentPrefix = `hole-${plan.id}:`;
    state.course.hazards = state.course.hazards.filter((hazard) => hazard.id.startsWith(currentPrefix));
    state.course.features = state.course.features.filter((feature) => feature.id.startsWith(currentPrefix));
    state.course.portals = state.course.portals?.filter((portal) => portal.id.startsWith(currentPrefix));
    state.course.itemPads = state.course.itemPads.filter((pad) => pad.id.startsWith(currentPrefix));
  }
  state.connections = [...(state.connections ?? []), {
    fromHole: state.hole - 1,
    toHole: state.hole,
    anchor: { ...expansion.anchor },
    offset: { ...expansion.offset },
    rotation: expansion.rotation,
    courseHash: courseHashFor(expansion.course),
  }];
  state.activeReality = state.queuedReality;
  state.queuedReality = undefined;
  state.holeRules = { ...plan.recipe.rules, sharedBoons: [...plan.recipe.rules.sharedBoons] };
  state.hazardElapsedMs = 0;
  state.coursePhase = 0;
  state.holeFinishSequence = 0;
  state.gadgets = [];
  state.players.forEach((player) => resetPlayerForCourse(player, state.course, state.holeRules));
  state.turn = { playerIndex: (state.hole - 1) % Math.max(1, state.players.length), secondsLeft: state.holeRules.timerSeconds, shotInFlight: false, cardPlayed: false };
};

const courseDimensionsFor = (config: GameConfig) => ({ width: config.courseWidth ?? COURSE_WIDTH, height: config.courseHeight ?? COURSE_HEIGHT });

const CHAOS_MODIFIERS: readonly ChaosModifier[] = ['fast greens', 'bumper bank', 'weather front', 'hazard bloom'];

const cloneRules = (rules: ReturnType<typeof defaultHoleRules>) => ({ ...rules, sharedBoons: [...rules.sharedBoons] });

const partyTerrain = (config: GameConfig, hole: number, themeIndex: number, layoutIndex: number) => {
  const ruleset = rulesetFor(config);
  const dimensions = courseDimensionsFor(config);
  const theme = ruleset.biomes?.[themeIndex % ruleset.biomes.length] ?? 'speedway';
  const archetype = ruleset.layouts?.[layoutIndex % ruleset.layouts.length] ?? 'ribbon';
  return {
    ...defaultTerrainSettings(),
    ...dimensions,
    theme,
    archetype,
    sizeProfile: 'standard' as const,
    laneWidth: archetype === 'ribbon' ? 2 : 1,
    branches: archetype === 'fork' ? 1 : 0,
    density: .44,
    noiseAmplitude: theme === 'quarry' ? .38 : theme === 'carnival' ? .32 : .24,
    chaos: .32,
    elevation: theme === 'quarry' ? .72 : .36,
    maxElevation: theme === 'quarry' ? 3 : 2,
    boosterRate: theme === 'speedway' ? .2 : .08,
    conveyorRate: theme === 'speedway' ? .17 : .06,
    springRate: theme === 'carnival' ? .16 : .04,
    bumperCount: theme === 'carnival' ? 3 : theme === 'quarry' ? 1 : 0,
    wallCount: theme === 'quarry' ? 3 : 1,
    sweeperCount: theme === 'speedway' ? 1 : 0,
    gateCount: theme === 'speedway' ? 1 : 0,
    portalPairs: 0,
    sinkholePairs: 0,
    thornCount: 0,
    pulseCount: 0,
    updraftCount: 0,
    lowBarCount: 0,
    airRingCount: theme === 'quarry' ? 1 : 0,
    gustCount: 0,
    variation: hole * 10 + themeIndex * 3 + layoutIndex,
  };
};

const partyHoleRules = (config: GameConfig, index: number) => ({
  ...defaultHoleRules(),
  timerSeconds: [14, 16, 18][index % 3]!,
  strokeCap: Math.max(8, Math.ceil((config.courseWidth ?? COURSE_WIDTH) / 6) + 3),
  collisions: true,
  powerUps: true,
  hazardPhaseCount: 8,
  scoreMultiplier: 1,
});


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

/**
 * A post-hole course must attach cleanly to the accumulated atlas. The expansion
 * selects a collision-free rotation or, for a crowded late atlas, a narrow
 * connector to a separated board. Seed variants remain a deterministic fallback.
 */
const appendCourseFor = (state: GameState, proposedSeed: string, terrain: ReturnType<typeof defaultTerrainSettings>, rules: ReturnType<typeof defaultHoleRules>) => {
  let best: { seed: string; course: Course; trackOverlapCount: number } | undefined;
  for (let attempt = 0; attempt < 4; attempt += 1) {
    const seed = attempt === 0 ? proposedSeed : `${proposedSeed}:append-${attempt}`;
    const candidate = generateCourse(seed, terrain, rules.hazardPhaseCount);
    if (state.hole === 1) return { seed, course: candidate };
    const expansion = expandCourseAtCup(state.course, candidate);
    if (!best || expansion.trackOverlapCount < best.trackOverlapCount) best = { seed, course: candidate, trackOverlapCount: expansion.trackOverlapCount };
    if (expansion.trackOverlapCount === 0) return { seed, course: candidate };
  }
  recordInstrumentation(state, { type: 'generation-failure', hole: state.hole, detail: `append overlap remained after 4 seeded candidates (${best!.trackOverlapCount} tiles)` });
  return { seed: best!.seed, course: best!.course };
};

const automaticChaosFor = (random: Random, ruleset: ReturnType<typeof rulesetFor>) => {
  const count = 1 + random.int(0, ruleset.id === 'party' ? 1 : 2);
  const available = [...CHAOS_MODIFIERS];
  const chaos: ChaosModifier[] = [];
  for (let index = 0; index < count; index += 1) chaos.push(available.splice(random.int(0, available.length - 1), 1)[0]!);
  return chaos;
};

/**
 * The course shuffler is intentionally not a player action. It makes the
 * campaign a seeded arcade gauntlet: every hole is random, reproducible, and
 * already known to the stitcher before the first putt.
 */
const automaticPlanFor = (state: GameState): PlannedHole => {
  const random = new Random(`${state.config.seed}:course-shuffle:${state.hole}`);
  const ruleset = rulesetFor(state.config);
  let terrain: ReturnType<typeof defaultTerrainSettings>;
  let holeRules: ReturnType<typeof defaultHoleRules>;
  let biome: string;
  let layout: string;
  let rules: string;
  if (ruleset.id === 'party') {
    const biomeIndex = random.int(0, Math.max(0, (ruleset.biomes?.length ?? 1) - 1));
    const layoutIndex = random.int(0, Math.max(0, (ruleset.layouts?.length ?? 1) - 1));
    const rulesIndex = random.int(0, 2);
    terrain = partyTerrain(state.config, state.hole, biomeIndex, layoutIndex);
    holeRules = partyHoleRules(state.config, rulesIndex);
    biome = terrain.theme;
    layout = `${terrain.archetype} · ${terrain.sizeProfile}`;
    rules = `${holeRules.timerSeconds}s · cap ${holeRules.strokeCap}`;
  } else {
    const source = random.pick(generateCoursePackages(`${state.config.seed}:course-shuffle:${state.hole}`, state.hole, courseDimensionsFor(state.config)));
    terrain = { ...source.recipe.terrain };
    holeRules = cloneRules(source.recipe.rules);
    biome = terrain.theme;
    layout = `${terrain.archetype} · ${terrain.sizeProfile}`;
    rules = `${holeRules.timerSeconds}s · cap ${holeRules.strokeCap}`;
  }
  const chaos = automaticChaosFor(random, ruleset);
  chaos.forEach((modifier) => applyChaos(terrain, holeRules, modifier));
  const ingredients = [biome, layout.replace(/ /g, '-'), rules.replace(/ /g, '-'), ...chaos].join(':');
  const selectedCourse = appendCourseFor(state, `${state.config.seed}:course:${state.hole}:${ingredients}`, terrain, holeRules);
  const planned: PlannedHole = {
    id: `hole-${state.hole}-shuffle-${ingredients}`,
    label: [biome, layout, ...chaos].join(' · '),
    courseSeed: selectedCourse.seed,
    recipe: {
      terrain,
      rules: holeRules,
      metadata: {
        schemaVersion: RECIPE_SCHEMA_VERSION,
        generatorVersion: GENERATOR_VERSION,
        seed: selectedCourse.seed,
        resolvedReels: { biome, layout, rules, chaos },
        contentIds: ruleset.id === 'party' ? [...PARTY_TRICK_CARDS] : [],
      },
    },
  };
  const validation = validateCourse(selectedCourse.course, holeRules.hazardPhaseCount);
  if (!validation.valid) recordInstrumentation(state, { type: 'generation-failure', hole: state.hole, detail: validation.failures.join('; ') });
  planned.recipe.metadata!.routeRoles = selectedCourse.course.routeRoles?.map((assignment) => ({ ...assignment, marker: { ...assignment.marker }, points: assignment.points.map((point) => ({ ...point })) }));
  planned.recipe.metadata!.courseHash = courseHashFor(selectedCourse.course);
  return planned;
};

const campaignPlan = (config: GameConfig): PlannedHole[] => {
  const plans: PlannedHole[] = [];
  let atlas: Course | undefined;
  for (let index = 0; index < config.holeCount; index += 1) {
    const hole = index + 1;
    const state = { config, hole, course: atlas } as GameState;
    const plan = automaticPlanFor(state);
    plans.push(plan);
    const course = courseForPlan(plan);
    atlas = atlas ? expandCourseAtCup(atlas, course).course : course;
  }
  return plans;
};

export const createGameState = (config: GameConfig): GameState => {
  const resolvedConfig = { ...config, ruleset: config.ruleset ?? 'party' };
  const coursePlan = campaignPlan(resolvedConfig);
  const firstPlan = coursePlan[0]!;
  const course = cloneCourse(courseForPlan(firstPlan));
  const holeRules = { ...firstPlan.recipe.rules, sharedBoons: [...firstPlan.recipe.rules.sharedBoons] };
  const players = Array.from({ length: config.humanCount }, (_, index) => emptyPlayer(`human-${index}`, index, 'human', 0, course));
  players.push(...Array.from({ length: config.botCount }, (_, index) => emptyPlayer(`bot-${index}`, players.length + index, 'bot', config.botSkill, course)));
  players.forEach((player) => resetPlayerForCourse(player, course, holeRules));
  const state: GameState = {
    config: resolvedConfig,
    course,
    holeRules,
    hole: 1,
    holeFinishSequence: 0,
    hazardElapsedMs: 0,
    coursePhase: 0,
    coursePlan,
    connections: [],
    emotes: [],
    emoteSequence: 0,
    cardSequence: 0,
    players,
    gadgets: [],
    turn: { playerIndex: 0, secondsLeft: holeRules.timerSeconds, shotInFlight: false, cardPlayed: false },
    paused: false,
    status: 'playing',
    messages: [`the course shuffler locked ${coursePlan.length} random hazards — tee off`],
    instrumentation: { events: [{ type: 'recipe', hole: 1, detail: firstPlan.recipe.metadata?.courseHash ?? firstPlan.courseSeed }, { type: 'reveal', hole: 1, detail: firstPlan.label }] },
  };
  return state;
};

export const cloneGameState = (state: GameState): GameState => ({
  ...state,
  course: cloneCourse(state.course),
  holeRules: { ...state.holeRules, sharedBoons: [...state.holeRules.sharedBoons] },
  coursePlan: (state.coursePlan ?? []).map(clonePlan),
  transition: state.transition ? { next: clonePlan(state.transition.next) } : undefined,
  connections: state.connections?.map((connection) => ({ ...connection, anchor: { ...connection.anchor }, offset: { ...connection.offset } })),
  emotes: state.emotes.map((emote) => ({ ...emote })),
  players: state.players.map((player) => ({ ...player, ball: { ...player.ball }, upgrades: [...player.upgrades], caddies: player.caddies.map((caddy) => ({ ...caddy })), pockets: player.pockets.map((pocket) => ({ ...pocket, duration: pocket.duration ? { ...pocket.duration } : undefined })), attachments: (player.attachments ?? []).map((attachment) => ({ ...attachment })), shotHistory: player.shotHistory.map((entry) => ({ ...entry, before: { ...entry.before }, after: { ...entry.after } })) })),
  gadgets: (state.gadgets ?? []).map((gadget) => ({ ...gadget, point: { ...gadget.point } })),
  shop: state.shop ? { ...state.shop, shelf: state.shop.shelf.map((offer) => ({ ...offer })), buyerOrder: [...state.shop.buyerOrder], completedBuyerIds: [...state.shop.completedBuyerIds], rerollVotes: { ...state.shop.rerollVotes } } : undefined,
  turn: { ...state.turn },
  messages: [...state.messages],
  instrumentation: state.instrumentation ? { ...state.instrumentation, events: state.instrumentation.events.map((event) => ({ ...event })) } : undefined,
});

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
  const next = state.coursePlan[nextHole - 1];
  if (!next) return;
  state.transition = { next: clonePlan(next) };
  state.status = 'transitioning';
  recordInstrumentation(state, { type: 'recipe', hole: nextHole, detail: next.recipe.metadata?.courseHash ?? next.courseSeed });
  recordInstrumentation(state, { type: 'reveal', hole: nextHole, detail: next.label });
  addMessage(state, `hole ${state.hole - 1} complete — the shuffler throws ${next.label}`);
};
