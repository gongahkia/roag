import type { PackageExteriorReadout, PackageTerms, RevealedPackageContents } from './types'

/** Authored facts stay separate from seed-created contracts and player-visible history. */
export interface AuthoredSealedPackageDefinition {
  id: string
  title: string
  hiddenContentsId: string
  exterior: PackageExteriorReadout
  terms: Omit<PackageTerms, 'destinationSiteId' | 'destinationLabel' | 'deadlineDay' | 'deadlineReckoning'>
  contents: RevealedPackageContents
}

export const KESTREL_CALIBRATION_CASE: AuthoredSealedPackageDefinition = {
  id: 'sealed-package.kestrel-calibration-case',
  title: 'Kestrel calibration case',
  hiddenContentsId: 'contents.kestrel-calibration-cores',
  exterior: {
    sealMark: 'three-point inductive seal, unbroken',
    temperature: '2°C above carrier ambient',
    powerDraw: 'intermittent 0.3 kW draw',
    balance: 'mass biased toward the lower shell',
    shielding: 'lead-composite field liner',
    handlingMark: 'UPRIGHT · DO NOT VENT · NO OPEN-FIELD SCAN'
  },
  terms: {
    sender: 'Kestrel Survey Exchange',
    intermediary: 'Jomon custody terminal',
    recipient: 'Kestrel landing instrumentation clerk',
    declaredMassKg: 18,
    holdUnits: 2,
    handlingClass: 'shielded calibration material',
    permittedInspection: 'external thermal, power, balance, seal, and shielding readout only',
    prohibitedActions: ['breaking the inductive seal', 'venting the liner', 'open-field scanning'],
    payment: 36,
    collateral: 12,
    intactSettlement: 'full payment and collateral return',
    tamperedSettlement: 'reduced payment; collateral is retained',
    failureRule: 'the recipient closes the contract after its deadline or a recorded loss'
  },
  contents: {
    id: 'contents.kestrel-calibration-cores',
    label: 'three wet calibration cores',
    materialBenefit: 'The cores contain a current Kestrel approach calibration that can be copied into the Manifest.',
    danger: 'The opened liner is no longer certified for transport and leaks corrosive coolant residue.',
    knowledge: 'The delivery label names a decommissioned relay aperture that is still drawing maintenance power.'
  }
}

export const sealedPackageDefinition = (id: string): AuthoredSealedPackageDefinition | undefined => id === KESTREL_CALIBRATION_CASE.id ? KESTREL_CALIBRATION_CASE : undefined
