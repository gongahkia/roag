/**
 * The browser bootstrap owns exactly one mutable world at a time. This is
 * deliberately in-memory: it prevents competing roots in one browser session
 * without confusing it for a cross-device or server lock.
 */
export class MutableWorldSession {
  private worldId: string | undefined

  open(worldId: string): void {
    if (this.worldId !== undefined && this.worldId !== worldId) throw new Error('another medieval world is already open in this browser session')
    this.worldId = worldId
  }

  assertOwner(worldId: string): void {
    if (this.worldId !== worldId) throw new Error('this world is not the mutable browser-session world')
  }

  release(worldId: string): void {
    this.assertOwner(worldId)
    this.worldId = undefined
  }

  current(): string | undefined { return this.worldId }
}
