import type { CardDurationRange, CardPolarity, CardTargetMode, CardTiming, CaddyId, ChronoCard, ContentCategory, ContentId, GadgetKind, PowerUp, RealityCard, StrategyCard } from './types';

export interface ContentDefinition {
  id: ContentId;
  category: ContentCategory;
  icon: string;
  description: string;
  price: number;
  timing?: CardTiming;
  polarity?: CardPolarity;
  targetMode?: CardTargetMode;
  duration?: CardDurationRange;
}

const caddy = (id: CaddyId, icon: string, description: string): ContentDefinition => ({ id, category: 'caddy', icon, description, price: 6 });
const pocket = (id: PowerUp, icon: string, description: string): ContentDefinition => ({ id, category: 'pocket', icon, description, price: 4 });
const form = (id: PowerUp, icon: string, description: string): ContentDefinition => ({ id, category: 'form', icon, description, price: 4 });
const gadget = (id: GadgetKind, icon: string, description: string): ContentDefinition => ({ id, category: 'gadget', icon, description, price: 4 });
const reality = (id: RealityCard, icon: string, description: string): ContentDefinition => ({ id, category: 'reality', icon, description, price: 8 });
const chrono = (id: ChronoCard, icon: string, description: string): ContentDefinition => ({ id, category: 'chrono', icon, description, price: 10 });
const strategy = (id: StrategyCard, icon: string, description: string, timing: CardTiming, polarity: Exclude<CardPolarity, 'neutral'>, duration?: CardDurationRange): ContentDefinition => ({
  id, category: 'pocket', icon, description, price: timing === 'hole' ? 6 : timing === 'round' ? 5 : 4, timing, polarity, targetMode: 'player', duration,
});

