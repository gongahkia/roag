# Kenjaku browser

Static React/Vite frontend for GitHub Pages. It uses relative asset paths and contains no network client or server configuration. Trajectories are accepted only through the browser's local file picker; the app does not upload them.

`public/models/manifest.json` lists browser model assets by site-relative path, byte count, and SHA-256. The Pages workflow runs `python3 scripts/verify_browser_model_assets.py web/dist` after the Vite build; add a hashed manifest entry with each future model asset.

```bash
npm install
npm test
npm run build
```
