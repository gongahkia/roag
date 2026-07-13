# Kenjaku browser

Static React/Vite frontend for GitHub Pages. It uses relative asset paths and contains no network client or server configuration. Trajectories and `.onnx` models are accepted only through the browser's local file picker; the app does not upload them. Local ONNX models use ONNX Runtime Web with WebGPU when available and a WebAssembly fallback.

`public/models/manifest.json` lists browser model assets by site-relative path, byte count, and SHA-256. The Pages workflow runs `python3 scripts/verify_browser_model_assets.py web/dist` after the Vite build; add a hashed manifest entry with each future model asset.

```bash
npm install
npm test
npm run build
```
