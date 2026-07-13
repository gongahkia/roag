import type { ActionResult, GameEventType } from './engine'

export class AudioBus {
  private context?: AudioContext

  play(events: ActionResult): void {
    if (!events.length) return
    this.context ??= new AudioContext()
    if (this.context.state === 'suspended') void this.context.resume()
    for (const event of new Set(events.map(event => event.type))) this.tone(event)
  }

  private tone(event: GameEventType): void {
    if (!this.context) return
    const settings: Record<GameEventType, [number, number, OscillatorType]> = {
      move: [180, .025, 'square'], hit: [110, .06, 'sawtooth'], hurt: [75, .08, 'sawtooth'], pickup: [520, .06, 'square'], spell: [340, .1, 'sine'], boom: [55, .18, 'sawtooth'], danger: [95, .1, 'square'], menu: [300, .03, 'square'], death: [48, .3, 'sawtooth'], win: [660, .25, 'square'], floor: [440, .15, 'sine'], areaComplete: [620, .2, 'sine'], gateResolved: [700, .18, 'sine'], rescue: [560, .16, 'sine']
    }
    const [frequency, duration, type] = settings[event]
    const oscillator = this.context.createOscillator()
    const gain = this.context.createGain()
    oscillator.type = type
    oscillator.frequency.value = frequency
    gain.gain.setValueAtTime(.025, this.context.currentTime)
    gain.gain.exponentialRampToValueAtTime(.001, this.context.currentTime + duration)
    oscillator.connect(gain).connect(this.context.destination)
    oscillator.start()
    oscillator.stop(this.context.currentTime + duration)
  }
}
