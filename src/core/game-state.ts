import { defaultTerrainSettings } from './generator';
import { newBall } from './physics';
import { hashSeed } from './random';
import type { BuildState, Course, GameConfig, GameState, Player } from './types';

const colors = ['#f6c26b', '#8bd5ca', '#f38ba8', '#cba6f7', '#a6e3a1', '#89b4fa', '#fab387', '#f9e2af', '#94e2d5', '#eba0ac', '#b4befe', '#f5c2e7'];

export const defaultConfig = (): GameConfig => ({
  seed: `enemy-${Math.random().toString(36).slice(2, 8)}`,
  timerSeconds: 24,
  strokeCap: 10,
  collisions: true,
  powerUps: true,
  botCount: 3,
  humanCount: 1,
  botSkill: 5,
});

export const addMessage = (state: GameState, message: string) => {
  state.messages = [message, ...state.messages].slice(0, 5);
};

export const activePlayer = (state: GameState) => state.players[state.turn.playerIndex]!;

export const blankCourse = (seed: string): Course => {
  const tee = { x: 2, y: 7 };
  const cup = { x: 17, y: 7 };
  return {
    id: `build-${seed}`,
    seed,
    width: 20,
    height: 14,
    tiles: Array.from({ length: 280 }, () => ({ surface: 'void', height: 0 })),
    tee,
    cup,
    route: [],
    hazards: [],
    portals: [],
    itemPads: [],
    score: { playable: false, estimatedStrokes: 0, hazards: 0, elevation: 0, routes: 0, novelty: 0, total: 0, solverShots: [], rejection: 'builder has not validated this course' },
  };
};

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

export const createBuildState = (authorIndex: number): BuildState => ({
  authorIndex,
  tool: 'fairway',
  height: 0,
  direction: { x: 1, y: 0 },
  portalPairId: 1,
  terrain: defaultTerrainSettings(),
  generated: false,
});

export const createGameState = (config: GameConfig): GameState => {
  const course = blankCourse(hashSeed(config.seed, 0));
  const players = Array.from({ length: config.humanCount }, (_, index) => emptyPlayer(`human-${index}`, index, 'human', 0, course));
  players.push(...Array.from({ length: config.botCount }, (_, index) => emptyPlayer(`bot-${index}`, players.length + index, 'bot', config.botSkill, course)));
  return {
    config,
    course,
    hole: 1,
    coursePhase: 0,
    emotes: [],
    emoteSequence: 0,
    players,
    turn: { playerIndex: 0, secondsLeft: config.timerSeconds, shotInFlight: false },
    authoredCourses: [],
    courseIndex: 0,
    build: createBuildState(0),
    status: 'build',
    messages: ['build a course, then sink it once to validate'],
  };
};

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

export const cloneGameState = (state: GameState): GameState => ({
  ...state,
  course: cloneCourse(state.course),
  authoredCourses: state.authoredCourses.map((entry) => ({ ...entry, course: cloneCourse(entry.course) })),
  build: state.build ? { ...state.build, direction: { ...state.build.direction }, terrain: { ...state.build.terrain } } : undefined,
  emotes: state.emotes.map((emote) => ({ ...emote })),
  players: state.players.map((player) => ({ ...player, ball: { ...player.ball }, upgrades: [...player.upgrades] })),
  turn: { ...state.turn },
  messages: [...state.messages],
});

export const beginBuild = (state: GameState, authorIndex: number) => {
  const author = state.players[authorIndex]!;
  state.course = blankCourse(hashSeed(state.config.seed, state.authoredCourses.length + authorIndex + 1));
  state.coursePhase = 0;
  state.build = createBuildState(authorIndex);
  state.turn = { playerIndex: authorIndex, secondsLeft: state.config.timerSeconds, shotInFlight: false };
  state.status = 'build';
  addMessage(state, `${author.name} is building a course`);
};
