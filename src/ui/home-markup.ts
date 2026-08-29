import type { GameConfig } from '../core/types';
import type { LobbyConfig, RoomSnapshot } from '../net/protocol';
import type { GamePreferences } from '../preferences';
import { escapeHtml } from './markup';

export type HomePanel = 'play' | 'settings';
export type HomeMode = 'modes' | 'local' | 'local-multiplayer' | 'multiplayer' | 'host' | 'join';

export interface HomeView {
  panel: HomePanel;
  mode: HomeMode;
  config: LobbyConfig;
  preferences: GamePreferences;
  playerName: string;
  roomCode: string;
  serverUrl: string;
  connected: boolean;
  notice?: string;
}

const configFields = (config: LobbyConfig, prefix: string, minimumSeats = 1) => `<label>campaign seed <input id="${prefix}-seed" maxlength="32" value="${escapeHtml(config.seed)}"></label><div class="home-split"><label>holes <select id="${prefix}-holes">${[3, 6, 9, 18].map((count) => `<option value="${count}" ${config.holeCount === count ? 'selected' : ''}>${count}</option>`).join('')}</select></label><label>player seats <select id="${prefix}-seats">${Array.from({ length: 9 - minimumSeats }, (_, index) => index + minimumSeats).map((count) => `<option value="${count}" ${config.maxHumans === count ? 'selected' : ''}>${count}</option>`).join('')}</select></label></div><div class="home-split"><label>level width <input id="${prefix}-course-width" type="number" min="14" step="1" value="${config.courseWidth}"></label><label>level height <input id="${prefix}-course-height" type="number" min="10" step="1" value="${config.courseHeight}"></label></div><p class="hint">Large courses use the follow camera; online rooms must also stay within the host server's tile budget.</p><div class="home-split"><label>AI enemies <select id="${prefix}-bots">${Array.from({ length: 5 }, (_, index) => index).map((count) => `<option value="${count}" ${config.botCount === count ? 'selected' : ''}>${count}</option>`).join('')}</select></label><label>AI skill <select id="${prefix}-skill"><option value="adaptive" ${config.botSkill === 'adaptive' ? 'selected' : ''}>adaptive</option>${Array.from({ length: 10 }, (_, index) => index + 1).map((skill) => `<option value="${skill}" ${config.botSkill === skill ? 'selected' : ''}>${skill}</option>`).join('')}</select></label></div>`;

const settingsFields = (preferences: GamePreferences) => `<section class="home-settings panel"><header><p class="eyebrow">PERSISTENT CLIENT SETTINGS</p><h2>settings</h2></header><div class="settings-grid"><label class="setting-toggle"><input data-preference="reducedMotion" type="checkbox" ${preferences.reducedMotion ? 'checked' : ''}> reduced motion and flash</label><label class="setting-toggle"><input data-preference="highContrast" type="checkbox" ${preferences.highContrast ? 'checked' : ''}> high-contrast palette</label><label class="setting-toggle"><input data-preference="controllerVibration" type="checkbox" ${preferences.controllerVibration ? 'checked' : ''}> controller vibration</label><label class="setting-toggle"><input data-preference="showMerchantHoldings" type="checkbox" ${preferences.showMerchantHoldings ? 'checked' : ''}> show table cards and boons in merchant</label><p class="setting-note">mouse shots: drag with the left button to putt or the right button to chip, then release to strike</p><label>master volume <input data-preference-range="masterVolume" type="range" min="0" max="1" step="0.05" value="${preferences.masterVolume}"></label><label>effects volume <input data-preference-range="effectsVolume" type="range" min="0" max="1" step="0.05" value="${preferences.effectsVolume}"></label><label>controller deadzone <input data-preference-range="controllerDeadzone" type="range" min="0.05" max="0.5" step="0.01" value="${preferences.controllerDeadzone}"></label><label>controller aim sensitivity <input data-preference-range="controllerAimSensitivity" type="range" min="0.5" max="2" step="0.05" value="${preferences.controllerAimSensitivity}"></label></div><p class="hint">Controller menus: D-pad or left stick navigates, A selects, and B goes back. On the course: left stick aims, A shoots, B uses the held item, View opens game controls, and Menu/Start pauses.</p><button data-home-panel="play">back to clubhouse</button></section>`;

export const lobbyConfigFromGame = (config: GameConfig): LobbyConfig => ({ seed: config.seed, holeCount: config.holeCount, botCount: config.botCount, botSkill: config.botSkill, maxHumans: config.humanCount, courseWidth: config.courseWidth ?? 20, courseHeight: config.courseHeight ?? 14, skipDieBets: config.skipDieBets === true });

