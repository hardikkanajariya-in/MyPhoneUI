export type HelperMode = "RealMode" | "MockMode";
export type DeviceStatus = "Disconnected" | "Pairing" | "Connected" | "CallActive" | "Error";
export type CallStatus = "idle" | "ringing" | "dialing" | "active" | "ended" | "error";
export type CallDirection = "incoming" | "outgoing";

export interface RuntimeInfo {
  helperPort: number;
  platform: string;
  isPackaged: boolean;
}

export interface BluetoothDevice {
  id: string;
  name: string;
  address?: string;
  isConnected: boolean;
  canAdvertiseHfp: boolean;
  statusText: string;
}

export interface AudioEndpoint {
  id: string;
  name: string;
  direction: "input" | "output" | "unknown";
  isBluetoothHandsFreeCandidate: boolean;
  state: string;
}

export interface Contact {
  id: string;
  name: string;
  phone: string;
  favorite: boolean;
  createdAt: string;
}

export interface RecentCall {
  number: string;
  name?: string;
  direction: CallDirection;
  status: "answered" | "missed" | "rejected";
  startedAt: string;
  durationSeconds: number;
}

export interface HelperState {
  selectedDeviceId?: string;
  helperMode: HelperMode;
  deviceStatus: DeviceStatus;
  selectedDevice?: BluetoothDevice;
  audioEndpoints: AudioEndpoint[];
  recentCalls: RecentCall[];
}

export interface LogEntry {
  id: string;
  timestamp: string;
  level: "Debug" | "Info" | "Warning" | "Error";
  source: string;
  message: string;
}

export interface ActiveCall {
  id: string;
  number: string;
  name?: string;
  direction: CallDirection;
  status: CallStatus;
  startedAt?: string;
  durationSeconds: number;
}

export type BridgeInbound =
  | { type: "helper:status"; payload: HelperState }
  | { type: "device:listResult"; payload: BluetoothDevice[] }
  | { type: "device:statusChanged"; payload: HelperState }
  | { type: "contacts:listResult"; payload: Contact[] }
  | { type: "audio:devicesChanged"; payload: AudioEndpoint[] }
  | { type: "call:incoming"; payload: ActiveCall }
  | { type: "call:ringing"; payload: ActiveCall }
  | { type: "call:active"; payload: ActiveCall }
  | { type: "call:ended"; payload: ActiveCall }
  | { type: "call:error"; payload: { message: string; code?: string } }
  | { type: "log:entry"; payload: LogEntry }
  | { type: "logs:listResult"; payload: LogEntry[] }
  | { type: "bridge:response"; requestId: string; payload?: unknown; error?: string };

export interface BridgeRequest<T = unknown> {
  type: string;
  requestId?: string;
  payload?: T;
}

declare global {
  interface Window {
    deskcallRuntime?: {
      getRuntime: () => Promise<RuntimeInfo>;
    };
  }
}
