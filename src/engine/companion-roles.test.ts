import { describe, expect, it } from 'vitest'
import { COMPANION_ROLE_CONTRACTS, companionRoleContract, companionRoleContractErrors, isLegalCompanionRoleAction } from './companion-roles'

describe('companion role contracts', () => {
  it('completes each bounded role contract with separate direct and autonomous behavior', () => {
    expect(Object.keys(COMPANION_ROLE_CONTRACTS).sort()).toEqual(['guard', 'pathmaker', 'ritualist', 'scout'])
    for (const role of Object.keys(COMPANION_ROLE_CONTRACTS) as Array<keyof typeof COMPANION_ROLE_CONTRACTS>) expect(companionRoleContractErrors(companionRoleContract(role))).toEqual([])
    expect(isLegalCompanionRoleAction('guard', 'intercept')).toBe(true)
    expect(isLegalCompanionRoleAction('guard', 'ward')).toBe(false)
    expect(companionRoleContract('ritualist')).toMatchObject({ contentConstraint: 'TR-01', fictionNote: expect.stringContaining('Jomon-inspired fiction') })
  })

  it('never grants hidden-room locations without an in-world discovery action', () => {
    for (const role of Object.keys(COMPANION_ROLE_CONTRACTS) as Array<keyof typeof COMPANION_ROLE_CONTRACTS>) {
      const contract = companionRoleContract(role)
      expect(contract.passiveInformation.join(' ')).not.toContain('hidden-room location')
      expect(contract.prohibitedActions).toContain('reveal hidden-room locations')
    }
  })
})