const modeCards = () => {
  const hole = (mode: HomeMode, label: string, detail: string, icon: string, x: string, y: string, chain: string) => `<button class="green-hole green-hole-${mode}" data-home-mode="${mode}" style="--hole-x:${x};--hole-y:${y};--chain-length:${chain}" aria-label="putt into the ${label} hole"><span class="green-hole-sign"><small>${detail}</small><strong>${label}</strong></span><span class="green-hole-flag" aria-hidden="true">${icon}</span><span class="green-hole-cup" aria-hidden="true"></span></button>`;
  return `<section class="clubhouse-green" data-home-green aria-label="choose a game mode by putting the ball into a cup"><p class="green-instructions">pick a cup to putt your ball into the next game</p><span id="clubhouse-ball" class="clubhouse-ball" aria-hidden="true"></span>${hole('local', 'couch campaign', 'solo or pass the club', '⚑', '20%', '68%', '15rem')}${hole('local-multiplayer', 'local multiplayer', '2+ golfers · one screen', '◎', '50%', '43%', '11rem')}${hole('multiplayer', 'multiplayer', 'private room · live table', '⌁', '80%', '68%', '15rem')}</section>`;
};

const localCard = (view: HomeView) => `<section class="setup-shell"><article class="home-card panel"><div class="card-back"><button data-home-mode="modes">← modes</button><p class="eyebrow">LOCAL PARTY</p></div><h2>couch campaign</h2><p>Pass the device between golfers. AI fills any enemy seats.</p>${configFields(view.config, 'local')}<div class="local-start-actions"><button class="primary" data-start-local>start local game</button><button class="quick-start-button" data-quick-start-local><span aria-hidden="true">⌁</span><span>quick start</span><small>automatic slot results</small></button></div><p class="hint">Quick start skips the wagering windows and uses seeded automatic slot results.</p></article></section>`;

const localMultiplayerCard = (view: HomeView) => `<section class="setup-shell"><article class="home-card panel"><div class="card-back"><button data-home-mode="modes">← modes</button><p class="eyebrow">LOCAL MULTIPLAYER</p></div><h2>shared-screen match</h2><p>Build a turn-based match for two or more golfers on one device.</p>${configFields(view.config, 'local-multiplayer', 2)}<div class="local-start-actions"><button class="primary" data-start-local-multiplayer>start shared match</button><button class="quick-start-button" data-quick-start-local-multiplayer><span aria-hidden="true">⌁</span><span>quick start together</span><small>automatic slot results</small></button></div><p class="hint">One shared active-ball camera follows the current turn on large courses, while the aim guide stays available for planning. Split-screen is not needed between turns.</p></article></section>`;

const multiplayerCards = () => `<section class="mode-grid"><article class="mode-card panel"><span aria-hidden="true">⌁</span><p class="eyebrow">HOST A ROOM</p><h2>open the clubhouse</h2><p>Choose the campaign rules, create a code, then invite your players into the lobby.</p><button class="primary" data-home-mode="host">host a room</button></article><article class="mode-card panel"><span aria-hidden="true">⌁</span><p class="eyebrow">JOIN A ROOM</p><h2>enter the clubhouse</h2><p>Use the room code shared by the host. The game configuration is locked by that lobby.</p><button class="primary" data-home-mode="join">join a room</button></article></section>`;

const hostCard = (view: HomeView) => `<section class="setup-shell"><article class="home-card panel"><div class="card-back"><button data-home-mode="multiplayer">← rooms</button><p class="eyebrow">HOST A ROOM</p></div><h2>open the clubhouse</h2><p>Choose the campaign rules, create a code, then invite your players into the lobby.</p><label>display name <input id="player-name" maxlength="16" value="${escapeHtml(view.playerName)}"></label><label>game server <input id="server-url" type="url" value="${escapeHtml(view.serverUrl)}" placeholder="ws://localhost:8787"></label>${configFields(view.config, 'online')}<button class="primary" data-create-room>create room</button><button data-create-quick-room>create quick-start room · automatic slots</button><p class="hint">Quick start skips wagers and uses seeded automatic slot results between holes.</p></article></section>`;

const joinCard = (view: HomeView) => `<section class="setup-shell"><article class="home-card panel"><div class="card-back"><button data-home-mode="multiplayer">← rooms</button><p class="eyebrow">JOIN A ROOM</p></div><h2>enter the clubhouse</h2><p>Use the room code shared by the host. The game configuration is locked by that lobby.</p><label>display name <input id="join-player-name" maxlength="16" value="${escapeHtml(view.playerName)}"></label><label>game server <input id="join-server-url" type="url" value="${escapeHtml(view.serverUrl)}" placeholder="ws://localhost:8787"></label><label>room code <input id="room-code" maxlength="6" value="${escapeHtml(view.roomCode)}" placeholder="ABC123"></label><button class="primary" data-join-room>join room</button><p class="hint">Need a code? Ask the host. The lobby shows who is connected before the match begins.</p></article></section>`;

