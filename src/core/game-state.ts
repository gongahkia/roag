import { defaultHoleRules, defaultTerrainSettings, generateCourse, generateCoursePackages } from './generator';
import { expandCourseAtCup, type CourseExpansion } from './campaign';
import { elapsedMsForPhase } from './hazards';
import { resetPlayerForCourse } from './player-effects';
import { newBall } from './physics';
import { Random } from './random';
import { COURSE_HEIGHT, COURSE_WIDTH, type Course, type CoursePackage, type DieFace, type DieState, type DieWager, type GameConfig, type GameState, type PlannedHole, type Player } from './types';

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
  state.die?.faces.forEach((face) => {
    normalizeCourse(face.course);
    normalizeTerrain(face.recipe.terrain);
    face.weight ??= 1;
    face.augmentations ??= {};
  });
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

const clonePackage = (option: CoursePackage): CoursePackage => ({
  ...option,
  course: cloneCourse(option.course),
  recipe: { terrain: { ...option.recipe.terrain }, rules: { ...option.recipe.rules, sharedBoons: [...option.recipe.rules.sharedBoons] } },
});

const planFromPackage = (option: CoursePackage): PlannedHole => ({
  id: option.id,
  label: option.label,
  courseSeed: option.course.seed,
  recipe: { terrain: { ...option.recipe.terrain }, rules: { ...option.recipe.rules, sharedBoons: [...option.recipe.rules.sharedBoons] } },
});

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

const DIE_SECONDS = 18;
const DIE_ROLL_SECONDS = 1.15;

const dieFaceAt = (config: GameConfig, hole: number, index: number, addedBy?: string): DieFace => {
  const packages = generateCoursePackages(`${config.seed}:die:${hole}:batch:${Math.floor(index / 3)}`, hole, courseDimensionsFor(config));
  const source = packages[index % packages.length]!;
  return { ...clonePackage(source), id: `hole-${hole}-die-${index + 1}`, weight: 1, addedBy, augmentations: {} };
};

const wagersFor = (players: readonly Player[]) => Object.fromEntries(players.map((player) => [player.id, { addedSides: 0, augmentations: {}, ready: false } satisfies DieWager]));

const newDie = (config: GameConfig, hole: number, players: readonly Player[]): DieState => ({
  faces: Array.from({ length: 6 }, (_, index) => dieFaceAt(config, hole, index)),
  wagers: wagersFor(players),
  secondsLeft: DIE_SECONDS,
});

const quickStartPlan = (config: GameConfig): PlannedHole[] => Array.from({ length: config.holeCount }, (_, index) => {
  const hole = index + 1;
  const faces = newDie(config, hole, []).faces;
  const selected = faces[new Random(`${config.seed}:quick-die:hole:${hole}`).int(0, faces.length - 1)]!;
  return planFromPackage(selected);
});

