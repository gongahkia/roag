export const CHECKPOINT_MANIFEST_V1_KIND = "kenjaku-checkpoint-manifest-v1";
export const CHECKPOINT_RULESETS = ["tenhou-4p", "tenhou-3p"] as const;

export type CheckpointRuleset = (typeof CHECKPOINT_RULESETS)[number];

export interface SemanticVersion {
  readonly major: string;
  readonly minor: string;
  readonly patch: string;
  readonly prerelease: readonly string[];
  readonly build: readonly string[];
}

export interface CheckpointCompatibilityV1 {
  readonly observation_kind: string;
  readonly action_kind: string;
  readonly legal_action_mask_kind: string;
  readonly legal_action_mask_dim: number;
  readonly decision_result_kind: string;
}

export interface CheckpointManifestV1 {
  readonly checkpoint_id: string;
  readonly model_kind: string;
  readonly model_version: SemanticVersion;
  readonly rulesets: readonly CheckpointRuleset[];
  readonly compatibility: CheckpointCompatibilityV1;
}

export interface CheckpointRequirementV1 {
  readonly ruleset: CheckpointRuleset;
  readonly minimum_model_version: SemanticVersion;
  readonly compatibility: CheckpointCompatibilityV1;
  readonly model_kind?: string;
}

export interface CheckpointRequirementInput {
  readonly ruleset: CheckpointRuleset;
  readonly minimum_model_version: string | SemanticVersion;
  readonly compatibility: CheckpointCompatibilityV1;
  readonly model_kind?: string;
}

export interface RegisteredCheckpoint<T> {
  readonly manifest: CheckpointManifestV1;
  readonly value: T;
}

export interface CheckpointRejection {
  readonly checkpoint_id: string;
  readonly errors: readonly string[];
}

export interface CheckpointResolution<T> {
  readonly checkpoint: RegisteredCheckpoint<T> | null;
  readonly rejections: readonly CheckpointRejection[];
}

export const CURRENT_CHECKPOINT_COMPATIBILITY_V1: CheckpointCompatibilityV1 = Object.freeze({
  observation_kind: "kenjaku-observation-v1",
  action_kind: "kenjaku-action-v1",
  legal_action_mask_kind: "kenjaku-legal-action-mask-v1",
  legal_action_mask_dim: 276,
  decision_result_kind: "kenjaku-decision-result-v1"
});

const MANIFEST_FIELDS = [
  "kind",
  "checkpoint_id",
  "model_kind",
  "model_version",
  "rulesets",
  "compatibility"
] as const;
const COMPATIBILITY_FIELDS = [
  "observation_kind",
  "action_kind",
  "legal_action_mask_kind",
  "legal_action_mask_dim",
  "decision_result_kind"
] as const;
const SEMVER = /^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$/;

export function parseCheckpointManifestV1(payload: unknown): CheckpointManifestV1 {
  const manifest = requireRecord(payload, "CheckpointManifestV1");
  requireExactFields(manifest, MANIFEST_FIELDS, "CheckpointManifestV1");
  if (readString(manifest, "kind") !== CHECKPOINT_MANIFEST_V1_KIND) {
    throw new Error(`CheckpointManifestV1 kind must be ${CHECKPOINT_MANIFEST_V1_KIND}`);
  }
  const rulesets = readRulesets(manifest.rulesets);
  return Object.freeze({
    checkpoint_id: readNonEmptyString(manifest, "checkpoint_id"),
    model_kind: readNonEmptyString(manifest, "model_kind"),
    model_version: parseSemanticVersion(readString(manifest, "model_version")),
    rulesets: Object.freeze(rulesets),
    compatibility: parseCheckpointCompatibilityV1(manifest.compatibility)
  });
}

export function parseCheckpointManifestV1Json(source: string): CheckpointManifestV1 {
  let payload: unknown;
  try {
    payload = JSON.parse(source);
  } catch {
    throw new Error("checkpoint manifest must be valid JSON");
  }
  return parseCheckpointManifestV1(payload);
}

