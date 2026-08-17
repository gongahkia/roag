import { describe, expect, it } from 'vitest';
import { createGame, defaultConfig } from '../src/core/game';
import { renderAppMarkup } from '../src/ui/markup';
import { defaultPreferences } from '../src/preferences';

describe('voting overlay markup', () => {
  it('keeps the ballot in the foreground with three icon-rich package cards and named vote controls', () => {
    const state = createGame({ ...defaultConfig(), seed: 'overlay-markup', humanCount: 1, botCount: 1 });
    const markup = renderAppMarkup({ state, config: state.config, preferences: defaultPreferences(), overlay: undefined, drawer: undefined, aim: { angle: 0, power: 4 }, shotInFlight: false, ledger: [], callouts: [] });
    expect(markup).toContain('class="vote-overlay"');
    expect(markup).toContain('class="vote-card-grid"');
    expect(markup.match(/class="vote-card /g)).toHaveLength(3);
    expect(markup).toContain('class="course-glyphs"');
    expect(markup).toContain('data-vote-player="human-0"');
    expect(markup).toContain('app-shell voting');
  });
});
