import { combatReadabilityScenarios, validateCombatReadability } from '../src/engine/combat-readability'

const errors = validateCombatReadability()
console.log(JSON.stringify({ scenarios: combatReadabilityScenarios, errors }, null, 2))
if (errors.length) process.exitCode = 1
