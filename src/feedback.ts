export type FeedbackTone = 'neutral' | 'info' | 'good' | 'danger' | 'chaos';

export interface FeedbackPresentation {
  tone: FeedbackTone;
  major: boolean;
}

export const presentFeedback = (message: string): FeedbackPresentation => {
  const value = message.toLowerCase();
  if (/(sink|campaign complete|drafts |winner)/.test(value)) return { tone: 'good', major: true };
  if (/(void|timeout|freeze|bomb)/.test(value)) return { tone: 'danger', major: true };
  if (/(turbo|swap)/.test(value)) return { tone: 'chaos', major: true };
  if (/(locked|generated|rerolled|draft phase|tee off)/.test(value)) return { tone: 'info', major: false };
  return { tone: 'neutral', major: false };
};
