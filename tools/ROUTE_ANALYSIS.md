# Route analysis

The production route graph is generated deterministically from the run seed
and the declarative `route_profile.legacy.base` content.

Run a headless structural batch:

```bash
luajit tools/analyze_routes.lua --seed 1000 --count 1000
```

Use `--profile route_profile.legacy.base` to select the current profile
explicitly, or `--json report.json` for a machine-readable
`roag.route_report` version 1 result.

The report lists structural failures by concrete root seed, branch and
convergence counts, and biome choices by route depth. A reported seed can be
used to start a normal run; a route node's saved floor seed can also be passed
to the generation inspector together with that node's biome and tier.

The normal-game route map appears only between floors after reconstruction and
curse selection. It uses the existing W/S or arrow-key selection and Enter/E
confirmation controls; there is no separate developer-mode route selector.
