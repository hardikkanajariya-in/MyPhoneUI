import { Bug, ChevronUp } from "lucide-react";
import { useState } from "react";
import type { LogEntry } from "../lib/types";

export function ConnectionLogs({ logs }: { logs: LogEntry[] }) {
  const [expanded, setExpanded] = useState(false);
  const visibleLogs = expanded ? logs.slice(0, 80) : logs.slice(0, 3);

  return (
    <section className={`glass-panel rounded-2xl transition-all ${expanded ? "max-h-72" : "max-h-24"}`}>
      <button
        type="button"
        onClick={() => setExpanded((value) => !value)}
        className="flex h-12 w-full items-center justify-between px-5 text-left"
      >
        <span className="flex items-center gap-2 text-sm font-semibold text-white">
          <Bug size={17} className="text-cyan-200" />
          Connection logs
        </span>
        <span className="flex items-center gap-2 text-xs text-slate-400">
          {logs.length} entries
          <ChevronUp size={16} className={`transition ${expanded ? "rotate-180" : ""}`} />
        </span>
      </button>
      <div className={`${expanded ? "max-h-60 overflow-y-auto" : "overflow-hidden"} px-5 pb-4`}>
        <div className="space-y-2 font-mono text-xs">
          {visibleLogs.length === 0 ? (
            <p className="text-slate-500">No helper logs yet.</p>
          ) : (
            visibleLogs.map((log) => (
              <div key={log.id} className="grid grid-cols-[92px_74px_90px_1fr] gap-3 rounded-lg bg-slate-950/38 px-3 py-2">
                <span className="text-slate-500">{new Date(log.timestamp).toLocaleTimeString()}</span>
                <span className={levelClass(log.level)}>{log.level}</span>
                <span className="truncate text-cyan-200/80">{log.source}</span>
                <span className="truncate text-slate-300">{log.message}</span>
              </div>
            ))
          )}
        </div>
      </div>
    </section>
  );
}

function levelClass(level: LogEntry["level"]) {
  if (level === "error") {
    return "text-rose-300";
  }
  if (level === "warning") {
    return "text-amber-300";
  }
  if (level === "debug") {
    return "text-slate-500";
  }
  return "text-emerald-300";
}
