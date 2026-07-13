import { useState } from "react";
import type { ChangeEvent } from "react";

import { readLocalMjsonFile } from "./lib/local-file";

const fixtureActions = ["Discard 5p", "Riichi", "Pass"] as const;

function App() {
  const [selectedIndex, setSelectedIndex] = useState(0);
  const [fileStatus, setFileStatus] = useState("No local trajectory loaded.");
  const selectedAction = fixtureActions[selectedIndex];

  async function onFileChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.currentTarget.files?.[0];
    if (!file) return;
    try {
      const result = await readLocalMjsonFile(file);
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
      <section aria-labelledby="fixture-heading" className="panel">
        <h2 id="fixture-heading">Fixture decision</h2>
        <p aria-live="polite">Selected action: {selectedAction}</p>
        <div className="action-list">
          {fixtureActions.map((action, index) => (
            <button
              aria-pressed={selectedIndex === index}
              key={action}
              onClick={() => setSelectedIndex(index)}
              type="button"
            >
              {action}
            </button>
          ))}
        </div>
      </section>
      <section aria-labelledby="local-file-heading" className="panel">
        <h2 id="local-file-heading">Local trajectory</h2>
        <label className="file-picker">
          Choose MJSON or JSONL
          <input accept=".mjson,.jsonl,application/json,text/plain" onChange={onFileChange} type="file" />
        </label>
        <p aria-live="polite">{fileStatus}</p>
      </section>
    </main>
  );
}

export default App;
