import type { Records, RunState } from './types'

const DB = 'blockscape-expedition-v2'
const STORE = 'state'
const RUN = 'active-run'
const RECORDS = 'records'

const database = (): Promise<IDBDatabase> => new Promise((resolve, reject) => {
  const request = indexedDB.open(DB, 1)
  request.onupgradeneeded = () => request.result.createObjectStore(STORE)
  request.onsuccess = () => resolve(request.result)
  request.onerror = () => reject(request.error)
})

const get = async <T>(key: string): Promise<T | undefined> => {
  try {
    const db = await database()
    return await new Promise((resolve, reject) => {
      const request = db.transaction(STORE).objectStore(STORE).get(key)
      request.onsuccess = () => resolve(request.result as T | undefined)
      request.onerror = () => reject(request.error)
    })
  } catch { return undefined }
}

const put = async <T>(key: string, value: T): Promise<void> => {
  try {
    const db = await database()
    await new Promise<void>((resolve, reject) => {
      const request = db.transaction(STORE, 'readwrite').objectStore(STORE).put(value, key)
      request.onsuccess = () => resolve()
      request.onerror = () => reject(request.error)
    })
  } catch { }
}

export const loadRun = () => get<RunState>(RUN)
export const saveRun = (state: RunState) => put(RUN, state)
export async function deleteRun(): Promise<void> {
  try {
    const db = await database()
    await new Promise<void>((resolve, reject) => {
      const request = db.transaction(STORE, 'readwrite').objectStore(STORE).delete(RUN)
      request.onsuccess = () => resolve()
      request.onerror = () => reject(request.error)
    })
  } catch { }
}

const emptyRecords = (): Records => ({ bestDepth: 0, wins: 0, deaths: 0, runs: [] })
export const loadRecords = async (): Promise<Records> => (await get<Records>(RECORDS)) ?? emptyRecords()
export const saveRecords = (records: Records) => put(RECORDS, records)
