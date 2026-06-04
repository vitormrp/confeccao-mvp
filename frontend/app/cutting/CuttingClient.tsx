"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import {
  getCuttingQueue,
  registerCut,
  type CutterOrderDto,
} from "@/lib/api/cutting";
import { useCurrentUser } from "@/lib/currentUser";

type State =
  | { kind: "idle" }
  | { kind: "loading" }
  | { kind: "ready"; orders: CutterOrderDto[] }
  | { kind: "error"; message: string };

export function CuttingClient() {
  const router = useRouter();
  const { userId } = useCurrentUser();
  const [state, setState] = useState<State>({ kind: "idle" });
  const [drafts, setDrafts] = useState<Record<string, string>>({});
  const [submittingOrderId, setSubmittingOrderId] = useState<string | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);

  useEffect(() => {
    if (!userId) {
      setState({ kind: "idle" });
      return;
    }
    let cancelled = false;
    setState({ kind: "loading" });
    getCuttingQueue(userId)
      .then((orders) => {
        if (!cancelled) setState({ kind: "ready", orders });
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
  }, [userId]);

  const submit = async (order: CutterOrderDto) => {
    if (!userId) return;
    setSubmitError(null);
    setSubmittingOrderId(order.orderId);
    try {
      const cuts = order.items.flatMap((item) => {
        const raw = drafts[item.orderItemId];
        const n = parseInt(raw ?? "", 10);
        return Number.isFinite(n) && n > 0
          ? [{ orderItemId: item.orderItemId, quantityCut: n }]
          : [];
      });
      if (cuts.length === 0) {
        setSubmitError("Informe ao menos uma quantidade > 0.");
        setSubmittingOrderId(null);
        return;
      }
      await registerCut(order.orderId, userId, { cuts });
      setDrafts((prev) => {
        const next = { ...prev };
        for (const item of order.items) delete next[item.orderItemId];
        return next;
      });
      // Refetch & refresh dashboard.
      const refreshed = await getCuttingQueue(userId);
      setState({ kind: "ready", orders: refreshed });
      router.refresh();
    } catch (err) {
      setSubmitError(err instanceof Error ? err.message : String(err));
    } finally {
      setSubmittingOrderId(null);
    }
  };

  if (!userId) {
    return (
      <div className="rounded-2xl border border-border bg-card p-5 shadow-card">
        <p className="text-xs text-text3">
          Selecione um cortador acima para ver as ordens disponíveis.
        </p>
      </div>
    );
  }

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

  if (state.orders.length === 0) {
    return (
      <div className="rounded-2xl border border-border bg-card p-5 shadow-card">
        <p className="text-xs text-text3">Nenhuma tarefa de corte no momento.</p>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      {state.orders.map((order) => (
        <div
          key={order.orderId}
          className="rounded-2xl border border-border bg-card p-5 shadow-card"
        >
          <div className="bg-accent-light border border-accent-border rounded-lg p-3 text-xs text-accent mb-3">
            <strong>Ordem #{order.number}</strong> — Tecido:{" "}
            <strong>{order.fabricCode}</strong> · Cor:{" "}
            <span
              className="inline-block w-2 h-2 rounded-full mr-1 align-middle border border-black/10"
              style={{ background: order.colorHex }}
            />
            <strong>{order.colorName}</strong>
            {order.colorHasLining && (
              <span className="ml-2 px-2 py-0.5 rounded-full text-[10px] bg-amber-light text-amber border border-amber-border">
                com forro
              </span>
            )}
            {order.instructions && <div className="mt-1">{order.instructions}</div>}
          </div>

          <div className="text-sm font-semibold mb-1">
            Registrar corte — Ordem #{order.number}
          </div>
          <p className="text-[11px] text-text3 mb-3">
            Preencha a quantidade real cortada de cada peça. Itens em branco contam como 0.
          </p>

          <div className="overflow-x-auto mb-3">
            <table className="text-xs w-full">
              <thead>
                <tr className="bg-bg2">
                  <th className="text-left border border-border px-2 py-1.5 text-[11px]">
                    Modelo
                  </th>
                  <th className="border border-border px-2 py-1.5 text-[11px]">Tam</th>
                  <th className="border border-border px-2 py-1.5 text-[11px]">
                    Múltiplo prev.
                  </th>
                  <th className="border border-border px-2 py-1.5 text-[11px]">
                    Qtd cortada
                  </th>
                </tr>
              </thead>
              <tbody>
                {order.items.map((item) => (
                  <tr key={item.orderItemId}>
                    <td className="border border-border px-2 py-1.5 font-medium">
                      {item.modelName}
                    </td>
                    <td className="border border-border px-2 py-1.5 text-center">
                      {item.size}
                    </td>
                    <td className="border border-border px-2 py-1.5 text-center font-mono text-text3">
                      ×{item.plannedQuantity}
                    </td>
                    <td className="border border-border px-1 py-1 text-center">
                      <input
                        type="number"
                        min={0}
                        value={drafts[item.orderItemId] ?? ""}
                        onChange={(e) =>
                          setDrafts((prev) => ({
                            ...prev,
                            [item.orderItemId]: e.target.value,
                          }))
                        }
                        placeholder="0"
                        className="w-20 text-center font-mono text-xs px-1 py-0.5 border border-border2 rounded"
                      />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {submitError && submittingOrderId === order.orderId && (
            <div className="text-xs text-red bg-red-light border border-red-border rounded-lg px-3 py-2 mb-3">
              {submitError}
            </div>
          )}

          <button
            type="button"
            disabled={submittingOrderId !== null}
            onClick={() => submit(order)}
            className="px-3.5 py-1.5 rounded-lg text-xs font-medium bg-green text-white border border-green hover:opacity-90 transition-opacity disabled:opacity-60"
          >
            {submittingOrderId === order.orderId
              ? "Enviando..."
              : "Confirmar corte"}
          </button>
        </div>
      ))}
    </div>
  );
}
