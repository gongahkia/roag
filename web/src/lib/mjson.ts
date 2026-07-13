export const MJSON_EVENT_TYPES = [
  "start_game",
  "start_kyoku",
  "tsumo",
  "dahai",
  "pon",
  "chi",
  "daiminkan",
  "ankan",
  "kakan",
  "kita",
  "reach",
  "reach_accepted",
  "hora",
  "ryukyoku",
  "request_action",
  "end_kyoku",
  "end_game"
] as const;

export type MjsonEventType = (typeof MJSON_EVENT_TYPES)[number];
export type MjsonEvent = Readonly<Record<string, unknown>> & { readonly type: MjsonEventType };
export type MjsonParseErrorCode = "invalid_json" | "invalid_event";

export interface MjsonParseError {
  readonly line: number;
  readonly code: MjsonParseErrorCode;
  readonly message: string;
}

export interface MjsonParseResult {
  readonly events: readonly MjsonEvent[];
  readonly errors: readonly MjsonParseError[];
}

const ACTOR_EVENTS = new Set<MjsonEventType>([
  "tsumo",
  "dahai",
  "pon",
  "chi",
  "daiminkan",
  "ankan",
  "kakan",
  "kita",
  "reach",
  "reach_accepted",
  "hora"
]);
const CALL_EVENTS = new Set<MjsonEventType>([
  "pon",
  "chi",
  "daiminkan",
  "ankan",
  "kakan",
  "kita"
]);
const TARGET_EVENTS = new Set<MjsonEventType>(["pon", "chi", "daiminkan", "hora"]);
const TILE_PATTERN = /^(?:[0-9][mps]r?|[ESWN?])$/;

export function parseMjsonTrajectory(source: string): MjsonParseResult {
  const events: MjsonEvent[] = [];
  const errors: MjsonParseError[] = [];
  for (const [index, line] of source.split(/\r?\n/).entries()) {
    if (!line.trim()) continue;
    const lineNumber = index + 1;
    let payload: unknown;
    try {
      payload = JSON.parse(line);
    } catch {
      errors.push({ line: lineNumber, code: "invalid_json", message: "invalid JSON object" });
      continue;
    }
    const validationErrors = validateMjsonEvent(payload);
    if (validationErrors.length > 0) {
      errors.push(
        ...validationErrors.map((message) => ({
          line: lineNumber,
          code: "invalid_event" as const,
          message
        }))
      );
      continue;
    }
    events.push(payload as MjsonEvent);
  }
  return { events, errors };
}

export function validateMjsonEvent(payload: unknown): readonly string[] {
  if (!isRecord(payload)) return ["event must be an object"];
  const type = payload.type;
  if (typeof type !== "string" || !isMjsonEventType(type)) {
    return ["event type is unsupported"];
  }
  const errors: string[] = [];
  if (ACTOR_EVENTS.has(type)) validateSeat(payload.actor, "actor", errors);
  if (TARGET_EVENTS.has(type) && "target" in payload) {
    validateSeat(payload.target, "target", errors);
  }
  if (type === "tsumo" || type === "dahai") {
    validateTile(payload.pai, "pai", errors);
  }
  if (type === "dahai" && typeof payload.tsumogiri !== "boolean") {
    errors.push("dahai tsumogiri must be a boolean");
  }
  if (CALL_EVENTS.has(type)) validateCall(payload, errors);
  if (type === "start_kyoku") validateStartKyoku(payload, errors);
  if (type === "request_action") validateRequestAction(payload, errors);
  return errors;
}

function validateCall(event: Record<string, unknown>, errors: string[]): void {
  if ("pai" in event) validateTile(event.pai, "call pai", errors);
  if ("consumed" in event) validateTiles(event.consumed, "consumed", errors);
}

function validateStartKyoku(event: Record<string, unknown>, errors: string[]): void {
  if ("scores" in event) validateIntegerArray(event.scores, "scores", errors);
  if ("tehais" in event && !isTileMatrix(event.tehais)) {
    errors.push("tehais must be an array of tile arrays");
  }
  if ("dora_marker" in event) validateTile(event.dora_marker, "dora_marker", errors);
}

function validateRequestAction(event: Record<string, unknown>, errors: string[]): void {
  if (!Array.isArray(event.possible_actions)) {
    errors.push("request_action possible_actions must be an array");
    return;
  }
  for (const action of event.possible_actions) {
    if (!isRecord(action) || typeof action.type !== "string" || !action.type) {
      errors.push("request_action possible_actions entries must be action objects");
      return;
    }
  }
}

function validateSeat(value: unknown, field: string, errors: string[]): void {
  if (!Number.isInteger(value) || (value as number) < 0) {
    errors.push(`${field} must be a seat index`);
  }
}

function validateTile(value: unknown, field: string, errors: string[]): void {
  if (typeof value !== "string" || !TILE_PATTERN.test(value)) {
    errors.push(`${field} must be a tile string`);
  }
}

function validateTiles(value: unknown, field: string, errors: string[]): void {
  if (!Array.isArray(value)) {
    errors.push(`${field} must be an array of tile strings`);
    return;
  }
  for (const tile of value) validateTile(tile, field, errors);
}

function validateIntegerArray(value: unknown, field: string, errors: string[]): void {
  if (!Array.isArray(value) || value.some((item) => !Number.isInteger(item))) {
    errors.push(`${field} must be an array of integers`);
  }
}

function isTileMatrix(value: unknown): boolean {
  return Array.isArray(value) && value.every((hand) => Array.isArray(hand) && hand.every(isTile));
}

function isTile(value: unknown): boolean {
  return typeof value === "string" && TILE_PATTERN.test(value);
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function isMjsonEventType(value: string): value is MjsonEventType {
  return (MJSON_EVENT_TYPES as readonly string[]).includes(value);
}
