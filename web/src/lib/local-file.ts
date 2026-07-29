import { parseMjsonTrajectory, type MjsonParseResult } from "./mjson";

export const MAX_LOCAL_TRAJECTORY_BYTES = 32 * 1024 * 1024;

export interface LocalTextFile {
  readonly name: string;
  readonly size: number;
  readonly type: string;
  text(): Promise<string>;
}

const ACCEPTED_EXTENSIONS = [".mjson", ".jsonl"] as const;
const ACCEPTED_MIME_TYPES = new Set([
  "application/json",
  "application/ndjson",
  "application/x-ndjson",
  "text/plain"
]);

export function validateLocalMjsonFile(file: unknown): readonly string[] {
  if (!isLocalTextFile(file)) return ["choose a local file"];
  const errors: string[] = [];
  if (!isSafeName(file.name)) errors.push("file name must be a local .mjson or .jsonl name");
  if (!Number.isInteger(file.size) || file.size <= 0) errors.push("file must not be empty");
  if (file.size > MAX_LOCAL_TRAJECTORY_BYTES) {
    errors.push(`file exceeds the ${MAX_LOCAL_TRAJECTORY_BYTES / 1024 / 1024} MiB limit`);
  }
  if (file.type && !ACCEPTED_MIME_TYPES.has(file.type)) {
    errors.push("file type must be JSON, JSONL, or plain text");
  }
  return errors;
}

export async function readLocalMjsonFile(file: LocalTextFile): Promise<MjsonParseResult> {
  const errors = validateLocalMjsonFile(file);
  if (errors.length > 0) throw new Error(errors.join("; "));
  let source: unknown;
  try {
    source = await file.text();
  } catch {
    throw new Error("local file could not be read");
  }
  if (typeof source !== "string") throw new Error("local file could not be read");
  return parseMjsonTrajectory(source);
}

function isLocalTextFile(value: unknown): value is LocalTextFile {
  if (typeof value !== "object" || value === null || Array.isArray(value)) return false;
  const file = value as Partial<LocalTextFile>;
  return (
    typeof file.name === "string" &&
    typeof file.size === "number" &&
    typeof file.type === "string" && typeof file.text === "function"
  );
}

function isSafeName(name: string): boolean {
  if (!name || /[\\/\0]/.test(name)) return false;
  const normalized = name.toLowerCase();
  return ACCEPTED_EXTENSIONS.some((extension) => normalized.endsWith(extension));
}
