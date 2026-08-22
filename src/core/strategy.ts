import { definitionFor } from './catalog';
import type { CardAttachment, GameState, Player, PocketCard, StrategyCard } from './types';

export const strategyDefinition = (card: PocketCard | StrategyCard) => definitionFor(typeof card === 'string' ? card : card.id);
export const isPlayerTargetedCard = (card: PocketCard | StrategyCard) => strategyDefinition(card)?.targetMode === 'player';

export const attachmentsFor = (player: Player) => (player.attachments ??= []);
export const hasAttachment = (player: Player, effect: StrategyCard) => attachmentsFor(player).some((attachment) => attachment.effect === effect);
export const attachmentFor = (player: Player, effect: StrategyCard) => attachmentsFor(player).find((attachment) => attachment.effect === effect);

const removeAttachment = (player: Player, effect: StrategyCard) => {
  const attachment = attachmentFor(player, effect);
  if (!attachment) return undefined;
  player.attachments = attachmentsFor(player).filter((candidate) => candidate.id !== attachment.id);
  return attachment;
};

export type AttachResult = { applied: boolean; target: Player; message?: string };

/** Applies all boon/curse protection and keeps attachment state bounded and visible. */
export const attachStrategyCard = (state: GameState, caster: Player, initialTarget: Player, card: PocketCard): AttachResult => {
  const definition = strategyDefinition(card);
  if (!definition?.timing || !definition.polarity || definition.targetMode !== 'player') return { applied: false, target: initialTarget };
  if (card.id === 'clean slate') {
    const curse = attachmentsFor(initialTarget).find((attachment) => attachment.polarity === 'curse');
    if (!curse) return { applied: false, target: initialTarget, message: `${initialTarget.name} has no curse to clear` };
    initialTarget.attachments = attachmentsFor(initialTarget).filter((attachment) => attachment.id !== curse.id);
    return { applied: true, target: initialTarget, message: `${caster.name} clears ${curse.cardId} from ${initialTarget.name}` };
  }

  let target = initialTarget;
  if (definition.polarity === 'boon' && hasAttachment(target, 'black pennant')) return { applied: false, target, message: `${target.name}'s black pennant rejects the boon` };
  if (definition.polarity === 'curse' && removeAttachment(target, 'umbrella cart')) return { applied: true, target, message: `${target.name}'s umbrella cart blocks ${card.id}` };
  if (definition.polarity === 'curse' && removeAttachment(target, 'mirror caddy')) {
    target = caster;
    if (hasAttachment(target, 'umbrella cart')) {
      removeAttachment(target, 'umbrella cart');
      return { applied: true, target, message: `${initialTarget.name}'s mirror caddy reflects and blocks ${card.id}` };
    }
  }

  const duration = card.duration ?? (definition.timing === 'putt' ? { unit: 'putt' as const, amount: 1 } : undefined);
  if (!duration) return { applied: false, target };
  const current = attachmentsFor(target);
  const existing = current.find((attachment) => attachment.effect === card.id);
  if (existing) {
    existing.remaining = Math.max(existing.remaining, duration.amount);
    existing.casterId = caster.id;
    return { applied: true, target, message: `${caster.name} refreshes ${card.id} on ${target.name}` };
  }
  if (current.length >= 4) return { applied: false, target, message: `${target.name} cannot carry more than four card effects` };
  const attachment: CardAttachment = {
    id: `effect-${++state.cardSequence}`,
    cardId: card.id as StrategyCard,
    effect: card.id as StrategyCard,
    casterId: caster.id,
    polarity: definition.polarity as 'boon' | 'curse',
    unit: duration.unit,
    remaining: duration.amount,
  };
  target.attachments = [...current, attachment];
  return { applied: true, target, message: `${caster.name} plays ${card.id} on ${target.name}` };
};

export const consumePuttAttachments = (player: Player) => {
  player.attachments = attachmentsFor(player).filter((attachment) => attachment.unit !== 'putt');
};

/** A round belongs to its caster: timers tick when that player returns to the tee. */
export const tickRoundAttachments = (state: GameState, casterId: string) => {
  state.players.forEach((player) => {
    player.attachments = attachmentsFor(player).flatMap((attachment) => {
      if (attachment.unit !== 'round' || attachment.casterId !== casterId) return [attachment];
      const remaining = attachment.remaining - 1;
      return remaining > 0 ? [{ ...attachment, remaining }] : [];
    });
  });
};

/** Hole effects pay out first, then consume one covered hole. */
export const resolveHoleAttachments = (state: GameState) => {
  state.players.forEach((player) => {
    attachmentsFor(player).filter((attachment) => attachment.unit === 'hole').forEach((attachment) => {
      if (attachment.effect === 'sponsor tab') player.cash += 2;
      if (attachment.effect === 'relay fund') player.cash += 4;
      if (attachment.effect === 'bogey tax') player.cash = Math.max(0, player.cash - 2);
    });
    player.attachments = attachmentsFor(player).flatMap((attachment) => {
      if (attachment.unit !== 'hole') return [attachment];
      const remaining = attachment.remaining - 1;
      return remaining > 0 ? [{ ...attachment, remaining }] : [];
    });
  });
};

export const attachmentLabel = (attachment: CardAttachment) => `${attachment.cardId} · ${attachment.remaining} ${attachment.unit}${attachment.remaining === 1 ? '' : 's'}`;
