import { deserializeGame, serializeGame } from "./engine";
import type { GameSave, GameState } from "./types";

const DATABASE_NAME = "kenjaku-browser-game";
const DATABASE_VERSION = 1;
const STORE_NAME = "games";
const FALLBACK_KEY = "kenjaku-browser-game:last";

interface StoredGame {
  readonly id: string;
  readonly savedAt: number;
  readonly save: GameSave;
}

export interface SavedGameSummary {
  readonly id: string;
  readonly savedAt: number;
  readonly seed: string;
  readonly version: number;
  readonly terminal: boolean;
}

export async function saveLocalGame(state: GameState): Promise<void> {
  const save = serializeGame(state);
  if (!hasIndexedDb()) {
    window.localStorage.setItem(FALLBACK_KEY, JSON.stringify(save));
    return;
  }
  const database = await openDatabase();
  await requestToPromise(database.transaction(STORE_NAME, "readwrite").objectStore(STORE_NAME).put({
    id: state.id,
    savedAt: save.savedAt,
    save
  } satisfies StoredGame));
  database.close();
}

export async function loadLocalGame(id: string): Promise<GameState | null> {
  if (!hasIndexedDb()) return readFallback();
  const database = await openDatabase();
  const stored = await requestToPromise<StoredGame | undefined>(database.transaction(STORE_NAME, "readonly").objectStore(STORE_NAME).get(id));
  database.close();
  return stored ? deserializeGame(stored.save) : null;
}

export async function loadLatestLocalGame(): Promise<GameState | null> {
  if (!hasIndexedDb()) return readFallback();
  const summaries = await listLocalGames();
  return summaries.length === 0 ? null : loadLocalGame(summaries[0].id);
}

export async function listLocalGames(): Promise<readonly SavedGameSummary[]> {
  if (!hasIndexedDb()) {
    const fallback = readFallback();
    return fallback === null ? [] : [summaryFor(fallback, Date.now())];
  }
  const database = await openDatabase();
  const stored = await requestToPromise<StoredGame[]>(database.transaction(STORE_NAME, "readonly").objectStore(STORE_NAME).getAll());
  database.close();
  return Object.freeze(stored
    .map((entry) => summaryFor(deserializeGame(entry.save), entry.savedAt))
    .sort((left, right) => right.savedAt - left.savedAt));
}

export async function deleteLocalGame(id: string): Promise<void> {
  if (!hasIndexedDb()) {
    window.localStorage.removeItem(FALLBACK_KEY);
    return;
  }
  const database = await openDatabase();
  await requestToPromise(database.transaction(STORE_NAME, "readwrite").objectStore(STORE_NAME).delete(id));
  database.close();
}

export function exportGameReplay(state: GameState): string {
  return `${JSON.stringify(serializeGame(state), null, 2)}\n`;
}

function summaryFor(state: GameState, savedAt: number): SavedGameSummary {
  return Object.freeze({
    id: state.id,
    savedAt,
    seed: state.seed,
    version: state.version,
    terminal: state.terminal !== null
  });
}

function readFallback(): GameState | null {
  try {
    const raw = window.localStorage.getItem(FALLBACK_KEY);
    return raw === null ? null : deserializeGame(JSON.parse(raw));
  } catch {
    return null;
  }
}

function hasIndexedDb(): boolean {
  return typeof indexedDB !== "undefined";
}

function openDatabase(): Promise<IDBDatabase> {
  return new Promise((resolve, reject) => {
    const request = indexedDB.open(DATABASE_NAME, DATABASE_VERSION);
    request.onerror = () => reject(request.error ?? new Error("could not open local game database"));
    request.onupgradeneeded = () => {
      if (!request.result.objectStoreNames.contains(STORE_NAME)) request.result.createObjectStore(STORE_NAME, { keyPath: "id" });
    };
    request.onsuccess = () => resolve(request.result);
  });
}

function requestToPromise<T = undefined>(request: IDBRequest<T>): Promise<T> {
  return new Promise((resolve, reject) => {
    request.onerror = () => reject(request.error ?? new Error("local game database request failed"));
    request.onsuccess = () => resolve(request.result);
  });
}