export const CONTENT: readonly ContentDefinition[] = [
  caddy('heavy ball', '●', '+12% launch power and mass per stack'), caddy('ice skates', '⛸', 'slides farther on ice'), caddy('extra charge', '✚', 'refills an empty pocket after a shot'), caddy('bank shot', '↩', 'retains speed on wall rebounds'), caddy('hazard shield', '⬡', 'survives one void rebound'), caddy('chaos magnet', '🧲', 'may refill after a pocket card'), caddy('portal savvy', '◉', 'faster portal exits'), caddy('second wind', '↯', 'arms an extra putt'), caddy('scavenger', '▣', '+1 pocket capacity'), caddy('aerial ace', '⌒', 'longer chips'), caddy('cup reader', '◎', 'larger cup capture'), caddy('gadgeteer', '⚒', '+1 active gadget'),
  caddy('backboard', '▰', 'wall bounces gain speed'), caddy('pinball wizard', '✹', 'wall bounces pay cash'), caddy('rough rider', '♣', 'rough loses drag'), caddy('sand wedge', '⌁', 'sand kicks forward'), caddy('conveyor cultist', '➳', 'stronger terrain acceleration'), caddy('gatecrasher', '⊣', 'phases a closed gate'), caddy('thornmail', '✽', 'thorns kick toward the cup'), caddy('air mail', '◯', 'air rings pay cash'), caddy('shock absorber', '◌', 'resists knockback'), caddy('first responder', '✚', 'safer void recovery'), caddy('pickpocket', '▣', 'opponents picking items pays cash'), caddy('revenge club', '⚑', 'being hit empowers the next shot'), caddy('headwind', '≋', 'sinking slows opponents'), caddy('bogeyman', '☠', 'stronger while behind'), caddy('coin slot', '$', 'cash pads pay more'), caddy('broker', '¤', 'cheaper Caddies'), caddy('echo chamber', '∞', 'repeats the first trigger'), caddy('paradox partner', '⌛', 'one position rewind'), caddy('hole hunter', '◉', 'trick shots enlarge the cup'), caddy('black market caddy', '♠', 'contraband buys include a bonus card'), caddy('cushion keeper', '▤', 'cushion turf slows less per stack'), caddy('spring coach', '⌃', 'spring tiles launch higher per stack'), caddy('bumper apprentice', '◇', 'bumper banks retain more speed'), caddy('slope scout', '⛰', 'downhill pull weakens per stack'), caddy('wind warden', '〰', 'gust lanes push less per stack'),
  pocket('turbo', '↯', 'arm a powerful next shot'), pocket('shield', '⬡', 'survive a void fall'), pocket('bomb', '✹', 'blast a target ball'), pocket('freeze', '❄', 'skip a target turn'), pocket('swap', '⇄', 'swap balls with a target'), pocket('two putts', '2P', 'take an immediate extra putt'), pocket('cup magnet', '⊙', 'pull your ball toward the cup'), pocket('slipstream', '➳', 'reduce next-shot drag'), pocket('rebound rig', '↩', 'strengthen wall rebounds'), pocket('phase shift', '⇤', 'send a target backward'), pocket('sandbag', '▰', 'weaken a target shot'), pocket('rescue drone', '✈', 'advance along the route'), pocket('airhorn', '📣', 'force a target chip'), pocket('club flipper', '↻', 'reverse target controls'), pocket('time dilator', '⌛', 'slow a target next shot'), pocket('mugger', '♜', 'steal cash or a card'), pocket('scramble', '⇆', 'cycle every active ball'), pocket('gravity gloves', '☄', 'pull toward the cup'), pocket('bunker buster', '⛏', 'open a wall or gate'), pocket('portal remote', '◉', 'jump to a portal exit'), pocket('red tee', '⚑', 'set a recovery checkpoint'), pocket('black flag', '⚐', 'force a target to play next'), pocket('cherry bomb', '✹', 'blast all opponents'), pocket('copycat', '▣', 'copy a target pocket card'), pocket('wind sock', '〰', 'reverse the next gust lane for a player'), pocket('slope stabilizer', '⛰', 'flatten a player’s next downhill pull'), pocket('spring polish', '⌃', 'boost a player’s next spring launch'), pocket('bumper wax', '◇', 'strengthen a player’s next bumper bank'), pocket('cushion map', '▤', 'make cushion turf gentler for a player'),
  form('heavy', '●', 'huge mass for one shot'), form('bouncy', '◌', 'extra lively rebounds'), form('ghost', '◐', 'phase one obstacle'), form('magnet', '🧲', 'pull toward pads'), form('ice', '❄', 'long ice slides'), form('portal', '◉', 'choose a portal exit'), form('glider', '⌒', 'long airborne time'), form('sticky', '▣', 'brake and deaden rebounds'), form('orbit', '◎', 'larger cup capture'), form('quantum', '∞', 'keep the better branch'), form('mirror', '◇', 'redirect the first rebound'), form('anvil', '⬟', 'break an obstacle'), form('vampire', '☽', 'steal cash on collision'), form('boomerang', '↶', 'return after a void fall'),
  gadget('popper pad', '↑', 'pushes crossing balls'), gadget('snare patch', '⌁', 'slows crossing balls'), gadget('blast mine', '✹', 'explodes on contact'), gadget('slick patch', '≋', 'keeps balls rolling'), gadget('sky spring', '⌃', 'launches balls airborne'), gadget('gravity well', '◉', 'pulls nearby balls'), gadget('mirror plate', '◇', 'reflects crossing balls'), gadget('toll booth', '$', 'pays its owner when crossed'), gadget('control inverter', '↻', 'reverses crossing balls controls'), gadget('portal gun', '◉', 'places a temporary portal'),
  reality('wall is cup', '▰', 'wall contact sinks the hitter'), reality('void is fairway', '░', 'fill void with fairway'), reality('fairway is ice', '❄', 'all fairway slides'), reality('gravity is sideways', '↔', 'global lateral force'), reality('cup walks', '◉', 'cup steps down the route'), reality('everybody is ghost', '◐', 'everyone phases once'), reality('two is one', '∞', 'best of two shot branches'), reality('portals are plenty', '◉', 'portals choose exits'), reality('turns are backwards', '⇤', 'reverse turn order'), reality('gates are open', '⊣', 'gates cannot close'), reality('cups are many', '$', 'cash pads capture balls'), reality('ball is cup', '●', 'ball collisions can sink'), reality('bank holiday', '◇', 'every bumper and wall bank runs hot'), reality('spring fling', '⌃', 'booster lanes become spring tiles'), reality('high winds', '〰', 'gust lanes blow harder all hole'), reality('cushion league', '▤', 'sand bunkers become safe cushion turf'),
  chrono('undo drive', '↶', 'restore position; keep stroke'), chrono('second chance', '↺', 'restore position and stroke'), chrono('echo putt', '◌', 'repeat a prior trajectory'), chrono('future sight', '⌘', 'preview deterministic branches'), chrono('time theft', '⌛', 'take a bonus turn'), chrono('frozen frame', '❄', 'pause hazards for one shot'), chrono('parallel parking', '⇆', 'choose a legal landing branch'), chrono('grandfather clause', '⌫', 'rewind a target prior shot'),
  strategy('tailwind', '↯', 'give a player a powerful next putt', 'putt', 'boon'),
  strategy('guardian pin', '⬡', 'protect a player from the next void rebound', 'putt', 'boon'),
  strategy('line reader', '◎', 'pull a player’s next putt toward the cup', 'putt', 'boon'),
  strategy('soft landing', '▣', 'make a player’s next ball sticky', 'putt', 'boon'),
  strategy('ghost pass', '◐', 'phase a player through one obstacle', 'putt', 'boon'),
  strategy('mulligan relay', '2P', 'grant a player an extra putt after their next shot', 'putt', 'boon'),
  strategy('clean slate', '✚', 'remove one curse from a player', 'immediate', 'boon'),
  strategy('fairway draft', '➳', 'reduce drag for every putt this round', 'round', 'boon', { unit: 'round', min: 1, max: 3 }),
  strategy('banker advice', '↩', 'strengthen every wall rebound this round', 'round', 'boon', { unit: 'round', min: 1, max: 3 }),
  strategy('windbreak', '⬡', 'protect every putt from void this round', 'round', 'boon', { unit: 'round', min: 1, max: 3 }),
  strategy('steady hands', '◎', 'enlarge cup capture for every putt this round', 'round', 'boon', { unit: 'round', min: 1, max: 3 }),
  strategy('umbrella cart', '☂', 'block the next curse played on a player', 'round', 'boon', { unit: 'round', min: 1, max: 3 }),
  strategy('mirror caddy', '◇', 'reflect the next curse back to its caster', 'round', 'boon', { unit: 'round', min: 1, max: 3 }),
  strategy('sponsor tab', '$', 'pay a player $2 after each covered hole', 'hole', 'boon', { unit: 'hole', min: 1, max: 2 }),
  strategy('relay fund', '¤', 'pay $2 now so a player earns $4 after each covered hole', 'hole', 'boon', { unit: 'hole', min: 1, max: 2 }),
  strategy('gadgeteer favor', '⚒', 'let a player maintain one extra gadget this hole', 'hole', 'boon', { unit: 'hole', min: 1, max: 2 }),
  strategy('sandbag slip', '▰', 'weaken a player’s next putt', 'putt', 'curse'),
  strategy('club flip', '↻', 'reverse a player’s next putt controls', 'putt', 'curse'),
  strategy('slow clock', '⌛', 'halve a player’s next putt power', 'putt', 'curse'),
  strategy('forced chip', '⌒', 'force a player’s next shot into a chip', 'putt', 'curse'),
  strategy('headwind gust', '≋', 'weaken every putt this round', 'round', 'curse', { unit: 'round', min: 1, max: 3 }),
  strategy('frayed grip', '↻', 'reverse every putt control this round', 'round', 'curse', { unit: 'round', min: 1, max: 3 }),
  strategy('bogey tax', '$', 'take $2 after each covered hole', 'hole', 'curse', { unit: 'hole', min: 1, max: 2 }),
  strategy('black pennant', '⚐', 'prevent new boons on a player this hole', 'hole', 'curse', { unit: 'hole', min: 1, max: 2 }),
  strategy('anchor line', '⚓', 'anchor a player’s next putt against slopes and knockback', 'putt', 'boon'),
  strategy('spring ticket', '⌃', 'boost a player’s next spring launch', 'putt', 'boon'),
  strategy('cushion call', '▤', 'make cushion turf gentler for a player’s next putt', 'putt', 'boon'),
  strategy('bumper lease', '◇', 'boost a player’s next bumper bank', 'putt', 'boon'),
  strategy('shared draft', '⇄', 'give both golfers low drag this round', 'round', 'boon', { unit: 'round', min: 1, max: 3 }),
  strategy('wind sail', '〰', 'make gust lanes favor a player this round', 'round', 'boon', { unit: 'round', min: 1, max: 3 }),
  strategy('grounds crew', '⛰', 'soften slopes for a player this round', 'round', 'boon', { unit: 'round', min: 1, max: 3 }),
  strategy('rescue pact', '✚', 'protect both golfers from one void rebound this hole', 'hole', 'boon', { unit: 'hole', min: 1, max: 2 }),
  strategy('clubhouse pool', '$', 'pay both golfers $2 after this covered hole', 'hole', 'boon', { unit: 'hole', min: 1, max: 2 }),
  strategy('sticky forecast', '▤', 'make a player’s cushion turf grip harder next putt', 'putt', 'curse'),
  strategy('crosswind debt', '〰', 'turn gust lanes against a player this round', 'round', 'curse', { unit: 'round', min: 1, max: 3 }),
  strategy('dead bounce', '◇', 'deadens a player’s bumper banks this hole', 'hole', 'curse', { unit: 'hole', min: 1, max: 2 }),
] as const;

export const CONTENT_BY_ID: ReadonlyMap<ContentId, ContentDefinition> = new Map(CONTENT.map((definition) => [definition.id, definition]));
export const CADDIES = CONTENT.filter((definition): definition is ContentDefinition & { id: CaddyId } => definition.category === 'caddy');
export const SHOP_CATEGORIES: readonly ContentCategory[] = ['caddy', 'pocket', 'form', 'gadget', 'reality', 'chrono'];
export const definitionFor = (id: ContentId) => CONTENT_BY_ID.get(id);
