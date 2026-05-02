import { Delete, PhoneCall } from "lucide-react";
import { useState } from "react";

const keys = [
  ["1", ""],
  ["2", "ABC"],
  ["3", "DEF"],
  ["4", "GHI"],
  ["5", "JKL"],
  ["6", "MNO"],
  ["7", "PQRS"],
  ["8", "TUV"],
  ["9", "WXYZ"],
  ["*", ""],
  ["0", "+"],
  ["#", ""]
];

interface DialPadProps {
  disabled?: boolean;
  onDial: (number: string) => Promise<unknown>;
}

export function DialPad({ disabled, onDial }: DialPadProps) {
  const [number, setNumber] = useState("");

  function press(value: string) {
    setNumber((current) => `${current}${value}`);
  }

  function backspace() {
    setNumber((current) => current.slice(0, -1));
  }

  function dial() {
    if (number.trim()) {
      void onDial(number.trim());
    }
  }

  return (
    <section className="glass-panel min-h-0 flex-1 rounded-2xl p-5">
      <div className="mb-5 flex items-center justify-between">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-cyan-200/80">Dial pad</p>
          <h2 className="mt-1 text-xl font-semibold text-white">Manual call</h2>
        </div>
        <button
          type="button"
          onClick={backspace}
          className="grid h-10 w-10 place-items-center rounded-full border border-line bg-white/8 text-slate-200 transition hover:bg-white/12"
          aria-label="Delete digit"
        >
          <Delete size={18} />
        </button>
      </div>

      <div className="mb-5 flex h-16 items-center justify-center rounded-2xl border border-line bg-slate-950/40 px-4 text-3xl font-semibold tracking-[0.16em] text-white">
        {number || <span className="text-base font-normal tracking-normal text-slate-500">Enter number</span>}
      </div>

      <div className="grid grid-cols-3 gap-3">
        {keys.map(([digit, letters]) => (
          <button
            key={digit}
            type="button"
            onClick={() => press(digit)}
            className="h-16 rounded-2xl border border-line bg-white/7 text-white transition hover:border-cyan-300/40 hover:bg-cyan-300/10 active:scale-[0.98]"
          >
            <span className="block text-2xl font-semibold">{digit}</span>
            <span className="block text-[10px] font-medium tracking-[0.18em] text-slate-500">{letters || "\u00a0"}</span>
          </button>
        ))}
      </div>

      <button
        type="button"
        disabled={disabled || !number.trim()}
        onClick={dial}
        className="mt-5 flex h-14 w-full items-center justify-center gap-3 rounded-2xl bg-emerald-400 text-base font-bold text-emerald-950 shadow-lg shadow-emerald-950/30 transition hover:bg-emerald-300 disabled:cursor-not-allowed disabled:opacity-40"
      >
        <PhoneCall size={20} />
        Call
      </button>
    </section>
  );
}