export const createGameState = (config: GameConfig): GameState => {
  const resolvedConfig = { ...config, skipDieBets: config.skipDieBets === true };
  const coursePlan = resolvedConfig.skipDieBets ? quickStartPlan(resolvedConfig) : [];
  const firstPlan = coursePlan[0];
  const starterDie = firstPlan ? undefined : newDie(resolvedConfig, 1, []);
  const course = firstPlan ? cloneCourse(courseForPlan(firstPlan)) : cloneCourse(starterDie!.faces[0]!.course);
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
    faces: state.die.faces.map((face) => ({ ...clonePackage(face), weight: face.weight, addedBy: face.addedBy, augmentations: { ...face.augmentations } })),
    wagers: Object.fromEntries(Object.entries(state.die.wagers).map(([playerId, wager]) => [playerId, { ...wager, augmentations: { ...wager.augmentations } }])),
    roll: state.die.roll ? { ...state.die.roll } : undefined,
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

const wagerFor = (die: DieState, playerId: string) => die.wagers[playerId] ??= { addedSides: 0, augmentations: {}, ready: false };

const startDieRoll = (state: GameState) => {
  const die = state.die;
  if (!die || die.roll) return;
  const totalWeight = die.faces.reduce((total, face) => total + face.weight, 0);
  const random = new Random(`${state.config.seed}:die:${state.hole}:${die.faces.map((face) => `${face.id}:${face.weight}`).join('|')}`);
  let remaining = random.next() * totalWeight;
  let selected = die.faces[die.faces.length - 1]!;
  for (const face of die.faces) {
    remaining -= face.weight;
    if (remaining < 0) { selected = face; break; }
  }
  die.roll = { faceId: selected.id, secondsLeft: DIE_ROLL_SECONDS };
  addMessage(state, `the ${die.faces.length}-stop course slot machine starts to spin`);
};

const resolveDieRoll = (state: GameState) => {
  const die = state.die;
  const selected = die?.faces.find((face) => face.id === die.roll?.faceId);
  if (!die || !selected) return;
  const planned = planFromPackage(selected);
  state.coursePlan = [...(state.coursePlan ?? []), planned];
  state.die = undefined;
  if (state.hole === 1) {
    activatePlan(state, planned);
    state.status = 'playing';
    addMessage(state, `${selected.label} locks in — tee off`);
    return;
  }
  state.transition = { next: clonePlan(planned) };
  state.gadgets = [];
  state.paused = false;
  state.status = 'transitioning';
  addMessage(state, `${selected.label} locks in — rebuilding hole ${state.hole}`);
};

export const addDieSide = (state: GameState, playerId: string) => {
  if (state.status !== 'rolling' || !state.die || state.die.roll) return;
  const player = state.players.find((candidate) => candidate.id === playerId);
  if (!player) return;
  const wager = wagerFor(state.die, playerId);
  const cost = 1 + wager.addedSides;
  if (player.cash < cost) return;
  player.cash -= cost;
  wager.addedSides += 1;
  const face = dieFaceAt(state.config, state.hole, state.die.faces.length, playerId);
  state.die.faces.push(face);
  addMessage(state, `${player.name} adds a wild reel stop for $${cost}`);
};

export const augmentDieFace = (state: GameState, playerId: string, faceId: string) => {
  if (state.status !== 'rolling' || !state.die || state.die.roll) return;
  const player = state.players.find((candidate) => candidate.id === playerId);
  const face = state.die.faces.find((candidate) => candidate.id === faceId);
  if (!player || !face) return;
  const wager = wagerFor(state.die, playerId);
  const existing = wager.augmentations[faceId] ?? 0;
  const cost = 1 + Math.floor(existing / 2);
  if (player.cash < cost) return;
  player.cash -= cost;
  wager.augmentations[faceId] = existing + 1;
  face.augmentations[playerId] = (face.augmentations[playerId] ?? 0) + 1;
  face.weight += 1;
  addMessage(state, `${player.name} weights ${face.label} for $${cost}`);
};

export const readyDieRoll = (state: GameState, playerId: string) => {
  if (state.status !== 'rolling' || !state.die || state.die.roll || !state.players.some((player) => player.id === playerId)) return;
  const wager = wagerFor(state.die, playerId);
  if (wager.ready) return;
  wager.ready = true;
  const player = state.players.find((candidate) => candidate.id === playerId)!;
  addMessage(state, `${player.name} is ready to pull the lever`);
  if (state.players.every((candidate) => wagerFor(state.die!, candidate.id).ready)) startDieRoll(state);
};

export const tickDie = (state: GameState, elapsedSeconds: number) => {
  if (state.status !== 'rolling' || !state.die || state.paused) return false;
  const elapsed = Math.max(0, Number.isFinite(elapsedSeconds) ? elapsedSeconds : 0);
  if (state.die.roll) {
    state.die.roll.secondsLeft = Math.max(0, state.die.roll.secondsLeft - elapsed);
    if (state.die.roll.secondsLeft === 0) resolveDieRoll(state);
    return true;
  }
  state.die.secondsLeft = Math.max(0, state.die.secondsLeft - elapsed);
  if (state.die.secondsLeft === 0) {
    state.players.forEach((player) => { wagerFor(state.die!, player.id).ready = true; });
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
