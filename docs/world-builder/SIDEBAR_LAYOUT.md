# Settlement sidebar layout

The world now fills the left side and the full viewport height. The title,
world tagline and horizontal bottom tool strip have been removed. The right
sidebar keeps treasury, population, mood, calendar, speed, inspection, sound,
menu, camera and Undo/Redo controls together. Its lower section scrolls through
all 18 tools, grouped as Homes, Work, Preparedness, Social, Roads, Landscape and
Manage. Each entry retains its existing sprite, price and description, with a
highlight and check for the selected tool. Free building mode displays free
prices consistently.

Click a sidebar entry, then click or drag in the world as before. B scrolls to
the catalogue; 1–0 and all other existing shortcuts remain available. Wheel
input over the sidebar scrolls the catalogue; wheel input over the world zooms
the camera. Inspection and conversations appear in the sidebar. Opening
inspection, relocation or a conversation brings that content into view.

## Run locally

From the repository root, after building:

```sh
make settlement-serve
```

Open the printed URL. The updated production build was rebuilt locally. To
rebuild again, use `make settlement` with Flutter 3.50.0 on PATH, or specify an
absolute SDK executable with `FLUTTER_BIN`. No deployment is part of this change.

## Ownership and verification

- `apps/settlement/lib/main.dart` owns this layout, sidebar scroll controller,
  selected tool presentation and focus/inspection routing. Renderer, simulation,
  terrain, construction transactions, prices and save schemas are unchanged.
- `apps/settlement/tool/sidebar_browser.cjs` exercises the real release with
  Playwright mouse, wheel and keyboard input in an isolated Chromium context.
  Its explicitly loaded existing-town fixture is separate from personal browser
  storage. These are automated browser checks, not native desktop manual play.

```sh
make settlement-build FLUTTER_BIN=/absolute/path/to/flutter/bin/flutter
make settlement-test FLUTTER_BIN=/absolute/path/to/flutter/bin/flutter
cd apps/settlement
flutter analyze
# In another terminal, start make settlement-serve from the repository root.
# Use that server's printed address; Playwright is the existing tool dependency.
SETTLEMENT_URL=http://127.0.0.1:PORT/roag/settlement/ \
  node tool/sidebar_browser.cjs /var/tmp/roag-sidebar-browser
```

Verified on 2026-11-21:

- Production release build passed; all **214 Flutter tests passed**.
- Full `flutter analyze`, Dart formatting, JavaScript syntax and
  `git diff --check` passed.
- The release browser passed eight journeys covering existing-town compatibility,
  all 18 tools, scroll/keyboard behaviour, normal construction costs, Undo/Redo,
  inspection, relocation, conversation, road dragging, mute/camera controls,
  export and exact paused reload. No browser errors or failed asset requests.
- At 800×600, 1024×640 and 1440×900, the world occupies the full viewport height
  and a majority of its width; controls remain on the right.
- All six README files have identical initial/final hashes. Personal browser
  profiles were not opened; the tests used disposable contexts. Renderer,
  simulation, schema, original artwork and Lua files were not changed.

Evidence: [browser results](evidence/sidebar/results.json),
[browser log](evidence/sidebar/browser.log),
[Flutter tests](evidence/sidebar/flutter-tests.log),
[release build](evidence/sidebar/release-build.log),
[hashes](evidence/sidebar/preservation.json).

Screenshots: [1024×640](evidence/sidebar/sidebar-1024.png),
[1440×900](evidence/sidebar/sidebar-1440.png),
[lower catalogue](evidence/sidebar/sidebar-landscape-tools.png),
[inspection](evidence/sidebar/sidebar-inspection.png),
[conversation](evidence/sidebar/sidebar-conversation.png).

Desktop sizes are covered; a phone-specific layout is outside this change.
Contextual world event notices and the optional first-time introduction remain
available. This change is local; the public hosted release was not updated.
