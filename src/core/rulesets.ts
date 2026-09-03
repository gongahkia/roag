import type { CourseArchetype, CourseTheme, GameConfig, PowerUp, RulesetId } from './types';

export const PARTY_RULESET_VERSION = 'party-rules-v1';
export const RECIPE_SCHEMA_VERSION = 1;
/** Recipes made before the wider fairway carving shipped with this version. */
export const LEGACY_GENERATOR_VERSION = 'party-slice-v1';
/** Recipes made with wider fairways but before directional routes and terrain noise. */
export const PREVIOUS_GENERATOR_VERSION = 'party-slice-v2';
/** Bump this whenever a generator change can alter a materialized course. */
export const GENERATOR_VERSION = 'party-slice-v3';
export type GeneratorVersion = typeof LEGACY_GENERATOR_VERSION | typeof PREVIOUS_GENERATOR_VERSION | typeof GENERATOR_VERSION;

/**
 * Missing metadata predates versioned recipes, so preserve the oldest known
 * builder. Unknown future versions use the current builder until explicitly
 * added to this compatibility table.
 */
export const generatorVersionFor = (version: string | undefined): GeneratorVersion => version === undefined || version === LEGACY_GENERATOR_VERSION
  ? LEGACY_GENERATOR_VERSION
  : version === PREVIOUS_GENERATOR_VERSION
    ? PREVIOUS_GENERATOR_VERSION
    : GENERATOR_VERSION;

export const PARTY_BIOMES = ['speedway', 'quarry', 'carnival'] as const satisfies readonly CourseTheme[];
export const PARTY_LAYOUTS = ['ribbon', 'fork', 'courtyard'] as const satisfies readonly CourseArchetype[];
export const PARTY_TRICK_CARDS = ['turbo', 'shield', 'heavy', 'airhorn', 'freeze', 'popper pad', 'rescue drone', 'glider'] as const satisfies readonly PowerUp[];

export type PartyTrickCard = typeof PARTY_TRICK_CARDS[number];

export interface RulesetConfig {
  id: RulesetId;
  label: string;
  cards: {
    handLimit: number;
    perShotLimit: number;
    allowed: readonly PowerUp[];
    durations: readonly ('shot' | 'hole')[];
  };
  shop: {
    offerCount: number;
    seconds: number;
    allowsRerollVote: boolean;
  };
  biomes?: readonly CourseTheme[];
  layouts?: readonly CourseArchetype[];
}

/** The public game: a group builds one constrained physical hole, then plays
 * it immediately. There is intentionally no card or merchant layer. */
export const COURSEWRIGHT_RULES: RulesetConfig = {
  id: 'coursewright',
  label: 'Coursewright Rules',
  cards: { handLimit: 0, perShotLimit: 0, allowed: [], durations: [] },
  shop: { offerCount: 0, seconds: 0, allowsRerollVote: false },
  biomes: PARTY_BIOMES,
  layouts: ['fork'],
};

export const PARTY_RULES: RulesetConfig = {
  id: 'party',
  label: 'Party Rules',
  cards: {
    handLimit: 2,
    perShotLimit: 1,
    allowed: PARTY_TRICK_CARDS,
    durations: ['shot', 'hole'],
  },
  shop: { offerCount: 3, seconds: 15, allowsRerollVote: false },
  biomes: PARTY_BIOMES,
  layouts: PARTY_LAYOUTS,
};

const CUSTOM_RULES: RulesetConfig = {
  id: 'custom',
  label: 'Custom Rules',
  cards: {
    handLimit: Number.POSITIVE_INFINITY,
    perShotLimit: 1,
    allowed: [],
    durations: ['shot', 'hole'],
  },
  shop: { offerCount: 7, seconds: 20, allowsRerollVote: true },
};

export const rulesetFor = (config: Pick<GameConfig, 'ruleset'>): RulesetConfig => config.ruleset === 'custom'
  ? CUSTOM_RULES
  : config.ruleset === 'party'
    ? PARTY_RULES
    : COURSEWRIGHT_RULES;
export const isPartyRules = (config: Pick<GameConfig, 'ruleset'>) => rulesetFor(config).id === 'party';
export const isCoursewrightRules = (config: Pick<GameConfig, 'ruleset'>) => rulesetFor(config).id === 'coursewright';
export const isPartyTrickCard = (value: PowerUp): value is PartyTrickCard => PARTY_TRICK_CARDS.includes(value as PartyTrickCard);

export const trickCardDetails: Record<PartyTrickCard, { target: string; duration: 'shot' | 'hole'; expiry: string }> = {
  turbo: { target: 'your ball', duration: 'shot', expiry: 'after your next shot resolves' },
  shield: { target: 'your ball', duration: 'hole', expiry: 'after it blocks one hazard or the hole ends' },
  heavy: { target: 'your ball', duration: 'shot', expiry: 'after your next shot resolves' },
  airhorn: { target: 'one opponent', duration: 'shot', expiry: 'after their next shot resolves' },
  freeze: { target: 'one moving obstacle', duration: 'shot', expiry: 'after your next shot resolves' },
  'popper pad': { target: 'one open tile', duration: 'hole', expiry: 'when triggered or when the hole ends' },
  'rescue drone': { target: 'your ball', duration: 'shot', expiry: 'immediately after it moves you to the safe route' },
  glider: { target: 'your ball', duration: 'shot', expiry: 'after your next chip or putt resolves' },
};
