import { discoverSecretClues, secretDiscoveryMessage } from '../secrets'
import type { RunState, SecretClueChannel, SecretRoom } from '../types'
import { recordOptionalContent } from '../telemetry'
import { log } from './shared'

export const revealSecretClues = (state: RunState, channel: SecretClueChannel): SecretRoom[] => {
  const rooms = discoverSecretClues(state, channel)
  for (const room of rooms) { recordOptionalContent(state, 'discovered', `secret:${room.id}`); log(state, secretDiscoveryMessage(room)) }
  return rooms
}
