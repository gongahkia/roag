# Companion combat development

`talent.*` definitions describe combat dispositions and permanent class choices. Every generated person receives one innate disposition through a separate seed/actor-ID hash. This does not advance name, class, world or combat randomness. The disposition stays attached to that person through recruitment, dismissal, death and saving. Narrative `trait.*` definitions still describe values, aversions and work habits.

The shipped catalog contains six innates and sixteen class choices: two alternatives at level 2 and two at level 4 for each of Warden, Ranger, Arcanist and Vestal. A living active party member can choose once per milestone between encounters. Choices cost no supplies, time or randomness; failed choices change nothing. They modify existing abilities and defenses without adding action slots or restricting dungeon access. Learning a choice is permanent; inspecting alternatives is free.

```sh
./dev content list talent
./dev content inspect talent.warden-bulwark
./dev content references class.warden
./dev content scaffold talent --id talent.new-development
./dev content validate
./dev preview talent talent.warden-bulwark --seed 42
./dev preview talent talent.arcanist-conduit --seed 1337
```

Each definition needs a sprite, description and one or two `bonuses` from the registered handlers below. `acquisition: innate` requires `level: 1` and no class; all loaded innate definitions enter the deterministic generation pool. At least one innate must remain; reference-aware removal protects the last one. Add another innate before removing that final definition. Innate bonuses must be usable by every class because assignment is independent of class. `acquisition: choice` requires a real `class.*` reference and `level: 2` or `4`. All loaded choices for that class appear in its development panel, including locked alternatives.

```json
{
  "kind": "talent",
  "id": "talent.example-breaker",
  "name": "Breaker",
  "sprite": "spark",
  "description": "Attacks gain 4 direct power against an already stunned target.",
  "acquisition": "choice",
  "class": "class.warden",
  "level": 2,
  "bonuses": { "stunned-damage": 4 }
}
```

| Bonus | Runtime effect |
| --- | --- |
| `initiative` | Adds speed when establishing initiative each round. |
| `protect` | Blocks damage for another ally already at half HP or lower, once per protector per round. Combines with equipped protection, capped at 12; an attack still deals at least 1 damage. |
| `healing` | Adds healing power to heal, medicine and cleanse abilities. Existing maximum HP still applies. |
| `calm` | Adds stress removal to calm abilities, capped by the target's current stress. Does not change rally or cleanse stress removal. |
| `mana-discount` | Reduces the acting character's paid ability cost, with a minimum cost of 1 MP. Free abilities stay free; helper costs do not change. |
| `wounded-damage` | Adds direct power against a target already at half HP or lower. |
| `marked-damage` | Adds direct power against an already marked target. The first application does not qualify itself. |
| `stunned-damage` | Adds direct power against an already stunned target. The first application does not qualify itself. |
| `all-target-damage` | Adds direct power to attacks targeting all enemies. |
| `burn-damage` | Adds direct power to burning attacks; later burn ticks retain their normal damage. |
| `cooperative-damage` | Adds direct power when this character initiates a cooperative damaging ability. Assisting alone does not trigger it. |

Power is bounded 1–8, except `calm` allows 1–20 and `mana-discount` allows 1–2. Damage bonuses combine before armor, guard, ally protection and actual remaining HP. Choosing equipment and developments that cooperate with another character's mark/stun actions is intentional. A class choice must have a compatible ability in its class, common or cooperative pools; validation rejects unused effects, such as a Warden calm bonus with no calming ability. A mana discount needs at least one action costing more than the discount so its full bonus can apply. Prepared abilities belong to enemy behavior rules and are rejected from every player action pool.

Talent previews generate matched isolated encounters, neutralize unrelated dispositions and execute the real initiative, ability or enemy-attack resolver. Each declared bonus reports its expected and observed change, feedback events, targets and pass/fail result. They support medicine/cooperative abilities and a catalog with only one innate. These are mechanic checks rather than balance simulations; they do not use player saves. Focused tests also cover inactive conditions, unchanged invalid commands, exact battle continuation after loading and forged save rejection.

Use `./roag.sh --scene world-progression` for the native four-class fixture and `./dev verify-native --scenes world-progression` for its mouse acceptance checks. Live gameplay remains the source of pacing and balance feedback. Adding/removing or changing talent mechanics changes the gameplay content hash, preserving existing incompatible saves behind the explicit-new-run safeguard. Save loading validates innate IDs, class eligibility, unlocked milestones and one selection per milestone. New handlers require Core implementation, action previews, validation, runtime preview coverage and tests before being authored in JSON.
