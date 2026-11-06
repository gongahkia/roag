export type View = Readonly<{ x: number; y: number; zoom: number }>;
export type Size = Readonly<{ width: number; height: number }>;
export const WORLD = { width: 1536, height: 1536 } as const;
export const TILE = 12;
export const MAX_ZOOM = 6;
export function minZoom(size: Size): number {
  return Math.max(1, Math.ceil(Math.max(size.width / WORLD.width, size.height / WORLD.height)));
}
export function constrain(view: View, size: Size): View {
  const zoom = Math.max(minZoom(size), Math.min(Math.max(MAX_ZOOM, minZoom(size)), Math.round(view.zoom)));
  const halfW = size.width / (2 * zoom);
  const halfH = size.height / (2 * zoom);
  return { zoom, x: Math.max(halfW, Math.min(WORLD.width - halfW, view.x)),
    y: Math.max(halfH, Math.min(WORLD.height - halfH, view.y)) };
}
export function zoomAt(view: View, size: Size, delta: number, pointer = { x: size.width / 2, y: size.height / 2 }): View {
  const zoom = constrain({ ...view, zoom: view.zoom + delta }, size).zoom;
  return constrain({ zoom, x: view.x + (pointer.x - size.width / 2) * (1 / view.zoom - 1 / zoom),
    y: view.y + (pointer.y - size.height / 2) * (1 / view.zoom - 1 / zoom) }, size);
}
