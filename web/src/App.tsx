import { useEffect, useRef, useState } from "react";
import type { ChangeEvent } from "react";

import {
  DecisionRationaleDetails,
  type DecisionCounterfactualsView
} from "./components/DecisionRationaleDetails";
import { readLocalMjsonFile } from "./lib/local-file";
import {
  loadLocalMultiActionOnnxPolicy,
  type BrowserMultiActionOnnxPolicy
} from "./lib/onnx-inference";
import { buildReplayTimeline, type ReplayBoardState, type ReplayTimelineStep } from "./lib/replay";

const rationalePreview: DecisionCounterfactualsView = {
  decision: {
    selected_action: { action: "discard", tile: "5p" },
    rationale: {
      factors: [
        {
          factor: "shape_improvement",
          value: 0.8,
          contribution: 0.24,
          evidence: ["Retains two-sided wait potential"]
        },
        {
          factor: "defense_risk",
          value: 0.15,
          contribution: -0.06,
          evidence: []
        }
      ]
    }
  },
  selected_score: 0.42,
  top_alternatives: [
    {
      action: { action: "discard", tile: "9p" },
      score: 0.35,
      score_delta: -0.07,
      rationale: {
        factors: [
          {
            factor: "shape_improvement",
            value: 0.5,
            contribution: 0.12,
            evidence: ["Keeps a weaker wait"]
          }
        ]
      }
    }
  ]
};

function App() {
  const [timeline, setTimeline] = useState<readonly ReplayTimelineStep[]>([]);
  const [selectedIndex, setSelectedIndex] = useState(0);
  const [fileStatus, setFileStatus] = useState("No local trajectory loaded.");
  const [modelStatus, setModelStatus] = useState("No local ONNX model loaded.");
  const [policy, setPolicy] = useState<BrowserMultiActionOnnxPolicy | null>(null);
  const modelLoadSequence = useRef(0);
  const selectedStep = timeline[selectedIndex];

  useEffect(() => () => {
    if (policy) void policy.release();
  }, [policy]);

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

  async function onModelChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.currentTarget.files?.[0];
    if (!file) return;
    const sequence = modelLoadSequence.current + 1;
    modelLoadSequence.current = sequence;
    setModelStatus(`Loading ${file.name} from this device.`);
    try {
      const nextPolicy = await loadLocalMultiActionOnnxPolicy(file);
      if (sequence !== modelLoadSequence.current) {
        await nextPolicy.release();
        return;
      }
      setPolicy(nextPolicy);
      setModelStatus(
        `Loaded ${file.name} with ${nextPolicy.provider.toUpperCase()}${nextPolicy.usedFallback ? " fallback" : ""}.`
      );
    } catch (error) {
      if (sequence === modelLoadSequence.current) {
        setModelStatus(error instanceof Error ? error.message : "local ONNX model could not be loaded");
      }
    } finally {
      event.currentTarget.value = "";
    }
  }

  return (
    <main className="app-shell">
      <header>
        <p className="eyebrow">Kenjaku browser</p>
        <h1>Local trajectory inspection</h1>
        <p className="lede">Trajectory data and ONNX models stay in this browser and are never uploaded.</p>
      </header>
      <section aria-labelledby="local-file-heading" className="panel">
        <h2 id="local-file-heading">Local trajectory</h2>
        <label className="file-picker">
          Choose MJSON or JSONL
          <input accept=".mjson,.jsonl,application/json,text/plain" onChange={onFileChange} type="file" />
        </label>
        <p aria-live="polite">{fileStatus}</p>
      </section>
      <section aria-labelledby="local-model-heading" className="panel">
        <h2 id="local-model-heading">Local ONNX model</h2>
        <label className="file-picker">
          Choose ONNX model
          <input accept=".onnx,application/onnx,application/octet-stream" onChange={onModelChange} type="file" />
        </label>
        <p aria-live="polite">{modelStatus}</p>
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
      <DecisionRationaleDetails counterfactuals={rationalePreview} title="Rationale preview" />
      <p className="preview-note">Example payload; not an inference result.</p>
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
