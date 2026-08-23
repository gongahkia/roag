import { defaultHoleRules, defaultTerrainSettings, generateCourse, generateVotingOptions } from './generator';
import { expandCourseAtCup, type CourseExpansion } from './campaign';
import { elapsedMsForPhase } from './hazards';
import { resetPlayerForCourse } from './player-effects';
import { newBall } from './physics';
import { Random } from './random';
import { COURSE_HEIGHT, COURSE_WIDTH, type Course, type GameConfig, type GameState, type PlannedHole, type Player, type VoteState, type VotingOption } from './types';

const colors = ['#f6c26b', '#8bd5ca', '#f38ba8', '#cba6f7', '#a6e3a1', '#89b4fa', '#fab387', '#f9e2af', '#94e2d5', '#eba0ac', '#b4befe', '#f5c2e7'];

export const defaultConfig = (): GameConfig => ({
  seed: `enemy-${Math.random().toString(36).slice(2, 8)}`,
  holeCount: 9,
  botCount: 3,
  humanCount: 1,
  botSkill: 5,
  courseWidth: COURSE_WIDTH,
  courseHeight: COURSE_HEIGHT,
  skipVoting: false,
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
  state.config.skipVoting ??= false;
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
  state.vote?.options.forEach((option) => {
    normalizeCourse(option.course);
    normalizeTerrain(option.recipe.terrain);
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
  state.coursePlan.forEach((plan) => normalizeTerrain(plan.recipe.terrain));
  if (state.status !== 'voting' && state.coursePlan.length < state.hole) {
    state.coursePlan.push({ id: `legacy-hole-${state.hole}`, label: `legacy hole ${state.hole}`, courseSeed: state.course.seed, recipe: { terrain: defaultTerrainSettings(), rules: { ...state.holeRules, sharedBoons: [...state.holeRules.sharedBoons] } } });
  }
  while (state.status !== 'voting' && state.coursePlan.length < state.config.holeCount) {
    const option = generateVotingOptions(state.config.seed, state.coursePlan.length + 1, courseDimensionsFor(state.config))[0]!;
    state.coursePlan.push(planFromOption(option));
  }
  const legacy = state as unknown as { status: string; assembly?: unknown };
  if (legacy.status === 'assembling') {
    delete legacy.assembly;
    state.status = 'playing';
  }
  state.emotes ??= [];
  state.emoteSequence ??= 0;
  return state;
};

const cloneOption = (option: VotingOption): VotingOption => ({
  ...option,
  course: cloneCourse(option.course),
  recipe: { terrain: { ...option.recipe.terrain }, rules: { ...option.recipe.rules, sharedBoons: [...option.recipe.rules.sharedBoons] } },
});

const planFromOption = (option: VotingOption): PlannedHole => ({
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

const newVote = (config: GameConfig, hole: number): VoteState => ({ options: generateVotingOptions(config.seed, hole, courseDimensionsFor(config)), ballots: {} });

const quickStartPlan = (config: GameConfig): PlannedHole[] => Array.from({ length: config.holeCount }, (_, index) => {
  const hole = index + 1;
  const options = generateVotingOptions(config.seed, hole, courseDimensionsFor(config));
  const selected = options[new Random(`${config.seed}:quick-start:hole:${hole}`).int(0, options.length - 1)]!;
  return planFromOption(selected);
});

export const createGameState = (config: GameConfig): GameState => {
  const resolvedConfig = { ...config, skipVoting: config.skipVoting === true };
  const coursePlan = resolvedConfig.skipVoting ? quickStartPlan(resolvedConfig) : [];
  const vote = resolvedConfig.skipVoting ? undefined : newVote(resolvedConfig, 1);
  const firstPlan = coursePlan[0];
  const course = firstPlan ? cloneCourse(courseForPlan(firstPlan)) : cloneCourse(vote!.options[0]!.course);
  const holeRules = firstPlan ? { ...firstPlan.recipe.rules, sharedBoons: [...firstPlan.recipe.rules.sharedBoons] } : defaultHoleRules();
  const players = Array.from({ length: config.humanCount }, (_, index) => emptyPlayer(`human-${index}`, index, 'human', 0, course));
  players.push(...Array.from({ length: config.botCount }, (_, index) => emptyPlayer(`bot-${index}`, players.length + index, 'bot', config.botSkill, course)));
  if (firstPlan) players.forEach((player) => resetPlayerForCourse(player, course, holeRules));
  const state: GameState = {
    config: resolvedConfig,
    course,
    holeRules,
    hole: 1,
    holeFinishSequence: 0,
    hazardElapsedMs: 0,
    coursePhase: 0,
    vote,
    coursePlan,
    emotes: [],
    emoteSequence: 0,
    cardSequence: 0,
    players,
    gadgets: [],
    turn: { playerIndex: 0, secondsLeft: holeRules.timerSeconds, shotInFlight: false, cardPlayed: false },
    paused: false,
    status: firstPlan ? 'playing' : 'voting',
    messages: firstPlan ? [`quick start locked ${coursePlan.length} random courses — tee off`] : ['vote for the first course and house rules'],
  };
  return state;
};

export const cloneGameState = (state: GameState): GameState => ({
  ...state,
  course: cloneCourse(state.course),
  holeRules: { ...state.holeRules, sharedBoons: [...state.holeRules.sharedBoons] },
  vote: state.vote ? { options: state.vote.options.map(cloneOption), ballots: { ...state.vote.ballots } } : undefined,
  coursePlan: (state.coursePlan ?? []).map(clonePlan),
  transition: state.transition ? { next: clonePlan(state.transition.next) } : undefined,
  emotes: state.emotes.map((emote) => ({ ...emote })),
  players: state.players.map((player) => ({ ...player, ball: { ...player.ball }, upgrades: [...player.upgrades], caddies: player.caddies.map((caddy) => ({ ...caddy })), pockets: player.pockets.map((pocket) => ({ ...pocket, duration: pocket.duration ? { ...pocket.duration } : undefined })), attachments: (player.attachments ?? []).map((attachment) => ({ ...attachment })), shotHistory: player.shotHistory.map((entry) => ({ ...entry, before: { ...entry.before }, after: { ...entry.after } })) })),
  gadgets: (state.gadgets ?? []).map((gadget) => ({ ...gadget, point: { ...gadget.point } })),
  shop: state.shop ? { ...state.shop, shelf: state.shop.shelf.map((offer) => ({ ...offer })), buyerOrder: [...state.shop.buyerOrder], completedBuyerIds: [...state.shop.completedBuyerIds], rerollVotes: { ...state.shop.rerollVotes } } : undefined,
  turn: { ...state.turn },
  messages: [...state.messages],
});

const resolveVote = (state: GameState) => {
  const vote = state.vote!;
  const counts = new Map(vote.options.map((option) => [option.id, 0]));
  Object.values(vote.ballots).forEach((optionId) => counts.set(optionId, (counts.get(optionId) ?? 0) + 1));
  const highest = Math.max(...counts.values());
  const tied = vote.options.filter((option) => counts.get(option.id) === highest);
  const winner = tied[new Random(`${state.config.seed}:hole:${state.hole}:tie`).int(0, tied.length - 1)]!;
  const planned = planFromOption(winner);
  state.coursePlan = [...(state.coursePlan ?? []), planned];
  state.vote = undefined;
  if (state.coursePlan.length < state.config.holeCount) {
    const nextHole = state.coursePlan.length + 1;
    state.vote = newVote(state.config, nextHole);
    state.course = cloneCourse(state.vote.options[0]!.course);
    addMessage(state, `${winner.label} locks hole ${state.coursePlan.length} — choose hole ${nextHole}`);
    return;
  }
  state.hole = 1;
  activatePlan(state, state.coursePlan[0]!);
  state.status = 'playing';
  addMessage(state, 'full course plan locked — tee off');
};

export const castVote = (state: GameState, playerId: string, optionId: string) => {
  if (state.status !== 'voting' || !state.vote) return;
  const player = state.players.find((candidate) => candidate.id === playerId);
  if (!player || !state.vote.options.some((option) => option.id === optionId)) return;
  state.vote.ballots[playerId] = optionId;
  addMessage(state, `${player.name} votes ${state.vote.options.find((option) => option.id === optionId)!.label}`);
  if (state.players.every((candidate) => state.vote!.ballots[candidate.id])) resolveVote(state);
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
  const next = state.coursePlan[nextHole - 1];
  if (!next) return;
  state.hole = nextHole;
  state.transition = { next: clonePlan(next) };
  state.gadgets = [];
  state.paused = false;
  state.status = 'transitioning';
  addMessage(state, `hole ${state.hole - 1} complete — rebuilding hole ${state.hole}`);
};
