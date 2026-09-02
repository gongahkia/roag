import { describe, expect, it } from 'vitest'
import { renderBoundedMedievalCanvasRows, wrapMedievalCanvasText } from './app'

const textWidth = (text: string): number => text.length * 10

describe('medieval canvas text bounds', () => {
  it('wraps long words to the fixed canvas content width', () => {
    const context = { measureText: (text: string): TextMetrics => ({ width: textWidth(text) } as TextMetrics) }
    const lines = wrapMedievalCanvasText(context, 'x'.repeat(200))

    expect(lines.length).toBeGreaterThan(1)
    expect(lines.every(line => textWidth(line) <= 724)).toBe(true)
  })

  it('uses the supplied panel width for compact management text', () => {
    const context = { measureText: (text: string): TextMetrics => ({ width: textWidth(text) } as TextMetrics) }
    const lines = wrapMedievalCanvasText(context, 'KNOWN REGION NAME // FROM TRADER // KNOWN 120M // FRESH 120M', 180)

    expect(lines.length).toBeGreaterThan(1)
    expect(lines.every(line => textWidth(line) <= 180)).toBe(true)
  })

  it('keeps the explicit empty-message and disabled-prompt labels inside the reserved main-panel width', () => {
    const context = { measureText: (text: string): TextMetrics => ({ width: textWidth(text) } as TextMetrics) }
    const message = wrapMedievalCanvasText(context, 'MESSAGES // No current authoritative messages.', 445)
    const prompt = wrapMedievalCanvasText(context, '! [ENTER] NO CONTEXTUAL ACTION // DISABLED', 445)

    expect(message.every(line => textWidth(line) <= 445)).toBe(true)
    expect(prompt.every(line => textWidth(line) <= 445)).toBe(true)
  })

  it('keeps a long local-storage diagnostic within its reserved in-panel rows', () => {
    const drawn: { text: string; y: number }[] = []
    const context = {
      fillStyle: '',
      measureText: (text: string): TextMetrics => ({ width: textWidth(text) } as TextMetrics),
      fillText: (text: string, _x: number, y: number): void => { drawn.push({ text, y }) }
    } as unknown as CanvasRenderingContext2D

    const nextLine = renderBoundedMedievalCanvasRows(context, 21, 23, `ERROR // LOCAL STORAGE: ${'selected-world-is-no-longer-present '.repeat(80)}`, '#ff0000')

    expect(nextLine).toBe(24)
    expect(drawn).toHaveLength(3)
    expect(drawn.map(line => line.y)).toEqual([540, 562, 584])
    expect(drawn.every(line => textWidth(line.text) <= 724)).toBe(true)
    expect(drawn.at(-1)?.text).toMatch(/\.\.\.$/)
  })
})
