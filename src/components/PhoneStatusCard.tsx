import { BluetoothConnected, BluetoothSearching, Loader2, Phone, Power, RefreshCw } from "lucide-react";
import type { BluetoothDevice, HelperState } from "../lib/types";

interface PhoneStatusCardProps {
  state: HelperState;
  devices: BluetoothDevice[];
  helperConnected: boolean;
  onRefreshDevices: () => Promise<unknown>;
  onSelectDevice: (deviceId: string) => Promise<unknown>;
  onConnect: () => Promise<unknown>;
  onDisconnect: () => Promise<unknown>;
}

export function PhoneStatusCard({
  state,
  devices,
  helperConnected,
  onRefreshDevices,
  onSelectDevice,
  onConnect,
  onDisconnect
}: PhoneStatusCardProps) {
  const selected = state.selectedDevice ?? devices.find((device) => device.id === state.selectedDeviceId);

  return (
    <section className="glass-panel flex min-h-0 flex-1 flex-col rounded-2xl p-5">
      <div className="mb-5 flex items-center justify-between">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-cyan-200/80">Selected phone</p>
          <h2 className="mt-1 text-xl font-semibold text-white">{selected?.name ?? "No phone selected"}</h2>
        </div>
        <button
          type="button"
          onClick={() => void onRefreshDevices()}
          className="grid h-10 w-10 place-items-center rounded-full border border-line bg-white/8 text-slate-200 transition hover:border-cyan-300/40 hover:bg-cyan-300/10"
          aria-label="Refresh paired Bluetooth devices"
        >
          <RefreshCw size={17} />
        </button>
      </div>

      <div className="rounded-2xl border border-cyan-300/15 bg-cyan-300/8 p-5 shadow-glow">
        <div className="mx-auto grid h-24 w-24 place-items-center rounded-[2rem] border border-cyan-200/20 bg-slate-950/60 text-cyan-200">
          {state.deviceStatus === "pairing" ? <Loader2 className="animate-spin" size={34} /> : <Phone size={36} />}
        </div>
        <div className="mt-5 text-center">
          <p className="text-sm text-slate-400">{selected?.address ?? "Pair your phone in Windows Bluetooth settings first"}</p>
          <p className="mt-2 text-sm font-medium text-slate-200">{selected?.statusText ?? "Waiting for paired devices"}</p>
        </div>
        <div className="mt-5 grid grid-cols-2 gap-3">
          <button
            type="button"
            disabled={!helperConnected || !selected}
            onClick={() => void onConnect()}
            className="flex items-center justify-center gap-2 rounded-xl bg-cyan-300 px-4 py-3 text-sm font-semibold text-slate-950 transition hover:bg-cyan-200 disabled:cursor-not-allowed disabled:opacity-40"
          >
            <BluetoothConnected size={17} />
            Connect
          </button>
          <button
            type="button"
            disabled={!helperConnected || !selected}
            onClick={() => void onDisconnect()}
            className="flex items-center justify-center gap-2 rounded-xl border border-line bg-white/8 px-4 py-3 text-sm font-semibold text-slate-200 transition hover:bg-white/12 disabled:cursor-not-allowed disabled:opacity-40"
          >
            <Power size={17} />
            Disconnect
          </button>
        </div>
      </div>

      <div className="mt-5 flex min-h-0 flex-1 flex-col">
        <div className="mb-3 flex items-center justify-between">
          <h3 className="text-sm font-semibold text-white">Paired devices</h3>
          <span className="text-xs text-slate-500">{devices.length} found</span>
        </div>
        <div className="min-h-0 space-y-2 overflow-y-auto pr-1">
          {devices.length === 0 ? (
            <div className="rounded-xl border border-dashed border-slate-600/60 p-4 text-sm text-slate-400">
              Use refresh after pairing your phone in Windows Settings.
            </div>
          ) : (
            devices.map((device) => (
              <button
                key={device.id}
                type="button"
                onClick={() => void onSelectDevice(device.id)}
                className={`w-full rounded-xl border p-3 text-left transition ${
                  state.selectedDeviceId === device.id
                    ? "border-cyan-300/40 bg-cyan-300/10"
                    : "border-line bg-white/5 hover:border-slate-400/40 hover:bg-white/8"
                }`}
              >
                <div className="flex items-center gap-3">
                  <BluetoothSearching size={18} className={device.canAdvertiseHfp ? "text-cyan-200" : "text-slate-500"} />
                  <div className="min-w-0 flex-1">
                    <p className="truncate text-sm font-medium text-white">{device.name}</p>
                    <p className="truncate text-xs text-slate-500">{device.address ?? device.id}</p>
                  </div>
                </div>
              </button>
            ))
          )}
        </div>
      </div>
    </section>
  );
}
