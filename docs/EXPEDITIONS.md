# Ithica expeditions

The expedition view is part of the same lunar colony campaign and window. Residents who leave stop working at home; their injuries, charge, fatigue, morale and experience come back with them. Start with Ada alone and grow through rescue.

## Leave and return

Click a stranded person or site on the colony map. Preparation holds time, shows home stock/staffing, and lets you select one to four ready residents and carried supplies. Enter confirms; Esc cancels without spending anything. The standard pack is 4 food, 4 water, 2 medical kits and 1 filter; adjust it to the route and party.

The approach costs eight shared ticks. Outside, WASD/arrows or clicks move; E interacts. Share one food and water with the stranded person, then escort them back through the return approach. Escorted people still use oxygen on logical ticks; refilling supplies them too. They join only in breathable home shelter and usually need recovery.

Return travel costs eight ticks. The paused arrival report deposits cargo and unused supplies once and preserves resident condition. Dismiss it to resume the previous home speed/pause state. A failed return leaves carried salvage recoverable at its site, retaining cleared encounters.

## Decisions and time

Thinking, inventory viewing, targeting, guide/atlas reading and camera changes spend no time. Accepted steps/waits/combat actions advance one shared tick; rescues, salvage, refills and suppression cost four; treatment eight; repairs/seal cycling twelve; rest thirty-two. Hostile actions resolve deterministically to the next human decision. The colony never runs against wall time while you choose.

Click-routes commit steps progressively and stop on encounters, exposure, blocked movement or dangerous new home notices. Esc stops the remaining route. Home pressure/stock remain visible. High-priority home notices appear at a decision boundary.

## Combat and supplies

Combat stays top-down. Read the enemy's intent, choose 1–6, then click an enemy/unit/party card as appropriate. Enter uses the current action on a default appropriate target. Roles provide three abilities each; everyone can guard, use carried medical supplies or retreat. It is ability/initiative combat without tactical walking, range or cover.

Outside combat, O refills suits with one water per person, H treats the selected resident with a medical kit, C rests with food/water, and T suppresses nearby exposure with a kit. These operations consume time and supplies. Rest restores energy/charge/composure; treatment restores health. Home beds recharge equipment, and powered medical care improves sleeping recovery.

## Progression

The first rescue introduces travel and return without a mandatory battle. After recruitment, a salvage site offers an encounter and a usable component. Bring it home, then use U to build suit service on powered deck for 24 metal, 4 crystal and 1 component. Powered service equips more oxygen-efficient suits and reveals a deeper signal.

Below the surface, restore the pump with two carried water, cycle the seal, resolve the heavier encounter and recover another component. The pump's persistent water supply helps the shelter and resolves three deep readings near the station. Back home, use Deep Workings to survey, construct a powered pressure annex/refuge, operate a rig and turn ore into reinforcement/components. The first readings use the existing colony map; drilling has three finite layers, without walking z-levels. Priorities, shifts and rest determine who can staff the mine or maintain the shelter; stranded/carrying/off-duty residents cannot depart. Reinforced suit service further improves oxygen savings to 50% on later departures.

## Inspect safely

```sh
./ithica.sh --scene preparation
./ithica.sh --scene surface
./ithica.sh --scene battle
./ithica.sh --scene structure
./ithica.sh --scene arrival
```

These fresh inspection scenes use real commands and do not autosave on exit. F5 explicitly saves; F9 loads a whole campaign. Normal saves live at `~/.ithica/campaign.json`. F2 opens the complete atlas in the same window; F1 opens the guide outside.

See [the master reference](PROJECT_STATE.md) for exact mechanics, counts, saves and controls. The independent historical fantasy prototype remains under `./rpg.sh` / `--preview`; its guide is [LEGACY_RPG.md](LEGACY_RPG.md).
