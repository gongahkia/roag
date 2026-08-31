// 3e61f2b0be4188050f22123056a7e05863385cc2
import { ITEM } from '../content';
import { recordTelemetryCount } from '../telemetry';
import { advance } from './combat';
import { grantGold } from './economy';
import { event, log } from './shared';
import { refreshFov } from './visibility';
import { acquireOptionalTraversalTool, boonRank, toolFor } from './buildcraft';
import { settleCurseAfterEncounter } from './curses';
import { addCondition } from './conditions';
import { tend } from './alignment';
import { adjustSocialReputation, socialDispositionFor } from '../social-contract';
const expansionEncounterKinds = ['sunTribute', 'mirageMarket', 'brineOath', 'glassTrial', 'whiteRoad', 'saltCache', 'iceDuel', 'winterTithe', 'rimeContract', 'frostCache', 'whiteout', 'reliquaryTrial'];
const expansionProfiles = {
  sunTribute: {
    title: 'SUN TRIBUTE',
    cost: 'health',
    value: 3,
    boon: 'sunstep',
    reward: 'fireJar',
    gold: 100,
    risk: 'terrain',
    terrain: 'brine'
  },
  mirageMarket: {
    title: 'MIRAGE MARKET',
    cost: 'focus',
    value: 3,
    boon: 'mirageMap',
    reward: 'blink',
    gold: 75,
    risk: 'map',
    terrain: 'brine'
  },
  brineOath: {
    title: 'BRINE OATH',
    cost: 'maxHealth',
    value: 3,
    boon: 'brineWard',
    reward: 'focusTonic',
    gold: 120,
    risk: 'curse',
    terrain: 'brine'
  },
  glassTrial: {
    title: 'GLASS TRIAL',
    cost: 'item',
    value: 1,
    item: 'tonic',
    boon: 'mirrorHunt',
    reward: 'sight',
    gold: 95,
    risk: 'terrain',
    terrain: 'brine'
  },
  whiteRoad: {
    title: 'WHITE ROAD',
    cost: 'health',
    value: 4,
    boon: 'whiteRoad',
    reward: 'bridgeKit',
    gold: 115,
    risk: 'shield',
    terrain: 'brine'
  },
  saltCache: {
    title: 'SALT CACHE',
    cost: 'focus',
    value: 2,
    boon: 'saltLedger',
    reward: 'mapScroll',
    gold: 90,
    risk: 'curse',
    terrain: 'brine'
  },
  iceDuel: {
    title: 'ICE DUEL',
    cost: 'health',
    value: 4,
    boon: 'duelistOath',
    reward: 'ward',
    gold: 120,
    risk: 'shield',
    terrain: 'frostRime'
  },
  winterTithe: {
    title: 'WINTER TITHE',
    cost: 'maxHealth',
    value: 3,
    boon: 'winterRations',
    reward: 'mend',
    gold: 125,
    risk: 'curse',
    terrain: 'frostRime'
  },
  rimeContract: {
    title: 'RIME CONTRACT',
    cost: 'item',
    value: 1,
    item: 'focusTonic',
    boon: 'shatterMark',
    reward: 'grappleLine',
    gold: 105,
    risk: 'terrain',
    terrain: 'frostRime'
  },
  frostCache: {
    title: 'FROST CACHE',
    cost: 'focus',
    value: 3,
    boon: 'coldRead',
    reward: 'sight',
    gold: 90,
    risk: 'map',
    terrain: 'frostRime'
  },
  whiteout: {
    title: 'WHITEOUT',
    cost: 'health',
    value: 3,
    boon: 'thawStep',
    reward: 'tonic',
    gold: 110,
    risk: 'curse',
    terrain: 'frostRime'
  },
  reliquaryTrial: {
    title: 'RELIQUARY TRIAL',
    cost: 'focus',
    value: 3,
    boon: 'reliquaryEcho',
    reward: 'wardScript',
    gold: 110,
    risk: 'shield',
    terrain: 'frostRime'
  }
};
const expansionProfileFor = kind => expansionEncounterKinds.includes(kind) ? expansionProfiles[kind] : undefined;
const alignmentEncounterKinds = ['minePact', 'mineKami', 'wildsPact', 'wildsKami', 'cavernsPact', 'cavernsKami', 'ruinsPact', 'ruinsKami', 'furnacePact', 'furnaceKami', 'floodedPact', 'floodedKami', 'cliffsPact', 'cliffsKami', 'burialPact', 'burialKami', 'saltPact', 'saltKami', 'frostPact', 'frostKami'];
const alignmentProfiles = {
  minePact: {
    title: 'KESTREL PRACTICAL PLAN',
    alignment: 'villagePact',
    cost: 'cash',
    value: 35,
    reward: 'bombPack',
    gold: 70
  },
  mineKami: {
    title: 'KESTREL IDEALIST PLAN',
    alignment: 'kami',
    cost: 'focus',
    value: 2,
    reward: 'ward',
    gold: 45
  },
  wildsPact: {
    title: 'VERDANT PRACTICAL PLAN',
    alignment: 'villagePact',
    cost: 'cash',
    value: 30,
    reward: 'ropeBundle',
    gold: 65
  },
  wildsKami: {
    title: 'VERDANT IDEALIST PLAN',
    alignment: 'kami',
    cost: 'health',
    value: 2,
    reward: 'sight',
    gold: 45
  },
  cavernsPact: {
    title: 'PELAGOS PRACTICAL PLAN',
    alignment: 'villagePact',
    cost: 'focus',
    value: 2,
    reward: 'bridgeKit',
    gold: 60
  },
  cavernsKami: {
    title: 'PELAGOS IDEALIST PLAN',
    alignment: 'kami',
    cost: 'health',
    value: 2,
    reward: 'focusTonic',
    gold: 55
  },
  ruinsPact: {
    title: 'ORISON PRACTICAL PLAN',
    alignment: 'villagePact',
    cost: 'cash',
    value: 40,
    reward: 'mapScroll',
    gold: 80
  },
  ruinsKami: {
    title: 'ORISON IDEALIST PLAN',
    alignment: 'kami',
    cost: 'focus',
    value: 3,
    reward: 'wardScript',
    gold: 65
  },
  furnacePact: {
    title: 'HELION PRACTICAL PLAN',
    alignment: 'villagePact',
    cost: 'health',
    value: 3,
    reward: 'fireJar',
    gold: 90
  },
  furnaceKami: {
    title: 'HELION IDEALIST PLAN',
    alignment: 'kami',
    cost: 'focus',
    value: 3,
    reward: 'tonic',
    gold: 75
  },
  floodedPact: {
    title: 'NERIDA PRACTICAL PLAN',
    alignment: 'villagePact',
    cost: 'cash',
    value: 45,
    reward: 'portableWinch',
    gold: 90
  },
  floodedKami: {
    title: 'NERIDA IDEALIST PLAN',
    alignment: 'kami',
    cost: 'health',
    value: 2,
    reward: 'grappleLine',
    gold: 70
  },
  cliffsPact: {
    title: 'AERIE PRACTICAL PLAN',
    alignment: 'villagePact',
    cost: 'focus',
    value: 2,
    reward: 'cliffSpool',
    gold: 75
  },
  cliffsKami: {
    title: 'AERIE IDEALIST PLAN',
    alignment: 'kami',
    cost: 'health',
    value: 3,
    reward: 'sight',
    gold: 70
  },
  burialPact: {
    title: 'MEMORIAL PRACTICAL PLAN',
    alignment: 'villagePact',
    cost: 'cash',
    value: 45,
    reward: 'mend',
    gold: 85
  },
  burialKami: {
    title: 'MEMORIAL IDEALIST PLAN',
    alignment: 'kami',
    cost: 'focus',
    value: 3,
    reward: 'ancestorToken',
    gold: 75
  },
  saltPact: {
    title: 'HALCYON PRACTICAL PLAN',
    alignment: 'villagePact',
    cost: 'health',
    value: 3,
    reward: 'bridgeKit',
    gold: 95
  },
  saltKami: {
    title: 'HALCYON IDEALIST PLAN',
    alignment: 'kami',
    cost: 'focus',
    value: 3,
    reward: 'fireJar',
    gold: 80
  },
  frostPact: {
    title: 'BOREALIS PRACTICAL PLAN',
    alignment: 'villagePact',
    cost: 'cash',
    value: 50,
    reward: 'mend',
    gold: 100
  },
  frostKami: {
    title: 'BOREALIS IDEALIST PLAN',
    alignment: 'kami',
    cost: 'health',
    value: 3,
    reward: 'ward',
    gold: 80
  }
};
const alignmentProfileFor = kind => alignmentEncounterKinds.includes(kind) ? alignmentProfiles[kind] : undefined;
const existingEncounterAlignment = {
  wayfarer: 'villagePact',
  bloodBargain: 'kami',
  stormCache: 'villagePact',
  windTrial: 'kami',
  ancestorDebt: 'kami',
  tombAuction: 'kami',
  oathwell: 'kami',
  cursedObject: 'kami',
  sunTribute: 'kami',
  mirageMarket: 'villagePact',
  brineOath: 'kami',
  glassTrial: 'villagePact',
  whiteRoad: 'villagePact',
  saltCache: 'villagePact',
  iceDuel: 'villagePact',
  winterTithe: 'kami',
  rimeContract: 'villagePact',
  frostCache: 'villagePact',
  whiteout: 'kami',
  reliquaryTrial: 'kami'
};
export const encounterTitle = kind => {
  var _alignmentProfileFor$, _alignmentProfileFor, _expansionProfileFor;
  const title = (_alignmentProfileFor$ = (_alignmentProfileFor = alignmentProfileFor(kind)) === null || _alignmentProfileFor === void 0 ? void 0 : _alignmentProfileFor.title) !== null && _alignmentProfileFor$ !== void 0 ? _alignmentProfileFor$ : (_expansionProfileFor = expansionProfileFor(kind)) === null || _expansionProfileFor === void 0 ? void 0 : _expansionProfileFor.title;
  return title !== null && title !== void 0 ? title : kind === 'wayfarer' ? 'STRANDED CREW' : kind === 'bloodBargain' ? 'SEALED PROTOCOL' : kind === 'stormCache' ? 'VECTOR CACHE' : kind === 'windTrial' ? 'VECTOR TEST' : kind === 'ancestorDebt' ? 'ARCHIVE DEBT' : kind === 'tombAuction' ? 'SALVAGE EXCHANGE' : kind === 'oathwell' ? 'PROTOCOL WELL' : kind === 'cursedObject' ? 'CORRUPTED OBJECT' : 'SHIFTING CHAMBER';
};
const traversalRewards = ['grappleLine', 'bridgeKit', 'steamJetpack', 'portableWinch'];
const encounterAtReach = state => {
  var _state$floor$encounte;
  return (_state$floor$encounte = state.floor.encounters) === null || _state$floor$encounte === void 0 ? void 0 : _state$floor$encounte.find(encounter => encounter.state === 'dormant' && Math.max(Math.abs(encounter.x - state.hero.x), Math.abs(encounter.y - state.hero.y)) <= 1);
};
const encounter = (state, id) => {
  var _state$floor$encounte2;
  return (_state$floor$encounte2 = state.floor.encounters) === null || _state$floor$encounte2 === void 0 ? void 0 : _state$floor$encounte2.find(current => current.id === id && current.state === 'dormant');
};
const rewardFor = (state, source) => traversalRewards[(state.seed + state.floor.index + source.x + source.y) % traversalRewards.length];
const grantItem = (state, id) => {
  if (state.hero.inventory.length < 12) state.hero.inventory.push(id);else state.floor.items.push({
    id,
    x: state.hero.x,
    y: state.hero.y,
    count: 1,
    visibleInFog: true
  });
};
const grantContextGold = (state, amount) => {
  var _state$floor$difficul, _state$floor$difficul2;
  grantGold(state, amount + ((_state$floor$difficul = (_state$floor$difficul2 = state.floor.difficulty) === null || _state$floor$difficul2 === void 0 ? void 0 : _state$floor$difficul2.threat) !== null && _state$floor$difficul !== void 0 ? _state$floor$difficul : 0) * 5);
};
const contextualCost = (state, amount) => {
  var _state$floor$difficul3, _state$floor$difficul4;
  return amount + ((_state$floor$difficul3 = (_state$floor$difficul4 = state.floor.difficulty) === null || _state$floor$difficul4 === void 0 ? void 0 : _state$floor$difficul4.threat) !== null && _state$floor$difficul3 !== void 0 ? _state$floor$difficul3 : 0) * 3;
};
const bindOptionalTool = (state, tool) => {
  const acquired = acquireOptionalTraversalTool(state, tool);
  log(state, acquired.result === 'bound' ? `You bind the ${toolFor(tool).name}.` : acquired.result === 'duplicate' ? `You already carry the ${toolFor(tool).name}; leave its duplicate behind.` : `You replace ${toolFor(acquired.replaced).name} with the ${toolFor(tool).name}.`);
};
const toolOfferDetail = tool => `Optional tool: ${toolFor(tool).name}; duplicates stay unbound and a full loadout replaces its oldest slot.`;
const socialOfferDetail = source => {
  var _source$social, _source$social2;
  return ((_source$social = source.social) === null || _source$social === void 0 ? void 0 : _source$social.offer) === 'supplyCache' ? 'Gain a rope bundle and mark the cache.' : ((_source$social2 = source.social) === null || _source$social2 === void 0 ? void 0 : _source$social2.offer) === 'shortcut' ? 'Open one nearby locked route, if present.' : 'Reveal the remaining route.';
};
const applySocialOffer = (state, source) => {
  var _source$social3, _source$social4;
  if (((_source$social3 = source.social) === null || _source$social3 === void 0 ? void 0 : _source$social3.offer) === 'supplyCache') grantItem(state, 'ropeBundle');else if (((_source$social4 = source.social) === null || _source$social4 === void 0 ? void 0 : _source$social4.offer) === 'shortcut') {
    const shortcut = state.floor.tiles.find(tile => tile.kind === 'lockedDoor');
    if (shortcut) shortcut.kind = 'door';else state.floor.tiles.forEach(tile => {
      tile.explored = true;
    });
  } else state.floor.tiles.forEach(tile => {
    tile.explored = true;
  });
  refreshFov(state);
};
const socialHostile = (state, source) => {
  const social = source.social;
  const actor = {
    id: `social-hostile:${source.id}`,
    role: 'ally',
    kind: 'ally',
    name: social.faction === 'kami' ? 'Idealist holdout' : 'Pragmatic rival',
    x: source.x,
    y: source.y,
    health: 12,
    maxHealth: 12,
    attack: 4,
    defense: 9,
    speed: 90,
    energy: 0,
    glyph: '!',
    color: social.faction === 'kami' ? '#b6d8ff' : '#f7c677',
    hostile: true,
    ai: 'chase',
    tags: ['social', social.faction, social.role],
    status: [`social:${social.id}:hostile`]
  };
  state.floor.actors.push(actor);
};
const resolve = (state, source, outcome, events) => {
  var _state$hero$condition, _state$hero$condition2;
  source.state = 'resolved';
  state.modal = undefined;
  if (source.kind !== 'cursedObject') settleCurseAfterEncounter(state);
  const dividend = boonRank(state, 'echoDividend') * 10;
  if (dividend) grantContextGold(state, dividend);
  const ward = boonRank(state, 'ossuaryWard');
  if (ward) state.hero.conditions = [...((_state$hero$condition = state.hero.conditions) !== null && _state$hero$condition !== void 0 ? _state$hero$condition : []), {
    kind: 'shielded',
    duration: 2,
    potency: ward
  }];
  const reliquaryEcho = boonRank(state, 'reliquaryEcho');
  if (reliquaryEcho) state.hero.conditions = [...((_state$hero$condition2 = state.hero.conditions) !== null && _state$hero$condition2 !== void 0 ? _state$hero$condition2 : []), {
    kind: 'shielded',
    duration: 2,
    potency: reliquaryEcho
  }];
  recordTelemetryCount(state, 'eventOutcomes', `${source.kind}:${outcome}`);
  const alignment = existingEncounterAlignment[source.kind];
  if (alignment && outcome !== 'leave' && outcome !== 'decline') tend(state, alignment);
  return [event('encounter'), ...events];
};
export const encounterOptions = (state, source) => {
  var _state$hero$oaths$len, _state$hero$oaths, _state$hero$oaths$len2, _state$hero$oaths2;
  if (source.social) {
    const disposition = socialDispositionFor(state.reputation, source.social.faction);
    const ally = disposition === 'hostile' ? 'MAKE AMENDS' : 'HONOR THE OFFER';
    return [{
      label: ally,
      detail: `${source.social.goal}; ${socialOfferDetail(source)}${source.toolOffer ? ` ${toolOfferDetail(source.toolOffer)}` : ''}`,
      available: true
    }, {
      label: 'PRESS THE CLAIM',
      detail: `Lose standing with ${source.social.faction}; they become hostile.`,
      available: true
    }, {
      label: 'LEAVE',
      detail: 'Leave this local concern untouched.',
      available: true
    }];
  }
  if (source.toolOffer) return [{
    label: 'TAKE TOOL',
    detail: toolOfferDetail(source.toolOffer),
    available: true
  }, {
    label: 'MAP ROUTE',
    detail: 'Reveal the floor without taking the tool.',
    available: true
  }, {
    label: 'LEAVE',
    detail: 'Leave the optional cache untouched.',
    available: true
  }];
  const alignment = alignmentProfileFor(source.kind);
  if (alignment) {
    const cost = alignment.cost === 'health' ? `Lose ${alignment.value} HP` : alignment.cost === 'focus' ? `Spend ${alignment.value} focus` : `Spend ${alignment.value} cash`;
    const available = alignment.cost === 'health' ? state.hero.health > alignment.value + 2 : alignment.cost === 'focus' ? state.hero.focus >= alignment.value : state.hero.gold >= alignment.value;
    return [{
      label: alignment.alignment === 'kami' ? 'BACK IDEALISM' : 'BACK PRAGMATISM',
      detail: `${cost}; gain ${alignment.gold} cash and ${ITEM[alignment.reward].name}.`,
      available
    }, {
      label: alignment.alignment === 'kami' ? 'PROTECT THE POSSIBILITY' : 'SECURE THE ROUTE',
      detail: `Lose 2 HP; reveal the floor and gain ${alignment.reward ? ITEM[alignment.reward].name : 'a route tool'}.`,
      available: state.hero.health > 4
    }, {
      label: 'LEAVE',
      detail: 'Pass the gathering without answer.',
      available: true
    }];
  }
  const expansion = expansionProfileFor(source.kind);
  if (expansion) {
    const primary = expansion.cost === 'health' ? {
      label: 'BLEED FOR THE OFFER',
      detail: `Lose ${expansion.value} HP; gain ${expansion.gold} cash, ${expansion.boon}, and ${expansion.reward ? ITEM[expansion.reward].name : 'a reward'}.`,
      available: state.hero.health > expansion.value + 2
    } : expansion.cost === 'maxHealth' ? {
      label: 'PAY VITALITY',
      detail: `Lose ${expansion.value} maximum HP; gain ${expansion.gold} cash and ${expansion.boon}.`,
      available: state.hero.maxHealth > expansion.value + 6
    } : expansion.cost === 'focus' ? {
      label: 'SPEND FOCUS',
      detail: `Spend ${expansion.value} focus; gain ${expansion.gold} cash and ${expansion.boon}.`,
      available: state.hero.focus >= expansion.value
    } : {
      label: 'GIVE AN OFFERING',
      detail: `Consume ${ITEM[expansion.item].name}; gain ${expansion.gold} cash and ${expansion.boon}.`,
      available: state.hero.inventory.includes(expansion.item)
    };
    const risk = expansion.risk === 'curse' ? {
      label: 'TAKE THE CURSE',
      detail: 'Take a two-encounter damage curse for 150 cash.',
      available: !state.hero.curse
    } : expansion.risk === 'terrain' ? {
      label: 'CRACK THE GROUND',
      detail: `Gain 90 cash; nearby ground becomes dangerous ${expansion.terrain === 'brine' ? 'brine' : 'rime'}.`,
      available: true
    } : expansion.risk === 'map' ? {
      label: 'READ THE OMEN',
      detail: 'Reveal the floor and gain 2 focus.',
      available: true
    } : {
      label: 'TAKE THE WARD',
      detail: 'Gain a three-turn shield and 70 cash.',
      available: true
    };
    return [primary, risk, {
      label: 'LEAVE',
      detail: 'Leave the offer untouched.',
      available: true
    }];
  }
  if (source.kind === 'stormCache') return [{
    label: 'OPEN CACHE',
    detail: 'Gain climbing gear, 60 cash, and one traversal Boon rank.',
    available: true
  }, {
    label: 'MAP WIND',
    detail: 'Reveal the floor and gain 2 focus.',
    available: true
  }, {
    label: 'LEAVE',
    detail: 'Leave the storm cache sealed.',
    available: true
  }];
  if (source.kind === 'windTrial') return [{
    label: 'RIDE THE GALE',
    detail: 'Lose 3 HP; gain 100 cash and one combat Boon rank.',
    available: state.hero.health > 5
  }, {
    label: 'BIND THE GALE',
    detail: 'Spend 3 focus; gain a grappling line.',
    available: state.hero.focus >= 3
  }, {
    label: 'LEAVE',
    detail: 'Keep your footing.',
    available: true
  }];
  if (source.kind === 'ancestorDebt') return [{
    label: 'PAY VITALITY',
    detail: 'Lose 3 maximum HP; gain 90 cash and a spirit item.',
    available: state.hero.maxHealth > 8
  }, {
    label: 'PAY AN OATH',
    detail: 'No healing for two floors; gain 110 cash.',
    available: ((_state$hero$oaths$len = (_state$hero$oaths = state.hero.oaths) === null || _state$hero$oaths === void 0 ? void 0 : _state$hero$oaths.length) !== null && _state$hero$oaths$len !== void 0 ? _state$hero$oaths$len : 0) < 3
  }, {
    label: 'LEAVE',
    detail: 'Leave the debt unsettled.',
    available: true
  }];
  if (source.kind === 'tombAuction') return [{
    label: 'BUY RELIC',
    detail: `Spend ${contextualCost(state, 55)} cash for a grave relic and 1 Boon rank.`,
    available: state.hero.gold >= contextualCost(state, 55)
  }, {
    label: 'SELL BLOOD',
    detail: 'Lose 5 HP for 85 cash.',
    available: state.hero.health > 6
  }, {
    label: 'LEAVE',
    detail: 'Do not bid.',
    available: true
  }];
  if (source.kind === 'oathwell') return [{
    label: 'SWEAR',
    detail: 'Take a two-floor no-charms oath for 90 cash and 1 Boon rank.',
    available: ((_state$hero$oaths$len2 = (_state$hero$oaths2 = state.hero.oaths) === null || _state$hero$oaths2 === void 0 ? void 0 : _state$hero$oaths2.length) !== null && _state$hero$oaths$len2 !== void 0 ? _state$hero$oaths$len2 : 0) < 3
  }, {
    label: 'SCOUR WELL',
    detail: 'Spend 2 focus to remove nearby hazards.',
    available: state.hero.focus >= 2
  }, {
    label: 'LEAVE',
    detail: 'Leave the well untouched.',
    available: true
  }];
  if (source.kind === 'cursedObject') return [{
    label: 'TAKE MIRROR',
    detail: 'Rare lethal: take no damage across the next 2 encounters; gain 180 cash.',
    available: !state.hero.curse
  }, {
    label: 'TAKE FLEECE',
    detail: 'Severe: take no damage across the next 2 encounters; gain 110 cash.',
    available: !state.hero.curse
  }, {
    label: 'TAKE IDOL',
    detail: 'Severe: take no damage across the next 2 encounters; gain 125 cash.',
    available: !state.hero.curse
  }, {
    label: 'TAKE SHARD',
    detail: 'Severe: take no damage across the next 2 encounters; gain 125 cash.',
    available: !state.hero.curse
  }, {
    label: 'LEAVE',
    detail: 'Leave the object alone.',
    available: true
  }];
  if (source.kind === 'wayfarer') return [{
    label: 'TRADE',
    detail: `${contextualCost(state, 35)} cash for ${ITEM[rewardFor(state, source)].name}.`,
    available: state.hero.gold >= contextualCost(state, 35)
  }, {
    label: 'ASK ROUTE',
    detail: 'Reveal the remaining floor; no payment.',
    available: true
  }, {
    label: 'LEAVE',
    detail: 'Keep moving.',
    available: true
  }];
  if (source.kind === 'bloodBargain') return [{
    label: 'GIVE VITALITY',
    detail: 'Lose 4 max HP for 75 cash and traversal gear.',
    available: state.hero.maxHealth > 8
  }, {
    label: 'GIVE FOCUS',
    detail: 'Lose 3 focus for 30 cash and a revealed floor.',
    available: state.hero.focus >= 3
  }, {
    label: 'DECLINE',
    detail: 'Leave the bargain untouched.',
    available: true
  }];
  return [{
    label: 'OPEN CHAMBER',
    detail: 'Spend 2 focus to flatten nearby hazards and blockers.',
    available: state.hero.focus >= 2
  }, {
    label: 'SURGE CHAMBER',
    detail: 'Gain 90 cash; nearby floor becomes current.',
    available: true
  }, {
    label: 'LEAVE',
    detail: 'Do not disturb the chamber.',
    available: true
  }];
};
export const openEncounter = state => {
  const source = encounterAtReach(state);
  if (!source) return undefined;
  state.modal = {
    kind: 'encounter',
    encounterId: source.id
  };
  const expansion = expansionProfileFor(source.kind);
  const alignment = alignmentProfileFor(source.kind);
  log(state, source.social ? `A ${source.social.role} signals: ${source.social.goal}.` : alignment ? `${alignment.title} waits for your answer.` : expansion ? `${expansion.title} presents a dangerous offer.` : source.kind === 'wayfarer' ? 'A stranded specialist calls from the landing zone.' : source.kind === 'bloodBargain' ? 'A sealed protocol waits for an answer.' : source.kind === 'cursedObject' ? 'A corrupted object hums beside the route.' : source.kind === 'stormCache' || source.kind === 'windTrial' ? 'A vector anomaly presents a dangerous offer.' : source.kind === 'ancestorDebt' || source.kind === 'tombAuction' ? 'An archive offers a price.' : source.kind === 'oathwell' ? 'A protocol well requests confirmation.' : 'The chamber walls grind, awaiting a command.');
  return [event('encounter'), event('menu')];
};
const chamberCells = (state, source, radius) => state.floor.tiles.flatMap((tile, index) => {
  const x = index % 48;
  const y = Math.floor(index / 48);
  return Math.max(Math.abs(x - source.x), Math.abs(y - source.y)) <= radius ? [{
    tile,
    x,
    y
  }] : [];
}).filter(cell => !(cell.x === state.hero.x && cell.y === state.hero.y) && !(cell.x === state.floor.start.x && cell.y === state.floor.start.y) && !(cell.x === state.floor.exit.x && cell.y === state.floor.exit.y));
const resolveExpansionEncounter = (state, source, index) => {
  const profile = expansionProfileFor(source.kind);
  if (!profile) return undefined;
  if (index === 0) {
    var _state$hero, _state$hero$boons, _state$hero$boons$pro;
    if (profile.cost === 'health') state.hero.health -= profile.value;
    if (profile.cost === 'maxHealth') {
      state.hero.maxHealth -= profile.value;
      state.hero.health = Math.min(state.hero.health, state.hero.maxHealth);
    }
    if (profile.cost === 'focus') state.hero.focus -= profile.value;
    if (profile.cost === 'item') state.hero.inventory.splice(state.hero.inventory.indexOf(profile.item), 1);
    (_state$hero$boons = (_state$hero = state.hero).boons) !== null && _state$hero$boons !== void 0 ? _state$hero$boons : _state$hero.boons = {};
    state.hero.boons[profile.boon] = ((_state$hero$boons$pro = state.hero.boons[profile.boon]) !== null && _state$hero$boons$pro !== void 0 ? _state$hero$boons$pro : 0) + 1;
    grantContextGold(state, profile.gold);
    if (profile.reward) grantItem(state, profile.reward);
    log(state, `${profile.title} grants ${profile.boon}.`);
    return resolve(state, source, 'offer', advance(state, [event('pickup')]));
  }
  if (index === 1) {
    if (profile.risk === 'curse') {
      state.hero.curse = {
        itemId: 'cursedMirror',
        name: ITEM.cursedMirror.name,
        condition: 'Take no damage before resolving two encounters.',
        remainingEncounters: 2,
        lethal: false
      };
      grantItem(state, 'cursedMirror');
      grantContextGold(state, 150);
      return resolve(state, source, 'curse', [event('pickup')]);
    }
    if (profile.risk === 'terrain') {
      const cells = chamberCells(state, source, 2).filter(cell => cell.tile.kind === 'floor').slice(0, 5);
      cells.forEach(cell => {
        cell.tile.kind = profile.terrain;
      });
      grantContextGold(state, 90);
      refreshFov(state);
      return resolve(state, source, 'terrain', advance(state, [event('danger')]));
    }
    if (profile.risk === 'map') {
      state.floor.tiles.forEach(tile => {
        tile.explored = true;
      });
      state.hero.focus = Math.min(state.hero.maxFocus, state.hero.focus + 2);
      refreshFov(state);
      return resolve(state, source, 'map', [event('menu')]);
    }
    addCondition(state.hero, {
      kind: 'shielded',
      duration: 3,
      potency: 2
    });
    grantContextGold(state, 70);
    return resolve(state, source, 'ward', [event('spell')]);
  }
  return resolve(state, source, 'leave', [event('menu')]);
};
const resolveAlignmentEncounter = (state, source, index) => {
  const profile = alignmentProfileFor(source.kind);
  if (!profile) return undefined;
  if (index === 0) {
    if (profile.cost === 'health') state.hero.health -= profile.value;
    if (profile.cost === 'focus') state.hero.focus -= profile.value;
    if (profile.cost === 'cash') state.hero.gold -= profile.value;
    grantContextGold(state, profile.gold);
    grantItem(state, profile.reward);
    log(state, `${profile.title} gives ${ITEM[profile.reward].name}.`);
    tend(state, profile.alignment);
    return resolve(state, source, 'pledge', advance(state, [event('pickup')]));
  }
  if (index === 1) {
    state.hero.health -= 2;
    state.floor.tiles.forEach(tile => {
      tile.explored = true;
    });
    grantItem(state, profile.reward);
    refreshFov(state);
    log(state, `${profile.title} reveals the route.`);
    tend(state, profile.alignment);
    return resolve(state, source, 'vigil', advance(state, [event('menu')]));
  }
  return resolve(state, source, 'leave', [event('menu')]);
};
const resolveSocialEncounter = (state, source, index) => {
  const social = source.social;
  if (!social) return undefined;
  if (index === 0) {
    source.social = {
      ...social,
      disposition: 'allied'
    };
    state.reputation = adjustSocialReputation(state.reputation, social.faction, 1);
    applySocialOffer(state, source);
    if (source.toolOffer) bindOptionalTool(state, source.toolOffer);
    log(state, `${social.faction} marks a route for you.`);
    return resolve(state, source, 'allied', advance(state, [event('pickup')]));
  }
  if (index === 1) {
    source.social = {
      ...social,
      disposition: 'hostile'
    };
    state.reputation = adjustSocialReputation(state.reputation, social.faction, -1);
    socialHostile(state, source);
    log(state, `${social.faction} turns hostile over the route.`);
    return resolve(state, source, 'hostile', advance(state, [event('encounter')]));
  }
  return resolve(state, source, 'leave', [event('menu')]);
};
export const chooseEncounter = (state, encounterId, command) => {
  const source = encounter(state, encounterId);
  if (!source) return [];
  const index = Number(command) - 1;
  const option = encounterOptions(state, source)[index];
  if (!option) return [];
  if (!option.available) {
    log(state, 'You cannot meet that cost.');
    return [event('menu')];
  }
  const social = resolveSocialEncounter(state, source, index);
  if (social) return social;
  if (source.toolOffer) {
    if (index === 0) {
      bindOptionalTool(state, source.toolOffer);
      return resolve(state, source, 'tool', advance(state, [event('pickup')]));
    }
    if (index === 1) {
      state.floor.tiles.forEach(tile => {
        tile.explored = true;
      });
      refreshFov(state);
      return resolve(state, source, 'route', [event('menu')]);
    }
    return resolve(state, source, 'leave', [event('menu')]);
  }
  const alignment = resolveAlignmentEncounter(state, source, index);
  if (alignment) return alignment;
  const expansion = resolveExpansionEncounter(state, source, index);
  if (expansion) return expansion;
  if (source.kind === 'stormCache') {
    if (index === 0) {
      var _state$hero2, _state$hero2$boons, _state$hero$boons$gal;
      grantContextGold(state, 60);
      grantItem(state, 'cliffSpool');
      (_state$hero2$boons = (_state$hero2 = state.hero).boons) !== null && _state$hero2$boons !== void 0 ? _state$hero2$boons : _state$hero2.boons = {};
      state.hero.boons.galeThread = ((_state$hero$boons$gal = state.hero.boons.galeThread) !== null && _state$hero$boons$gal !== void 0 ? _state$hero$boons$gal : 0) + 1;
      return resolve(state, source, 'open', advance(state, [event('pickup')]));
    }
    if (index === 1) {
      state.floor.tiles.forEach(tile => {
        tile.explored = true;
      });
      state.hero.focus = Math.min(state.hero.maxFocus, state.hero.focus + 2);
      refreshFov(state);
      return resolve(state, source, 'map', [event('menu')]);
    }
    return resolve(state, source, 'leave', [event('menu')]);
  }
  if (source.kind === 'windTrial') {
    if (index === 0) {
      var _state$hero3, _state$hero3$boons, _state$hero$boons$sky;
      state.hero.health -= 3;
      grantContextGold(state, 100);
      (_state$hero3$boons = (_state$hero3 = state.hero).boons) !== null && _state$hero3$boons !== void 0 ? _state$hero3$boons : _state$hero3.boons = {};
      state.hero.boons.skyhookReprisal = ((_state$hero$boons$sky = state.hero.boons.skyhookReprisal) !== null && _state$hero$boons$sky !== void 0 ? _state$hero$boons$sky : 0) + 1;
      return resolve(state, source, 'ride', advance(state, [event('hurt')]));
    }
    if (index === 1) {
      state.hero.focus -= 3;
      grantItem(state, 'grappleLine');
      return resolve(state, source, 'bind', advance(state, [event('pickup')]));
    }
    return resolve(state, source, 'leave', [event('menu')]);
  }
  if (source.kind === 'ancestorDebt') {
    if (index === 0) {
      state.hero.maxHealth -= 3;
      state.hero.health = Math.min(state.hero.health, state.hero.maxHealth);
      grantContextGold(state, 90);
      grantItem(state, 'ancestorToken');
      return resolve(state, source, 'vitality', advance(state, [event('pickup')]));
    }
    if (index === 1) {
      var _state$hero$oaths3;
      state.hero.oaths = [...((_state$hero$oaths3 = state.hero.oaths) !== null && _state$hero$oaths3 !== void 0 ? _state$hero$oaths3 : []), {
        id: 'noHealing',
        remainingFloors: 2
      }];
      grantContextGold(state, 110);
      return resolve(state, source, 'oath', [event('encounter')]);
    }
    return resolve(state, source, 'leave', [event('menu')]);
  }
  if (source.kind === 'tombAuction') {
    if (index === 0) {
      var _state$hero4, _state$hero4$boons, _state$hero$boons$gra;
      state.hero.gold -= contextualCost(state, 55);
      grantItem(state, 'mourningBell');
      (_state$hero4$boons = (_state$hero4 = state.hero).boons) !== null && _state$hero4$boons !== void 0 ? _state$hero4$boons : _state$hero4.boons = {};
      state.hero.boons.graveLedger = ((_state$hero$boons$gra = state.hero.boons.graveLedger) !== null && _state$hero$boons$gra !== void 0 ? _state$hero$boons$gra : 0) + 1;
      return resolve(state, source, 'buy', advance(state, [event('pickup')]));
    }
    if (index === 1) {
      state.hero.health -= 5;
      grantContextGold(state, 85);
      return resolve(state, source, 'blood', advance(state, [event('hurt')]));
    }
    return resolve(state, source, 'leave', [event('menu')]);
  }
  if (source.kind === 'oathwell') {
    if (index === 0) {
      var _state$hero$oaths4, _state$hero5, _state$hero5$boons, _state$hero$boons$bri;
      state.hero.oaths = [...((_state$hero$oaths4 = state.hero.oaths) !== null && _state$hero$oaths4 !== void 0 ? _state$hero$oaths4 : []), {
        id: 'noCharms',
        remainingFloors: 2
      }];
      grantContextGold(state, 90);
      (_state$hero5$boons = (_state$hero5 = state.hero).boons) !== null && _state$hero5$boons !== void 0 ? _state$hero5$boons : _state$hero5.boons = {};
      state.hero.boons.bridgeOfNames = ((_state$hero$boons$bri = state.hero.boons.bridgeOfNames) !== null && _state$hero$boons$bri !== void 0 ? _state$hero$boons$bri : 0) + 1;
      return resolve(state, source, 'swear', [event('encounter')]);
    }
    if (index === 1) {
      state.hero.focus -= 2;
      const cells = chamberCells(state, source, 2).filter(cell => ['rubble', 'bramble', 'boulder', 'pit', 'water', 'deepWater', 'current', 'gas', 'smoke', 'fireVent'].includes(cell.tile.kind));
      cells.forEach(cell => {
        cell.tile.kind = 'floor';
      });
      return resolve(state, source, 'scour', advance(state, [event('spell')]));
    }
    return resolve(state, source, 'leave', [event('menu')]);
  }
  if (source.kind === 'cursedObject') {
    if (index < 4) {
      const lethal = index === 0;
      const itemId = ['cursedMirror', 'graveFleece', 'stormIdol', 'oathShard'][index];
      state.hero.curse = {
        itemId,
        name: ITEM[itemId].name,
        condition: 'Take no damage before resolving two encounters.',
        remainingEncounters: 2,
        lethal
      };
      grantItem(state, itemId);
      grantContextGold(state, lethal ? 180 : itemId === 'graveFleece' ? 110 : 125);
      return resolve(state, source, itemId, [event('pickup')]);
    }
    return resolve(state, source, 'leave', [event('menu')]);
  }
  if (source.kind === 'wayfarer') {
    if (index === 0) {
      state.hero.gold -= contextualCost(state, 35);
      const reward = rewardFor(state, source);
      grantItem(state, reward);
      log(state, `The wayfarer trades ${ITEM[reward].name} for your cash.`);
      return resolve(state, source, 'trade', advance(state, [event('pickup')]));
    }
    if (index === 1) {
      state.floor.tiles.forEach(tile => {
        tile.explored = true;
      });
      refreshFov(state);
      log(state, 'The wayfarer maps the side routes.');
      return resolve(state, source, 'route', [event('menu')]);
    }
    log(state, 'The wayfarer fades back into the side trail.');
    return resolve(state, source, 'leave', [event('menu')]);
  }
  if (source.kind === 'bloodBargain') {
    if (index === 0) {
      state.hero.maxHealth -= 4;
      state.hero.health = Math.min(state.hero.health, state.hero.maxHealth);
      grantContextGold(state, 75);
      const reward = rewardFor(state, source);
      grantItem(state, reward);
      log(state, `The bargain takes vitality and leaves ${ITEM[reward].name}.`);
      return resolve(state, source, 'vitality', advance(state, [event('pickup')]));
    }
    if (index === 1) {
      state.hero.focus -= 3;
      grantContextGold(state, 30);
      state.floor.tiles.forEach(tile => {
        tile.explored = true;
      });
      refreshFov(state);
      log(state, 'The bargain drinks focus and exposes the trail.');
      return resolve(state, source, 'focus', advance(state, [event('spell')]));
    }
    log(state, 'The sealed bargain remains unopened.');
    return resolve(state, source, 'decline', [event('menu')]);
  }
  if (index === 0) {
    state.hero.focus -= 2;
    const mutable = new Set(['rubble', 'bramble', 'boulder', 'breakwall', 'pit', 'water', 'deepWater', 'current', 'gas', 'smoke', 'fireVent']);
    const cells = chamberCells(state, source, 2).filter(cell => mutable.has(cell.tile.kind));
    cells.forEach(cell => {
      cell.tile.kind = 'floor';
    });
    refreshFov(state);
    log(state, `The chamber opens ${cells.length} nearby cells.`);
    return resolve(state, source, 'open', advance(state, [event('spell')]));
  }
  if (index === 1) {
    const cells = chamberCells(state, source, 2).filter(cell => cell.tile.kind === 'floor').slice(0, 5);
    cells.forEach(cell => {
      cell.tile.kind = 'current';
    });
    grantContextGold(state, 90);
    refreshFov(state);
    log(state, `The chamber surges through ${cells.length} nearby cells.`);
    return resolve(state, source, 'surge', advance(state, [event('spell')]));
  }
  log(state, 'The chamber settles without changing its shape.');
  return resolve(state, source, 'leave', [event('menu')]);
};
//# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJuYW1lcyI6WyJJVEVNIiwicmVjb3JkVGVsZW1ldHJ5Q291bnQiLCJhZHZhbmNlIiwiZ3JhbnRHb2xkIiwiZXZlbnQiLCJsb2ciLCJyZWZyZXNoRm92IiwiYWNxdWlyZU9wdGlvbmFsVHJhdmVyc2FsVG9vbCIsImJvb25SYW5rIiwidG9vbEZvciIsInNldHRsZUN1cnNlQWZ0ZXJFbmNvdW50ZXIiLCJhZGRDb25kaXRpb24iLCJ0ZW5kIiwiYWRqdXN0U29jaWFsUmVwdXRhdGlvbiIsInNvY2lhbERpc3Bvc2l0aW9uRm9yIiwiZXhwYW5zaW9uRW5jb3VudGVyS2luZHMiLCJleHBhbnNpb25Qcm9maWxlcyIsInN1blRyaWJ1dGUiLCJ0aXRsZSIsImNvc3QiLCJ2YWx1ZSIsImJvb24iLCJyZXdhcmQiLCJnb2xkIiwicmlzayIsInRlcnJhaW4iLCJtaXJhZ2VNYXJrZXQiLCJicmluZU9hdGgiLCJnbGFzc1RyaWFsIiwiaXRlbSIsIndoaXRlUm9hZCIsInNhbHRDYWNoZSIsImljZUR1ZWwiLCJ3aW50ZXJUaXRoZSIsInJpbWVDb250cmFjdCIsImZyb3N0Q2FjaGUiLCJ3aGl0ZW91dCIsInJlbGlxdWFyeVRyaWFsIiwiZXhwYW5zaW9uUHJvZmlsZUZvciIsImtpbmQiLCJpbmNsdWRlcyIsInVuZGVmaW5lZCIsImFsaWdubWVudEVuY291bnRlcktpbmRzIiwiYWxpZ25tZW50UHJvZmlsZXMiLCJtaW5lUGFjdCIsImFsaWdubWVudCIsIm1pbmVLYW1pIiwid2lsZHNQYWN0Iiwid2lsZHNLYW1pIiwiY2F2ZXJuc1BhY3QiLCJjYXZlcm5zS2FtaSIsInJ1aW5zUGFjdCIsInJ1aW5zS2FtaSIsImZ1cm5hY2VQYWN0IiwiZnVybmFjZUthbWkiLCJmbG9vZGVkUGFjdCIsImZsb29kZWRLYW1pIiwiY2xpZmZzUGFjdCIsImNsaWZmc0thbWkiLCJidXJpYWxQYWN0IiwiYnVyaWFsS2FtaSIsInNhbHRQYWN0Iiwic2FsdEthbWkiLCJmcm9zdFBhY3QiLCJmcm9zdEthbWkiLCJhbGlnbm1lbnRQcm9maWxlRm9yIiwiZXhpc3RpbmdFbmNvdW50ZXJBbGlnbm1lbnQiLCJ3YXlmYXJlciIsImJsb29kQmFyZ2FpbiIsInN0b3JtQ2FjaGUiLCJ3aW5kVHJpYWwiLCJhbmNlc3RvckRlYnQiLCJ0b21iQXVjdGlvbiIsIm9hdGh3ZWxsIiwiY3Vyc2VkT2JqZWN0IiwiZW5jb3VudGVyVGl0bGUiLCJfYWxpZ25tZW50UHJvZmlsZUZvciQiLCJfYWxpZ25tZW50UHJvZmlsZUZvciIsIl9leHBhbnNpb25Qcm9maWxlRm9yIiwidHJhdmVyc2FsUmV3YXJkcyIsImVuY291bnRlckF0UmVhY2giLCJzdGF0ZSIsIl9zdGF0ZSRmbG9vciRlbmNvdW50ZSIsImZsb29yIiwiZW5jb3VudGVycyIsImZpbmQiLCJlbmNvdW50ZXIiLCJNYXRoIiwibWF4IiwiYWJzIiwieCIsImhlcm8iLCJ5IiwiaWQiLCJfc3RhdGUkZmxvb3IkZW5jb3VudGUyIiwiY3VycmVudCIsInJld2FyZEZvciIsInNvdXJjZSIsInNlZWQiLCJpbmRleCIsImxlbmd0aCIsImdyYW50SXRlbSIsImludmVudG9yeSIsInB1c2giLCJpdGVtcyIsImNvdW50IiwidmlzaWJsZUluRm9nIiwiZ3JhbnRDb250ZXh0R29sZCIsImFtb3VudCIsIl9zdGF0ZSRmbG9vciRkaWZmaWN1bCIsIl9zdGF0ZSRmbG9vciRkaWZmaWN1bDIiLCJkaWZmaWN1bHR5IiwidGhyZWF0IiwiY29udGV4dHVhbENvc3QiLCJfc3RhdGUkZmxvb3IkZGlmZmljdWwzIiwiX3N0YXRlJGZsb29yJGRpZmZpY3VsNCIsImJpbmRPcHRpb25hbFRvb2wiLCJ0b29sIiwiYWNxdWlyZWQiLCJyZXN1bHQiLCJuYW1lIiwicmVwbGFjZWQiLCJ0b29sT2ZmZXJEZXRhaWwiLCJzb2NpYWxPZmZlckRldGFpbCIsIl9zb3VyY2Ukc29jaWFsIiwiX3NvdXJjZSRzb2NpYWwyIiwic29jaWFsIiwib2ZmZXIiLCJhcHBseVNvY2lhbE9mZmVyIiwiX3NvdXJjZSRzb2NpYWwzIiwiX3NvdXJjZSRzb2NpYWw0Iiwic2hvcnRjdXQiLCJ0aWxlcyIsInRpbGUiLCJmb3JFYWNoIiwiZXhwbG9yZWQiLCJzb2NpYWxIb3N0aWxlIiwiYWN0b3IiLCJyb2xlIiwiZmFjdGlvbiIsImhlYWx0aCIsIm1heEhlYWx0aCIsImF0dGFjayIsImRlZmVuc2UiLCJzcGVlZCIsImVuZXJneSIsImdseXBoIiwiY29sb3IiLCJob3N0aWxlIiwiYWkiLCJ0YWdzIiwic3RhdHVzIiwiYWN0b3JzIiwicmVzb2x2ZSIsIm91dGNvbWUiLCJldmVudHMiLCJfc3RhdGUkaGVybyRjb25kaXRpb24iLCJfc3RhdGUkaGVybyRjb25kaXRpb24yIiwibW9kYWwiLCJkaXZpZGVuZCIsIndhcmQiLCJjb25kaXRpb25zIiwiZHVyYXRpb24iLCJwb3RlbmN5IiwicmVsaXF1YXJ5RWNobyIsImVuY291bnRlck9wdGlvbnMiLCJfc3RhdGUkaGVybyRvYXRocyRsZW4iLCJfc3RhdGUkaGVybyRvYXRocyIsIl9zdGF0ZSRoZXJvJG9hdGhzJGxlbjIiLCJfc3RhdGUkaGVybyRvYXRoczIiLCJkaXNwb3NpdGlvbiIsInJlcHV0YXRpb24iLCJhbGx5IiwibGFiZWwiLCJkZXRhaWwiLCJnb2FsIiwidG9vbE9mZmVyIiwiYXZhaWxhYmxlIiwiZm9jdXMiLCJleHBhbnNpb24iLCJwcmltYXJ5IiwiY3Vyc2UiLCJvYXRocyIsIm9wZW5FbmNvdW50ZXIiLCJlbmNvdW50ZXJJZCIsImNoYW1iZXJDZWxscyIsInJhZGl1cyIsImZsYXRNYXAiLCJmaWx0ZXIiLCJjZWxsIiwic3RhcnQiLCJleGl0IiwicmVzb2x2ZUV4cGFuc2lvbkVuY291bnRlciIsInByb2ZpbGUiLCJfc3RhdGUkaGVybyIsIl9zdGF0ZSRoZXJvJGJvb25zIiwiX3N0YXRlJGhlcm8kYm9vbnMkcHJvIiwibWluIiwic3BsaWNlIiwiaW5kZXhPZiIsImJvb25zIiwiaXRlbUlkIiwiY3Vyc2VkTWlycm9yIiwiY29uZGl0aW9uIiwicmVtYWluaW5nRW5jb3VudGVycyIsImxldGhhbCIsImNlbGxzIiwic2xpY2UiLCJtYXhGb2N1cyIsInJlc29sdmVBbGlnbm1lbnRFbmNvdW50ZXIiLCJyZXNvbHZlU29jaWFsRW5jb3VudGVyIiwiY2hvb3NlRW5jb3VudGVyIiwiY29tbWFuZCIsIk51bWJlciIsIm9wdGlvbiIsIl9zdGF0ZSRoZXJvMiIsIl9zdGF0ZSRoZXJvMiRib29ucyIsIl9zdGF0ZSRoZXJvJGJvb25zJGdhbCIsImdhbGVUaHJlYWQiLCJfc3RhdGUkaGVybzMiLCJfc3RhdGUkaGVybzMkYm9vbnMiLCJfc3RhdGUkaGVybyRib29ucyRza3kiLCJza3lob29rUmVwcmlzYWwiLCJfc3RhdGUkaGVybyRvYXRoczMiLCJyZW1haW5pbmdGbG9vcnMiLCJfc3RhdGUkaGVybzQiLCJfc3RhdGUkaGVybzQkYm9vbnMiLCJfc3RhdGUkaGVybyRib29ucyRncmEiLCJncmF2ZUxlZGdlciIsIl9zdGF0ZSRoZXJvJG9hdGhzNCIsIl9zdGF0ZSRoZXJvNSIsIl9zdGF0ZSRoZXJvNSRib29ucyIsIl9zdGF0ZSRoZXJvJGJvb25zJGJyaSIsImJyaWRnZU9mTmFtZXMiLCJtdXRhYmxlIiwiU2V0IiwiaGFzIl0sInNvdXJjZXMiOlsiZW5jb3VudGVycy50cyJdLCJzb3VyY2VzQ29udGVudCI6WyJpbXBvcnQgeyBJVEVNIH0gZnJvbSAnLi4vY29udGVudCdcbmltcG9ydCB0eXBlIHsgQWN0b3IsIEFsaWdubWVudCwgRmxvb3JFbmNvdW50ZXIsIEl0ZW1JZCwgUnVuU3RhdGUgfSBmcm9tICcuLi90eXBlcydcbmltcG9ydCB7IHJlY29yZFRlbGVtZXRyeUNvdW50IH0gZnJvbSAnLi4vdGVsZW1ldHJ5J1xuaW1wb3J0IHsgYWR2YW5jZSB9IGZyb20gJy4vY29tYmF0J1xuaW1wb3J0IHsgZ3JhbnRHb2xkIH0gZnJvbSAnLi9lY29ub215J1xuaW1wb3J0IHsgZXZlbnQsIGxvZywgdHlwZSBBY3Rpb25SZXN1bHQgfSBmcm9tICcuL3NoYXJlZCdcbmltcG9ydCB7IHJlZnJlc2hGb3YgfSBmcm9tICcuL3Zpc2liaWxpdHknXG5pbXBvcnQgeyBhY3F1aXJlT3B0aW9uYWxUcmF2ZXJzYWxUb29sLCBib29uUmFuaywgdG9vbEZvciB9IGZyb20gJy4vYnVpbGRjcmFmdCdcbmltcG9ydCB7IHNldHRsZUN1cnNlQWZ0ZXJFbmNvdW50ZXIgfSBmcm9tICcuL2N1cnNlcydcbmltcG9ydCB7IGFkZENvbmRpdGlvbiB9IGZyb20gJy4vY29uZGl0aW9ucydcbmltcG9ydCB7IHRlbmQgfSBmcm9tICcuL2FsaWdubWVudCdcbmltcG9ydCB7IGFkanVzdFNvY2lhbFJlcHV0YXRpb24sIHNvY2lhbERpc3Bvc2l0aW9uRm9yIH0gZnJvbSAnLi4vc29jaWFsLWNvbnRyYWN0J1xuXG5leHBvcnQgaW50ZXJmYWNlIEVuY291bnRlck9wdGlvbiB7IGxhYmVsOiBzdHJpbmc7IGRldGFpbDogc3RyaW5nOyBhdmFpbGFibGU6IGJvb2xlYW4gfVxuXG5jb25zdCBleHBhbnNpb25FbmNvdW50ZXJLaW5kcyA9IFsnc3VuVHJpYnV0ZScsICdtaXJhZ2VNYXJrZXQnLCAnYnJpbmVPYXRoJywgJ2dsYXNzVHJpYWwnLCAnd2hpdGVSb2FkJywgJ3NhbHRDYWNoZScsICdpY2VEdWVsJywgJ3dpbnRlclRpdGhlJywgJ3JpbWVDb250cmFjdCcsICdmcm9zdENhY2hlJywgJ3doaXRlb3V0JywgJ3JlbGlxdWFyeVRyaWFsJ10gYXMgY29uc3RcbnR5cGUgRXhwYW5zaW9uRW5jb3VudGVyS2luZCA9IHR5cGVvZiBleHBhbnNpb25FbmNvdW50ZXJLaW5kc1tudW1iZXJdXG50eXBlIEV4cGFuc2lvbkNvc3QgPSAnaGVhbHRoJyB8ICdtYXhIZWFsdGgnIHwgJ2ZvY3VzJyB8ICdpdGVtJ1xudHlwZSBFeHBhbnNpb25SaXNrID0gJ2N1cnNlJyB8ICd0ZXJyYWluJyB8ICdtYXAnIHwgJ3NoaWVsZCdcbmludGVyZmFjZSBFeHBhbnNpb25Qcm9maWxlIHsgdGl0bGU6IHN0cmluZzsgY29zdDogRXhwYW5zaW9uQ29zdDsgdmFsdWU6IG51bWJlcjsgaXRlbT86IEl0ZW1JZDsgYm9vbjogc3RyaW5nOyByZXdhcmQ/OiBJdGVtSWQ7IGdvbGQ6IG51bWJlcjsgcmlzazogRXhwYW5zaW9uUmlzazsgdGVycmFpbjogJ2JyaW5lJyB8ICdmcm9zdFJpbWUnIH1cbmNvbnN0IGV4cGFuc2lvblByb2ZpbGVzOiBSZWNvcmQ8RXhwYW5zaW9uRW5jb3VudGVyS2luZCwgRXhwYW5zaW9uUHJvZmlsZT4gPSB7XG4gIHN1blRyaWJ1dGU6IHsgdGl0bGU6ICdTVU4gVFJJQlVURScsIGNvc3Q6ICdoZWFsdGgnLCB2YWx1ZTogMywgYm9vbjogJ3N1bnN0ZXAnLCByZXdhcmQ6ICdmaXJlSmFyJywgZ29sZDogMTAwLCByaXNrOiAndGVycmFpbicsIHRlcnJhaW46ICdicmluZScgfSxcbiAgbWlyYWdlTWFya2V0OiB7IHRpdGxlOiAnTUlSQUdFIE1BUktFVCcsIGNvc3Q6ICdmb2N1cycsIHZhbHVlOiAzLCBib29uOiAnbWlyYWdlTWFwJywgcmV3YXJkOiAnYmxpbmsnLCBnb2xkOiA3NSwgcmlzazogJ21hcCcsIHRlcnJhaW46ICdicmluZScgfSxcbiAgYnJpbmVPYXRoOiB7IHRpdGxlOiAnQlJJTkUgT0FUSCcsIGNvc3Q6ICdtYXhIZWFsdGgnLCB2YWx1ZTogMywgYm9vbjogJ2JyaW5lV2FyZCcsIHJld2FyZDogJ2ZvY3VzVG9uaWMnLCBnb2xkOiAxMjAsIHJpc2s6ICdjdXJzZScsIHRlcnJhaW46ICdicmluZScgfSxcbiAgZ2xhc3NUcmlhbDogeyB0aXRsZTogJ0dMQVNTIFRSSUFMJywgY29zdDogJ2l0ZW0nLCB2YWx1ZTogMSwgaXRlbTogJ3RvbmljJywgYm9vbjogJ21pcnJvckh1bnQnLCByZXdhcmQ6ICdzaWdodCcsIGdvbGQ6IDk1LCByaXNrOiAndGVycmFpbicsIHRlcnJhaW46ICdicmluZScgfSxcbiAgd2hpdGVSb2FkOiB7IHRpdGxlOiAnV0hJVEUgUk9BRCcsIGNvc3Q6ICdoZWFsdGgnLCB2YWx1ZTogNCwgYm9vbjogJ3doaXRlUm9hZCcsIHJld2FyZDogJ2JyaWRnZUtpdCcsIGdvbGQ6IDExNSwgcmlzazogJ3NoaWVsZCcsIHRlcnJhaW46ICdicmluZScgfSxcbiAgc2FsdENhY2hlOiB7IHRpdGxlOiAnU0FMVCBDQUNIRScsIGNvc3Q6ICdmb2N1cycsIHZhbHVlOiAyLCBib29uOiAnc2FsdExlZGdlcicsIHJld2FyZDogJ21hcFNjcm9sbCcsIGdvbGQ6IDkwLCByaXNrOiAnY3Vyc2UnLCB0ZXJyYWluOiAnYnJpbmUnIH0sXG4gIGljZUR1ZWw6IHsgdGl0bGU6ICdJQ0UgRFVFTCcsIGNvc3Q6ICdoZWFsdGgnLCB2YWx1ZTogNCwgYm9vbjogJ2R1ZWxpc3RPYXRoJywgcmV3YXJkOiAnd2FyZCcsIGdvbGQ6IDEyMCwgcmlzazogJ3NoaWVsZCcsIHRlcnJhaW46ICdmcm9zdFJpbWUnIH0sXG4gIHdpbnRlclRpdGhlOiB7IHRpdGxlOiAnV0lOVEVSIFRJVEhFJywgY29zdDogJ21heEhlYWx0aCcsIHZhbHVlOiAzLCBib29uOiAnd2ludGVyUmF0aW9ucycsIHJld2FyZDogJ21lbmQnLCBnb2xkOiAxMjUsIHJpc2s6ICdjdXJzZScsIHRlcnJhaW46ICdmcm9zdFJpbWUnIH0sXG4gIHJpbWVDb250cmFjdDogeyB0aXRsZTogJ1JJTUUgQ09OVFJBQ1QnLCBjb3N0OiAnaXRlbScsIHZhbHVlOiAxLCBpdGVtOiAnZm9jdXNUb25pYycsIGJvb246ICdzaGF0dGVyTWFyaycsIHJld2FyZDogJ2dyYXBwbGVMaW5lJywgZ29sZDogMTA1LCByaXNrOiAndGVycmFpbicsIHRlcnJhaW46ICdmcm9zdFJpbWUnIH0sXG4gIGZyb3N0Q2FjaGU6IHsgdGl0bGU6ICdGUk9TVCBDQUNIRScsIGNvc3Q6ICdmb2N1cycsIHZhbHVlOiAzLCBib29uOiAnY29sZFJlYWQnLCByZXdhcmQ6ICdzaWdodCcsIGdvbGQ6IDkwLCByaXNrOiAnbWFwJywgdGVycmFpbjogJ2Zyb3N0UmltZScgfSxcbiAgd2hpdGVvdXQ6IHsgdGl0bGU6ICdXSElURU9VVCcsIGNvc3Q6ICdoZWFsdGgnLCB2YWx1ZTogMywgYm9vbjogJ3RoYXdTdGVwJywgcmV3YXJkOiAndG9uaWMnLCBnb2xkOiAxMTAsIHJpc2s6ICdjdXJzZScsIHRlcnJhaW46ICdmcm9zdFJpbWUnIH0sXG4gIHJlbGlxdWFyeVRyaWFsOiB7IHRpdGxlOiAnUkVMSVFVQVJZIFRSSUFMJywgY29zdDogJ2ZvY3VzJywgdmFsdWU6IDMsIGJvb246ICdyZWxpcXVhcnlFY2hvJywgcmV3YXJkOiAnd2FyZFNjcmlwdCcsIGdvbGQ6IDExMCwgcmlzazogJ3NoaWVsZCcsIHRlcnJhaW46ICdmcm9zdFJpbWUnIH1cbn1cbmNvbnN0IGV4cGFuc2lvblByb2ZpbGVGb3IgPSAoa2luZDogRmxvb3JFbmNvdW50ZXJbJ2tpbmQnXSk6IEV4cGFuc2lvblByb2ZpbGUgfCB1bmRlZmluZWQgPT4gZXhwYW5zaW9uRW5jb3VudGVyS2luZHMuaW5jbHVkZXMoa2luZCBhcyBFeHBhbnNpb25FbmNvdW50ZXJLaW5kKSA/IGV4cGFuc2lvblByb2ZpbGVzW2tpbmQgYXMgRXhwYW5zaW9uRW5jb3VudGVyS2luZF0gOiB1bmRlZmluZWRcblxuY29uc3QgYWxpZ25tZW50RW5jb3VudGVyS2luZHMgPSBbJ21pbmVQYWN0JywgJ21pbmVLYW1pJywgJ3dpbGRzUGFjdCcsICd3aWxkc0thbWknLCAnY2F2ZXJuc1BhY3QnLCAnY2F2ZXJuc0thbWknLCAncnVpbnNQYWN0JywgJ3J1aW5zS2FtaScsICdmdXJuYWNlUGFjdCcsICdmdXJuYWNlS2FtaScsICdmbG9vZGVkUGFjdCcsICdmbG9vZGVkS2FtaScsICdjbGlmZnNQYWN0JywgJ2NsaWZmc0thbWknLCAnYnVyaWFsUGFjdCcsICdidXJpYWxLYW1pJywgJ3NhbHRQYWN0JywgJ3NhbHRLYW1pJywgJ2Zyb3N0UGFjdCcsICdmcm9zdEthbWknXSBhcyBjb25zdFxudHlwZSBBbGlnbm1lbnRFbmNvdW50ZXJLaW5kID0gdHlwZW9mIGFsaWdubWVudEVuY291bnRlcktpbmRzW251bWJlcl1cbmludGVyZmFjZSBBbGlnbm1lbnRQcm9maWxlIHsgdGl0bGU6IHN0cmluZzsgYWxpZ25tZW50OiBBbGlnbm1lbnQ7IGNvc3Q6ICdoZWFsdGgnIHwgJ2ZvY3VzJyB8ICdjYXNoJzsgdmFsdWU6IG51bWJlcjsgcmV3YXJkOiBJdGVtSWQ7IGdvbGQ6IG51bWJlciB9XG5jb25zdCBhbGlnbm1lbnRQcm9maWxlczogUmVjb3JkPEFsaWdubWVudEVuY291bnRlcktpbmQsIEFsaWdubWVudFByb2ZpbGU+ID0ge1xuICBtaW5lUGFjdDogeyB0aXRsZTogJ0tFU1RSRUwgUFJBQ1RJQ0FMIFBMQU4nLCBhbGlnbm1lbnQ6ICd2aWxsYWdlUGFjdCcsIGNvc3Q6ICdjYXNoJywgdmFsdWU6IDM1LCByZXdhcmQ6ICdib21iUGFjaycsIGdvbGQ6IDcwIH0sXG4gIG1pbmVLYW1pOiB7IHRpdGxlOiAnS0VTVFJFTCBJREVBTElTVCBQTEFOJywgYWxpZ25tZW50OiAna2FtaScsIGNvc3Q6ICdmb2N1cycsIHZhbHVlOiAyLCByZXdhcmQ6ICd3YXJkJywgZ29sZDogNDUgfSxcbiAgd2lsZHNQYWN0OiB7IHRpdGxlOiAnVkVSREFOVCBQUkFDVElDQUwgUExBTicsIGFsaWdubWVudDogJ3ZpbGxhZ2VQYWN0JywgY29zdDogJ2Nhc2gnLCB2YWx1ZTogMzAsIHJld2FyZDogJ3JvcGVCdW5kbGUnLCBnb2xkOiA2NSB9LFxuICB3aWxkc0thbWk6IHsgdGl0bGU6ICdWRVJEQU5UIElERUFMSVNUIFBMQU4nLCBhbGlnbm1lbnQ6ICdrYW1pJywgY29zdDogJ2hlYWx0aCcsIHZhbHVlOiAyLCByZXdhcmQ6ICdzaWdodCcsIGdvbGQ6IDQ1IH0sXG4gIGNhdmVybnNQYWN0OiB7IHRpdGxlOiAnUEVMQUdPUyBQUkFDVElDQUwgUExBTicsIGFsaWdubWVudDogJ3ZpbGxhZ2VQYWN0JywgY29zdDogJ2ZvY3VzJywgdmFsdWU6IDIsIHJld2FyZDogJ2JyaWRnZUtpdCcsIGdvbGQ6IDYwIH0sXG4gIGNhdmVybnNLYW1pOiB7IHRpdGxlOiAnUEVMQUdPUyBJREVBTElTVCBQTEFOJywgYWxpZ25tZW50OiAna2FtaScsIGNvc3Q6ICdoZWFsdGgnLCB2YWx1ZTogMiwgcmV3YXJkOiAnZm9jdXNUb25pYycsIGdvbGQ6IDU1IH0sXG4gIHJ1aW5zUGFjdDogeyB0aXRsZTogJ09SSVNPTiBQUkFDVElDQUwgUExBTicsIGFsaWdubWVudDogJ3ZpbGxhZ2VQYWN0JywgY29zdDogJ2Nhc2gnLCB2YWx1ZTogNDAsIHJld2FyZDogJ21hcFNjcm9sbCcsIGdvbGQ6IDgwIH0sXG4gIHJ1aW5zS2FtaTogeyB0aXRsZTogJ09SSVNPTiBJREVBTElTVCBQTEFOJywgYWxpZ25tZW50OiAna2FtaScsIGNvc3Q6ICdmb2N1cycsIHZhbHVlOiAzLCByZXdhcmQ6ICd3YXJkU2NyaXB0JywgZ29sZDogNjUgfSxcbiAgZnVybmFjZVBhY3Q6IHsgdGl0bGU6ICdIRUxJT04gUFJBQ1RJQ0FMIFBMQU4nLCBhbGlnbm1lbnQ6ICd2aWxsYWdlUGFjdCcsIGNvc3Q6ICdoZWFsdGgnLCB2YWx1ZTogMywgcmV3YXJkOiAnZmlyZUphcicsIGdvbGQ6IDkwIH0sXG4gIGZ1cm5hY2VLYW1pOiB7IHRpdGxlOiAnSEVMSU9OIElERUFMSVNUIFBMQU4nLCBhbGlnbm1lbnQ6ICdrYW1pJywgY29zdDogJ2ZvY3VzJywgdmFsdWU6IDMsIHJld2FyZDogJ3RvbmljJywgZ29sZDogNzUgfSxcbiAgZmxvb2RlZFBhY3Q6IHsgdGl0bGU6ICdORVJJREEgUFJBQ1RJQ0FMIFBMQU4nLCBhbGlnbm1lbnQ6ICd2aWxsYWdlUGFjdCcsIGNvc3Q6ICdjYXNoJywgdmFsdWU6IDQ1LCByZXdhcmQ6ICdwb3J0YWJsZVdpbmNoJywgZ29sZDogOTAgfSxcbiAgZmxvb2RlZEthbWk6IHsgdGl0bGU6ICdORVJJREEgSURFQUxJU1QgUExBTicsIGFsaWdubWVudDogJ2thbWknLCBjb3N0OiAnaGVhbHRoJywgdmFsdWU6IDIsIHJld2FyZDogJ2dyYXBwbGVMaW5lJywgZ29sZDogNzAgfSxcbiAgY2xpZmZzUGFjdDogeyB0aXRsZTogJ0FFUklFIFBSQUNUSUNBTCBQTEFOJywgYWxpZ25tZW50OiAndmlsbGFnZVBhY3QnLCBjb3N0OiAnZm9jdXMnLCB2YWx1ZTogMiwgcmV3YXJkOiAnY2xpZmZTcG9vbCcsIGdvbGQ6IDc1IH0sXG4gIGNsaWZmc0thbWk6IHsgdGl0bGU6ICdBRVJJRSBJREVBTElTVCBQTEFOJywgYWxpZ25tZW50OiAna2FtaScsIGNvc3Q6ICdoZWFsdGgnLCB2YWx1ZTogMywgcmV3YXJkOiAnc2lnaHQnLCBnb2xkOiA3MCB9LFxuICBidXJpYWxQYWN0OiB7IHRpdGxlOiAnTUVNT1JJQUwgUFJBQ1RJQ0FMIFBMQU4nLCBhbGlnbm1lbnQ6ICd2aWxsYWdlUGFjdCcsIGNvc3Q6ICdjYXNoJywgdmFsdWU6IDQ1LCByZXdhcmQ6ICdtZW5kJywgZ29sZDogODUgfSxcbiAgYnVyaWFsS2FtaTogeyB0aXRsZTogJ01FTU9SSUFMIElERUFMSVNUIFBMQU4nLCBhbGlnbm1lbnQ6ICdrYW1pJywgY29zdDogJ2ZvY3VzJywgdmFsdWU6IDMsIHJld2FyZDogJ2FuY2VzdG9yVG9rZW4nLCBnb2xkOiA3NSB9LFxuICBzYWx0UGFjdDogeyB0aXRsZTogJ0hBTENZT04gUFJBQ1RJQ0FMIFBMQU4nLCBhbGlnbm1lbnQ6ICd2aWxsYWdlUGFjdCcsIGNvc3Q6ICdoZWFsdGgnLCB2YWx1ZTogMywgcmV3YXJkOiAnYnJpZGdlS2l0JywgZ29sZDogOTUgfSxcbiAgc2FsdEthbWk6IHsgdGl0bGU6ICdIQUxDWU9OIElERUFMSVNUIFBMQU4nLCBhbGlnbm1lbnQ6ICdrYW1pJywgY29zdDogJ2ZvY3VzJywgdmFsdWU6IDMsIHJld2FyZDogJ2ZpcmVKYXInLCBnb2xkOiA4MCB9LFxuICBmcm9zdFBhY3Q6IHsgdGl0bGU6ICdCT1JFQUxJUyBQUkFDVElDQUwgUExBTicsIGFsaWdubWVudDogJ3ZpbGxhZ2VQYWN0JywgY29zdDogJ2Nhc2gnLCB2YWx1ZTogNTAsIHJld2FyZDogJ21lbmQnLCBnb2xkOiAxMDAgfSxcbiAgZnJvc3RLYW1pOiB7IHRpdGxlOiAnQk9SRUFMSVMgSURFQUxJU1QgUExBTicsIGFsaWdubWVudDogJ2thbWknLCBjb3N0OiAnaGVhbHRoJywgdmFsdWU6IDMsIHJld2FyZDogJ3dhcmQnLCBnb2xkOiA4MCB9XG59XG5jb25zdCBhbGlnbm1lbnRQcm9maWxlRm9yID0gKGtpbmQ6IEZsb29yRW5jb3VudGVyWydraW5kJ10pOiBBbGlnbm1lbnRQcm9maWxlIHwgdW5kZWZpbmVkID0+IGFsaWdubWVudEVuY291bnRlcktpbmRzLmluY2x1ZGVzKGtpbmQgYXMgQWxpZ25tZW50RW5jb3VudGVyS2luZCkgPyBhbGlnbm1lbnRQcm9maWxlc1traW5kIGFzIEFsaWdubWVudEVuY291bnRlcktpbmRdIDogdW5kZWZpbmVkXG5jb25zdCBleGlzdGluZ0VuY291bnRlckFsaWdubWVudDogUGFydGlhbDxSZWNvcmQ8Rmxvb3JFbmNvdW50ZXJbJ2tpbmQnXSwgQWxpZ25tZW50Pj4gPSB7XG4gIHdheWZhcmVyOiAndmlsbGFnZVBhY3QnLCBibG9vZEJhcmdhaW46ICdrYW1pJywgc3Rvcm1DYWNoZTogJ3ZpbGxhZ2VQYWN0Jywgd2luZFRyaWFsOiAna2FtaScsIGFuY2VzdG9yRGVidDogJ2thbWknLCB0b21iQXVjdGlvbjogJ2thbWknLCBvYXRod2VsbDogJ2thbWknLCBjdXJzZWRPYmplY3Q6ICdrYW1pJyxcbiAgc3VuVHJpYnV0ZTogJ2thbWknLCBtaXJhZ2VNYXJrZXQ6ICd2aWxsYWdlUGFjdCcsIGJyaW5lT2F0aDogJ2thbWknLCBnbGFzc1RyaWFsOiAndmlsbGFnZVBhY3QnLCB3aGl0ZVJvYWQ6ICd2aWxsYWdlUGFjdCcsIHNhbHRDYWNoZTogJ3ZpbGxhZ2VQYWN0JyxcbiAgaWNlRHVlbDogJ3ZpbGxhZ2VQYWN0Jywgd2ludGVyVGl0aGU6ICdrYW1pJywgcmltZUNvbnRyYWN0OiAndmlsbGFnZVBhY3QnLCBmcm9zdENhY2hlOiAndmlsbGFnZVBhY3QnLCB3aGl0ZW91dDogJ2thbWknLCByZWxpcXVhcnlUcmlhbDogJ2thbWknXG59XG5leHBvcnQgY29uc3QgZW5jb3VudGVyVGl0bGUgPSAoa2luZDogRmxvb3JFbmNvdW50ZXJbJ2tpbmQnXSk6IHN0cmluZyA9PiB7XG4gIGNvbnN0IHRpdGxlID0gYWxpZ25tZW50UHJvZmlsZUZvcihraW5kKT8udGl0bGUgPz8gZXhwYW5zaW9uUHJvZmlsZUZvcihraW5kKT8udGl0bGVcbiAgcmV0dXJuIHRpdGxlID8/IChraW5kID09PSAnd2F5ZmFyZXInID8gJ1NUUkFOREVEIENSRVcnIDoga2luZCA9PT0gJ2Jsb29kQmFyZ2FpbicgPyAnU0VBTEVEIFBST1RPQ09MJyA6IGtpbmQgPT09ICdzdG9ybUNhY2hlJyA/ICdWRUNUT1IgQ0FDSEUnIDoga2luZCA9PT0gJ3dpbmRUcmlhbCcgPyAnVkVDVE9SIFRFU1QnIDoga2luZCA9PT0gJ2FuY2VzdG9yRGVidCcgPyAnQVJDSElWRSBERUJUJyA6IGtpbmQgPT09ICd0b21iQXVjdGlvbicgPyAnU0FMVkFHRSBFWENIQU5HRScgOiBraW5kID09PSAnb2F0aHdlbGwnID8gJ1BST1RPQ09MIFdFTEwnIDoga2luZCA9PT0gJ2N1cnNlZE9iamVjdCcgPyAnQ09SUlVQVEVEIE9CSkVDVCcgOiAnU0hJRlRJTkcgQ0hBTUJFUicpXG59XG5cbmNvbnN0IHRyYXZlcnNhbFJld2FyZHMgPSBbJ2dyYXBwbGVMaW5lJywgJ2JyaWRnZUtpdCcsICdzdGVhbUpldHBhY2snLCAncG9ydGFibGVXaW5jaCddIGFzIGNvbnN0XG5jb25zdCBlbmNvdW50ZXJBdFJlYWNoID0gKHN0YXRlOiBSdW5TdGF0ZSk6IEZsb29yRW5jb3VudGVyIHwgdW5kZWZpbmVkID0+IHN0YXRlLmZsb29yLmVuY291bnRlcnM/LmZpbmQoZW5jb3VudGVyID0+IGVuY291bnRlci5zdGF0ZSA9PT0gJ2Rvcm1hbnQnICYmIE1hdGgubWF4KE1hdGguYWJzKGVuY291bnRlci54IC0gc3RhdGUuaGVyby54KSwgTWF0aC5hYnMoZW5jb3VudGVyLnkgLSBzdGF0ZS5oZXJvLnkpKSA8PSAxKVxuY29uc3QgZW5jb3VudGVyID0gKHN0YXRlOiBSdW5TdGF0ZSwgaWQ6IHN0cmluZyk6IEZsb29yRW5jb3VudGVyIHwgdW5kZWZpbmVkID0+IHN0YXRlLmZsb29yLmVuY291bnRlcnM/LmZpbmQoY3VycmVudCA9PiBjdXJyZW50LmlkID09PSBpZCAmJiBjdXJyZW50LnN0YXRlID09PSAnZG9ybWFudCcpXG5jb25zdCByZXdhcmRGb3IgPSAoc3RhdGU6IFJ1blN0YXRlLCBzb3VyY2U6IEZsb29yRW5jb3VudGVyKTogc3RyaW5nID0+IHRyYXZlcnNhbFJld2FyZHNbKHN0YXRlLnNlZWQgKyBzdGF0ZS5mbG9vci5pbmRleCArIHNvdXJjZS54ICsgc291cmNlLnkpICUgdHJhdmVyc2FsUmV3YXJkcy5sZW5ndGhdXG5jb25zdCBncmFudEl0ZW0gPSAoc3RhdGU6IFJ1blN0YXRlLCBpZDogc3RyaW5nKTogdm9pZCA9PiB7XG4gIGlmIChzdGF0ZS5oZXJvLmludmVudG9yeS5sZW5ndGggPCAxMikgc3RhdGUuaGVyby5pbnZlbnRvcnkucHVzaChpZClcbiAgZWxzZSBzdGF0ZS5mbG9vci5pdGVtcy5wdXNoKHsgaWQsIHg6IHN0YXRlLmhlcm8ueCwgeTogc3RhdGUuaGVyby55LCBjb3VudDogMSwgdmlzaWJsZUluRm9nOiB0cnVlIH0pXG59XG5jb25zdCBncmFudENvbnRleHRHb2xkID0gKHN0YXRlOiBSdW5TdGF0ZSwgYW1vdW50OiBudW1iZXIpOiB2b2lkID0+IHsgZ3JhbnRHb2xkKHN0YXRlLCBhbW91bnQgKyAoc3RhdGUuZmxvb3IuZGlmZmljdWx0eT8udGhyZWF0ID8/IDApICogNSkgfVxuY29uc3QgY29udGV4dHVhbENvc3QgPSAoc3RhdGU6IFJ1blN0YXRlLCBhbW91bnQ6IG51bWJlcik6IG51bWJlciA9PiBhbW91bnQgKyAoc3RhdGUuZmxvb3IuZGlmZmljdWx0eT8udGhyZWF0ID8/IDApICogM1xuY29uc3QgYmluZE9wdGlvbmFsVG9vbCA9IChzdGF0ZTogUnVuU3RhdGUsIHRvb2w6IE5vbk51bGxhYmxlPEZsb29yRW5jb3VudGVyWyd0b29sT2ZmZXInXT4pOiB2b2lkID0+IHtcbiAgY29uc3QgYWNxdWlyZWQgPSBhY3F1aXJlT3B0aW9uYWxUcmF2ZXJzYWxUb29sKHN0YXRlLCB0b29sKVxuICBsb2coc3RhdGUsIGFjcXVpcmVkLnJlc3VsdCA9PT0gJ2JvdW5kJyA/IGBZb3UgYmluZCB0aGUgJHt0b29sRm9yKHRvb2wpLm5hbWV9LmAgOiBhY3F1aXJlZC5yZXN1bHQgPT09ICdkdXBsaWNhdGUnID8gYFlvdSBhbHJlYWR5IGNhcnJ5IHRoZSAke3Rvb2xGb3IodG9vbCkubmFtZX07IGxlYXZlIGl0cyBkdXBsaWNhdGUgYmVoaW5kLmAgOiBgWW91IHJlcGxhY2UgJHt0b29sRm9yKGFjcXVpcmVkLnJlcGxhY2VkISkubmFtZX0gd2l0aCB0aGUgJHt0b29sRm9yKHRvb2wpLm5hbWV9LmApXG59XG5jb25zdCB0b29sT2ZmZXJEZXRhaWwgPSAodG9vbDogTm9uTnVsbGFibGU8Rmxvb3JFbmNvdW50ZXJbJ3Rvb2xPZmZlciddPik6IHN0cmluZyA9PiBgT3B0aW9uYWwgdG9vbDogJHt0b29sRm9yKHRvb2wpLm5hbWV9OyBkdXBsaWNhdGVzIHN0YXkgdW5ib3VuZCBhbmQgYSBmdWxsIGxvYWRvdXQgcmVwbGFjZXMgaXRzIG9sZGVzdCBzbG90LmBcbmNvbnN0IHNvY2lhbE9mZmVyRGV0YWlsID0gKHNvdXJjZTogRmxvb3JFbmNvdW50ZXIpOiBzdHJpbmcgPT4gc291cmNlLnNvY2lhbD8ub2ZmZXIgPT09ICdzdXBwbHlDYWNoZScgPyAnR2FpbiBhIHJvcGUgYnVuZGxlIGFuZCBtYXJrIHRoZSBjYWNoZS4nIDogc291cmNlLnNvY2lhbD8ub2ZmZXIgPT09ICdzaG9ydGN1dCcgPyAnT3BlbiBvbmUgbmVhcmJ5IGxvY2tlZCByb3V0ZSwgaWYgcHJlc2VudC4nIDogJ1JldmVhbCB0aGUgcmVtYWluaW5nIHJvdXRlLidcbmNvbnN0IGFwcGx5U29jaWFsT2ZmZXIgPSAoc3RhdGU6IFJ1blN0YXRlLCBzb3VyY2U6IEZsb29yRW5jb3VudGVyKTogdm9pZCA9PiB7XG4gIGlmIChzb3VyY2Uuc29jaWFsPy5vZmZlciA9PT0gJ3N1cHBseUNhY2hlJykgZ3JhbnRJdGVtKHN0YXRlLCAncm9wZUJ1bmRsZScpXG4gIGVsc2UgaWYgKHNvdXJjZS5zb2NpYWw/Lm9mZmVyID09PSAnc2hvcnRjdXQnKSB7XG4gICAgY29uc3Qgc2hvcnRjdXQgPSBzdGF0ZS5mbG9vci50aWxlcy5maW5kKHRpbGUgPT4gdGlsZS5raW5kID09PSAnbG9ja2VkRG9vcicpXG4gICAgaWYgKHNob3J0Y3V0KSBzaG9ydGN1dC5raW5kID0gJ2Rvb3InXG4gICAgZWxzZSBzdGF0ZS5mbG9vci50aWxlcy5mb3JFYWNoKHRpbGUgPT4geyB0aWxlLmV4cGxvcmVkID0gdHJ1ZSB9KVxuICB9IGVsc2Ugc3RhdGUuZmxvb3IudGlsZXMuZm9yRWFjaCh0aWxlID0+IHsgdGlsZS5leHBsb3JlZCA9IHRydWUgfSlcbiAgcmVmcmVzaEZvdihzdGF0ZSlcbn1cbmNvbnN0IHNvY2lhbEhvc3RpbGUgPSAoc3RhdGU6IFJ1blN0YXRlLCBzb3VyY2U6IEZsb29yRW5jb3VudGVyKTogdm9pZCA9PiB7XG4gIGNvbnN0IHNvY2lhbCA9IHNvdXJjZS5zb2NpYWwhXG4gIGNvbnN0IGFjdG9yOiBBY3RvciA9IHsgaWQ6IGBzb2NpYWwtaG9zdGlsZToke3NvdXJjZS5pZH1gLCByb2xlOiAnYWxseScsIGtpbmQ6ICdhbGx5JywgbmFtZTogc29jaWFsLmZhY3Rpb24gPT09ICdrYW1pJyA/ICdJZGVhbGlzdCBob2xkb3V0JyA6ICdQcmFnbWF0aWMgcml2YWwnLCB4OiBzb3VyY2UueCwgeTogc291cmNlLnksIGhlYWx0aDogMTIsIG1heEhlYWx0aDogMTIsIGF0dGFjazogNCwgZGVmZW5zZTogOSwgc3BlZWQ6IDkwLCBlbmVyZ3k6IDAsIGdseXBoOiAnIScsIGNvbG9yOiBzb2NpYWwuZmFjdGlvbiA9PT0gJ2thbWknID8gJyNiNmQ4ZmYnIDogJyNmN2M2NzcnLCBob3N0aWxlOiB0cnVlLCBhaTogJ2NoYXNlJywgdGFnczogWydzb2NpYWwnLCBzb2NpYWwuZmFjdGlvbiwgc29jaWFsLnJvbGVdLCBzdGF0dXM6IFtgc29jaWFsOiR7c29jaWFsLmlkfTpob3N0aWxlYF0gfVxuICBzdGF0ZS5mbG9vci5hY3RvcnMucHVzaChhY3Rvcilcbn1cbmNvbnN0IHJlc29sdmUgPSAoc3RhdGU6IFJ1blN0YXRlLCBzb3VyY2U6IEZsb29yRW5jb3VudGVyLCBvdXRjb21lOiBzdHJpbmcsIGV2ZW50czogQWN0aW9uUmVzdWx0KTogQWN0aW9uUmVzdWx0ID0+IHtcbiAgc291cmNlLnN0YXRlID0gJ3Jlc29sdmVkJ1xuICBzdGF0ZS5tb2RhbCA9IHVuZGVmaW5lZFxuICBpZiAoc291cmNlLmtpbmQgIT09ICdjdXJzZWRPYmplY3QnKSBzZXR0bGVDdXJzZUFmdGVyRW5jb3VudGVyKHN0YXRlKVxuICBjb25zdCBkaXZpZGVuZCA9IGJvb25SYW5rKHN0YXRlLCAnZWNob0RpdmlkZW5kJykgKiAxMFxuICBpZiAoZGl2aWRlbmQpIGdyYW50Q29udGV4dEdvbGQoc3RhdGUsIGRpdmlkZW5kKVxuICBjb25zdCB3YXJkID0gYm9vblJhbmsoc3RhdGUsICdvc3N1YXJ5V2FyZCcpXG4gIGlmICh3YXJkKSBzdGF0ZS5oZXJvLmNvbmRpdGlvbnMgPSBbLi4uKHN0YXRlLmhlcm8uY29uZGl0aW9ucyA/PyBbXSksIHsga2luZDogJ3NoaWVsZGVkJywgZHVyYXRpb246IDIsIHBvdGVuY3k6IHdhcmQgfV1cbiAgY29uc3QgcmVsaXF1YXJ5RWNobyA9IGJvb25SYW5rKHN0YXRlLCAncmVsaXF1YXJ5RWNobycpXG4gIGlmIChyZWxpcXVhcnlFY2hvKSBzdGF0ZS5oZXJvLmNvbmRpdGlvbnMgPSBbLi4uKHN0YXRlLmhlcm8uY29uZGl0aW9ucyA/PyBbXSksIHsga2luZDogJ3NoaWVsZGVkJywgZHVyYXRpb246IDIsIHBvdGVuY3k6IHJlbGlxdWFyeUVjaG8gfV1cbiAgcmVjb3JkVGVsZW1ldHJ5Q291bnQoc3RhdGUsICdldmVudE91dGNvbWVzJywgYCR7c291cmNlLmtpbmR9OiR7b3V0Y29tZX1gKVxuICBjb25zdCBhbGlnbm1lbnQgPSBleGlzdGluZ0VuY291bnRlckFsaWdubWVudFtzb3VyY2Uua2luZF1cbiAgaWYgKGFsaWdubWVudCAmJiBvdXRjb21lICE9PSAnbGVhdmUnICYmIG91dGNvbWUgIT09ICdkZWNsaW5lJykgdGVuZChzdGF0ZSwgYWxpZ25tZW50KVxuICByZXR1cm4gW2V2ZW50KCdlbmNvdW50ZXInKSwgLi4uZXZlbnRzXVxufVxuXG5leHBvcnQgY29uc3QgZW5jb3VudGVyT3B0aW9ucyA9IChzdGF0ZTogUnVuU3RhdGUsIHNvdXJjZTogRmxvb3JFbmNvdW50ZXIpOiBFbmNvdW50ZXJPcHRpb25bXSA9PiB7XG4gIGlmIChzb3VyY2Uuc29jaWFsKSB7XG4gICAgY29uc3QgZGlzcG9zaXRpb24gPSBzb2NpYWxEaXNwb3NpdGlvbkZvcihzdGF0ZS5yZXB1dGF0aW9uLCBzb3VyY2Uuc29jaWFsLmZhY3Rpb24pXG4gICAgY29uc3QgYWxseSA9IGRpc3Bvc2l0aW9uID09PSAnaG9zdGlsZScgPyAnTUFLRSBBTUVORFMnIDogJ0hPTk9SIFRIRSBPRkZFUidcbiAgICByZXR1cm4gW1xuICAgICAgeyBsYWJlbDogYWxseSwgZGV0YWlsOiBgJHtzb3VyY2Uuc29jaWFsLmdvYWx9OyAke3NvY2lhbE9mZmVyRGV0YWlsKHNvdXJjZSl9JHtzb3VyY2UudG9vbE9mZmVyID8gYCAke3Rvb2xPZmZlckRldGFpbChzb3VyY2UudG9vbE9mZmVyKX1gIDogJyd9YCwgYXZhaWxhYmxlOiB0cnVlIH0sXG4gICAgICB7IGxhYmVsOiAnUFJFU1MgVEhFIENMQUlNJywgZGV0YWlsOiBgTG9zZSBzdGFuZGluZyB3aXRoICR7c291cmNlLnNvY2lhbC5mYWN0aW9ufTsgdGhleSBiZWNvbWUgaG9zdGlsZS5gLCBhdmFpbGFibGU6IHRydWUgfSxcbiAgICAgIHsgbGFiZWw6ICdMRUFWRScsIGRldGFpbDogJ0xlYXZlIHRoaXMgbG9jYWwgY29uY2VybiB1bnRvdWNoZWQuJywgYXZhaWxhYmxlOiB0cnVlIH1cbiAgICBdXG4gIH1cbiAgaWYgKHNvdXJjZS50b29sT2ZmZXIpIHJldHVybiBbXG4gICAgeyBsYWJlbDogJ1RBS0UgVE9PTCcsIGRldGFpbDogdG9vbE9mZmVyRGV0YWlsKHNvdXJjZS50b29sT2ZmZXIpLCBhdmFpbGFibGU6IHRydWUgfSxcbiAgICB7IGxhYmVsOiAnTUFQIFJPVVRFJywgZGV0YWlsOiAnUmV2ZWFsIHRoZSBmbG9vciB3aXRob3V0IHRha2luZyB0aGUgdG9vbC4nLCBhdmFpbGFibGU6IHRydWUgfSxcbiAgICB7IGxhYmVsOiAnTEVBVkUnLCBkZXRhaWw6ICdMZWF2ZSB0aGUgb3B0aW9uYWwgY2FjaGUgdW50b3VjaGVkLicsIGF2YWlsYWJsZTogdHJ1ZSB9XG4gIF1cbiAgY29uc3QgYWxpZ25tZW50ID0gYWxpZ25tZW50UHJvZmlsZUZvcihzb3VyY2Uua2luZClcbiAgaWYgKGFsaWdubWVudCkge1xuICAgIGNvbnN0IGNvc3QgPSBhbGlnbm1lbnQuY29zdCA9PT0gJ2hlYWx0aCcgPyBgTG9zZSAke2FsaWdubWVudC52YWx1ZX0gSFBgIDogYWxpZ25tZW50LmNvc3QgPT09ICdmb2N1cycgPyBgU3BlbmQgJHthbGlnbm1lbnQudmFsdWV9IGZvY3VzYCA6IGBTcGVuZCAke2FsaWdubWVudC52YWx1ZX0gY2FzaGBcbiAgICBjb25zdCBhdmFpbGFibGUgPSBhbGlnbm1lbnQuY29zdCA9PT0gJ2hlYWx0aCcgPyBzdGF0ZS5oZXJvLmhlYWx0aCA+IGFsaWdubWVudC52YWx1ZSArIDIgOiBhbGlnbm1lbnQuY29zdCA9PT0gJ2ZvY3VzJyA/IHN0YXRlLmhlcm8uZm9jdXMgPj0gYWxpZ25tZW50LnZhbHVlIDogc3RhdGUuaGVyby5nb2xkID49IGFsaWdubWVudC52YWx1ZVxuICAgIHJldHVybiBbXG4gICAgICB7IGxhYmVsOiBhbGlnbm1lbnQuYWxpZ25tZW50ID09PSAna2FtaScgPyAnQkFDSyBJREVBTElTTScgOiAnQkFDSyBQUkFHTUFUSVNNJywgZGV0YWlsOiBgJHtjb3N0fTsgZ2FpbiAke2FsaWdubWVudC5nb2xkfSBjYXNoIGFuZCAke0lURU1bYWxpZ25tZW50LnJld2FyZF0ubmFtZX0uYCwgYXZhaWxhYmxlIH0sXG4gICAgICB7IGxhYmVsOiBhbGlnbm1lbnQuYWxpZ25tZW50ID09PSAna2FtaScgPyAnUFJPVEVDVCBUSEUgUE9TU0lCSUxJVFknIDogJ1NFQ1VSRSBUSEUgUk9VVEUnLCBkZXRhaWw6IGBMb3NlIDIgSFA7IHJldmVhbCB0aGUgZmxvb3IgYW5kIGdhaW4gJHthbGlnbm1lbnQucmV3YXJkID8gSVRFTVthbGlnbm1lbnQucmV3YXJkXS5uYW1lIDogJ2Egcm91dGUgdG9vbCd9LmAsIGF2YWlsYWJsZTogc3RhdGUuaGVyby5oZWFsdGggPiA0IH0sXG4gICAgICB7IGxhYmVsOiAnTEVBVkUnLCBkZXRhaWw6ICdQYXNzIHRoZSBnYXRoZXJpbmcgd2l0aG91dCBhbnN3ZXIuJywgYXZhaWxhYmxlOiB0cnVlIH1cbiAgICBdXG4gIH1cbiAgY29uc3QgZXhwYW5zaW9uID0gZXhwYW5zaW9uUHJvZmlsZUZvcihzb3VyY2Uua2luZClcbiAgaWYgKGV4cGFuc2lvbikge1xuICAgIGNvbnN0IHByaW1hcnkgPSBleHBhbnNpb24uY29zdCA9PT0gJ2hlYWx0aCcgPyB7IGxhYmVsOiAnQkxFRUQgRk9SIFRIRSBPRkZFUicsIGRldGFpbDogYExvc2UgJHtleHBhbnNpb24udmFsdWV9IEhQOyBnYWluICR7ZXhwYW5zaW9uLmdvbGR9IGNhc2gsICR7ZXhwYW5zaW9uLmJvb259LCBhbmQgJHtleHBhbnNpb24ucmV3YXJkID8gSVRFTVtleHBhbnNpb24ucmV3YXJkXS5uYW1lIDogJ2EgcmV3YXJkJ30uYCwgYXZhaWxhYmxlOiBzdGF0ZS5oZXJvLmhlYWx0aCA+IGV4cGFuc2lvbi52YWx1ZSArIDIgfVxuICAgICAgOiBleHBhbnNpb24uY29zdCA9PT0gJ21heEhlYWx0aCcgPyB7IGxhYmVsOiAnUEFZIFZJVEFMSVRZJywgZGV0YWlsOiBgTG9zZSAke2V4cGFuc2lvbi52YWx1ZX0gbWF4aW11bSBIUDsgZ2FpbiAke2V4cGFuc2lvbi5nb2xkfSBjYXNoIGFuZCAke2V4cGFuc2lvbi5ib29ufS5gLCBhdmFpbGFibGU6IHN0YXRlLmhlcm8ubWF4SGVhbHRoID4gZXhwYW5zaW9uLnZhbHVlICsgNiB9XG4gICAgICAgIDogZXhwYW5zaW9uLmNvc3QgPT09ICdmb2N1cycgPyB7IGxhYmVsOiAnU1BFTkQgRk9DVVMnLCBkZXRhaWw6IGBTcGVuZCAke2V4cGFuc2lvbi52YWx1ZX0gZm9jdXM7IGdhaW4gJHtleHBhbnNpb24uZ29sZH0gY2FzaCBhbmQgJHtleHBhbnNpb24uYm9vbn0uYCwgYXZhaWxhYmxlOiBzdGF0ZS5oZXJvLmZvY3VzID49IGV4cGFuc2lvbi52YWx1ZSB9XG4gICAgICAgICAgOiB7IGxhYmVsOiAnR0lWRSBBTiBPRkZFUklORycsIGRldGFpbDogYENvbnN1bWUgJHtJVEVNW2V4cGFuc2lvbi5pdGVtIV0ubmFtZX07IGdhaW4gJHtleHBhbnNpb24uZ29sZH0gY2FzaCBhbmQgJHtleHBhbnNpb24uYm9vbn0uYCwgYXZhaWxhYmxlOiBzdGF0ZS5oZXJvLmludmVudG9yeS5pbmNsdWRlcyhleHBhbnNpb24uaXRlbSEpIH1cbiAgICBjb25zdCByaXNrID0gZXhwYW5zaW9uLnJpc2sgPT09ICdjdXJzZScgPyB7IGxhYmVsOiAnVEFLRSBUSEUgQ1VSU0UnLCBkZXRhaWw6ICdUYWtlIGEgdHdvLWVuY291bnRlciBkYW1hZ2UgY3Vyc2UgZm9yIDE1MCBjYXNoLicsIGF2YWlsYWJsZTogIXN0YXRlLmhlcm8uY3Vyc2UgfVxuICAgICAgOiBleHBhbnNpb24ucmlzayA9PT0gJ3RlcnJhaW4nID8geyBsYWJlbDogJ0NSQUNLIFRIRSBHUk9VTkQnLCBkZXRhaWw6IGBHYWluIDkwIGNhc2g7IG5lYXJieSBncm91bmQgYmVjb21lcyBkYW5nZXJvdXMgJHtleHBhbnNpb24udGVycmFpbiA9PT0gJ2JyaW5lJyA/ICdicmluZScgOiAncmltZSd9LmAsIGF2YWlsYWJsZTogdHJ1ZSB9XG4gICAgICAgIDogZXhwYW5zaW9uLnJpc2sgPT09ICdtYXAnID8geyBsYWJlbDogJ1JFQUQgVEhFIE9NRU4nLCBkZXRhaWw6ICdSZXZlYWwgdGhlIGZsb29yIGFuZCBnYWluIDIgZm9jdXMuJywgYXZhaWxhYmxlOiB0cnVlIH1cbiAgICAgICAgICA6IHsgbGFiZWw6ICdUQUtFIFRIRSBXQVJEJywgZGV0YWlsOiAnR2FpbiBhIHRocmVlLXR1cm4gc2hpZWxkIGFuZCA3MCBjYXNoLicsIGF2YWlsYWJsZTogdHJ1ZSB9XG4gICAgcmV0dXJuIFtwcmltYXJ5LCByaXNrLCB7IGxhYmVsOiAnTEVBVkUnLCBkZXRhaWw6ICdMZWF2ZSB0aGUgb2ZmZXIgdW50b3VjaGVkLicsIGF2YWlsYWJsZTogdHJ1ZSB9XVxuICB9XG4gIGlmIChzb3VyY2Uua2luZCA9PT0gJ3N0b3JtQ2FjaGUnKSByZXR1cm4gW1xuICAgIHsgbGFiZWw6ICdPUEVOIENBQ0hFJywgZGV0YWlsOiAnR2FpbiBjbGltYmluZyBnZWFyLCA2MCBjYXNoLCBhbmQgb25lIHRyYXZlcnNhbCBCb29uIHJhbmsuJywgYXZhaWxhYmxlOiB0cnVlIH0sXG4gICAgeyBsYWJlbDogJ01BUCBXSU5EJywgZGV0YWlsOiAnUmV2ZWFsIHRoZSBmbG9vciBhbmQgZ2FpbiAyIGZvY3VzLicsIGF2YWlsYWJsZTogdHJ1ZSB9LFxuICAgIHsgbGFiZWw6ICdMRUFWRScsIGRldGFpbDogJ0xlYXZlIHRoZSBzdG9ybSBjYWNoZSBzZWFsZWQuJywgYXZhaWxhYmxlOiB0cnVlIH1cbiAgXVxuICBpZiAoc291cmNlLmtpbmQgPT09ICd3aW5kVHJpYWwnKSByZXR1cm4gW1xuICAgIHsgbGFiZWw6ICdSSURFIFRIRSBHQUxFJywgZGV0YWlsOiAnTG9zZSAzIEhQOyBnYWluIDEwMCBjYXNoIGFuZCBvbmUgY29tYmF0IEJvb24gcmFuay4nLCBhdmFpbGFibGU6IHN0YXRlLmhlcm8uaGVhbHRoID4gNSB9LFxuICAgIHsgbGFiZWw6ICdCSU5EIFRIRSBHQUxFJywgZGV0YWlsOiAnU3BlbmQgMyBmb2N1czsgZ2FpbiBhIGdyYXBwbGluZyBsaW5lLicsIGF2YWlsYWJsZTogc3RhdGUuaGVyby5mb2N1cyA+PSAzIH0sXG4gICAgeyBsYWJlbDogJ0xFQVZFJywgZGV0YWlsOiAnS2VlcCB5b3VyIGZvb3RpbmcuJywgYXZhaWxhYmxlOiB0cnVlIH1cbiAgXVxuICBpZiAoc291cmNlLmtpbmQgPT09ICdhbmNlc3RvckRlYnQnKSByZXR1cm4gW1xuICAgIHsgbGFiZWw6ICdQQVkgVklUQUxJVFknLCBkZXRhaWw6ICdMb3NlIDMgbWF4aW11bSBIUDsgZ2FpbiA5MCBjYXNoIGFuZCBhIHNwaXJpdCBpdGVtLicsIGF2YWlsYWJsZTogc3RhdGUuaGVyby5tYXhIZWFsdGggPiA4IH0sXG4gICAgeyBsYWJlbDogJ1BBWSBBTiBPQVRIJywgZGV0YWlsOiAnTm8gaGVhbGluZyBmb3IgdHdvIGZsb29yczsgZ2FpbiAxMTAgY2FzaC4nLCBhdmFpbGFibGU6IChzdGF0ZS5oZXJvLm9hdGhzPy5sZW5ndGggPz8gMCkgPCAzIH0sXG4gICAgeyBsYWJlbDogJ0xFQVZFJywgZGV0YWlsOiAnTGVhdmUgdGhlIGRlYnQgdW5zZXR0bGVkLicsIGF2YWlsYWJsZTogdHJ1ZSB9XG4gIF1cbiAgaWYgKHNvdXJjZS5raW5kID09PSAndG9tYkF1Y3Rpb24nKSByZXR1cm4gW1xuICAgIHsgbGFiZWw6ICdCVVkgUkVMSUMnLCBkZXRhaWw6IGBTcGVuZCAke2NvbnRleHR1YWxDb3N0KHN0YXRlLCA1NSl9IGNhc2ggZm9yIGEgZ3JhdmUgcmVsaWMgYW5kIDEgQm9vbiByYW5rLmAsIGF2YWlsYWJsZTogc3RhdGUuaGVyby5nb2xkID49IGNvbnRleHR1YWxDb3N0KHN0YXRlLCA1NSkgfSxcbiAgICB7IGxhYmVsOiAnU0VMTCBCTE9PRCcsIGRldGFpbDogJ0xvc2UgNSBIUCBmb3IgODUgY2FzaC4nLCBhdmFpbGFibGU6IHN0YXRlLmhlcm8uaGVhbHRoID4gNiB9LFxuICAgIHsgbGFiZWw6ICdMRUFWRScsIGRldGFpbDogJ0RvIG5vdCBiaWQuJywgYXZhaWxhYmxlOiB0cnVlIH1cbiAgXVxuICBpZiAoc291cmNlLmtpbmQgPT09ICdvYXRod2VsbCcpIHJldHVybiBbXG4gICAgeyBsYWJlbDogJ1NXRUFSJywgZGV0YWlsOiAnVGFrZSBhIHR3by1mbG9vciBuby1jaGFybXMgb2F0aCBmb3IgOTAgY2FzaCBhbmQgMSBCb29uIHJhbmsuJywgYXZhaWxhYmxlOiAoc3RhdGUuaGVyby5vYXRocz8ubGVuZ3RoID8/IDApIDwgMyB9LFxuICAgIHsgbGFiZWw6ICdTQ09VUiBXRUxMJywgZGV0YWlsOiAnU3BlbmQgMiBmb2N1cyB0byByZW1vdmUgbmVhcmJ5IGhhemFyZHMuJywgYXZhaWxhYmxlOiBzdGF0ZS5oZXJvLmZvY3VzID49IDIgfSxcbiAgICB7IGxhYmVsOiAnTEVBVkUnLCBkZXRhaWw6ICdMZWF2ZSB0aGUgd2VsbCB1bnRvdWNoZWQuJywgYXZhaWxhYmxlOiB0cnVlIH1cbiAgXVxuICBpZiAoc291cmNlLmtpbmQgPT09ICdjdXJzZWRPYmplY3QnKSByZXR1cm4gW1xuICAgIHsgbGFiZWw6ICdUQUtFIE1JUlJPUicsIGRldGFpbDogJ1JhcmUgbGV0aGFsOiB0YWtlIG5vIGRhbWFnZSBhY3Jvc3MgdGhlIG5leHQgMiBlbmNvdW50ZXJzOyBnYWluIDE4MCBjYXNoLicsIGF2YWlsYWJsZTogIXN0YXRlLmhlcm8uY3Vyc2UgfSxcbiAgICB7IGxhYmVsOiAnVEFLRSBGTEVFQ0UnLCBkZXRhaWw6ICdTZXZlcmU6IHRha2Ugbm8gZGFtYWdlIGFjcm9zcyB0aGUgbmV4dCAyIGVuY291bnRlcnM7IGdhaW4gMTEwIGNhc2guJywgYXZhaWxhYmxlOiAhc3RhdGUuaGVyby5jdXJzZSB9LFxuICAgIHsgbGFiZWw6ICdUQUtFIElET0wnLCBkZXRhaWw6ICdTZXZlcmU6IHRha2Ugbm8gZGFtYWdlIGFjcm9zcyB0aGUgbmV4dCAyIGVuY291bnRlcnM7IGdhaW4gMTI1IGNhc2guJywgYXZhaWxhYmxlOiAhc3RhdGUuaGVyby5jdXJzZSB9LFxuICAgIHsgbGFiZWw6ICdUQUtFIFNIQVJEJywgZGV0YWlsOiAnU2V2ZXJlOiB0YWtlIG5vIGRhbWFnZSBhY3Jvc3MgdGhlIG5leHQgMiBlbmNvdW50ZXJzOyBnYWluIDEyNSBjYXNoLicsIGF2YWlsYWJsZTogIXN0YXRlLmhlcm8uY3Vyc2UgfSxcbiAgICB7IGxhYmVsOiAnTEVBVkUnLCBkZXRhaWw6ICdMZWF2ZSB0aGUgb2JqZWN0IGFsb25lLicsIGF2YWlsYWJsZTogdHJ1ZSB9XG4gIF1cbiAgaWYgKHNvdXJjZS5raW5kID09PSAnd2F5ZmFyZXInKSByZXR1cm4gW1xuICAgIHsgbGFiZWw6ICdUUkFERScsIGRldGFpbDogYCR7Y29udGV4dHVhbENvc3Qoc3RhdGUsIDM1KX0gY2FzaCBmb3IgJHtJVEVNW3Jld2FyZEZvcihzdGF0ZSwgc291cmNlKV0ubmFtZX0uYCwgYXZhaWxhYmxlOiBzdGF0ZS5oZXJvLmdvbGQgPj0gY29udGV4dHVhbENvc3Qoc3RhdGUsIDM1KSB9LFxuICAgIHsgbGFiZWw6ICdBU0sgUk9VVEUnLCBkZXRhaWw6ICdSZXZlYWwgdGhlIHJlbWFpbmluZyBmbG9vcjsgbm8gcGF5bWVudC4nLCBhdmFpbGFibGU6IHRydWUgfSxcbiAgICB7IGxhYmVsOiAnTEVBVkUnLCBkZXRhaWw6ICdLZWVwIG1vdmluZy4nLCBhdmFpbGFibGU6IHRydWUgfVxuICBdXG4gIGlmIChzb3VyY2Uua2luZCA9PT0gJ2Jsb29kQmFyZ2FpbicpIHJldHVybiBbXG4gICAgeyBsYWJlbDogJ0dJVkUgVklUQUxJVFknLCBkZXRhaWw6ICdMb3NlIDQgbWF4IEhQIGZvciA3NSBjYXNoIGFuZCB0cmF2ZXJzYWwgZ2Vhci4nLCBhdmFpbGFibGU6IHN0YXRlLmhlcm8ubWF4SGVhbHRoID4gOCB9LFxuICAgIHsgbGFiZWw6ICdHSVZFIEZPQ1VTJywgZGV0YWlsOiAnTG9zZSAzIGZvY3VzIGZvciAzMCBjYXNoIGFuZCBhIHJldmVhbGVkIGZsb29yLicsIGF2YWlsYWJsZTogc3RhdGUuaGVyby5mb2N1cyA+PSAzIH0sXG4gICAgeyBsYWJlbDogJ0RFQ0xJTkUnLCBkZXRhaWw6ICdMZWF2ZSB0aGUgYmFyZ2FpbiB1bnRvdWNoZWQuJywgYXZhaWxhYmxlOiB0cnVlIH1cbiAgXVxuICByZXR1cm4gW1xuICAgIHsgbGFiZWw6ICdPUEVOIENIQU1CRVInLCBkZXRhaWw6ICdTcGVuZCAyIGZvY3VzIHRvIGZsYXR0ZW4gbmVhcmJ5IGhhemFyZHMgYW5kIGJsb2NrZXJzLicsIGF2YWlsYWJsZTogc3RhdGUuaGVyby5mb2N1cyA+PSAyIH0sXG4gICAgeyBsYWJlbDogJ1NVUkdFIENIQU1CRVInLCBkZXRhaWw6ICdHYWluIDkwIGNhc2g7IG5lYXJieSBmbG9vciBiZWNvbWVzIGN1cnJlbnQuJywgYXZhaWxhYmxlOiB0cnVlIH0sXG4gICAgeyBsYWJlbDogJ0xFQVZFJywgZGV0YWlsOiAnRG8gbm90IGRpc3R1cmIgdGhlIGNoYW1iZXIuJywgYXZhaWxhYmxlOiB0cnVlIH1cbiAgXVxufVxuXG5leHBvcnQgY29uc3Qgb3BlbkVuY291bnRlciA9IChzdGF0ZTogUnVuU3RhdGUpOiBBY3Rpb25SZXN1bHQgfCB1bmRlZmluZWQgPT4ge1xuICBjb25zdCBzb3VyY2UgPSBlbmNvdW50ZXJBdFJlYWNoKHN0YXRlKVxuICBpZiAoIXNvdXJjZSkgcmV0dXJuIHVuZGVmaW5lZFxuICBzdGF0ZS5tb2RhbCA9IHsga2luZDogJ2VuY291bnRlcicsIGVuY291bnRlcklkOiBzb3VyY2UuaWQgfVxuICBjb25zdCBleHBhbnNpb24gPSBleHBhbnNpb25Qcm9maWxlRm9yKHNvdXJjZS5raW5kKVxuICBjb25zdCBhbGlnbm1lbnQgPSBhbGlnbm1lbnRQcm9maWxlRm9yKHNvdXJjZS5raW5kKVxuICBsb2coc3RhdGUsIHNvdXJjZS5zb2NpYWwgPyBgQSAke3NvdXJjZS5zb2NpYWwucm9sZX0gc2lnbmFsczogJHtzb3VyY2Uuc29jaWFsLmdvYWx9LmAgOiBhbGlnbm1lbnQgPyBgJHthbGlnbm1lbnQudGl0bGV9IHdhaXRzIGZvciB5b3VyIGFuc3dlci5gIDogZXhwYW5zaW9uID8gYCR7ZXhwYW5zaW9uLnRpdGxlfSBwcmVzZW50cyBhIGRhbmdlcm91cyBvZmZlci5gIDogc291cmNlLmtpbmQgPT09ICd3YXlmYXJlcicgPyAnQSBzdHJhbmRlZCBzcGVjaWFsaXN0IGNhbGxzIGZyb20gdGhlIGxhbmRpbmcgem9uZS4nIDogc291cmNlLmtpbmQgPT09ICdibG9vZEJhcmdhaW4nID8gJ0Egc2VhbGVkIHByb3RvY29sIHdhaXRzIGZvciBhbiBhbnN3ZXIuJyA6IHNvdXJjZS5raW5kID09PSAnY3Vyc2VkT2JqZWN0JyA/ICdBIGNvcnJ1cHRlZCBvYmplY3QgaHVtcyBiZXNpZGUgdGhlIHJvdXRlLicgOiBzb3VyY2Uua2luZCA9PT0gJ3N0b3JtQ2FjaGUnIHx8IHNvdXJjZS5raW5kID09PSAnd2luZFRyaWFsJyA/ICdBIHZlY3RvciBhbm9tYWx5IHByZXNlbnRzIGEgZGFuZ2Vyb3VzIG9mZmVyLicgOiBzb3VyY2Uua2luZCA9PT0gJ2FuY2VzdG9yRGVidCcgfHwgc291cmNlLmtpbmQgPT09ICd0b21iQXVjdGlvbicgPyAnQW4gYXJjaGl2ZSBvZmZlcnMgYSBwcmljZS4nIDogc291cmNlLmtpbmQgPT09ICdvYXRod2VsbCcgPyAnQSBwcm90b2NvbCB3ZWxsIHJlcXVlc3RzIGNvbmZpcm1hdGlvbi4nIDogJ1RoZSBjaGFtYmVyIHdhbGxzIGdyaW5kLCBhd2FpdGluZyBhIGNvbW1hbmQuJylcbiAgcmV0dXJuIFtldmVudCgnZW5jb3VudGVyJyksIGV2ZW50KCdtZW51JyldXG59XG5cbmNvbnN0IGNoYW1iZXJDZWxscyA9IChzdGF0ZTogUnVuU3RhdGUsIHNvdXJjZTogRmxvb3JFbmNvdW50ZXIsIHJhZGl1czogbnVtYmVyKSA9PiBzdGF0ZS5mbG9vci50aWxlcy5mbGF0TWFwKCh0aWxlLCBpbmRleCkgPT4ge1xuICBjb25zdCB4ID0gaW5kZXggJSA0OFxuICBjb25zdCB5ID0gTWF0aC5mbG9vcihpbmRleCAvIDQ4KVxuICByZXR1cm4gTWF0aC5tYXgoTWF0aC5hYnMoeCAtIHNvdXJjZS54KSwgTWF0aC5hYnMoeSAtIHNvdXJjZS55KSkgPD0gcmFkaXVzID8gW3sgdGlsZSwgeCwgeSB9XSA6IFtdXG59KS5maWx0ZXIoY2VsbCA9PiAhKGNlbGwueCA9PT0gc3RhdGUuaGVyby54ICYmIGNlbGwueSA9PT0gc3RhdGUuaGVyby55KSAmJiAhKGNlbGwueCA9PT0gc3RhdGUuZmxvb3Iuc3RhcnQueCAmJiBjZWxsLnkgPT09IHN0YXRlLmZsb29yLnN0YXJ0LnkpICYmICEoY2VsbC54ID09PSBzdGF0ZS5mbG9vci5leGl0LnggJiYgY2VsbC55ID09PSBzdGF0ZS5mbG9vci5leGl0LnkpKVxuXG5jb25zdCByZXNvbHZlRXhwYW5zaW9uRW5jb3VudGVyID0gKHN0YXRlOiBSdW5TdGF0ZSwgc291cmNlOiBGbG9vckVuY291bnRlciwgaW5kZXg6IG51bWJlcik6IEFjdGlvblJlc3VsdCB8IHVuZGVmaW5lZCA9PiB7XG4gIGNvbnN0IHByb2ZpbGUgPSBleHBhbnNpb25Qcm9maWxlRm9yKHNvdXJjZS5raW5kKVxuICBpZiAoIXByb2ZpbGUpIHJldHVybiB1bmRlZmluZWRcbiAgaWYgKGluZGV4ID09PSAwKSB7XG4gICAgaWYgKHByb2ZpbGUuY29zdCA9PT0gJ2hlYWx0aCcpIHN0YXRlLmhlcm8uaGVhbHRoIC09IHByb2ZpbGUudmFsdWVcbiAgICBpZiAocHJvZmlsZS5jb3N0ID09PSAnbWF4SGVhbHRoJykgeyBzdGF0ZS5oZXJvLm1heEhlYWx0aCAtPSBwcm9maWxlLnZhbHVlOyBzdGF0ZS5oZXJvLmhlYWx0aCA9IE1hdGgubWluKHN0YXRlLmhlcm8uaGVhbHRoLCBzdGF0ZS5oZXJvLm1heEhlYWx0aCkgfVxuICAgIGlmIChwcm9maWxlLmNvc3QgPT09ICdmb2N1cycpIHN0YXRlLmhlcm8uZm9jdXMgLT0gcHJvZmlsZS52YWx1ZVxuICAgIGlmIChwcm9maWxlLmNvc3QgPT09ICdpdGVtJykgc3RhdGUuaGVyby5pbnZlbnRvcnkuc3BsaWNlKHN0YXRlLmhlcm8uaW52ZW50b3J5LmluZGV4T2YocHJvZmlsZS5pdGVtISksIDEpXG4gICAgc3RhdGUuaGVyby5ib29ucyA/Pz0ge31cbiAgICBzdGF0ZS5oZXJvLmJvb25zW3Byb2ZpbGUuYm9vbl0gPSAoc3RhdGUuaGVyby5ib29uc1twcm9maWxlLmJvb25dID8/IDApICsgMVxuICAgIGdyYW50Q29udGV4dEdvbGQoc3RhdGUsIHByb2ZpbGUuZ29sZClcbiAgICBpZiAocHJvZmlsZS5yZXdhcmQpIGdyYW50SXRlbShzdGF0ZSwgcHJvZmlsZS5yZXdhcmQpXG4gICAgbG9nKHN0YXRlLCBgJHtwcm9maWxlLnRpdGxlfSBncmFudHMgJHtwcm9maWxlLmJvb259LmApXG4gICAgcmV0dXJuIHJlc29sdmUoc3RhdGUsIHNvdXJjZSwgJ29mZmVyJywgYWR2YW5jZShzdGF0ZSwgW2V2ZW50KCdwaWNrdXAnKV0pKVxuICB9XG4gIGlmIChpbmRleCA9PT0gMSkge1xuICAgIGlmIChwcm9maWxlLnJpc2sgPT09ICdjdXJzZScpIHtcbiAgICAgIHN0YXRlLmhlcm8uY3Vyc2UgPSB7IGl0ZW1JZDogJ2N1cnNlZE1pcnJvcicsIG5hbWU6IElURU0uY3Vyc2VkTWlycm9yLm5hbWUsIGNvbmRpdGlvbjogJ1Rha2Ugbm8gZGFtYWdlIGJlZm9yZSByZXNvbHZpbmcgdHdvIGVuY291bnRlcnMuJywgcmVtYWluaW5nRW5jb3VudGVyczogMiwgbGV0aGFsOiBmYWxzZSB9XG4gICAgICBncmFudEl0ZW0oc3RhdGUsICdjdXJzZWRNaXJyb3InKVxuICAgICAgZ3JhbnRDb250ZXh0R29sZChzdGF0ZSwgMTUwKVxuICAgICAgcmV0dXJuIHJlc29sdmUoc3RhdGUsIHNvdXJjZSwgJ2N1cnNlJywgW2V2ZW50KCdwaWNrdXAnKV0pXG4gICAgfVxuICAgIGlmIChwcm9maWxlLnJpc2sgPT09ICd0ZXJyYWluJykge1xuICAgICAgY29uc3QgY2VsbHMgPSBjaGFtYmVyQ2VsbHMoc3RhdGUsIHNvdXJjZSwgMikuZmlsdGVyKGNlbGwgPT4gY2VsbC50aWxlLmtpbmQgPT09ICdmbG9vcicpLnNsaWNlKDAsIDUpXG4gICAgICBjZWxscy5mb3JFYWNoKGNlbGwgPT4geyBjZWxsLnRpbGUua2luZCA9IHByb2ZpbGUudGVycmFpbiB9KVxuICAgICAgZ3JhbnRDb250ZXh0R29sZChzdGF0ZSwgOTApXG4gICAgICByZWZyZXNoRm92KHN0YXRlKVxuICAgICAgcmV0dXJuIHJlc29sdmUoc3RhdGUsIHNvdXJjZSwgJ3RlcnJhaW4nLCBhZHZhbmNlKHN0YXRlLCBbZXZlbnQoJ2RhbmdlcicpXSkpXG4gICAgfVxuICAgIGlmIChwcm9maWxlLnJpc2sgPT09ICdtYXAnKSB7XG4gICAgICBzdGF0ZS5mbG9vci50aWxlcy5mb3JFYWNoKHRpbGUgPT4geyB0aWxlLmV4cGxvcmVkID0gdHJ1ZSB9KVxuICAgICAgc3RhdGUuaGVyby5mb2N1cyA9IE1hdGgubWluKHN0YXRlLmhlcm8ubWF4Rm9jdXMsIHN0YXRlLmhlcm8uZm9jdXMgKyAyKVxuICAgICAgcmVmcmVzaEZvdihzdGF0ZSlcbiAgICAgIHJldHVybiByZXNvbHZlKHN0YXRlLCBzb3VyY2UsICdtYXAnLCBbZXZlbnQoJ21lbnUnKV0pXG4gICAgfVxuICAgIGFkZENvbmRpdGlvbihzdGF0ZS5oZXJvLCB7IGtpbmQ6ICdzaGllbGRlZCcsIGR1cmF0aW9uOiAzLCBwb3RlbmN5OiAyIH0pXG4gICAgZ3JhbnRDb250ZXh0R29sZChzdGF0ZSwgNzApXG4gICAgcmV0dXJuIHJlc29sdmUoc3RhdGUsIHNvdXJjZSwgJ3dhcmQnLCBbZXZlbnQoJ3NwZWxsJyldKVxuICB9XG4gIHJldHVybiByZXNvbHZlKHN0YXRlLCBzb3VyY2UsICdsZWF2ZScsIFtldmVudCgnbWVudScpXSlcbn1cblxuY29uc3QgcmVzb2x2ZUFsaWdubWVudEVuY291bnRlciA9IChzdGF0ZTogUnVuU3RhdGUsIHNvdXJjZTogRmxvb3JFbmNvdW50ZXIsIGluZGV4OiBudW1iZXIpOiBBY3Rpb25SZXN1bHQgfCB1bmRlZmluZWQgPT4ge1xuICBjb25zdCBwcm9maWxlID0gYWxpZ25tZW50UHJvZmlsZUZvcihzb3VyY2Uua2luZClcbiAgaWYgKCFwcm9maWxlKSByZXR1cm4gdW5kZWZpbmVkXG4gIGlmIChpbmRleCA9PT0gMCkge1xuICAgIGlmIChwcm9maWxlLmNvc3QgPT09ICdoZWFsdGgnKSBzdGF0ZS5oZXJvLmhlYWx0aCAtPSBwcm9maWxlLnZhbHVlXG4gICAgaWYgKHByb2ZpbGUuY29zdCA9PT0gJ2ZvY3VzJykgc3RhdGUuaGVyby5mb2N1cyAtPSBwcm9maWxlLnZhbHVlXG4gICAgaWYgKHByb2ZpbGUuY29zdCA9PT0gJ2Nhc2gnKSBzdGF0ZS5oZXJvLmdvbGQgLT0gcHJvZmlsZS52YWx1ZVxuICAgIGdyYW50Q29udGV4dEdvbGQoc3RhdGUsIHByb2ZpbGUuZ29sZClcbiAgICBncmFudEl0ZW0oc3RhdGUsIHByb2ZpbGUucmV3YXJkKVxuICAgIGxvZyhzdGF0ZSwgYCR7cHJvZmlsZS50aXRsZX0gZ2l2ZXMgJHtJVEVNW3Byb2ZpbGUucmV3YXJkXS5uYW1lfS5gKVxuICAgIHRlbmQoc3RhdGUsIHByb2ZpbGUuYWxpZ25tZW50KVxuICAgIHJldHVybiByZXNvbHZlKHN0YXRlLCBzb3VyY2UsICdwbGVkZ2UnLCBhZHZhbmNlKHN0YXRlLCBbZXZlbnQoJ3BpY2t1cCcpXSkpXG4gIH1cbiAgaWYgKGluZGV4ID09PSAxKSB7XG4gICAgc3RhdGUuaGVyby5oZWFsdGggLT0gMlxuICAgIHN0YXRlLmZsb29yLnRpbGVzLmZvckVhY2godGlsZSA9PiB7IHRpbGUuZXhwbG9yZWQgPSB0cnVlIH0pXG4gICAgZ3JhbnRJdGVtKHN0YXRlLCBwcm9maWxlLnJld2FyZClcbiAgICByZWZyZXNoRm92KHN0YXRlKVxuICAgIGxvZyhzdGF0ZSwgYCR7cHJvZmlsZS50aXRsZX0gcmV2ZWFscyB0aGUgcm91dGUuYClcbiAgICB0ZW5kKHN0YXRlLCBwcm9maWxlLmFsaWdubWVudClcbiAgICByZXR1cm4gcmVzb2x2ZShzdGF0ZSwgc291cmNlLCAndmlnaWwnLCBhZHZhbmNlKHN0YXRlLCBbZXZlbnQoJ21lbnUnKV0pKVxuICB9XG4gIHJldHVybiByZXNvbHZlKHN0YXRlLCBzb3VyY2UsICdsZWF2ZScsIFtldmVudCgnbWVudScpXSlcbn1cblxuY29uc3QgcmVzb2x2ZVNvY2lhbEVuY291bnRlciA9IChzdGF0ZTogUnVuU3RhdGUsIHNvdXJjZTogRmxvb3JFbmNvdW50ZXIsIGluZGV4OiBudW1iZXIpOiBBY3Rpb25SZXN1bHQgfCB1bmRlZmluZWQgPT4ge1xuICBjb25zdCBzb2NpYWwgPSBzb3VyY2Uuc29jaWFsXG4gIGlmICghc29jaWFsKSByZXR1cm4gdW5kZWZpbmVkXG4gIGlmIChpbmRleCA9PT0gMCkge1xuICAgIHNvdXJjZS5zb2NpYWwgPSB7IC4uLnNvY2lhbCwgZGlzcG9zaXRpb246ICdhbGxpZWQnIH1cbiAgICBzdGF0ZS5yZXB1dGF0aW9uID0gYWRqdXN0U29jaWFsUmVwdXRhdGlvbihzdGF0ZS5yZXB1dGF0aW9uLCBzb2NpYWwuZmFjdGlvbiwgMSlcbiAgICBhcHBseVNvY2lhbE9mZmVyKHN0YXRlLCBzb3VyY2UpXG4gICAgaWYgKHNvdXJjZS50b29sT2ZmZXIpIGJpbmRPcHRpb25hbFRvb2woc3RhdGUsIHNvdXJjZS50b29sT2ZmZXIpXG4gICAgbG9nKHN0YXRlLCBgJHtzb2NpYWwuZmFjdGlvbn0gbWFya3MgYSByb3V0ZSBmb3IgeW91LmApXG4gICAgcmV0dXJuIHJlc29sdmUoc3RhdGUsIHNvdXJjZSwgJ2FsbGllZCcsIGFkdmFuY2Uoc3RhdGUsIFtldmVudCgncGlja3VwJyldKSlcbiAgfVxuICBpZiAoaW5kZXggPT09IDEpIHtcbiAgICBzb3VyY2Uuc29jaWFsID0geyAuLi5zb2NpYWwsIGRpc3Bvc2l0aW9uOiAnaG9zdGlsZScgfVxuICAgIHN0YXRlLnJlcHV0YXRpb24gPSBhZGp1c3RTb2NpYWxSZXB1dGF0aW9uKHN0YXRlLnJlcHV0YXRpb24sIHNvY2lhbC5mYWN0aW9uLCAtMSlcbiAgICBzb2NpYWxIb3N0aWxlKHN0YXRlLCBzb3VyY2UpXG4gICAgbG9nKHN0YXRlLCBgJHtzb2NpYWwuZmFjdGlvbn0gdHVybnMgaG9zdGlsZSBvdmVyIHRoZSByb3V0ZS5gKVxuICAgIHJldHVybiByZXNvbHZlKHN0YXRlLCBzb3VyY2UsICdob3N0aWxlJywgYWR2YW5jZShzdGF0ZSwgW2V2ZW50KCdlbmNvdW50ZXInKV0pKVxuICB9XG4gIHJldHVybiByZXNvbHZlKHN0YXRlLCBzb3VyY2UsICdsZWF2ZScsIFtldmVudCgnbWVudScpXSlcbn1cblxuZXhwb3J0IGNvbnN0IGNob29zZUVuY291bnRlciA9IChzdGF0ZTogUnVuU3RhdGUsIGVuY291bnRlcklkOiBzdHJpbmcsIGNvbW1hbmQ6IHN0cmluZyk6IEFjdGlvblJlc3VsdCA9PiB7XG4gIGNvbnN0IHNvdXJjZSA9IGVuY291bnRlcihzdGF0ZSwgZW5jb3VudGVySWQpXG4gIGlmICghc291cmNlKSByZXR1cm4gW11cbiAgY29uc3QgaW5kZXggPSBOdW1iZXIoY29tbWFuZCkgLSAxXG4gIGNvbnN0IG9wdGlvbiA9IGVuY291bnRlck9wdGlvbnMoc3RhdGUsIHNvdXJjZSlbaW5kZXhdXG4gIGlmICghb3B0aW9uKSByZXR1cm4gW11cbiAgaWYgKCFvcHRpb24uYXZhaWxhYmxlKSB7IGxvZyhzdGF0ZSwgJ1lvdSBjYW5ub3QgbWVldCB0aGF0IGNvc3QuJyk7IHJldHVybiBbZXZlbnQoJ21lbnUnKV0gfVxuICBjb25zdCBzb2NpYWwgPSByZXNvbHZlU29jaWFsRW5jb3VudGVyKHN0YXRlLCBzb3VyY2UsIGluZGV4KVxuICBpZiAoc29jaWFsKSByZXR1cm4gc29jaWFsXG4gIGlmIChzb3VyY2UudG9vbE9mZmVyKSB7XG4gICAgaWYgKGluZGV4ID09PSAwKSB7IGJpbmRPcHRpb25hbFRvb2woc3RhdGUsIHNvdXJjZS50b29sT2ZmZXIpOyByZXR1cm4gcmVzb2x2ZShzdGF0ZSwgc291cmNlLCAndG9vbCcsIGFkdmFuY2Uoc3RhdGUsIFtldmVudCgncGlja3VwJyldKSkgfVxuICAgIGlmIChpbmRleCA9PT0gMSkgeyBzdGF0ZS5mbG9vci50aWxlcy5mb3JFYWNoKHRpbGUgPT4geyB0aWxlLmV4cGxvcmVkID0gdHJ1ZSB9KTsgcmVmcmVzaEZvdihzdGF0ZSk7IHJldHVybiByZXNvbHZlKHN0YXRlLCBzb3VyY2UsICdyb3V0ZScsIFtldmVudCgnbWVudScpXSkgfVxuICAgIHJldHVybiByZXNvbHZlKHN0YXRlLCBzb3VyY2UsICdsZWF2ZScsIFtldmVudCgnbWVudScpXSlcbiAgfVxuICBjb25zdCBhbGlnbm1lbnQgPSByZXNvbHZlQWxpZ25tZW50RW5jb3VudGVyKHN0YXRlLCBzb3VyY2UsIGluZGV4KVxuICBpZiAoYWxpZ25tZW50KSByZXR1cm4gYWxpZ25tZW50XG4gIGNvbnN0IGV4cGFuc2lvbiA9IHJlc29sdmVFeHBhbnNpb25FbmNvdW50ZXIoc3RhdGUsIHNvdXJjZSwgaW5kZXgpXG4gIGlmIChleHBhbnNpb24pIHJldHVybiBleHBhbnNpb25cbiAgaWYgKHNvdXJjZS5raW5kID09PSAnc3Rvcm1DYWNoZScpIHtcbiAgICBpZiAoaW5kZXggPT09IDApIHsgZ3JhbnRDb250ZXh0R29sZChzdGF0ZSwgNjApOyBncmFudEl0ZW0oc3RhdGUsICdjbGlmZlNwb29sJyk7IHN0YXRlLmhlcm8uYm9vbnMgPz89IHt9OyBzdGF0ZS5oZXJvLmJvb25zLmdhbGVUaHJlYWQgPSAoc3RhdGUuaGVyby5ib29ucy5nYWxlVGhyZWFkID8/IDApICsgMTsgcmV0dXJuIHJlc29sdmUoc3RhdGUsIHNvdXJjZSwgJ29wZW4nLCBhZHZhbmNlKHN0YXRlLCBbZXZlbnQoJ3BpY2t1cCcpXSkpIH1cbiAgICBpZiAoaW5kZXggPT09IDEpIHsgc3RhdGUuZmxvb3IudGlsZXMuZm9yRWFjaCh0aWxlID0+IHsgdGlsZS5leHBsb3JlZCA9IHRydWUgfSk7IHN0YXRlLmhlcm8uZm9jdXMgPSBNYXRoLm1pbihzdGF0ZS5oZXJvLm1heEZvY3VzLCBzdGF0ZS5oZXJvLmZvY3VzICsgMik7IHJlZnJlc2hGb3Yoc3RhdGUpOyByZXR1cm4gcmVzb2x2ZShzdGF0ZSwgc291cmNlLCAnbWFwJywgW2V2ZW50KCdtZW51JyldKSB9XG4gICAgcmV0dXJuIHJlc29sdmUoc3RhdGUsIHNvdXJjZSwgJ2xlYXZlJywgW2V2ZW50KCdtZW51JyldKVxuICB9XG4gIGlmIChzb3VyY2Uua2luZCA9PT0gJ3dpbmRUcmlhbCcpIHtcbiAgICBpZiAoaW5kZXggPT09IDApIHsgc3RhdGUuaGVyby5oZWFsdGggLT0gMzsgZ3JhbnRDb250ZXh0R29sZChzdGF0ZSwgMTAwKTsgc3RhdGUuaGVyby5ib29ucyA/Pz0ge307IHN0YXRlLmhlcm8uYm9vbnMuc2t5aG9va1JlcHJpc2FsID0gKHN0YXRlLmhlcm8uYm9vbnMuc2t5aG9va1JlcHJpc2FsID8/IDApICsgMTsgcmV0dXJuIHJlc29sdmUoc3RhdGUsIHNvdXJjZSwgJ3JpZGUnLCBhZHZhbmNlKHN0YXRlLCBbZXZlbnQoJ2h1cnQnKV0pKSB9XG4gICAgaWYgKGluZGV4ID09PSAxKSB7IHN0YXRlLmhlcm8uZm9jdXMgLT0gMzsgZ3JhbnRJdGVtKHN0YXRlLCAnZ3JhcHBsZUxpbmUnKTsgcmV0dXJuIHJlc29sdmUoc3RhdGUsIHNvdXJjZSwgJ2JpbmQnLCBhZHZhbmNlKHN0YXRlLCBbZXZlbnQoJ3BpY2t1cCcpXSkpIH1cbiAgICByZXR1cm4gcmVzb2x2ZShzdGF0ZSwgc291cmNlLCAnbGVhdmUnLCBbZXZlbnQoJ21lbnUnKV0pXG4gIH1cbiAgaWYgKHNvdXJjZS5raW5kID09PSAnYW5jZXN0b3JEZWJ0Jykge1xuICAgIGlmIChpbmRleCA9PT0gMCkgeyBzdGF0ZS5oZXJvLm1heEhlYWx0aCAtPSAzOyBzdGF0ZS5oZXJvLmhlYWx0aCA9IE1hdGgubWluKHN0YXRlLmhlcm8uaGVhbHRoLCBzdGF0ZS5oZXJvLm1heEhlYWx0aCk7IGdyYW50Q29udGV4dEdvbGQoc3RhdGUsIDkwKTsgZ3JhbnRJdGVtKHN0YXRlLCAnYW5jZXN0b3JUb2tlbicpOyByZXR1cm4gcmVzb2x2ZShzdGF0ZSwgc291cmNlLCAndml0YWxpdHknLCBhZHZhbmNlKHN0YXRlLCBbZXZlbnQoJ3BpY2t1cCcpXSkpIH1cbiAgICBpZiAoaW5kZXggPT09IDEpIHsgc3RhdGUuaGVyby5vYXRocyA9IFsuLi4oc3RhdGUuaGVyby5vYXRocyA/PyBbXSksIHsgaWQ6ICdub0hlYWxpbmcnLCByZW1haW5pbmdGbG9vcnM6IDIgfV07IGdyYW50Q29udGV4dEdvbGQoc3RhdGUsIDExMCk7IHJldHVybiByZXNvbHZlKHN0YXRlLCBzb3VyY2UsICdvYXRoJywgW2V2ZW50KCdlbmNvdW50ZXInKV0pIH1cbiAgICByZXR1cm4gcmVzb2x2ZShzdGF0ZSwgc291cmNlLCAnbGVhdmUnLCBbZXZlbnQoJ21lbnUnKV0pXG4gIH1cbiAgaWYgKHNvdXJjZS5raW5kID09PSAndG9tYkF1Y3Rpb24nKSB7XG4gICAgaWYgKGluZGV4ID09PSAwKSB7IHN0YXRlLmhlcm8uZ29sZCAtPSBjb250ZXh0dWFsQ29zdChzdGF0ZSwgNTUpOyBncmFudEl0ZW0oc3RhdGUsICdtb3VybmluZ0JlbGwnKTsgc3RhdGUuaGVyby5ib29ucyA/Pz0ge307IHN0YXRlLmhlcm8uYm9vbnMuZ3JhdmVMZWRnZXIgPSAoc3RhdGUuaGVyby5ib29ucy5ncmF2ZUxlZGdlciA/PyAwKSArIDE7IHJldHVybiByZXNvbHZlKHN0YXRlLCBzb3VyY2UsICdidXknLCBhZHZhbmNlKHN0YXRlLCBbZXZlbnQoJ3BpY2t1cCcpXSkpIH1cbiAgICBpZiAoaW5kZXggPT09IDEpIHsgc3RhdGUuaGVyby5oZWFsdGggLT0gNTsgZ3JhbnRDb250ZXh0R29sZChzdGF0ZSwgODUpOyByZXR1cm4gcmVzb2x2ZShzdGF0ZSwgc291cmNlLCAnYmxvb2QnLCBhZHZhbmNlKHN0YXRlLCBbZXZlbnQoJ2h1cnQnKV0pKSB9XG4gICAgcmV0dXJuIHJlc29sdmUoc3RhdGUsIHNvdXJjZSwgJ2xlYXZlJywgW2V2ZW50KCdtZW51JyldKVxuICB9XG4gIGlmIChzb3VyY2Uua2luZCA9PT0gJ29hdGh3ZWxsJykge1xuICAgIGlmIChpbmRleCA9PT0gMCkgeyBzdGF0ZS5oZXJvLm9hdGhzID0gWy4uLihzdGF0ZS5oZXJvLm9hdGhzID8/IFtdKSwgeyBpZDogJ25vQ2hhcm1zJywgcmVtYWluaW5nRmxvb3JzOiAyIH1dOyBncmFudENvbnRleHRHb2xkKHN0YXRlLCA5MCk7IHN0YXRlLmhlcm8uYm9vbnMgPz89IHt9OyBzdGF0ZS5oZXJvLmJvb25zLmJyaWRnZU9mTmFtZXMgPSAoc3RhdGUuaGVyby5ib29ucy5icmlkZ2VPZk5hbWVzID8/IDApICsgMTsgcmV0dXJuIHJlc29sdmUoc3RhdGUsIHNvdXJjZSwgJ3N3ZWFyJywgW2V2ZW50KCdlbmNvdW50ZXInKV0pIH1cbiAgICBpZiAoaW5kZXggPT09IDEpIHsgc3RhdGUuaGVyby5mb2N1cyAtPSAyOyBjb25zdCBjZWxscyA9IGNoYW1iZXJDZWxscyhzdGF0ZSwgc291cmNlLCAyKS5maWx0ZXIoY2VsbCA9PiBbJ3J1YmJsZScsICdicmFtYmxlJywgJ2JvdWxkZXInLCAncGl0JywgJ3dhdGVyJywgJ2RlZXBXYXRlcicsICdjdXJyZW50JywgJ2dhcycsICdzbW9rZScsICdmaXJlVmVudCddLmluY2x1ZGVzKGNlbGwudGlsZS5raW5kKSk7IGNlbGxzLmZvckVhY2goY2VsbCA9PiB7IGNlbGwudGlsZS5raW5kID0gJ2Zsb29yJyB9KTsgcmV0dXJuIHJlc29sdmUoc3RhdGUsIHNvdXJjZSwgJ3Njb3VyJywgYWR2YW5jZShzdGF0ZSwgW2V2ZW50KCdzcGVsbCcpXSkpIH1cbiAgICByZXR1cm4gcmVzb2x2ZShzdGF0ZSwgc291cmNlLCAnbGVhdmUnLCBbZXZlbnQoJ21lbnUnKV0pXG4gIH1cbiAgaWYgKHNvdXJjZS5raW5kID09PSAnY3Vyc2VkT2JqZWN0Jykge1xuICAgIGlmIChpbmRleCA8IDQpIHtcbiAgICAgIGNvbnN0IGxldGhhbCA9IGluZGV4ID09PSAwXG4gICAgICBjb25zdCBpdGVtSWQgPSBbJ2N1cnNlZE1pcnJvcicsICdncmF2ZUZsZWVjZScsICdzdG9ybUlkb2wnLCAnb2F0aFNoYXJkJ11baW5kZXhdIGFzICdjdXJzZWRNaXJyb3InIHwgJ2dyYXZlRmxlZWNlJyB8ICdzdG9ybUlkb2wnIHwgJ29hdGhTaGFyZCdcbiAgICAgIHN0YXRlLmhlcm8uY3Vyc2UgPSB7IGl0ZW1JZCwgbmFtZTogSVRFTVtpdGVtSWRdLm5hbWUsIGNvbmRpdGlvbjogJ1Rha2Ugbm8gZGFtYWdlIGJlZm9yZSByZXNvbHZpbmcgdHdvIGVuY291bnRlcnMuJywgcmVtYWluaW5nRW5jb3VudGVyczogMiwgbGV0aGFsIH1cbiAgICAgIGdyYW50SXRlbShzdGF0ZSwgaXRlbUlkKVxuICAgICAgZ3JhbnRDb250ZXh0R29sZChzdGF0ZSwgbGV0aGFsID8gMTgwIDogaXRlbUlkID09PSAnZ3JhdmVGbGVlY2UnID8gMTEwIDogMTI1KVxuICAgICAgcmV0dXJuIHJlc29sdmUoc3RhdGUsIHNvdXJjZSwgaXRlbUlkLCBbZXZlbnQoJ3BpY2t1cCcpXSlcbiAgICB9XG4gICAgcmV0dXJuIHJlc29sdmUoc3RhdGUsIHNvdXJjZSwgJ2xlYXZlJywgW2V2ZW50KCdtZW51JyldKVxuICB9XG4gIGlmIChzb3VyY2Uua2luZCA9PT0gJ3dheWZhcmVyJykge1xuICAgIGlmIChpbmRleCA9PT0gMCkgeyBzdGF0ZS5oZXJvLmdvbGQgLT0gY29udGV4dHVhbENvc3Qoc3RhdGUsIDM1KTsgY29uc3QgcmV3YXJkID0gcmV3YXJkRm9yKHN0YXRlLCBzb3VyY2UpOyBncmFudEl0ZW0oc3RhdGUsIHJld2FyZCk7IGxvZyhzdGF0ZSwgYFRoZSB3YXlmYXJlciB0cmFkZXMgJHtJVEVNW3Jld2FyZF0ubmFtZX0gZm9yIHlvdXIgY2FzaC5gKTsgcmV0dXJuIHJlc29sdmUoc3RhdGUsIHNvdXJjZSwgJ3RyYWRlJywgYWR2YW5jZShzdGF0ZSwgW2V2ZW50KCdwaWNrdXAnKV0pKSB9XG4gICAgaWYgKGluZGV4ID09PSAxKSB7IHN0YXRlLmZsb29yLnRpbGVzLmZvckVhY2godGlsZSA9PiB7IHRpbGUuZXhwbG9yZWQgPSB0cnVlIH0pOyByZWZyZXNoRm92KHN0YXRlKTsgbG9nKHN0YXRlLCAnVGhlIHdheWZhcmVyIG1hcHMgdGhlIHNpZGUgcm91dGVzLicpOyByZXR1cm4gcmVzb2x2ZShzdGF0ZSwgc291cmNlLCAncm91dGUnLCBbZXZlbnQoJ21lbnUnKV0pIH1cbiAgICBsb2coc3RhdGUsICdUaGUgd2F5ZmFyZXIgZmFkZXMgYmFjayBpbnRvIHRoZSBzaWRlIHRyYWlsLicpXG4gICAgcmV0dXJuIHJlc29sdmUoc3RhdGUsIHNvdXJjZSwgJ2xlYXZlJywgW2V2ZW50KCdtZW51JyldKVxuICB9XG4gIGlmIChzb3VyY2Uua2luZCA9PT0gJ2Jsb29kQmFyZ2FpbicpIHtcbiAgICBpZiAoaW5kZXggPT09IDApIHsgc3RhdGUuaGVyby5tYXhIZWFsdGggLT0gNDsgc3RhdGUuaGVyby5oZWFsdGggPSBNYXRoLm1pbihzdGF0ZS5oZXJvLmhlYWx0aCwgc3RhdGUuaGVyby5tYXhIZWFsdGgpOyBncmFudENvbnRleHRHb2xkKHN0YXRlLCA3NSk7IGNvbnN0IHJld2FyZCA9IHJld2FyZEZvcihzdGF0ZSwgc291cmNlKTsgZ3JhbnRJdGVtKHN0YXRlLCByZXdhcmQpOyBsb2coc3RhdGUsIGBUaGUgYmFyZ2FpbiB0YWtlcyB2aXRhbGl0eSBhbmQgbGVhdmVzICR7SVRFTVtyZXdhcmRdLm5hbWV9LmApOyByZXR1cm4gcmVzb2x2ZShzdGF0ZSwgc291cmNlLCAndml0YWxpdHknLCBhZHZhbmNlKHN0YXRlLCBbZXZlbnQoJ3BpY2t1cCcpXSkpIH1cbiAgICBpZiAoaW5kZXggPT09IDEpIHsgc3RhdGUuaGVyby5mb2N1cyAtPSAzOyBncmFudENvbnRleHRHb2xkKHN0YXRlLCAzMCk7IHN0YXRlLmZsb29yLnRpbGVzLmZvckVhY2godGlsZSA9PiB7IHRpbGUuZXhwbG9yZWQgPSB0cnVlIH0pOyByZWZyZXNoRm92KHN0YXRlKTsgbG9nKHN0YXRlLCAnVGhlIGJhcmdhaW4gZHJpbmtzIGZvY3VzIGFuZCBleHBvc2VzIHRoZSB0cmFpbC4nKTsgcmV0dXJuIHJlc29sdmUoc3RhdGUsIHNvdXJjZSwgJ2ZvY3VzJywgYWR2YW5jZShzdGF0ZSwgW2V2ZW50KCdzcGVsbCcpXSkpIH1cbiAgICBsb2coc3RhdGUsICdUaGUgc2VhbGVkIGJhcmdhaW4gcmVtYWlucyB1bm9wZW5lZC4nKVxuICAgIHJldHVybiByZXNvbHZlKHN0YXRlLCBzb3VyY2UsICdkZWNsaW5lJywgW2V2ZW50KCdtZW51JyldKVxuICB9XG4gIGlmIChpbmRleCA9PT0gMCkge1xuICAgIHN0YXRlLmhlcm8uZm9jdXMgLT0gMlxuICAgIGNvbnN0IG11dGFibGUgPSBuZXcgU2V0KFsncnViYmxlJywgJ2JyYW1ibGUnLCAnYm91bGRlcicsICdicmVha3dhbGwnLCAncGl0JywgJ3dhdGVyJywgJ2RlZXBXYXRlcicsICdjdXJyZW50JywgJ2dhcycsICdzbW9rZScsICdmaXJlVmVudCddKVxuICAgIGNvbnN0IGNlbGxzID0gY2hhbWJlckNlbGxzKHN0YXRlLCBzb3VyY2UsIDIpLmZpbHRlcihjZWxsID0+IG11dGFibGUuaGFzKGNlbGwudGlsZS5raW5kKSlcbiAgICBjZWxscy5mb3JFYWNoKGNlbGwgPT4geyBjZWxsLnRpbGUua2luZCA9ICdmbG9vcicgfSlcbiAgICByZWZyZXNoRm92KHN0YXRlKVxuICAgIGxvZyhzdGF0ZSwgYFRoZSBjaGFtYmVyIG9wZW5zICR7Y2VsbHMubGVuZ3RofSBuZWFyYnkgY2VsbHMuYClcbiAgICByZXR1cm4gcmVzb2x2ZShzdGF0ZSwgc291cmNlLCAnb3BlbicsIGFkdmFuY2Uoc3RhdGUsIFtldmVudCgnc3BlbGwnKV0pKVxuICB9XG4gIGlmIChpbmRleCA9PT0gMSkge1xuICAgIGNvbnN0IGNlbGxzID0gY2hhbWJlckNlbGxzKHN0YXRlLCBzb3VyY2UsIDIpLmZpbHRlcihjZWxsID0+IGNlbGwudGlsZS5raW5kID09PSAnZmxvb3InKS5zbGljZSgwLCA1KVxuICAgIGNlbGxzLmZvckVhY2goY2VsbCA9PiB7IGNlbGwudGlsZS5raW5kID0gJ2N1cnJlbnQnIH0pXG4gICAgZ3JhbnRDb250ZXh0R29sZChzdGF0ZSwgOTApXG4gICAgcmVmcmVzaEZvdihzdGF0ZSlcbiAgICBsb2coc3RhdGUsIGBUaGUgY2hhbWJlciBzdXJnZXMgdGhyb3VnaCAke2NlbGxzLmxlbmd0aH0gbmVhcmJ5IGNlbGxzLmApXG4gICAgcmV0dXJuIHJlc29sdmUoc3RhdGUsIHNvdXJjZSwgJ3N1cmdlJywgYWR2YW5jZShzdGF0ZSwgW2V2ZW50KCdzcGVsbCcpXSkpXG4gIH1cbiAgbG9nKHN0YXRlLCAnVGhlIGNoYW1iZXIgc2V0dGxlcyB3aXRob3V0IGNoYW5naW5nIGl0cyBzaGFwZS4nKVxuICByZXR1cm4gcmVzb2x2ZShzdGF0ZSwgc291cmNlLCAnbGVhdmUnLCBbZXZlbnQoJ21lbnUnKV0pXG59XG4iXSwibWFwcGluZ3MiOiJBQUFBLFNBQVNBLElBQUksUUFBUSxZQUFZO0FBRWpDLFNBQVNDLG9CQUFvQixRQUFRLGNBQWM7QUFDbkQsU0FBU0MsT0FBTyxRQUFRLFVBQVU7QUFDbEMsU0FBU0MsU0FBUyxRQUFRLFdBQVc7QUFDckMsU0FBU0MsS0FBSyxFQUFFQyxHQUFHLFFBQTJCLFVBQVU7QUFDeEQsU0FBU0MsVUFBVSxRQUFRLGNBQWM7QUFDekMsU0FBU0MsNEJBQTRCLEVBQUVDLFFBQVEsRUFBRUMsT0FBTyxRQUFRLGNBQWM7QUFDOUUsU0FBU0MseUJBQXlCLFFBQVEsVUFBVTtBQUNwRCxTQUFTQyxZQUFZLFFBQVEsY0FBYztBQUMzQyxTQUFTQyxJQUFJLFFBQVEsYUFBYTtBQUNsQyxTQUFTQyxzQkFBc0IsRUFBRUMsb0JBQW9CLFFBQVEsb0JBQW9CO0FBSWpGLE1BQU1DLHVCQUF1QixHQUFHLENBQUMsWUFBWSxFQUFFLGNBQWMsRUFBRSxXQUFXLEVBQUUsWUFBWSxFQUFFLFdBQVcsRUFBRSxXQUFXLEVBQUUsU0FBUyxFQUFFLGFBQWEsRUFBRSxjQUFjLEVBQUUsWUFBWSxFQUFFLFVBQVUsRUFBRSxnQkFBZ0IsQ0FBVTtBQUtsTixNQUFNQyxpQkFBbUUsR0FBRztFQUMxRUMsVUFBVSxFQUFFO0lBQUVDLEtBQUssRUFBRSxhQUFhO0lBQUVDLElBQUksRUFBRSxRQUFRO0lBQUVDLEtBQUssRUFBRSxDQUFDO0lBQUVDLElBQUksRUFBRSxTQUFTO0lBQUVDLE1BQU0sRUFBRSxTQUFTO0lBQUVDLElBQUksRUFBRSxHQUFHO0lBQUVDLElBQUksRUFBRSxTQUFTO0lBQUVDLE9BQU8sRUFBRTtFQUFRLENBQUM7RUFDaEpDLFlBQVksRUFBRTtJQUFFUixLQUFLLEVBQUUsZUFBZTtJQUFFQyxJQUFJLEVBQUUsT0FBTztJQUFFQyxLQUFLLEVBQUUsQ0FBQztJQUFFQyxJQUFJLEVBQUUsV0FBVztJQUFFQyxNQUFNLEVBQUUsT0FBTztJQUFFQyxJQUFJLEVBQUUsRUFBRTtJQUFFQyxJQUFJLEVBQUUsS0FBSztJQUFFQyxPQUFPLEVBQUU7RUFBUSxDQUFDO0VBQzlJRSxTQUFTLEVBQUU7SUFBRVQsS0FBSyxFQUFFLFlBQVk7SUFBRUMsSUFBSSxFQUFFLFdBQVc7SUFBRUMsS0FBSyxFQUFFLENBQUM7SUFBRUMsSUFBSSxFQUFFLFdBQVc7SUFBRUMsTUFBTSxFQUFFLFlBQVk7SUFBRUMsSUFBSSxFQUFFLEdBQUc7SUFBRUMsSUFBSSxFQUFFLE9BQU87SUFBRUMsT0FBTyxFQUFFO0VBQVEsQ0FBQztFQUNwSkcsVUFBVSxFQUFFO0lBQUVWLEtBQUssRUFBRSxhQUFhO0lBQUVDLElBQUksRUFBRSxNQUFNO0lBQUVDLEtBQUssRUFBRSxDQUFDO0lBQUVTLElBQUksRUFBRSxPQUFPO0lBQUVSLElBQUksRUFBRSxZQUFZO0lBQUVDLE1BQU0sRUFBRSxPQUFPO0lBQUVDLElBQUksRUFBRSxFQUFFO0lBQUVDLElBQUksRUFBRSxTQUFTO0lBQUVDLE9BQU8sRUFBRTtFQUFRLENBQUM7RUFDN0pLLFNBQVMsRUFBRTtJQUFFWixLQUFLLEVBQUUsWUFBWTtJQUFFQyxJQUFJLEVBQUUsUUFBUTtJQUFFQyxLQUFLLEVBQUUsQ0FBQztJQUFFQyxJQUFJLEVBQUUsV0FBVztJQUFFQyxNQUFNLEVBQUUsV0FBVztJQUFFQyxJQUFJLEVBQUUsR0FBRztJQUFFQyxJQUFJLEVBQUUsUUFBUTtJQUFFQyxPQUFPLEVBQUU7RUFBUSxDQUFDO0VBQ2pKTSxTQUFTLEVBQUU7SUFBRWIsS0FBSyxFQUFFLFlBQVk7SUFBRUMsSUFBSSxFQUFFLE9BQU87SUFBRUMsS0FBSyxFQUFFLENBQUM7SUFBRUMsSUFBSSxFQUFFLFlBQVk7SUFBRUMsTUFBTSxFQUFFLFdBQVc7SUFBRUMsSUFBSSxFQUFFLEVBQUU7SUFB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