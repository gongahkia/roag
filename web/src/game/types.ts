export const GAME_STATE_KIND = "kenjaku-browser-game-state-v1";
export const GAME_SAVE_KIND = "kenjaku-browser-game-save-v1";
export const GAME_POLICY_KIND = "kenjaku-browser-policy-v1";

export type PlayerCount = 3 | 4;
export type Tile = string;
export type Wind = "E" | "S" | "W" | "N";
export type GamePhase = "draw" | "discard" | "reaction" | "terminal";
export type MeldKind = "chi" | "pon" | "minkan" | "ankan" | "kakan";
export type EventKind =
  | "game_started"
  | "draw"
  | "discard"
  | "riichi"
  | "call"
  | "pass"
  | "win"
  | "draw_game"
  | "system";

export interface GameConfig {
  readonly players: PlayerCount;
  readonly humanSeat: number;
  readonly seed: string;
  readonly names?: readonly string[];
}

export interface GameMeld {
  readonly kind: MeldKind;
  readonly tiles: readonly Tile[];
  readonly fromSeat: number | null;
}

export interface PendingDiscard {
  readonly tile: Tile;
  readonly fromSeat: number;
  readonly reactionSeats: readonly number[];
}

export interface ScoreLine {
  readonly name: string;
  readonly han: number;
}

export interface ScoreSummary {
  readonly winner: number;
  readonly fromSeat: number | null;
  readonly winKind: "tsumo" | "ron";
  readonly yaku: readonly ScoreLine[];
  readonly han: number;
  readonly fu: number;
  readonly basePoints: number;
  readonly payments: readonly number[];
  readonly total: number;
}

export interface TerminalResult {
  readonly reason: "tsumo" | "ron" | "exhaustive_draw";
  readonly score: ScoreSummary | null;
}

export type GameAction =
  | { readonly kind: "discard"; readonly tile: Tile }
  | { readonly kind: "riichi" }
  | { readonly kind: "tsumo" }
  | { readonly kind: "ron" }
  | { readonly kind: "pass" }
  | { readonly kind: "chi"; readonly consumed: readonly Tile[] }
  | { readonly kind: "pon"; readonly consumed: readonly Tile[] }
  | { readonly kind: "minkan"; readonly consumed: readonly Tile[] }
  | { readonly kind: "ankan"; readonly tile: Tile }
  | { readonly kind: "kakan"; readonly tile: Tile }
  | { readonly kind: "kita"; readonly tile: Tile };

export interface GameEvent {
  readonly id: string;
  readonly version: number;
  readonly kind: EventKind;
  readonly seat: number | null;
  readonly action: GameAction | null;
  readonly message: string;
}

export interface GameState {
  readonly kind: typeof GAME_STATE_KIND;
  readonly version: number;
  readonly id: string;
  readonly seed: string;
  readonly players: PlayerCount;
  readonly humanSeat: number;
  readonly names: readonly string[];
  readonly phase: GamePhase;
  readonly roundWind: Wind;
  readonly handNumber: number;
  readonly dealerSeat: number;
  readonly currentSeat: number;
  readonly turn: number;
  readonly wall: readonly Tile[];
  readonly deadWall: readonly Tile[];
  readonly doraIndicators: readonly Tile[];
  readonly hands: readonly (readonly Tile[])[];
  readonly discards: readonly (readonly Tile[])[];
  readonly melds: readonly (readonly GameMeld[])[];
  readonly kitaTiles: readonly (readonly Tile[])[];
  readonly points: readonly number[];
  readonly riichiSeats: readonly boolean[];
  readonly riichiSticks: number;
  readonly drawnTile: Tile | null;
  readonly pendingDiscard: PendingDiscard | null;
  readonly terminal: TerminalResult | null;
  readonly history: readonly GameEvent[];
}

export interface GameSave {
  readonly kind: typeof GAME_SAVE_KIND;
  readonly savedAt: number;
  readonly state: GameState;
}

export interface PolicyArtifact {
  readonly kind: typeof GAME_POLICY_KIND;
  readonly version: string;
  readonly name: string;
  readonly weights: Readonly<{
    readonly terminalDiscard: number;
    readonly honorDiscard: number;
    readonly isolatedDiscard: number;
    readonly duplicateKeep: number;
    readonly sequenceKeep: number;
    readonly riichi: number;
    readonly chi: number;
    readonly pon: number;
    readonly kan: number;
    readonly pass: number;
  }>;
}

export interface PolicyChoice {
  readonly action: GameAction;
  readonly score: number;
  readonly probability: number;
  readonly rationale: readonly string[];
}

export interface PolicyDecision {
  readonly policy: Pick<PolicyArtifact, "name" | "version">;
  readonly seat: number;
  readonly choices: readonly PolicyChoice[];
  readonly selected: PolicyChoice;
}
