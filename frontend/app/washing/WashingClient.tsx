"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import {
  completeLaundryPackage,
  getLaundryQueue,
  type LaundryPackageDto,
} from "@/lib/api/laundry";
import { useCurrentUser } from "@/lib/currentUser";

type State =
  | { kind: "idle" }
  | { kind: "loading" }
  | { kind: "ready"; packages: LaundryPackageDto[] }
  | { kind: "error"; message: string };

export function WashingClient() {
  const router = useRouter();
  const { userId } = useCurrentUser();
  const [state, setState] = useState<State>({ kind: "idle" });
  const [completingId, setCompletingId] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const refresh = async () => {
    setState({ kind: "loading" });
    try {
      const packages = await getLaundryQueue();
      setState({ kind: "ready", packages });
    } catch (err) {
      setState({
        kind: "error",
        message: err instanceof Error ? err.message : String(err),
      });
    }
  };

  useEffect(() => {
    refresh();
  }, []);

  const complete = async (pkg: LaundryPackageDto) => {
    if (!userId) {
      setActionError("Selecione um operador da lavanderia acima.");
      return;
    }
    setActionError(null);
    setCompletingId(pkg.packageId);
    try {
      await completeLaundryPackage(pkg.packageId, userId);
      await refresh();
      router.refresh();
    } catch (err) {
      setActionError(err instanceof Error ? err.message : String(err));
    } finally {
      setCompletingId(null);
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

  if (state.packages.length === 0) {
    return (
      <div className="rounded-2xl border border-border bg-card p-5 shadow-card">
        <p className="text-xs text-text3">Nenhum pacote aguardando lavagem.</p>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      {actionError && (
        <div className="text-xs text-red bg-red-light border border-red-border rounded-lg px-3 py-2">
          {actionError}
        </div>
      )}
      {state.packages.map((pkg) => (
        <div
          key={pkg.packageId}
          className="rounded-2xl border border-border bg-card p-5 shadow-card"
        >
          <div className="flex items-start justify-between gap-2 mb-2">
            <div>
              <div className="text-sm font-medium">
                Pacote #{pkg.number} —{" "}
                <span className="text-accent font-mono">{pkg.totalQuantity}</span> peças
              </div>
              <div className="text-[11px] text-text3 mt-0.5">
                Recebido em {new Date(pkg.sentAt).toLocaleString("pt-BR")}
              </div>
            </div>
            <span className="inline-block px-2 py-0.5 rounded-full text-[11px] bg-accent-light text-accent border border-accent-border">
              Aguardando
            </span>
          </div>
          <ul className="space-y-1 mb-3">
            {pkg.items.map((item) => (
              <li key={item.pipelineItemId} className="text-[11px] text-text2">
                <span
                  className="inline-block w-2 h-2 rounded-full mr-1.5 align-middle border border-black/10"
                  style={{ background: item.colorHex }}
                />
                {item.modelName} tam. {item.size}: <strong className="font-mono">{item.quantity}</strong>{" "}
                <span className="text-text3">— Ordem #{item.orderNumber}</span>
              </li>
            ))}
          </ul>
          <button
            type="button"
            disabled={completingId !== null || !userId}
            onClick={() => complete(pkg)}
            className="px-3.5 py-1.5 rounded-lg text-xs font-medium bg-green text-white border border-green hover:opacity-90 transition-opacity disabled:opacity-60"
          >
            {completingId === pkg.packageId
              ? "Confirmando..."
              : "✓ Tudo lavado — OK"}
          </button>
        </div>
      ))}
    </div>
  );
}
