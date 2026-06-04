"use client";

import { useEffect, useState } from "react";
import { getHealth, type HealthResponse } from "@/lib/api/health";

type State =
  | { kind: "loading" }
  | { kind: "ok"; data: HealthResponse }
  | { kind: "error"; message: string };

export function HealthBadge() {
  const [state, setState] = useState<State>({ kind: "loading" });

  useEffect(() => {
    let cancelled = false;
    getHealth()
      .then((data) => {
        if (!cancelled) setState({ kind: "ok", data });
      })
      .catch((err: unknown) => {
        if (!cancelled)
          setState({
            kind: "error",
            message: err instanceof Error ? err.message : String(err),
          });
      });
    return () => {
      cancelled = true;
    };
  }, []);

  if (state.kind === "loading") {
    return (
      <div className="rounded-lg border border-border bg-card p-4 shadow-card">
        <div className="text-xs text-text3">Backend health</div>
        <div className="text-sm">Verificando…</div>
      </div>
    );
  }

  if (state.kind === "error") {
    return (
      <div className="rounded-lg border border-red-border bg-red-light p-4 shadow-card">
        <div className="text-xs text-red font-semibold">Backend unreachable</div>
        <div className="text-xs text-red mt-1 break-all">{state.message}</div>
      </div>
    );
  }

  return (
    <div className="rounded-lg border border-green-border bg-green-light p-4 shadow-card">
      <div className="text-xs text-green font-semibold">Backend OK</div>
      <div className="text-xs text-green mt-1">
        DB: {state.data.database} · {new Date(state.data.timestamp).toLocaleString("pt-BR")}
      </div>
    </div>
  );
}
