import type { ColonyController } from '../platform/controller.ts';
import type { Cell } from '../simulation/model.ts';
import type { BattleOrder, Combat } from '../simulation/combat-model.ts';
import { COMBAT_CONTENT } from '../simulation/combat-content.ts';
import { ENCOUNTER_CONTENT } from '../simulation/encounter-content.ts';
import type { RaidKind } from '../simulation/raid-model.ts';
import { founders } from '../content.ts';
import { element } from './panels.ts';
import type { UiCommand } from './panels.ts';

const roles: Record<RaidKind, string> = { raider: 'Raider', breacher: 'Breacher', shield: 'Shield guard', archer: 'Archer', healer: 'Healer', rusher: 'Rusher', captain: 'Ossuary Captain' };
const duration = (ticks: number): string => `${Math.max(0, ticks)} ticks (${Math.ceil(Math.max(0, ticks) / 10)} simulation seconds)`;

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
  const barrierRows = new Map<number, { node: HTMLElement; status: HTMLElement; clear: HTMLButtonElement }>();
  let historyKey = '';
  let selected: number | null = null;
  let phase = '';
  return () => {
    const w = controller.world, c = w.combat, p = w.people.find(person => person.id === controller.selected);
    const phaseKey = `${w.seed}:${c.phase}:${c.wave}:${w.terminal}`;
    if (phaseKey !== phase) {
      element('raid-announcement').textContent = w.terminal ? `Colony lost: ${w.terminal === 'heart-destroyed' ? 'the heart was destroyed' : 'every founder has died'}. Start a new run when ready.`
        : c.phase === 'warning' ? `Raid warning: wave ${c.wave} approaches from the ${c.edge}.${c.pending.some(t => t.kind === 'captain') ? ' The Ossuary Captain leads this wave.' : ''} Prepare defenses and shelter civilians.`
        : c.phase === 'combat' ? `Wave ${c.wave}: defend the colony heart.`
        : c.phase === 'recovery' ? `Wave ${c.completed} survived. Treat injuries, repair defenses and reload before wave ${c.completed + 1}.`
        : 'Prepare the colony: build defenses, make ammunition and assign guards.';
      phase = phaseKey;
    }
    element('raid-timing').textContent = c.phase === 'peace' ? `First warning in ${duration(c.warningAt - w.tick)}.`
      : c.phase === 'warning' ? `Arrival in ${duration(c.spawnAt - w.tick)}.`
      : c.phase === 'combat' ? `${c.raiders.length} attackers on the map · ${c.pending.length} awaiting entry.`
      : `Wave ${c.completed + 1} warning in ${duration(c.warningAt - w.tick)}. Recovery until tick ${c.recoveryUntil}.`;
    element('raid-budget').textContent = c.wave ? `Wave ${c.wave} budget: ${c.spent}/${c.budget} committed · ${c.pending.length} awaiting entry · ${c.completed} completed`
      : 'Every fourth wave brings the Ossuary Captain. Each announced wave has a finite budget.';
    element('heart-health').textContent = String(w.heart.health);
    element('raid-phase').textContent = c.phase;
    element('battle-status').textContent = c.status;
    element('battle-statistics').textContent = `${c.stats.kills} enemies defeated · ${c.stats.bosses} Captains defeated · ${c.stats.shots} shots · ${c.stats.traps} trap triggers · ${c.stats.heartDamage} heart damage · ${c.stats.colonyDamage} founder damage · ${c.stats.structuresLost} structures lost · ${c.stats.repairs} repairs`;
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
      row.node.dataset.role = raider.kind;
      row.node.querySelector('button')!.textContent = `Find ${raider.kind} #${raider.id}`;
      const leader = c.raiders.find(r => r.id === raider.leaderId);
      const pendingLeader = c.pending.some(t => t.id === raider.leaderId);
      const attack = c.attacks.find(a => a.source.kind === 'raider' && a.source.id === raider.id);
      const group = raider.squadId === null ? 'Independent'
        : `Squad #${raider.squadId} · ${raider.leaderId === raider.id ? 'squad leader' : leader ? `leader ${roles[leader.kind]} #${leader.id}` : pendingLeader ? 'leader awaiting entry' : 'leader lost'}`;
      let state = attack ? `${attack.kind} committed · impact in ${attack.due - w.tick} ticks` : 'Advancing';
      if (raider.kind === 'healer') state = attack?.kind === 'heal' ? `Healing #${attack.recipient} at locked tile ${attack.target.x}, ${attack.target.y} · ${attack.due - w.tick} ticks` : attack ? state : 'Seeking a wounded squad member';
      if (raider.kind === 'rusher' && leader?.kind === 'healer' && raider.health < COMBAT_CONTENT.rusher.health) state += ' · wounded; healer support available';
      if (raider.kind === 'shield') state += raider.loweredUntil > w.tick ? ` · shield lowered for ${raider.loweredUntil - w.tick} ticks` : ` · front protection ${ENCOUNTER_CONTENT.shield.reductionPercent}%`;
      if (raider.kind === 'captain') {
        if (raider.charge) state = `Charging ${raider.charge.cursor}/${raider.charge.cells.length} cells · ${raider.charge.hits.length} identities struck`;
        if (raider.exposedUntil > w.tick) state += ` · exposed: +${ENCOUNTER_CONTENT.captain.exposedPercent}% incoming damage for ${raider.exposedUntil - w.tick} ticks`;
        else state += raider.charge || attack ? ' · front protection open during commitment' : ` · front protection ${ENCOUNTER_CONTENT.captain.reductionPercent}%`;
      }
      row.status.textContent = `${roles[raider.kind]} #${raider.id} · health ${raider.health}/${COMBAT_CONTENT[raider.kind].health} · tile ${raider.x}, ${raider.y}\nFacing ${raider.facing} · ${group}\n${state}`;
    }
    for (const [id, row] of barrierRows) if (!c.barricades.some(barrier => barrier.id === id)) { row.node.remove(); barrierRows.delete(id); }
    element('barricade-empty').hidden = c.barricades.length > 0;
    for (const barrier of c.barricades) {
      let row = barrierRows.get(barrier.id);
      if (!row) {
        const node = document.createElement('li'), status = document.createElement('span'), actions = document.createElement('div'), find = document.createElement('button'), clear = document.createElement('button');
        node.dataset.barricade = String(barrier.id); actions.className = 'barricade-actions'; find.type = clear.type = 'button';
        find.textContent = 'Find / select barricade'; clear.textContent = 'Order all to clear';
        find.addEventListener('click', () => {
          const target = controller.world.combat.barricades.find(b => b.id === barrier.id); if (!target) return;
          x.value = String(target.x); y.value = String(target.y); mode.value = 'clear'; send({ type: 'focus', x: target.x, y: target.y });
        });
        clear.addEventListener('click', () => {
          const target = controller.world.combat.barricades.find(b => b.id === barrier.id);
          if (target) controller.work({ kind: 'battle', actor: 0, mode: 'clear', target: { x: target.x, y: target.y } });
        });
        actions.append(find, clear); node.append(status, actions); element('barricades').append(node); row = { node, status, clear }; barrierRows.set(barrier.id, row);
      }
      row.status.textContent = `Barricade #${barrier.id} · health ${barrier.health}/${ENCOUNTER_CONTENT.rampart.health} · tile ${barrier.x}, ${barrier.y} · Captain #${barrier.owner}\nBlocks movement and shots · expires in ${duration(barrier.expiresAt - w.tick)}`;
      row.clear.disabled = Boolean(w.terminal) || controller.busy || !w.people.some(person => person.life === 'alive');
    }
    element('attack-summary').textContent = c.attacks.length ? c.attacks.map(attack => `${attack.faction === 'hostile' ? 'Hostile' : 'Colony'} ${attack.kind}: tile ${attack.target.x}, ${attack.target.y}${attack.recipient !== null ? ` · recipient #${attack.recipient}` : ''} · ${attack.cells.length} locked ${attack.cells.length === 1 ? 'cell' : 'cells'} · impact in ${Math.max(0, attack.due - w.tick)} ticks`).join(' • ')
      : 'No attacks pending. Marked map cells show locked attack targets; the number is ticks until impact.';
    const history = JSON.stringify(c.history);
    if (history !== historyKey) {
      const body = element('raid-history-body'); body.replaceChildren();
      for (const summary of [...c.history].reverse()) {
        const row = document.createElement('tr'); row.dataset.wave = String(summary.wave);
        for (const value of [summary.wave, `${summary.spent}/${summary.budget}`, summary.kills, summary.colonistsLost, summary.ammoSpent, summary.structuresLost, summary.repairs, `${summary.startedAt}–${summary.endedAt}`]) {
          const cell = document.createElement('td'); cell.textContent = String(value); row.append(cell);
        }
        body.append(row);
      }
      element('raid-history-empty').hidden = c.history.length > 0; historyKey = history;
    }
  };
}
