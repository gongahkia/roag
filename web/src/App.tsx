import { useState } from "react";

const fixtureActions = ["Discard 5p", "Riichi", "Pass"] as const;

function App() {
  const [selectedIndex, setSelectedIndex] = useState(0);
  const selectedAction = fixtureActions[selectedIndex];

  return (
    <main className="app-shell">
      <header>
        <p className="eyebrow">Kenjaku browser</p>
        <h1>Local trajectory inspection</h1>
        <p className="lede">Fixture-only shell. Future screens load user-selected local files.</p>
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
    </main>
  );
}

export default App;
