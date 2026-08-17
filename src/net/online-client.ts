import type { ClientMessage, RoomSnapshot, ServerMessage } from './protocol';

export interface OnlineClientHandlers {
  onRoom(room: RoomSnapshot): void;
  onJoined(playerId: string, reconnectToken: string): void;
  onError(message: string): void;
  onConnection(connected: boolean): void;
}

export class OnlineClient {
  private socket?: WebSocket;

  constructor(private readonly handlers: OnlineClientHandlers) {}

  connect(url: string, onOpen?: () => void) {
    this.disconnect();
    const socket = new WebSocket(url);
    this.socket = socket;
    socket.addEventListener('open', () => {
      this.handlers.onConnection(true);
      onOpen?.();
    });
    socket.addEventListener('close', () => {
      if (this.socket === socket) this.handlers.onConnection(false);
    });
    socket.addEventListener('error', () => this.handlers.onError('unable to reach the game server'));
    socket.addEventListener('message', (event) => {
      try {
        const message = JSON.parse(String(event.data)) as ServerMessage;
        if (message.type === 'room-state') this.handlers.onRoom(message.room);
        if (message.type === 'joined') this.handlers.onJoined(message.playerId, message.reconnectToken);
        if (message.type === 'error') this.handlers.onError(message.message);
      } catch { this.handlers.onError('server sent an invalid response'); }
    });
  }

  send(message: ClientMessage) {
    if (this.socket?.readyState !== WebSocket.OPEN) {
      this.handlers.onError('not connected to the game server');
      return false;
    }
    this.socket.send(JSON.stringify(message));
    return true;
  }

  disconnect() {
    this.socket?.close();
    this.socket = undefined;
  }
}
