import { defaultHoleRules, generateVotingOptions } from './generator';
import { resetPlayerForCourse } from './player-effects';
import { newBall } from './physics';
import { Random } from './random';
import type { Course, GameConfig, GameState, Player, VoteState, VotingOption } from './types';

const colors = ['#f6c26b', '#8bd5ca', '#f38ba8', '#cba6f7', '#a6e3a1', '#89b4fa', '#fab387', '#f9e2af', '#94e2d5', '#eba0ac', '#b4befe', '#f5c2e7'];

export const defaultConfig = (): GameConfig => ({
  seed: `enemy-${Math.random().toString(36).slice(2, 8)}`,
  holeCount: 9,
  botCount: 3,
  humanCount: 1,
  botSkill: 5,
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
  total: 0,
});

export const cloneCourse = (course: Course): Course => ({
  ...course,
  tiles: course.tiles.map((tile) => ({ ...tile, corners: tile.corners ? [...tile.corners] as [number, number, number, number] : undefined, direction: tile.direction ? { ...tile.direction } : undefined })),
  route: course.route.map((point) => ({ ...point })),
  tee: { ...course.tee },
  cup: { ...course.cup },
  hazards: course.hazards.map((hazard) => ({ ...hazard, point: { ...hazard.point } })),
  portals: course.portals?.map((pair) => ({ ...pair, entrance: pair.entrance ? { point: { ...pair.entrance.point }, direction: { ...pair.entrance.direction } } : undefined, exit: pair.exit ? { point: { ...pair.exit.point }, direction: { ...pair.exit.direction } } : undefined })),
  itemPads: course.itemPads.map((pad) => ({ ...pad, point: { ...pad.point } })),
});

const cloneOption = (option: VotingOption): VotingOption => ({
  ...option,
  course: cloneCourse(option.course),
  recipe: { terrain: { ...option.recipe.terrain }, rules: { ...option.recipe.rules, sharedBoons: [...option.recipe.rules.sharedBoons] } },
});

const newVote = (config: GameConfig, hole: number): VoteState => ({ options: generateVotingOptions(config.seed, hole), ballots: {} });

export const createGameState = (config: GameConfig): GameState => {
  const vote = newVote(config, 1);
  const course = cloneCourse(vote.options[0]!.course);
  const players = Array.from({ length: config.humanCount }, (_, index) => emptyPlayer(`human-${index}`, index, 'human', 0, course));
  players.push(...Array.from({ length: config.botCount }, (_, index) => emptyPlayer(`bot-${index}`, players.length + index, 'bot', config.botSkill, course)));
  return {
    config,
    course,
    holeRules: defaultHoleRules(),
    hole: 1,
    coursePhase: 0,
    vote,
    emotes: [],
    emoteSequence: 0,
    players,
    turn: { playerIndex: 0, secondsLeft: defaultHoleRules().timerSeconds, shotInFlight: false },
    paused: false,
    status: 'voting',
    messages: ['vote for the first course and house rules'],
  };
};

export const cloneGameState = (state: GameState): GameState => ({
  ...state,
  course: cloneCourse(state.course),
  holeRules: { ...state.holeRules, sharedBoons: [...state.holeRules.sharedBoons] },
  vote: state.vote ? { options: state.vote.options.map(cloneOption), ballots: { ...state.vote.ballots } } : undefined,
  assembly: state.assembly ? { ...state.assembly } : undefined,
  emotes: state.emotes.map((emote) => ({ ...emote })),
  players: state.players.map((player) => ({ ...player, ball: { ...player.ball }, upgrades: [...player.upgrades] })),
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
  state.course = cloneCourse(winner.course);
  state.holeRules = { ...winner.recipe.rules, sharedBoons: [...winner.recipe.rules.sharedBoons] };
  state.coursePhase = 0;
  state.players.forEach((player) => resetPlayerForCourse(player, state.course, state.holeRules));
  state.turn = { playerIndex: 0, secondsLeft: state.holeRules.timerSeconds, shotInFlight: false };
  state.vote = undefined;
  state.assembly = { optionId: winner.id, label: winner.label, theme: winner.recipe.terrain.theme, votes: counts.get(winner.id) ?? 0, totalBallots: state.players.length };
  state.status = 'assembling';
  addMessage(state, `${winner.label} wins the vote — assembling course`);
};

export const castVote = (state: GameState, playerId: string, optionId: string) => {
  if (state.status !== 'voting' || !state.vote) return;
  const player = state.players.find((candidate) => candidate.id === playerId);
  if (!player || !state.vote.options.some((option) => option.id === optionId)) return;
  state.vote.ballots[playerId] = optionId;
  addMessage(state, `${player.name} votes ${state.vote.options.find((option) => option.id === optionId)!.label}`);
  if (state.players.every((candidate) => state.vote!.ballots[candidate.id])) resolveVote(state);
};

export const completeAssembly = (state: GameState) => {
  if (state.status !== 'assembling') return;
  state.assembly = undefined;
  state.status = 'playing';
  addMessage(state, 'course assembled — tee off');
};

export const setPaused = (state: GameState, paused: boolean) => {
  if (state.status === 'finished') return;
  state.paused = paused;
  addMessage(state, paused ? 'match paused' : 'match resumed');
};

export const beginNextVote = (state: GameState) => {
  state.hole += 1;
  state.vote = newVote(state.config, state.hole);
  state.assembly = undefined;
  state.course = cloneCourse(state.vote.options[0]!.course);
  state.coursePhase = 0;
  state.paused = false;
  state.status = 'voting';
  state.turn = { playerIndex: 0, secondsLeft: defaultHoleRules().timerSeconds, shotInFlight: false };
  addMessage(state, `hole ${state.hole}: vote for the next course package`);
};
