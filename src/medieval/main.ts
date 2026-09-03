import { MedievalApp } from './app'
import { JOMON_PALETTE, JOMON_PALETTE_CSS_PROPERTIES, MEDIEVAL_PALETTE_VERSION } from './palette'

for (const [property, value] of Object.entries(JOMON_PALETTE_CSS_PROPERTIES)) document.documentElement.style.setProperty(property, value)
document.documentElement.dataset.medievalPaletteVersion = String(MEDIEVAL_PALETTE_VERSION)
const themeColor = document.querySelector<HTMLMetaElement>('meta[name="theme-color"]') ?? document.head.appendChild(document.createElement('meta'))
themeColor.name = 'theme-color'
themeColor.content = JOMON_PALETTE.consoleGround

const canvas = document.querySelector<HTMLCanvasElement>('#game')
if (!canvas) throw new Error('Jomon canvas is missing')

const app = new MedievalApp(canvas)
void app.start()
