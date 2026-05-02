import { PhoneCall, PhoneOff, UserRound } from "lucide-react";
import type { ActiveCall } from "../lib/types";

interface IncomingCallModalProps {
  call: ActiveCall;
  onAnswer: () => Promise<unknown>;
  onReject: () => Promise<unknown>;
}

export function IncomingCallModal({ call, onAnswer, onReject }: IncomingCallModalProps) {
  return (
    <div className="absolute inset-0 z-20 grid place-items-center bg-slate-950/66 backdrop-blur-md">
      <section className="w-[390px] rounded-[2rem] border border-cyan-200/20 bg-slate-950/85 p-8 text-center shadow-panel">
        <div className="relative mx-auto grid h-36 w-36 place-items-center">
          <div className="absolute inset-0 animate-ping rounded-full bg-cyan-300/12" />
          <div className="absolute inset-4 rounded-full border border-cyan-200/20 bg-cyan-300/8" />
          <div className="relative grid h-24 w-24 place-items-center rounded-[2rem] bg-cyan-300/14 text-cyan-100 soft-ring">
            <UserRound size={42} />
          </div>
        </div>

        <p className="mt-4 text-sm font-semibold uppercase tracking-[0.24em] text-cyan-200">Incoming call</p>
        <h2 className="mt-2 truncate text-3xl font-semibold text-white">{call.name ?? "Unknown caller"}</h2>
        <p className="mt-2 text-base text-slate-400">{call.number}</p>

        <div className="mt-8 grid grid-cols-2 gap-4">
          <button
            type="button"
            onClick={() => void onReject()}
            className="grid h-16 place-items-center rounded-2xl bg-rose-500 text-white shadow-lg shadow-rose-950/40 transition hover:bg-rose-400"
            aria-label="Reject call"
          >
            <PhoneOff size={26} />
          </button>
          <button
            type="button"
            onClick={() => void onAnswer()}
            className="grid h-16 place-items-center rounded-2xl bg-emerald-400 text-emerald-950 shadow-lg shadow-emerald-950/40 transition hover:bg-emerald-300"
            aria-label="Answer call"
          >
            <PhoneCall size={26} />
          </button>
        </div>
      </section>
    </div>
  );
}
