import './style.css'

const query = new URLSearchParams(location.search)
if (query.has('atlas')) void import('./atlas-inspector')
else bootVoyager()

function bootVoyager(): void {
  const canvas = document.querySelector<HTMLCanvasElement>('#game')
  if (!canvas) return
  const activate = document.createElement('button')
  activate.type = 'button'
  activate.className = 'boot-activate'
  activate.textContent = 'Initialize Voyager'
  activate.setAttribute('aria-controls', 'game')
  document.body.append(activate)
  let loading = false

  const draw = (headline: string, detail: string): void => {
    const context = canvas.getContext('2d')
    if (!context) return
    context.fillStyle = '#06070b'
    context.fillRect(0, 0, canvas.width, canvas.height)
    context.fillStyle = '#f4d26a'
    context.font = '24px BigBlueTerm, monospace'
    context.fillText('JOMON VOYAGER', 72, 180)
    context.fillStyle = '#d6dce8'
    context.font = '14px BigBlueTerm, monospace'
    context.fillText(headline, 72, 230)
    context.fillStyle = '#8da0b8'
    context.fillText(detail, 72, 264)
  }
  const removeListeners = () => {
    canvas.removeEventListener('pointerdown', load)
    canvas.removeEventListener('focus', load)
    window.removeEventListener('keydown', loadFromKey)
    activate.removeEventListener('click', load)
  }
  const retry = () => {
    loading = false
    activate.disabled = false
    activate.textContent = 'Retry initialization'
    draw('Initialization failed', 'Select retry to load the current Voyager build.')
  }
  const load = () => {
    if (loading) return
    loading = true
    activate.disabled = true
    activate.textContent = 'Loading Voyager…'
    draw('Loading mission systems…', 'The carrier is preparing the active game modules.')
    void import('./main').then(() => {
      removeListeners()
      activate.remove()
      canvas.focus()
    }).catch(retry)
  }
  const loadFromKey = (keyboardEvent: KeyboardEvent) => {
    if (keyboardEvent.metaKey || keyboardEvent.ctrlKey || keyboardEvent.altKey) return
    keyboardEvent.preventDefault()
    load()
  }
  window.addEventListener('vite:preloadError', event => {
    event.preventDefault()
    if (loading) retry()
  })
  canvas.addEventListener('pointerdown', load, { once: true })
  canvas.addEventListener('focus', load, { once: true })
  window.addEventListener('keydown', loadFromKey)
  activate.addEventListener('click', load)
  draw('Carrier systems standing by', 'Click the display, focus it, or press a key to initialize.')
}
