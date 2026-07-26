# Kenjaku browser table

This is the canonical Kenjaku gameplay surface. It is a static React/Vite app: tiles, turn state, legal-action generation, local policy decisions, scoring, event history, replay frames, undo, import/export, and IndexedDB persistence execute in the browser. GitHub Pages serves assets only; gameplay has no Python runtime, HTTP game API, account, or upload path.

The local rules core supports 3P/4P wall construction, draw/discard turns, reaction passes, chi/pon/minkan/ankan/kakan/Kita paths, standard/chiitoitsu/kokushi win shapes, and a bounded local score model. It is not a claim of parity with the full Python research sandbox or a complete riichi rules validator.

`public/policies/local-shape-policy-v1.json` is the versioned browser policy artifact. The app loads it from the same static site and falls back to the equivalent built-in policy only if that local asset is unavailable. Existing local ONNX replay-inspection modules remain offline-only utilities; nothing is uploaded.

`public/models/manifest.json` lists browser model assets by site-relative path, byte count, and SHA-256. The Pages workflow runs `python3 scripts/verify_browser_model_assets.py web/dist` after the Vite build; add a hashed manifest entry with each future model asset.

```bash
npm install
npm test
npm run build
npm run dev
```
