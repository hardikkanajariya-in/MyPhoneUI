import { Bluetooth, Headphones, Radio, RefreshCw, TestTube2, X } from "lucide-react";
import type { ReactNode } from "react";
import type { BluetoothDevice, HelperMode, HelperState } from "../lib/types";

interface SettingsPanelProps {
  open: boolean;
  state: HelperState;
  devices: BluetoothDevice[];
  helperConnected: boolean;
  onClose: () => void;
  onSetMode: (mode: HelperMode) => Promise<unknown>;
  onMockIncoming: () => Promise<unknown>;
  onTestLog: () => Promise<unknown>;
  onRefreshDevices: () => Promise<unknown>;
}

export function SettingsPanel({
  open,
  state,
  devices,
  helperConnected,
  onClose,
  onSetMode,
  onMockIncoming,
  onTestLog,
  onRefreshDevices
}: SettingsPanelProps) {
  if (!open) {
    return null;
  }

  return (
    <div className="absolute inset-0 z-30 flex justify-end bg-slate-950/50 backdrop-blur-sm">
      <aside className="h-full w-[460px] border-l border-line bg-slate-950/92 p-6 shadow-panel">
        <div className="mb-6 flex items-center justify-between">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-cyan-200/80">Settings</p>
            <h2 className="mt-1 text-2xl font-semibold text-white">Hardware status</h2>
          </div>
          <button type="button" onClick={onClose} className="grid h-10 w-10 place-items-center rounded-full border border-line bg-white/8 text-slate-200 transition hover:bg-white/12" aria-label="Close settings">
            <X size={18} />
          </button>
        </div>

        <div className="space-y-4">
          <StatusRow icon={<Radio size={18} />} label="Helper" value={helperConnected ? "Online" : "Offline"} tone={helperConnected ? "good" : "bad"} />
          <StatusRow icon={<Bluetooth size={18} />} label="Selected phone" value={state.selectedDevice?.name ?? "None"} tone={state.selectedDevice ? "good" : "warn"} />
          <StatusRow icon={<Headphones size={18} />} label="Hands-free audio" value={handsFreeLabel(state)} tone={handsFreeLabel(state) === "Candidate found" ? "good" : "warn"} />
        </div>

        <div className="mt-6 rounded-2xl border border-line bg-white/5 p-4">
          <h3 className="mb-3 font-semibold text-white">Helper mode</h3>
          <div className="grid grid-cols-2 gap-3">
            {(["mockMode", "realMode"] as HelperMode[]).map((mode) => (
              <button
                key={mode}
                type="button"
                onClick={() => void onSetMode(mode)}
                className={`rounded-xl border px-4 py-3 text-sm font-semibold transition ${
                  state.helperMode === mode ? "border-cyan-300/50 bg-cyan-300/12 text-cyan-100" : "border-line bg-white/5 text-slate-300 hover:bg-white/10"
                }`}
              >
                {formatMode(mode)}
              </button>
            ))}
          </div>
        </div>

        <div className="mt-6 rounded-2xl border border-line bg-white/5 p-4">
          <h3 className="mb-3 font-semibold text-white">Testing</h3>
          <div className="grid grid-cols-2 gap-3">
            <button type="button" onClick={() => void onMockIncoming()} className="flex items-center justify-center gap-2 rounded-xl bg-cyan-300 px-4 py-3 text-sm font-semibold text-slate-950 transition hover:bg-cyan-200">
              <TestTube2 size={16} />
              Incoming
            </button>
            <button type="button" onClick={() => void onTestLog()} className="flex items-center justify-center gap-2 rounded-xl border border-line bg-white/8 px-4 py-3 text-sm font-semibold text-slate-200 transition hover:bg-white/12">
              <RefreshCw size={16} />
              Test log
            </button>
          </div>
        </div>

        <div className="mt-6 rounded-2xl border border-line bg-white/5 p-4">
          <div className="mb-3 flex items-center justify-between">
            <h3 className="font-semibold text-white">Detected devices</h3>
            <button type="button" onClick={() => void onRefreshDevices()} className="text-cyan-200 transition hover:text-cyan-100">
              <RefreshCw size={16} />
            </button>
          </div>
          <div className="max-h-44 space-y-2 overflow-y-auto">
            {devices.map((device) => (
              <div key={device.id} className="rounded-xl border border-line bg-slate-950/38 p-3">
                <p className="truncate text-sm font-medium text-white">{device.name}</p>
                <p className="truncate text-xs text-slate-500">{device.statusText}</p>
              </div>
            ))}
          </div>
        </div>

        <div className="mt-6 rounded-2xl border border-line bg-white/5 p-4">
          <h3 className="mb-3 font-semibold text-white">Audio endpoints</h3>
          <div className="max-h-44 space-y-2 overflow-y-auto">
            {state.audioEndpoints.length === 0 ? (
              <p className="text-sm text-slate-500">No audio endpoint data from helper yet.</p>
            ) : (
              state.audioEndpoints.map((endpoint) => (
                <div key={endpoint.id} className="rounded-xl border border-line bg-slate-950/38 p-3">
                  <p className="truncate text-sm font-medium text-white">{endpoint.name}</p>
                  <p className="text-xs text-slate-500">{endpoint.direction} - {endpoint.state}</p>
                </div>
              ))
            )}
          </div>
        </div>
      </aside>
    </div>
  );
}

function StatusRow({ icon, label, value, tone }: { icon: ReactNode; label: string; value: string; tone: "good" | "warn" | "bad" }) {
  const color = tone === "good" ? "text-emerald-300" : tone === "bad" ? "text-rose-300" : "text-amber-300";
  return (
    <div className="flex items-center gap-3 rounded-2xl border border-line bg-white/5 p-4">
      <div className={`grid h-10 w-10 place-items-center rounded-xl bg-white/8 ${color}`}>{icon}</div>
      <div className="min-w-0">
        <p className="text-xs text-slate-500">{label}</p>
        <p className="truncate text-sm font-semibold text-white">{value}</p>
      </div>
    </div>
  );
}

function handsFreeLabel(state: HelperState) {
  return state.audioEndpoints.some((endpoint) => endpoint.isBluetoothHandsFreeCandidate) ? "Candidate found" : "Not detected";
}

function formatMode(mode: HelperMode) {
  return mode === "mockMode" ? "MockMode" : "RealMode";
}
