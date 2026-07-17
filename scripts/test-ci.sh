#!/usr/bin/env bash
set -euo pipefail

npx vitest run --exclude src/autoplay.test.ts
npx vitest run src/autoplay.test.ts --testNamePattern='^(?!.*(completes the Mine reference run|clears the pressure-detour regression seed|clears the ranged-corridor regression seed|clears the moving-route and mixed-altar regression seed|clears the telegraphed guardian-route regression seed)).*$'
npx vitest run src/autoplay.test.ts --testNamePattern='completes the Mine reference run'
npx vitest run src/autoplay.test.ts --testNamePattern='clears the pressure-detour regression seed'
npx vitest run src/autoplay.test.ts --testNamePattern='clears the ranged-corridor regression seed'
npx vitest run src/autoplay.test.ts --testNamePattern='clears the moving-route and mixed-altar regression seed'
npx vitest run src/autoplay.test.ts --testNamePattern='clears the telegraphed guardian-route regression seed'
