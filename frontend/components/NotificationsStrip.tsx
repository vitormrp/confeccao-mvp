"use client";

import { useEffect, useState } from "react";
import { getAwaiting, type AwaitingItemDto } from "@/lib/api/dispatch";
import { stageLabel } from "@/lib/labels";

type State =
  | { kind: "loading" }
  | { kind: "ready"; items: AwaitingItemDto[] }
  | { kind: "error"; message: string };

/**
 * Compact alerts above the dashboard summarising what's waiting on the manager.
 * The full dispatch UI is in <code>DispatchPanel</code>; this strip just gives
 * a glanceable count per channel.
 */
export function NotificationsStrip() {
  const [state, setState] = useState<State>({ kind: "loading" });

  useEffect(() => {
    let cancelled = false;
    getAwaiting()
      .then((items) => {
        if (!cancelled) setState({ kind: "ready", items });
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

  if (state.kind === "loading") return null;
  if (state.kind === "error") return null;
  if (state.items.length === 0) return null;

  const totals = {
    generic: { count: 0, qty: 0, byStage: new Map<string, number>() },
    sewing: { count: 0, qty: 0 },
    laundry: { count: 0, qty: 0 },
  };

  for (const item of state.items) {
    if (item.dispatchKind === "generic") {
      totals.generic.count++;
      totals.generic.qty += item.quantity;
      totals.generic.byStage.set(
        item.stage,
        (totals.generic.byStage.get(item.stage) ?? 0) + item.quantity,
      );
    } else if (item.dispatchKind === "per-user") {
      totals.sewing.count++;
      totals.sewing.qty += item.quantity;
    } else if (item.dispatchKind === "package") {
      totals.laundry.count++;
      totals.laundry.qty += item.quantity;
    }
  }

  const alerts: { tone: "ready" | "warn"; msg: string }[] = [];
  if (totals.generic.count > 0) {
    const stages = Array.from(totals.generic.byStage.entries())
      .map(([s, q]) => `${q} para ${stageLabel(s)}`)
      .join(" · ");
    alerts.push({
      tone: "ready",
      msg: `${totals.generic.qty} peças prontas para próxima etapa — ${stages}`,
    });
  }
  if (totals.sewing.count > 0) {
    alerts.push({
      tone: "ready",
      msg: `${totals.sewing.qty} peças aguardando distribuição para costureira (${totals.sewing.count} ${totals.sewing.count === 1 ? "lote" : "lotes"})`,
    });
  }
  if (totals.laundry.count > 0) {
    alerts.push({
      tone: "ready",
      msg: `${totals.laundry.qty} peças aguardando montagem de pacote de lavanderia`,
    });
  }

  if (alerts.length === 0) return null;

  return (
    <div className="mb-3 space-y-2">
      {alerts.map((a, i) => (
        <div
          key={i}
          className={
            a.tone === "ready"
              ? "px-3 py-2 rounded-lg border border-green-border bg-green-light text-green text-xs"
              : "px-3 py-2 rounded-lg border border-amber-border bg-amber-light text-amber text-xs"
          }
        >
          {a.msg}
        </div>
      ))}
    </div>
  );
}
