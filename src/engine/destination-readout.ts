import type { DestinationCondition } from '../types'

const details: Record<DestinationCondition, string> = {
  'calibration-queue': 'Instrumentation clerks are clearing a bounded calibration queue.',
  'approach-inspection': 'Approach instruments are under an inspection hold.',
  'relay-balanced': 'Relay crews report balanced thermal load.',
  'relay-overheated': 'Relay heat exchangers are carrying more load than planned.',
  'relay-throttled': 'Relay staff have imposed a controlled transfer window.',
  'tender-cycle': 'Salvage tenders are cycling through ordinary drydock work.',
  'dock-congested': 'Tender berths are congested and local dock work is delayed.',
  'pump-watch': 'Pump Bank Four is on watch for wear in the pressure chain.',
  'cavitation-restriction': 'Pump cavitation has narrowed the safe transfer approaches.',
  'bypass-stabilizing': 'A temporary bypass is carrying load while the pump chain is verified.',
  'pump-stabilized': 'Pump crews have stabilized the bypass, but the repair remains provisional.',
  'kiln-nominal': 'Ceramic kilns are operating inside their planned thermal envelope.',
  'kiln-backlog': 'Ceramic orders are accumulating behind maintenance work.',
  'kiln-cooldown': 'Glassworks crews are using a controlled cooldown to protect the kilns.'
}

export const destinationConditionDetail = (condition: DestinationCondition): string => details[condition]
