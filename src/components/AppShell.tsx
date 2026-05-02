import { Bluetooth, Radio, Settings } from "lucide-react";
import type { ReactNode } from "react";
import type { HelperState } from "../lib/types";

interface AppShellProps {
  helperConnected: boolean;
  state: HelperState;
  onOpenSettings: () => void;
  children: ReactNode;
}

export function AppShell({ helperConnected, state, onOpenSettings, children }: AppShellProps) {
  return (
    <div className="relative flex h-screen flex-col overflow-hidden px-6 pb-5 pt-6 text-slate-100">
      <header className="mb-5 flex items-center justify-between">
        <div className="flex items-center gap-4">
          <div className="grid h-12 w-12 place-items-center rounded-2xl bg-cyan-400/15 text-cyan-200 soft-ring">
            <Bluetooth size={24} />
          </div>
          <div>
            <h1 className="text-2xl font-semibold tracking-[0.08em] text-white">DeskCall</h1>
            <p className="text-sm text-slate-400">Bluetooth calling console for a paired phone</p>
          </div>
        </div>

        <div className="flex items-center gap-3">
          <StatusPill label={state.deviceStatus} />
          <div className="flex items-center gap-2 rounded-full border border-line bg-white/5 px-4 py-2 text-sm text-slate-300">
            <Radio size={16} className={helperConnected ? "text-emerald-300" : "text-rose-300"} />
            Helper {helperConnected ? "online" : "offline"}
          </div>
          <button
            type="button"
            onClick={onOpenSettings}
            className="grid h-10 w-10 place-items-center rounded-full border border-line bg-white/8 text-slate-200 transition hover:border-cyan-300/40 hover:bg-cyan-300/10"
            aria-label="Open settings"
          >
            <Settings size={18} />
          </button>
        </div>
      </header>

      <div className="min-h-0 flex-1">{children}</div>
    </div>
  );
}

function StatusPill({ label }: { label: string }) {
  const tone =
    label === "connected" || label === "callActive"
      ? "border-emerald-300/30 bg-emerald-300/10 text-emerald-200"
      : label === "pairing"
        ? "border-cyan-300/30 bg-cyan-300/10 text-cyan-200"
        : label === "error"
          ? "border-rose-300/30 bg-rose-300/10 text-rose-200"
          : "border-slate-500/30 bg-slate-400/10 text-slate-300";

  return <div className={`rounded-full border px-4 py-2 text-sm font-medium ${tone}`}>{formatStatus(label)}</div>;
}

function formatStatus(value: string) {
  return value.replace(/([A-Z])/g, " $1").replace(/^./, (letter) => letter.toUpperCase());
}
