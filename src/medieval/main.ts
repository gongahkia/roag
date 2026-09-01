import { MedievalApp } from './app'

const canvas = document.querySelector<HTMLCanvasElement>('#game')
if (!canvas) throw new Error('Jomon canvas is missing')

const app = new MedievalApp(canvas)
void app.start()
