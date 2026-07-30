import type { AutoplayCandidate } from './types'

export const AUTOPLAY_INFERENCE_ARTIFACT_VERSION = 1 as const
export interface ApprovedAutoplayPolicyArtifact { version: typeof AUTOPLAY_INFERENCE_ARTIFACT_VERSION; kind: 'jomon-approved-visible-policy'; promotion: { verdict: 'promotable'; partition: 'held-out'; objective: 'campaign-clears-deaths-stalls-exploration-resources' }; actions: Record<string, string> }
export interface AutoplayInferenceShadow { enabled: boolean; heuristic: string; inferred?: string; selected: string; reason: 'disabled' | 'artifact-unavailable' | 'artifact-invalid' | 'inference-missing' | 'illegal-action' | 'inference-selected'; disagreement: boolean }

const validArtifact = (value: unknown): value is ApprovedAutoplayPolicyArtifact => {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return false
  const artifact = value as Partial<ApprovedAutoplayPolicyArtifact>
  return artifact.version === AUTOPLAY_INFERENCE_ARTIFACT_VERSION && artifact.kind === 'jomon-approved-visible-policy' && artifact.promotion?.verdict === 'promotable' && artifact.promotion.partition === 'held-out' && artifact.promotion.objective === 'campaign-clears-deaths-stalls-exploration-resources' && Boolean(artifact.actions && typeof artifact.actions === 'object' && !Array.isArray(artifact.actions))
}

export const chooseAutoplayInferenceAction = (input: { enabled?: boolean; artifact?: unknown; fingerprint: string; heuristic: AutoplayCandidate; legalCandidates: readonly AutoplayCandidate[] }): AutoplayInferenceShadow => {
  const base = { enabled: input.enabled === true, heuristic: input.heuristic.command, selected: input.heuristic.command }
  if (!input.enabled) return { ...base, reason: 'disabled', disagreement: false }
  if (!input.artifact) return { ...base, reason: 'artifact-unavailable', disagreement: false }
  if (!validArtifact(input.artifact)) return { ...base, reason: 'artifact-invalid', disagreement: false }
  const inferred = input.artifact.actions[input.fingerprint]
  if (!inferred) return { ...base, reason: 'inference-missing', disagreement: false }
  if (!input.legalCandidates.some(candidate => candidate.command === inferred)) return { ...base, inferred, reason: 'illegal-action', disagreement: true }
  return { ...base, inferred, selected: inferred, reason: 'inference-selected', disagreement: inferred !== input.heuristic.command }
}
