import type { Cell } from './model.ts';
export type RaiderKind = 'raider' | 'breacher';
export type CombatRef = { kind:'person'|'raider'|'structure'|'heart'; id:number };
export type Raider = Cell & { id:number; kind:RaiderKind; health:number; next:Cell|null; progress:number; route:Cell[]; routeVersion:number; retryAt:number; cooldown:number; target:CombatRef|null };
export type Attack = { id:number; kind:'melee'|'bolt'|'trap'; source:CombatRef; origin:Cell; target:Cell; cells:Cell[]; due:number; damage:number; faction:'colony'|'hostile' };
export type BattleOrder = { actor:number; mode:'rally'|'retreat'|'shelter'; target:Cell; resume:Cell|null };
export type Defense = { structure:number; cooldown:number };
export type Combat = { phase:'peace'|'warning'|'combat'|'recovery'; wave:number; warningAt:number; spawnAt:number; completed:number; edge:'north'|'east'|'south'|'west'; pending:RaiderKind[]; raiders:Raider[]; attacks:Attack[]; orders:BattleOrder[]; defenses:Defense[]; cooldowns:{actor:number;until:number}[]; stats:{spawned:number;kills:number;shots:number;traps:number;heartDamage:number;colonyDamage:number;structuresLost:number}; lootWood:number; status:string };
export type CombatCommand = { kind:'battle'; actor:number; mode:'rally'|'retreat'|'shelter'|'release'; target:Cell } | { kind:'reload'; structure:number };
