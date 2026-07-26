# Browser Table

`web/` is Kenjaku's canonical gameplay product surface. It is a static React/Vite application, so
players use the browser rather than a Python command.

```bash
cd web
npm ci
npm run dev
```

For deployment, build `web/dist`; the Pages workflow publishes that directory as a static site.
The browser owns game state, legal actions, local policy selection, scoring, history, replay frames,
undo, import/export, and local persistence. It makes no gameplay API calls and uploads no state.

The current browser rules core is a local sandbox: it supports 3P/4P turn flow, calls, bounded
scoring, and selected winning shapes. It does not claim full parity with the Python research
sandbox or a complete riichi rules validator.
