import type { BridgeInbound, BridgeRequest, RuntimeInfo } from "./types";

type Listener = (message: BridgeInbound) => void;
type ConnectionListener = (connected: boolean) => void;

export class DeskCallBridge {
  private socket: WebSocket | null = null;
  private listeners = new Set<Listener>();
  private connectionListeners = new Set<ConnectionListener>();
  private pending = new Map<string, { resolve: (value: unknown) => void; reject: (reason?: unknown) => void }>();
  private runtime: RuntimeInfo | null = null;
  private reconnectTimer: number | null = null;
  private connectPromise: Promise<void> | null = null;

  async connect() {
    if (this.socket && (this.socket.readyState === WebSocket.OPEN || this.socket.readyState === WebSocket.CONNECTING)) {
      return this.connectPromise ?? Promise.resolve();
    }

    this.runtime = await this.getRuntime();
    this.connectPromise = this.openSocket();
    return this.connectPromise;
  }

  onMessage(listener: Listener) {
    this.listeners.add(listener);
    return () => this.listeners.delete(listener);
  }

  onConnection(listener: ConnectionListener) {
    this.connectionListeners.add(listener);
    return () => this.connectionListeners.delete(listener);
  }

  request<TPayload = unknown, TResult = unknown>(type: string, payload?: TPayload): Promise<TResult> {
    const requestId = crypto.randomUUID();
    const message: BridgeRequest<TPayload> = { type, requestId, payload };

    if (!this.socket || this.socket.readyState !== WebSocket.OPEN) {
      return Promise.reject(new Error("DeskCall helper is not connected."));
    }

    return new Promise<TResult>((resolve, reject) => {
      this.pending.set(requestId, {
        resolve: (value) => resolve(value as TResult),
        reject
      });
      this.socket?.send(JSON.stringify(message));
      window.setTimeout(() => {
        if (this.pending.has(requestId)) {
          this.pending.delete(requestId);
          reject(new Error(`Timed out waiting for ${type}`));
        }
      }, 10000);
    });
  }

  send<TPayload = unknown>(type: string, payload?: TPayload) {
    if (!this.socket || this.socket.readyState !== WebSocket.OPEN) {
      throw new Error("DeskCall helper is not connected.");
    }

    this.socket.send(JSON.stringify({ type, payload }));
  }

  private async getRuntime(): Promise<RuntimeInfo> {
    if (window.deskcallRuntime) {
      return window.deskcallRuntime.getRuntime();
    }

    return { helperPort: 49321, platform: navigator.platform, isPackaged: false };
  }

  private openSocket(): Promise<void> {
    const port = this.runtime?.helperPort ?? 49321;
    this.socket = new WebSocket(`ws://127.0.0.1:${port}/deskcall/`);

    return new Promise((resolve) => {
      this.socket!.onopen = () => {
        this.emitConnection(true);
        this.request("helper:getStatus").catch(() => undefined);
        this.request("contacts:list").catch(() => undefined);
        this.request("logs:list").catch(() => undefined);
        resolve();
      };

      this.socket!.onmessage = (event) => {
        const message = JSON.parse(event.data) as BridgeInbound;
        if (message.type === "bridge:response") {
          const pending = this.pending.get(message.requestId);
          if (pending) {
            this.pending.delete(message.requestId);
            if (message.error) {
              pending.reject(new Error(message.error));
            } else {
              pending.resolve(message.payload);
            }
          }
          return;
        }

        this.listeners.forEach((listener) => listener(message));
      };

      this.socket!.onclose = () => {
        this.emitConnection(false);
        this.connectPromise = null;
        this.scheduleReconnect();
      };

      this.socket!.onerror = () => {
        this.emitConnection(false);
      };
    });
  }

  private scheduleReconnect() {
    if (this.reconnectTimer) {
      return;
    }

    this.reconnectTimer = window.setTimeout(() => {
      this.reconnectTimer = null;
      this.openSocket();
    }, 1500);
  }

  private emitConnection(connected: boolean) {
    this.connectionListeners.forEach((listener) => listener(connected));
  }
}

export const deskCallBridge = new DeskCallBridge();
