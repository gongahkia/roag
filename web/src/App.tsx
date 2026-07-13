import { useState } from "react";
import type { ChangeEvent } from "react";

import { readLocalMjsonFile } from "./lib/local-file";
import { buildReplayTimeline, type ReplayBoardState, type ReplayTimelineStep } from "./lib/replay";

function App() {
  const [timeline, setTimeline] = useState<readonly ReplayTimelineStep[]>([]);
  const [selectedIndex, setSelectedIndex] = useState(0);
  const [fileStatus, setFileStatus] = useState("No local trajectory loaded.");
  const selectedStep = timeline[selectedIndex];

  async function onFileChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.currentTarget.files?.[0];
    if (!file) return;
    try {
      const result = await readLocalMjsonFile(file);
      setTimeline(buildReplayTimeline(result.events));
      setSelectedIndex(0);
      setFileStatus(
        `Loaded ${result.events.length} events with ${result.errors.length} invalid lines.`
      );
    } catch (error) {
      setFileStatus(error instanceof Error ? error.message : "local file could not be read");
    } finally {
      event.currentTarget.value = "";
    }
  }

  return (
    <main className="app-shell">
      <header>
        <p className="eyebrow">Kenjaku browser</p>
        <h1>Local trajectory inspection</h1>
        <p className="lede">Trajectory data stays in this browser and is never uploaded.</p>
      </header>
      <section aria-labelledby="local-file-heading" className="panel">
        <h2 id="local-file-heading">Local trajectory</h2>
        <label className="file-picker">
          Choose MJSON or JSONL
          <input accept=".mjson,.jsonl,application/json,text/plain" onChange={onFileChange} type="file" />
        </label>
        <p aria-live="polite">{fileStatus}</p>
      </section>
      {selectedStep ? (
        <ReplayViewer
          onSelect={setSelectedIndex}
          selectedIndex={selectedIndex}
          selectedStep={selectedStep}
          timeline={timeline}
        />
      ) : (
        <section className="panel">
          <h2>Replay timeline</h2>
          <p>Choose a local trajectory to inspect its board state and action requests.</p>
        </section>
      )}
    </main>
  );
}

interface ReplayViewerProps {
  readonly onSelect: (index: number) => void;
  readonly selectedIndex: number;
  readonly selectedStep: ReplayTimelineStep;
  readonly timeline: readonly ReplayTimelineStep[];
}

function ReplayViewer({ onSelect, selectedIndex, selectedStep, timeline }: ReplayViewerProps) {
  return (
    <section aria-label="Replay timeline" className="replay">
      <section className="panel replay-controls">
        <div>
          <h2>Replay timeline</h2>
          <p aria-live="polite">
            Event {selectedIndex + 1} of {timeline.length}: {selectedStep.label}
          </p>
        </div>
        <label className="timeline-slider">
          Timeline position
          <input
            aria-label="Timeline position"
            max={timeline.length - 1}
            min="0"
            onChange={(event) => onSelect(Number(event.currentTarget.value))}
            type="range"
            value={selectedIndex}
          />
        </label>
      </section>
      <div className="replay-layout">
        <ol aria-label="Replay events" className="event-list panel">
          {timeline.map((step) => (
            <li key={step.index}>
              <button
                aria-current={step.index === selectedIndex ? "step" : undefined}
                onClick={() => onSelect(step.index)}
                type="button"
              >
                {step.index + 1}. {step.label}
              </button>
            </li>
          ))}
        </ol>
        <DecisionInspection step={selectedStep} />
      </div>
      <BoardState state={selectedStep.state} />
    </section>
  );
}

function DecisionInspection({ step }: { readonly step: ReplayTimelineStep }) {
  return (
    <section aria-labelledby="decision-heading" className="panel decision-inspection">
      <h2 id="decision-heading">Decision inspection</h2>
      {step.decision ? (
        <>
          <p>Seat {step.decision.actor ?? "?"} requested an action.</p>
          <ul aria-label="Legal actions" className="legal-actions">
            {step.decision.legal_actions.map((action, index) => (
              <li key={`${action}-${index}`}>{action}</li>
            ))}
          </ul>
        </>
      ) : (
        <p>This event has no requested action.</p>
      )}
    </section>
  );
}

function BoardState({ state }: { readonly state: ReplayBoardState }) {
  return (
    <section aria-labelledby="board-heading" className="panel board-state">
      <h2 id="board-heading">Board state</h2>
      <p className="board-meta">
        {state.round_wind ?? "?"}{state.kyoku ?? "?"} · honba {state.honba} · sticks {state.kyotaku} · dora{" "}
        {state.dora_indicators.join(", ") || "?"}
      </p>
      <div className="seat-grid">
        {Array.from({ length: state.players }, (_, seat) => (
          <section className={state.active_seat === seat ? "seat active-seat" : "seat"} key={seat}>
            <h3>
              {state.names[seat]} · {state.scores[seat]}
              {state.riichi_seats[seat] ? " · riichi" : ""}
            </h3>
            <TileRow label="Hand" tiles={state.hands[seat]} />
            <TileRow label="Discards" tiles={state.discards[seat]} />
            <p className="seat-meta">
              Melds: {state.melds[seat].map((meld) => meld.tiles.join(" ")).join(" | ") || "none"}
              <br />
              Kita: {state.kita_tiles[seat].join(" ") || "none"}
            </p>
          </section>
        ))}
      </div>
    </section>
  );
}

function TileRow({ label, tiles }: { readonly label: string; readonly tiles: readonly string[] }) {
  return (
    <div className="tile-group">
      <span>{label}</span>
      <div className="tile-row">{tiles.length > 0 ? tiles.map((tile, index) => <b key={`${tile}-${index}`}>{tile}</b>) : "—"}</div>
    </div>
  );
}

export default App;
