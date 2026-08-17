import { describe, expect, it } from 'vitest';
import { applyCommand, createGame, defaultConfig } from '../src/core/game';
import { renderAppMarkup } from '../src/ui/markup';
import { defaultPreferences } from '../src/preferences';

describe('voting overlay markup', () => {
  it('keeps the ballot in the foreground with three icon-rich clickable package cards and vote pills', () => {
    const state = createGame({ ...defaultConfig(), seed: 'overlay-markup', humanCount: 1, botCount: 1 });
    const markup = renderAppMarkup({ state, config: state.config, preferences: defaultPreferences(), overlay: undefined, drawer: undefined, aim: { angle: 0, power: 4 }, shotInFlight: false, multiplayer: { online: false, connected: false, host: true }, ledger: [], callouts: [] });
    expect(markup).toContain('class="vote-overlay"');
    expect(markup).toContain('class="vote-card-grid"');
    expect(markup.match(/class="vote-card /g)).toHaveLength(3);
    expect(markup).toContain('class="course-glyphs"');
    expect(markup).toContain('data-vote-option="hole-1-option-1"');
    expect(markup).toContain('class="vote-pills"');
    expect(markup).toContain('app-shell voting');
  });

  it('shows the selected course package while its arena is assembling', () => {
    let state = createGame({ ...defaultConfig(), seed: 'assembly-markup', humanCount: 1, botCount: 1 });
    const optionId = state.vote!.options[0]!.id;
    state = state.players.reduce((next, player) => applyCommand(next, { type: 'cast-vote', playerId: player.id, optionId }), state);
    const markup = renderAppMarkup({ state, config: state.config, preferences: defaultPreferences(), overlay: undefined, drawer: undefined, aim: { angle: 0, power: 4 }, shotInFlight: false, assemblyProgress: .5, multiplayer: { online: false, connected: false, host: true }, ledger: [], callouts: [] });
    expect(state.status).toBe('assembling');
    expect(markup).toContain('class="assembly-overlay"');
    expect(markup).toContain(state.assembly!.label);
    expect(markup).toContain('id="assembly-progress-fill"');
    expect(markup).toContain('app-shell assembling');
  });

  it('presents a winner, last place, podium, and final standings after the campaign', () => {
    const state = createGame({ ...defaultConfig(), seed: 'results-markup', humanCount: 1, botCount: 2 });
    state.status = 'finished';
    state.players[0]!.total = 17;
    state.players[1]!.total = 22;
    state.players[2]!.total = 28;
    const markup = renderAppMarkup({ state, config: state.config, preferences: defaultPreferences(), overlay: undefined, drawer: undefined, aim: { angle: 0, power: 4 }, shotInFlight: false, multiplayer: { online: false, connected: false, host: true }, ledger: [], callouts: [] });
    expect(markup).toContain('class="results-overlay"');
    expect(markup).toContain('Clubhouse champion');
    expect(markup).toContain('class="podium-card podium-place-1"');
    expect(markup).toContain('full standings');
    expect(markup).toContain('last place');
    expect(markup).toContain('data-restart-run');
    expect(markup).toContain('app-shell finished');
  });
});
