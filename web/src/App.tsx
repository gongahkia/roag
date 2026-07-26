import { useEffect, useMemo, useRef, useState } from "react";
import type { ChangeEvent } from "react";

import { AsciiField } from "./components/AsciiField";
import {
  actionKey,
  actionLabel,
  advanceAutomated,
  applyAction,
  createGame,
  deserializeGame,
  gameStateDigest,
  legalActions,
  restoreTimelineFrame,
  tileGlyph,
  undoLastHumanAction
} from "./game/engine";
import { exportGameReplay, loadLatestLocalGame, saveLocalGame } from "./game/persistence";
import { decideAction, DEFAULT_POLICY, loadPolicyArtifact } from "./game/policy";
import type { GameAction, GameState, PolicyArtifact } from "./game/types";

const INITIAL_SEED = "kenjaku-local-table-v1";
const AUTOPLAY_STEP_MS = 80;

function App() {
  const [game, setGame] = useState<GameState>(() => settle(createGame({ players: 4, humanSeat: 0, seed: INITIAL_SEED }), DEFAULT_POLICY));
  const [policy, setPolicy] = useState<PolicyArtifact>(DEFAULT_POLICY);
  const [policyStatus, setPolicyStatus] = useState("Built-in local policy active.");
  const [storageStatus, setStorageStatus] = useState("Loading local table history.");
  const [seed, setSeed] = useState(INITIAL_SEED);
  const [showAllHands, setShowAllHands] = useState(false);
  const [motionEnabled, setMotionEnabled] = useState(() => !window.matchMedia("(prefers-reduced-motion: reduce)").matches);
  const [autoplayEnabled, setAutoplayEnabled] = useState(false);
  const [replayIndex, setReplayIndex] = useState(0);
  const restored = useRef(false);
  const legal = useMemo(() => legalActions(game), [game]);
  const decision = useMemo(() => legal.length > 0 ? decideAction(game, legal, policy) : null, [game, legal, policy]);
  const digest = gameStateDigest(game);
  const lastEvent = game.history.at(-1);
  const topOpponent = 2;
  const leftOpponent = game.players === 4 ? 3 : 1;
  const rightOpponent = game.players === 4 ? 1 : null;
  const replayFrame = game.timeline[replayIndex] ?? null;

  useEffect(() => {
    let active = true;
    void loadPolicyArtifact().then((nextPolicy) => {
      if (!active) return;
      setPolicy(nextPolicy);
      setPolicyStatus(`${nextPolicy.name} ${nextPolicy.version} loaded from local site assets.`);
    }).catch(() => {
      if (active) setPolicyStatus("Policy asset unavailable; built-in local policy remains active.");
    });
    return () => { active = false; };
  }, []);

  useEffect(() => {
    let active = true;
    void loadLatestLocalGame().then((saved) => {
      if (!active) return;
      restored.current = true;
      if (saved !== null) {
        setGame(saved);
        setSeed(saved.seed);
        setStorageStatus(`Recovered local table ${saved.id}.`);
      } else {
        setStorageStatus("New local table. Changes save on this device.");
      }
    }).catch(() => {
      if (active) {
        restored.current = true;
        setStorageStatus("Local storage unavailable; this table remains in this tab.");
      }
    });
    return () => { active = false; };
  }, []);

  useEffect(() => {
    if (!restored.current) return;
    void saveLocalGame(game).then(() => {
      setStorageStatus(`Saved version ${game.version} locally.`);
    }).catch(() => {
      setStorageStatus("Could not persist this table locally.");
    });
  }, [game]);

  useEffect(() => {
    setReplayIndex(Math.max(0, game.timeline.length - 1));
  }, [game.version]);

  useEffect(() => {
    if (!autoplayEnabled || game.phase === "terminal") return;
    const timeout = window.setTimeout(() => {
      setGame((current) => advanceAutomated(current, (active, actions) => decideAction(active, actions, policy).selected.action, {
        includeHuman: true,
        limit: 1
      }));
    }, AUTOPLAY_STEP_MS);
    return () => window.clearTimeout(timeout);
  }, [autoplayEnabled, game.phase, game.version, policy]);

  useEffect(() => {
    if (game.phase === "terminal") setAutoplayEnabled(false);
  }, [game.phase]);

  function commit(action: GameAction) {
    if (autoplayEnabled) return;
    setGame((current) => settle(applyAction(current, action), policy));
  }

  function letPolicyCommit() {
    if (autoplayEnabled || decision === null) return;
    setGame((current) => settle(applyAction(current, decision.selected.action), policy));
  }

  function newGame(players: 3 | 4) {
    const normalized = seed.trim() || INITIAL_SEED;
    setGame(settle(createGame({ players, humanSeat: 0, seed: normalized }), policy));
    setSeed(normalized);
    setStorageStatus(`Started a deterministic ${players}-player table.`);
  }

  function undo() {
    setAutoplayEnabled(false);
    setGame((current) => undoLastHumanAction(current));
    setStorageStatus("Rewound to the state before your last local action.");
  }

  function restoreFrame() {
    if (replayFrame === null) return;
    setAutoplayEnabled(false);
    setGame((current) => restoreTimelineFrame(current, replayIndex));
    setStorageStatus(`Restored local replay frame ${replayIndex + 1}.`);
  }

  function exportReplay() {
    const blob = new Blob([exportGameReplay(game)], { type: "application/json" });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = `${game.id}-v${game.version}.json`;
    anchor.click();
    URL.revokeObjectURL(url);
    setStorageStatus("Exported deterministic local history.");
  }

  async function importReplay(event: ChangeEvent<HTMLInputElement>) {
    const input = event.currentTarget;
    const file = input.files?.[0];
    if (!file) return;
    try {
      setAutoplayEnabled(false);
      setGame(settle(deserializeGame(JSON.parse(await file.text())), policy));
      setStorageStatus(`Imported ${file.name} into the browser table.`);
    } catch (error) {
      setStorageStatus(error instanceof Error ? error.message : "Could not import local game history.");
    } finally {
      input.value = "";
    }
  }

  return (
    <main className={motionEnabled ? "game-app" : "game-app motion-off"}>
      <a className="skip-link" href="#table">Skip to table</a>
      <header className="command-header">
        <div>
          <p className="eyebrow">Kenjaku / browser-native local table</p>
          <h1>Own the hand.</h1>
          <p className="lede">Tiles, turns, legal actions, policy choice, score, and replay history execute in this browser. No game state leaves this device.</p>
        </div>
        <div className="command-actions" aria-label="Table controls">
          <label className="seed-input">
            <span>Seed</span>
            <input aria-label="Game seed" onChange={(event) => setSeed(event.currentTarget.value)} value={seed} />
          </label>
          <button className="command-button" onClick={() => newGame(4)} type="button">New 4P</button>
          <button className="command-button" onClick={() => newGame(3)} type="button">New 3P</button>
          <button aria-pressed={autoplayEnabled} className="command-button" disabled={game.phase === "terminal"} onClick={() => setAutoplayEnabled((value) => !value)} type="button">{autoplayEnabled ? "Stop autoplay" : "Autoplay all"}</button>
          <button className="command-button" disabled={!game.timeline.some((frame) => frame.event.seat === game.humanSeat && frame.event.action !== null)} onClick={undo} type="button">Undo</button>
          <button className="command-button" onClick={exportReplay} type="button">Export</button>
          <label className="command-button import-button">
            <span>Import</span>
            <input accept="application/json,.json" aria-label="Import local game history" onChange={importReplay} type="file" />
          </label>
          <button className="command-button" onClick={() => setMotionEnabled((value) => !value)} type="button">{motionEnabled ? "Motion on" : "Motion off"}</button>
        </div>
      </header>

      <section aria-label="Runtime state" className="runtime-strip">
        <span>state {digest}</span>
        <span>v{game.version}</span>
        <span>{game.players}P / E{game.handNumber}</span>
        <span>{autoplayEnabled ? "all seats autoplaying" : "manual seat 0"}</span>
        <span>{storageStatus}</span>
      </section>

      <section className={lastEvent?.kind === "discard" || lastEvent?.kind === "win" ? "table-shell is-impact" : "table-shell"} id="table">
        <div className="table-hud" aria-label="Round status">
          <Stat label="Round" value={`${game.roundWind}${game.handNumber}`} />
          <Stat label="Wall" value={`${game.wall.length}`} />
          <Stat label="Dora" value={game.doraIndicators.map(tileGlyph).join(" ") || "—"} />
          <Stat label="Phase" value={game.phase} />
          <Stat label="Turn" value={game.names[game.currentSeat]} />
          <Stat label="Sticks" value={`${game.riichiSticks}`} />
        </div>

        <div className="table-grid">
          <div className="opponent-row top-seat">
            <SeatPanel game={game} seat={topOpponent} showTiles={showAllHands} />
          </div>
          <div className="opponent-row left-seat">
            <SeatPanel game={game} seat={leftOpponent} showTiles={showAllHands} />
          </div>
          <section aria-label="ASCII table field" className="table-core">
            <AsciiField seed={`${digest}:${lastEvent?.id ?? "boot"}`} />
            <div className="ascii-overlay">
              <p>LOCAL ENGINE / {game.phase.toUpperCase()}</p>
              <strong>{lastEvent?.message ?? "Booting table."}</strong>
              <span className="turn-arrow">{seatArrow(game.currentSeat)} {game.names[game.currentSeat]}</span>
            </div>
            <div className="discard-field" aria-label="Discard fields">
              {Array.from({ length: game.players }, (_, seat) => (
                <div className="discard-stack" key={seat}>
                  <span>{game.names[seat]}</span>
                  <TileStrip tiles={game.discards[seat]} />
                </div>
              ))}
            </div>
          </section>
          {rightOpponent === null ? <div aria-hidden="true" className="seat-spacer" /> : <div className="opponent-row right-seat"><SeatPanel game={game} seat={rightOpponent} showTiles={showAllHands} /></div>}
        </div>

        <section aria-label="Your hand" className="hand-console">
          <div className="hand-heading">
            <div>
              <p className="panel-kicker">Seat 0 / {autoplayEnabled ? "policy control" : "direct control"}</p>
              <h2>{game.names[game.humanSeat]} hand</h2>
            </div>
            <p>{game.phase === "terminal" ? "Round complete" : autoplayEnabled ? "All seats are resolving with the local policy" : game.currentSeat === game.humanSeat ? "Select a tile or legal action" : "Models are resolving the table"}</p>
          </div>
          <div aria-label="Player hand" className="player-hand">
            {game.hands[game.humanSeat].map((tile, index) => {
              const discard = legal.find((action): action is Extract<GameAction, { kind: "discard" }> => action.kind === "discard" && action.tile === tile);
              return (
                <button
                  aria-label={`Discard ${tileGlyph(tile)} ${tile}`}
                  className={discard ? "game-tile is-legal" : "game-tile"}
                  disabled={!discard || autoplayEnabled}
                  key={`${tile}-${index}`}
                  onClick={() => discard && commit(discard)}
                  type="button"
                >
                  <b>{tileGlyph(tile)}</b><span>{tile}</span>
                </button>
              );
            })}
          </div>
          <div aria-label="Legal game actions" className="action-rail">
            {legal.filter((action) => action.kind !== "discard").map((action) => (
              <button className="action-button" disabled={autoplayEnabled} key={actionKey(action)} onClick={() => commit(action)} type="button">{actionLabel(action)}</button>
            ))}
            {legal.length === 0 && game.phase !== "terminal" ? <span className="disabled-action">Awaiting model turn</span> : null}
          </div>
        </section>
      </section>

      <section className="control-grid">
        <section className="panel policy-panel" aria-labelledby="policy-heading">
          <div className="panel-heading">
            <div><p className="panel-kicker">Browser policy</p><h2 id="policy-heading">Action aperture</h2></div>
            <button className="command-button" disabled={autoplayEnabled || decision === null} onClick={letPolicyCommit} type="button">Model commit</button>
          </div>
          <p className="status-line" aria-live="polite">{policyStatus}</p>
          {decision ? (
            <ol className="policy-list" aria-label="Policy ranked actions">
              {decision.choices.slice(0, 6).map((choice) => (
                <li key={actionKey(choice.action)}>
                  <button disabled={autoplayEnabled} onClick={() => commit(choice.action)} type="button">
                    <span>{actionLabel(choice.action)}</span><b>{Math.round(choice.probability * 100)}%</b>
                  </button>
                  <p>{choice.rationale.join(" · ")}</p>
                </li>
              ))}
            </ol>
          ) : <p className="muted">No action is pending.</p>}
        </section>

        <section className="panel ledger-panel" aria-labelledby="ledger-heading">
          <div className="panel-heading"><div><p className="panel-kicker">Scores / ownership</p><h2 id="ledger-heading">Table ledger</h2></div></div>
          <ol className="score-list">
            {game.points.map((points, seat) => <li className={seat === game.currentSeat ? "is-current" : ""} key={seat}><span>{seatArrow(seat)} {game.names[seat]}</span><b>{points.toLocaleString()}</b>{game.riichiSeats[seat] ? <em>riichi</em> : null}</li>)}
          </ol>
          <button className="text-button" onClick={() => setShowAllHands((value) => !value)} type="button">{showAllHands ? "Hide engine hands" : "Show engine hands"}</button>
          {game.terminal?.score ? <ScoreResult score={game.terminal.score} /> : game.terminal ? <p className="terminal-copy">Exhaustive draw. No score transfer.</p> : null}
        </section>

        <section className="panel history-panel" aria-labelledby="history-heading">
          <div className="panel-heading"><div><p className="panel-kicker">Append-only replay</p><h2 id="history-heading">History</h2></div><span>{game.history.length} events</span></div>
          {game.timeline.length > 0 ? <label className="replay-scrub"><span>Frame {replayIndex + 1} / {game.timeline.length}</span><input aria-label="Replay frame" max={game.timeline.length - 1} min="0" onChange={(event) => setReplayIndex(Number(event.currentTarget.value))} type="range" value={replayIndex} /></label> : null}
          {replayFrame ? <div className="replay-frame"><span>v{replayFrame.state.version} / {replayFrame.state.phase} / {replayFrame.state.wall.length} wall</span><b>{replayFrame.event.message}</b><button className="text-button" disabled={replayIndex === game.timeline.length - 1} onClick={restoreFrame} type="button">Restore this frame</button></div> : null}
          <ol className="event-log" aria-label="Game event history">
            {[...game.history].reverse().slice(0, 14).map((event) => <li key={event.id}><span>{String(event.version).padStart(3, "0")}</span>{event.message}</li>)}
          </ol>
        </section>
      </section>
    </main>
  );
}

