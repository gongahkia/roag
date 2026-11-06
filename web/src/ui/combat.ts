import type { ColonyController } from '../platform/controller.ts';
import type { Cell } from '../simulation/model.ts';
import type { BattleOrder, Combat } from '../simulation/combat-model.ts';
import { founders } from '../content.ts';
import { element } from './panels.ts';
import type { UiCommand } from './panels.ts';

export function entryCell(edge: Combat['edge']): Cell {
  return edge === 'north' ? { x: 64, y: 1 } : edge === 'south' ? { x: 64, y: 126 }
    : edge === 'west' ? { x: 1, y: 64 } : { x: 126, y: 64 };
}

export function setupCombat(controller: ColonyController, send: (command: UiCommand) => void): () => void {
  const x = element<HTMLInputElement>('battle-x'), y = element<HTMLInputElement>('battle-y');
  const mode = element<HTMLSelectElement>('battle-mode');
  const order = (battleMode: BattleOrder['mode'] | 'release'): void => {
    if (controller.selected === null) return;
    controller.work({ kind: 'battle', actor: controller.selected, mode: battleMode, target: { x: Number(x.value), y: Number(y.value) } });
  };
  element('battle-form').addEventListener('submit', event => { event.preventDefault(); order(mode.value as BattleOrder['mode']); });
  element('battle-release').addEventListener('click', () => order('release'));
  element('battle-shelter-all').addEventListener('click', () => {
    if (x.reportValidity() && y.reportValidity()) controller.work({ kind: 'battle', actor: 0, mode: 'shelter', target: { x: Number(x.value), y: Number(y.value) } });
  });
  element('battle-release-all').addEventListener('click', () => controller.work({ kind: 'battle', actor: 0, mode: 'release', target: { x: 64, y: 66 } }));
  element('battle-find-entry').addEventListener('click', () => {
    const target = entryCell(controller.world.combat.edge); send({ type: 'focus', ...target });
  });
  element('battle-find-heart').addEventListener('click', () => send({ type: 'focus', ...controller.world.heart }));
  const raiderRows = new Map<number, { node: HTMLElement; status: HTMLElement }>();
  let selected: number | null = null;
  let phase = '';
  return () => {
    const w = controller.world, c = w.combat, p = w.people.find(person => person.id === controller.selected);
    const phaseKey = `${w.seed}:${c.phase}:${c.wave}:${w.terminal}`;
    if (phaseKey !== phase) {
      element('raid-announcement').textContent = w.terminal ? `Colony lost: ${w.terminal === 'heart-destroyed' ? 'the heart was destroyed' : 'every founder has died'}. Start a new run when ready.`
        : c.phase === 'warning' ? `Raid warning: wave ${c.wave} approaches from the ${c.edge}. Prepare defenses and shelter civilians.`
        : c.phase === 'combat' ? `Wave ${c.wave}: defend the colony heart.`
        : c.phase === 'recovery' ? 'The raid has ended. Treat injuries, repair defenses and resume work.'
        : 'Prepare the colony: build defenses, make ammunition and assign guards.';
      phase = phaseKey;
    }
    const countdown = c.phase === 'peace' ? Math.max(0, c.warningAt - w.tick) : Math.max(0, c.spawnAt - w.tick);
    element('raid-timing').textContent = c.phase === 'peace' ? `First warning in ${countdown} ticks (${Math.ceil(countdown / 10)} simulation seconds).`
      : c.phase === 'warning' ? `Arrival in ${countdown} ticks (${Math.ceil(countdown / 10)} simulation seconds).`
      : c.phase === 'combat' ? `${c.raiders.length} attackers on the map · ${c.pending.length} awaiting entry.` : `${c.completed} wave completed.`;
    element('heart-health').textContent = String(w.heart.health);
    element('raid-phase').textContent = c.phase;
    element('battle-status').textContent = c.status;
    element('battle-statistics').textContent = `${c.stats.kills} enemies defeated · ${c.stats.shots} shots · ${c.stats.traps} trap triggers · ${c.stats.heartDamage} heart damage · ${c.stats.colonyDamage} founder damage · ${c.stats.structuresLost} structures lost`;
    element<HTMLButtonElement>('battle-find-entry').disabled = c.phase === 'peace';
    element('battle-selected').textContent = p ? `${founders[p.founder]!.name} · ${p.life}` : 'Select a founder on the map or in the roster to give a defense order.';
    const current = c.orders.find(value => value.actor === p?.id);
    element('battle-current-order').textContent = current ? `${current.mode} at ${current.target.x}, ${current.target.y}` : 'No defense order; ordinary duties apply.';
    if (p && selected !== p.id) { x.value = String(p.x); y.value = String(p.y); }
    selected = p?.id ?? null;
    for (const control of [x, y]) control.disabled = Boolean(w.terminal) || controller.busy;
    for (const control of [mode, element<HTMLButtonElement>('battle-submit')]) control.disabled = !p || p.life !== 'alive' || Boolean(w.terminal) || controller.busy;
    element<HTMLButtonElement>('battle-release').disabled = !current || Boolean(w.terminal) || controller.busy;
    element<HTMLButtonElement>('battle-shelter-all').disabled = Boolean(w.terminal) || controller.busy || !w.people.some(person => person.life === 'alive');
    element<HTMLButtonElement>('battle-release-all').disabled = Boolean(w.terminal) || controller.busy || c.orders.length === 0;
    for (const [id, row] of raiderRows) if (!c.raiders.some(raider => raider.id === id)) { row.node.remove(); raiderRows.delete(id); }
    element('raider-empty').hidden = c.raiders.length > 0;
    for (const raider of c.raiders) {
      let row = raiderRows.get(raider.id);
      if (!row) {
        const node = document.createElement('li'), status = document.createElement('span'), find = document.createElement('button');
        node.dataset.raider = String(raider.id); find.type = 'button'; find.textContent = `Find ${raider.kind} #${raider.id}`;
        find.addEventListener('click', () => { const enemy = controller.world.combat.raiders.find(value => value.id === raider.id); if (enemy) send({ type: 'focus', x: enemy.x, y: enemy.y }); });
        node.append(status, find); element('raiders').append(node); row = { node, status }; raiderRows.set(raider.id, row);
      }
      row.status.textContent = `${raider.kind} #${raider.id} · health ${raider.health} · tile ${raider.x}, ${raider.y}`;
    }
    element('attack-summary').textContent = c.attacks.length ? c.attacks.map(attack => `${attack.faction === 'hostile' ? 'Hostile' : 'Colony'} ${attack.kind}: tile ${attack.target.x}, ${attack.target.y} · impact in ${Math.max(0, attack.due - w.tick)} ticks`).join(' • ')
      : 'No attacks pending. Marked map cells show locked attack targets; the number is ticks until impact.';
  };
}