const gearIcon = '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 15.2a3.2 3.2 0 1 0 0-6.4 3.2 3.2 0 0 0 0 6.4Z"/><path d="M19.4 13.5a7.7 7.7 0 0 0 .1-1.5 7.7 7.7 0 0 0-.1-1.5l2-1.5-2-3.4-2.4 1a8 8 0 0 0-2.6-1.5L14.1 2h-4l-.4 3.1a8 8 0 0 0-2.6 1.5l-2.4-1-2 3.4 2 1.5a7.7 7.7 0 0 0-.1 1.5c0 .5 0 1 .1 1.5l-2 1.5 2 3.4 2.4-1a8 8 0 0 0 2.6 1.5l.4 3.1h4l.4-3.1a8 8 0 0 0 2.6-1.5l2.4 1 2-3.4-2.1-1.5Z"/></svg>';
const homeTitle = '<h1>G<span class="title-golf-ball" role="img" aria-label="spinning golf ball"></span>LF <em>Y<span class="title-cursing-emoji" role="img" aria-label="angry face cursing">🤬</span>UR</em> ENEMIES</h1>';

export const renderHomeMarkup = (view: HomeView) => {
  const playContent = view.mode === 'local'
    ? localCard(view)
    : view.mode === 'local-multiplayer'
      ? localMultiplayerCard(view)
    : view.mode === 'host'
      ? hostCard(view)
      : view.mode === 'join'
        ? joinCard(view)
        : view.mode === 'multiplayer'
          ? multiplayerCards()
          : modeCards();
  return `<main class="home-shell"><button class="home-settings-button ${view.panel === 'settings' ? 'selected' : ''}" data-home-panel="settings" aria-label="open settings" aria-pressed="${view.panel === 'settings'}" title="settings">${gearIcon}</button><header class="home-hero home-sign">${homeTitle}</header>${view.panel === 'settings' ? settingsFields(view.preferences) : playContent}${view.notice ? `<p class="home-notice" role="status">${escapeHtml(view.notice)}</p>` : ''}</main>`;
};

export const renderMatchLaunchMarkup = (_launch: { quickStart: boolean; title: string; detail: string }) => `<main class="match-loading" aria-live="polite"><span class="match-loading-ball" role="img" aria-label="loading match"></span></main>`;

export const renderQuickStartLaunchMarkup = () => renderMatchLaunchMarkup({ quickStart: true, title: 'loading the course', detail: 'Preparing seeded automatic slot results for the round.' });

export const renderLobbyMarkup = (room: RoomSnapshot, playerId: string | undefined, connected: boolean, notice?: string) => {
  const memberRows = room.members.map((member) => `<li><i class="${member.connected ? 'connected' : ''}"></i><strong>${escapeHtml(member.name)}</strong><span>${member.host ? 'host' : `seat ${member.slot + 1}`}</span><small>${member.connected ? 'connected' : 'reconnecting'}</small></li>`).join('');
  const host = playerId === room.hostId;
  const selection = room.config.skipDieBets ? 'quick start · seeded automatic slot results' : 'shared course slots · wagers before every hole';
  const startLabel = room.config.skipDieBets ? 'start quick match' : 'start room';
  return `<main class="lobby-shell"><header><p class="eyebrow">ONLINE CLUBHOUSE ROOM</p><h1>room <em>${escapeHtml(room.code)}</em></h1><p>Share this code with your players. The host starts when everyone is connected.</p></header><section class="lobby-card panel"><div class="room-code"><span>ROOM CODE</span><strong>${escapeHtml(room.code)}</strong><small>${connected ? 'server connected' : 'reconnecting to server'}</small></div><h2>players ${room.members.length}/${room.config.maxHumans}</h2><ol class="lobby-members">${memberRows}</ol><dl class="lobby-rules"><dt>campaign</dt><dd>${room.config.holeCount} holes · ${room.config.courseWidth}×${room.config.courseHeight} tiles · seed ${escapeHtml(room.config.seed)}</dd><dt>selection</dt><dd>${selection}</dd><dt>AI</dt><dd>${room.config.botCount} enemies · ${escapeHtml(String(room.config.botSkill))} skill</dd><dt>authority</dt><dd>server timers, bots, scores, and pause control</dd></dl>${host ? `<button class="primary" data-start-room ${room.members.some((member) => !member.connected) ? 'disabled' : ''}>${startLabel}</button>` : '<p class="hint">Waiting for the host to start the campaign.</p>'}<button data-leave-lobby>leave room</button></section>${notice ? `<p class="home-notice" role="status">${escapeHtml(notice)}</p>` : ''}</main>`;
};
