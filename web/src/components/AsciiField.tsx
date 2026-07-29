import { useEffect, useRef } from "react";

import { createTableCutsceneFrame } from "../lib/ascii-renderer";

interface AsciiFieldProps {
  readonly seed: string;
  readonly players: 3 | 4;
  readonly names: readonly string[];
  readonly activeSeat: number;
  readonly eventKind: "game_started" | "draw" | "discard" | "riichi" | "call" | "pass" | "win" | "draw_game" | "system" | null;
}

export function AsciiField({ seed, players, names, activeSeat, eventKind }: AsciiFieldProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return undefined;

    function render() {
      const target = canvasRef.current;
      if (!target) return;
      const bounds = target.getBoundingClientRect();
      if (bounds.width <= 0 || bounds.height <= 0) return;
      const ratio = Math.min(window.devicePixelRatio || 1, 2);
      const columns = Math.max(36, Math.min(100, Math.floor(bounds.width / 7)));
      const rows = Math.max(16, Math.min(42, Math.floor(bounds.height / 11)));
      const characterWidth = bounds.width / columns;
      const characterHeight = bounds.height / rows;
      const frame = createTableCutsceneFrame({ columns, rows, seed, players, names, activeSeat, eventKind });

      target.width = Math.max(1, Math.floor(bounds.width * ratio));
      target.height = Math.max(1, Math.floor(bounds.height * ratio));
      const context = target.getContext("2d");
      if (!context) return;
      context.setTransform(ratio, 0, 0, ratio, 0, 0);
      context.clearRect(0, 0, bounds.width, bounds.height);
      context.font = `${Math.max(7, Math.floor(characterHeight * 0.86))}px ui-monospace, SFMono-Regular, Menlo, monospace`;
      context.textBaseline = "top";
      context.fillStyle = "#f1eddb";

      for (const [row, line] of frame.entries()) {
        for (const [column, glyph] of Array.from(line).entries()) {
          if (glyph === " ") continue;
          const x = column * characterWidth;
          const y = row * characterHeight;
          context.globalAlpha = glyph === "@" || glyph === "+" || glyph === "|" || glyph === "-" ? 0.86 : 0.28;
          context.fillText(glyph, x + 1, y);
          context.globalAlpha = glyph === "@" ? 1 : 0.68;
          context.fillText(glyph, x, y);
        }
      }
      context.globalAlpha = 1;
    }

    render();
    if (typeof ResizeObserver === "undefined") {
      window.addEventListener("resize", render);
      return () => window.removeEventListener("resize", render);
    }
    const observer = new ResizeObserver(render);
    observer.observe(canvas);
    return () => observer.disconnect();
  }, [activeSeat, eventKind, names, players, seed]);

  return <canvas aria-label={`ASCII cutscene: camera focuses ${names[activeSeat] ?? `seat ${activeSeat + 1}`} at the mahjong table.`} className="ascii-field" data-ascii-renderer ref={canvasRef} role="img" />;
}
