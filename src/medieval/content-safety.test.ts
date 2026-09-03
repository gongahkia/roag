import { describe, expect, it } from 'vitest'
import { MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION, PROHIBITED_MEDIEVAL_CONTENT_CLASSES, auditMedievalContentSafety, classifyMedievalContent, validateMedievalContentSafety, type ClassifiedMedievalContent, type ProhibitedMedievalContentClass } from './content-safety'
import { createInitialHousehold, initialHouseholdContentRecords } from './initial-household'
import { resolveWorldGenerationConfig } from './generation-config'

const safeHistory = (id = 'history:river-worker'): ClassifiedMedievalContent => ({
  id,
  domain: 'history',
  classification: classifyMedievalContent('history', ['adult-labour', 'travel'], 'adults-only', ['player-facing-text'])
})

describe('medieval content safety policy', () => {
  it.each(PROHIBITED_MEDIEVAL_CONTENT_CLASSES)('rejects %s deterministically', prohibitedClass => {
    const unsafe = structuredClone(safeHistory()) as unknown as { classification: { exclusions: Record<ProhibitedMedievalContentClass, string> } }
    unsafe.classification.exclusions[prohibitedClass] = 'present'

    expect(validateMedievalContentSafety([unsafe])).toEqual({
      policyVersion: MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION,
      status: 'rejected',
      diagnostics: [{ contentId: 'history:river-worker', code: `content-safety.prohibited.${prohibitedClass}` }]
    })
  })

  it('accepts explicitly classified safe template and data records', () => {
    const content: readonly ClassifiedMedievalContent[] = [
      { id: 'template:harbour-notice', domain: 'template', classification: classifyMedievalContent('template', ['commerce', 'settlement'], 'not-applicable', ['player-facing-text']) },
      { id: 'data:river-level', domain: 'data', classification: classifyMedievalContent('data', ['environment', 'navigation'], 'not-applicable') }
    ]

    expect(auditMedievalContentSafety(content)).toEqual({
      policyVersion: MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION,
      status: 'accepted',
      reviewed: [
        { id: 'data:river-level', domain: 'data', classification: classifyMedievalContent('data', ['environment', 'navigation'], 'not-applicable') },
        { id: 'template:harbour-notice', domain: 'template', classification: classifyMedievalContent('template', ['commerce', 'settlement'], 'not-applicable', ['player-facing-text']) }
      ],
      diagnostics: []
    })
  })

  it('fails closed when required content classification is absent or unknown', () => {
    expect(validateMedievalContentSafety([{ id: 'contract:unclassified', domain: 'contract' }])).toEqual({
      policyVersion: MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION,
      status: 'rejected',
      diagnostics: [{ contentId: 'contract:unclassified', code: 'content-safety.missing-classification' }]
    })

    expect(validateMedievalContentSafety([{
      id: 'hazard:unclassified',
      domain: 'hazard',
      classification: {
        policyVersion: MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION,
        domains: ['hazard'],
        participantScope: 'not-applicable',
        tags: ['environment'],
        exclusions: {
          'sexual-violence': 'excluded',
          slavery: 'excluded',
          torture: 'excluded'
        }
      }
    }])).toEqual({
      policyVersion: MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION,
      status: 'rejected',
      diagnostics: [{ contentId: 'hazard:unclassified', code: 'content-safety.malformed-exclusions' }]
    })
  })

  it('keeps policy version and diagnostics reproducible regardless of input ordering', () => {
    const first = structuredClone(safeHistory('history:first')) as unknown as { classification: { exclusions: Record<ProhibitedMedievalContentClass, string> } }
    const second = structuredClone(safeHistory('history:second')) as unknown as { classification: { exclusions: Record<ProhibitedMedievalContentClass, string> } }
    first.classification.exclusions.torture = 'present'
    second.classification.exclusions.slavery = 'present'

    const forward = validateMedievalContentSafety([second, first])
    const reversed = validateMedievalContentSafety([first, second])

    expect(forward).toEqual(reversed)
    expect(forward).toEqual({
      policyVersion: MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION,
      status: 'rejected',
      diagnostics: [
        { contentId: 'history:first', code: 'content-safety.prohibited.torture' },
        { contentId: 'history:second', code: 'content-safety.prohibited.slavery' }
      ]
    })
  })

  it('audits every generated household identity and history and rejects a forged classification', () => {
    const resolved = resolveWorldGenerationConfig({ preset: 'watershed' })
    if (resolved.status !== 'valid') throw new Error('test configuration must resolve')
    const household = createInitialHousehold({ seed: 'household content audit', configuration: resolved.configuration })
    const records = initialHouseholdContentRecords(household.roster)
    const unsafe = structuredClone(records)
    ;(unsafe[1]!.classification.exclusions as unknown as Record<string, string>).torture = 'present'

    expect(records).toHaveLength(household.roster.length * 2)
    expect(auditMedievalContentSafety(records).status).toBe('accepted')
    expect(validateMedievalContentSafety(unsafe)).toMatchObject({ status: 'rejected', diagnostics: [{ contentId: household.roster[0]!.id + ':history', code: 'content-safety.prohibited.torture' }] })
  })
})
