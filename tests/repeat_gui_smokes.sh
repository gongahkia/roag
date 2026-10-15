#!/bin/sh
# Run every GUI smoke in a fresh process, retaining every result and stderr.
set -u
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
rounds=${1:-5}
output_dir=${2:-$(mktemp -d "${TMPDIR:-/tmp}/roag-gui-repeat-XXXXXX")}
mkdir -p "$output_dir"
manifest="$output_dir/results.tsv"
printf 'round\tharness\texit\tlog\n' > "$manifest"
failures=0
for round in $(seq 1 "$rounds"); do
    for harness in gui_smoke enemy_gui_smoke boon_gui_smoke reward_gui_smoke generated_gui_smoke exploration_gui_smoke stage_gui_smoke persistence_gui_smoke; do
        log="$output_dir/${round}-${harness}.log"
        artifacts="$output_dir/${round}-${harness}-artifacts"
        mkdir -p "$artifacts"
        (cd "$project_dir" && TMPDIR="$artifacts" sh "tests/${harness}.sh") >"$log" 2>&1
        code=$?
        printf '%s\t%s\t%s\t%s\n' "$round" "$harness" "$code" "$log" >> "$manifest"
        if [ "$code" -ne 0 ]; then
            failures=$((failures+1))
            printf 'FAIL round=%s harness=%s exit=%s log=%s\n' "$round" "$harness" "$code" "$log"
        else
            printf 'PASS round=%s harness=%s\n' "$round" "$harness"
        fi
    done
done
printf 'GUI repetition: %s runs, %s failures; results in %s\n' "$((rounds*8))" "$failures" "$output_dir"
test "$failures" -eq 0
