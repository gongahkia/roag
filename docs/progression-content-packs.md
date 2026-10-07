# Progression content packs

`content_packs/<pack>/progression_text.json` supplies player-facing progression wording: skill branches and nodes, regional and personal practices, manoeuvres, training and milestone messages, teaching and journal text, and progression overlays.

The engine owns branch, node, milestone, practice, manoeuvre, and effect IDs; prerequisites; costs; unlocks; learned state; eligibility; effects; and ordering. New saves record stable practice IDs such as `practice.bank_water_cadence` and `practice.personal:<role>`. Old bundled-default practice names are mapped only by the engine's explicit compatibility table; unknown old text remains inert and is never guessed from a selected pack.

A writing-focused editor may change names, descriptions, explanations, templates, and narration in this file. It must not change IDs, placeholder contracts, prerequisites, effect IDs, costs, learned state, manoeuvre eligibility, progression rules, RNG, or state transitions. Item, character, action/combat, and UI wording continues to use their own presentation surfaces.
