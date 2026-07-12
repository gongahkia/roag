import type { WorldSave } from './types'

const DB = 'blockscape'
const STORE = 'worlds'
const KEY = 'active'

const database = (): Promise<IDBDatabase> => new Promise((resolve, reject) => {
  const request = indexedDB.open(DB, 1)
  request.onupgradeneeded = () => request.result.createObjectStore(STORE)
  request.onsuccess = () => resolve(request.result)
  request.onerror = () => reject(request.error)
})

export async function loadWorld(): Promise<WorldSave | undefined> {
  try {
    const db = await database()
    return await new Promise((resolve, reject) => {
      const request = db.transaction(STORE).objectStore(STORE).get(KEY)
      request.onsuccess = () => resolve(request.result as WorldSave | undefined)
      request.onerror = () => reject(request.error)
    })
  } catch { return undefined }
}

export async function saveWorld(save: WorldSave): Promise<void> {
  try {
    const db = await database()
    await new Promise<void>((resolve, reject) => {
      const request = db.transaction(STORE, 'readwrite').objectStore(STORE).put(save, KEY)
      request.onsuccess = () => resolve()
      request.onerror = () => reject(request.error)
    })
  } catch { }
}
