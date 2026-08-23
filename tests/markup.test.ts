import { describe, expect, it } from 'vitest';
import { applyCommand, createGame, defaultConfig } from '../src/core/game';
import { openShop } from '../src/core/shop';
import { renderAppMarkup, renderControlsMarkup } from '../src/ui/markup';
import { defaultPreferences } from '../src/preferences';
import { lobbyConfigFromGame, renderHomeMarkup, renderLobbyMarkup } from '../src/ui/home-markup';

describe('voting overlay markup', () => {
  it('keeps the clubhouse focused on choosing local or multiplayer before exposing setup forms', () => {
    const state = createGame(defaultConfig());
    const markup = renderHomeMarkup({ panel: 'play', mode: 'modes', config: lobbyConfigFromGame(state.config), preferences: defaultPreferences(), playerName: 'golfer-1', roomCode: '', serverUrl: 'ws://localhost:8787', connected: false });
    expect(markup).toContain('data-home-mode="local"');
    expect(markup).toContain('data-home-mode="multiplayer"');
    expect(markup).not.toContain('id="local-seed"');
    expect(markup).not.toContain('id="online-seed"');
  });

  it('shows host and join choices before rendering either detailed room form', () => {
    const state = createGame(defaultConfig());
    const view = { panel: 'play' as const, config: lobbyConfigFromGame(state.config), preferences: defaultPreferences(), playerName: 'golfer-1', roomCode: '', serverUrl: 'ws://localhost:8787', connected: false };
    const choices = renderHomeMarkup({ ...view, mode: 'multiplayer' });
    expect(choices).toContain('data-home-mode="host"');
    expect(choices).toContain('data-home-mode="join"');
    expect(choices).not.toContain('id="online-seed"');
    expect(choices).not.toContain('id="join-player-name"');

    const host = renderHomeMarkup({ ...view, mode: 'host' });
    expect(host).toContain('id="online-seed"');
    expect(host).toContain('id="online-course-width"');
    expect(host).toContain('id="online-course-height"');
    expect(host).not.toContain('max="24"');
    expect(host).not.toContain('max="16"');
    expect(host).toContain('data-create-quick-room');
    expect(host).not.toContain('id="join-player-name"');

    const join = renderHomeMarkup({ ...view, mode: 'join' });
    expect(join).toContain('id="join-player-name"');
    expect(join).toContain('id="join-server-url"');
    expect(join).toContain('id="room-code"');
    expect(join).not.toContain('id="online-seed"');
  });

  it('offers local quick start and reports a no-vote room rule to online guests', () => {
    const state = createGame(defaultConfig());
    const local = renderHomeMarkup({ panel: 'play', mode: 'local', config: lobbyConfigFromGame(state.config), preferences: defaultPreferences(), playerName: 'golfer-1', roomCode: '', serverUrl: 'ws://localhost:8787', connected: false });
    expect(local).toContain('data-quick-start-local');
    expect(local).toContain('randomly locks all configured holes');
    const lobby = renderLobbyMarkup({ code: 'ABC123', hostId: 'human-0', config: { ...lobbyConfigFromGame({ ...state.config, skipVoting: true }), skipVoting: true }, members: [{ id: 'human-0', name: 'golfer-1', slot: 0, connected: true, host: true }], phase: 'lobby', updatedAt: 0 }, 'human-0', true);
    expect(lobby).toContain('every hole randomly locked, no voting');
    expect(lobby).toContain('start quick match');
  });

  it('keeps the ballot in the foreground with three icon-rich clickable package cards and vote pills', () => {
    const state = createGame({ ...defaultConfig(), seed: 'overlay-markup', humanCount: 1, botCount: 1 });
    const markup = renderAppMarkup({ state, config: state.config, preferences: defaultPreferences(), overlay: undefined, drawer: undefined, aim: { angle: 0, power: 4 }, shotInFlight: false, multiplayer: { online: false, connected: false, host: true }, ledger: [], callouts: [] });
    expect(markup).toContain('class="vote-overlay"');
    expect(markup).toContain('class="vote-card-grid"');
    expect(markup.match(/class="vote-card /g)).toHaveLength(3);
    expect(markup).toContain('class="course-glyphs"');
    expect(markup).toContain('title="course dimensions"');
    expect(markup).toContain('air hazards: updrafts and low bars');
    expect(markup).toContain('air rings');
    expect(markup).toContain('data-vote-option="hole-1-option-1"');
    expect(markup).toContain('class="vote-pills"');
    expect(markup).toContain('app-shell voting');
  });

  it('renders target selection and click-confirmed gadget placement guidance for new items', () => {
    const state = createGame({ ...defaultConfig(), seed: 'item-markup', humanCount: 1, botCount: 1 });
    state.status = 'playing';
    state.vote = undefined;
    state.players[0]!.inventory = 'sandbag';
    state.players[0]!.spareInventory = 'popper pad';
    const markup = renderControlsMarkup({ state, config: state.config, preferences: defaultPreferences(), overlay: undefined, drawer: undefined, aim: { angle: 0, power: 4 }, placement: { kind: 'popper pad', point: { x: 4, y: 3 }, valid: true, confirmed: true }, shotInFlight: false, multiplayer: { online: false, connected: false, host: true }, ledger: [], callouts: [] });
    expect(markup).toContain('id="powerup-target"');
    expect(markup).toContain('data-use-powerup="popper pad"');
    expect(markup).toContain('click again or press Enter to place');
  });

  it('renders visible player targets, instance durations, and attached strategy cards without blocking the putt', () => {
    const state = createGame({ ...defaultConfig(), seed: 'strategy-markup', humanCount: 1, botCount: 1 });
    state.status = 'playing';
    state.vote = undefined;
    state.players[0]!.pockets = [{ id: 'tailwind', source: 'shop', instanceId: 'tailwind-1' }];
    state.players[1]!.attachments = [{ id: 'effect-1', cardId: 'windbreak', effect: 'windbreak', casterId: state.players[0]!.id, polarity: 'boon', unit: 'round', remaining: 2 }];
    state.turn.cardPlayed = true;
    const markup = renderControlsMarkup({ state, config: state.config, preferences: defaultPreferences(), overlay: undefined, drawer: undefined, aim: { angle: 0, power: 4 }, shotInFlight: false, multiplayer: { online: false, connected: false, host: true }, ledger: [], callouts: [] });
    expect(markup).toContain('play on');
    expect(markup).toContain('human-0">golfer-1 (self)');
    expect(markup).toContain('data-card-id="tailwind-1"');
    expect(markup).toContain('one card committed this turn');
    expect(markup).toContain('table cards');
    expect(markup).toContain('windbreak (2 rounds)');
    expect(markup).toMatch(/id="shoot" class="primary"(?! disabled)/);
  });

  it('renders the original clubhouse merchant with a shared seven-card shelf and table vote', () => {
    const state = createGame({ ...defaultConfig(), seed: 'merchant-markup', holeCount: 1, humanCount: 2, botCount: 0, skipVoting: true });
    openShop(state);
    const markup = renderAppMarkup({ state, config: state.config, preferences: defaultPreferences(), overlay: undefined, drawer: undefined, aim: { angle: 0, power: 4 }, shotInFlight: false, multiplayer: { online: false, connected: false, host: true }, ledger: [], callouts: [] });
    expect(markup).toContain('class="merchant-overlay"');
    expect(markup).toContain('THE NINETEENTH HOLE · SHARED MARKET');
    expect(markup.match(/class="merchant-card /g)).toHaveLength(7);
    expect(markup).toContain('Caddies stack');
    expect(markup).toContain('data-shop-reroll="yes"');
    expect(markup).toContain('class="merchant-ledger"');
  });

  it('shows a strategy card’s rolled duration before the merchant purchase', () => {
    const state = createGame({ ...defaultConfig(), seed: 'merchant-duration', holeCount: 1, humanCount: 1, botCount: 0, skipVoting: true });
    openShop(state);
    state.shop!.shelf[0] = { id: 'fairway-draft', contentId: 'fairway draft', category: 'pocket', price: 5, duration: { unit: 'round', amount: 3 } };
    const markup = renderAppMarkup({ state, config: state.config, preferences: defaultPreferences(), overlay: undefined, drawer: undefined, aim: { angle: 0, power: 4 }, shotInFlight: false, multiplayer: { online: false, connected: false, host: true }, ledger: [], callouts: [] });
    expect(markup).toContain('fairway draft');
    expect(markup).toContain('boon · 3 rounds');
  });

  it('offers distinct putt and chip controls with an explicit chip cue', () => {
    const state = createGame({ ...defaultConfig(), seed: 'shot-modes', humanCount: 1, botCount: 0 });
    state.status = 'playing';
    state.vote = undefined;
    const markup = renderControlsMarkup({ state, config: state.config, preferences: defaultPreferences(), overlay: undefined, drawer: undefined, aim: { angle: 0, power: 4, kind: 'chip' }, shotInFlight: false, multiplayer: { online: false, connected: false, host: true }, ledger: [], callouts: [] });
    expect(markup).toContain('data-shot-kind="putt"');
    expect(markup).toContain('data-shot-kind="chip"');
    expect(markup).toContain('clear walls · C');
    expect(markup).toContain('chip power');
    expect(markup).toContain('scroll course · click a cell');
    expect(markup.match(/data-power="/g)).toHaveLength(15);
    expect(markup.match(/power-cell active/g)).toHaveLength(7);
    const inspector = renderAppMarkup({ state, config: state.config, preferences: defaultPreferences(), overlay: undefined, drawer: 'intel', aim: { angle: 0, power: 4 }, shotInFlight: false, multiplayer: { online: false, connected: false, host: true }, ledger: [], callouts: [] });
    expect(inspector).toContain('blue: slow mover');
    expect(inspector).toContain('amber: standard mover');
    expect(inspector).toContain('red: fast mover');
    expect(markup).not.toContain('COURSE INTEL');
    const cursorControls = renderControlsMarkup({ state, config: state.config, preferences: { ...defaultPreferences(), mousePowerMode: 'cursor' }, overlay: undefined, drawer: undefined, aim: { angle: 0, power: 4 }, shotInFlight: false, multiplayer: { online: false, connected: false, host: true }, ledger: [], callouts: [] });
    expect(cursorControls).toContain('move cursor to set power · click a cell');
    const settings = renderAppMarkup({ state, config: state.config, preferences: defaultPreferences(), overlay: 'settings', drawer: undefined, aim: { angle: 0, power: 4 }, shotInFlight: false, multiplayer: { online: false, connected: false, host: true }, ledger: [], callouts: [] });
    expect(settings).toContain('data-preference-select="mousePowerMode"');
  });

  it('removes the loading overlay after the full match plan is selected', () => {
    let state = createGame({ ...defaultConfig(), seed: 'plan-markup', holeCount: 1, humanCount: 1, botCount: 1 });
    const optionId = state.vote!.options[0]!.id;
    state = state.players.reduce((next, player) => applyCommand(next, { type: 'cast-vote', playerId: player.id, optionId }), state);
    const markup = renderAppMarkup({ state, config: state.config, preferences: defaultPreferences(), overlay: undefined, drawer: undefined, aim: { angle: 0, power: 4 }, shotInFlight: false, multiplayer: { online: false, connected: false, host: true }, ledger: [], callouts: [] });
    expect(state.status).toBe('playing');
    expect(markup).not.toContain('assembly-overlay');
    expect(markup).not.toContain('assembly-progress');
    expect(markup).not.toContain('app-shell assembling');
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
    expect(markup).toContain('complete course route remains visible');
  });
});