function settle(state: GameState, policy: PolicyArtifact): GameState {
  return advanceAutomated(state, (current, legal) => decideAction(current, legal, policy).selected.action);
}

function Stat({ label, value }: { readonly label: string; readonly value: string }) {
  return <div className="stat"><span>{label}</span><b>{value}</b></div>;
}

function SeatPanel({ game, seat, showTiles }: { readonly game: GameState; readonly seat: number; readonly showTiles: boolean }) {
  return (
    <section className={seat === game.currentSeat ? "seat-panel is-current" : "seat-panel"} aria-label={`${game.names[seat]} state`}>
      <div><span>{seatArrow(seat)}</span><b>{game.names[seat]}</b><em>{game.points[seat].toLocaleString()}</em></div>
      <p>{game.riichiSeats[seat] ? "RIICHI / " : ""}{game.hands[seat].length} concealed / {game.melds[seat].length} meld</p>
      {showTiles ? <TileStrip tiles={game.hands[seat]} /> : <div className="hidden-tiles" aria-label={`${game.hands[seat].length} concealed tiles`}>{"▣".repeat(Math.min(14, game.hands[seat].length))}</div>}
      {game.melds[seat].length > 0 ? <p className="meld-line">{game.melds[seat].map((meld) => `${meld.kind}: ${meld.tiles.join(" ")}`).join(" / ")}</p> : null}
    </section>
  );
}

function TileStrip({ tiles }: { readonly tiles: readonly string[] }) {
  return <div className="tile-strip">{tiles.length === 0 ? <span>—</span> : tiles.map((tile, index) => <span className="mini-tile" key={`${tile}-${index}`}>{tileGlyph(tile)}</span>)}</div>;
}

function ScoreResult({ score }: { readonly score: NonNullable<GameState["terminal"]>["score"] & {} }) {
  if (score === null) return null;
  return <div className="score-result"><strong>{score.winKind.toUpperCase()} / {score.han} HAN / {score.fu} FU</strong><span>{score.yaku.map((line) => `${line.name} ${line.han}`).join(" · ")}</span><b>{score.total.toLocaleString()} point transfer</b></div>;
}

function seatArrow(seat: number): string {
  return ["↓", "←", "↑", "→"][seat] ?? "•";
}

export default App;
