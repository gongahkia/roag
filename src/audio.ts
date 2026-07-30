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
    const settings: Record<GameEventType, [number, number, number, OscillatorType, number]> = {
      move: [180, 160, .025, 'square', .018], traverse: [260, 560, .11, 'triangle', .026], encounter: [410, 660, .16, 'sine', .03], hit: [110, 72, .06, 'sawtooth', .028], hurt: [75, 48, .08, 'sawtooth', .03], pickup: [520, 660, .06, 'square', .022], spell: [340, 520, .1, 'sine', .028], boom: [55, 38, .18, 'sawtooth', .035], danger: [95, 70, .1, 'square', .024], menu: [300, 340, .03, 'square', .014], level: [780, 1040, .22, 'sine', .032], rope: [410, 350, .08, 'square', .02], suspend: [240, 170, .12, 'sine', .022], death: [48, 32, .3, 'sawtooth', .035], win: [660, 990, .25, 'square', .03], floor: [440, 620, .15, 'sine', .026], areaComplete: [620, 880, .2, 'sine', .03], gateResolved: [700, 930, .18, 'sine', .028], rescue: [560, 740, .16, 'sine', .025], terrain: [220, 300, .08, 'triangle', .024]
    }
    const [frequency, endFrequency, duration, type, volume] = settings[event]
    const oscillator = this.context.createOscillator()
    const gain = this.context.createGain()
    oscillator.type = type
    oscillator.frequency.value = frequency
    oscillator.frequency.exponentialRampToValueAtTime(endFrequency, this.context.currentTime + duration)
    gain.gain.setValueAtTime(volume, this.context.currentTime)
    gain.gain.exponentialRampToValueAtTime(.001, this.context.currentTime + duration)
    oscillator.connect(gain).connect(this.context.destination)
    oscillator.start()
    oscillator.stop(this.context.currentTime + duration)
  }
}
