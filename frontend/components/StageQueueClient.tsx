"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import {
  completeStageItem,
  getStageQueue,
  type StageQueueItemDto,
} from "@/lib/api/stages";
import { useCurrentUser } from "@/lib/currentUser";
import { stageLabel } from "@/lib/labels";

type State =
  | { kind: "idle" }
  | { kind: "loading" }
  | { kind: "ready"; items: StageQueueItemDto[] }
  | { kind: "error"; message: string };

type QueueLoader = (userId: string | null) => Promise<StageQueueItemDto[]>;

/**
 * Shared client component for operator stage queues. Defaults to the generic
 * stage queue (shared across all users for interfacing/buttoning/labeling/
 * pressing); the sewing page passes a custom <code>queueLoader</code> that
 * filters by the current seamstress.
 *
 * <code>requireUserForQueue</code>: when true, no items are fetched until a
 * user is picked. Used by sewing — without a seamstress, "queue" has no meaning.
 */
export function StageQueueClient({
  stage,
  queueLoader,
  requireUserForQueue = false,
  emptyHint,
}: {
  stage: string;
  queueLoader?: QueueLoader;
  requireUserForQueue?: boolean;
  emptyHint?: string;
}) {
  const router = useRouter();
  const { userId } = useCurrentUser();
  const [state, setState] = useState<State>({ kind: "idle" });
  const [partials, setPartials] = useState<Record<string, string>>({});
  const [submittingId, setSubmittingId] = useState<string | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const refresh = async () => {
    if (requireUserForQueue && !userId) {
      setState({ kind: "ready", items: [] });
      return;
    }
    setState({ kind: "loading" });
    try {
      const items = queueLoader
        ? await queueLoader(userId)
        : await getStageQueue(stage);
      setState({ kind: "ready", items });
    } catch (err) {
      setState({
        kind: "error",
        message: err instanceof Error ? err.message : String(err),
      });
    }
  };

  useEffect(() => {
    refresh();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [stage, userId]);

  const submit = async (item: StageQueueItemDto, quantity: number) => {
    if (!userId) {
      setSubmitError("Selecione um operador acima antes de confirmar.");
      return;
    }
    setSubmitError(null);
    setSubmittingId(item.pipelineItemId);
    try {
      await completeStageItem(stage, item.pipelineItemId, userId, quantity);
      setPartials((prev) => {
        const next = { ...prev };
        delete next[item.pipelineItemId];
        return next;
      });
      await refresh();
      router.refresh();
    } catch (err) {
      setSubmitError(err instanceof Error ? err.message : String(err));
    } finally {
      setSubmittingId(null);
    }
  };

  if (state.kind === "loading" || state.kind === "idle") {
    return (
      <div className="rounded-2xl border border-border bg-card p-5 shadow-card">
        <p className="text-xs text-text3">Carregando…</p>
      </div>
    );
  }

  if (state.kind === "error") {
    return (
      <div className="rounded-2xl border border-red-border bg-red-light p-5">
        <p className="text-xs text-red font-semibold">Erro</p>
        <p className="text-xs text-red mt-1 break-all">{state.message}</p>
      </div>
    );
  }

  if (state.items.length === 0) {
    return (
      <div className="rounded-2xl border border-border bg-card p-5 shadow-card">
        <p className="text-xs text-text3">
          {emptyHint ??
            `Nenhum lote em andamento para ${stageLabel(stage).toLowerCase()} no momento.`}
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      {submitError && (
        <div className="text-xs text-red bg-red-light border border-red-border rounded-lg px-3 py-2">
          {submitError}
        </div>
      )}
      {state.items.map((item) => {
        const remaining = item.quantityTotal - item.quantityDone;
        const progressPct =
          item.quantityTotal === 0
            ? 0
            : Math.round((item.quantityDone / item.quantityTotal) * 100);
        const partial = partials[item.pipelineItemId] ?? "";
        return (
          <div
            key={item.pipelineItemId}
            className="rounded-2xl border border-border bg-card p-4 shadow-card"
          >
            <div className="flex items-start justify-between gap-2">
              <div>
                <div className="text-sm font-medium">
                  {item.modelName}{" "}
                  <span className="text-text3 font-normal">tam. {item.size}</span>{" "}
                  — {item.quantityTotal} peças
                </div>
                <div className="text-[11px] text-text3 mt-0.5">
                  Ordem #{item.orderNumber} ·{" "}
                  <span
                    className="inline-block w-2 h-2 rounded-full mr-1 align-middle border border-black/10"
                    style={{ background: item.colorHex }}
                  />
                  {item.colorName}
                  {item.colorHasLining && (
                    <span className="ml-2 text-amber">com forro</span>
                  )}
                </div>
              </div>
              <span className="inline-block px-2 py-0.5 rounded-full text-[11px] bg-accent-light text-accent border border-accent-border">
                Em andamento
              </span>
            </div>

            <div className="text-[11px] text-text2 mt-2 flex gap-3">
              <span>
                Total: <strong className="font-mono">{item.quantityTotal}</strong>
              </span>
              <span>
                Feitas: <strong className="font-mono">{item.quantityDone}</strong>
              </span>
              <span>
                Restam: <strong className="font-mono">{remaining}</strong>
              </span>
            </div>
            <div className="h-1 bg-bg3 rounded-full overflow-hidden mt-1.5">
              <div
                className="h-full bg-accent transition-[width]"
                style={{ width: `${progressPct}%` }}
              />
            </div>

            <div className="flex flex-wrap gap-2 mt-3 items-center">
              <button
                type="button"
                disabled={submittingId !== null || !userId}
                onClick={() => submit(item, remaining)}
                className="px-3.5 py-1.5 rounded-lg text-xs font-medium bg-green text-white border border-green hover:opacity-90 transition-opacity disabled:opacity-60"
              >
                {submittingId === item.pipelineItemId
                  ? "Enviando..."
                  : "✓ Tudo pronto"}
              </button>
              <input
                type="number"
                min={1}
                max={remaining}
                value={partial}
                onChange={(e) =>
                  setPartials((prev) => ({
                    ...prev,
                    [item.pipelineItemId]: e.target.value,
                  }))
                }
                placeholder="Qtd parcial"
                className="w-28 text-xs px-2 py-1.5 border border-border2 rounded-lg font-mono"
              />
              <button
                type="button"
                disabled={submittingId !== null || !userId}
                onClick={() => {
                  const n = parseInt(partial, 10);
                  if (!Number.isFinite(n) || n <= 0) {
                    setSubmitError("Informe uma quantidade parcial > 0.");
                    return;
                  }
                  if (n > remaining) {
                    setSubmitError(
                      `Quantidade parcial maior que o restante (${remaining}).`,
                    );
                    return;
                  }
                  submit(item, n);
                }}
                className="px-3.5 py-1.5 rounded-lg text-xs font-medium bg-accent-light text-accent border border-accent-border hover:opacity-90 transition-opacity disabled:opacity-60"
              >
                Confirmar parcial
              </button>
            </div>
          </div>
        );
      })}
    </div>
  );
}