export function parseSemanticVersion(value: unknown): SemanticVersion {
  if (typeof value !== "string") throw new Error("semantic version must be a string");
  const match = SEMVER.exec(value);
  if (!match) throw new Error("invalid semantic version");
  const prerelease = splitIdentifiers(match[4], true);
  const build = splitIdentifiers(match[5], false);
  return Object.freeze({
    major: match[1],
    minor: match[2],
    patch: match[3],
    prerelease: Object.freeze(prerelease),
    build: Object.freeze(build)
  });
}

export function semanticVersionToString(value: SemanticVersion): string {
  const version = normalizeSemanticVersion(value);
  let output = `${version.major}.${version.minor}.${version.patch}`;
  if (version.prerelease.length > 0) output += `-${version.prerelease.join(".")}`;
  if (version.build.length > 0) output += `+${version.build.join(".")}`;
  return output;
}

export function compareSemanticVersions(left: SemanticVersion, right: SemanticVersion): number {
  const normalizedLeft = normalizeSemanticVersion(left);
  const normalizedRight = normalizeSemanticVersion(right);
  for (const key of ["major", "minor", "patch"] as const) {
    const comparison = compareNumericIdentifier(normalizedLeft[key], normalizedRight[key]);
    if (comparison !== 0) return comparison;
  }
  if (normalizedLeft.prerelease.length === 0 && normalizedRight.prerelease.length === 0) return 0;
  if (normalizedLeft.prerelease.length === 0) return 1;
  if (normalizedRight.prerelease.length === 0) return -1;
  for (let index = 0; index < Math.min(normalizedLeft.prerelease.length, normalizedRight.prerelease.length); index += 1) {
    const comparison = comparePrereleaseIdentifier(
      normalizedLeft.prerelease[index],
      normalizedRight.prerelease[index]
    );
    if (comparison !== 0) return comparison;
  }
  return compareNumericIdentifier(
    String(normalizedLeft.prerelease.length),
    String(normalizedRight.prerelease.length)
  );
}

export function createCheckpointRequirementV1(
  input: CheckpointRequirementInput
): CheckpointRequirementV1 {
  const requirement = requireRecord(input, "CheckpointRequirementV1");
  const ruleset = requirement.ruleset;
  if (!isCheckpointRuleset(ruleset)) {
    throw new Error("unsupported checkpoint requirement ruleset");
  }
  const modelKind = requirement.model_kind;
  if (modelKind !== undefined && (typeof modelKind !== "string" || !modelKind)) {
    throw new Error("model_kind must be a non-empty string");
  }
  return Object.freeze({
    ruleset,
    minimum_model_version: normalizeSemanticVersion(requirement.minimum_model_version),
    compatibility: parseCheckpointCompatibilityV1(requirement.compatibility),
    ...(modelKind === undefined ? {} : { model_kind: modelKind })
  });
}

export function checkpointCompatibilityErrors(
  manifest: CheckpointManifestV1,
  requirement: CheckpointRequirementV1
): readonly string[] {
  const normalizedManifest = normalizeManifest(manifest);
  const normalizedRequirement = normalizeRequirement(requirement);
  const errors: string[] = [];
  if (!normalizedManifest.rulesets.includes(normalizedRequirement.ruleset)) {
    errors.push("ruleset is unsupported");
  }
  if (
    normalizedRequirement.model_kind !== undefined &&
    normalizedManifest.model_kind !== normalizedRequirement.model_kind
  ) {
    errors.push("model_kind differs");
  }
  if (normalizedManifest.model_version.major !== normalizedRequirement.minimum_model_version.major) {
    errors.push("model_version major differs");
  } else if (
    compareSemanticVersions(normalizedManifest.model_version, normalizedRequirement.minimum_model_version) < 0
  ) {
    errors.push("model_version is below minimum");
  }
  for (const field of COMPATIBILITY_FIELDS) {
    if (normalizedManifest.compatibility[field] !== normalizedRequirement.compatibility[field]) {
      errors.push(`compatibility.${field} differs`);
    }
  }
  return Object.freeze(errors);
}

export class BrowserCheckpointRegistry<T> {
  readonly #checkpoints = new Map<string, RegisteredCheckpoint<T>>();

  register(checkpoint: RegisteredCheckpoint<T>): RegisteredCheckpoint<T> {
    const normalized = normalizeRegisteredCheckpoint(checkpoint);
    if (this.#checkpoints.has(normalized.manifest.checkpoint_id)) {
      throw new Error(`checkpoint_id is already registered: ${normalized.manifest.checkpoint_id}`);
    }
    this.#checkpoints.set(normalized.manifest.checkpoint_id, normalized);
    return normalized;
  }

