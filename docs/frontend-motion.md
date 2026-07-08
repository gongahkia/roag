# Frontend Motion Primitives

`kenjaku.frontend_static` exposes reusable static UI motion primitives for generated viewers.

## CSS

- `kj-motion-lift`: transform-only hover lift.
- `kj-motion-press`: transform-only active press.
- `kj-motion-selected-pulse`: selected tile/card pulse.
- `kj-motion-confirm-flash`: action confirmation flash.
- `kj-motion-score-count`: score count-up pop.
- `kj-motion-warning-shake`: warning/error shake.

These primitives use transform, filter, and box-shadow only. They must not change margin, padding, width, height, or positioning.

`prefers-reduced-motion: reduce` disables nonessential animation and transitions.

## JS

`motion_primitives_script()` installs `window.KenjakuMotion`:

- `confirm(target)`
- `pulse(target)`
- `shake(target)`
- `countUp(target, nextText, options)`
- `prefersReducedMotion()`

Use these from static viewers when feedback is caused by state changes. Browser-demo currently uses lift/press, selected pulse, confirmation flash, and score count-up.
