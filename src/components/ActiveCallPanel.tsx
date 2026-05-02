import { PhoneCall, PhoneOff, Timer, UserRound, X } from "lucide-react";
import type { ActiveCall } from "../lib/types";

interface ActiveCallPanelProps {
  activeCall: ActiveCall | null;
  onAnswer: () => Promise<unknown>;
  onReject: () => Promise<unknown>;
  onEnd: () => Promise<unknown>;
}

export function ActiveCallPanel({ activeCall, onAnswer, onReject, onEnd }: ActiveCallPanelProps) {
  return (
    <section className="glass-panel rounded-2xl p-5">
      <div className="flex items-center justify-between">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-cyan-200/80">Call session</p>
          <h2 className="mt-1 text-xl font-semibold text-white">{activeCall ? callTitle(activeCall) : "Ready"}</h2>
        </div>
        <div className="rounded-full border border-line bg-white/8 px-3 py-1 text-xs font-medium text-slate-300">
          {activeCall?.status ?? "idle"}
        </div>
      </div>

      <div className="mt-5 flex items-center gap-5 rounded-2xl border border-line bg-slate-950/35 p-5">
        <div className="grid h-20 w-20 place-items-center rounded-[1.7rem] bg-cyan-300/10 text-cyan-200 soft-ring">
          <UserRound size={34} />
        </div>
        <div className="min-w-0 flex-1">
          <p className="truncate text-2xl font-semibold text-white">{activeCall?.name ?? activeCall?.number ?? "No active call"}</p>
          <p className="mt-1 truncate text-sm text-slate-400">{activeCall?.number ?? "Select a contact or enter a number"}</p>
          <div className="mt-3 flex items-center gap-2 text-sm text-slate-300">
            <Timer size={16} className="text-cyan-200" />
            {formatDuration(activeCall?.durationSeconds ?? 0)}
          </div>
        </div>
      </div>

      <div className="mt-5 grid grid-cols-3 gap-3">
        <button
          type="button"
          disabled={!activeCall || activeCall.status !== "ringing"}
          onClick={() => void onAnswer()}
          className="flex h-12 items-center justify-center gap-2 rounded-xl bg-emerald-400 font-semibold text-emerald-950 transition hover:bg-emerald-300 disabled:cursor-not-allowed disabled:opacity-35"
        >
          <PhoneCall size={18} />
          Answer
        </button>
        <button
          type="button"
          disabled={!activeCall || activeCall.status !== "ringing"}
          onClick={() => void onReject()}
          className="flex h-12 items-center justify-center gap-2 rounded-xl bg-rose-400 font-semibold text-rose-950 transition hover:bg-rose-300 disabled:cursor-not-allowed disabled:opacity-35"
        >
          <X size={18} />
          Reject
        </button>
        <button
          type="button"
          disabled={!activeCall || activeCall.status === "ended"}
          onClick={() => void onEnd()}
          className="flex h-12 items-center justify-center gap-2 rounded-xl border border-line bg-white/8 font-semibold text-slate-200 transition hover:bg-white/12 disabled:cursor-not-allowed disabled:opacity-35"
        >
          <PhoneOff size={18} />
          End
        </button>
      </div>
    </section>
  );
}

function callTitle(call: ActiveCall) {
  if (call.status === "ringing") {
    return call.direction === "incoming" ? "Incoming call" : "Ringing";
  }

  if (call.status === "active") {
    return "Call active";
  }

  return call.direction === "outgoing" ? "Outgoing call" : "Call ended";
}

function formatDuration(seconds: number) {
  const minutes = Math.floor(seconds / 60)
    .toString()
    .padStart(2, "0");
  const remaining = Math.floor(seconds % 60)
    .toString()
    .padStart(2, "0");
  return `${minutes}:${remaining}`;
}