  list(): readonly RegisteredCheckpoint<T>[] {
    return Object.freeze([...this.#checkpoints.values()].sort(compareCheckpointIds));
  }

  resolve(requirement: CheckpointRequirementV1): CheckpointResolution<T> {
    const normalizedRequirement = normalizeRequirement(requirement);
    const compatible: RegisteredCheckpoint<T>[] = [];
    const rejections: CheckpointRejection[] = [];
    for (const checkpoint of this.#checkpoints.values()) {
      const errors = checkpointCompatibilityErrors(checkpoint.manifest, normalizedRequirement);
      if (errors.length === 0) compatible.push(checkpoint);
      else rejections.push(Object.freeze({ checkpoint_id: checkpoint.manifest.checkpoint_id, errors }));
    }
    compatible.sort(comparePreferredCheckpoint);
    rejections.sort((left, right) => compareText(left.checkpoint_id, right.checkpoint_id));
    return Object.freeze({
      checkpoint: compatible[0] ?? null,
      rejections: Object.freeze(rejections)
    });
  }
}

function parseCheckpointCompatibilityV1(payload: unknown): CheckpointCompatibilityV1 {
  const compatibility = requireRecord(payload, "CheckpointCompatibilityV1");
  requireExactFields(compatibility, COMPATIBILITY_FIELDS, "CheckpointCompatibilityV1");
  const legalActionMaskDim = compatibility.legal_action_mask_dim;
  if (
    typeof legalActionMaskDim !== "number" ||
    !Number.isInteger(legalActionMaskDim) ||
    legalActionMaskDim <= 0
  ) {
    throw new Error("legal_action_mask_dim must be a positive integer");
  }
  return Object.freeze({
    observation_kind: readNonEmptyString(compatibility, "observation_kind"),
    action_kind: readNonEmptyString(compatibility, "action_kind"),
    legal_action_mask_kind: readNonEmptyString(compatibility, "legal_action_mask_kind"),
    legal_action_mask_dim: legalActionMaskDim,
    decision_result_kind: readNonEmptyString(compatibility, "decision_result_kind")
  });
}

function normalizeManifest(value: CheckpointManifestV1): CheckpointManifestV1 {
  const manifest = requireRecord(value, "CheckpointManifestV1");
  return parseCheckpointManifestV1({
    kind: CHECKPOINT_MANIFEST_V1_KIND,
    checkpoint_id: manifest.checkpoint_id,
    model_kind: manifest.model_kind,
    model_version: semanticVersionToString(normalizeSemanticVersion(manifest.model_version)),
    rulesets: manifest.rulesets,
    compatibility: manifest.compatibility
  });
}

function normalizeRequirement(value: CheckpointRequirementV1): CheckpointRequirementV1 {
  const requirement = requireRecord(value, "CheckpointRequirementV1");
  const modelKind = requirement.model_kind;
  if (modelKind !== undefined && typeof modelKind !== "string") {
    throw new Error("model_kind must be a non-empty string");
  }
  return createCheckpointRequirementV1({
    ruleset: requirement.ruleset as CheckpointRuleset,
    minimum_model_version: normalizeSemanticVersion(requirement.minimum_model_version),
    compatibility: requirement.compatibility as CheckpointCompatibilityV1,
    ...(modelKind === undefined ? {} : { model_kind: modelKind })
  });
}

function normalizeRegisteredCheckpoint<T>(value: RegisteredCheckpoint<T>): RegisteredCheckpoint<T> {
  const checkpoint = requireRecord(value, "RegisteredCheckpoint");
  if (!("value" in checkpoint)) throw new Error("RegisteredCheckpoint value is required");
  return Object.freeze({
    manifest: normalizeManifest(checkpoint.manifest as CheckpointManifestV1),
    value: checkpoint.value as T
  });
}

function normalizeSemanticVersion(value: unknown): SemanticVersion {
  if (typeof value === "string") return parseSemanticVersion(value);
  const version = requireRecord(value, "semantic version");
  if (
    typeof version.major !== "string" ||
    typeof version.minor !== "string" ||
    typeof version.patch !== "string" ||
    !isStringArray(version.prerelease) ||
    !isStringArray(version.build)
  ) {
    throw new Error("invalid semantic version");
  }
  let source = `${version.major}.${version.minor}.${version.patch}`;
  if (version.prerelease.length > 0) source += `-${version.prerelease.join(".")}`;
  if (version.build.length > 0) source += `+${version.build.join(".")}`;
  return parseSemanticVersion(source);
}

function readRulesets(value: unknown): CheckpointRuleset[] {
  if (!Array.isArray(value)) throw new Error("rulesets must be an array");
  if (value.length === 0 || value.some((ruleset) => !isCheckpointRuleset(ruleset))) {
    throw new Error("rulesets must contain supported rulesets");
  }
  if (new Set(value).size !== value.length) throw new Error("rulesets must not contain duplicates");
  return [...value];
}

function requireExactFields(
  payload: Record<string, unknown>,
  fields: readonly string[],
  name: string
): void {
  const actual = Object.keys(payload).sort(compareText);
  const expected = [...fields].sort(compareText);
  const missing = expected.filter((field) => !actual.includes(field));
  const unexpected = actual.filter((field) => !expected.includes(field));
  if (missing.length === 0 && unexpected.length === 0) return;
  const details = [
    ...(missing.length > 0 ? [`missing=${missing.join(",")}`] : []),
    ...(unexpected.length > 0 ? [`unexpected=${unexpected.join(",")}`] : [])
  ];
  throw new Error(`${name} fields must match v1 schema: ${details.join("; ")}`);
}

function requireRecord(value: unknown, name: string): Record<string, unknown> {
  if (typeof value !== "object" || value === null || Array.isArray(value)) {
    throw new Error(`${name} must be an object`);
  }
  return value as Record<string, unknown>;
}

function readString(payload: Record<string, unknown>, field: string): string {
  if (typeof payload[field] !== "string") throw new Error(`${field} must be a string`);
  return payload[field];
}

function readNonEmptyString(payload: Record<string, unknown>, field: string): string {
  const value = readString(payload, field);
  if (!value) throw new Error(`${field} must be a non-empty string`);
  return value;
}

function splitIdentifiers(value: string | undefined, forbidNumericLeadingZero: boolean): string[] {
  if (value === undefined) return [];
  const identifiers = value.split(".");
  if (
    identifiers.some(
      (identifier) =>
        !/^[0-9A-Za-z-]+$/.test(identifier) ||
        (forbidNumericLeadingZero && /^0[0-9]+$/.test(identifier))
    )
  ) {
    throw new Error("invalid semantic version");
  }
  return identifiers;
}

function isCheckpointRuleset(value: unknown): value is CheckpointRuleset {
  return typeof value === "string" && (CHECKPOINT_RULESETS as readonly string[]).includes(value);
}

function isStringArray(value: unknown): value is readonly string[] {
  return Array.isArray(value) && value.every((item) => typeof item === "string");
}

function compareNumericIdentifier(left: string, right: string): number {
  if (left.length !== right.length) return left.length < right.length ? -1 : 1;
  return compareText(left, right);
}

function comparePrereleaseIdentifier(left: string, right: string): number {
  if (left === right) return 0;
  const leftNumeric = /^[0-9]+$/.test(left);
  const rightNumeric = /^[0-9]+$/.test(right);
  if (leftNumeric && rightNumeric) return compareNumericIdentifier(left, right);
  if (leftNumeric) return -1;
  if (rightNumeric) return 1;
  return compareText(left, right);
}

function compareCheckpointIds<T>(left: RegisteredCheckpoint<T>, right: RegisteredCheckpoint<T>): number {
  return compareText(left.manifest.checkpoint_id, right.manifest.checkpoint_id);
}

function comparePreferredCheckpoint<T>(left: RegisteredCheckpoint<T>, right: RegisteredCheckpoint<T>): number {
  const version = compareSemanticVersions(right.manifest.model_version, left.manifest.model_version);
  return version === 0
    ? compareText(left.manifest.checkpoint_id, right.manifest.checkpoint_id)
    : version;
}

function compareText(left: string, right: string): number {
  if (left === right) return 0;
  return left < right ? -1 : 1;
}
