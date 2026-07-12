import './style.css'
import { BLOCKS, BlockType, type CameraState, type PlayerState, WORLD_SIZE } from './types'
import { Renderer, type HoveredBlock } from './renderer'
import { loadWorld, saveWorld } from './storage'
import { BlockWorld, generateWorld } from './world'

const canvas = document.querySelector<HTMLCanvasElement>('#game')!
const hotbar = document.querySelector<HTMLElement>('#hotbar')!
const seedLabel = document.querySelector<HTMLElement>('#seed-label')!
const renderer = new Renderer(canvas)
const seed = 260712
const world: BlockWorld = generateWorld(seed)
const player: PlayerState = { x: WORLD_SIZE / 2 + .5, y: 8, z: WORLD_SIZE / 2 + .5, velocityY: 0, grounded: false }
const camera: CameraState = { rotation: 0, zoom: 1 }
const keys = new Set<string>()
let selected = 0
let hover: HoveredBlock | undefined
let removeArmed = false
let last = performance.now()
let dirty = false

seedLabel.textContent = `WORLD ${seed}`
for (const [i, block] of BLOCKS.entries()) {
  const button = document.createElement('button')
  button.className = 'slot'
  button.title = block.name
  button.setAttribute('aria-label', `Select ${block.name}`)
  button.innerHTML = `<span style="--block:${block.color}"></span><b>${i + 1}</b>`
  button.addEventListener('click', () => { selected = i; refreshHotbar() })
  hotbar.append(button)
}
refreshHotbar()

void loadWorld().then(save => { if (save) world.applySave(save) })
window.setInterval(() => { if (dirty) { dirty = false; void saveWorld(world.toSave()) } }, 1200)

window.addEventListener('keydown', event => {
  if (['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', ' ', 'q', 'e', 'Q', 'E'].includes(event.key)) event.preventDefault()
  keys.add(event.key.toLowerCase())
  if (event.key >= '1' && event.key <= '9') { selected = Number(event.key) - 1; refreshHotbar() }
  if (event.key.toLowerCase() === 'q') rotate(-1)
  if (event.key.toLowerCase() === 'e') rotate(1)
})
window.addEventListener('keyup', event => keys.delete(event.key.toLowerCase()))
canvas.addEventListener('contextmenu', event => event.preventDefault())
canvas.addEventListener('pointermove', event => { hover = pick(event); canvas.classList.toggle('targeting', Boolean(hover)) })
canvas.addEventListener('pointerleave', () => { hover = undefined; canvas.classList.remove('targeting') })
canvas.addEventListener('pointerdown', event => {
  canvas.setPointerCapture(event.pointerId)
  hover = pick(event)
  if (event.button === 2 || removeArmed) edit(true)
  else edit(false)
})
canvas.addEventListener('wheel', event => {
  event.preventDefault()
  camera.zoom = Math.max(.72, Math.min(1.45, camera.zoom + (event.deltaY < 0 ? .08 : -.08)))
}, { passive: false })

document.querySelectorAll<HTMLButtonElement>('[data-action]').forEach(button => {
  const action = button.dataset.action!
  const down = (event: Event) => {
    event.preventDefault()
    if (action === 'rotate-left') return void rotate(-1)
    if (action === 'rotate-right') return void rotate(1)
    if (action === 'remove') { removeArmed = true; button.classList.add('armed'); return }
    if (action === 'jump') { jump(); return }
    keys.add(action)
  }
  const up = () => { keys.delete(action); if (action === 'remove') { removeArmed = false; button.classList.remove('armed') } }
  button.addEventListener('pointerdown', down)
  button.addEventListener('pointerup', up)
  button.addEventListener('pointercancel', up)
  button.addEventListener('pointerleave', up)
})

function pick(event: PointerEvent): HoveredBlock | undefined {
  const rect = canvas.getBoundingClientRect()
  return renderer.pick(world, camera, (event.clientX - rect.left) * canvas.width / rect.width, (event.clientY - rect.top) * canvas.height / rect.height)
}

function edit(remove: boolean): void {
  if (!hover) return
  if (remove) {
    if (world.set(hover.x, hover.y, hover.z, BlockType.Air)) dirty = true
    return
  }
  const type = BLOCKS[selected].type
  if (hover.y < 11 && world.set(hover.x, hover.y + 1, hover.z, type)) dirty = true
}

function rotate(delta: number): void {
  camera.rotation = ((camera.rotation + delta + 4) % 4) as CameraState['rotation']
}

function refreshHotbar(): void {
  hotbar.querySelectorAll('.slot').forEach((slot, i) => slot.classList.toggle('active', i === selected))
}

function jump(): void {
  if (!player.grounded) return
  player.velocityY = 7.2
  player.grounded = false
}

function update(dt: number): void {
  let horizontal = 0
  let vertical = 0
  if (keys.has('w') || keys.has('arrowup') || keys.has('up')) vertical -= 1
  if (keys.has('s') || keys.has('arrowdown') || keys.has('down')) vertical += 1
  if (keys.has('a') || keys.has('arrowleft') || keys.has('left')) horizontal -= 1
  if (keys.has('d') || keys.has('arrowright') || keys.has('right')) horizontal += 1
  if (keys.has(' ') || keys.has('jump')) jump()
  if (horizontal || vertical) {
    const length = Math.hypot(horizontal, vertical)
    horizontal /= length
    vertical /= length
    const angle = camera.rotation * Math.PI / 2
    const dx = (horizontal * Math.cos(angle) - vertical * Math.sin(angle)) * 4 * dt
    const dz = (horizontal * Math.sin(angle) + vertical * Math.cos(angle)) * 4 * dt
    move(dx, dz)
  }
  player.velocityY -= 18 * dt
  player.y += player.velocityY * dt
  const cellX = Math.floor(player.x)
  const cellZ = Math.floor(player.z)
  const floor = world.highestSolid(cellX, cellZ) + 1
  if (player.y <= floor) { player.y = floor; player.velocityY = 0; player.grounded = true }
}

function move(dx: number, dz: number): void {
  const x = Math.max(.2, Math.min(WORLD_SIZE - .2, player.x + dx))
  const z = Math.max(.2, Math.min(WORLD_SIZE - .2, player.z + dz))
  const top = world.highestSolid(Math.floor(x), Math.floor(z)) + 1
  if (top <= player.y + .6) { player.x = x; player.z = z; if (top > player.y) player.y = top }
}

function frame(now: number): void {
  const dt = Math.min(.05, (now - last) / 1000)
  last = now
  update(dt)
  renderer.draw(world, player, camera, hover)
  requestAnimationFrame(frame)
}
requestAnimationFrame(frame)
