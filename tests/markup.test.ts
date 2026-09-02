import { describe, expect, it } from 'vitest';
import { createGame, defaultConfig } from '../src/core/game';
import { openShop } from '../src/core/shop';
import { renderAppMarkup, renderControlsMarkup } from '../src/ui/markup';
import { defaultPreferences } from '../src/preferences';
import { lobbyConfigFromGame, renderHomeMarkup, renderLobbyMarkup, renderMatchLaunchMarkup, renderQuickStartLaunchMarkup } from '../src/ui/home-markup';

describe('course shuffler markup', () => {
  it('keeps the title menu focused on choosing a play mode before exposing setup forms', () => {
    const state = createGame(defaultConfig());
    const markup = renderHomeMarkup({ panel: 'play', mode: 'modes', config: lobbyConfigFromGame(state.config), preferences: defaultPreferences(), playerName: 'golfer-1', roomCode: '', serverUrl: 'ws://localhost:8787', connected: false });
    expect(markup).toContain('data-home-mode="local"');
    expect(markup).toContain('data-home-mode="multiplayer"');
    expect(markup).toContain('home-settings-button');
    expect(markup).toContain('aria-label="open settings"');
    expect(markup).toContain('local play');
    expect(markup).toContain('GOLF <em>WITH YOUR</em> ENEMIES');
    expect(markup).toContain('class="title-menu panel"');
    expect(markup).toContain('choose local play');
    expect(markup).not.toContain('home-course-canvas');
    expect(markup).not.toContain('data-home-putt-target');
    expect(markup).toContain('up to eight golfers');
    expect(markup).not.toContain('ASCII ISOMETRIC MINI GOLF · ONLINE OR COUCH');
    expect(markup).not.toContain('Seeded nine-hole party golf with public ballots, chaos items, and server-authoritative online rooms.');
    expect(markup).not.toContain('id="local-seed"');
    expect(markup).not.toContain('local-multiplayer');
    expect(markup).not.toContain('id="online-seed"');
    const settings = renderHomeMarkup({ panel: 'settings', mode: 'modes', config: lobbyConfigFromGame(state.config), preferences: defaultPreferences(), playerName: 'golfer-1', roomCode: '', serverUrl: 'ws://localhost:8787', connected: false });
    expect(settings).toContain('data-preference="showPartyDiagnostics"');
    expect(settings).toContain('data-reset-party-guide');
  });

  it('uses one local setup for solo play and pass-and-play', () => {
    const state = createGame(defaultConfig());
    const markup = renderHomeMarkup({ panel: 'play', mode: 'local', config: lobbyConfigFromGame(state.config), preferences: defaultPreferences(), playerName: 'golfer-1', roomCode: '', serverUrl: 'ws://localhost:8787', connected: false });
    expect(markup).toContain('id="local-seed"');
    expect(markup).toContain('data-start-local');
    expect(markup).not.toContain('data-quick-start-local');
    expect(markup).toContain('solo play or add seats for pass-and-play');
    expect(markup).toContain('<option value="1" selected>1</option>');
    expect(markup).toContain('<option value="8" >8</option>');

    const game = renderAppMarkup({ state, config: state.config, preferences: defaultPreferences(), overlay: undefined, drawer: undefined, aim: { angle: 0, power: 4 }, shotInFlight: false, multiplayer: { online: false, connected: false, host: true }, ledger: [], callouts: [] });
    expect(game).toContain('LOCAL PLAY');
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
    expect(host).not.toContain('data-create-quick-room');
    expect(host).not.toContain('id="join-player-name"');

    const join = renderHomeMarkup({ ...view, mode: 'join' });
    expect(join).toContain('id="join-player-name"');
    expect(join).toContain('id="join-server-url"');
    expect(join).toContain('id="room-code"');
    expect(join).not.toContain('id="online-seed"');
  });

  it('reports the automatic pre-shuffled campaign rules to local and online players', () => {
    const state = createGame(defaultConfig());
    const local = renderHomeMarkup({ panel: 'play', mode: 'local', config: lobbyConfigFromGame(state.config), preferences: defaultPreferences(), playerName: 'golfer-1', roomCode: '', serverUrl: 'ws://localhost:8787', connected: false });
    expect(local).not.toContain('data-quick-start-local');
    expect(local).toContain('class="home-actions"');
    expect(local).toContain('start local game');
    expect(local).toContain('seed-shuffled automatically');
    expect(renderQuickStartLaunchMarkup()).toContain('class="match-loading-ball"');
    expect(renderQuickStartLaunchMarkup()).not.toContain('loading the course');
    const normalLaunch = renderMatchLaunchMarkup({ quickStart: false, title: 'building the opening hole', detail: 'Setting up players.' });
    expect(normalLaunch).toContain('class="match-loading-ball"');
    expect(normalLaunch).not.toContain('MATCH SETUP');
    const lobby = renderLobbyMarkup({ code: 'ABC123', hostId: 'human-0', config: lobbyConfigFromGame(state.config), members: [{ id: 'human-0', name: 'golfer-1', slot: 0, connected: true, host: true }], phase: 'lobby', updatedAt: 0 }, 'human-0', true);
    expect(lobby).toContain('seeded course shuffler');
    expect(lobby).toContain('start room');
  });

  it('removes the shared course slot machine and starts directly on a shuffled course', () => {
    const state = createGame({ ...defaultConfig(), seed: 'overlay-markup', humanCount: 1, botCount: 1 });
    const markup = renderAppMarkup({ state, config: state.config, preferences: defaultPreferences(), overlay: undefined, drawer: undefined, aim: { angle: 0, power: 4 }, shotInFlight: false, multiplayer: { online: false, connected: false, host: true }, ledger: [], callouts: [] });
    expect(state.status).toBe('playing');
    expect(markup).not.toContain('die-overlay');
    expect(markup).not.toContain('slot-machine');
    expect(markup).not.toContain('COURSE SLOTS');
    expect(markup).not.toContain('data-ready-slot-spin');
    expect(markup).toContain('chaos:');
  });

  it('renders target selection and click-confirmed gadget placement guidance for new items', () => {
    const state = createGame({ ...defaultConfig(), seed: 'item-markup', humanCount: 1, botCount: 1 });
    state.status = 'playing';
    state.players[0]!.inventory = 'sandbag';
    state.players[0]!.spareInventory = 'popper pad';
    const markup = renderControlsMarkup({ state, config: state.config, preferences: defaultPreferences(), overlay: undefined, drawer: undefined, aim: { angle: 0, power: 4 }, placement: { kind: 'popper pad', point: { x: 4, y: 3 }, valid: true, confirmed: true }, shotInFlight: false, multiplayer: { online: false, connected: false, host: true }, ledger: [], callouts: [] });
    expect(markup).toContain('id="powerup-target"');
    expect(markup).toContain('data-use-powerup="popper pad"');
    expect(markup).toContain('click again or press Enter to place');
  });

  it('renders visible player targets, instance durations, and attached strategy cards without blocking the putt', () => {
    // The old catalog is only needed for this rendering fixture. Build the fast
    // Party Rules course, then expose the legacy card controls under test.
    const state = createGame({ ...defaultConfig(), seed: 'strategy-markup', humanCount: 1, botCount: 1 });
    state.config.ruleset = 'custom';
    state.status = 'playing';
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
    expect(markup).not.toContain('id="shoot"');
  });

  it('renders the original clubhouse merchant with a shared seven-card shelf and table vote', () => {
    const state = createGame({ ...defaultConfig(), ruleset: 'custom', seed: 'merchant-markup', holeCount: 1, humanCount: 2, botCount: 0 });
    openShop(state);
    state.players[0]!.caddies = [{ id: 'heavy ball', stacks: 2 }];
    state.players[0]!.pockets = [{ id: 'future sight', source: 'shop', instanceId: 'future-sight-1', duration: { unit: 'round', amount: 2 } }];
    state.players[1]!.attachments = [{ id: 'windbreak-1', cardId: 'windbreak', effect: 'windbreak', casterId: state.players[0]!.id, polarity: 'boon', unit: 'round', remaining: 2 }];
    const markup = renderAppMarkup({ state, config: state.config, preferences: defaultPreferences(), overlay: undefined, drawer: undefined, aim: { angle: 0, power: 4 }, shotInFlight: false, multiplayer: { online: false, connected: false, host: true }, ledger: [], callouts: [] });
    expect(markup).toContain('class="merchant-overlay"');
    expect(markup).toContain('THE NINETEENTH HOLE · SHARED MARKET');
    expect(markup.match(/class="merchant-card /g)).toHaveLength(7);
    expect(markup.match(/class="merchant-card-icon"/g)).toHaveLength(7);
    expect(markup).toContain('Caddies stack');
    expect(markup).toContain('data-shop-reroll="yes"');
    expect(markup).toContain('class="merchant-ledger"');
    expect(markup).toContain('table holdings');
    expect(markup).toContain('<small>boons</small> heavy ball ×2');
    expect(markup).toContain('<small>cards</small> future sight (2 rounds)');
    expect(markup).toContain('<small>effects</small> windbreak (2 rounds)');

    const hidden = renderAppMarkup({ state, config: state.config, preferences: { ...defaultPreferences(), showMerchantHoldings: false }, overlay: undefined, drawer: undefined, aim: { angle: 0, power: 4 }, shotInFlight: false, multiplayer: { online: false, connected: false, host: true }, ledger: [], callouts: [] });
    expect(hidden).toContain('table ledger');
    expect(hidden).toContain('<span class="merchant-holdings hidden">holdings hidden</span>');
  });

  it('shows a strategy card’s rolled duration before the merchant purchase', () => {
    const state = createGame({ ...defaultConfig(), seed: 'merchant-duration', holeCount: 1, humanCount: 1, botCount: 0 });
    openShop(state);
    state.shop!.shelf[0] = { id: 'fairway-draft', contentId: 'fairway draft', category: 'pocket', price: 5, duration: { unit: 'round', amount: 3 } };
    const markup = renderAppMarkup({ state, config: state.config, preferences: defaultPreferences(), overlay: undefined, drawer: undefined, aim: { angle: 0, power: 4 }, shotInFlight: false, multiplayer: { online: false, connected: false, host: true }, ledger: [], callouts: [] });
    expect(markup).toContain('fairway draft');
    expect(markup).toContain('boon · 3 rounds');
  });

  it('renders direct left/right course pulls, a strength meter, and modern reaction controls', () => {
    const state = createGame({ ...defaultConfig(), seed: 'shot-modes', humanCount: 1, botCount: 0 });
    state.status = 'playing';
    const markup = renderAppMarkup({ state, config: state.config, preferences: defaultPreferences(), overlay: undefined, drawer: undefined, aim: { angle: 0, power: 4, kind: 'chip' }, shotInFlight: false, multiplayer: { online: false, connected: false, host: true }, ledger: [], callouts: [] });
    expect(markup).not.toContain('data-shot-kind="putt"');
    expect(markup).not.toContain('data-shot-kind="chip"');
    expect(markup).not.toContain('id="pull-ball"');
    expect(markup).not.toContain('DRAG BACK · RELEASE TO STRIKE');
    expect(markup).toContain('class="reaction-dock"');
    expect(markup).toContain('aria-label="send cheer"');
    expect(markup).toContain('++');
    expect(markup).toContain('class="hud-leaderboard"');
    expect(markup).toContain('class="hud-course"');
    expect(markup).toContain('class="hud-timer"');
    expect(markup).toContain('class="match-rail"');
    expect(markup).toContain('id="hud-strength-meter"');
    expect(markup).toContain('class="hud-camera"');
    expect(markup).toContain('class="route-role-key"');
    expect(markup).toContain('wide, steady route');
    expect(markup).toContain('right drag');
    expect(markup).toContain('data-camera-mode');
    expect(markup).toContain('data-camera-zoom="in"');
    expect(markup).not.toContain('social-receipts');
    expect(markup).not.toContain('power-cell');
    const inspector = renderAppMarkup({ state, config: state.config, preferences: defaultPreferences(), overlay: undefined, drawer: 'intel', aim: { angle: 0, power: 4 }, shotInFlight: false, multiplayer: { online: false, connected: false, host: true }, ledger: [], callouts: [] });
    expect(inspector).toContain('blue: slow mover');
    expect(inspector).toContain('amber: standard mover');
    expect(inspector).toContain('red: fast mover');
    expect(markup).not.toContain('COURSE INTEL');
    const settings = renderAppMarkup({ state, config: state.config, preferences: defaultPreferences(), overlay: 'settings', drawer: undefined, aim: { angle: 0, power: 4 }, shotInFlight: false, multiplayer: { online: false, connected: false, host: true }, ledger: [], callouts: [] });
    expect(settings).toContain('mouse shots: left-drag to putt or right-drag to chip, then release to strike');
    expect(settings).not.toContain('data-preference-select="mousePowerMode"');
    expect(settings).toContain('data-preference="showMerchantHoldings"');
  });

  it('does not render a die overlay for an already shuffled campaign', () => {
    const state = createGame({ ...defaultConfig(), seed: 'plan-markup', holeCount: 1, humanCount: 1, botCount: 1 });
    const markup = renderAppMarkup({ state, config: state.config, preferences: defaultPreferences(), overlay: undefined, drawer: undefined, aim: { angle: 0, power: 4 }, shotInFlight: false, multiplayer: { online: false, connected: false, host: true }, ledger: [], callouts: [] });
    expect(state.status).toBe('playing');
    expect(markup).not.toContain('die-overlay');
    expect(markup).not.toContain('app-shell rolling');
  }, 15_000);

  it('shows only the current contextual Party Rules guide step and supports a clean opt-out', () => {
    const state = createGame({ ...defaultConfig(), seed: 'party-guide', humanCount: 2, botCount: 0 });
    const guided = renderAppMarkup({ state, config: state.config, preferences: defaultPreferences(), overlay: undefined, drawer: undefined, aim: { angle: 0, power: 4 }, shotInFlight: false, multiplayer: { online: false, connected: false, host: true }, ledger: [], callouts: [] });
    const optedOut = renderAppMarkup({ state, config: state.config, preferences: { ...defaultPreferences(), partyGuideStep: 8 }, overlay: undefined, drawer: undefined, aim: { angle: 0, power: 4 }, shotInFlight: false, multiplayer: { online: false, connected: false, host: true }, ledger: [], callouts: [] });
    expect(guided).toContain('Play the problem together.');
    expect(guided).toContain('data-skip-party-guide');
    expect(optedOut).not.toContain('class="party-guide"');
  });

  it('renders a skippable course briefing and local handoff without changing shared rules', () => {
    const state = createGame({ ...defaultConfig(), seed: 'party-briefing', humanCount: 2, botCount: 0 });
    const briefing = renderAppMarkup({ state, config: state.config, preferences: { ...defaultPreferences(), partyGuideStep: 8 }, overlay: undefined, drawer: undefined, aim: { angle: 0, power: 4 }, shotInFlight: false, briefingHole: 1, multiplayer: { online: false, connected: false, host: true }, ledger: [], callouts: [] });
    const handoff = renderAppMarkup({ state, config: state.config, preferences: { ...defaultPreferences(), partyGuideStep: 8 }, overlay: undefined, drawer: undefined, aim: { angle: 0, power: 4 }, shotInFlight: false, handoff: { playerName: 'golfer-2', color: '#ffffff', hole: 1 }, multiplayer: { online: false, connected: false, host: true }, ledger: [], callouts: [] });
    expect(briefing).toContain('COURSE REVEAL');
    expect(briefing).toContain('data-dismiss-briefing');
    expect(briefing).toContain('safe line');
    expect(handoff).toContain('PASS THE DEVICE');
    expect(handoff).toContain('data-ready-handoff');
  }, 30_000);

  it('presents a winner, factual completion summary, podium, and final standings after the campaign', () => {
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
    expect(markup).toContain('complete the route');
    expect(markup).toContain('data-copy-replay');
    expect(markup).not.toContain('data-export-party-diagnostics');
    const diagnosticsMarkup = renderAppMarkup({ state, config: state.config, preferences: { ...defaultPreferences(), showPartyDiagnostics: true }, overlay: undefined, drawer: undefined, aim: { angle: 0, power: 4 }, shotInFlight: false, multiplayer: { online: false, connected: false, host: true }, ledger: [], callouts: [] });
    expect(diagnosticsMarkup).toContain('data-export-party-diagnostics');
    expect(markup).toContain('data-restart-run');
    expect(markup).toContain('app-shell finished');
    expect(markup).toContain('complete course route remains visible');
  });
});
