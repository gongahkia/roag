#!/usr/bin/env bash
set -euo pipefail

cpu_count="$(getconf _NPROCESSORS_ONLN)"
case "$(uname -s)" in
  Darwin) load_1m="$(sysctl -n vm.loadavg | awk '{gsub(/[{}]/, ""); print $1}')" ;;
  *) load_1m="$(awk '{print $1}' /proc/loadavg)" ;;
esac
if ! awk -v one_minute_load="$load_1m" -v cores="$cpu_count" 'BEGIN { exit !(one_minute_load < cores) }'; then
  printf 'test host is saturated: 1m load %s >= %s CPUs; retry on an idle host\n' "$load_1m" "$cpu_count" >&2
  exit 2
fi

vitest=(npx vitest run --maxWorkers=1 --no-file-parallelism)
campaign_tests=(
  'completes the Mine reference run'
  'clears the pressure-detour regression seed'
  'clears the ranged-corridor regression seed'
  'clears the moving-route and mixed-altar regression seed'
  'clears the telegraphed Mine exit regression seed'
  'clears the long telegraph-detour regression seed'
  'clears the rail-tunnel telegraph regression seed'
  'clears the telegraphed guardian-route regression seed'
)

"${vitest[@]}" --exclude src/autoplay.test.ts
"${vitest[@]}" src/autoplay.test.ts --testNamePattern='^(?!.*(completes the Mine reference run|clears the pressure-detour regression seed|clears the ranged-corridor regression seed|clears the moving-route and mixed-altar regression seed|clears the telegraphed Mine exit regression seed|clears the long telegraph-detour regression seed|clears the rail-tunnel telegraph regression seed|clears the telegraphed guardian-route regression seed)).*$'
for test_name in "${campaign_tests[@]}"; do "${vitest[@]}" src/autoplay.test.ts --testNamePattern="$test_name"; done
